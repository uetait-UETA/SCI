using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;

/// <summary>
/// Data-access and payload-builder for the Goods Receipt PO feature.
/// Reads from SMM database (mapping/log tables) and cross-DB from origin company.
/// </summary>
public class GoodsReceipt
{
    private readonly SqlDb  _db;
    private readonly string _originCompany;

    public GoodsReceipt()
    {
        _db            = new SqlDb();
        _originCompany = ConfigurationManager.AppSettings["SL_OriginCompany"] ?? "";
    }

    public string LastError { get; private set; }

    // ── Store listing (legacy) ───────────────────────────────────────────────

    public DataTable GetStores()
    {
        LastError = null;
        var dt = new DataTable();
        dt.Columns.Add("StoreCode", typeof(string));
        dt.Columns.Add("StoreName", typeof(string));

        try
        {
            _db.Connect();
            _db.cmd.CommandText = @"
                SELECT DISTINCT StoreCode, StoreName
                FROM   dbo.GrpoStoreMapping " + Queries.WITH_NOLOCK + @"
                WHERE  IsActive = 1
                ORDER  BY StoreName";
            _db.cmd.CommandType = CommandType.Text;
            dt.Load(_db.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        finally { _db.Disconnect(); }
        return dt;
    }

    // ── Warehouses for the selected company ──────────────────────────────────

    public DataTable GetWarehouses(string sapDb)
    {
        LastError = null;
        var dt = new DataTable();
        try
        {
            _db.Connect();
            string sql = string.Format(@"
                SELECT WhsCode,
                       WhsCode + ' - ' + LTRIM(RTRIM(WhsName)) AS WhsDisplay
                FROM   {0}..OWHS " + Queries.WITH_NOLOCK + @"
                WHERE  Inactive = 'N'
                ORDER  BY WhsCode", sapDb);
            _db.cmd.CommandText = sql;
            _db.cmd.CommandType = CommandType.Text;
            dt.Load(_db.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
        finally { _db.Disconnect(); }
        return dt;
    }

    // ── Open A/P Invoices (OPCH) from the selected company ───────────────────

    public DataTable GetOpenInvoices(string sapDb, DateTime? fromDate, DateTime? toDate)
    {
        var dt = new DataTable();
        try
        {
            _db.Connect();

            string dateFrom = fromDate.HasValue
                ? fromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : "2000-01-01";
            string dateTo = toDate.HasValue
                ? toDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : DateTime.Today.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            string sql = string.Format(@"
SELECT
    p.DocEntry,
    p.DocNum,
    CONVERT(DATE, p.DocDate)  AS DocDate,
    p.CardCode,
    p.CardName,
    ISNULL(p.NumAtCard, '')   AS NumAtCard,
    ISNULL(p.Comments,  '')   AS Comments,
    p.DocTotal,
    p.DocCur
FROM   {0}.dbo.OPCH p " + Queries.WITH_NOLOCK + @"
WHERE  p.DocStatus = 'O'
  AND  p.DocDate  >= '{1}'
  AND  p.DocDate  <= '{2}'
ORDER  BY p.DocDate DESC, p.DocNum DESC",
                sapDb, dateFrom, dateTo);

            _db.cmd.CommandText = sql;
            _db.cmd.CommandType = CommandType.Text;
            dt.Load(_db.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
        }
        finally { _db.Disconnect(); }
        return dt;
    }

    // ── Pending invoices (legacy — store-mapping flow) ───────────────────────

    public DataTable GetPendingInvoices(string storeCodeList,
        DateTime? fromDate, DateTime? toDate)
    {
        var dt = new DataTable();
        try
        {
            _db.Connect();

            string dateFrom = fromDate.HasValue
                ? fromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : "2000-01-01";
            string dateTo = toDate.HasValue
                ? toDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : DateTime.Today.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            string sql = @"
SELECT
    p.DocEntry,
    p.DocNum,
    CONVERT(DATE, p.DocDate)     AS DocDate,
    p.CardCode,
    p.CardName,
    ISNULL(p.NumAtCard, '')      AS NumAtCard,
    ISNULL(p.Comments,  '')      AS Comments,
    p.DocTotal,
    p.DocCur,
    m.StoreCode,
    m.StoreName,
    m.DestCardCode,
    m.DestWhsCode
FROM   {0}.dbo.OPCH p " + Queries.WITH_NOLOCK + @"
JOIN   dbo.GrpoStoreMapping m " + Queries.WITH_NOLOCK + @"
           ON  m.OriginCardCode = p.CardCode
           AND m.IsActive = 1
WHERE  p.isIns     = 'N'
  AND  p.DocStatus = 'O'
  AND  m.StoreCode  IN ({1})
  AND  p.DocDate   >= '{2}'
  AND  p.DocDate   <= '{3}'
  AND  NOT EXISTS (
           SELECT 1
           FROM   dbo.GrpoReceiptLog r " + Queries.WITH_NOLOCK + @"
           WHERE  r.OriginDocEntry = p.DocEntry
             AND  r.OriginCompany  = '{0}'
             AND  r.Status         = 'SUCCESS'
       )
ORDER  BY p.DocDate DESC, p.DocNum DESC";

            sql = string.Format(sql, _originCompany, storeCodeList, dateFrom, dateTo);
            _db.cmd.CommandText = sql;
            _db.cmd.CommandType = CommandType.Text;
            dt.Load(_db.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
        }
        finally { _db.Disconnect(); }
        return dt;
    }

    // ── Pending Transfer Requests for a warehouse ────────────────────────────

    public DataTable GetPendingTransferRequests(string sapDb, string whsCode)
    {
        LastError = null;
        var dt = new DataTable();
        dt.Columns.Add("LocalDocEntry",  typeof(int));
        dt.Columns.Add("LocalDocNum",    typeof(int));
        dt.Columns.Add("DocDate",        typeof(object));
        dt.Columns.Add("FromWhsCode",    typeof(string));
        dt.Columns.Add("ToWhsCode",      typeof(string));
        dt.Columns.Add("SapTrReqEntry",  typeof(int));
        dt.Columns.Add("SapTrReqDocNum", typeof(int));
        dt.Columns.Add("DispatchUser",   typeof(string));

        try
        {
            _db.Connect();
            string sql = string.Format(@"
                SELECT
                    o.DocEntry                       AS LocalDocEntry,
                    o.DocNum                         AS LocalDocNum,
                    CONVERT(DATE, o.DocDate)         AS DocDate,
                    o.FromWhsCode,
                    o.ToWhsCode,
                    o.DocEntryITR                    AS SapTrReqEntry,
                    ISNULL(o.DocNumITR, 0)           AS SapTrReqDocNum,
                    ISNULL(o.userdispatch, '')        AS DispatchUser
                FROM smm_Transdiscrep_odrf o " + Queries.WITH_NOLOCK + @"
                WHERE o.CompanyId   = '{0}'
                  AND o.Dispatched  = 'Y'
                  AND o.Received    = 'N'
                  AND o.DocEntryITR > 0
                  AND o.FromWhsCode = '{1}'
                ORDER BY o.DocDate DESC, o.DocNum DESC",
                sapDb, whsCode.Replace("'", "''"));
            _db.cmd.CommandText = sql;
            _db.cmd.CommandType = CommandType.Text;
            dt.Load(_db.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            dt.Clear();
        }
        finally { _db.Disconnect(); }
        return dt;
    }

    // ── Lines of a Transfer Request (dispatch quantities) ────────────────────

    public DataTable GetTransferRequestLines(string sapDb, int localDocEntry)
    {
        var dt = new DataTable();
        try
        {
            _db.Connect();
            string sql = string.Format(@"
                SELECT d.LineNum,
                       d.ItemCode,
                       d.ToWhsCode,
                       CAST(d.DispatchQuantity AS int) AS Qty,
                       h.FromWhsCode
                FROM smm_Transdiscrep_drf1 d " + Queries.WITH_NOLOCK + @"
                INNER JOIN smm_Transdiscrep_odrf h " + Queries.WITH_NOLOCK + @"
                    ON d.DocEntry = h.DocEntry AND d.CompanyId = h.CompanyId
                WHERE d.CompanyId        = '{0}'
                  AND d.DocEntry         = {1}
                  AND d.DispatchQuantity > 0
                ORDER BY d.LineNum",
                sapDb, localDocEntry);
            _db.cmd.CommandText = sql;
            _db.cmd.CommandType = CommandType.Text;
            dt.Load(_db.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
        }
        finally { _db.Disconnect(); }
        return dt;
    }

    // ── Mark Transfer as received and store SAP Stock Transfer reference ──────

    public void MarkTransferReceived(string sapDb, int localDocEntry,
        int sapStEntry, int sapStDocNum, string userId)
    {
        try
        {
            _db.Connect();
            SqlCommand cmd = new SqlCommand(@"
                UPDATE smm_Transdiscrep_odrf
                SET    Received         = 'Y',
                       userreceive      = @UserId,
                       DocEntryTraRec2  = @StEntry,
                       DocNumTraRec2    = @StDocNum
                WHERE  CompanyId = @Company
                  AND  DocEntry   = @LocalEntry",
                _db.Conn);
            cmd.Parameters.AddWithValue("@UserId",     userId     ?? "");
            cmd.Parameters.AddWithValue("@StEntry",    sapStEntry);
            cmd.Parameters.AddWithValue("@StDocNum",   sapStDocNum);
            cmd.Parameters.AddWithValue("@Company",    sapDb      ?? "");
            cmd.Parameters.AddWithValue("@LocalEntry", localDocEntry);
            cmd.ExecuteNonQuery();
        }
        catch { }
        finally { _db.Disconnect(); }
    }

    // ── Invoice lines ────────────────────────────────────────────────────────

    public DataTable GetInvoiceLines(int docEntry)
    {
        var dt = new DataTable();
        try
        {
            _db.Connect();
            string sql = @"
SELECT
    LineNum,
    ItemCode,
    Dscription,
    Quantity,
    Price,
    WhsCode,
    ISNULL(unitMsr, '') AS UoMCode
FROM   {0}.dbo.PCH1 " + Queries.WITH_NOLOCK + @"
WHERE  DocEntry = {1}
ORDER  BY LineNum";
            sql = string.Format(sql, _originCompany, docEntry);
            _db.cmd.CommandText = sql;
            _db.cmd.CommandType = CommandType.Text;
            dt.Load(_db.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
        }
        finally { _db.Disconnect(); }
        return dt;
    }

    // ── Audit log ────────────────────────────────────────────────────────────

    public string LogReceipt(int originDocEntry, int originDocNum,
        int destDocEntry, int destDocNum,
        string destCardCode, string destWhsCode,
        string receivedBy, string status, string errorMsg)
    {
        try
        {
            _db.Connect();
            _db.cmd.CommandText = @"
INSERT INTO dbo.GrpoReceiptLog
    (OriginCompany, OriginDocEntry, OriginDocNum,
     DestCompany,   DestDocEntry,   DestDocNum,
     DestCardCode,  DestWhsCode,
     ReceivedBy, ReceivedAt, Status, ErrorMsg)
VALUES
    (@OriginCompany, @OriginDocEntry, @OriginDocNum,
     @DestCompany,   @DestDocEntry,   @DestDocNum,
     @DestCardCode,  @DestWhsCode,
     @ReceivedBy, GETDATE(), @Status, @ErrorMsg)";
            _db.cmd.CommandType = CommandType.Text;
            _db.cmd.Parameters.Clear();
            _db.cmd.Parameters.Add(new SqlParameter("@OriginCompany",  _originCompany));
            _db.cmd.Parameters.Add(new SqlParameter("@OriginDocEntry", originDocEntry));
            _db.cmd.Parameters.Add(new SqlParameter("@OriginDocNum",   originDocNum));
            _db.cmd.Parameters.Add(new SqlParameter("@DestCompany",
                ConfigurationManager.AppSettings["SiteName"] ?? ""));
            _db.cmd.Parameters.Add(new SqlParameter("@DestDocEntry",   destDocEntry));
            _db.cmd.Parameters.Add(new SqlParameter("@DestDocNum",     destDocNum));
            _db.cmd.Parameters.Add(new SqlParameter("@DestCardCode",   destCardCode ?? ""));
            _db.cmd.Parameters.Add(new SqlParameter("@DestWhsCode",    destWhsCode  ?? ""));
            _db.cmd.Parameters.Add(new SqlParameter("@ReceivedBy",     receivedBy   ?? ""));
            _db.cmd.Parameters.Add(new SqlParameter("@Status",         status       ?? ""));
            _db.cmd.Parameters.Add(new SqlParameter("@ErrorMsg",       errorMsg     ?? ""));
            _db.cmd.ExecuteNonQuery();
            return "1";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally { _db.Disconnect(); }
    }

    // ── GRPO payload builder ─────────────────────────────────────────────────

    /// <summary>
    /// Builds JSON for a standalone GRPO (no base-document cross-company ref).
    /// </summary>
    public string BuildGrpoPayload(string destCardCode, string destWhsCode,
        int originDocNum, string numAtCard, string originCompany, DataTable dtLines)
    {
        string today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var sb = new StringBuilder();

        sb.Append("{");
        sb.AppendFormat("\"CardCode\":\"{0}\",",    EscJson(destCardCode));
        sb.AppendFormat("\"DocDate\":\"{0}\",",     today);
        sb.AppendFormat("\"TaxDate\":\"{0}\",",     today);
        sb.AppendFormat("\"DocDueDate\":\"{0}\",",  today);

        if (!string.IsNullOrEmpty(numAtCard))
            sb.AppendFormat("\"NumAtCard\":\"{0}\",", EscJson(numAtCard));

        sb.AppendFormat("\"Comments\":\"Receipt from {0} Invoice #{1}\",",
            EscJson(originCompany), originDocNum);

        sb.Append("\"DocumentLines\":[");
        for (int i = 0; i < dtLines.Rows.Count; i++)
        {
            if (i > 0) sb.Append(",");
            DataRow r     = dtLines.Rows[i];
            decimal qty   = Convert.ToDecimal(r["Quantity"]);
            decimal price = Convert.ToDecimal(r["Price"]);
            string  uom   = r["UoMCode"].ToString();

            sb.Append("{");
            sb.AppendFormat("\"ItemCode\":\"{0}\",",     EscJson(r["ItemCode"].ToString()));
            sb.AppendFormat("\"Quantity\":{0},",         qty.ToString("0.######",   CultureInfo.InvariantCulture));
            sb.AppendFormat("\"UnitPrice\":{0},",        price.ToString("0.######", CultureInfo.InvariantCulture));
            sb.AppendFormat("\"WarehouseCode\":\"{0}\"", EscJson(destWhsCode));
            if (!string.IsNullOrEmpty(uom))
                sb.AppendFormat(",\"UoMCode\":\"{0}\"", EscJson(uom));
            sb.Append("}");
        }
        sb.Append("]}");

        return sb.ToString();
    }

    private static string EscJson(string s)
    {
        return (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"")
                        .Replace("\n", "\\n").Replace("\r", "\\r");
    }
}
