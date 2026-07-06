-- ============================================================
-- Migration script: SMM_PROD + PRO_DFSC
-- Equivalent to: SMM_DFC   + DFC_HOLDINGS
--
-- Run this script connected to SMM_PROD.
-- PRO_DFSC references are cross-database (must be on same SQL instance).
-- ============================================================

USE [SMM_PROD]
GO

PRINT '=== SCIv2 Migration to SMM_PROD / PRO_DFSC ==='
PRINT ''

-- ============================================================
-- 1. NEW TABLES
-- ============================================================

-- 1a. Repln_Brand_Priority
--     Brand-level replenishment priority per store/warehouse.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Repln_Brand_Priority' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Repln_Brand_Priority] (
        [Company]  VARCHAR(30) NOT NULL,
        [Location] VARCHAR(20) NOT NULL,   -- destination warehouse (tienda)
        [Brand]    VARCHAR(50) NOT NULL,   -- OITM.U_Brand value
        [Priority] INT         NOT NULL CONSTRAINT DF_Repln_Brand_Priority_Priority DEFAULT 99,
        [Branch]   INT         NOT NULL,
        CONSTRAINT PK_Repln_Brand_Priority PRIMARY KEY CLUSTERED ([Company], [Location], [Brand])
    );
    CREATE NONCLUSTERED INDEX IX_Repln_Brand_Priority_Location
        ON [dbo].[Repln_Brand_Priority] ([Company], [Location]);
    PRINT 'Created: Repln_Brand_Priority';
END
ELSE PRINT 'Exists:  Repln_Brand_Priority';
GO

-- 1b. GrpoStoreMapping
--     Store-to-vendor mapping for Goods Receipt PO (ComprasDirectas.aspx).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'GrpoStoreMapping' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.GrpoStoreMapping (
        MappingId      INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
        StoreCode      NVARCHAR(30)  NOT NULL,
        StoreName      NVARCHAR(100) NOT NULL,
        OriginCardCode NVARCHAR(50)  NOT NULL,
        DestCardCode   NVARCHAR(50)  NOT NULL,
        DestWhsCode    NVARCHAR(20)  NOT NULL,
        IsActive       BIT           NOT NULL DEFAULT 1,
        Notes          NVARCHAR(255) NULL
    );
    PRINT 'Created: GrpoStoreMapping';
END
ELSE PRINT 'Exists:  GrpoStoreMapping';
GO

-- 1c. GrpoReceiptLog
--     Audit log for all Goods Receipt PO operations.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'GrpoReceiptLog' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.GrpoReceiptLog (
        LogId          INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OriginCompany  NVARCHAR(30)   NOT NULL,
        OriginDocEntry INT            NOT NULL,
        OriginDocNum   INT            NOT NULL,
        DestCompany    NVARCHAR(30)   NOT NULL,
        DestDocEntry   INT            NOT NULL DEFAULT 0,
        DestDocNum     INT            NOT NULL DEFAULT 0,
        DestCardCode   NVARCHAR(50)   NOT NULL,
        DestWhsCode    NVARCHAR(20)   NOT NULL,
        ReceivedBy     NVARCHAR(50)   NOT NULL,
        ReceivedAt     DATETIME       NOT NULL DEFAULT GETDATE(),
        Status         NVARCHAR(10)   NOT NULL,   -- 'SUCCESS' or 'ERROR'
        ErrorMsg       NVARCHAR(2000) NULL
    );
    PRINT 'Created: GrpoReceiptLog';
END
ELSE PRINT 'Exists:  GrpoReceiptLog';
GO

