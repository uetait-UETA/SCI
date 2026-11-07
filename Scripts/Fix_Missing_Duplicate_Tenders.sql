/* =====================================================================
   Agrega a la_store_payments los pagos que faltan porque el POS tenía dos
   o más pagos IDÉNTICOS en la misma transacción (misma forma de pago, monto,
   tipo de línea) y la carga los juntó en uno.

   Causa: CreaFacturasVentasSAP (ExtractPagosEnMemoriaAsync) hace
   SELECT DISTINCT sobre POS_TransactionTender sin TenderLineID. Mismo bug
   que se corrigió en ADA el 2026-10-08.
   Ej.: TxnID 19538 (C3001 DF 07/08) tiene VISA 10.00 dos veces en el POS
   (TenderLineID 7530 y 7531) y en la_store_payments solo una → faltan 10.00.

   Solo toca transacciones que YA tienen pagos cargados; agrega las filas
   que faltan (POS - SCI) por combinación TxnID + TenderID + Amount +
   LineType + IsChange, copiando CompanyId / transnum de la fila existente.

   USO: ejecutar en SMM_PROD. Ajustar @Desde/@Hasta (BusinessDayDate).
        Primero con @Commit = 0 (simulación), luego con @Commit = 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit  = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar
DECLARE @Desde  date = '2026-08-06';
DECLARE @Hasta  date = '2026-08-15';

IF OBJECT_ID('tempdb..#pos') IS NOT NULL DROP TABLE #pos;
IF OBJECT_ID('tempdb..#add') IS NOT NULL DROP TABLE #add;

SELECT t.TenderLineID, t.TransactionID,
       CAST(t.TenderID   AS nvarchar(50)) COLLATE DATABASE_DEFAULT AS TenderID,
       t.Amount,
       CAST(t.LineType   AS nvarchar(50)) COLLATE DATABASE_DEFAULT AS LineType,
       CAST(ISNULL(t.IsChange, 0) AS bit) AS IsChange,
       CAST(t.CurrencyID AS nvarchar(10)) COLLATE DATABASE_DEFAULT AS CurrencyID,
       CAST(t.SiteID     AS nvarchar(20)) COLLATE DATABASE_DEFAULT AS SiteID,
       CAST(t.CashDrawerID AS nvarchar(20)) COLLATE DATABASE_DEFAULT AS CashDrawerID,
       CAST(t.BusinessDayDate AS date) AS BusinessDayDate,
       ROW_NUMBER() OVER (PARTITION BY t.TransactionID, t.TenderID, t.Amount, t.LineType, ISNULL(t.IsChange, 0)
                          ORDER BY t.TenderLineID) AS rn
INTO #pos
FROM DFSPOS.POS.dbo.POS_TransactionTender t
WHERE t.BusinessDayDate >= @Desde AND t.BusinessDayDate < DATEADD(DAY, 1, @Hasta);

-- Filas del POS por encima de las que ya hay en SCI para esa combinación
SELECT p.*, e.CompanyId, e.transnum
INTO #add
FROM #pos p
CROSS APPLY (SELECT COUNT(*) AS n FROM dbo.la_store_payments s
             WHERE s.TransactionID = p.TransactionID AND s.TenderID COLLATE DATABASE_DEFAULT = p.TenderID
               AND s.Amount = p.Amount AND s.LineType COLLATE DATABASE_DEFAULT = p.LineType
               AND s.IsChange = p.IsChange) c
CROSS APPLY (SELECT TOP 1 s.CompanyId, s.transnum FROM dbo.la_store_payments s
             WHERE s.TransactionID = p.TransactionID
               AND EXISTS (SELECT 1 FROM dbo.la_store_sales ss WITH (NOLOCK)
                           WHERE ss.TransactionID = s.TransactionID AND ss.transnum = s.transnum)
             ORDER BY s.ID) e
WHERE p.rn > c.n;

SELECT COUNT(DISTINCT TransactionID) AS Transacciones, COUNT(*) AS FilasAAgregar,
       SUM(CASE WHEN IsChange = 1 THEN -Amount ELSE Amount END) AS MontoNeto
FROM #add;
SELECT TransactionID, transnum, TenderID, Amount, LineType, IsChange, BusinessDayDate, TenderLineID FROM #add ORDER BY TransactionID;

BEGIN TRAN;

INSERT INTO dbo.la_store_payments
    (CompanyId, transnum, TransactionID, TenderID, Amount, CurrencyID, LineType, IsChange, SiteID, CashDrawerID, BusinessDayDate, date_created)
SELECT CompanyId, transnum, TransactionID, TenderID, Amount, CurrencyID, LineType, IsChange, SiteID, CashDrawerID, BusinessDayDate, GETDATE()
FROM #add;
PRINT 'la_store_payments: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) agregadas';

IF @Commit = 1
BEGIN
    COMMIT;
    PRINT 'COMMIT: pagos agregados.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT 'SIMULACION: se hizo ROLLBACK. Cambiar @Commit = 1 para confirmar.';
END
