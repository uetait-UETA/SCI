/* =====================================================================
   Corrige las transacciones mixtas viejas cuya VENTA quedó dentro de una
   ORDN (como si fuera devolución) — 14 casos de ago/sep-2026, ej. TxnID
   72942 / ORDN 372, C3001 12/08: devolución 36 + venta 50, ORDN por 86.

   La ORDN no se puede cancelar (el artículo devuelto ya no tiene stock) ni
   SAP permite cerrar una línea de una devolución. Por eso:
     1. Las filas de VENTA de la transacción quedan pendientes
        (DeliveryDocNum = NULL): ExtraeCreaDocSalesSAP crea una ODLN normal,
        que saca del almacén las unidades que la ORDN metió por error.
     2. La línea de la ORDN que corresponde a esa venta (mismo artículo,
        cantidad y monto) se registra en dbo.la_invoice_excluded_lines:
        SCI (FacturacionSAP) no la copia a la nota de crédito.
     3. Después de subir la nota de crédito del día, cerrar la ORDN en SAP
        (Close: sin movimiento de inventario).

   Solo toca transacciones cuya ORDN está abierta y sin nota de crédito.
   Respaldo en dbo.la_store_sales_bak_mixed_ordn.

   USO: ejecutar en SMM_PROD. Poner los TransactionID en @txns.
        Primero con @Commit = 0 (simulación), luego con @Commit = 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar

DECLARE @txns TABLE (TransactionID bigint PRIMARY KEY);
INSERT @txns VALUES (72942);

IF OBJECT_ID('tempdb..#sales') IS NOT NULL DROP TABLE #sales;
IF OBJECT_ID('tempdb..#lines') IS NOT NULL DROP TABLE #lines;

IF OBJECT_ID('dbo.la_invoice_excluded_lines') IS NULL
    CREATE TABLE dbo.la_invoice_excluded_lines (
        BaseType     int           NOT NULL,     -- 15 = ODLN, 16 = ORDN
        DocEntry     int           NOT NULL,
        LineNum      int           NOT NULL,
        Reason       nvarchar(200) NULL,
        date_created datetime      NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_la_invoice_excluded_lines PRIMARY KEY (BaseType, DocEntry, LineNum)
    );

-- Filas de VENTA metidas en la ORDN (venta y devolución de la misma transacción con el mismo DocEntry)
SELECT s.ID, s.TransactionID, s.itemnum, s.skunum, s.qty, s.Extsellprice, s.DeliveryDocNum, s.DeliveryDocEntry,
       ROW_NUMBER() OVER (PARTITION BY s.DeliveryDocEntry, s.skunum, ABS(s.qty), CAST(ABS(s.Extsellprice) AS decimal(19,2))
                          ORDER BY s.itemnum) AS rn
INTO #sales
FROM dbo.la_store_sales s
INNER JOIN @txns t ON t.TransactionID = s.TransactionID
WHERE s.txnmodifier = 0 AND s.DeliveryDocNum > 0
  AND EXISTS (SELECT 1 FROM dbo.la_store_sales x
              WHERE x.TransactionID = s.TransactionID AND x.txnmodifier = 1
                AND x.DeliveryDocEntry = s.DeliveryDocEntry)
  AND EXISTS (SELECT 1 FROM PRO_DFSC.dbo.ORDN r
              WHERE r.DocEntry = s.DeliveryDocEntry AND r.DocStatus = 'O' AND r.CANCELED = 'N')
  AND NOT EXISTS (SELECT 1 FROM PRO_DFSC.dbo.RIN1 i WHERE i.BaseType = 16 AND i.BaseEntry = s.DeliveryDocEntry);

-- Línea de la ORDN de cada venta: mismo artículo, cantidad y monto (en orden). El monto entra en
-- la clave porque la venta y la devolución pueden ser del mismo artículo y cantidad (ej. TxnID 318898).
SELECT s.ID, s.TransactionID, s.DeliveryDocEntry, l.LineNum, l.ItemCode, l.Quantity, l.LineTotal, s.Extsellprice,
       CASE WHEN l.LineNum IS NOT NULL THEN 1 ELSE 0 END AS Ok
INTO #lines
FROM #sales s
LEFT JOIN (SELECT l.DocEntry, l.LineNum, l.ItemCode, l.Quantity, l.LineTotal,
                  ROW_NUMBER() OVER (PARTITION BY l.DocEntry, l.ItemCode, l.Quantity, CAST(l.LineTotal AS decimal(19,2))
                                     ORDER BY l.LineNum) AS rn
           FROM PRO_DFSC.dbo.RDN1 l
           WHERE l.DocEntry IN (SELECT DeliveryDocEntry FROM #sales)) l
    ON l.DocEntry = s.DeliveryDocEntry AND l.ItemCode COLLATE DATABASE_DEFAULT = s.skunum COLLATE DATABASE_DEFAULT
   AND l.Quantity = ABS(s.qty) AND l.LineTotal = CAST(ABS(s.Extsellprice) AS decimal(19,2)) AND l.rn = s.rn;

-- Una transacción se corrige solo si TODAS sus ventas tienen su línea en la ORDN
SELECT l.*, CASE WHEN EXISTS (SELECT 1 FROM #lines z WHERE z.TransactionID = l.TransactionID AND z.Ok = 0)
                 THEN 'NO: alguna venta no tiene su linea en la ORDN' ELSE 'se corrige' END AS Accion
FROM #lines l ORDER BY l.TransactionID, l.LineNum;

DELETE l FROM #lines l WHERE EXISTS (SELECT 1 FROM #lines z WHERE z.TransactionID = l.TransactionID AND z.Ok = 0);

BEGIN TRAN;

IF OBJECT_ID('dbo.la_store_sales_bak_mixed_ordn') IS NULL
    CREATE TABLE dbo.la_store_sales_bak_mixed_ordn (
        ID               int      NOT NULL,
        TransactionID    bigint   NULL,
        DeliveryDocNum   int      NULL,
        DeliveryDocEntry bigint   NULL,
        date_reset       datetime NOT NULL DEFAULT GETDATE()
    );

INSERT dbo.la_store_sales_bak_mixed_ordn (ID, TransactionID, DeliveryDocNum, DeliveryDocEntry)
SELECT s.ID, s.TransactionID, s.DeliveryDocNum, s.DeliveryDocEntry
FROM dbo.la_store_sales s INNER JOIN #lines l ON l.ID = s.ID;

INSERT dbo.la_invoice_excluded_lines (BaseType, DocEntry, LineNum, Reason)
SELECT 16, l.DeliveryDocEntry, l.LineNum,
       'TxnID ' + CAST(l.TransactionID AS varchar(20)) + ': sale line inside ORDN (old integration); sale re-delivered with a new ODLN'
FROM #lines l
WHERE NOT EXISTS (SELECT 1 FROM dbo.la_invoice_excluded_lines x WHERE x.BaseType = 16 AND x.DocEntry = l.DeliveryDocEntry AND x.LineNum = l.LineNum);
PRINT 'la_invoice_excluded_lines: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' linea(s) de ORDN excluidas';

UPDATE s SET s.DeliveryDocNum = NULL, s.DeliveryDocEntry = NULL
FROM dbo.la_store_sales s INNER JOIN #lines l ON l.ID = s.ID;
PRINT 'la_store_sales: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) de venta quedan pendientes';

IF @Commit = 1
BEGIN
    COMMIT;
    PRINT 'COMMIT: la integracion creara la ODLN en su proxima corrida.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT 'SIMULACION: se hizo ROLLBACK. Cambiar @Commit = 1 para confirmar.';
END

/* Para revertir (solo si la integración todavía no creó la ODLN):
UPDATE s SET s.DeliveryDocNum = b.DeliveryDocNum, s.DeliveryDocEntry = b.DeliveryDocEntry
FROM dbo.la_store_sales s INNER JOIN dbo.la_store_sales_bak_mixed_ordn b ON b.ID = s.ID;
DELETE FROM dbo.la_invoice_excluded_lines WHERE Reason LIKE 'TxnID %: sale line inside ORDN%';
*/