-- 1d. ApriCardCodeMapping
--     Maps destination BPLId to the inter-company SAP CardCode used in ORDR/OINV.
--     Required for Branch 1 (BODEGA) → Branch 4+ (TIENDA) auto-dispatch flow.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApriCardCodeMapping' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.ApriCardCodeMapping (
        MappingId    INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OinvCardCode NVARCHAR(50)  NOT NULL,   -- SAP CardCode for ORDR/OINV header
        DestBPLId    INT           NOT NULL,   -- destination branch BPLId (= OWHS.BPLId)
        IsActive     BIT           NOT NULL DEFAULT 1,
        Notes        NVARCHAR(255) NULL
    );
    PRINT 'Created: ApriCardCodeMapping';
END
ELSE PRINT 'Exists:  ApriCardCodeMapping';
GO

-- ============================================================
-- 2. ALTER EXISTING TABLES — smm_Transdiscrep_odrf
-- ============================================================

-- 2a. ITR / ORDR document references (Dispatch phase)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.smm_Transdiscrep_odrf') AND name = 'DocEntryITR')
BEGIN
    ALTER TABLE dbo.smm_Transdiscrep_odrf ADD DocEntryITR INT NULL, DocNumITR INT NULL;
    PRINT 'Added columns: smm_Transdiscrep_odrf.DocEntryITR, DocNumITR';
END
ELSE PRINT 'Exists:  smm_Transdiscrep_odrf.DocEntryITR';
GO

-- 2b. Surplus OPDN #2 document references (over-receive on Duty Paid)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.smm_Transdiscrep_odrf') AND name = 'DocEntryOpdn2')
BEGIN
    ALTER TABLE dbo.smm_Transdiscrep_odrf ADD DocEntryOpdn2 INT NULL, DocNumOpdn2 INT NULL;
    PRINT 'Added columns: smm_Transdiscrep_odrf.DocEntryOpdn2, DocNumOpdn2';
END
ELSE PRINT 'Exists:  smm_Transdiscrep_odrf.DocEntryOpdn2';
GO

-- ============================================================
-- 3. ACCESS CONTROL — new pages
-- ============================================================

-- 3a. FillBrandPriority.aspx
IF NOT EXISTS (SELECT 1 FROM dbo.SISINV_CONTROLS WHERE ControlName = 'FillBrandPriority.aspx')
BEGIN
    INSERT INTO dbo.SISINV_CONTROLS (ControlName, ControlType, ControlDesc, Date_Created, Created_By)
    VALUES ('FillBrandPriority.aspx', 'FORM', 'Brand Replenishment Priority', GETDATE(), 'SYSTEM');
    PRINT 'Inserted: SISINV_CONTROLS FillBrandPriority.aspx';
END
ELSE PRINT 'Exists:  SISINV_CONTROLS FillBrandPriority.aspx';
GO

INSERT INTO dbo.SISINV_ROLE_CONTROL (RoleID, ControlName, AccessType, Date_Created, Created_By)
SELECT rc.RoleID, 'FillBrandPriority.aspx', rc.AccessType, GETDATE(), 'SYSTEM'
FROM dbo.SISINV_ROLE_CONTROL rc
WHERE rc.ControlName = 'FillPriority.aspx'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.SISINV_ROLE_CONTROL x
      WHERE x.ControlName = 'FillBrandPriority.aspx' AND x.RoleID = rc.RoleID
  );
PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' SISINV_ROLE_CONTROL rows inserted for FillBrandPriority.aspx';
GO

-- 3b. ComprasDirectas.aspx
IF NOT EXISTS (SELECT 1 FROM dbo.SISINV_CONTROLS WHERE ControlName = 'ComprasDirectas.aspx')
BEGIN
    INSERT INTO dbo.SISINV_CONTROLS (ControlName, ControlType, ControlDesc, Date_Created, Created_By)
    VALUES ('ComprasDirectas.aspx', 'FORM', 'Direct Purchase Receiving', GETDATE(), 'SYSTEM');
    PRINT 'Inserted: SISINV_CONTROLS ComprasDirectas.aspx';
END
ELSE PRINT 'Exists:  SISINV_CONTROLS ComprasDirectas.aspx';
GO

