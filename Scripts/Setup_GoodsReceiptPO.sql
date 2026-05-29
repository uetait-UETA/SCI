-- ============================================================
-- Setup: Goods Receipt PO tables
-- Run on SMM database (SMM_DFC / SMM_LAT)
-- ============================================================

-- ── 1. Store mapping table ──────────────────────────────────
--   StoreCode      : short code shown in the combo-box (e.g. 'LAX', 'SFO')
--   StoreName      : display name (e.g. 'Duty Free LAX')
--   OriginCardCode : vendor CardCode in the origin SAP company (OPCH.CardCode)
--   DestCardCode   : CardCode in the destination SAP company for the GRPO header
--   DestWhsCode    : default destination warehouse in the destination company
--   IsActive       : 1 = show in filter, 0 = hidden
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE type = 'U' AND name = 'GrpoStoreMapping'
)
BEGIN
    CREATE TABLE dbo.GrpoStoreMapping
    (
        MappingId      INT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
        StoreCode      NVARCHAR(30)  NOT NULL,
        StoreName      NVARCHAR(100) NOT NULL,
        OriginCardCode NVARCHAR(50)  NOT NULL,   -- vendor in origin company
        DestCardCode   NVARCHAR(50)  NOT NULL,   -- vendor/card in destination company
        DestWhsCode    NVARCHAR(20)  NOT NULL,   -- destination warehouse
        IsActive       BIT           NOT NULL DEFAULT 1,
        Notes          NVARCHAR(255) NULL
    );

    PRINT 'GrpoStoreMapping created.';
END
ELSE
    PRINT 'GrpoStoreMapping already exists.';
GO

-- ── 2. Receipt audit log table ──────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE type = 'U' AND name = 'GrpoReceiptLog'
)
BEGIN
    CREATE TABLE dbo.GrpoReceiptLog
    (
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

    PRINT 'GrpoReceiptLog created.';
END
ELSE
    PRINT 'GrpoReceiptLog already exists.';
GO

-- ── 3. Sample mapping rows (edit values to match your SAP setup) ────────────
-- INSERT INTO dbo.GrpoStoreMapping
--     (StoreCode, StoreName, OriginCardCode, DestCardCode, DestWhsCode, IsActive)
-- VALUES
--     ('LAX', 'Duty Free California LAX', 'V_DFSC_BG', 'V_DFSC_BG', 'STORE-LAX', 1),
--     ('SFO', 'Duty Free California SFO', 'V_DFSC_BG', 'V_DFSC_BG', 'STORE-SFO', 1);

-- ============================================================
-- web.config keys to add in <appSettings>:
--
--   <add key="SL_BaseUrl"       value="https://YOUR_SAP_SERVER:50000/b1s/v1" />
--   <add key="SL_OriginCompany" value="DFC_HOLDINGS" />
--   <add key="SiteName"         value="DFC_HOLDINGS" />
--
-- serverUserName and serverPwd already exist in web.config.
-- ============================================================
