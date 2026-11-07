/* =====================================================================
   C3117 DP 07/08/2026 — ventas apuntando a entregas devueltas a mano.

   El 07/08 el usuario SAP "cacre" devolvió con la ORDN 187 (1,864.09) las
   131 líneas de 70 ODLN DP de C3117 de ese día y las volvió a entregar en
   la ODLN 6253 "Based on Return 187" (mismos artículos, cantidades, precios
   y bodega 1180, impuesto 159.34; marcada Duty Free).

   la_store_sales sigue apuntando a las 70 ODLN originales, cuyas líneas
   están cerradas por la ORDN 187: SCI solo veía 743.86 (líneas abiertas
   de las otras 26 ODLN + impuesto de todas) contra 2,448.61 de pagos.

   Este script apunta esas 131 ventas a la ODLN 6253, que es la que tiene
   las líneas abiertas para facturar. Con eso el grupo DP 07/08 queda:
   534.65 + 1,704.75 + impuesto 49.87 + 159.34 = 2,448.61 = pagos POS.

   No toca SAP. Respaldo en dbo.la_store_sales_bak_c3117_0807.

   USO: ejecutar en SMM_PROD. Primero @Commit = 0 (simulación), luego 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar

DECLARE @NewEntry int = 6253, @NewNum int = 6253;

-- Validaciones: la ODLN 6253 sigue abierta y con 131 líneas abiertas; la ORDN 187 cerró 131 líneas
IF NOT EXISTS (SELECT 1 FROM PRO_DFSC.dbo.ODLN WHERE DocEntry = @NewEntry AND DocNum = @NewNum
               AND CardCode = 'C3117' AND DocStatus = 'O' AND CANCELED = 'N')
BEGIN RAISERROR('La ODLN 6253 no está abierta: revisar antes de continuar.', 16, 1); RETURN; END

IF (SELECT COUNT(*) FROM PRO_DFSC.dbo.DLN1 WHERE DocEntry = @NewEntry AND LineStatus = 'O') <> 131
BEGIN RAISERROR('La ODLN 6253 no tiene 131 líneas abiertas: revisar antes de continuar.', 16, 1); RETURN; END

IF OBJECT_ID('tempdb..#orig') IS NOT NULL DROP TABLE #orig;
SELECT DISTINCT DocEntry INTO #orig FROM PRO_DFSC.dbo.DLN1 WHERE TargetType = 16 AND TrgetEntry = 187;

IF OBJECT_ID('tempdb..#ventas') IS NOT NULL DROP TABLE #ventas;
SELECT s.ID, s.TransactionID, s.DeliveryDocNum, s.DeliveryDocEntry, s.Extsellprice
INTO #ventas
FROM dbo.la_store_sales s
INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = s.NUMSERIE AND ts.NUMSTORE = CAST(s.storenum AS INT) AND ts.DUTYTYPE = s.TRANSTYPE
INNER JOIN #orig o ON o.DocEntry = s.DeliveryDocEntry
WHERE ts.CARCODE = 'C3117' AND s.TRANSTYPE = 'DP' AND s.txnmodifier = 0
  AND CAST(s.itemdatetime AS DATE) = '2026-08-07';

SELECT COUNT(*) AS ventas, COUNT(DISTINCT TransactionID) AS transacciones,
       COUNT(DISTINCT DeliveryDocEntry) AS odln_originales, SUM(Extsellprice) AS neto
FROM #ventas;

IF (SELECT COUNT(*) FROM #ventas) <> 131 OR (SELECT SUM(Extsellprice) FROM #ventas) <> 1704.75
BEGIN RAISERROR('Se esperaban 131 ventas por 1,704.75: revisar antes de continuar.', 16, 1); RETURN; END

BEGIN TRAN;

IF OBJECT_ID('dbo.la_store_sales_bak_c3117_0807') IS NULL
    SELECT s.*, GETDATE() AS bak_date INTO dbo.la_store_sales_bak_c3117_0807
    FROM dbo.la_store_sales s INNER JOIN #ventas v ON v.ID = s.ID;

UPDATE s SET s.DeliveryDocEntry = @NewEntry, s.DeliveryDocNum = @NewNum
FROM dbo.la_store_sales s INNER JOIN #ventas v ON v.ID = s.ID;
PRINT 'la_store_sales: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) apuntadas a la ODLN 6253';

-- Resultado: entregas del grupo DP 07/08 y su total en SAP (líneas abiertas + impuesto)
;WITH docs AS (
    SELECT DISTINCT s.DeliveryDocEntry
    FROM dbo.la_store_sales s
    INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = s.NUMSERIE AND ts.NUMSTORE = CAST(s.storenum AS INT) AND ts.DUTYTYPE = s.TRANSTYPE
    WHERE ts.CARCODE = 'C3117' AND s.TRANSTYPE = 'DP' AND s.txnmodifier = 0
      AND CAST(s.itemdatetime AS DATE) = '2026-08-07' AND s.DeliveryDocNum > 0)
SELECT (SELECT SUM(l.LineTotal) FROM PRO_DFSC.dbo.DLN1 l INNER JOIN docs d ON d.DeliveryDocEntry = l.DocEntry WHERE l.LineStatus = 'O') AS lineas_abiertas,
       (SELECT SUM(e.LineTotal) FROM PRO_DFSC.dbo.DLN3 e INNER JOIN docs d ON d.DeliveryDocEntry = e.DocEntry) AS impuesto,
       (SELECT COUNT(*) FROM docs) AS entregas;

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
