/* =====================================================================
   Llena la_daily_invoices / la_daily_returns con las facturas (OINV) y
   notas de crédito (ORIN) que ya existen en SAP para las tiendas
   (clientes C30xx LAX / C31xx SFO) pero que no quedaron registradas.

   Un grupo = CardCode + TransType (DF/DP) + día de venta + venta/devolución,
   igual que FacturacionSAP.aspx. El documento de SAP se identifica por sus
   líneas basadas en las ODLN (BaseType 15) / ORDN (BaseType 16) de esas ventas
   (la_store_sales.DeliveryDocEntry).

   - Inserta los grupos que no existen.
   - Actualiza los que existen sin número válido (NULL, 0 o -1 = error previo).
   - No toca los que ya tienen número.
   - Pago (RCT2 → ORCT) y reembolso (VPM2 → OVPM) si existen.

   USO: ejecutar en la base SMM (SMM_PROD). Si la base SAP no es PRO_DFSC,
        reemplazar "PRO_DFSC." en todo el script.
        Primero con @Commit = 0 (simulación), luego con @Commit = 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit  = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar
DECLARE @Desde  date = '2024-01-01';
DECLARE @Hasta  date = CAST(GETDATE() AS date);

IF OBJECT_ID('tempdb..#grupos') IS NOT NULL DROP TABLE #grupos;
IF OBJECT_ID('tempdb..#docs')   IS NOT NULL DROP TABLE #docs;

-- 1. ODLN / ORDN de cada grupo de ventas
SELECT DISTINCT ts.CARCODE, CAST(ts.CompanyId AS INT) AS BPLId, ss.TRANSTYPE AS TransType,
       CAST(ss.itemdatetime AS DATE) AS FechaDoc, ss.txnmodifier, ss.DeliveryDocEntry
INTO #grupos
FROM dbo.la_store_sales ss
INNER JOIN dbo.ADR_TIENDA_SERIE ts
    ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE
WHERE CAST(ss.itemdatetime AS DATE) BETWEEN @Desde AND @Hasta
  AND ss.DeliveryDocNum > 0
  AND ss.DeliveryDocEntry IS NOT NULL AND ss.DeliveryDocEntry <> 99999999
  AND (ts.CARCODE LIKE 'C30%' OR ts.CARCODE LIKE 'C31%');

-- 2. Documento de SAP que copió esas entregas (el más reciente por grupo)
SELECT g.CARCODE, g.BPLId, g.TransType, g.FechaDoc, g.txnmodifier,
       MAX(o.DocNum) AS DocNum, MAX(o.DocEntry) AS DocEntry
INTO #docs
FROM #grupos g
INNER JOIN PRO_DFSC.dbo.INV1 l ON l.BaseType = 15 AND l.BaseEntry = g.DeliveryDocEntry
INNER JOIN PRO_DFSC.dbo.OINV o ON o.DocEntry = l.DocEntry AND o.CANCELED = 'N' AND o.CardCode = g.CARCODE
WHERE g.txnmodifier = 0
GROUP BY g.CARCODE, g.BPLId, g.TransType, g.FechaDoc, g.txnmodifier
UNION ALL
SELECT g.CARCODE, g.BPLId, g.TransType, g.FechaDoc, g.txnmodifier,
       MAX(o.DocNum), MAX(o.DocEntry)
FROM #grupos g
INNER JOIN PRO_DFSC.dbo.RIN1 l ON l.BaseType = 16 AND l.BaseEntry = g.DeliveryDocEntry
INNER JOIN PRO_DFSC.dbo.ORIN o ON o.DocEntry = l.DocEntry AND o.CANCELED = 'N' AND o.CardCode = g.CARCODE
WHERE g.txnmodifier = 1
GROUP BY g.CARCODE, g.BPLId, g.TransType, g.FechaDoc, g.txnmodifier;

-- Resumen de lo que se va a registrar
SELECT CASE d.txnmodifier WHEN 0 THEN 'OINV -> la_daily_invoices' ELSE 'ORIN -> la_daily_returns' END AS Destino,
       SUM(CASE WHEN x.ID IS NULL THEN 1 ELSE 0 END) AS Insertar,
       SUM(CASE WHEN x.ID IS NOT NULL AND ISNULL(x.Num, 0) <= 0 THEN 1 ELSE 0 END) AS Actualizar,
       SUM(CASE WHEN x.ID IS NOT NULL AND x.Num > 0 THEN 1 ELSE 0 END) AS YaRegistrados,
       MIN(d.FechaDoc) AS Desde, MAX(d.FechaDoc) AS Hasta
FROM #docs d
OUTER APPLY (
    SELECT i.ID, i.InvoiceDocNum AS Num FROM dbo.la_daily_invoices i
    WHERE d.txnmodifier = 0 AND i.CardCode = d.CARCODE AND i.FechaDoc = d.FechaDoc AND i.TransType = d.TransType
    UNION ALL
    SELECT r.ID, r.ReturnDocNum FROM dbo.la_daily_returns r
    WHERE d.txnmodifier = 1 AND r.CardCode = d.CARCODE AND r.FechaDoc = d.FechaDoc AND r.TransType = d.TransType
) x
GROUP BY d.txnmodifier;

BEGIN TRAN;

-- 3. Facturas
MERGE dbo.la_daily_invoices AS t
USING (
    SELECT d.*, (SELECT MAX(p.DocNum) FROM PRO_DFSC.dbo.RCT2 r2
                 INNER JOIN PRO_DFSC.dbo.ORCT p ON p.DocEntry = r2.DocNum AND p.Canceled = 'N'
                 WHERE r2.InvType = 13 AND r2.DocEntry = d.DocEntry) AS PayNum
    FROM #docs d WHERE d.txnmodifier = 0
) AS s
   ON t.CardCode = s.CARCODE AND t.FechaDoc = s.FechaDoc AND t.TransType = s.TransType
WHEN MATCHED AND ISNULL(t.InvoiceDocNum, 0) <= 0 THEN
    UPDATE SET InvoiceDocNum = s.DocNum, InvoiceDocEntry = s.DocEntry,
               PaymentDocNum = ISNULL(t.PaymentDocNum, s.PayNum),
               error_message = NULL, date_updated = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (CompanyId, CardCode, BPLId, FechaDoc, TransType, InvoiceDocNum, InvoiceDocEntry, PaymentDocNum, date_created, error_message)
    VALUES (s.BPLId, s.CARCODE, s.BPLId, s.FechaDoc, s.TransType, s.DocNum, s.DocEntry, s.PayNum, GETDATE(), NULL);
PRINT 'la_daily_invoices: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s)';

-- 4. Notas de crédito
MERGE dbo.la_daily_returns AS t
USING (
    SELECT d.*, (SELECT MAX(p.DocNum) FROM PRO_DFSC.dbo.VPM2 v2
                 INNER JOIN PRO_DFSC.dbo.OVPM p ON p.DocEntry = v2.DocNum AND p.Canceled = 'N'
                 WHERE v2.InvType = 14 AND v2.DocEntry = d.DocEntry) AS RefNum
    FROM #docs d WHERE d.txnmodifier = 1
) AS s
   ON t.CardCode = s.CARCODE AND t.FechaDoc = s.FechaDoc AND t.TransType = s.TransType
WHEN MATCHED AND ISNULL(t.ReturnDocNum, 0) <= 0 THEN
    UPDATE SET ReturnDocNum = s.DocNum, ReturnDocEntry = s.DocEntry,
               RefundDocNum = ISNULL(t.RefundDocNum, s.RefNum),
               error_message = NULL, date_updated = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (CompanyId, CardCode, BPLId, FechaDoc, TransType, ReturnDocNum, ReturnDocEntry, RefundDocNum, date_created)
    VALUES (s.BPLId, s.CARCODE, s.BPLId, s.FechaDoc, s.TransType, s.DocNum, s.DocEntry, s.RefNum, GETDATE());
PRINT 'la_daily_returns: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s)';

IF @Commit = 1
BEGIN
    COMMIT;
    PRINT 'COMMIT: tablas actualizadas.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT 'SIMULACION: se hizo ROLLBACK. Cambiar @Commit = 1 para confirmar.';
END
