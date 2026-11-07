using System;
using System.Data;
using System.Data.SqlClient;

// Backs FacturacionSAP.aspx — the manual-review screen for CreaFacturasVentasSAP.
// Computes pending invoicing groups live from la_store_sales + ADR_TIENDA_SERIE
// (no la_invoice_preview staging table needed for DFS: the scheduled --dry-run is not required).
// All queries are scoped to the user's branch via bplId = Session["BranchId"] (SAP BPL).
//
// DFS stores use la_store_sales.TRANSTYPE (DF/DP) to separate Duty-Free from Duty-Paid items.
// Each (CardCode, TransType, FechaDoc, EsDevolucion) combination becomes a separate OINV/ORIN.
public class InvoicePreview
{
    public string LastError;

    // ── Completeness against the POS ─────────────────────────────────────────────
    // SCI only checks what is loaded (SAP deliveries vs la_store_payments): a sale that never
    // reached la_store_sales, together with its payment, still "matches". Found 2026-11-01: an
    // ExtraeCreaDocSalesSAP bug skipped 26 POS lines and C3111 DP 15/10 and DF 19/10 showed Ready
    // without 4 sales. This compares the POS itself (linked server) with what is loaded.
    // Expects #ss (la_store_sales of @desde-1 .. @hasta+1) and #sp (la_store_payments of the range),
    // @desde, @hasta, @bplId, @deeeItem. Declares @posOk (0 = POS not reachable) and creates:
    //   #posmiss (CARCODE, TransType, FechaDoc, TransactionID, Monto) - POS lines not in la_store_sales
    //   #paymiss (CARCODE, TransType, FechaDoc, TransactionID, Monto) - POS tenders not in la_store_payments
    // Same filters as ExtraeCreaDocSalesSAP (view type, SELL/RETURN, DF items excluded from DP
    // transactions, registers without ADR_TIENDA_SERIE mapping are not loaded there either).
    internal static string PosGapsSql()
    {
        string posDb = System.Configuration.ConfigurationManager.AppSettings["FacturacionSAP_PosDb"];
        if (string.IsNullOrWhiteSpace(posDb) || !System.Text.RegularExpressions.Regex.IsMatch(posDb, @"^[A-Za-z0-9_\.\[\]\-]+$"))
            posDb = "DFSPOS.POS";

        return @"
DECLARE @posOk BIT = 1;
CREATE TABLE #posl (TransactionID BIGINT, SaleLineNumber INT, FechaDoc DATE, PosLoc INT, Serie NVARCHAR(20),
                    TransType NVARCHAR(5), DutyType NVARCHAR(20), ItemCode NVARCHAR(50), Monto DECIMAL(19,4));
CREATE TABLE #post (TransactionID BIGINT, Lineas INT, Monto DECIMAL(19,4));
BEGIN TRY
    INSERT INTO #posl
    EXEC sp_executesql N'
        SELECT DISTINCT v.TransactionID, v.SaleLineNumber, CAST(v.BusinessDayDate AS DATE), TRY_CAST(v.PosLoc AS INT),
               CAST(v.CashDrawerID AS NVARCHAR(20)), CAST(v.TransType AS NVARCHAR(5)), CAST(v.DutyType AS NVARCHAR(20)),
               CAST(v.POSItemID AS NVARCHAR(50)), v.MontoExtendido
        FROM " + posDb + @".dbo.vw_POS_VentaItems v
        WHERE v.BusinessDayDate BETWEEN @d AND @h
          AND v.TransactionViewType = ''RetailTransactionStockView'' AND v.ItemAccion IN (''SELL'', ''RETURN'')
          AND ISNULL(v.POSItemID, '''') <> '''' AND ISNULL(v.PosLoc, '''') <> '''' AND ISNULL(v.TipoTransaccion, '''') <> ''''',
        N'@d DATE, @h DATE', @d = @desde, @h = @hasta;
    INSERT INTO #post
    EXEC sp_executesql N'
        SELECT t.TransactionID, COUNT(*), SUM(CASE WHEN ISNULL(t.IsChange, 0) = 1 THEN -t.Amount ELSE t.Amount END)
        FROM " + posDb + @".dbo.POS_TransactionTender t
        WHERE t.BusinessDayDate BETWEEN @d AND @h AND t.LineType IN (''TenderMerch'', ''TenderChange'')
        GROUP BY t.TransactionID',
        N'@d DATE, @h DATE', @d = @desde, @h = @hasta;
END TRY
BEGIN CATCH
    SET @posOk = 0;
END CATCH;

SELECT DISTINCT TransactionID, itemnum INTO #ssk FROM #ss WHERE TransactionID IS NOT NULL;
CREATE CLUSTERED INDEX IX_ssk ON #ssk (TransactionID, itemnum);

SELECT ts.CARCODE, ts.DUTYTYPE AS TransType, p.FechaDoc, p.TransactionID, p.Monto
INTO #posmiss
FROM #posl p
CROSS APPLY (SELECT TOP 1 t.CARCODE, t.DUTYTYPE, t.CompanyId FROM dbo.ADR_TIENDA_SERIE t
             WHERE t.NUMSERIE = p.Serie COLLATE DATABASE_DEFAULT AND t.NUMSTORE = p.PosLoc
             ORDER BY CASE WHEN t.DUTYTYPE = p.TransType COLLATE DATABASE_DEFAULT THEN 0 ELSE 1 END) ts
WHERE CAST(ts.CompanyId AS INT) = @bplId
  AND NOT EXISTS (SELECT 1 FROM #ssk k WHERE k.TransactionID = p.TransactionID AND k.itemnum = p.SaleLineNumber)
  -- same as SyncService.NormalizeDutyType: the POS sends 'DutyFree' / 'DutyPaid' (or 'DF' / 'DP')
  AND NOT (ts.DUTYTYPE = 'DP' AND p.DutyType IN ('DutyFree', 'DF')
           AND ISNULL(p.ItemCode, '') <> ISNULL(@deeeItem, ''));

-- Payments: POS tender lines vs la_store_payments rows that cross with their sale
SELECT x.CARCODE, x.TransType, x.FechaDoc, x.TransactionID, o.Monto - ISNULL(c.Monto, 0) AS Monto
INTO #paymiss
FROM (SELECT DISTINCT ss.TransactionID, ts.CARCODE, ss.TRANSTYPE AS TransType, CAST(ss.itemdatetime AS DATE) AS FechaDoc
      FROM #ss ss
      INNER JOIN dbo.ADR_TIENDA_SERIE ts
          ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE
      WHERE ss.TransactionID IS NOT NULL AND CAST(ts.CompanyId AS INT) = @bplId
        AND CAST(ss.itemdatetime AS DATE) BETWEEN @desde AND @hasta) x
INNER JOIN #post o ON o.TransactionID = x.TransactionID
OUTER APPLY (SELECT COUNT(*) AS Lineas, SUM(CASE WHEN sp.IsChange = 1 THEN -sp.Amount ELSE sp.Amount END) AS Monto
             FROM #sp sp
             WHERE sp.TransactionID = x.TransactionID AND sp.LineType IN ('TenderMerch', 'TenderChange')
               AND EXISTS (SELECT 1 FROM #ss s2 WHERE s2.TransactionID = sp.TransactionID AND s2.transnum = sp.transnum)) c
WHERE o.Lineas > ISNULL(c.Lineas, 0);
";
    }

    // Same POS check for one group, used by InvoiceUploader right before creating the OINV/ORIN.
    // Returns null when the group matches the POS, otherwise the reason to block the upload.
    public static string GetPosGapMessage(string cardCode, string transType, DateTime fecha, int bplId)
    {
        string sql = @"
SET NOCOUNT ON;
SELECT ID, NUMSERIE, storenum, TRANSTYPE, itemdatetime, transnum, TransactionID, itemnum
INTO #ss
FROM dbo.la_store_sales WITH (NOLOCK)
WHERE itemdatetime >= DATEADD(DAY, -1, @desde) AND itemdatetime < DATEADD(DAY, 2, @hasta);
SELECT ID, transnum, TransactionID, Amount, IsChange, LineType
INTO #sp
FROM dbo.la_store_payments WITH (NOLOCK)
WHERE BusinessDayDate BETWEEN @desde AND @hasta;
" + PosGapsSql() + @"
SELECT CAST(@posOk AS INT) AS PosOk,
       (SELECT COUNT(DISTINCT TransactionID) FROM #posmiss WHERE CARCODE = @cardCode AND TransType = @transType AND FechaDoc = @desde) AS SaleTxns,
       (SELECT SUM(Monto) FROM #posmiss WHERE CARCODE = @cardCode AND TransType = @transType AND FechaDoc = @desde) AS SaleMonto,
       (SELECT COUNT(DISTINCT TransactionID) FROM #paymiss WHERE CARCODE = @cardCode AND TransType = @transType AND FechaDoc = @desde) AS PayTxns,
       (SELECT SUM(Monto) FROM #paymiss WHERE CARCODE = @cardCode AND TransType = @transType AND FechaDoc = @desde) AS PayMonto;";

        DataTable dt = new DataTable();
        var db = new SqlDb();
        db.Connect();
        try
        {
            using (var cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@desde", fecha.Date);
                cmd.Parameters.AddWithValue("@hasta", fecha.Date);
                cmd.Parameters.AddWithValue("@bplId", bplId);
                cmd.Parameters.AddWithValue("@cardCode", cardCode);
                cmd.Parameters.AddWithValue("@transType", transType ?? "");
                cmd.Parameters.AddWithValue("@deeeItem", System.Configuration.ConfigurationManager.AppSettings["DeeeItemCode"] ?? "");
                cmd.CommandTimeout = 300;
                db.adapter.SelectCommand = cmd;
                db.adapter.Fill(dt);
            }
        }
        finally { db.Disconnect(); }

        DataRow r = dt.Rows[0];
        if (Convert.ToInt32(r["PosOk"]) == 0)
            return "POS check unavailable (the POS database could not be read).";
        if (Convert.ToInt32(r["SaleTxns"]) > 0)
            return r["SaleTxns"] + " POS transaction(s) (" + Convert.ToDecimal(r["SaleMonto"]).ToString("N2")
                + ") not loaded in la_store_sales yet (ExtraeCreaDocSalesSAP).";
        if (Convert.ToInt32(r["PayTxns"]) > 0)
            return "POS payments of " + r["PayTxns"] + " transaction(s) (" + Convert.ToDecimal(r["PayMonto"]).ToString("N2")
                + ") not loaded in la_store_payments yet (CreaFacturasVentasSAP --payments-only).";
        return null;
    }

    // Deliveries holding POS sales of several days. Until 2026-11-01 ExtraeCreaDocSalesSAP grouped
    // pending sales by transnum (repeats across days), so one ODLN/ORDN can carry sales of later
    // days, dated by the oldest one. Each day must be invoiced with only its own lines - those are
    // the ones its payments cover; the rest stay open for their own day. la_store_sales has no
    // line reference, so the document lines are paired in order with the POS sale rows of the
    // document's customer, and every pair must match item, quantity and amount.
    // Expects #mixcand (DeliveryDocEntry, txnmodifier). Creates:
    //   #mixlines (DocEntry, txnmodifier, LineNum, FechaDoc, Matched) - sale day of each line
    //   #mixdocs  (DocEntry, txnmodifier, Splittable) - 0 when the lines don't match the sales, or
    //             the document has additional expenses that don't match the transactions' tax
    //   #mixexp   (DocEntry, txnmodifier, FechaDoc, Monto) - each day's share of the document's
    //             additional expenses (tax): SalesTaxAmount of that day's transactions. Splittable
    //             only with one expense code whose total equals the sum of the shares (2026-11-01:
    //             in the 5 DP cases the whole tax belonged to the original day, later days had 0).
    // {0} = SAP company DB (string.Format placeholder; no other braces in the text).
    internal const string MixedDeliveriesSql = @"
SELECT s.ID, s.DeliveryDocEntry, s.txnmodifier, s.TransactionID, s.itemnum, s.itemdatetime,
       s.skunum, s.qty, s.extsellprice, s.NUMSERIE, s.storenum, s.TRANSTYPE, s.SalesTaxAmount
INTO #mixraw
FROM dbo.la_store_sales s WITH (NOLOCK)
WHERE s.DeliveryDocNum > 0
  AND s.DeliveryDocEntry IN (SELECT DeliveryDocEntry FROM #mixcand);

SELECT s.DeliveryDocEntry AS DocEntry, s.txnmodifier, s.ID, s.skunum, s.qty, s.extsellprice,
       s.TransactionID, s.SalesTaxAmount, CAST(s.itemdatetime AS DATE) AS FechaDoc,
       ROW_NUMBER() OVER (PARTITION BY s.DeliveryDocEntry, s.txnmodifier
                          ORDER BY s.itemdatetime, s.TransactionID, s.itemnum, s.ID) - 1 AS rn
INTO #mixsales
FROM #mixraw s
INNER JOIN (SELECT DISTINCT DeliveryDocEntry, txnmodifier FROM #mixcand) c
    ON c.DeliveryDocEntry = s.DeliveryDocEntry AND c.txnmodifier = s.txnmodifier
INNER JOIN dbo.ADR_TIENDA_SERIE ts
    ON ts.NUMSERIE = s.NUMSERIE AND ts.NUMSTORE = CAST(s.storenum AS INT) AND ts.DUTYTYPE = s.TRANSTYPE
LEFT JOIN {0}..ODLN o ON s.txnmodifier = 0 AND o.DocEntry = s.DeliveryDocEntry
LEFT JOIN {0}..ORDN r ON s.txnmodifier = 1 AND r.DocEntry = s.DeliveryDocEntry
-- Same customer as the document, and not an old mixed transaction (2026-08/09) whose sale and
-- return lines all went into one ORDN: its DocEntry number can also be an ODLN of the same store.
WHERE ts.CARCODE COLLATE DATABASE_DEFAULT = COALESCE(o.CardCode, r.CardCode) COLLATE DATABASE_DEFAULT
  AND NOT EXISTS (SELECT 1 FROM #mixraw x
                  WHERE x.TransactionID = s.TransactionID AND x.txnmodifier <> s.txnmodifier
                    AND x.DeliveryDocEntry = s.DeliveryDocEntry);

DELETE m FROM #mixsales m
WHERE NOT EXISTS (SELECT 1 FROM #mixsales x
                  WHERE x.DocEntry = m.DocEntry AND x.txnmodifier = m.txnmodifier AND x.FechaDoc <> m.FechaDoc);

SELECT d.DocEntry, d.txnmodifier, l.LineNum, l.ItemCode, l.Quantity, l.LineTotal,
       ROW_NUMBER() OVER (PARTITION BY d.DocEntry, d.txnmodifier ORDER BY l.VisOrder, l.LineNum) - 1 AS rn
INTO #mixdoclines
FROM (SELECT DISTINCT DocEntry, txnmodifier FROM #mixsales) d
CROSS APPLY (SELECT LineNum, ItemCode, Quantity, LineTotal, VisOrder FROM {0}..DLN1
             WHERE d.txnmodifier = 0 AND DocEntry = d.DocEntry
             UNION ALL
             SELECT LineNum, ItemCode, Quantity, LineTotal, VisOrder FROM {0}..RDN1
             WHERE d.txnmodifier = 1 AND DocEntry = d.DocEntry) l;

SELECT COALESCE(s.DocEntry, l.DocEntry) AS DocEntry, COALESCE(s.txnmodifier, l.txnmodifier) AS txnmodifier,
       l.LineNum, s.FechaDoc,
       CASE WHEN s.ID IS NULL OR l.LineNum IS NULL
              OR s.skunum COLLATE DATABASE_DEFAULT <> l.ItemCode COLLATE DATABASE_DEFAULT
              OR ABS(ABS(s.qty) - l.Quantity) > 0.001
              OR ABS(ABS(s.extsellprice) - l.LineTotal) > 0.05 THEN 0 ELSE 1 END AS Matched
INTO #mixlines
FROM #mixsales s
FULL JOIN #mixdoclines l ON l.DocEntry = s.DocEntry AND l.txnmodifier = s.txnmodifier AND l.rn = s.rn;

-- Tax of each day (SalesTaxAmount is per transaction, repeated on its rows)
SELECT t.DocEntry, t.txnmodifier, t.FechaDoc, SUM(t.Tax) AS Monto
INTO #mixexp
FROM (SELECT DocEntry, txnmodifier, FechaDoc, TransactionID, MAX(ABS(ISNULL(SalesTaxAmount, 0))) AS Tax
      FROM #mixsales GROUP BY DocEntry, txnmodifier, FechaDoc, TransactionID) t
GROUP BY t.DocEntry, t.txnmodifier, t.FechaDoc;

SELECT m.DocEntry, m.txnmodifier,
       CASE WHEN MIN(m.Matched) = 1
                 AND (ISNULL(MAX(x.Codigos), 0) = 0
                      OR (MAX(x.Codigos) = 1
                          AND ABS(MAX(x.Total) - ISNULL((SELECT SUM(me.Monto) FROM #mixexp me
                                                         WHERE me.DocEntry = m.DocEntry AND me.txnmodifier = m.txnmodifier), 0)) <= 0.02))
            THEN 1 ELSE 0 END AS Splittable
INTO #mixdocs
FROM #mixlines m
OUTER APPLY (SELECT COUNT(DISTINCT e.ExpnsCode) AS Codigos, SUM(e.LineTotal) AS Total
             FROM (SELECT ExpnsCode, LineTotal FROM {0}..DLN3 WHERE m.txnmodifier = 0 AND DocEntry = m.DocEntry AND LineTotal <> 0
                   UNION ALL
                   SELECT ExpnsCode, LineTotal FROM {0}..RDN3 WHERE m.txnmodifier = 1 AND DocEntry = m.DocEntry AND LineTotal <> 0) e) x
GROUP BY m.DocEntry, m.txnmodifier;
";

    // Returns all (CardCode, TransType, FechaDoc) groups that still need an OINV/ORIN —
    // i.e., have at least one delivered ODLN/ORDN and no invoice yet. Also surfaces groups
    // where ALL items are stuck in la_delivery_errors (no ODLN at all) so the user can see
    // what is holding them. Filters to the user's BPL (branch) via bplId.
    // includeInvoiced = true also returns groups already invoiced (Invoiced = 1, InvoicedDocNum),
    // detected from la_daily_invoices / la_daily_returns or directly in SAP: an OINV/ORIN whose
    // lines are based on the group's ODLN/ORDN (covers invoices created by hand in SAP B1).
    public DataTable GetPreview(string sapDb, int bplId, DateTime desde, DateTime hasta, bool includeInvoiced = false)
    {
        LastError = null;
        var dt = new DataTable();
        var db = new SqlDb();
        db.Connect();
        try
        {
            // la_store_sales has no index on transnum and is joined by it several times below:
            // copy only the rows of the range (+-1 day for tickets around midnight) into indexed
            // temp tables first, then run everything against them.
            string sql = string.Format(@"
SET NOCOUNT ON;
SELECT ID, NUMSERIE, storenum, TRANSTYPE, itemdatetime, txnmodifier, DeliveryDocNum, DeliveryDocEntry,
       transnum, TransactionID, itemnum, Extsellprice, SalesTaxAmount, MontoSinImpuesto
INTO #ss
FROM dbo.la_store_sales WITH (NOLOCK)
WHERE itemdatetime >= DATEADD(DAY, -1, @desde) AND itemdatetime < DATEADD(DAY, 2, @hasta);
CREATE CLUSTERED INDEX IX_ss_transnum ON #ss (transnum);

SELECT ID, transnum, TransactionID, TenderID, Amount, IsChange, LineType, BusinessDayDate
INTO #sp
FROM dbo.la_store_payments WITH (NOLOCK)
WHERE BusinessDayDate BETWEEN @desde AND @hasta;
CREATE CLUSTERED INDEX IX_sp_transnum ON #sp (transnum);
" + PosGapsSql() + @"
SELECT DISTINCT DeliveryDocEntry, txnmodifier
INTO #mixcand
FROM #ss
WHERE DeliveryDocNum > 0 AND DeliveryDocEntry IS NOT NULL AND DeliveryDocEntry <> 99999999;
" + MixedDeliveriesSql + @"
;WITH SalesBase AS (
    SELECT ts.CARCODE, CAST(ts.CompanyId AS INT) AS BPLId, ss.TRANSTYPE AS TransType,
           CAST(ss.itemdatetime AS DATE) AS FechaDoc,
           CASE WHEN ss.DeliveryDocNum = -3 THEN 0 ELSE ss.txnmodifier END AS txnmodifier,
           ss.transnum, ss.DeliveryDocNum, ss.DeliveryDocEntry,
           CASE WHEN ss.DeliveryDocNum = -3 THEN -ABS(ss.Extsellprice) ELSE ABS(ss.Extsellprice) END AS ExtSell,
           ABS(ss.SalesTaxAmount) AS Tax
    FROM #ss ss
    INNER JOIN dbo.ADR_TIENDA_SERIE ts
        ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE
    WHERE CAST(ss.itemdatetime AS DATE) BETWEEN @desde AND @hasta
      AND CAST(ts.CompanyId AS INT) = @bplId
      AND ISNULL(ss.DeliveryDocNum, -1) <> 0
),
TxnTotals AS (
    SELECT CARCODE, TransType, FechaDoc, txnmodifier, transnum,
           SUM(ExtSell) + ABS(MAX(Tax)) AS TxnTotal
    FROM SalesBase GROUP BY CARCODE, TransType, FechaDoc, txnmodifier, transnum
),
PosTotals AS (
    SELECT CARCODE, TransType, FechaDoc, txnmodifier, SUM(TxnTotal) AS PosTotal
    FROM TxnTotals GROUP BY CARCODE, TransType, FechaDoc, txnmodifier
),
DeliveredGroups AS (
    SELECT DISTINCT CARCODE, BPLId, TransType, FechaDoc, txnmodifier
    FROM SalesBase WHERE DeliveryDocNum > 0
),
PendingDeliveries AS (
    SELECT DISTINCT CARCODE, TransType, FechaDoc
    FROM SalesBase WHERE DeliveryDocNum = -1
),
-- Items / Total (SAP): same lines InvoiceUploader.ProcessSide copies into the OINV/ORIN —
-- open ODLN/ORDN lines + additional expenses, minus mixed-transaction returns (-3).
DeliveryDocs AS (
    SELECT DISTINCT CARCODE, TransType, FechaDoc, txnmodifier, DeliveryDocEntry
    FROM SalesBase
    WHERE DeliveryDocNum > 0 AND DeliveryDocEntry IS NOT NULL AND DeliveryDocEntry <> 99999999
),
-- Deliveries mixing several days (#mixdocs) only contribute the lines of the group's own day.
-- la_invoice_excluded_lines: lines never to be invoiced (e.g. sale lines that an old integration
-- version put inside an ORDN; see Scripts/Reset_Old_Mixed_ORDN.sql).
SapLines AS (
    SELECT d.CARCODE, d.TransType, d.FechaDoc, d.txnmodifier, l.LineTotal AS Amt, 1 AS IsDelivLine
    FROM DeliveryDocs d INNER JOIN {0}..DLN1 l ON l.DocEntry = d.DeliveryDocEntry AND l.LineStatus = 'O'
    WHERE d.txnmodifier = 0
      AND NOT EXISTS (SELECT 1 FROM dbo.la_invoice_excluded_lines xl WHERE xl.BaseType = 15 AND xl.DocEntry = l.DocEntry AND xl.LineNum = l.LineNum)
      AND (NOT EXISTS (SELECT 1 FROM #mixdocs md WHERE md.DocEntry = l.DocEntry AND md.txnmodifier = 0)
           OR EXISTS (SELECT 1 FROM #mixlines ml WHERE ml.DocEntry = l.DocEntry AND ml.txnmodifier = 0
                                                   AND ml.LineNum = l.LineNum AND ml.FechaDoc = d.FechaDoc))
    UNION ALL
    SELECT d.CARCODE, d.TransType, d.FechaDoc, d.txnmodifier, l.LineTotal, 1
    FROM DeliveryDocs d INNER JOIN {0}..RDN1 l ON l.DocEntry = d.DeliveryDocEntry AND l.LineStatus = 'O'
    WHERE d.txnmodifier = 1
      AND NOT EXISTS (SELECT 1 FROM dbo.la_invoice_excluded_lines xl WHERE xl.BaseType = 16 AND xl.DocEntry = l.DocEntry AND xl.LineNum = l.LineNum)
      AND (NOT EXISTS (SELECT 1 FROM #mixdocs md WHERE md.DocEntry = l.DocEntry AND md.txnmodifier = 1)
           OR EXISTS (SELECT 1 FROM #mixlines ml WHERE ml.DocEntry = l.DocEntry AND ml.txnmodifier = 1
                                                   AND ml.LineNum = l.LineNum AND ml.FechaDoc = d.FechaDoc))
    UNION ALL
    SELECT d.CARCODE, d.TransType, d.FechaDoc, d.txnmodifier, e.LineTotal, 0
    FROM DeliveryDocs d INNER JOIN {0}..DLN3 e ON e.DocEntry = d.DeliveryDocEntry
    WHERE d.txnmodifier = 0
      -- LineNum -1 in la_invoice_excluded_lines = the document's additional expenses (e.g. the tax
      -- an old integration version repeated in both the ODLN and the ORDN of a mixed transaction)
      AND NOT EXISTS (SELECT 1 FROM dbo.la_invoice_excluded_lines xl WHERE xl.BaseType = 15 AND xl.DocEntry = e.DocEntry AND xl.LineNum = -1)
      AND NOT EXISTS (SELECT 1 FROM #mixdocs md WHERE md.DocEntry = e.DocEntry AND md.txnmodifier = 0)
    UNION ALL
    SELECT d.CARCODE, d.TransType, d.FechaDoc, d.txnmodifier, e.LineTotal, 0
    FROM DeliveryDocs d INNER JOIN {0}..RDN3 e ON e.DocEntry = d.DeliveryDocEntry
    WHERE d.txnmodifier = 1
      AND NOT EXISTS (SELECT 1 FROM dbo.la_invoice_excluded_lines xl WHERE xl.BaseType = 16 AND xl.DocEntry = e.DocEntry AND xl.LineNum = -1)
      AND NOT EXISTS (SELECT 1 FROM #mixdocs md WHERE md.DocEntry = e.DocEntry AND md.txnmodifier = 1)
    UNION ALL
    -- Deliveries mixing several days: only the group's day share of the additional expenses (tax)
    SELECT d.CARCODE, d.TransType, d.FechaDoc, d.txnmodifier, me.Monto, 0
    FROM DeliveryDocs d
    INNER JOIN #mixexp me ON me.DocEntry = d.DeliveryDocEntry AND me.txnmodifier = d.txnmodifier AND me.FechaDoc = d.FechaDoc
    WHERE EXISTS (SELECT 1 FROM {0}..DLN3 e WHERE d.txnmodifier = 0 AND e.DocEntry = d.DeliveryDocEntry AND e.LineTotal <> 0
                  UNION ALL
                  SELECT 1 FROM {0}..RDN3 e WHERE d.txnmodifier = 1 AND e.DocEntry = d.DeliveryDocEntry AND e.LineTotal <> 0)
      AND NOT EXISTS (SELECT 1 FROM dbo.la_invoice_excluded_lines xl
                      WHERE xl.BaseType = CASE WHEN d.txnmodifier = 1 THEN 16 ELSE 15 END AND xl.DocEntry = d.DeliveryDocEntry AND xl.LineNum = -1)
    UNION ALL
    SELECT CARCODE, TransType, FechaDoc, 0, ExtSell, 0
    FROM SalesBase WHERE DeliveryDocNum = -3
),
SapTotals AS (
    SELECT CARCODE, TransType, FechaDoc, txnmodifier,
           SUM(IsDelivLine) AS DelivLines, COUNT(*) AS ItemCount, SUM(Amt) AS InvoiceTotal
    FROM SapLines
    WHERE IsDelivLine = 1 OR Amt <> 0
    GROUP BY CARCODE, TransType, FechaDoc, txnmodifier
),
SapInvoices AS (
    SELECT d.CARCODE, d.TransType, d.FechaDoc, d.txnmodifier, MAX(o.DocNum) AS DocNum
    FROM DeliveryDocs d
    INNER JOIN {0}..INV1 l ON l.BaseType = 15 AND l.BaseEntry = d.DeliveryDocEntry
    INNER JOIN {0}..OINV o ON o.DocEntry = l.DocEntry AND o.CANCELED = 'N'
    WHERE d.txnmodifier = 0
      AND (NOT EXISTS (SELECT 1 FROM #mixdocs md WHERE md.DocEntry = d.DeliveryDocEntry AND md.txnmodifier = 0)
           OR EXISTS (SELECT 1 FROM #mixlines ml WHERE ml.DocEntry = l.BaseEntry AND ml.txnmodifier = 0
                                                   AND ml.LineNum = l.BaseLine AND ml.FechaDoc = d.FechaDoc))
    GROUP BY d.CARCODE, d.TransType, d.FechaDoc, d.txnmodifier
    UNION ALL
    SELECT d.CARCODE, d.TransType, d.FechaDoc, d.txnmodifier, MAX(o.DocNum)
    FROM DeliveryDocs d
    INNER JOIN {0}..RIN1 l ON l.BaseType = 16 AND l.BaseEntry = d.DeliveryDocEntry
    INNER JOIN {0}..ORIN o ON o.DocEntry = l.DocEntry AND o.CANCELED = 'N'
    WHERE d.txnmodifier = 1
      AND (NOT EXISTS (SELECT 1 FROM #mixdocs md WHERE md.DocEntry = d.DeliveryDocEntry AND md.txnmodifier = 1)
           OR EXISTS (SELECT 1 FROM #mixlines ml WHERE ml.DocEntry = l.BaseEntry AND ml.txnmodifier = 1
                                                   AND ml.LineNum = l.BaseLine AND ml.FechaDoc = d.FechaDoc))
    GROUP BY d.CARCODE, d.TransType, d.FechaDoc, d.txnmodifier
),
-- Sales / payments the POS has but SCI doesn't (see PosGapsSql).
PosGaps AS (
    SELECT CARCODE, TransType, FechaDoc, COUNT(DISTINCT TransactionID) AS Txns, SUM(Monto) AS Monto
    FROM #posmiss GROUP BY CARCODE, TransType, FechaDoc
),
PayGaps AS (
    SELECT CARCODE, TransType, FechaDoc, COUNT(DISTINCT TransactionID) AS Txns, SUM(Monto) AS Monto
    FROM #paymiss GROUP BY CARCODE, TransType, FechaDoc
),
-- A delivery mixing several days whose lines can't be told apart (or with expenses) blocks the group.
MixBlocked AS (
    SELECT d.CARCODE, d.TransType, d.FechaDoc, d.txnmodifier, MIN(d.DeliveryDocEntry) AS DocEntry
    FROM DeliveryDocs d
    INNER JOIN #mixdocs md ON md.DocEntry = d.DeliveryDocEntry AND md.txnmodifier = d.txnmodifier
    WHERE md.Splittable = 0
    GROUP BY d.CARCODE, d.TransType, d.FechaDoc, d.txnmodifier
),
-- Payments validation - same queries InvoiceUploader runs before creating anything
-- (GetPagosParaFactura / GetPagosParaDevolucion / GetCompensacionMixtas / tender mapping).
-- Keyed by TransactionID: transnum repeats across days, TransactionID is unique per ticket.
PayTxn AS (
    SELECT TransactionID, transnum, TRANSTYPE,
           SUM(CASE WHEN txnmodifier = 0 THEN ABS(extsellprice) + ABS(extsellprice) - ABS(ISNULL(MontoSinImpuesto, extsellprice)) ELSE 0 END) AS VentaTotal,
           SUM(CASE WHEN txnmodifier = 1 THEN ABS(extsellprice) + ABS(extsellprice) - ABS(ISNULL(MontoSinImpuesto, extsellprice)) ELSE 0 END) AS DevTotal,
           MAX(CASE WHEN DeliveryDocNum = -3 THEN 1 ELSE 0 END) AS EnFactura
    FROM #ss
    WHERE ISNULL(DeliveryDocNum, -1) <> 0
      -- la_store_sales has no index on transnum: bound it by sale date (indexed), +-1 day for
      -- tickets around midnight whose BusinessDayDate differs from itemdatetime.
      AND itemdatetime >= DATEADD(DAY, -1, @desde) AND itemdatetime < DATEADD(DAY, 2, @hasta)
    GROUP BY TransactionID, transnum, TRANSTYPE
),
PayRows AS (
    SELECT DISTINCT ts.CARCODE, ss.TRANSTYPE AS TransType, sp.BusinessDayDate AS FechaDoc, 0 AS txnmodifier, sp.ID, sp.TenderID,
           CASE WHEN sp.IsChange = 1 THEN -sp.Amount ELSE sp.Amount END AS Amount
    FROM #sp sp
    INNER JOIN #ss ss ON ss.TransactionID = sp.TransactionID AND ss.transnum = sp.transnum
                                    AND ss.itemdatetime >= DATEADD(DAY, -1, @desde) AND ss.itemdatetime < DATEADD(DAY, 2, @hasta)
    INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE
    INNER JOIN PayTxn tt ON tt.TransactionID = sp.TransactionID AND tt.transnum = sp.transnum AND tt.TRANSTYPE = ss.TRANSTYPE
    WHERE sp.BusinessDayDate BETWEEN @desde AND @hasta AND CAST(ts.CompanyId AS INT) = @bplId
      AND sp.LineType IN ('TenderMerch', 'TenderChange') AND ss.txnmodifier = 0
      AND tt.VentaTotal > tt.DevTotal AND tt.EnFactura = 0
    UNION
    SELECT DISTINCT ts.CARCODE, ss.TRANSTYPE, sp.BusinessDayDate, 0, sp.ID, sp.TenderID,
           (CASE WHEN tt.DevTotal > tt.VentaTotal THEN -1 ELSE 1 END) * (CASE WHEN sp.IsChange = 1 THEN -1 ELSE 1 END) * ABS(sp.Amount)
    FROM #sp sp
    INNER JOIN #ss ss ON ss.TransactionID = sp.TransactionID AND ss.transnum = sp.transnum
                                    AND ss.itemdatetime >= DATEADD(DAY, -1, @desde) AND ss.itemdatetime < DATEADD(DAY, 2, @hasta)
    INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE
    INNER JOIN PayTxn tt ON tt.TransactionID = sp.TransactionID AND tt.transnum = sp.transnum AND tt.TRANSTYPE = ss.TRANSTYPE
    WHERE sp.BusinessDayDate BETWEEN @desde AND @hasta AND CAST(ts.CompanyId AS INT) = @bplId
      AND sp.LineType IN ('TenderMerch', 'TenderChange') AND tt.EnFactura = 1
    UNION
    SELECT DISTINCT ts.CARCODE, ss.TRANSTYPE, sp.BusinessDayDate, 1, sp.ID, sp.TenderID,
           CASE WHEN sp.IsChange = 1 THEN -ABS(sp.Amount) ELSE ABS(sp.Amount) END
    FROM #sp sp
    INNER JOIN #ss ss ON ss.TransactionID = sp.TransactionID AND ss.transnum = sp.transnum
                                    AND ss.itemdatetime >= DATEADD(DAY, -1, @desde) AND ss.itemdatetime < DATEADD(DAY, 2, @hasta)
    INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE
    INNER JOIN PayTxn tt ON tt.TransactionID = sp.TransactionID AND tt.transnum = sp.transnum AND tt.TRANSTYPE = ss.TRANSTYPE
    WHERE sp.BusinessDayDate BETWEEN @desde AND @hasta AND CAST(ts.CompanyId AS INT) = @bplId
      AND sp.LineType IN ('TenderMerch', 'TenderChange') AND ss.txnmodifier = 1
      AND tt.DevTotal >= tt.VentaTotal AND tt.EnFactura = 0
),
PayRowsMap AS (
    SELECT p.*, CASE WHEN EXISTS (SELECT 1 FROM dbo.ADR_TENDER_SAP m
                                  WHERE m.TenderID = p.TenderID AND (m.BPLId = @bplId OR m.BPLId IS NULL))
                     THEN NULL ELSE p.TenderID END AS Unmapped
    FROM PayRows p
),
PayTotals AS (
    SELECT CARCODE, TransType, FechaDoc, txnmodifier, SUM(Amount) AS Pagos, MAX(Unmapped) AS TenderSinMapeo
    FROM PayRowsMap
    GROUP BY CARCODE, TransType, FechaDoc, txnmodifier
),
MixedOffset AS (
    SELECT DISTINCT ts.CARCODE, tt.TRANSTYPE AS TransType, CAST(ss.itemdatetime AS DATE) AS FechaDoc, tt.TransactionID, tt.transnum,
           CASE WHEN tt.VentaTotal < tt.DevTotal THEN tt.VentaTotal ELSE tt.DevTotal END AS Offset
    FROM PayTxn tt
    INNER JOIN #ss ss ON ss.TransactionID = tt.TransactionID AND ss.transnum = tt.transnum
                                    AND ss.itemdatetime >= @desde AND ss.itemdatetime < DATEADD(DAY, 1, @hasta)
    INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE
    WHERE tt.VentaTotal > 0 AND tt.DevTotal > 0 AND tt.EnFactura = 0
      AND CAST(ss.itemdatetime AS DATE) BETWEEN @desde AND @hasta
),
MixedTotals AS (
    SELECT CARCODE, TransType, FechaDoc, SUM(Offset) AS Compensacion
    FROM MixedOffset GROUP BY CARCODE, TransType, FechaDoc
),
ErrorItems AS (
    SELECT DISTINCT ts.CARCODE, ss.TRANSTYPE AS TransType, CAST(ss.itemdatetime AS DATE) AS FechaDoc
    FROM dbo.la_delivery_errors de
    INNER JOIN #ss ss ON (
        (de.TransactionID IS NOT NULL AND ss.TransactionID = de.TransactionID AND ss.itemnum = de.itemnum)
        -- transnum repeats across days: without TransactionID the sale day must match too.
        OR (de.TransactionID IS NULL AND ss.transnum = de.transnum AND ss.itemnum = de.itemnum
            AND CAST(ss.itemdatetime AS DATE) = CAST(de.itemdatetime AS DATE))
    )
    INNER JOIN dbo.ADR_TIENDA_SERIE ts
        ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE
    WHERE CAST(ss.itemdatetime AS DATE) BETWEEN @desde AND @hasta
      AND CAST(ts.CompanyId AS INT) = @bplId
)
SELECT dg.CARCODE AS CardCode, dg.TransType, dg.FechaDoc,
       CAST(dg.txnmodifier AS BIT) AS EsDevolucion, dg.BPLId,
       ISNULL(c.CardName,'') AS CardName,
       CAST(CASE WHEN x.InvoicedNum IS NOT NULL OR pd.CARCODE IS NOT NULL OR ei.CARCODE IS NOT NULL OR mb.CARCODE IS NOT NULL
                   OR @posOk = 0 OR pg.CARCODE IS NOT NULL OR pyg.CARCODE IS NOT NULL
                   OR ISNULL(st.DelivLines, 0) = 0 OR v.PayMismatch = 1 OR pay.TenderSinMapeo IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS Blocked,
       CASE
         WHEN x.InvoicedNum IS NOT NULL THEN 'Invoiced in SAP - ' + CASE WHEN dg.txnmodifier = 1 THEN 'ORIN' ELSE 'OINV' END
              + ' #' + CAST(x.InvoicedNum AS VARCHAR(20))
         WHEN @posOk = 0 THEN 'POS check unavailable (the POS database could not be read). Upload blocked until the POS can be checked.'
         WHEN pg.CARCODE IS NOT NULL THEN CAST(pg.Txns AS VARCHAR(10)) + ' POS transaction(s) ('
              + CONVERT(VARCHAR(30), CAST(pg.Monto AS MONEY), 1) + ') not loaded in la_store_sales yet (ExtraeCreaDocSalesSAP).'
         WHEN pyg.CARCODE IS NOT NULL THEN 'POS payments of ' + CAST(pyg.Txns AS VARCHAR(10)) + ' transaction(s) ('
              + CONVERT(VARCHAR(30), CAST(pyg.Monto AS MONEY), 1) + ') not loaded in la_store_payments yet (CreaFacturasVentasSAP --payments-only).'
         WHEN pd.CARCODE IS NOT NULL AND ei.CARCODE IS NOT NULL THEN 'Pending deliveries and delivery errors'
         WHEN pd.CARCODE IS NOT NULL THEN 'Pending deliveries (ODLN/ORDN not complete yet)'
         WHEN ei.CARCODE IS NOT NULL THEN 'Pending delivery errors in la_delivery_errors'
         WHEN mb.CARCODE IS NOT NULL THEN CASE WHEN dg.txnmodifier = 1 THEN 'ORDN' ELSE 'ODLN' END
              + ' #' + CAST(mb.DocEntry AS VARCHAR(20)) + ' mixes POS sales of several days and its lines can''t be split by day'
              + ' (lines don''t match the sales, or it has additional expenses). Manual resolution required in SAP B1.'
         WHEN ISNULL(st.DelivLines, 0) = 0 THEN 'All ' + CASE WHEN dg.txnmodifier = 1 THEN 'ORDN' ELSE 'ODLN' END
              + ' lines are already invoiced (bost_Close). Manual resolution required in SAP B1.'
         WHEN v.PayMismatch = 1 THEN
              CASE WHEN dg.txnmodifier = 1 THEN 'Credit memo total (' + CONVERT(VARCHAR(30), CAST(st.InvoiceTotal AS MONEY), 1)
                        + ') does not match total refunded in payments ('
                   ELSE 'Invoice total (' + CONVERT(VARCHAR(30), CAST(st.InvoiceTotal AS MONEY), 1)
                        + ') does not match total collected in payments (' END
              + CONVERT(VARCHAR(30), CAST(ISNULL(pay.Pagos, 0) AS MONEY), 1) + ')'
              + CASE WHEN ISNULL(mx.Compensacion, 0) <> 0
                     THEN ' + mixed-transaction offset ' + CONVERT(VARCHAR(30), CAST(mx.Compensacion AS MONEY), 1) ELSE '' END + '.'
         WHEN pay.TenderSinMapeo IS NOT NULL THEN 'Payment ' + pay.TenderSinMapeo + ' not in ADR_TENDER_SAP for this branch.'
         ELSE NULL
       END AS BlockReason,
       pt.PosTotal,
       ISNULL(st.ItemCount, 0) AS ItemCount,
       CAST(CASE WHEN x.InvoicedNum IS NOT NULL AND sd.DocTotal IS NOT NULL THEN sd.DocTotal
                 ELSE st.InvoiceTotal END AS DECIMAL(18,2)) AS InvoiceTotal,
       CAST(CASE WHEN x.InvoicedNum IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS Invoiced,
       x.InvoicedNum AS InvoicedDocNum,
       CAST(v.RoundingAdj AS DECIMAL(18,2)) AS RoundingAdj,
       CAST(pp.PaymentPending AS BIT) AS PaymentPending
FROM DeliveredGroups dg
LEFT JOIN {0}..OCRD c ON c.CardCode COLLATE DATABASE_DEFAULT = dg.CARCODE COLLATE DATABASE_DEFAULT
LEFT JOIN PendingDeliveries pd ON pd.CARCODE = dg.CARCODE AND pd.TransType = dg.TransType AND pd.FechaDoc = dg.FechaDoc
LEFT JOIN ErrorItems ei ON ei.CARCODE = dg.CARCODE AND ei.TransType = dg.TransType AND ei.FechaDoc = dg.FechaDoc
LEFT JOIN PosTotals pt ON pt.CARCODE = dg.CARCODE AND pt.TransType = dg.TransType AND pt.FechaDoc = dg.FechaDoc AND pt.txnmodifier = dg.txnmodifier
LEFT JOIN SapTotals st ON st.CARCODE = dg.CARCODE AND st.TransType = dg.TransType AND st.FechaDoc = dg.FechaDoc AND st.txnmodifier = dg.txnmodifier
LEFT JOIN SapInvoices si ON si.CARCODE = dg.CARCODE AND si.TransType = dg.TransType AND si.FechaDoc = dg.FechaDoc AND si.txnmodifier = dg.txnmodifier
LEFT JOIN MixBlocked mb ON mb.CARCODE = dg.CARCODE AND mb.TransType = dg.TransType AND mb.FechaDoc = dg.FechaDoc AND mb.txnmodifier = dg.txnmodifier
LEFT JOIN PosGaps pg ON pg.CARCODE = dg.CARCODE AND pg.TransType = dg.TransType AND pg.FechaDoc = dg.FechaDoc
LEFT JOIN PayGaps pyg ON pyg.CARCODE = dg.CARCODE AND pyg.TransType = dg.TransType AND pyg.FechaDoc = dg.FechaDoc
OUTER APPLY (SELECT TOP 1 di.InvoiceDocNum AS n, di.InvoiceDocEntry AS e, di.PaymentDocNum AS p FROM dbo.la_daily_invoices di
             WHERE dg.txnmodifier = 0 AND di.CardCode = dg.CARCODE AND di.TransType = dg.TransType
               AND di.FechaDoc = dg.FechaDoc AND di.InvoiceDocNum > 0) dinv
OUTER APPLY (SELECT TOP 1 dr.ReturnDocNum AS n, dr.ReturnDocEntry AS e, dr.RefundDocNum AS p FROM dbo.la_daily_returns dr
             WHERE dg.txnmodifier = 1 AND dr.CardCode = dg.CARCODE AND dr.TransType = dg.TransType
               AND dr.FechaDoc = dg.FechaDoc AND dr.ReturnDocNum > 0) dret
-- Payment pending: OINV/ORIN created by the upload but its cobro/reembolso failed (no PaymentDocNum /
-- RefundDocNum) and the document is still open and unpaid in SAP. Shown even without @includeInvoiced.
CROSS APPLY (SELECT CASE WHEN (dinv.n IS NOT NULL AND dinv.p IS NULL AND EXISTS (SELECT 1 FROM {0}..OINV o
                                  WHERE o.DocEntry = dinv.e AND o.DocStatus = 'O' AND o.PaidToDate = 0))
                           OR (dret.n IS NOT NULL AND dret.p IS NULL AND EXISTS (SELECT 1 FROM {0}..ORIN o
                                  WHERE o.DocEntry = dret.e AND o.DocStatus = 'O' AND o.PaidToDate = 0))
                         THEN 1 ELSE 0 END AS PaymentPending) pp
-- Invoiced rows show the real OINV/ORIN total (DLN3 expenses have no open/closed status, so the
-- SapTotals of an invoiced group would still count them).
OUTER APPLY (SELECT o.DocTotal FROM {0}..OINV o WHERE o.DocEntry = dinv.e
             UNION ALL
             SELECT o.DocTotal FROM {0}..ORIN o WHERE o.DocEntry = dret.e) sd
-- Invoiced: logged by the upload, or every ODLN/ORDN line already closed and drawn into an OINV/ORIN.
CROSS APPLY (SELECT COALESCE(dinv.n, dret.n, CASE WHEN ISNULL(st.DelivLines, 0) = 0 THEN si.DocNum END) AS InvoicedNum) x
LEFT JOIN PayTotals pay ON pay.CARCODE = dg.CARCODE AND pay.TransType = dg.TransType AND pay.FechaDoc = dg.FechaDoc AND pay.txnmodifier = dg.txnmodifier
LEFT JOIN MixedTotals mx ON mx.CARCODE = dg.CARCODE AND mx.TransType = dg.TransType AND mx.FechaDoc = dg.FechaDoc
CROSS APPLY (SELECT ROUND(ISNULL(st.InvoiceTotal, 0) - ISNULL(pay.Pagos, 0) - ISNULL(mx.Compensacion, 0), 2) AS Dif) d
-- Cent differences up to @roundTol are absorbed by InvoiceUploader as a rounding adjustment on
-- the largest line (ODLNs created before 2026-08-27 stored prices with 2 decimals).
CROSS APPLY (SELECT CASE WHEN ABS(d.Dif) > @roundTol THEN 1 ELSE 0 END AS PayMismatch,
                    CASE WHEN ABS(d.Dif) <= @roundTol THEN -d.Dif ELSE 0 END AS RoundingAdj) v
WHERE (@includeInvoiced = 1 OR x.InvoicedNum IS NULL OR pp.PaymentPending = 1)

UNION ALL

SELECT ei2.CARCODE AS CardCode, ei2.TransType, ei2.FechaDoc,
       CAST(0 AS BIT) AS EsDevolucion, CAST(@bplId AS INT) AS BPLId,
       ISNULL(c2.CardName,'') AS CardName,
       CAST(1 AS BIT) AS Blocked,
       'Pending delivery errors (la_delivery_errors) - no ODLN created yet.' AS BlockReason,
       pt2.PosTotal, 0 AS ItemCount, CAST(NULL AS DECIMAL(18,2)) AS InvoiceTotal,
       CAST(0 AS BIT) AS Invoiced, CAST(NULL AS INT) AS InvoicedDocNum,
       CAST(0 AS DECIMAL(18,2)) AS RoundingAdj,
       CAST(0 AS BIT) AS PaymentPending
FROM ErrorItems ei2
LEFT JOIN {0}..OCRD c2 ON c2.CardCode COLLATE DATABASE_DEFAULT = ei2.CARCODE COLLATE DATABASE_DEFAULT
LEFT JOIN PosTotals pt2 ON pt2.CARCODE = ei2.CARCODE AND pt2.TransType = ei2.TransType AND pt2.FechaDoc = ei2.FechaDoc AND pt2.txnmodifier = 0
WHERE NOT EXISTS (
    SELECT 1 FROM DeliveredGroups dg2
    WHERE dg2.CARCODE = ei2.CARCODE AND dg2.TransType = ei2.TransType AND dg2.FechaDoc = ei2.FechaDoc
)
AND NOT EXISTS (
    SELECT 1 FROM dbo.la_daily_invoices di2
    WHERE di2.CardCode = ei2.CARCODE AND di2.TransType = ei2.TransType AND di2.FechaDoc = ei2.FechaDoc
      AND di2.InvoiceDocNum > 0
)

ORDER BY FechaDoc, CardCode, TransType, EsDevolucion
-- The data volume per date range varies a lot (e.g. days with/without payments loaded): a plan
-- cached for one range can take minutes on another, so always compile for the actual parameters.
OPTION (RECOMPILE)", sapDb);

            using (var cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@desde", desde.Date);
                cmd.Parameters.AddWithValue("@hasta", hasta.Date);
                cmd.Parameters.AddWithValue("@bplId", bplId);
                cmd.Parameters.AddWithValue("@includeInvoiced", includeInvoiced ? 1 : 0);
                cmd.Parameters.AddWithValue("@roundTol", InvoiceUploader.MaxRoundingAdjustment);
                cmd.Parameters.AddWithValue("@deeeItem", System.Configuration.ConfigurationManager.AppSettings["DeeeItemCode"] ?? "");
                cmd.CommandTimeout = 300;
                db.adapter.SelectCommand = cmd;
                db.adapter.Fill(dt);
            }
        }
        catch (Exception ex) { LastError = ex.Message; }
        finally { db.Disconnect(); }
        return dt;
    }

    // Per-transaction detail: transactions whose POS total doesn't match their payments.
    // Same logic as ADA's InvoicePreview, filtered by transType for DFS.
    public DataTable GetTransactionMismatches(string cardCode, DateTime fecha, bool esDevolucion, string transType)
    {
        LastError = null;
        var dt = new DataTable();
        var db = new SqlDb();
        db.Connect();
        try
        {
            using (var cmd = new SqlCommand(@"
;WITH SalesByTxn AS (
    SELECT ss.transnum AS Transnum, MAX(ss.TransactionID) AS TransactionID,
           ABS(SUM(CASE WHEN ss.DeliveryDocNum = -3 THEN -ABS(ss.Extsellprice) ELSE ABS(ss.Extsellprice) END))
             + ABS(MAX(ss.SalesTaxAmount)) AS SalesTotal
    FROM dbo.la_store_sales ss
    INNER JOIN dbo.ADR_TIENDA_SERIE ts
        ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE
    WHERE ts.CARCODE = @cardCode
      AND CAST(ss.itemdatetime AS DATE) = @fecha
      AND (CASE WHEN ss.DeliveryDocNum = -3 THEN 0 ELSE ss.txnmodifier END) = @txnmodifier
      AND ISNULL(ss.DeliveryDocNum, -1) <> 0
      AND ss.TRANSTYPE = @transType
    GROUP BY ss.transnum
),
PaymentsByTxn AS (
    SELECT sp.transnum AS Transnum,
           SUM(CASE WHEN sp.IsChange = 1 THEN -ABS(sp.Amount) ELSE ABS(sp.Amount) END) AS PaymentsTotal
    FROM dbo.la_store_payments sp
    WHERE sp.transnum IN (SELECT Transnum FROM SalesByTxn)
      AND sp.BusinessDayDate = @fecha
    GROUP BY sp.transnum
)
SELECT s.Transnum,
       SUBSTRING(s.Transnum, 5, 3) AS RegNo,
       SUBSTRING(s.Transnum, 8, LEN(s.Transnum)) AS EventNo,
       s.TransactionID, s.SalesTotal, ISNULL(p.PaymentsTotal, 0) AS PaymentsTotal,
       s.SalesTotal - ISNULL(p.PaymentsTotal, 0) AS Difference
FROM SalesByTxn s
LEFT JOIN PaymentsByTxn p ON p.Transnum = s.Transnum
WHERE ABS(s.SalesTotal - ISNULL(p.PaymentsTotal, 0)) > 0.01
ORDER BY ABS(s.SalesTotal - ISNULL(p.PaymentsTotal, 0)) DESC", db.Conn))
            {
                cmd.Parameters.AddWithValue("@cardCode", cardCode);
                cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                cmd.Parameters.AddWithValue("@txnmodifier", esDevolucion ? 1 : 0);
                cmd.Parameters.AddWithValue("@transType", transType ?? "DF");
                db.adapter.SelectCommand = cmd;
                db.adapter.Fill(dt);
            }
        }
        catch (Exception ex) { LastError = ex.Message; }
        finally { db.Disconnect(); }
        return dt;
    }

    // POS lines still without ODLN/ORDN (DeliveryDocNum NULL or -1), with their delivery
    // error message if any — same as ADA's InvoicePreview, adapted for DFS TRANSTYPE join.
    public DataTable GetPendingDeliveryItems(string cardCode, DateTime fecha, string transType)
    {
        LastError = null;
        var dt = new DataTable();
        var db = new SqlDb();
        db.Connect();
        try
        {
            using (var cmd = new SqlCommand(@"
SELECT ss.transnum AS Transnum,
       SUBSTRING(ss.transnum, 5, 3) AS RegNo,
       SUBSTRING(ss.transnum, 8, LEN(ss.transnum)) AS EventNo,
       ss.TransactionID,
       CASE WHEN ss.txnmodifier = 1 THEN 'Return' ELSE 'Sale' END AS Tipo,
       ss.skunum AS ItemCode, ss.pludesc AS Descripcion, ss.qty AS Qty, ss.extsellprice AS Monto,
       ss.WhsCode, ss.TRANSTYPE AS TransType,
       ISNULL(de.error_message, 'Not processed yet.') AS Estado
FROM dbo.la_store_sales ss
INNER JOIN dbo.ADR_TIENDA_SERIE ts
    ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE
LEFT JOIN dbo.la_delivery_errors de ON (
        (de.TransactionID IS NOT NULL AND de.TransactionID = ss.TransactionID AND de.itemnum = ss.itemnum)
     OR (de.TransactionID IS NULL AND de.transnum = ss.transnum AND de.itemnum = ss.itemnum
         AND CAST(de.itemdatetime AS DATE) = CAST(ss.itemdatetime AS DATE)))
WHERE ts.CARCODE = @cardCode
  AND CAST(ss.itemdatetime AS DATE) = @fecha
  AND ISNULL(ss.DeliveryDocNum, -1) = -1
  AND ss.TRANSTYPE = @transType
ORDER BY ss.TransactionID, ss.itemnum", db.Conn))
            {
                cmd.Parameters.AddWithValue("@cardCode", cardCode);
                cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                cmd.Parameters.AddWithValue("@transType", transType ?? "");
                db.adapter.SelectCommand = cmd;
                db.adapter.Fill(dt);
            }
        }
        catch (Exception ex) { LastError = ex.Message; }
        finally { db.Disconnect(); }
        return dt;
    }

    // Records the result of a direct upload attempt in la_invoice_upload_requests. If the group
    // was queued ("In Progress", ProcessedAt NULL), that request is completed instead of adding a row.
    public void RecordUploadResult(string cardCode, string transType, DateTime fecha, string requestedBy, bool success, string summary)
    {
        LastError = null;
        var db = new SqlDb();
        db.Connect();
        try
        {
            using (var cmd = new SqlCommand(@"
UPDATE dbo.la_invoice_upload_requests
SET ProcessedAt = GETDATE(), Success = @success, Summary = @summary
WHERE ProcessedAt IS NULL AND CardCode = @cardCode AND ISNULL(TransType, '') = @transType AND FechaDoc = @fecha;

IF @@ROWCOUNT = 0
    INSERT INTO dbo.la_invoice_upload_requests (CardCode, TransType, FechaDoc, RequestedBy, ProcessedAt, Success, Summary)
    VALUES (@cardCode, @transType, @fecha, @requestedBy, GETDATE(), @success, @summary)", db.Conn))
            {
                cmd.Parameters.AddWithValue("@cardCode", cardCode);
                cmd.Parameters.AddWithValue("@transType", transType ?? "");
                cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                cmd.Parameters.AddWithValue("@requestedBy", requestedBy ?? "");
                cmd.Parameters.AddWithValue("@success", success);
                cmd.Parameters.AddWithValue("@summary", summary ?? "");
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex) { LastError = ex.Message; }
        finally { db.Disconnect(); }
    }

    // Queue mode (appSetting FacturacionSAP_UploadMode = Queue): "Upload Selected" only leaves the
    // group pending (ProcessedAt NULL = "In Progress"); FacturacionSAPConsole --process-queue uploads
    // it at night. Returns false if that group already had a pending request (no duplicate).
    public bool QueueUploadRequest(string cardCode, string transType, DateTime fecha, string requestedBy)
    {
        LastError = null;
        var db = new SqlDb();
        db.Connect();
        try
        {
            using (var cmd = new SqlCommand(@"
INSERT INTO dbo.la_invoice_upload_requests (CardCode, TransType, FechaDoc, RequestedBy)
SELECT @cardCode, @transType, @fecha, @requestedBy
WHERE NOT EXISTS (SELECT 1 FROM dbo.la_invoice_upload_requests
                  WHERE ProcessedAt IS NULL AND CardCode = @cardCode
                    AND ISNULL(TransType, '') = @transType AND FechaDoc = @fecha)", db.Conn))
            {
                cmd.Parameters.AddWithValue("@cardCode", cardCode);
                cmd.Parameters.AddWithValue("@transType", transType ?? "");
                cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                cmd.Parameters.AddWithValue("@requestedBy", requestedBy ?? "");
                return cmd.ExecuteNonQuery() > 0;
            }
        }
        catch (Exception ex) { LastError = ex.Message; return false; }
        finally { db.Disconnect(); }
    }

    // Pending ("In Progress") requests, oldest first. bplId 0 = all branches (night process).
    public DataTable GetPendingUploadRequests(int bplId)
    {
        LastError = null;
        var dt = new DataTable();
        var db = new SqlDb();
        db.Connect();
        try
        {
            using (var cmd = new SqlCommand(@"
SELECT r.Id, r.CardCode, ISNULL(r.TransType, '') AS TransType, r.FechaDoc, r.RequestedBy, r.RequestedAt
FROM dbo.la_invoice_upload_requests r
WHERE r.ProcessedAt IS NULL
  AND (@bplId = 0 OR EXISTS (SELECT 1 FROM dbo.ADR_TIENDA_SERIE ts
                             WHERE ts.CARCODE = r.CardCode AND CAST(ts.CompanyId AS INT) = @bplId))
ORDER BY r.RequestedAt, r.Id", db.Conn))
            {
                cmd.Parameters.AddWithValue("@bplId", bplId);
                db.adapter.SelectCommand = cmd;
                db.adapter.Fill(dt);
            }
        }
        catch (Exception ex) { LastError = ex.Message; }
        finally { db.Disconnect(); }
        return dt;
    }

    // Completes a queued request with the result of the night upload.
    public void MarkUploadRequestProcessed(int id, bool success, string summary)
    {
        LastError = null;
        var db = new SqlDb();
        db.Connect();
        try
        {
            using (var cmd = new SqlCommand(@"
UPDATE dbo.la_invoice_upload_requests
SET ProcessedAt = GETDATE(), Success = @success, Summary = @summary
WHERE Id = @id AND ProcessedAt IS NULL", db.Conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@success", success);
                cmd.Parameters.AddWithValue("@summary", summary ?? "");
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex) { LastError = ex.Message; }
        finally { db.Disconnect(); }
    }

    // Recent upload history for the date range (newest first), only the stores of the session's
    // branch: la_invoice_upload_requests has no BPLId, so it comes from the store's CardCode in
    // ADR_TIENDA_SERIE (CompanyId = BPLId).
    public DataTable GetUploadRequests(DateTime desde, DateTime hasta, int bplId)
    {
        LastError = null;
        var dt = new DataTable();
        var db = new SqlDb();
        db.Connect();
        try
        {
            using (var cmd = new SqlCommand(@"
SELECT r.Id, r.CardCode, r.TransType, r.FechaDoc, r.RequestedBy, r.RequestedAt, r.ProcessedAt, r.Success, r.Summary
FROM dbo.la_invoice_upload_requests r
WHERE r.FechaDoc BETWEEN @desde AND @hasta
  AND EXISTS (SELECT 1 FROM dbo.ADR_TIENDA_SERIE ts
              WHERE ts.CARCODE = r.CardCode AND CAST(ts.CompanyId AS INT) = @bplId)
ORDER BY r.RequestedAt DESC", db.Conn))
            {
                cmd.Parameters.AddWithValue("@desde", desde.Date);
                cmd.Parameters.AddWithValue("@hasta", hasta.Date);
                cmd.Parameters.AddWithValue("@bplId", bplId);
                db.adapter.SelectCommand = cmd;
                db.adapter.Fill(dt);
            }
        }
        catch (Exception ex) { LastError = ex.Message; }
        finally { db.Disconnect(); }
        return dt;
    }
}
