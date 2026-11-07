/* =====================================================================
   ADR_TENDER_SAP: formas de pago del POS que aparecen desde el 16/08/2026
   y no estaban mapeadas (los días que las usan quedarían Blocked con
   "Payment X not in ADR_TENDER_SAP"):

     VISA DCC-8        → tarjeta 1 VISA   (1207-00-07)  - Visa con conversión de moneda
     MASTERCARD DCC-9  → tarjeta 2 MASTER (1207-00-07)  - Mastercard con conversión de moneda
     ALIPAY_CN-23      → tarjeta 6 CITCON (1207-00-08)  - CITCON procesa WeChat/Alipay

   DFA VIP Points-92 queda pendiente de contabilidad.
   No duplica: solo inserta los TenderID que no existen.

   USO: ejecutar en SMM_PROD. Primero con @Commit = 0 (simulación),
        luego con @Commit = 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar

DECLARE @new TABLE (TenderID nvarchar(100), SapAccount nvarchar(20), PaymentType nvarchar(20), Description nvarchar(100));
INSERT @new VALUES ('VISA DCC-8',       '1', 'CreditCard', 'Visa DCC'),
                   ('MASTERCARD DCC-9', '2', 'CreditCard', 'Mastercard DCC'),
                   ('ALIPAY_CN-23',     '6', 'CreditCard', 'Alipay (CITCON)');

SELECT n.*, c.CardName, c.AcctCode,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.ADR_TENDER_SAP t WHERE t.TenderID = n.TenderID) THEN 'ya existe' ELSE 'se agrega' END AS Accion
FROM @new n LEFT JOIN PRO_DFSC.dbo.OCRC c ON c.CreditCard = CAST(n.SapAccount AS int);

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
    PRINT 'COMMIT: mapeos agregados.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT 'SIMULACION: se hizo ROLLBACK. Cambiar @Commit = 1 para confirmar.';
END
