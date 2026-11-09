/* =====================================================================
   ADR_TIENDA_SERIE — cajas mapeadas a un solo tipo (DF o DP) que tienen
   ventas del otro tipo.

   Ej.: C3012 cajas 202/203/205/207 (solo DP) venden "Export" a pasajeros
   internacionales; el POS las marca DF (sin impuesto). La integración las
   carga por el fallback de SyncService (usa la fila del tipo contrario:
   misma tienda y bodega) y crea la ODLN Duty Free, pero SAP Invoicing
   cruza la_store_sales con ADR_TIENDA_SERIE por NUMSERIE + NUMSTORE +
   DUTYTYPE y no las ve: ~300,000 en ventas sin facturar desde el 06/08.

   Este script copia la fila existente de esa caja con el otro DUTYTYPE
   (mismo CARCODE, WHSCODE y CompanyId). La integración sigue igual: hoy
   ya usa esa misma fila por el fallback. SAP Invoicing muestra esas
   ventas como un grupo aparte (ej. C3012 DF).

   Solo cajas DFS (CompanyId 3 LAX / 4 SFO) con ventas cargadas desde el
   06/08/2026 (con TransactionID y entrega creada).

   Revertir: DELETE de las filas listadas (respaldo en
   dbo.ADR_TIENDA_SERIE_bak_20261109).

   USO: ejecutar en SMM_PROD. Primero @Commit = 0 (simulación), luego 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar

IF OBJECT_ID('tempdb..#faltan') IS NOT NULL DROP TABLE #faltan;

-- Caja + tienda + tipo con ventas cargadas que no tienen fila para ese tipo
SELECT s.NUMSERIE, CAST(s.storenum AS INT) AS NUMSTORE, s.TRANSTYPE AS DUTYTYPE,
       COUNT(DISTINCT s.TransactionID) AS Txns, MIN(CAST(s.itemdatetime AS DATE)) AS Desde, MAX(CAST(s.itemdatetime AS DATE)) AS Hasta
INTO #faltan
FROM dbo.la_store_sales s
WHERE CAST(s.itemdatetime AS DATE) >= '2026-08-06'
  -- Solo DF (ventas Export en tiendas DP). NO DP en tiendas DF (C3105/C3109): con fila DP la
  -- integración aplicaría "txn DP no puede llevar ítems DF" y excluiría ítems que hoy, por el
  -- fallback, sí carga.
  AND s.TransactionID IS NOT NULL AND s.DeliveryDocNum > 0 AND s.TRANSTYPE = 'DF'
  AND NOT EXISTS (SELECT 1 FROM dbo.ADR_TIENDA_SERIE t
                  WHERE t.NUMSERIE = s.NUMSERIE AND t.NUMSTORE = CAST(s.storenum AS INT) AND t.DUTYTYPE = s.TRANSTYPE)
  AND EXISTS (SELECT 1 FROM dbo.ADR_TIENDA_SERIE t
              WHERE t.NUMSERIE = s.NUMSERIE AND t.NUMSTORE = CAST(s.storenum AS INT)
                AND t.DUTYTYPE <> s.TRANSTYPE AND CAST(t.CompanyId AS INT) IN (3, 4))
GROUP BY s.NUMSERIE, CAST(s.storenum AS INT), s.TRANSTYPE;

-- Filas a insertar: copia de la fila del tipo contrario
SELECT o.WHSCODE, o.CARCODE, o.NUMSERIE, o.NUMSTORE, o.CompanyId, f.DUTYTYPE,
       f.Txns, f.Desde, f.Hasta, o.DUTYTYPE AS CopiaDe
FROM #faltan f
INNER JOIN dbo.ADR_TIENDA_SERIE o ON o.NUMSERIE = f.NUMSERIE AND o.NUMSTORE = f.NUMSTORE AND o.DUTYTYPE <> f.DUTYTYPE
ORDER BY o.CARCODE, o.NUMSERIE;

BEGIN TRAN;

IF OBJECT_ID('dbo.ADR_TIENDA_SERIE_bak_20261109') IS NULL
    SELECT *, GETDATE() AS bak_date INTO dbo.ADR_TIENDA_SERIE_bak_20261109 FROM dbo.ADR_TIENDA_SERIE;

INSERT INTO dbo.ADR_TIENDA_SERIE (WHSCODE, CARCODE, NUMSERIE, NUMSTORE, CompanyId, DUTYTYPE)
SELECT o.WHSCODE, o.CARCODE, o.NUMSERIE, o.NUMSTORE, o.CompanyId, f.DUTYTYPE
FROM #faltan f
INNER JOIN dbo.ADR_TIENDA_SERIE o ON o.NUMSERIE = f.NUMSERIE AND o.NUMSTORE = f.NUMSTORE AND o.DUTYTYPE <> f.DUTYTYPE
WHERE NOT EXISTS (SELECT 1 FROM dbo.ADR_TIENDA_SERIE x
                  WHERE x.NUMSERIE = o.NUMSERIE AND x.NUMSTORE = o.NUMSTORE AND x.DUTYTYPE = f.DUTYTYPE AND x.WHSCODE = o.WHSCODE);
PRINT 'ADR_TIENDA_SERIE: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) insertadas';

-- Ventas que ahora cruzan con ADR_TIENDA_SERIE (antes invisibles para SAP Invoicing)
SELECT ts.CARCODE, s.TRANSTYPE, COUNT(DISTINCT s.TransactionID) AS Txns,
       SUM(CASE WHEN s.txnmodifier = 0 THEN s.Extsellprice ELSE 0 END) AS Ventas,
       SUM(CASE WHEN s.txnmodifier = 1 THEN s.Extsellprice ELSE 0 END) AS Devoluciones
FROM dbo.la_store_sales s
INNER JOIN #faltan f ON f.NUMSERIE = s.NUMSERIE AND f.NUMSTORE = CAST(s.storenum AS INT) AND f.DUTYTYPE = s.TRANSTYPE
INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = s.NUMSERIE AND ts.NUMSTORE = CAST(s.storenum AS INT) AND ts.DUTYTYPE = s.TRANSTYPE
WHERE CAST(s.itemdatetime AS DATE) >= '2026-08-06' AND s.TransactionID IS NOT NULL AND s.DeliveryDocNum > 0
GROUP BY ts.CARCODE, s.TRANSTYPE
ORDER BY ts.CARCODE;

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
