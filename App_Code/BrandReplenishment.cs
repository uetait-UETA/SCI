using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Batch replenishment by brand priority (Repln_Brand_Priority table).
/// For each active warehouse pair whose destination has brand entries:
///   1. Fetches items below min using the same NetInventory CTE as CreateTransfer.aspx.
///   2. Creates drafts in smm_odrf / smm_drf1 (max 20 lines each).
///   3. Calls TransferAutoDispatch.RunAutoDispatch per draft:
///        Branch 4 destination → ORDR (Sales Order) in SAP B1
///        Branch 3 destination → OWTQ (ITR) in SAP B1
///        Tienda→Tienda         → draft only, no SAP doc
/// </summary>
public static class BrandReplenishment
{
    private const int MaxLinesPerDraft = 20;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true when the given destination warehouse has brand priorities configured,
    /// meaning the category-based replenishment should be skipped for this pair.
    /// </summary>
    public static bool HasBrandPriorities(string companyId, string toWhs)
    {
        var db = new SqlDb();
        db.Connect();
        try
        {
            using (var cmd = new SqlCommand(
                "SELECT COUNT(1) FROM Repln_Brand_Priority WITH(NOLOCK) " +
                "WHERE Company = @cid AND Location = @loc", db.Conn))
            {
                cmd.Parameters.AddWithValue("@cid", companyId);
                cmd.Parameters.AddWithValue("@loc", toWhs);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }
        finally { db.Disconnect(); }
    }

    /// <summary>
    /// Processes all active warehouse pairs for the given company that have brand
    /// priorities configured. Returns null on full success, or an error summary.
    /// </summary>
    public static string Run(string companyId, string sapDb, string userApp)
    {
        List<WarehousePair> pairs = GetWarehousePairs(companyId);
        if (pairs.Count == 0) return null;

        var errors = new System.Text.StringBuilder();
        foreach (WarehousePair pair in pairs)
        {
            try
            {
                string err = ProcessPair(companyId, sapDb, pair.FromWhs, pair.ToWhs, userApp);
                if (err != null)
                    errors.AppendLine(string.Format("[{0}→{1}] {2}", pair.FromWhs, pair.ToWhs, err));
            }
            catch (Exception ex)
            {
                errors.AppendLine(string.Format("[{0}→{1}] {2}", pair.FromWhs, pair.ToWhs, ex.Message));
            }
        }
        return errors.Length > 0 ? errors.ToString() : null;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string ProcessPair(string companyId, string sapDb,
        string fromWhs, string toWhs, string userApp)
    {
        DataTable items = GetItemsToReplenish(companyId, sapDb, fromWhs, toWhs);
        if (items == null || items.Rows.Count == 0) return null;

        int serieDoc = 20;
        int.TryParse(ConfigurationManager.AppSettings["CreateTransferSerieDoc"], out serieDoc);

        var errors = new System.Text.StringBuilder();
        int lineNum  = 0;
        int docEntry = 0;
        var db = new SqlDb();

        foreach (DataRow row in items.Rows)
        {
            int transferQty = Convert.ToInt32(row["Transfer_Quantity"]);
            if (transferQty <= 0) continue;

            if (lineNum == 0)
            {
                db.Connect();
                docEntry = GetNextDocEntry(db.Conn);
                InsertDraftHeader(db.Conn, companyId, docEntry, fromWhs, userApp, serieDoc);
            }

            InsertDraftLine(db.Conn, companyId, docEntry, lineNum,
                row["ItemCode"].ToString(),
                row["ItemDescription"].ToString(),
                transferQty,
                toWhs);
            lineNum++;

            if (lineNum >= MaxLinesPerDraft)
            {
                db.Disconnect();
                string batchErr = FlushDraft(docEntry, companyId, sapDb, userApp);
                if (batchErr != null)
                    errors.AppendLine(string.Format("Draft {0}: {1}", docEntry, batchErr));
                lineNum = 0;
            }
        }

        // Dispatch any partial draft that didn't fill the 20-line limit.
        if (lineNum > 0)
        {
            if (db.DbConnectionState == ConnectionState.Open)
                db.Disconnect();
            string finalErr = FlushDraft(docEntry, companyId, sapDb, userApp);
            if (finalErr != null)
                errors.AppendLine(string.Format("Draft {0}: {1}", docEntry, finalErr));
        }
        else if (db.DbConnectionState == ConnectionState.Open)
        {
            db.Disconnect();
        }

        return errors.Length > 0 ? errors.ToString() : null;
    }

    private static string FlushDraft(int docEntry, string companyId, string sapDb, string userApp)
    {
        int    sapDocNum;
        string sapDocType;
        return TransferAutoDispatch.RunAutoDispatch(
            docEntry, companyId, sapDb, userApp, out sapDocNum, out sapDocType);
    }

    // Returns all active (fromWhs, toWhs) pairs where toWhs has brand priorities.
    private static List<WarehousePair> GetWarehousePairs(string companyId)
    {
        var list = new List<WarehousePair>();
        var db   = new SqlDb();
        db.Connect();
        try
        {
            string sql =
                @"SELECT DISTINCT bt.BodegaID AS FromWhs, rbp.Location AS ToWhs
                  FROM Repln_Brand_Priority rbp WITH(NOLOCK)
                  INNER JOIN SMM_REA_BODEGA_TIENDAS bt WITH(NOLOCK)
                      ON bt.CompanyID = rbp.Company AND bt.TiendaID = rbp.Location
                  WHERE rbp.Company = @cid AND bt.isActive = 1
                  ORDER BY bt.BodegaID, rbp.Location";

            using (var cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@cid", companyId);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                        list.Add(new WarehousePair {
                            FromWhs = rdr.GetString(0),
                            ToWhs   = rdr.GetString(1)
                        });
                }
            }
        }
        finally { db.Disconnect(); }
        return list;
    }

    // Returns items that need replenishment for the given pair, ordered by brand priority.
    private static DataTable GetItemsToReplenish(string companyId, string sapDb,
        string fromWhs, string toWhs)
    {
        // NetInventory CTE uses {0} = companyId; cross-db OITM ref uses {1} = sapDb.
        string sql = string.Format(
            Queries.With_NetInventory() + @"
            SELECT
                bp.Priority,
                ISNULL(d.U_Brand, '') AS U_Brand,
                src.ItemCode,
                d.ItemName  AS ItemDescription,
                d.ItmsGrpCod AS GroupCode,
                CASE
                    WHEN (src.OnHand - src.in_transit_out) >= (b.max_qty - dst.net_inventory)
                         THEN (b.max_qty - dst.net_inventory)
                              - ((b.max_qty - dst.net_inventory)
                                 % CASE WHEN ISNULL(mult.ORDER_MULTIPLE,'E') = 'E'
                                        THEN 1 ELSE ISNULL(d.U_BOT,1) END)
                    ELSE (src.OnHand - src.in_transit_out)
                         - ((src.OnHand - src.in_transit_out)
                            % CASE WHEN ISNULL(mult.ORDER_MULTIPLE,'E') = 'E'
                                   THEN 1 ELSE ISNULL(d.U_BOT,1) END)
                END AS Transfer_Quantity
            FROM NetInventory src
            INNER JOIN rss_store_item_min_max b WITH(NOLOCK)
                ON b.Item = src.ItemCode AND b.CompanyId = src.CompanyId AND b.Loc = @toWhs
            INNER JOIN NetInventory dst
                ON dst.ItemCode = src.ItemCode AND dst.CompanyId = src.CompanyId
               AND dst.WhsCode = @toWhs
            INNER JOIN [{1}].dbo.OITM d WITH(NOLOCK)
                ON d.ItemCode = src.ItemCode
            INNER JOIN rss_loc_dept_multiple mult WITH(NOLOCK)
                ON mult.dept = d.ItmsGrpCod AND mult.CompanyId = src.CompanyId
               AND mult.LOC = @toWhs
            INNER JOIN Repln_Brand_Priority bp WITH(NOLOCK)
                ON bp.Company = src.CompanyId AND bp.Location = @toWhs
               AND ISNULL(bp.Brand,'') COLLATE DATABASE_DEFAULT
                 = ISNULL(d.U_Brand,'') COLLATE DATABASE_DEFAULT
            WHERE src.WhsCode = @fromWhs
              AND (src.OnHand - src.in_transit_out) > 0
              AND b.hold = 0
              AND dst.net_inventory < b.min_qty
              AND (b.max_qty - dst.net_inventory) > 0
            ORDER BY bp.Priority, d.U_Brand, src.ItemCode",
            companyId,   // {0} for NetInventory CTE
            sapDb        // {1} for cross-db OITM
        );

        var db = new SqlDb();
        db.Connect();
        try
        {
            var dt = new DataTable();
            using (var cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.CommandTimeout = 120;
                cmd.Parameters.AddWithValue("@fromWhs", fromWhs);
                cmd.Parameters.AddWithValue("@toWhs",   toWhs);
                new SqlDataAdapter(cmd).Fill(dt);
            }
            return dt;
        }
        finally { db.Disconnect(); }
    }

    // Calls Smm_Rea_Odrf_seq to get the next draft DocEntry.
    private static int GetNextDocEntry(SqlConnection conn)
    {
        using (var cmd = new SqlCommand("Smm_Rea_Odrf_seq", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@p1", 1);
            var outParam = new SqlParameter("@OutputParameter", SqlDbType.Int)
                { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(outParam);
            cmd.ExecuteNonQuery();
            return (int)outParam.Value;
        }
    }

    private static void InsertDraftHeader(SqlConnection conn, string companyId,
        int docEntry, string fromWhs, string userApp, int serieDoc)
    {
        string dateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
        string comments = string.Format("SMM:User<{0}>DateTime<{1}>", userApp, dateTime);
        int    docTime  = int.Parse(DateTime.Now.ToString("HHmm"));

        using (var cmd = new SqlCommand(
            @"INSERT INTO SMM_ODRF WITH(ROWLOCK)
              (CompanyId, DocEntry, DocNum, DocType, CANCELED, DocStatus,
               DocDate, DocTime, DocDueDate, ObjType,
               CardCode, CardName, DocCur, DocRate, DocTotal, DocTotalFC,
               Ref1, Ref2, Comments, CreateDate,
               Series, Filler, FromDate, ToDate,
               U_RECEIVE, U_DESPATCH, U_ORITOWHS)
              VALUES
              (@cid, @de, @de, 'I', 'N', 'O',
               GETDATE(), @docTime, GETDATE(), '67',
               NULL, NULL, 'USD', 1.00, 100.00, 0.00,
               NULL, NULL, @comments, GETDATE(),
               @serie, @fromWhs, GETDATE(), GETDATE(),
               NULL, NULL, @user)", conn))
        {
            cmd.Parameters.AddWithValue("@cid",      companyId);
            cmd.Parameters.AddWithValue("@de",       docEntry);
            cmd.Parameters.AddWithValue("@docTime",  docTime);
            cmd.Parameters.AddWithValue("@comments", comments);
            cmd.Parameters.AddWithValue("@serie",    serieDoc);
            cmd.Parameters.AddWithValue("@fromWhs",  fromWhs);
            cmd.Parameters.AddWithValue("@user",     userApp);
            cmd.ExecuteNonQuery();
        }
    }

    private static void InsertDraftLine(SqlConnection conn, string companyId,
        int docEntry, int lineNum, string itemCode, string desc, int qty, string toWhs)
    {
        using (var cmd = new SqlCommand(
            @"INSERT INTO SMM_DRF1 WITH(ROWLOCK)
              (CompanyId, DocEntry, LineNum, TargetType,
               ItemCode, Dscription, Quantity,
               Price, Currency, Rate, LineTotal, TotalFrgn,
               SerialNum, WhsCode, DocDate, OcrCode)
              VALUES
              (@cid, @de, @line, -1,
               @item, @desc, @qty,
               1.00, 'USD', 1.00, 1.00, 0.00,
               NULL, @toWhs, GETDATE(), NULL)", conn))
        {
            cmd.Parameters.AddWithValue("@cid",   companyId);
            cmd.Parameters.AddWithValue("@de",    docEntry);
            cmd.Parameters.AddWithValue("@line",  lineNum);
            cmd.Parameters.AddWithValue("@item",  itemCode);
            cmd.Parameters.AddWithValue("@desc",  desc);
            cmd.Parameters.AddWithValue("@qty",   qty);
            cmd.Parameters.AddWithValue("@toWhs", toWhs);
            cmd.ExecuteNonQuery();
        }
    }

    private struct WarehousePair
    {
        public string FromWhs;
        public string ToWhs;
    }
}
