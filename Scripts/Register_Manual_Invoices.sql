/* =====================================================================
   Registra en SCI las facturas + cobros creados a mano en SAP (fuera de
   FacturacionSAP.aspx), para que aparezcan en "Upload Requests" y en
   la_daily_invoices igual que las subidas desde la pantalla.

   Caso: C3017 DF 11/08-13/08 — por la división de la tienda 3012 (series
   201/204 pasaron a C3017) parte de las ODLN de esos días quedaron a nombre
   de C3012; se factura cada parte a su cliente.

   - la_invoice_upload_requests: inserta la fila si no existe ya ese Summary.
   - la_daily_invoices: MERGE por CardCode + FechaDoc + TransType (igual que
     InvoiceUploader); no pisa un registro que ya tenga otra factura.

   USO: ejecutar en SMM_PROD. Agregar filas en @docs para cada día.
        Primero con @Commit = 0 (simulación), luego con @Commit = 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar

DECLARE @docs TABLE (
    CardCode nvarchar(15), TransType nvarchar(5), FechaDoc date, BPLId int,
    InvoiceDocNum int, InvoiceDocEntry int, PaymentDocNum int,
    RequestedBy nvarchar(50), Nota nvarchar(200)
);
INSERT @docs VALUES
 ('C3012', 'DF', '2026-08-11', 3, 216, 216, 891, 'dgiltie', 'manual: C3017 DF sales, ODLN under C3012 (store split)'),
 ('C3017', 'DF', '2026-08-11', 3, 217, 217, 892, 'dgiltie', 'manual: split by ODLN customer (store split)'),
 ('C3012', 'DF', '2026-08-12', 3, 218, 218, 893, 'dgiltie', 'manual: C3017 DF sales, ODLN under C3012 (store split)'),
 ('C3017', 'DF', '2026-08-12', 3, 219, 219, 894, 'dgiltie', 'manual: split by ODLN customer (store split), rounding adj. -0.01'),
 ('C3012', 'DF', '2026-08-13', 3, 220, 220, 895, 'dgiltie', 'manual: C3017 DF sales, ODLN under C3012 (store split)'),
 ('C3017', 'DF', '2026-08-13', 3, 221, 221, 896, 'dgiltie', 'manual: split by ODLN customer (store split)');

-- Lo que se va a registrar, con el estado real en SAP
SELECT d.*, o.CardCode AS SapCardCode, o.DocTotal, o.PaidToDate, o.DocStatus,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.la_invoice_upload_requests r
                         WHERE r.CardCode = d.CardCode AND r.FechaDoc = d.FechaDoc AND r.TransType = d.TransType
                           AND r.Summary LIKE 'OINV #' + CAST(d.InvoiceDocNum AS varchar(10)) + ' %')
            THEN 'ya registrado' ELSE 'nuevo' END AS UploadRequest,
       (SELECT TOP 1 CAST(i.InvoiceDocNum AS varchar(10)) FROM dbo.la_daily_invoices i
         WHERE i.CardCode = d.CardCode AND i.FechaDoc = d.FechaDoc AND i.TransType = d.TransType) AS DailyActual
FROM @docs d
LEFT JOIN PRO_DFSC.dbo.OINV o ON o.DocEntry = d.InvoiceDocEntry;

BEGIN TRAN;

INSERT INTO dbo.la_invoice_upload_requests (CardCode, TransType, FechaDoc, RequestedBy, ProcessedAt, Success, Summary)
SELECT d.CardCode, d.TransType, d.FechaDoc, d.RequestedBy, GETDATE(), 1,
       'OINV #' + CAST(d.InvoiceDocNum AS varchar(10)) + ' + payment #' + CAST(d.PaymentDocNum AS varchar(10)) + ' (' + d.Nota + ')'
FROM @docs d
WHERE NOT EXISTS (SELECT 1 FROM dbo.la_invoice_upload_requests r
                  WHERE r.CardCode = d.CardCode AND r.FechaDoc = d.FechaDoc AND r.TransType = d.TransType
                    AND r.Summary LIKE 'OINV #' + CAST(d.InvoiceDocNum AS varchar(10)) + ' %');
PRINT 'la_invoice_upload_requests: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s)';

MERGE dbo.la_daily_invoices AS t
USING @docs AS s
   ON t.CardCode = s.CardCode AND t.FechaDoc = s.FechaDoc AND t.TransType = s.TransType
WHEN MATCHED AND ISNULL(t.InvoiceDocNum, 0) <= 0 THEN
    UPDATE SET InvoiceDocNum = s.InvoiceDocNum, InvoiceDocEntry = s.InvoiceDocEntry, PaymentDocNum = s.PaymentDocNum,
               error_message = NULL, date_updated = GETDATE()
WHEN NOT MATCHED THEN
    INSERT (CompanyId, CardCode, BPLId, FechaDoc, TransType, InvoiceDocNum, InvoiceDocEntry, PaymentDocNum, date_created, error_message)
    VALUES (0, s.CardCode, s.BPLId, s.FechaDoc, s.TransType, s.InvoiceDocNum, s.InvoiceDocEntry, s.PaymentDocNum, GETDATE(), NULL);
PRINT 'la_daily_invoices: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s)';

IF @Commit = 1
BEGIN
    COMMIT;
    PRINT 'COMMIT: facturas registradas.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT 'SIMULACION: se hizo ROLLBACK. Cambiar @Commit = 1 para confirmar.';
END
