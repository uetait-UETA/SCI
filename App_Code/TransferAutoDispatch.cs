using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;

/// <summary>
/// Auto-dispatch for transfers whose warehouses include at least one with BPLId=1.
/// After submit_DraftXsap (or smm_populate_Smm_Draft), calling RunAutoDispatch creates
/// the SAP B1 Stock Transfer Request automatically.
/// On SAP failure the local draft is deleted so the user can start over.
/// </summary>
public static class TransferAutoDispatch
{
    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true when at least one of the two warehouse codes has BPLId = 1 in SAP.
    /// </summary>
    public static bool IsBranch1Transfer(string fromWhs, string toWhs, string sapDb)
    {
        var db = new SqlDb();
        db.Connect();
        try
        {
            string sql = string.Format(
                "SELECT COUNT(1) FROM [{0}]..OWHS WITH(NOLOCK) WHERE WhsCode IN (@from, @to) AND BPLId = 1",
                sapDb);
            using (var cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@from", fromWhs);
                cmd.Parameters.AddWithValue("@to",   toWhs);
                return (int)cmd.ExecuteScalar() > 0;
            }
        }
        finally
        {
            db.Disconnect();
        }
    }

    /// <summary>
    /// Executes the automatic dispatch flow:
    ///   1. smm_populate_discrep_odrf
    ///   2. TmpQuantity = DraftQuantity for every line
    ///   3. POST /InventoryTransferRequests to SAP B1
    ///   4. Smm_populate_whs_transfers_Batch + smm_insert_Transdiscrep_audit_odrf
    ///
    /// Returns null on success.
    /// On any failure: cleans up all draft records for docEntry and returns the error message.
    /// </summary>
    /// <param name="sapDocNum">SAP document number created (0 if unknown/none).</param>
    public static string RunAutoDispatch(
        int docEntry, string companyId, string sapDb, string userApp, out int sapDocNum)
    {
        sapDocNum = 0;
        var db = new SqlDb();
        db.Connect();
        try
        {
            // Step 1: populate discrepancy tables
            using (var cmd = new SqlCommand("smm_populate_discrep_odrf", db.Conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@DocEntry",  docEntry);
                cmd.Parameters.AddWithValue("@CompanyId", companyId);
                cmd.ExecuteNonQuery();
            }

            // Step 2: set TmpQuantity = DraftQuantity for all lines (full auto-dispatch)
            using (var cmd = new SqlCommand(
                "UPDATE smm_Transdiscrep_drf1 SET TmpQuantity = DraftQuantity " +
                "WHERE CompanyId = @cid AND DocEntry = @de", db.Conn))
            {
                cmd.Parameters.AddWithValue("@cid", companyId);
                cmd.Parameters.AddWithValue("@de",  docEntry);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            db.Disconnect();
            return "Error preparando auto-dispatch: " + ex.Message;
        }

        // Step 3: SAP B1 Transfer Request — only when FROM=BODEGA (BPLId=1) and TO=TIENDA
        if (!IsBodegaToTiendaTransfer(db.Conn, docEntry, companyId, sapDb))
        {
            db.Disconnect();
            return null;
        }

        string sapError = CreateSapTransferRequest(db.Conn, docEntry, companyId, sapDb, userApp, out sapDocNum);
        if (sapError != null)
        {
            try { CleanupDraft(db.Conn, companyId, docEntry); } catch { }
            db.Disconnect();
            return sapError;
        }

        // Step 4: post-SAP local updates
        try
        {
            using (var cmd = new SqlCommand("Smm_populate_whs_transfers_Batch", db.Conn))
            {
                cmd.CommandType  = CommandType.StoredProcedure;
                cmd.CommandTimeout = 0;
                cmd.Parameters.AddWithValue("@CompanyId",   companyId);
                cmd.Parameters.AddWithValue("@DocEntryDrf", docEntry);
                cmd.Parameters.AddWithValue("@TypeTran",    "D");
                cmd.Parameters.AddWithValue("@UserApp",     userApp);
                cmd.ExecuteNonQuery();
            }

            // TraRec2 belongs exclusively to the Receive (OWTR) step — clear any value the SP may have set.
            using (var cmd = new SqlCommand(
                "UPDATE smm_Transdiscrep_odrf SET DocEntryTraRec2 = NULL, DocNumTraRec2 = NULL " +
                "WHERE CompanyId = @cid AND DocEntry = @de", db.Conn))
            {
                cmd.Parameters.AddWithValue("@cid", companyId);
                cmd.Parameters.AddWithValue("@de",  docEntry);
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SqlCommand("smm_insert_Transdiscrep_audit_odrf", db.Conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@CompanyId",   companyId);
                cmd.Parameters.AddWithValue("@DocEntry",    docEntry);
                cmd.Parameters.AddWithValue("@TypeTrans",   "D");
                cmd.Parameters.AddWithValue("@SourceTrans", "SISINV");
                cmd.ExecuteNonQuery();
            }

            db.Disconnect();
            return null; // success
        }
        catch (Exception ex)
        {
            db.Disconnect();
            return "Error post-SAP local: " + ex.Message;
        }
    }

    /// <summary>
    /// Validates auto-dispatch prerequisites using fromWhs/toWhs directly,
    /// before any draft is created. Returns null if the transfer can proceed,
    /// or an error message if a required configuration is missing.
    /// </summary>
    public static string ValidateAutoDispatch(string fromWhs, string toWhs, string companyId, string sapDb)
    {
        var db = new SqlDb();
        db.Connect();
        try
        {
            string sql = string.Format(
                @"SELECT ISNULL(tf.TYPEWHS,'') AS FromType,
                         ISNULL(wf.BPLId,0)   AS FromBPLId,
                         ISNULL(tt.TYPEWHS,'') AS ToType
                  FROM [{0}]..OWHS wf WITH(NOLOCK)
                  LEFT JOIN dbo.SMM_WHSTYPE tf WITH(NOLOCK)
                      ON tf.WHSCODE = wf.WhsCode AND tf.COMPANYID = @cid
                  LEFT JOIN dbo.SMM_WHSTYPE tt WITH(NOLOCK)
                      ON tt.WHSCODE = @toWhs    AND tt.COMPANYID = @cid
                  WHERE wf.WhsCode = @fromWhs", sapDb);

            bool isBodegaToTienda = false;
            using (var cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@fromWhs", fromWhs);
                cmd.Parameters.AddWithValue("@toWhs",   toWhs);
                cmd.Parameters.AddWithValue("@cid",     companyId);
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                        isBodegaToTienda =
                            string.Equals(dr["FromType"].ToString(), "BODEGA", StringComparison.OrdinalIgnoreCase)
                            && Convert.ToInt32(dr["FromBPLId"]) == 1
                            && string.Equals(dr["ToType"].ToString(), "TIENDA", StringComparison.OrdinalIgnoreCase);
                }
            }

            if (!isBodegaToTienda) return null; // not a BODEGA→TIENDA; no SO needed

            // BODEGA→TIENDA confirmed — check CardCode mapping for the destination warehouse
            string cardCodeSql = string.Format(
                "SELECT TOP 1 m.OinvCardCode FROM dbo.ApriCardCodeMapping m " +
                "JOIN [{0}]..OWHS w WITH(NOLOCK) ON w.BPLId = m.DestBPLId " +
                "WHERE w.WhsCode = @toWhs AND m.IsActive = 1", sapDb);

            string cardCode = null;
            using (var cmd = new SqlCommand(cardCodeSql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@toWhs", toWhs);
                object val = cmd.ExecuteScalar();
                if (val != null && val != DBNull.Value) cardCode = val.ToString();
            }

            if (string.IsNullOrEmpty(cardCode))
                return "No CardCode mapping found in ApriCardCodeMapping for destination warehouse '" + toWhs
                       + "'. Contact your administrator.";

            return null; // all checks passed
        }
        catch (Exception ex)
        {
            return "Error validating transfer prerequisites: " + ex.Message;
        }
        finally
        {
            db.Disconnect();
        }
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    // Creates a Sales Order (ORDR) in SAP B1 for BODEGA→TIENDA (inter-branch) transfers.
    // Price per line is taken from OITW.AvgPrice of the origin warehouse.
    private static string CreateSapTransferRequest(
        SqlConnection conn, int docEntry, string companyId, string sapDb,
        string userApp, out int sapDocNum)
    {
        sapDocNum = 0;
        string fromWhs = "", toWhs = "";
        var lines = new JArray();

        string sql = string.Format(@"
            SELECT h.FromWhsCode, h.ToWhsCode,
                   d.LineNum, d.ItemCode,
                   CAST(d.TmpQuantity AS int) AS Qty,
                   ISNULL(iw.AvgPrice, 0) AS UnitPrice
            FROM smm_Transdiscrep_odrf h WITH(NOLOCK)
            INNER JOIN smm_Transdiscrep_drf1 d WITH(NOLOCK)
                ON h.DocEntry = d.DocEntry AND h.CompanyId = d.CompanyId
            LEFT JOIN [{0}]..OITW iw WITH(NOLOCK)
                ON iw.ItemCode = d.ItemCode AND iw.WhsCode = h.FromWhsCode
            WHERE h.CompanyId = @cid AND h.DocEntry = @de
              AND d.TmpQuantity > 0
            ORDER BY d.LineNum", sapDb);

        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@cid", companyId);
            cmd.Parameters.AddWithValue("@de",  docEntry);

            var dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);

            if (dt.Rows.Count == 0) return null;

            fromWhs = dt.Rows[0]["FromWhsCode"].ToString();
            toWhs   = dt.Rows[0]["ToWhsCode"].ToString();

            foreach (DataRow row in dt.Rows)
            {
                lines.Add(new JObject(
                    new JProperty("ItemCode",      row["ItemCode"].ToString()),
                    new JProperty("Quantity",      Convert.ToInt32(row["Qty"])),
                    new JProperty("UnitPrice",     Convert.ToDecimal(row["UnitPrice"])),
                    new JProperty("WarehouseCode", fromWhs)
                ));
            }
        }

        if (lines.Count == 0) return null;

        string cardCode = GetOrderCardCode(conn, toWhs, sapDb);
        if (string.IsNullOrEmpty(cardCode))
            return "No CardCode mapping found in ApriCardCodeMapping for destination warehouse " + toWhs;

        string companyDb = ConfigurationManager.AppSettings["SL_CompanyDB"] ?? sapDb;

        string today = DateTime.Today.ToString("yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture);

        var payload = new JObject(
            new JProperty("CardCode",                  cardCode),
            new JProperty("BPL_IDAssignedToInvoice",   1),
            new JProperty("DocDate",                   today),
            new JProperty("DocDueDate",                today),
            new JProperty("TaxDate",                   today),
            new JProperty("U_BOL",                     docEntry.ToString()),
            new JProperty("U_DESPATCH",                userApp),
            new JProperty("U_ORITOWHS",                toWhs),
            new JProperty("DocumentLines",             lines)
        );

        var sl = new SapServiceLayer();
        try
        {
            sl.Login(companyDb);
            string response = sl.CreateSalesOrder(payload.ToString(Newtonsoft.Json.Formatting.None));

            try
            {
                var respObj  = JObject.Parse(response);
                int sapEntry = respObj["DocEntry"] != null ? Convert.ToInt32(respObj["DocEntry"]) : 0;
                sapDocNum    = respObj["DocNum"]   != null ? Convert.ToInt32(respObj["DocNum"])   : 0;

                if (sapEntry > 0)
                {
                    using (var upd = new SqlCommand(
                        "UPDATE smm_Transdiscrep_odrf " +
                        "SET DocEntryITR = @entry, DocNumITR = @num, TransferType = 'SO' " +
                        "WHERE CompanyId = @cid AND DocEntry = @de", conn))
                    {
                        upd.Parameters.AddWithValue("@entry", sapEntry);
                        upd.Parameters.AddWithValue("@num",   sapDocNum);
                        upd.Parameters.AddWithValue("@cid",   companyId);
                        upd.Parameters.AddWithValue("@de",    docEntry);
                        upd.ExecuteNonQuery();
                    }
                }
            }
            catch { }

            return null; // success
        }
        catch (System.Net.WebException wex)
        {
            return SapServiceLayer.GetSlErrorMessage(wex);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            sl.Logout();
        }
    }

    // Returns the inter-company customer CardCode for a destination warehouse,
    // looked up via ApriCardCodeMapping.DestBPLId = OWHS.BPLId.
    private static string GetOrderCardCode(SqlConnection conn, string toWhs, string sapDb)
    {
        string sql = string.Format(
            "SELECT TOP 1 m.OinvCardCode FROM dbo.ApriCardCodeMapping m " +
            "JOIN [{0}]..OWHS w WITH(NOLOCK) ON w.BPLId = m.DestBPLId " +
            "WHERE w.WhsCode = @toWhs AND m.IsActive = 1", sapDb);
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@toWhs", toWhs);
            object val = cmd.ExecuteScalar();
            return val != null && val != DBNull.Value ? val.ToString() : null;
        }
    }

    private static bool IsBodegaToTiendaTransfer(SqlConnection conn, int docEntry, string companyId, string sapDb)
    {
        // Uses SMM_WHSTYPE.TYPEWHS (the app's warehouse classification) instead of
        // OWHS.U_Type (SAP field), which may differ. TO warehouse is read from the
        // detail lines since smm_Transdiscrep_odrf.ToWhsCode can be NULL.
        string sql = string.Format(
            @"SELECT TOP 1
                  ISNULL(tf.TYPEWHS,'') AS FromType,
                  ISNULL(wf.BPLId,0)   AS FromBPLId,
                  ISNULL(tt.TYPEWHS,'') AS ToType
              FROM smm_Transdiscrep_odrf h WITH(NOLOCK)
              LEFT JOIN [{0}]..OWHS        wf WITH(NOLOCK) ON wf.WhsCode  = h.FromWhsCode
              LEFT JOIN dbo.SMM_WHSTYPE   tf WITH(NOLOCK) ON tf.WHSCODE  = h.FromWhsCode AND tf.COMPANYID = @cid
              LEFT JOIN smm_Transdiscrep_drf1 d WITH(NOLOCK)
                  ON d.DocEntry = h.DocEntry AND d.CompanyId = h.CompanyId
              LEFT JOIN dbo.SMM_WHSTYPE   tt WITH(NOLOCK) ON tt.WHSCODE  = d.ToWhsCode  AND tt.COMPANYID = @cid
              WHERE h.CompanyId = @cid AND h.DocEntry = @de", sapDb);
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@cid", companyId);
            cmd.Parameters.AddWithValue("@de",  docEntry);
            using (var dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                    return string.Equals(dr["FromType"].ToString(), "BODEGA", StringComparison.OrdinalIgnoreCase)
                           && Convert.ToInt32(dr["FromBPLId"]) == 1
                           && string.Equals(dr["ToType"].ToString(),  "TIENDA", StringComparison.OrdinalIgnoreCase);
            }
        }
        return false;
    }

    // Deletes all local records for docEntry to revert a failed auto-dispatch.
    private static void CleanupDraft(SqlConnection conn, string companyId, int docEntry)
    {
        string[] tables = {
            "smm_Transdiscrep_drf1", "smm_Transdiscrep_odrf",
            "Smm_drf1",              "SMM_odrf",
            "smm_TransXsap_drf1",    "smm_TransXsap_odrf"
        };
        foreach (string table in tables)
        {
            try
            {
                using (var cmd = new SqlCommand(
                    "DELETE FROM dbo." + table + " WHERE CompanyId = @cid AND DocEntry = @de",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@cid", companyId);
                    cmd.Parameters.AddWithValue("@de",  docEntry);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { /* best-effort per table */ }
        }
    }
}
