/* =====================================================================
   C3105 (SFO DF Burberry) caja 307 — mapeo DP.

   La caja 307 solo tenía fila DF en ADR_TIENDA_SERIE. El 30/09/2026 vendió
   un artículo Duty Paid con impuesto (TxnID 614484, transnum 31053071619,
   18455922, 199.00 + 18.66, VISA 217.66). La integración la cargó por el
   fallback (fila DF) y creó la ODLN 215833 a nombre de C3105, pero SAP
   Invoicing cruza por NUMSERIE + NUMSTORE + DUTYTYPE y no la veía.

   Decisión del usuario (2026-11-11): facturarla a nombre de C3105.

   Este script:
   1. Agrega la fila DP de la caja 307 copiando la DF (C3105, bodega 1783,
      CompanyId 4). Aparece el grupo "C3105 DP 30/09" (217.66).
      Efecto en la integración: las transacciones DP de esa caja dejarán de
      llevar artículos DF (regla "txn DP no puede llevar ítems DF"), que es
      lo correcto: SAP rechaza documentos DP con artículos DF.
   2. Marca con DeliveryDocNum = 99999999 la prueba de 0.25 del 06/08 de
      esa caja (TxnID 906 venta / 910 devolución; ODLN 9485 y ORDN 225 ya
      cerradas en SAP el 2026-11-11). Sin esto aparecerían como grupo
      "C3105 DP 06/08" bloqueado. DeliveryDocEntry no se toca.

   Revertir: DELETE de la fila insertada y restaurar DeliveryDocNum desde
   dbo.la_store_sales_bak_c3105_307.

   USO: ejecutar en SMM_PROD. Primero @Commit = 0 (simulación), luego 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar

-- Fila actual de la caja
SELECT WHSCODE, CARCODE, NUMSERIE, NUMSTORE, CompanyId, DUTYTYPE
FROM dbo.ADR_TIENDA_SERIE WHERE NUMSTORE = 3105 AND NUMSERIE = 307;

-- Ventas DP de la caja (hoy sin mapeo)
SELECT s.ID, CAST(s.itemdatetime AS DATE) AS Fecha, s.transnum, s.TransactionID, s.txnmodifier,
       s.skunum, s.qty, s.Extsellprice, s.DeliveryDocNum, s.DeliveryDocEntry
FROM dbo.la_store_sales s
WHERE CAST(s.storenum AS INT) = 3105 AND s.NUMSERIE = 307 AND s.TRANSTYPE = 'DP'
ORDER BY s.itemdatetime;

BEGIN TRAN;

-- 1. Fila DP copiando la DF
INSERT INTO dbo.ADR_TIENDA_SERIE (WHSCODE, CARCODE, NUMSERIE, NUMSTORE, CompanyId, DUTYTYPE)
SELECT o.WHSCODE, o.CARCODE, o.NUMSERIE, o.NUMSTORE, o.CompanyId, 'DP'
FROM dbo.ADR_TIENDA_SERIE o
WHERE o.NUMSTORE = 3105 AND o.NUMSERIE = 307 AND o.DUTYTYPE = 'DF' AND o.CARCODE = 'C3105'
  AND NOT EXISTS (SELECT 1 FROM dbo.ADR_TIENDA_SERIE x WHERE x.NUMSTORE = 3105 AND x.NUMSERIE = 307 AND x.DUTYTYPE = 'DP');
PRINT 'ADR_TIENDA_SERIE: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) insertadas';

-- 2. Prueba de 0.25 del 06/08 → 99999999
IF OBJECT_ID('dbo.la_store_sales_bak_c3105_307') IS NULL
    SELECT s.*, GETDATE() AS bak_date INTO dbo.la_store_sales_bak_c3105_307
    FROM dbo.la_store_sales s
    WHERE CAST(s.storenum AS INT) = 3105 AND s.NUMSERIE = 307 AND s.TRANSTYPE = 'DP' AND s.TransactionID IN (906, 910);

UPDATE dbo.la_store_sales SET DeliveryDocNum = 99999999
WHERE CAST(storenum AS INT) = 3105 AND NUMSERIE = 307 AND TRANSTYPE = 'DP' AND TransactionID IN (906, 910)
  AND DeliveryDocNum IN (9485, 225);
PRINT 'la_store_sales: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) de prueba marcadas 99999999';

-- Resultado
SELECT WHSCODE, CARCODE, NUMSERIE, NUMSTORE, CompanyId, DUTYTYPE
FROM dbo.ADR_TIENDA_SERIE WHERE NUMSTORE = 3105 AND NUMSERIE = 307;

SELECT ts.CARCODE, s.TRANSTYPE, CAST(s.itemdatetime AS DATE) AS Fecha, COUNT(*) AS Lineas,
       SUM(s.Extsellprice) AS Neto, MIN(s.DeliveryDocNum) AS Entrega
FROM dbo.la_store_sales s
INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = s.NUMSERIE AND ts.NUMSTORE = CAST(s.storenum AS INT) AND ts.DUTYTYPE = s.TRANSTYPE
WHERE CAST(s.storenum AS INT) = 3105 AND s.NUMSERIE = 307 AND s.TRANSTYPE = 'DP'
  AND s.DeliveryDocNum NOT IN (0, 99999999)
GROUP BY ts.CARCODE, s.TRANSTYPE, CAST(s.itemdatetime AS DATE);

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