INSERT INTO dbo.SISINV_ROLE_CONTROL (RoleID, ControlName, AccessType, Date_Created, Created_By)
SELECT rc.RoleID, 'ComprasDirectas.aspx', rc.AccessType, GETDATE(), 'SYSTEM'
FROM dbo.SISINV_ROLE_CONTROL rc
WHERE rc.ControlName = 'Transfers.aspx'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.SISINV_ROLE_CONTROL x
      WHERE x.ControlName = 'ComprasDirectas.aspx' AND x.RoleID = rc.RoleID
  );
PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' SISINV_ROLE_CONTROL rows inserted for ComprasDirectas.aspx';
GO

-- 3c. ComprasDirectasDetail.aspx
IF NOT EXISTS (SELECT 1 FROM dbo.SISINV_CONTROLS WHERE ControlName = 'ComprasDirectasDetail.aspx')
BEGIN
    INSERT INTO dbo.SISINV_CONTROLS (ControlName, ControlType, ControlDesc, Date_Created, Created_By)
    VALUES ('ComprasDirectasDetail.aspx', 'FORM', 'Direct Purchase Receiving Detail', GETDATE(), 'SYSTEM');
    PRINT 'Inserted: SISINV_CONTROLS ComprasDirectasDetail.aspx';
END
ELSE PRINT 'Exists:  SISINV_CONTROLS ComprasDirectasDetail.aspx';
GO

INSERT INTO dbo.SISINV_ROLE_CONTROL (RoleID, ControlName, AccessType, Date_Created, Created_By)
SELECT rc.RoleID, 'ComprasDirectasDetail.aspx', rc.AccessType, GETDATE(), 'SYSTEM'
FROM dbo.SISINV_ROLE_CONTROL rc
WHERE rc.ControlName = 'Transfers.aspx'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.SISINV_ROLE_CONTROL x
      WHERE x.ControlName = 'ComprasDirectasDetail.aspx' AND x.RoleID = rc.RoleID
  );
PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' SISINV_ROLE_CONTROL rows inserted for ComprasDirectasDetail.aspx';
GO

-- ============================================================
-- 4. SMM_WHSTYPE — warehouse type classification
--    PRO_DFSC equivalent of what was set in DFC_HOLDINGS.
--
--    The relay warehouses (Duty Free / Duty Paid) at the
--    SFO branch must be TIENDA (not BODEGA) so they don't
--    trigger auto-dispatch from their side.
--
--    Replace warehouse codes below with PRO_DFSC actual codes
--    (equivalent of 1130=DF Relay, 1888=DP Relay in DFC_HOLDINGS).
-- ============================================================

-- ⚠️ ACTION REQUIRED: Replace 'RELAY_DF' and 'RELAY_DP' with
--    the actual WhsCodes used in PRO_DFSC for the SFO relay warehouses.

/*
UPDATE dbo.SMM_WHSTYPE
SET TYPEWHS = 'TIENDA'
WHERE COMPANYID = 'PRO_DFSC'
  AND WHSCODE IN ('RELAY_DF', 'RELAY_DP');
*/

PRINT '⚠️  ACTION REQUIRED: Update SMM_WHSTYPE for PRO_DFSC relay warehouses (see comment above).';
GO

-- ============================================================
-- 5. SMM_REA_BODEGA_TIENDAS — bodega-to-tienda pairs
--    Required by BrandReplenishment batch.
--
--    This INSERT selects all pairs from PRO_DFSC automatically:
--    BODEGA warehouses (BPLId=1, TYPEWHS='BODEGA') paired with
--    each TIENDA warehouse under the same company.
--
--    ⚠️ Verify BODEGA and TIENDA WhsCodes in PRO_DFSC/SMM_WHSTYPE
--       before running.
-- ============================================================

