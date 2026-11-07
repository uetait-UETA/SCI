/* =====================================================================
   ADR_TENDER_SAP: PayPal y Venmo como en SAP B1.

   En SAP (PRO_DFSC.OCRC) existen las tarjetas 8 PAYPAL (cuenta 1207-00-12
   Paypal) y 9 VENMO (cuenta 1207-00-13 Venmo), pero ADR_TENDER_SAP las
   mandaba como Transfer a 1299-00-00 (Clearing Account, la que contabilidad
   usa para contracargos). Se pasan a CreditCard 8 / 9, igual que CITCON (6)
   y WeChat (7). Las tarjetas 8 y 9 nunca se habían usado en SAP.

   Respaldo en dbo.ADR_TENDER_SAP_bak_20261101.

   USO: ejecutar en SMM_PROD. Primero con @Commit = 0 (simulación),
        luego con @Commit = 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar

DECLARE @map TABLE (TenderID varchar(50), PaymentType varchar(20), SapAccount varchar(20));
INSERT @map VALUES ('PAYPAL-26', 'CreditCard', '8'),
                   ('VENMO-26',  'CreditCard', '9');

-- Antes / después, con lo que hay en SAP
SELECT m.TenderID, t.PaymentType AS TipoAntes, t.SapAccount AS CuentaAntes,
       m.PaymentType AS TipoNuevo, m.SapAccount AS TarjetaNueva, c.CardName, c.AcctCode AS CuentaTarjetaSAP
FROM @map m
INNER JOIN dbo.ADR_TENDER_SAP t ON t.TenderID = m.TenderID
LEFT JOIN PRO_DFSC.dbo.OCRC c ON c.CreditCard = CAST(m.SapAccount AS int);

BEGIN TRAN;

IF OBJECT_ID('dbo.ADR_TENDER_SAP_bak_20261101') IS NULL
    SELECT * INTO dbo.ADR_TENDER_SAP_bak_20261101 FROM dbo.ADR_TENDER_SAP;

UPDATE t SET t.PaymentType = m.PaymentType, t.SapAccount = m.SapAccount
FROM dbo.ADR_TENDER_SAP t
INNER JOIN @map m ON m.TenderID = t.TenderID
WHERE EXISTS (SELECT 1 FROM PRO_DFSC.dbo.OCRC c WHERE c.CreditCard = CAST(m.SapAccount AS int));
PRINT 'ADR_TENDER_SAP: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) actualizadas';

IF @Commit = 1
BEGIN
    COMMIT;
    PRINT 'COMMIT: mapeo actualizado.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT 'SIMULACION: se hizo ROLLBACK. Cambiar @Commit = 1 para confirmar.';
END

/* Para revertir:
UPDATE t SET t.PaymentType = b.PaymentType, t.SapAccount = b.SapAccount
FROM dbo.ADR_TENDER_SAP t INNER JOIN dbo.ADR_TENDER_SAP_bak_20261101 b ON b.ID = t.ID
WHERE t.TenderID IN ('PAYPAL-26', 'VENMO-26');
*/
