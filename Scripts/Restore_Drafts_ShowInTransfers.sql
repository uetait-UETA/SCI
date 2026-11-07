/* =====================================================================
   Complemento de Restore_Drafts_From_Bads_ITR.sql

   Transfers.aspx (Queries.With_SmmDraftHeader) solo lista drafts de
   smm_Transdiscrep_odrf que además existan en SMM_ODRF o en
   smm_TransXsap_Drafts. TransferDelete también las borró de SMM_ODRF,
   así que se registran en smm_TransXsap_Drafts para que vuelvan a
   aparecer en el listado (y cuenten como en tránsito mientras estén
   abiertas, igual que cualquier otra draft).
   ===================================================================== */

SET NOCOUNT ON;

DECLARE @Commit  bit          = 0;            -- 0 = simulación (ROLLBACK), 1 = confirmar
DECLARE @Company nvarchar(30) = N'PRO_DFSC';

DECLARE @Drafts TABLE (DocEntry int PRIMARY KEY);
INSERT INTO @Drafts VALUES (2000947), (2000948), (2000949), (2000950), (2000953);

BEGIN TRAN;

INSERT INTO dbo.smm_TransXsap_Drafts (DocEntry, DocNum, UserDraft, Date_Created, Created_By)
SELECT o.DocEntry, o.DocNum, o.Created_By, o.Date_Created, o.Created_By
FROM dbo.smm_Transdiscrep_odrf o
JOIN @Drafts d ON d.DocEntry = o.DocEntry
WHERE o.CompanyId = @Company
  AND NOT EXISTS (SELECT 1 FROM dbo.smm_TransXsap_Drafts x WHERE x.DocEntry = o.DocEntry);

PRINT N'Registradas: ' + CAST(@@ROWCOUNT AS nvarchar(10));

-- Debe salir una fila por draft, con EnTransdiscrep = 'Y' y EnXsapDrafts = 'Y'
SELECT d.DocEntry,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.smm_Transdiscrep_odrf o WHERE o.CompanyId = @Company AND o.DocEntry = d.DocEntry) THEN 'Y' ELSE 'N' END AS EnTransdiscrep,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.smm_TransXsap_Drafts x WHERE x.DocEntry = d.DocEntry) THEN 'Y' ELSE 'N' END AS EnXsapDrafts
FROM @Drafts d
ORDER BY d.DocEntry;

IF @Commit = 1
BEGIN
    COMMIT;
    PRINT N'COMMIT: las drafts ya deben aparecer en Transfers.aspx.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT N'SIMULACIÓN: se hizo ROLLBACK. Cambiar @Commit = 1 para confirmar.';
END
