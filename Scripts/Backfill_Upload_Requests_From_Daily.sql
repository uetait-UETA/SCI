/* =====================================================================
   Registra en la_invoice_upload_requests (grid "Upload Requests" de
   FacturacionSAP.aspx) las facturas / notas de crédito que ya están en
   la_daily_invoices / la_daily_returns pero que no se subieron desde la
   pantalla (las creó el proceso automático de integración POS o se
   registraron con Backfill_Daily_Invoices_From_SAP.sql).

   - Una fila por CardCode + TransType (DF/DP) + día, con RequestedBy =
     'POS INTEGRATION' y el detalle de OINV / ORIN / pago / reembolso.
   - RequestedAt / ProcessedAt = fecha en que se registró el documento.
   - No duplica: omite los grupos que ya tienen una fila exitosa en la tabla.

   USO: ejecutar en la base SMM (SMM_PROD).
        Primero con @Commit = 0 (simulación), luego con @Commit = 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;                    -- 0 = simulación (ROLLBACK), 1 = confirmar
DECLARE @By nvarchar(200) = N'POS INTEGRATION';

IF OBJECT_ID('tempdb..#req') IS NOT NULL DROP TABLE #req;

SELECT COALESCE(i.CardCode, r.CardCode)   AS CardCode,
       COALESCE(i.TransType, r.TransType) AS TransType,
       COALESCE(i.FechaDoc, r.FechaDoc)   AS FechaDoc,
       CASE WHEN i.date_created IS NULL THEN r.date_created
            WHEN r.date_created IS NULL THEN i.date_created
            WHEN i.date_created < r.date_created THEN i.date_created ELSE r.date_created END AS RegistradoEl,
       STUFF(
           ISNULL('; OINV #' + CAST(i.InvoiceDocNum AS nvarchar(20))
                  + ISNULL(' (Payment #' + CAST(i.PaymentDocNum AS nvarchar(20)) + ')', ''), '')
         + ISNULL('; ORIN #' + CAST(r.ReturnDocNum AS nvarchar(20))
                  + ISNULL(' (Refund #' + CAST(r.RefundDocNum AS nvarchar(20)) + ')', ''), '')
         , 1, 2, '') AS Summary
INTO #req
FROM (SELECT * FROM dbo.la_daily_invoices WHERE InvoiceDocNum > 0) i
FULL JOIN (SELECT * FROM dbo.la_daily_returns WHERE ReturnDocNum > 0) r
       ON r.CardCode = i.CardCode AND r.TransType = i.TransType AND r.FechaDoc = i.FechaDoc;

DELETE q FROM #req q
WHERE EXISTS (SELECT 1 FROM dbo.la_invoice_upload_requests u
              WHERE u.CardCode = q.CardCode AND u.FechaDoc = q.FechaDoc
                AND ISNULL(u.TransType, '') = ISNULL(q.TransType, '') AND u.Success = 1);

SELECT COUNT(*) AS PorRegistrar, MIN(FechaDoc) AS Desde, MAX(FechaDoc) AS Hasta FROM #req;
SELECT TOP 10 CardCode, TransType, FechaDoc, Summary FROM #req ORDER BY FechaDoc, CardCode, TransType;

BEGIN TRAN;

INSERT INTO dbo.la_invoice_upload_requests (CardCode, TransType, FechaDoc, RequestedBy, RequestedAt, ProcessedAt, Success, Summary)
SELECT CardCode, TransType, FechaDoc, @By, RegistradoEl, RegistradoEl, 1, Summary
FROM #req
ORDER BY FechaDoc, CardCode, TransType;
PRINT 'la_invoice_upload_requests: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s)';

IF @Commit = 1
BEGIN
    COMMIT;
    PRINT 'COMMIT: Upload Requests actualizado.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT 'SIMULACION: se hizo ROLLBACK. Cambiar @Commit = 1 para confirmar.';
END
