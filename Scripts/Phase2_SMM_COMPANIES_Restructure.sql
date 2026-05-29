-- ============================================================
-- Phase 2: Restructure SMM_COMPANIES for shared Companycode
-- Run on SMM database (SMM_DFC / SMM_LAT)
-- ============================================================
-- CONTEXT:
--   Previously each company had a unique Companycode (DFC_LAX, DFC_SFO).
--   Now both branches live in ONE SAP B1 database (DFC_HOLDINGS),
--   so both rows must share the same Companycode.
--   SMM_COMPANIES.Branch (BPLId) is the differentiator.
--
--   Before:
--     CompanyId=1  Companycode=DFC_LAX   Branch=3
--     CompanyId=7  Companycode=DFC_SFO   Branch=4
--
--   After:
--     CompanyId=1  Companycode=DFC_HOLDINGS  Branch=3  (Duty Free California LAX)
--     CompanyId=7  Companycode=DFC_HOLDINGS  Branch=4  (Duty Free California SFO)
-- ============================================================

-- STEP 1: Drop the old unique index (was unique on Companycode alone)
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'SMM_COMPANIES_IDX1' AND object_id = OBJECT_ID('dbo.SMM_COMPANIES')
)
BEGIN
    DROP INDEX SMM_COMPANIES_IDX1 ON dbo.SMM_COMPANIES;
    PRINT 'SMM_COMPANIES_IDX1 dropped.';
END
GO

-- STEP 2: Create new unique index on (Companycode, Branch)
--         Allows multiple rows with same Companycode, differentiated by Branch
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'SMM_COMPANIES_IDX1' AND object_id = OBJECT_ID('dbo.SMM_COMPANIES')
)
BEGIN
    CREATE UNIQUE INDEX SMM_COMPANIES_IDX1
        ON dbo.SMM_COMPANIES (Companycode, Branch);
    PRINT 'SMM_COMPANIES_IDX1 recreated on (Companycode, Branch).';
END
GO

-- STEP 3: Update both rows to use the new shared Companycode
UPDATE dbo.SMM_COMPANIES
SET Companycode = 'DFC_HOLDINGS'
WHERE CompanyId IN (1, 7);
PRINT 'Companycode updated to DFC_HOLDINGS for CompanyId 1 and 7.';
GO

-- STEP 4: Verify the result
SELECT CompanyId, Companycode, CompanyName, tienda_db, Branch
FROM dbo.SMM_COMPANIES
ORDER BY CompanyId;
GO

-- ============================================================
-- OPTIONAL STEP 5: Add DFSC Buying Group entry (Branch 1)
--   Uncomment if users from the buying group also need to log in
-- ============================================================
/*
INSERT INTO dbo.SMM_COMPANIES (Companycode, CompanyName, tienda_db, Branch)
VALUES ('DFC_HOLDINGS', 'DFSC Buying Group, Inc.', NULL, 1);
PRINT 'DFSC Buying Group entry added.';
*/
GO

-- ============================================================
-- OPTIONAL STEP 6: Migrate existing SMM_ table history
--   Only needed if you want old transfer records to appear
--   under the new DFC_HOLDINGS company context.
--   REVIEW each table before running - check row counts first.
-- ============================================================
/*
SELECT 'SMM_ODRF'              AS Tabla, COUNT(*) AS Rows, CompanyId FROM dbo.SMM_ODRF              GROUP BY CompanyId
UNION ALL
SELECT 'SMM_DRF1'             , COUNT(*), CompanyId FROM dbo.SMM_DRF1              GROUP BY CompanyId
UNION ALL
SELECT 'smm_Transdiscrep_odrf', COUNT(*), CompanyId FROM dbo.smm_Transdiscrep_odrf GROUP BY CompanyId
UNION ALL
SELECT 'smm_Transdiscrep_drf1', COUNT(*), CompanyId FROM dbo.smm_Transdiscrep_drf1 GROUP BY CompanyId
UNION ALL
SELECT 'SMM_WHSTYPE'          , COUNT(*), COMPANYID FROM dbo.SMM_WHSTYPE            GROUP BY COMPANYID
UNION ALL
SELECT 'RSS_OWHS_CONTROL'     , COUNT(*), CompanyId FROM dbo.RSS_OWHS_CONTROL       GROUP BY CompanyId;

-- Run only after verifying the counts above
UPDATE dbo.SMM_ODRF              SET CompanyId = 'DFC_HOLDINGS' WHERE CompanyId IN ('DFC_LAX','DFC_SFO');
UPDATE dbo.SMM_DRF1              SET CompanyId = 'DFC_HOLDINGS' WHERE CompanyId IN ('DFC_LAX','DFC_SFO');
UPDATE dbo.smm_Transdiscrep_odrf SET CompanyId = 'DFC_HOLDINGS' WHERE CompanyId IN ('DFC_LAX','DFC_SFO');
UPDATE dbo.smm_Transdiscrep_drf1 SET CompanyId = 'DFC_HOLDINGS' WHERE CompanyId IN ('DFC_LAX','DFC_SFO');
UPDATE dbo.SMM_WHSTYPE           SET COMPANYID = 'DFC_HOLDINGS' WHERE COMPANYID  IN ('DFC_LAX','DFC_SFO');
UPDATE dbo.RSS_OWHS_CONTROL      SET CompanyId = 'DFC_HOLDINGS' WHERE CompanyId  IN ('DFC_LAX','DFC_SFO');
UPDATE dbo.smm_odrf_audit        SET CompanyId = 'DFC_HOLDINGS' WHERE CompanyId  IN ('DFC_LAX','DFC_SFO');
UPDATE dbo.SMM_BRANCHES          SET CompanyId = 'DFC_HOLDINGS' WHERE CompanyId  IN ('DFC_LAX','DFC_SFO');
PRINT 'Historical data migrated to DFC_HOLDINGS.';
*/
GO
