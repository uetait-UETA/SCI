/* =====================================================================
   C3012 DF 11, 12 y 13/08/2026 — liberar el grupo para las ventas Export.

   Las OINV #216, #218 y #220 (C3012 Duty Free) se hicieron a mano el
   2026-11-01 por la división C3012/C3017 (ventas de las cajas 201/204) y
   se registraron en la_daily_invoices como CardCode C3012, TransType DF.
   Con eso SAP Invoicing da por facturado "C3012 DF" de esos días y oculta
   / no sube las ventas Export (DF sin impuesto) de las cajas 202/203/205/207,
   que no están en ninguna factura (77 txns, 7,841.40; ver
   Add_ADR_TIENDA_SERIE_Missing_DutyType.sql).

   Este script cambia TransType de esas 3 filas a 'DFM' (DF manual de la
   división). No toca SAP ni borra nada: las facturas y sus cobros siguen
   igual y Upload Requests conserva el historial. Después "C3012 DF" de esos
   días aparece Ready solo con las ventas Export.

   Revertir: UPDATE dbo.la_daily_invoices SET TransType = 'DF'
             WHERE ID IN (230, 232, 234);   (si aún no se subieron las Export)

   USO: ejecutar en SMM_PROD. Primero @Commit = 0 (simulación), luego 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar

SELECT ID, CardCode, FechaDoc, TransType, InvoiceDocNum, PaymentDocNum
FROM dbo.la_daily_invoices
WHERE CardCode = 'C3012' AND TransType = 'DF' AND InvoiceDocNum IN (216, 218, 220)
  AND FechaDoc BETWEEN '2026-08-11' AND '2026-08-13';

BEGIN TRAN;

UPDATE dbo.la_daily_invoices
SET TransType = 'DFM', date_updated = GETDATE(),
    error_message = 'Factura manual division C3012/C3017 (cajas 201/204); liberada para ventas Export C3012 DF'
WHERE CardCode = 'C3012' AND TransType = 'DF' AND InvoiceDocNum IN (216, 218, 220)
  AND FechaDoc BETWEEN '2026-08-11' AND '2026-08-13';
PRINT 'la_daily_invoices: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) marcadas DFM';

SELECT ID, CardCode, FechaDoc, TransType, InvoiceDocNum, PaymentDocNum, error_message
FROM dbo.la_daily_invoices
WHERE CardCode = 'C3012' AND FechaDoc BETWEEN '2026-08-11' AND '2026-08-13'
ORDER BY FechaDoc, TransType;

IF @Commit = 1
BEGIN
    COMMIT;
    PRINT 'CONFIRMADO.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT 'SIMULACION: se hizo ROLLBACK. Cambiar @Commit = 1 para confirmar.';
END
