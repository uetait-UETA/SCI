/* =====================================================================
   C3117 DP 06/08/2026 y C3008 DF 07/08/2026 — ventas apuntando a
   entregas devueltas a mano (mismo caso que
   Fix_C3117_0807_Redirect_ODLN6253.sql).

   El 07/08 el usuario SAP "cacre" devolvió y volvió a entregar:
     - C3117: ORDN 186 (2,715.27) sobre 108 ODLN DP del 06/08 (167 líneas)
              → ODLN 6252 "Based On Return 186" (167 líneas, impuesto 232.27).
     - C3008: ORDN 188 (3,400.00) sobre la ODLN 5694 del 07/08 (1 línea)
              → ODLN 6254 "Replace Delivery 5694".
   la_store_sales sigue apuntando a las ODLN originales (cerradas por la
   devolución): SAP Invoicing mostraba "bost_Close" y no podía facturar.

   Este script apunta esas ventas a la ODLN nueva (la que tiene las líneas
   abiertas). No toca SAP. Respaldo en dbo.la_store_sales_bak_cacre_0806.

   USO: ejecutar en SMM_PROD. Primero @Commit = 0 (simulación), luego 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar

DECLARE @map TABLE (CardCode nvarchar(20), TransType nvarchar(5), Fecha date, ReturnEntry int, NewEntry int, Lineas int);
INSERT @map VALUES ('C3117', 'DP', '2026-08-06', 186, 6252, 167),
                   ('C3008', 'DF', '2026-08-07', 188, 6254, 1);

IF OBJECT_ID('tempdb..#ventas') IS NOT NULL DROP TABLE #ventas;
SELECT s.ID, m.CardCode, m.NewEntry, s.TransactionID, s.DeliveryDocEntry, s.Extsellprice
INTO #ventas
FROM @map m
INNER JOIN dbo.la_store_sales s ON s.TRANSTYPE = m.TransType AND s.txnmodifier = 0 AND CAST(s.itemdatetime AS DATE) = m.Fecha
INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = s.NUMSERIE AND ts.NUMSTORE = CAST(s.storenum AS INT)
                                  AND ts.DUTYTYPE = s.TRANSTYPE AND ts.CARCODE = m.CardCode
-- solo ventas cuya ODLN original fue devuelta por esa ORDN
WHERE EXISTS (SELECT 1 FROM PRO_DFSC.dbo.DLN1 l
              WHERE l.DocEntry = s.DeliveryDocEntry AND l.TargetType = 16 AND l.TrgetEntry = m.ReturnEntry);

-- Validaciones: mismas líneas que la ODLN nueva, y la ODLN nueva abierta con esas líneas abiertas
SELECT m.CardCode, m.NewEntry, COUNT(v.ID) AS ventas, m.Lineas AS esperadas,
       (SELECT COUNT(*) FROM PRO_DFSC.dbo.DLN1 l WHERE l.DocEntry = m.NewEntry AND l.LineStatus = 'O') AS abiertas_odln_nueva,
       SUM(v.Extsellprice) AS neto,
       (SELECT SUM(l.LineTotal) FROM PRO_DFSC.dbo.DLN1 l WHERE l.DocEntry = m.NewEntry) AS neto_odln_nueva
FROM @map m LEFT JOIN #ventas v ON v.CardCode = m.CardCode
GROUP BY m.CardCode, m.NewEntry, m.Lineas;

IF EXISTS (SELECT 1 FROM @map m
           WHERE (SELECT COUNT(*) FROM #ventas v WHERE v.CardCode = m.CardCode) <> m.Lineas
              OR (SELECT COUNT(*) FROM PRO_DFSC.dbo.DLN1 l WHERE l.DocEntry = m.NewEntry AND l.LineStatus = 'O') <> m.Lineas)
BEGIN RAISERROR('Las líneas no coinciden con lo esperado: revisar antes de continuar.', 16, 1); RETURN; END

BEGIN TRAN;

IF OBJECT_ID('dbo.la_store_sales_bak_cacre_0806') IS NULL
    SELECT s.*, GETDATE() AS bak_date INTO dbo.la_store_sales_bak_cacre_0806
    FROM dbo.la_store_sales s INNER JOIN #ventas v ON v.ID = s.ID;

UPDATE s SET s.DeliveryDocEntry = v.NewEntry,
             s.DeliveryDocNum = (SELECT o.DocNum FROM PRO_DFSC.dbo.ODLN o WHERE o.DocEntry = v.NewEntry)
FROM dbo.la_store_sales s INNER JOIN #ventas v ON v.ID = s.ID;
PRINT 'la_store_sales: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) apuntadas a la ODLN nueva';

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
