/* =====================================================================
   Create Transfer by Excel — habilitar como destino las bodegas de daños DP:
   3090 LAX DAMAGE DP (BPLId 3) y 3190 SFO DAMAGE DP (BPLId 4).

   La lista "Destination" de CreateTransferByExcel.aspx solo muestra bodegas
   con Control = 'CRETRATOEXCEL' en RSS_OWHS_CONTROL. Estas dos solo tenían
   VIEWTRA y CRETRATOXSAP (destino en Create Transfer por SAP).

   Pedido del usuario 2026-11-11 (transferir de 1136 a 3190).

   Revertir: DELETE FROM dbo.RSS_OWHS_CONTROL
             WHERE CompanyId = 'PRO_DFSC' AND Whscode IN ('3090','3190') AND Control = 'CRETRATOEXCEL';

   USO: ejecutar en SMM_PROD. Primero @Commit = 0 (simulación), luego 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar

SELECT c.Whscode, c.Control, c.BPLId FROM dbo.RSS_OWHS_CONTROL c
WHERE c.CompanyId = 'PRO_DFSC' AND c.Whscode IN ('3090','3190') ORDER BY c.Whscode, c.Control;

BEGIN TRAN;

INSERT INTO dbo.RSS_OWHS_CONTROL (CompanyId, Whscode, Control, UserRole, BPLId)
SELECT 'PRO_DFSC', o.WhsCode, 'CRETRATOEXCEL', '', o.BPLid
FROM PRO_DFSC.dbo.OWHS o
WHERE o.WhsCode IN ('3090','3190')
  AND NOT EXISTS (SELECT 1 FROM dbo.RSS_OWHS_CONTROL x
                  WHERE x.CompanyId = 'PRO_DFSC' AND x.Whscode = o.WhsCode AND x.Control = 'CRETRATOEXCEL');
PRINT 'RSS_OWHS_CONTROL: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) insertadas';

SELECT c.Whscode, c.Control, c.BPLId FROM dbo.RSS_OWHS_CONTROL c
WHERE c.CompanyId = 'PRO_DFSC' AND c.Whscode IN ('3090','3190') ORDER BY c.Whscode, c.Control;

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
