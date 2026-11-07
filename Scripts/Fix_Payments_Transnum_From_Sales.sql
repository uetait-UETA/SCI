/* =====================================================================
   Corrige la_store_payments.transnum cuando no coincide con el de su venta.

   Caso encontrado (2026-11-01): 215 transacciones de C3017 (06/08-15/08)
   tienen el pago con transnum "3012..." y la venta con "3017..." (mismo
   TransactionID y mismo monto). FacturacionSAP cruza ventas y pagos por
   TransactionID + transnum, así que esos pagos no se contaban y la tienda
   quedaba Blocked (ej. C3017 DF 06/08: 1,310 vs 1,075).

   Regla: el pago se reasigna al transnum de la venta solo si
   - no existe ninguna venta con el mismo TransactionID + transnum del pago,
   - el TransactionID corresponde a UNA sola venta (un solo transnum), y
   - la transacción no tiene ya pagos con el transnum correcto (si los tiene,
     el "3012..." es una copia duplicada de la carga anterior).
   Las filas cambiadas se guardan en dbo.la_store_payments_bak_transnum.

   USO: ejecutar en SMM_PROD. Primero con @Commit = 0 (simulación),
        luego con @Commit = 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar

IF OBJECT_ID('tempdb..#ventas') IS NOT NULL DROP TABLE #ventas;
IF OBJECT_ID('tempdb..#fix')    IS NOT NULL DROP TABLE #fix;

-- 1. transnum de cada TransactionID en ventas (solo los que tienen uno solo)
SELECT ss.TransactionID, MIN(ss.transnum) AS transnum
INTO #ventas
FROM dbo.la_store_sales ss WITH (NOLOCK)
WHERE ss.TransactionID IS NOT NULL
  AND ss.TransactionID IN (SELECT DISTINCT TransactionID FROM dbo.la_store_payments)
GROUP BY ss.TransactionID
HAVING COUNT(DISTINCT ss.transnum) = 1;

-- 2. Pagos a corregir
SELECT p.ID, p.TransactionID, p.transnum AS TransnumAnterior, v.transnum AS TransnumNuevo,
       p.TenderID, p.Amount, p.IsChange, p.BusinessDayDate
INTO #fix
FROM dbo.la_store_payments p
INNER JOIN #ventas v ON v.TransactionID = p.TransactionID
WHERE p.transnum <> v.transnum
  AND NOT EXISTS (SELECT 1 FROM dbo.la_store_sales s WITH (NOLOCK)
                  WHERE s.TransactionID = p.TransactionID AND s.transnum = p.transnum)
  -- Si la transacción ya tiene pagos con el transnum correcto, el de "3012..." es una copia
  -- de la carga anterior a la división de la tienda: no se mueve (duplicaría el pago).
  AND NOT EXISTS (SELECT 1 FROM dbo.la_store_payments o
                  WHERE o.TransactionID = p.TransactionID AND o.transnum = v.transnum);

-- Resumen
SELECT LEFT(TransnumAnterior, 4) AS PrefijoAnterior, LEFT(TransnumNuevo, 4) AS PrefijoNuevo,
       COUNT(DISTINCT TransactionID) AS Transacciones, COUNT(*) AS FilasPago,
       MIN(BusinessDayDate) AS Desde, MAX(BusinessDayDate) AS Hasta
FROM #fix
GROUP BY LEFT(TransnumAnterior, 4), LEFT(TransnumNuevo, 4);

BEGIN TRAN;

IF OBJECT_ID('dbo.la_store_payments_bak_transnum') IS NULL
    CREATE TABLE dbo.la_store_payments_bak_transnum (
        ID               bigint       NOT NULL,
        TransactionID    bigint       NULL,
        TransnumAnterior varchar(50)  NULL,
        TransnumNuevo    varchar(50)  NULL,
        date_fixed       datetime     NOT NULL DEFAULT GETDATE()
    );

INSERT dbo.la_store_payments_bak_transnum (ID, TransactionID, TransnumAnterior, TransnumNuevo)
SELECT ID, TransactionID, TransnumAnterior, TransnumNuevo FROM #fix;

UPDATE p SET p.transnum = f.TransnumNuevo
FROM dbo.la_store_payments p
INNER JOIN #fix f ON f.ID = p.ID;
PRINT 'la_store_payments: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) corregidas';

IF @Commit = 1
BEGIN
    COMMIT;
    PRINT 'COMMIT: pagos corregidos. Respaldo en dbo.la_store_payments_bak_transnum.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT 'SIMULACION: se hizo ROLLBACK. Cambiar @Commit = 1 para confirmar.';
END

/* Para revertir:
UPDATE p SET p.transnum = b.TransnumAnterior
FROM dbo.la_store_payments p INNER JOIN dbo.la_store_payments_bak_transnum b ON b.ID = p.ID;
*/
