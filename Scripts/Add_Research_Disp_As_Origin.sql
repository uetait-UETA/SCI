/* =====================================================================
   Bodegas Research "DISP" como ORIGEN de transferencias (igual que las REC):
   3152 SFO RESEARCH DISP DP (BPLId 4) y 3052 LAX RESEARCH DISP DP (BPLId 3).

   No tenían ninguna configuración en SCI. Se copian las de 3153 / 3053:
   - RSS_OWHS_CONTROL: CRETRAFROM, CRETRAFROMXSAP, VIEWTRAREFROM
   - SMM_WHSTYPE: TIENDA

   Pedido del usuario 2026-11-11.

   Revertir:
     DELETE FROM dbo.RSS_OWHS_CONTROL WHERE CompanyId = 'PRO_DFSC' AND Whscode IN ('3052','3152')
       AND Control IN ('CRETRAFROM','CRETRAFROMXSAP','VIEWTRAREFROM');
     DELETE FROM dbo.SMM_WHSTYPE WHERE COMPANYID = 'PRO_DFSC' AND WHSCODE IN ('3052','3152');

   USO: ejecutar en SMM_PROD. Primero @Commit = 0 (simulación), luego 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit bit = 0;               -- 0 = simulación (ROLLBACK), 1 = confirmar

BEGIN TRAN;

-- Pares DISP → REC (modelo)
DECLARE @map TABLE (Disp nvarchar(8), Rec nvarchar(8));
INSERT INTO @map VALUES ('3152','3153'), ('3052','3053');

INSERT INTO dbo.RSS_OWHS_CONTROL (CompanyId, Whscode, Control, UserRole, BPLId)
SELECT c.CompanyId, m.Disp, c.Control, c.UserRole, c.BPLId
FROM @map m
INNER JOIN dbo.RSS_OWHS_CONTROL c ON c.CompanyId = 'PRO_DFSC' AND c.Whscode = m.Rec
WHERE c.Control IN ('CRETRAFROM','CRETRAFROMXSAP','VIEWTRAREFROM')
  AND NOT EXISTS (SELECT 1 FROM dbo.RSS_OWHS_CONTROL x
                  WHERE x.CompanyId = c.CompanyId AND x.Whscode = m.Disp AND x.Control = c.Control);
PRINT 'RSS_OWHS_CONTROL: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) insertadas';

INSERT INTO dbo.SMM_WHSTYPE (COMPANYID, WHSCODE, TYPEWHS, BPLId)
SELECT w.COMPANYID, m.Disp, w.TYPEWHS, w.BPLId
FROM @map m
INNER JOIN dbo.SMM_WHSTYPE w ON w.COMPANYID = 'PRO_DFSC' AND w.WHSCODE = m.Rec
WHERE NOT EXISTS (SELECT 1 FROM dbo.SMM_WHSTYPE x WHERE x.COMPANYID = w.COMPANYID AND x.WHSCODE = m.Disp);
PRINT 'SMM_WHSTYPE: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' fila(s) insertadas';

SELECT o.WhsCode, o.WhsName, o.BPLid, sw.TYPEWHS,
       STUFF((SELECT ',' + c.Control FROM dbo.RSS_OWHS_CONTROL c
              WHERE c.Whscode = o.WhsCode AND c.CompanyId = 'PRO_DFSC' ORDER BY c.Control FOR XML PATH('')), 1, 1, '') AS Controles
FROM PRO_DFSC.dbo.OWHS o
LEFT JOIN dbo.SMM_WHSTYPE sw ON sw.WHSCODE = o.WhsCode AND sw.COMPANYID = 'PRO_DFSC'
WHERE o.WhsCode IN ('3052','3053','3152','3153')
ORDER BY o.WhsCode;

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
