/* =====================================================================
   ADR_TENDER_SAP: DFA VIP Points-92 (pendiente de contabilidad desde el 20/10/2026).

     DFA VIP Points-92  → tarjeta 10 LOYALTY (6885-00-03 Loyalty Points)

   Contabilidad creó la tarjeta 10 en PRO_DFSC el 2026-11-02 (misma configuración
   que la 9 VENMO). Los días que la usan estaban Blocked con
   "Payment DFA VIP Points-92 not in ADR_TENDER_SAP".
   No duplica: solo inserta si el TenderID no existe y la tarjeta existe en SAP.

   USO: ejecutar en SMM_PROD. Primero con @Commit = 0 (simulación),
        luego con @Commit = 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar

DECLARE @new TABLE (TenderID nvarchar(100), SapAccount nvarchar(20), PaymentType nvarchar(20), Description nvarchar(100));
INSERT @new VALUES ('DFA VIP Points-92', '10', 'CreditCard', 'DFA VIP Points (Loyalty)');

SELECT n.*, c.CardName, c.AcctCode,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.ADR_TENDER_SAP t WHERE t.TenderID = n.TenderID) THEN 'ya existe' ELSE 'se agrega' END AS Accion
FROM @new n LEFT JOIN PRO_DFSC.dbo.OCRC c ON c.CreditCard = CAST(n.SapAccount AS int);

-- Pagos afectados
SELECT p.TenderID, p.BusinessDayDate, COUNT(*) AS Filas, SUM(p.Amount) AS Monto
FROM dbo.la_store_payments p
WHERE p.TenderID IN (SELECT TenderID FROM @new)
GROUP BY p.TenderID, p.BusinessDayDate
ORDER BY p.BusinessDayDate;

BEGIN TRAN;

INSERT INTO dbo.ADR_TENDER_SAP (TenderID, SapAccount, PaymentType, Description, BPLId)
SELECT n.TenderID, n.SapAccount, n.PaymentType, n.Description, NULL
FROM @new n
WHERE NOT EXISTS (SELECT 1 FROM dbo.ADR_TENDER_SAP t WHERE t.TenderID = n.TenderID)
  AND EXISTS (SELECT 1 FROM PRO_DFSC.dbo.OCRC c WHERE c.CreditCard = CAST(n.SapAccount AS int));
PRINT 'ADR_TENDER_SAP: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) agregadas';

IF @Commit = 1
BEGIN
    COMMIT;
    PRINT 'COMMIT: mapeo agregado.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT 'SIMULACION: se hizo ROLLBACK. Cambiar @Commit = 1 para confirmar.';
END
