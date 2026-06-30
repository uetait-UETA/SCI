-- Replenishment priority by brand x store location.
-- When a Location has rows here, the replenishment batch sends ONLY the
-- configured brands to that store (ignoring Repln_Location_Priority for that pair).
-- Priority 1 = highest, 99 = lowest (same scale as Repln_Location_Priority).

IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'Repln_Brand_Priority' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[Repln_Brand_Priority] (
        [Company]  VARCHAR(30) NOT NULL,
        [Location] VARCHAR(20) NOT NULL,   -- ToWhs (tienda / destination warehouse)
        [Brand]    VARCHAR(50) NOT NULL,   -- OITM.U_Brand value
        [Priority] INT         NOT NULL CONSTRAINT DF_Repln_Brand_Priority_Priority DEFAULT 99,
        [Branch]   INT         NOT NULL,
        CONSTRAINT PK_Repln_Brand_Priority PRIMARY KEY CLUSTERED ([Company], [Location], [Brand])
    );

    CREATE NONCLUSTERED INDEX IX_Repln_Brand_Priority_Location
        ON [dbo].[Repln_Brand_Priority] ([Company], [Location]);

    PRINT 'Table Repln_Brand_Priority created.';
END
ELSE
    PRINT 'Table Repln_Brand_Priority already exists.';
GO
