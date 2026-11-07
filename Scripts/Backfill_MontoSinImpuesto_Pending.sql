/* =====================================================================
   Completa la_store_sales.MontoSinImpuesto desde el POS en las ventas que
   TODAVÍA NO tienen entrega (DeliveryDocNum NULL o -1).

   ExtraeCreaDocSalesSAP no guardaba MontoSinImpuesto (corregido 2026-11-01);
   sin ese dato, al crear la ODLN/ORDN de una transacción mixta el impuesto se
   repite completo en los dos documentos. Las ventas ya entregadas NO se tocan
   (sus documentos ya existen y el preview/uploader las usa tal como están;
   los impuestos repetidos se manejan con Scripts/Exclude_Duplicated_Mixed_Tax.sql).

   USO: ejecutar en SMM_PROD. Primero con @Commit = 0 (simulación),
        luego con @Commit = 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar

IF OBJECT_ID('tempdb..#p') IS NOT NULL DROP TABLE #p;
IF OBJECT_ID('tempdb..#v') IS NOT NULL DROP TABLE #v;

SELECT ID, TransactionID, itemnum
INTO #p
FROM dbo.la_store_sales
WHERE (DeliveryDocNum IS NULL OR DeliveryDocNum = -1)
  AND MontoSinImpuesto IS NULL AND TransactionID IS NOT NULL;

SELECT DISTINCT v.TransactionID, v.SaleLineNumber, v.MontoSinImpuesto
INTO #v
FROM DFSPOS.POS.dbo.vw_POS_VentaItems v
WHERE v.TransactionID IN (SELECT DISTINCT TransactionID FROM #p);

SELECT COUNT(*) AS VentasPendientesSinDato,
       SUM(CASE WHEN v.TransactionID IS NOT NULL THEN 1 ELSE 0 END) AS ConDatoEnPOS,
       COUNT(DISTINCT p.TransactionID) AS Transacciones
FROM #p p LEFT JOIN #v v ON v.TransactionID = p.TransactionID AND v.SaleLineNumber = p.itemnum;

BEGIN TRAN;

UPDATE s SET s.MontoSinImpuesto = v.MontoSinImpuesto
FROM dbo.la_store_sales s
INNER JOIN #p p ON p.ID = s.ID
INNER JOIN #v v ON v.TransactionID = p.TransactionID AND v.SaleLineNumber = p.itemnum
WHERE (s.DeliveryDocNum IS NULL OR s.DeliveryDocNum = -1) AND s.MontoSinImpuesto IS NULL;
PRINT 'la_store_sales: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) con MontoSinImpuesto completado';

IF @Commit = 1
BEGIN
    COMMIT;
    PRINT 'COMMIT: MontoSinImpuesto completado.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT 'SIMULACION: se hizo ROLLBACK. Cambiar @Commit = 1 para confirmar.';
END
