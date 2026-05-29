-- ============================================================
-- SP_INVENTORY_KARDEX
-- Kardex de movimientos de inventario por item
-- Run on SMM database (SMM_DFC / SMM_LAT)
-- ============================================================
-- 2026-05-29: Agregado LEFT JOIN OWHS para incluir U_POSCode
-- ============================================================

ALTER PROCEDURE [dbo].[SP_INVENTORY_KARDEX]
    @Company  AS VARCHAR(50),
    @Grupo    AS VARCHAR(4000),
    @Item     AS VARCHAR(20),
    @FromDate AS VARCHAR(50),
    @ToDate   AS VARCHAR(50)
AS
BEGIN
    DECLARE @SQL AS VARCHAR(MAX), @WITH_NOLOCK NVARCHAR(20);

    SET @WITH_NOLOCK = N' ';

    IF @FromDate = ''
    BEGIN
        SET @FromDate = '1/1/0001';
    END

    IF @ToDate = ''
    BEGIN
        SET @ToDate = CONVERT(VARCHAR, GETDATE(), 101)
    END

    SET @SQL = N'WITH KARDEX_DATA AS (
  SELECT
    a.[TransNum], a.[DocDate], a.[CreatedBy],
    CASE a.[TransType]
      WHEN 20 THEN ISNULL((SELECT b.[U_BOL] FROM [' + @Company + '].[dbo].[OPDN] b ' + @WITH_NOLOCK + ' WHERE b.[DocEntry] = a.[CreatedBy]),'''')
      WHEN 67 THEN ISNULL((SELECT b.[U_BOL] FROM [' + @Company + '].[dbo].[OWTR] b ' + @WITH_NOLOCK + ' WHERE b.[DocEntry] = a.[CreatedBy]),'''')
      ELSE ''''
    END BaseDoc,
    [dbo].[UDF_GetSapDocumentName](a.[TransType]) NomDoc,
    a.[ItemCode],
    STUFF((SELECT '' - '' + RIGHT(BcdCode, 5) FROM ' + @Company + '.dbo.OBCD a1' + @WITH_NOLOCK + N'WHERE a1.ItemCode=a.[ItemCode] FOR XML PATH ('''')), 1, 3,'''') BarCode,
    a.[Dscription], a.[InQty], a.[InQty] AS InvPLInQty, a.[OutQty], a.[OutQty] AS InvPLOutQty,
    CASE a.[InQty]
      WHEN 0 THEN a.[OutQty] * -1
      ELSE a.[InQty]
    END QtyTrans,
    a.[Warehouse],
    wh.[U_POSCode],
    SUM( IIF(a.[InQty] = 0, a.[OutQty] * -1, a.[InQty]) ) OVER (ORDER BY a.[TransNum], a.[ItemCode] ROWS UNBOUNDED PRECEDING) AS Balance
  FROM [' + @Company + '].[dbo].[OINM] a ' + @WITH_NOLOCK + N'
  INNER JOIN [' + @Company + '].[dbo].[OITM] b ' + @WITH_NOLOCK + ' ON b.[ItemCode] = a.[ItemCode]
  LEFT JOIN  [' + @Company + '].[dbo].[OWHS] wh ' + @WITH_NOLOCK + ' ON wh.[WhsCode] = a.[Warehouse]
  WHERE b.[ItmsGrpCod] IN (SELECT Param FROM dbo.fn_MVParam(''' + @Grupo + ''','','')) ';

    IF @Item <> ''
    BEGIN
        SET @SQL = @SQL + N' AND a.[ItemCode] = ''' + @Item + N''' ';
    END

    SET @SQL = @SQL + N')
  SELECT
    a.[TransNum], a.[DocDate], a.[CreatedBy],
    a.[BaseDoc], a.[NomDoc], a.[ItemCode], a.[BarCode],
    a.[Dscription], a.[InQty], a.[InvPLInQty], a.[OutQty], a.[InvPLOutQty],
    a.[QtyTrans], a.[Warehouse], a.[U_POSCode],
    a.[Balance],
    SUM(a.[QtyTrans]) OVER (ORDER BY a.[TransNum], a.[ItemCode] ROWS UNBOUNDED PRECEDING) AS CurrentBalance
  FROM [KARDEX_DATA] a
  WHERE
    a.[DocDate] >= CONVERT(DATE, ''' + @FromDate + ''', 101)
    AND a.[DocDate] <= CONVERT(DATE, ''' + @ToDate + ''', 101) ';

    SET @SQL = @SQL + N' ORDER BY a.[TransNum] ';

    EXEC (@SQL);
END

-- Test:
-- EXEC [dbo].[SP_INVENTORY_KARDEX] 'DFC_HOLDINGS', '101', '01-00331', '01/01/2025', '12/31/2025'
