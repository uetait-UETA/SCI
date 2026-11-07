/* =====================================================================
   Transacciones mixtas DP (venta + devolución en el mismo ticket) cuyo
   impuesto quedó COMPLETO en la ODLN y también en la ORDN.

   Causa: versiones viejas de ExtraeCreaDocSalesSAP no guardaban
   MontoSinImpuesto; sin ese dato, en una transacción mixta el impuesto
   (SalesTaxAmount, que es de toda la transacción) se ponía como gasto
   adicional (DLN3/RDN3) en los dos documentos.
   Ej.: TxnID 528100 (C3012 DP 23/09): devolución 126 + venta 146, pagó 21.95;
   el impuesto 1.95 es de la venta, pero la ORDN 5514 también lo tiene →
   la nota de crédito del día acreditaría 1.95 de más (diferencia en SCI).

   El impuesto va solo en el lado NETO de la transacción:
     venta >= devolución → sobra en la ORDN (BaseType 16)
     devolución > venta  → sobra en la ODLN (BaseType 15)
   Se registra en dbo.la_invoice_excluded_lines con LineNum = -1 (= gastos
   adicionales del documento): FacturacionSAP no los copia a la OINV/ORIN.

   Solo toma documentos de UNA sola transacción, con el gasto igual al
   impuesto, y que todavía no se facturaron.

   USO: ejecutar en SMM_PROD. Primero con @Commit = 0 (simulación),
        luego con @Commit = 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar

IF OBJECT_ID('tempdb..#s') IS NOT NULL DROP TABLE #s;
IF OBJECT_ID('tempdb..#x') IS NOT NULL DROP TABLE #x;

SELECT ss.TransactionID, ss.txnmodifier, ss.Extsellprice, ss.SalesTaxAmount, ss.MontoSinImpuesto, ss.DeliveryDocEntry,
       CAST(ss.itemdatetime AS DATE) AS Dia
INTO #s
FROM dbo.la_store_sales ss WITH (NOLOCK)
WHERE ss.DeliveryDocNum > 0 AND ss.DeliveryDocEntry IS NOT NULL
  AND ss.TransactionID IN (SELECT TransactionID FROM dbo.la_store_sales WITH (NOLOCK)
                           WHERE DeliveryDocNum > 0 AND TransactionID IS NOT NULL
                           GROUP BY TransactionID HAVING COUNT(DISTINCT txnmodifier) = 2);

SELECT t.TransactionID, t.Dia, t.Venta, t.Dev, t.Tax, d.Odln, d.Ordn,
       (SELECT SUM(e.LineTotal) FROM PRO_DFSC.dbo.DLN3 e WHERE e.DocEntry = d.Odln) AS TaxOdln,
       (SELECT SUM(e.LineTotal) FROM PRO_DFSC.dbo.RDN3 e WHERE e.DocEntry = d.Ordn) AS TaxOrdn,
       CASE WHEN t.Venta >= t.Dev THEN 16 ELSE 15 END AS BaseType,
       CASE WHEN t.Venta >= t.Dev THEN d.Ordn ELSE d.Odln END AS DocEntry
INTO #x
FROM (SELECT TransactionID, MIN(Dia) AS Dia,
             SUM(CASE WHEN txnmodifier = 0 THEN Extsellprice ELSE 0 END) AS Venta,
             SUM(CASE WHEN txnmodifier = 1 THEN -Extsellprice ELSE 0 END) AS Dev,
             MAX(ABS(SalesTaxAmount)) AS Tax,
             SUM(CASE WHEN MontoSinImpuesto IS NULL THEN 1 ELSE 0 END) AS SinImp
      FROM #s GROUP BY TransactionID) t
CROSS APPLY (SELECT MIN(CASE WHEN txnmodifier = 0 THEN DeliveryDocEntry END) AS Odln,
                    MIN(CASE WHEN txnmodifier = 1 THEN DeliveryDocEntry END) AS Ordn
             FROM #s WHERE TransactionID = t.TransactionID) d
WHERE t.SinImp > 0;

-- Solo: impuesto completo en los dos documentos, cada documento de una sola transacción, sin facturar
DELETE x FROM #x x
WHERE NOT (ABS(ISNULL(x.TaxOdln, 0) - x.Tax) < 0.01 AND ABS(ISNULL(x.TaxOrdn, 0) - x.Tax) < 0.01 AND x.Tax <> 0)
   OR EXISTS (SELECT 1 FROM dbo.la_store_sales o WITH (NOLOCK)
              WHERE o.DeliveryDocEntry = x.DocEntry AND o.txnmodifier = CASE WHEN x.BaseType = 15 THEN 0 ELSE 1 END
                AND o.TransactionID <> x.TransactionID)
   OR (x.BaseType = 15 AND EXISTS (SELECT 1 FROM PRO_DFSC.dbo.INV1 i WHERE i.BaseType = 15 AND i.BaseEntry = x.DocEntry))
   OR (x.BaseType = 16 AND EXISTS (SELECT 1 FROM PRO_DFSC.dbo.RIN1 i WHERE i.BaseType = 16 AND i.BaseEntry = x.DocEntry));

SELECT x.TransactionID, x.Dia, x.Venta, x.Dev, x.Tax, x.TaxOdln, x.TaxOrdn,
       CASE x.BaseType WHEN 15 THEN 'ODLN' ELSE 'ORDN' END AS SeExcluyeImpuestoDe, x.DocEntry,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.la_invoice_excluded_lines l WHERE l.BaseType = x.BaseType AND l.DocEntry = x.DocEntry AND l.LineNum = -1)
            THEN 'ya excluido' ELSE 'se excluye' END AS Accion
FROM #x x ORDER BY x.Dia;

BEGIN TRAN;

INSERT dbo.la_invoice_excluded_lines (BaseType, DocEntry, LineNum, Reason)
SELECT x.BaseType, x.DocEntry, -1,
       'TxnID ' + CAST(x.TransactionID AS varchar(20)) + ': tax ' + CAST(x.Tax AS varchar(20))
       + ' repeated in ODLN and ORDN of a mixed transaction (old integration); belongs to the '
       + CASE WHEN x.BaseType = 16 THEN 'sale' ELSE 'return' END + ' side'
FROM #x x
WHERE NOT EXISTS (SELECT 1 FROM dbo.la_invoice_excluded_lines l WHERE l.BaseType = x.BaseType AND l.DocEntry = x.DocEntry AND l.LineNum = -1);
PRINT 'la_invoice_excluded_lines: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' impuesto(s) excluido(s)';

IF @Commit = 1
BEGIN
    COMMIT;
    PRINT 'COMMIT: impuestos repetidos excluidos.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT 'SIMULACION: se hizo ROLLBACK. Cambiar @Commit = 1 para confirmar.';
END

/* Para revertir:
DELETE FROM dbo.la_invoice_excluded_lines WHERE LineNum = -1 AND Reason LIKE 'TxnID %: tax % repeated in ODLN and ORDN%';
*/
