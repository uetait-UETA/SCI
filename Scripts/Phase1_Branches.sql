-- ============================================================
-- Phase 1: Branch (Sucursal) context support
-- Run on the SMM database (SMM_DFC / SMM_LAT)
-- ============================================================

-- 1. SMM_BRANCHES: stores the branches defined in SAP B1 OBPL
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'SMM_BRANCHES'
)
BEGIN
    CREATE TABLE dbo.SMM_BRANCHES (
        BranchId        INT           NOT NULL,   -- = OBPL.BPLId in SAP B1
        BranchCode      NVARCHAR(20)  NOT NULL,
        BranchName      NVARCHAR(100) NOT NULL,
        CompanyId       NVARCHAR(20)  NOT NULL,   -- FK to SMM_COMPANIES.Companycode
        IsSellerBranch  BIT           NOT NULL DEFAULT 0,
        Active          BIT           NOT NULL DEFAULT 1,
        CONSTRAINT PK_SMM_BRANCHES PRIMARY KEY (CompanyId, BranchId)
    );
    PRINT 'SMM_BRANCHES created.';
END
ELSE
    PRINT 'SMM_BRANCHES already exists.';
GO

-- 2. RSS_OWHS_CONTROL: add BPLId so warehouse controls can be scoped per branch
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'RSS_OWHS_CONTROL' AND COLUMN_NAME = 'BPLId'
)
BEGIN
    ALTER TABLE dbo.RSS_OWHS_CONTROL ADD BPLId INT NULL;
    PRINT 'BPLId added to RSS_OWHS_CONTROL.';
END
ELSE
    PRINT 'BPLId already exists in RSS_OWHS_CONTROL.';
GO

-- ============================================================
-- STEP 1: Verify your CompanyId before running steps below
--         This should match Companycode in SMM_COMPANIES
-- ============================================================
SELECT CompanyId, Companycode, CompanyName FROM dbo.SMM_COMPANIES;
GO

-- ============================================================
-- STEP 2: Populate SMM_BRANCHES with the actual OBPL data
--         IMPORTANT: Replace 'YOUR_COMPANY_CODE' with the
--         Companycode value returned by the query above
-- ============================================================
DECLARE @CompanyId NVARCHAR(20) = 'YOUR_COMPANY_CODE';  -- <-- Change this

-- Branches from SAP B1 OBPL:
--   BPLId 1 = DFSC Buying Group, Inc.       --> IsSellerBranch = 1 (vende a las otras)
--   BPLId 3 = Duty Free California LAX, LLC. --> IsSellerBranch = 0
--   BPLId 4 = Duty Free California SFO, LLC. --> IsSellerBranch = 0

INSERT INTO dbo.SMM_BRANCHES (BranchId, BranchCode, BranchName, CompanyId, IsSellerBranch, Active)
SELECT BranchId, BranchCode, BranchName, @CompanyId, IsSellerBranch, 1
FROM (VALUES
    (1, 'DFSC-BG',  'DFSC Buying Group, Inc.',        1),
    (3, 'DFC-LAX',  'Duty Free California LAX, LLC.',  0),
    (4, 'DFC-SFO',  'Duty Free California SFO, LLC.',  0)
) AS src(BranchId, BranchCode, BranchName, IsSellerBranch)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.SMM_BRANCHES b
    WHERE b.CompanyId = @CompanyId AND b.BranchId = src.BranchId
);

PRINT 'SMM_BRANCHES populated.';
GO

-- ============================================================
-- STEP 3: Assign BPLId to each warehouse in RSS_OWHS_CONTROL
--         Replace 'YOUR_COMPANY_CODE' and 'YOUR_SAP_DB' below
--         YOUR_SAP_DB is the SAP B1 company database name
--         (same value used in cross-DB queries: {0}..OWHS)
-- ============================================================
DECLARE @CompanyId2  NVARCHAR(20) = 'YOUR_COMPANY_CODE';  -- <-- Change this
DECLARE @SapDb       NVARCHAR(50) = 'YOUR_SAP_DB';        -- <-- SAP B1 DB name

DECLARE @sql NVARCHAR(MAX) = N'
UPDATE r
SET r.BPLId = w.BPLId
FROM dbo.RSS_OWHS_CONTROL r
INNER JOIN ' + QUOTENAME(@SapDb) + N'.dbo.OWHS w ON w.WhsCode = r.Whscode
WHERE r.CompanyId = @cid
  AND r.BPLId IS NULL';

EXEC sp_executesql @sql, N'@cid NVARCHAR(20)', @cid = @CompanyId2;
PRINT 'RSS_OWHS_CONTROL.BPLId updated.';
GO

-- ============================================================
-- STEP 4: Verify results
-- ============================================================
SELECT 'SMM_BRANCHES' AS [Table], BranchId, BranchCode, BranchName, CompanyId,
       CASE IsSellerBranch WHEN 1 THEN 'YES (Seller)' ELSE 'No' END AS IsSellerBranch
FROM dbo.SMM_BRANCHES
ORDER BY BranchId;

SELECT 'RSS_OWHS_CONTROL' AS [Table], CompanyId, Whscode, Control, BPLId
FROM dbo.RSS_OWHS_CONTROL
ORDER BY CompanyId, BPLId, Whscode;
GO
