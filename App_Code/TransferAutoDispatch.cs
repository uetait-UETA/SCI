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

    // ── Private helpers ─────────────────────────────────────────────────────

    private static string CreateSapTransferRequest(
        SqlConnection conn, int docEntry, string companyId, string sapDb,
        string userApp, out int sapDocNum)
    {
        sapDocNum = 0;
        string fromWhs = "", toWhs = "";
        var lines = new JArray();

        string sql = @"
            SELECT h.FromWhsCode, h.ToWhsCode,
                   d.LineNum, d.ItemCode, d.ToWhsCode AS LineToWhs,
                   CAST(d.TmpQuantity AS int) AS Qty
            FROM smm_Transdiscrep_odrf h WITH(NOLOCK)
            INNER JOIN smm_Transdiscrep_drf1 d WITH(NOLOCK)
                ON h.DocEntry = d.DocEntry AND h.CompanyId = d.CompanyId
            WHERE h.CompanyId = @cid AND h.DocEntry = @de
              AND d.TmpQuantity > 0
            ORDER BY d.LineNum";

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
                    new JProperty("ItemCode",          row["ItemCode"].ToString()),
                    new JProperty("Quantity",          Convert.ToInt32(row["Qty"])),
                    new JProperty("FromWarehouseCode", fromWhs),
                    new JProperty("WarehouseCode",     row["LineToWhs"].ToString())
                ));
            }
        }

        if (lines.Count == 0) return null;

        string companyDb  = ConfigurationManager.AppSettings["SL_CompanyDB"] ?? sapDb;
        string actionCode = "CREATE";

        var payload = new JObject(
            new JProperty("FromWarehouse",      fromWhs),
            new JProperty("ToWarehouse",        toWhs),
            new JProperty("U_BOL",              docEntry.ToString()),
            new JProperty("U_DESPATCH",         userApp),
            new JProperty("U_ORITOWHS",         fromWhs),
            new JProperty("U_ACTION_CODE",      actionCode),
            new JProperty("StockTransferLines", lines)
        );

        var sl = new SapServiceLayer();
        try
        {
            sl.Login(companyDb);
            string response = sl.CreateInventoryTransferRequest(
                payload.ToString(Newtonsoft.Json.Formatting.None));

            try
            {
                var respObj  = JObject.Parse(response);
                int sapEntry = respObj["DocEntry"] != null ? Convert.ToInt32(respObj["DocEntry"]) : 0;
                sapDocNum    = respObj["DocNum"]   != null ? Convert.ToInt32(respObj["DocNum"])   : 0;

                if (sapEntry > 0)
                {
                    using (var upd = new SqlCommand(
                        "UPDATE smm_Transdiscrep_odrf " +
                        "SET DocEntryITR = @entry, DocNumITR = @num " +
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
