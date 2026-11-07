/* =====================================================================
   Revierte los pagos DUPLICADOS que movió la primera versión de
   Fix_Payments_Transnum_From_Sales.sql (corrida el 2026-11-01).

   Esa versión cambió 248 pagos de "3012..." a "3017...". De esos, 113 filas
   (99 transacciones) eran copias de la carga anterior a la división de la
   tienda: la transacción ya tenía sus pagos con "3017...", así que quedaron
   duplicados (ej. C3017 DF 06/08: pagos 2,170 vs factura 1,310).

   Este script devuelve a su transnum anterior solo las filas cuya
   transacción tiene además otros pagos que el script no tocó, y las quita
   de dbo.la_store_payments_bak_transnum (que queda solo con los 135 pagos
   que sí faltaban).

   USO: ejecutar en SMM_PROD. Primero con @Commit = 0 (simulación),
        luego con @Commit = 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar

IF OBJECT_ID('tempdb..#rev') IS NOT NULL DROP TABLE #rev;

SELECT b.ID, b.TransactionID, b.TransnumAnterior, b.TransnumNuevo
INTO #rev
FROM dbo.la_store_payments_bak_transnum b
WHERE EXISTS (SELECT 1 FROM dbo.la_store_payments o
              WHERE o.TransactionID = b.TransactionID
                AND o.transnum = b.TransnumNuevo
                AND o.ID NOT IN (SELECT ID FROM dbo.la_store_payments_bak_transnum));

-- Resumen (esperado: 99 transacciones / 113 filas)
SELECT COUNT(DISTINCT TransactionID) AS Transacciones, COUNT(*) AS FilasARevertir FROM #rev;

BEGIN TRAN;

UPDATE p SET p.transnum = r.TransnumAnterior
FROM dbo.la_store_payments p
INNER JOIN #rev r ON r.ID = p.ID AND p.transnum = r.TransnumNuevo;
PRINT 'la_store_payments: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) revertidas';

DELETE b FROM dbo.la_store_payments_bak_transnum b INNER JOIN #rev r ON r.ID = b.ID;
PRINT 'la_store_payments_bak_transnum: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) quitadas';

IF @Commit = 1
BEGIN
    COMMIT;
    PRINT 'COMMIT: duplicados revertidos.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT 'SIMULACION: se hizo ROLLBACK. Cambiar @Commit = 1 para confirmar.';
END