INSERT INTO dbo.SMM_REA_BODEGA_TIENDAS (CompanyID, BodegaID, TiendaID, Priority, isActive)
SELECT
    'PRO_DFSC'  AS CompanyID,
    b.WHSCODE   AS BodegaID,
    t.WHSCODE   AS TiendaID,
    1           AS Priority,
    1           AS isActive
FROM dbo.SMM_WHSTYPE b
CROSS JOIN dbo.SMM_WHSTYPE t
WHERE b.COMPANYID = 'PRO_DFSC'
  AND b.TYPEWHS   = 'BODEGA'
  AND t.COMPANYID = 'PRO_DFSC'
  AND t.TYPEWHS   = 'TIENDA'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.SMM_REA_BODEGA_TIENDAS x
      WHERE x.CompanyID = 'PRO_DFSC'
        AND x.BodegaID  = b.WHSCODE
        AND x.TiendaID  = t.WHSCODE
  );

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' rows inserted into SMM_REA_BODEGA_TIENDAS for PRO_DFSC';
GO

-- ============================================================
-- 6. rss_loc_dept_multiple — StoreMinMax3 min/max entries
--    Ensures every warehouse+category combination that has
--    items in OITW is registered, otherwise StoreMinMax3
--    grid will appear empty.
-- ============================================================

INSERT INTO dbo.rss_loc_dept_multiple (LOC, dept, companyId, ORDER_MULTIPLE)
SELECT w.WhsCode, d.ItmsGrpCod, 'PRO_DFSC', 'E'
FROM [PRO_DFSC].dbo.OWHS w
INNER JOIN dbo.RSS_OWHS_CONTROL c
    ON c.WhsCode = w.WhsCode AND c.Control = 'SETMINMAX' AND c.CompanyId = 'PRO_DFSC'
CROSS JOIN [PRO_DFSC].dbo.oitb d
WHERE EXISTS (
    SELECT 1
    FROM [PRO_DFSC].dbo.OITW iw
    INNER JOIN [PRO_DFSC].dbo.OITM im ON im.ItemCode = iw.ItemCode
    WHERE iw.WhsCode = w.WhsCode AND im.ItmsGrpCod = d.ItmsGrpCod
)
AND NOT EXISTS (
    SELECT 1 FROM dbo.rss_loc_dept_multiple m
    WHERE m.LOC = w.WhsCode AND m.dept = d.ItmsGrpCod AND m.companyId = 'PRO_DFSC'
);

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' rows inserted into rss_loc_dept_multiple for PRO_DFSC';
GO

-- ============================================================
-- 7. ApriCardCodeMapping — inter-company CardCode for ORDR
--    Needed for auto-dispatch when origin=BODEGA(BPLId=1)
--    and destination=TIENDA at another branch.
--
--    ⚠️ ACTION REQUIRED: Insert the correct CardCode that
--       represents DFSC Buying Group as a vendor/customer
--       in PRO_DFSC for each destination BPLId.
--       Example (replace values as needed):
-- ============================================================

/*
-- BPLId=4 → SFO branch (equivalent of DFC_HOLDINGS BPLId=4)
INSERT INTO dbo.ApriCardCodeMapping (OinvCardCode, DestBPLId, IsActive, Notes)
VALUES ('DFS_SFO', 4, 1, 'SFO branch inter-company customer');

-- BPLId=3 → LAX branch (if ORDR is used instead of OWTQ for some flows)
-- INSERT INTO dbo.ApriCardCodeMapping (OinvCardCode, DestBPLId, IsActive, Notes)
-- VALUES ('DFS_LAX', 3, 1, 'LAX branch inter-company customer');
*/

PRINT '⚠️  ACTION REQUIRED: Insert ApriCardCodeMapping rows for PRO_DFSC (see comment above).';
GO

-- ============================================================
-- DONE
-- ============================================================
PRINT ''
PRINT '=== Migration script completed ==='
PRINT 'Review any ⚠️  ACTION REQUIRED messages above before going live.'
GO
