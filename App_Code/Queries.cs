/// <summary>
/// Summary description for Queries
/// </summary>
public class Queries
{
    public const string WITH_NOLOCK = " WITH(NOLOCK) ";

    /// <summary>
    /// Returns the SQL WITH sentece to get ResearchWhs. It could be used anywhere in the
    /// query you are building.
    /// In a string.Format function, it has following indexes:
    ///  {0} - Company Id
    /// </summary>
    /// <returns>string</returns>
    public static string With_ResearchWhsCodes()
    {
        return @"WITH ResearchWhs AS (
    SELECT WHSCODE 
    FROM SMM_WHSTYPE " + WITH_NOLOCK + @" 
    WHERE COMPANYID = '{0}' AND [TYPEWHS] = 'RESEARCH'
)";
    }
    /// <summary>
    /// Returns the SQL WITH sentences to get SmmDraftHeader. It also has TransdiscrepODRF, 
    /// TransdiscrepODRF2, SmmODRF, SmmODRF2 WITH sentences, which are used on 
    /// SmmDraftDetail WITH sentence. They could be used anywhere in the query 
    /// you are building.
    /// In a string.Format function, it has the following indexes:
    ///  {0} - Company Id
    /// </summary>
    /// <returns>string</returns>
    public static string With_SmmDraftHeader()
    {
        return @"WITH TransdiscrepODRF AS (
	SELECT a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.FromWhsCode, a.dispatched, a.received, a.DispCompleted, a.ReceCompleted, a.InputType, a.ScanStatus, a.DocNumITR, ISNULL(a.TransferType,'') TransferType
	FROM smm_Transdiscrep_odrf a" + WITH_NOLOCK + @"
	WHERE a.CompanyId = '{0}'
), TransdiscrepODRF2 AS (
	SELECT a.CompanyId AS CompanyId, a.DocEntry, DocDate, a.DocStatus, a.FromWhsCode AS FromLoc, b.ToWhsCode AS ToLoc, b.DraftQuantity, d.WhsName AS ToLocName, e.WhsName AS FromLocName, f.ItmsGrpNam AS Category, f.ItmsGrpCod
	FROM TransdiscrepODRF AS a " + WITH_NOLOCK + @" 
	INNER JOIN smm_Transdiscrep_drf1 AS b " + WITH_NOLOCK + @" ON a.DocEntry = b.DocEntry AND a.CompanyId = b.CompanyId
	INNER JOIN {0}.dbo.OITM AS c " + WITH_NOLOCK + @" ON b.ItemCode = c.ItemCode 
	INNER JOIN {0}.dbo.OWHS AS d " + WITH_NOLOCK + @" ON b.ToWhsCode = d.WhsCode 
	INNER JOIN {0}.dbo.OWHS AS e " + WITH_NOLOCK + @" ON a.FromWhsCode = e.WhsCode 
	INNER JOIN {0}.dbo.OITB AS f " + WITH_NOLOCK + @" ON c.ItmsGrpCod = f.ItmsGrpCod
), SmmODRF AS (
	SELECT a.CompanyId, a.DocEntry, a.DocDate, a.DocTime, a.DocStatus, a.Filler, a.CANCELED, a.ObjType, a.Comments
	FROM SMM_ODRF a " + WITH_NOLOCK + @" 
	WHERE CompanyId = '{0}'
), SmmODRF2 AS (
	SELECT a.CompanyId, a.DocEntry, CONVERT(varchar, a.DocDate, 101) + ' ' + CASE WHEN DocTime < 1000 THEN '0' + LEFT(CONVERT(varchar, a.DocTime), 1) + ':' + RIGHT(CONVERT(varchar, a.DocTime), 2) ELSE LEFT(CONVERT(varchar, a.DocTime), 2) + ':' + RIGHT(CONVERT(varchar, a.DocTime), 2) END AS DocDate, a.DocStatus, a.Filler AS FromLoc, b.WhsCode AS ToLoc, b.Quantity, d.WhsName AS ToLocName, e.WhsName AS FromLocName, f.ItmsGrpNam AS Category, f.ItmsGrpCod, a.CANCELED, a.ObjType, a.Comments
	FROM SmmODRF AS a
	INNER JOIN SMM_DRF1 AS b " + WITH_NOLOCK + @" ON a.DocEntry = b.DocEntry AND a.CompanyId = b.CompanyId
	INNER JOIN {0}.dbo.OITM AS c " + WITH_NOLOCK + @" ON b.ItemCode = c.ItemCode 
	INNER JOIN {0}.dbo.OWHS AS d " + WITH_NOLOCK + @" ON b.WhsCode = d.WhsCode
	INNER JOIN {0}.dbo.OWHS AS e " + WITH_NOLOCK + @" ON a.Filler = e.WhsCode
	INNER JOIN {0}.dbo.OITB AS f " + WITH_NOLOCK + @" ON c.ItmsGrpCod = f.ItmsGrpCod
), SmmDraftHeader AS (
    SELECT a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.FromLoc, a.ToLoc, COUNT(1) AS TotalLines, SUM(a.Quantity) AS TotalQty, a.ToLocName, a.FromLocName, a.Category, a.ItmsGrpCod
    FROM SmmODRF2 AS a
    WHERE a.CANCELED = 'N' AND a.ObjType = 67 AND a.Comments LIKE 'SMM%' AND NOT EXISTS (select 1 from TransdiscrepODRF b " + WITH_NOLOCK + @" where a.DocEntry = b.DocEntry)
    GROUP BY a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.FromLoc, a.ToLoc, a.ToLocName, a.FromLocName, a.Category, a.ItmsGrpCod
    UNION
    SELECT a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.FromLoc, a.ToLoc, COUNT(1) AS TotalLines, SUM(a.DraftQuantity) AS TotalQty, a.ToLocName, a.FromLocName, a.Category, a.ItmsGrpCod
    FROM TransdiscrepODRF2 a
    WHERE EXISTS (select 1 from SmmODRF b " + WITH_NOLOCK + @" where a.DocEntry = b.DocEntry)
    GROUP BY a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.ToLoc, a.FromLoc, a.ToLocName, a.FromLocName, a.Category, a.ItmsGrpCod
    UNION
    SELECT a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.FromLoc, a.ToLoc, COUNT(1) AS TotalLines, SUM(a.DraftQuantity) AS TotalQty, a.ToLocName, a.FromLocName, a.Category, a.ItmsGrpCod
    FROM TransdiscrepODRF2 a
    WHERE a.DocEntry IN (select DocEntry from smm_TransXsap_Drafts " + WITH_NOLOCK + @")
    GROUP BY a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.ToLoc, a.FromLoc, a.ToLocName, a.FromLocName, a.Category, a.ItmsGrpCod
    UNION
    SELECT a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.FromLoc, a.ToLoc, COUNT(1) AS TotalLines, SUM(a.DraftQuantity) AS TotalQty, a.ToLocName, a.FromLocName, a.Category, a.ItmsGrpCod
    FROM TransdiscrepODRF2 a
    WHERE NOT EXISTS (select 1 from SmmODRF b " + WITH_NOLOCK + @" where a.DocEntry = b.DocEntry)
    GROUP BY a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.ToLoc, a.FromLoc, a.ToLocName, a.FromLocName, a.Category, a.ItmsGrpCod
)";
    }

    /// <summary>
    /// Returns the SQL WITH sentences to get SmmDraftDetail. It also has SmmDraft, 
    /// TransdiscrepODRF, TransdiscrepODRF2, AuditODRF, SmmODRF, SmmODRF2, TransfersAudit, 
    /// SmmDraftHeader WITH sentences, which are used on SmmDraftDetail 
    /// WITH sentence. They could be used anywhere in the query you are building.
    /// In a string.Format function, it has the following indexes:
    ///  {0} - Company Id, {1} - DocEntry
    /// </summary>
    /// <returns>string</returns>
    public static string With_SmmDraftDetail()
    {
        return @"WITH SmmDraft AS (
	SELECT o.CompanyId, o.DocEntry, o.Filler, d.WhsCode, o.DocStatus, o.DocDate, d.LineNum
	FROM SMM_ODRF o WITH(NOLOCK)
	INNER JOIN SMM_DRF1 d WITH(NOLOCK) ON o.CompanyId = d.CompanyId AND o.DocEntry = d.DocEntry
	WHERE o.CompanyId = '{0}' AND d.LineNum = 0 AND o.DocEntry = {1}
), TransdiscrepODRF AS (
	SELECT a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.FromWhsCode, a.dispatched, a.received, a.DispCompleted, a.ReceCompleted, a.InputType, a.ScanStatus, a.ToWhsCode, a.FromWhsName, a.ToWhsName, a.created_By, a.UserDispatch, a.UserReceive, a.DocEntryTraRec2, a.DocNumTraRec2, a.DocNum
	FROM smm_Transdiscrep_odrf a " + WITH_NOLOCK + @" 
	WHERE a.CompanyId = '{0}' AND a.DocEntry = {1}
), TransdiscrepODRF2 AS (
	SELECT a.CompanyId AS CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.FromWhsCode AS FromLoc, b.ToWhsCode AS ToLoc, b.LineNum AS LineNumber, c.ItmsGrpCod AS DeptCode, f.ItmsGrpNam AS DeptName, b.ItemCode, b.ItemName, b.DraftQuantity AS Qty, a.DocNum, -1 AS TargetType, b.DraftQuantity, d.WhsName AS ToLocName, e.WhsName AS FromLocName, f.ItmsGrpNam AS Category, f.ItmsGrpCod, c.U_BOT
	FROM TransdiscrepODRF AS a " + WITH_NOLOCK + @" 
    INNER JOIN smm_Transdiscrep_drf1 AS b " + WITH_NOLOCK + @" ON a.DocEntry = b.DocEntry AND a.CompanyId = b.CompanyId 
    INNER JOIN {0}.dbo.OITM AS c " + WITH_NOLOCK + @" ON b.ItemCode = c.ItemCode 
    INNER JOIN {0}.dbo.OWHS AS d " + WITH_NOLOCK + @" ON b.ToWhsCode = d.WhsCode 
    INNER JOIN {0}.dbo.OWHS AS e " + WITH_NOLOCK + @" ON a.FromWhsCode = e.WhsCode 
    INNER JOIN {0}.dbo.OITB AS f " + WITH_NOLOCK + @" ON c.ItmsGrpCod = f.ItmsGrpCod
), AuditODRF AS (
	SELECT DocEntry, FromWhsCode, ToWhsCode, isnull((select b.DocStatus from TransdiscrepODRF b where b.docentry = a.docentry and b.CompanyId = a.CompanyId),'O') as DocStatus, DocDate, UserOrigin, CompanyId
	FROM smm_odrf_audit a " + WITH_NOLOCK + @" 
	WHERE CompanyId = '{0}' AND a.DocEntry = {1}
), SmmODRF AS (
	SELECT a.CompanyId, a.DocEntry, a.DocDate, a.DocTime, a.DocStatus, a.Filler, a.CANCELED, a.ObjType, a.Comments, a.DocNum
	FROM SMM_ODRF a " + WITH_NOLOCK + @" 
    WHERE CompanyId = '{0}' AND a.DocEntry = {1}
), SmmODRF2 AS (
	SELECT a.CompanyId, a.DocEntry, CONVERT(varchar, a.DocDate, 101) + ' ' + CASE WHEN a.DocTime < 1000 THEN '0' + LEFT(CONVERT(varchar, a.DocTime), 1) + ':' + RIGHT(CONVERT(varchar, a.DocTime), 2) ELSE LEFT(CONVERT(varchar, a.DocTime), 2) + ':' + RIGHT(CONVERT(varchar, a.DocTime), 2) END AS DocDate, a.DocStatus, a.Filler AS FromLoc, b.WhsCode AS ToLoc, b.LineNum AS LineNumber, c.ItmsGrpCod AS DeptCode, f.ItmsGrpNam AS DeptName, b.ItemCode, b.Dscription, b.Quantity AS Qty, a.DocNum, b.TargetType, b.Quantity, d.WhsName AS ToLocName, e.WhsName AS FromLocName, f.ItmsGrpNam AS Category, f.ItmsGrpCod, a.CANCELED, a.ObjType, a.Comments, c.U_BOT
	FROM SmmODRF AS a 
    INNER JOIN SMM_DRF1 AS b " + WITH_NOLOCK + @" ON a.DocEntry = b.DocEntry AND a.CompanyId = b.CompanyId 
    INNER JOIN {0}.dbo.OITM AS c " + WITH_NOLOCK + @" ON b.ItemCode = c.ItemCode
    INNER JOIN {0}.dbo.OWHS AS d " + WITH_NOLOCK + @" ON b.WhsCode = d.WhsCode
    INNER JOIN {0}.dbo.OWHS AS e " + WITH_NOLOCK + @" ON a.Filler = e.WhsCode
    INNER JOIN {0}.dbo.OITB AS f " + WITH_NOLOCK + @" ON c.ItmsGrpCod = f.ItmsGrpCod
),TransfersAudit AS (
    SELECT Draft_Numero, Usuario_Originador, Usuario_Despacho, Usuario_Recibo
    FROM (
        select  
	        a.CompanyId, a.DocEntry as Draft_Numero, a.FromWhsCode as Codigo_Origen, isnull(c.FromWhsName, (select whsname from {0}.dbo.owhs where whscode = a.FromWhsCode)) as Nombre_Origen, a.ToWhsCode as Codigo_Destino, isnull(c.ToWhsName, (select whsname from {0}.dbo.owhs where whscode = a.toWhsCode)) as Nombre_Destino, isnull(a.DocStatus,'O') as Estatus, isnull(c.Dispatched, 'N') as Despachado, isnull(c.Received,'N') as Recibido, isnull((select max(UserOrigin) from AuditODRF where docentry = a.docentry),(select max(created_By) from TransdiscrepODRF where docentry = a.docentry)) as Usuario_Originador, c.UserDispatch Usuario_Despacho, c.UserReceive as Usuario_Recibo, c.DocEntryTraRec2 as Doc_Entry_Sap, c.DocNumTraRec2 Doc_Num_Sap, a.DocDate as Fecha_Originacion, b.DispatchDate as Fecha_Despacho, b.ReceiveDate as Fecha_Recibo, round(cast(dateDiff(minute,c.DocDate,b.DispatchDate) as float)/60,2) as Tiempo_Despachar, round(cast(dateDiff(minute,b.DispatchDate,b.ReceiveDate)as float)/60, 2) as Tiempo_Recibir, b.DispatchType as SistemaDes, b.ReceiveType as SistemaRec
        from (
	        select aa.CompanyId, aa.DocEntry, aa.FromWhsCode, aa.ToWhsCode, aa.DocStatus, min(aa.DocDate) as DocDate
	        from (
		        select CompanyId, DocEntry, FromWhsCode, ToWhsCode, DocStatus, DocDate
		        from TransdiscrepODRF
		        union
		        select CompanyId, DocEntry, FromWhsCode, ToWhsCode, isnull((select DocStatus from TransdiscrepODRF b where b.docentry = a.docentry and b.CompanyId = a.CompanyId), 'O') as DocStatus, DocDate
		        from AuditODRF a
		        union
		        select CompanyId, DocEntry, Filler AS FromWhsCode, WhsCode AS ToWhsCode, DocStatus, DocDate
		        from SmmDraft
	        ) aa
	        group by aa.CompanyId, aa.DocEntry, aa.FromWhsCode, aa.ToWhsCode, aa.DocStatus
        ) a
        left join smm_Transdiscrep_audit_odrf b " + WITH_NOLOCK + @" on a.DocEntry = b.DocEntry and  b.CompanyId = a.CompanyId
        left join TransdiscrepODRF c on a.DocEntry = c.DocEntry and c.CompanyId = a.CompanyId
        where not exists (
	        select 1 from smm_TransXsap_odrf_bads " + WITH_NOLOCK + @" where docentry = a.DocEntry 
	        union
	        select 1 from smm_Transdiscrep_odrf_bads " + WITH_NOLOCK + @" where docentry = a.DocEntry 
	        union
	        select 1 from drf1_bads " + WITH_NOLOCK + @" where docentry = a.DocEntry
        )
    ) t1
    WHERE t1.CompanyId = '{0}' AND t1.Draft_Numero = {1}
), SmmDraftHeader AS (
    SELECT a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.FromLoc, a.ToLoc, COUNT(1) AS TotalLines, SUM(a.Quantity) AS TotalQty, a.ToLocName, a.FromLocName, a.Category, a.ItmsGrpCod
    FROM SmmODRF2 AS a
    WHERE a.CANCELED = 'N' AND a.ObjType = 67 AND a.Comments LIKE 'SMM%' AND NOT EXISTS (select 1 from TransdiscrepODRF b " + WITH_NOLOCK + @" where a.DocEntry = b.DocEntry)
    GROUP BY a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.FromLoc, a.ToLoc, a.ToLocName, a.FromLocName, a.Category, a.ItmsGrpCod
    UNION
    SELECT a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.FromLoc, a.ToLoc, COUNT(1) AS TotalLines, SUM(a.DraftQuantity) AS TotalQty, a.ToLocName, a.FromLocName, a.Category, a.ItmsGrpCod
    FROM TransdiscrepODRF2 a
    WHERE EXISTS (select 1 from SmmODRF b " + WITH_NOLOCK + @" where a.DocEntry = b.DocEntry)
    GROUP BY a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.ToLoc, a.FromLoc, a.ToLocName, a.FromLocName, a.Category, a.ItmsGrpCod
    UNION
    SELECT a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.FromLoc, a.ToLoc, COUNT(1) AS TotalLines, SUM(a.DraftQuantity) AS TotalQty, a.ToLocName, a.FromLocName, a.Category, a.ItmsGrpCod
    FROM TransdiscrepODRF2 a
    WHERE a.DocEntry IN (select DocEntry from smm_TransXsap_Drafts " + WITH_NOLOCK + @")
    GROUP BY a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.ToLoc, a.FromLoc, a.ToLocName, a.FromLocName, a.Category, a.ItmsGrpCod
    UNION
    SELECT a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.FromLoc, a.ToLoc, COUNT(1) AS TotalLines, SUM(a.DraftQuantity) AS TotalQty, a.ToLocName, a.FromLocName, a.Category, a.ItmsGrpCod
    FROM TransdiscrepODRF2 a
    WHERE NOT EXISTS (select 1 from SmmODRF b " + WITH_NOLOCK + @" where a.DocEntry = b.DocEntry)
    GROUP BY a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.ToLoc, a.FromLoc, a.ToLocName, a.FromLocName, a.Category, a.ItmsGrpCod
), SmmDraftDetail AS (
    SELECT t1.CompanyId, t1.DocEntry, t1.DocDate, t1.FromLoc, t1.ToLoc, t1.LineNumber, t1.DeptCode, t1.DeptName, t1.ItemCode, t1.Dscription, t1.Qty, t1.DocNum, t1.DocStatus, t1.TargetType, e.Price, t1.ToLocName, t1.FromLocName, t1.U_BOT
    FROM (
        SELECT a.CompanyId, a.DocEntry, a.DocDate, a.FromLoc, a.ToLoc, a.LineNumber, a.DeptCode, a.DeptName, a.ItemCode, a.Dscription, a.Qty, a.DocNum, a.DocStatus, a.TargetType, a.ToLocName, a.FromLocName, a.U_BOT
        FROM SmmODRF2 a
        UNION
        SELECT a.CompanyId, a.DocEntry, a.DocDate, a.FromLoc, a.ToLoc, a.LineNumber, a.DeptCode, a.DeptName, a.ItemCode, a.ItemName AS Dscription, a.Qty, a.DocNum, a.DocStatus, a.TargetType, a.ToLocName, a.FromLocName, a.U_BOT
        FROM TransdiscrepODRF2 a
    ) t1
    INNER JOIN {0}.dbo.ITM1 AS e " + WITH_NOLOCK + @" ON t1.ItemCode = e.ItemCode AND e.PriceList = 1
    WHERE t1.CompanyId = '{0}' AND t1.DocEntry = {1} 
)";
    }

    /// <summary>
    /// Returns a SQL WITH sentence that replaces SMM_LAT_TRANSFERS_AUDIT_VW (which was
    /// hardcoded to a legacy company). Must be appended AFTER With_SmmDraftDetail().
    /// In a string.Format function: {0} = CompanyId.
    /// </summary>
    public static string With_LatTransfersAudit()
    {
        return @", LatTransfersAudit AS (
    SELECT
        a.DocEntry,
        ISNULL(
            (SELECT MAX(aud.UserOrigin) FROM smm_odrf_audit aud WITH(NOLOCK) WHERE aud.DocEntry = a.DocEntry AND aud.CompanyId = '{0}'),
            ISNULL(MAX(a.created_By), '_')
        ) AS DraftUser,
        ISNULL(MAX(a.UserDispatch), '_') AS DespUser,
        ISNULL(MAX(a.UserReceive), '_') AS RecUser
    FROM smm_Transdiscrep_odrf a WITH(NOLOCK)
    WHERE a.CompanyId = '{0}'
    GROUP BY a.DocEntry
)";
    }

    /// <summary>
    /// Returns the SQL WITH sentences to get TransfersAudit. It also has SmmDraft,
    /// TransdiscrepODRF, AuditODRF WITH sentences, which are used on TransfersAudit
    /// WITH sentence. They could be used anywhere in the query you are building.
    /// In a string.Format function, it has the following indexes:
    ///  {0} - Company Id, {1} - DocEntry (Send -1, if you want to bring all entries, otherwise, send the DocEntry)
    /// </summary>
    /// <returns>string</returns>
    public static string With_TransfersAudit()
    {
        return @"WITH SmmDraft AS (
	SELECT o.CompanyId, o.DocEntry, o.Filler, d.WhsCode, o.DocStatus, o.DocDate, d.LineNum
	FROM SMM_ODRF o WITH(NOLOCK)
	INNER JOIN SMM_DRF1 d WITH(NOLOCK) ON o.CompanyId = d.CompanyId AND o.DocEntry = d.DocEntry
	WHERE o.CompanyId = '{0}' AND d.LineNum = 0
    AND o.DocEntry = IIF({1} = -1, o.DocEntry, {1})
), TransdiscrepODRF AS (
	SELECT a.CompanyId, a.DocEntry, a.DocDate, a.DocStatus, a.FromWhsCode, a.dispatched, a.received, a.DispCompleted, a.ReceCompleted, a.InputType, a.ScanStatus, a.ToWhsCode, a.FromWhsName, a.ToWhsName, a.created_By, a.UserDispatch, a.UserReceive, a.DocEntryTraRec2, a.DocNumTraRec2, a.DocNum, a.DocNumITR
	FROM smm_Transdiscrep_odrf a " + WITH_NOLOCK + @"
	WHERE a.CompanyId = '{0}'
    AND a.DocEntry = IIF({1} = -1, a.DocEntry, {1})
), AuditODRF AS (
	SELECT DocEntry, FromWhsCode, ToWhsCode, isnull((select b.DocStatus from TransdiscrepODRF b where b.docentry = a.docentry and b.CompanyId = a.CompanyId),'O') as DocStatus, DocDate, UserOrigin, CompanyId
	FROM smm_odrf_audit a 
	WHERE CompanyId = '{0}'
    AND a.DocEntry = IIF({1} = -1, a.DocEntry, {1})
), TransfersAudit AS (
    SELECT CompanyId, Draft_Numero, Usuario_Originador, Usuario_Despacho, Usuario_Recibo, Codigo_Origen, Nombre_Origen, Codigo_Destino, Nombre_Destino, Estatus, Despachado, Recibido, Doc_Entry_Sap, Doc_Num_Sap, Fecha_Originacion, Fecha_Despacho, Fecha_Recibo, Tiempo_Despachar, Tiempo_Recibir, SistemaDes, SistemaRec, DocNumITR
    FROM (
        select
	        a.CompanyId, a.DocEntry as Draft_Numero, a.FromWhsCode as Codigo_Origen, isnull(c.FromWhsName, (select whsname from {0}.dbo.owhs where whscode = a.FromWhsCode)) as Nombre_Origen, a.ToWhsCode as Codigo_Destino, isnull(c.ToWhsName, (select whsname from {0}.dbo.owhs where whscode = a.toWhsCode)) as Nombre_Destino, isnull(a.DocStatus,'O') as Estatus, isnull(c.Dispatched, 'N') as Despachado, CASE WHEN isnull(c.Received,'N') NOT IN ('N','P') THEN 'Y' ELSE isnull(c.Received,'N') END as Recibido, isnull((select max(UserOrigin) from AuditODRF where docentry = a.docentry),(select max(created_By) from TransdiscrepODRF where docentry = a.docentry)) as Usuario_Originador, c.UserDispatch Usuario_Despacho, c.UserReceive as Usuario_Recibo, c.DocEntryTraRec2 as Doc_Entry_Sap, c.DocNumTraRec2 Doc_Num_Sap, a.DocDate as Fecha_Originacion, b.DispatchDate as Fecha_Despacho, b.ReceiveDate as Fecha_Recibo, round(cast(dateDiff(minute,c.DocDate,b.DispatchDate) as float)/60,2) as Tiempo_Despachar, round(cast(dateDiff(minute,b.DispatchDate,b.ReceiveDate)as float)/60, 2) as Tiempo_Recibir, b.DispatchType as SistemaDes, b.ReceiveType as SistemaRec, ISNULL(c.DocNumITR, 0) AS DocNumITR
        from (
	        select aa.CompanyId, aa.DocEntry, aa.FromWhsCode, aa.ToWhsCode, MIN(aa.DocStatus) as DocStatus, min(aa.DocDate) as DocDate
	        from (
		        select CompanyId, DocEntry, FromWhsCode, ToWhsCode, DocStatus, DocDate
		        from TransdiscrepODRF
		        union
		        select CompanyId, DocEntry, FromWhsCode, ToWhsCode, isnull((select DocStatus from TransdiscrepODRF b where b.docentry = a.docentry and b.CompanyId = a.CompanyId), 'O') as DocStatus, DocDate
		        from AuditODRF a
		        WHERE NOT EXISTS (SELECT 1 FROM TransdiscrepODRF td WHERE td.DocEntry = a.DocEntry AND td.CompanyId = a.CompanyId)
		        union
		        select CompanyId, DocEntry, Filler AS FromWhsCode, WhsCode AS ToWhsCode, DocStatus, DocDate
		        from SmmDraft
		        WHERE NOT EXISTS (SELECT 1 FROM TransdiscrepODRF td WHERE td.DocEntry = SmmDraft.DocEntry AND td.CompanyId = SmmDraft.CompanyId)
	        ) aa
	        WHERE EXISTS (
	            SELECT 1 FROM smm_Transdiscrep_drf1 d WITH(NOLOCK)
	            INNER JOIN {0}.dbo.OITM i WITH(NOLOCK) ON d.ItemCode = i.ItemCode
	            WHERE d.DocEntry = aa.DocEntry AND d.CompanyId = aa.CompanyId
	        )
	        group by aa.CompanyId, aa.DocEntry, aa.FromWhsCode, aa.ToWhsCode
        ) a
        left join smm_Transdiscrep_audit_odrf b on a.DocEntry = b.DocEntry and b.CompanyId = a.CompanyId
        left join TransdiscrepODRF c on a.DocEntry = c.DocEntry and c.CompanyId = a.CompanyId
        where not exists (
	        select 1 from smm_TransXsap_odrf_bads where docentry = a.DocEntry 
	        union
	        select 1 from smm_Transdiscrep_odrf_bads where docentry = a.DocEntry 
	        union
	        select 1 from drf1_bads where docentry = a.DocEntry
        )
    ) t1
    WHERE t1.CompanyId = '{0}' AND t1.Draft_Numero = IIF({1} = -1, t1.Draft_Numero, {1})
)";
    }

    /// <summary>
    /// Returns the SQL WITH sentences to get SmmTransferItems.
    /// In a string.Format function, it has the following indexes:
    ///  {0} - Company Id, {1} - DocEntry (Send -1, if you want to bring all entries, otherwise, send the DocEntry)
    /// </summary>
    /// <returns>string</returns>
    public static string With_SmmTransferItems()
    {
        return @"WITH SmmTransferItems AS (
    SELECT * FROM (
        select 
            b.CompanyId,
            b.DocEntry Draft_Numero, 
            b.DocDate Fecha_Originacion, 
            b.FromWhsCode Codigo_Origen, 
            b.FromWhsName Nombre_Origen, 
            b.ToWhsCode Codigo_Destino, 
            b.ToWhsName Nombre_Destino, 
            b.DocStatus Estatus, 
            b.Dispatched , 
            b.Received, 
            b.DocEntryTraRec2, 
            b.UserDispatch, 
            b.UserReceive, 
            b.Date_Created, 
            b.Created_By,
            a.LineNum, 
            a.ItemCode, 
            a.ItemName, 
            a.DraftQuantity, 
            a.DispatchQuantity, 
            a.ReceivedQuantity, 
            a.Price
        from 
            smm_Transdiscrep_drf1 a " + WITH_NOLOCK + @",
            smm_Transdiscrep_odrf b " + WITH_NOLOCK + @"
        where
            a.CompanyId = b.CompanyId
            and a.docentry = b.DocEntry
    ) T
    WHERE T.CompanyId = '{0}' AND T.Draft_Numero = IIF({1} = -1, T.Draft_Numero, {1})
)";
    }

    /// <summary>
    /// Returns the SQL WITH sentences to get SmmTranSapDRF1. It also has SmmIntransitOut, 
    /// WITH sentence. It could be used anywhere in the query you are building.
    /// In a string.Format function, it has the following indexes:
    ///  {0} - Company Id, {1} - DocEntry (Send -1, if you want to bring all entries, otherwise, send the DocEntry)
    /// </summary>
    /// <returns>string</returns>
    public static string With_SmmTranSapDRF1()
    {
        return @"WITH SmmIntransitOut AS (
SELECT FromLoc, itemcode, Qty_intransit_out
FROM (
    select a.CompanyId, a.FromLoc, a.ItemCode, SUM(a.Qty_intransit) as Qty_intransit_out
    from
    (
        select sh.CompanyId, sh.FromWhsCode AS FromLoc, sd.ToWhsCode AS ToLoc, sd.ItemCode, sum(isnull(sd.dispatchquantity,0)) AS Qty_intransit
	    from dbo.smm_Transdiscrep_odrf sh " + WITH_NOLOCK + @" inner join dbo.smm_Transdiscrep_drf1 sd " + WITH_NOLOCK + @" ON sh.CompanyId = sd.CompanyId and sh.docentry = sd.DocEntry
        where sh.CompanyId = '{0}' AND (sh.DocStatus = 'O' or (sh.DocStatus = 'C' and (isnull(DocEntryTraRec2,0) +  isnull(DocEntryTraRec,0) = 0)))
	    group by sh.CompanyId, sh.FromWhsCode, sd.ToWhsCode, sd.ItemCode
        Union All
        SELECT odrf.CompanyId, odrf.Filler AS FromLoc, drf1.WhsCode AS ToLoc, drf1.ItemCode, SUM(drf1.Quantity) AS Qty_intransit
        FROM 
        SMM_ODRF AS odrf  " + WITH_NOLOCK + @" 
        INNER JOIN dbo.SMM_DRF1 AS drf1  " + WITH_NOLOCK + @" ON odrf.CompanyId = drf1.CompanyId and odrf.DocEntry = drf1.DocEntry
        WHERE odrf.CompanyId = '{0}' and (odrf.ObjType = 67) AND (odrf.DocStatus = 'O') AND (odrf.CANCELED = 'N')
	        and not exists (select 1 from smm_Transdiscrep_odrf xx " + WITH_NOLOCK + @" where xx.CompanyId = odrf.CompanyId and xx.docentry = odrf.docentry)
        GROUP BY odrf.CompanyId, odrf.Filler, drf1.WhsCode, drf1.ItemCode
        Union All
        SELECT odrf.CompanyId, odrf.FromWhsCode AS FromLoc, drf1.ToWhsCode AS ToLoc, drf1.ItemCode, SUM(drf1.TmpQuantity) AS Qty_intransit
        FROM smm_TransXsap_odrf AS odrf  " + WITH_NOLOCK + @"
        INNER JOIN smm_TransXsap_drf1 AS drf1  " + WITH_NOLOCK + @" ON odrf.DocEntry = drf1.DocEntry and odrf.CompanyId = drf1.CompanyId 
        WHERE not exists (
            select 1 from smm_Transdiscrep_odrf xx " + WITH_NOLOCK + @" where xx.CompanyId = odrf.CompanyId and xx.docentry = odrf.docentry
		    union
		    select 1 from smm_odrf xx " + WITH_NOLOCK + @" where CompanyId = odrf.CompanyId and xx.docentry = odrf.docentry
	    )
        GROUP BY odrf.CompanyId, odrf.FromWhsCode, drf1.ToWhsCode, drf1.ItemCode
    ) A
    group by a.CompanyId, a.FromLoc, a.ItemCode
) T
WHERE T.CompanyId = '{0}'
), SmmTranSapDRF1 AS (
    select zz.CompanyId, zz.whscode, zz.LineNum, zz.ItemCode, zz.ItemName, 
    convert(int,zz.onhand + (zz.DraftQuantity)*zz.fac) as onhand, convert(int,zz.DraftQuantity) as DraftQuantity, 
    zz.DocEntry, zz.fac  
    from
    (
        select xx.CompanyId, xx.whscode, xx.LineNum, xx.ItemCode, xx.ItemName, xx.onhand-isnull(yy.qty,0) as onhand,xx.DraftQuantity, xx.DocEntry, xx.fac  
        from
        (SELECT a.CompanyId as CompanyId, b.whscode, a.LineNum, a.ItemCode, a.ItemName, b.onhand, a.DraftQuantity, a.DocEntry , 1 as fac
        FROM smm_TransXsap_drf1 a " + WITH_NOLOCK + @", {0}.dbo.oitw b " + WITH_NOLOCK + @", smm_transxsap_odrf c " + WITH_NOLOCK + @"
        WHERE a.itemcode = b.itemcode and a.docentry = c.docentry and a.CompanyId = c.CompanyId and b.whscode = c.fromwhscode and c.CompanyId = '{0}'
            and not exists (select 1 from smm_transdiscrep_odrf f " + WITH_NOLOCK + @" where f.CompanyId = a.CompanyId and f.docentry = a.docentry) 
        union
        SELECT '{0}' as CompanyId, b.whscode, a.LineNum, a.ItemCode, a.ItemName, b.onhand, a.DraftQuantity, a.DocEntry, 0 as fac 
        FROM smm_Transdiscrep_drf1 a " + WITH_NOLOCK + @", {0}.dbo.oitw b " + WITH_NOLOCK + @", smm_Transdiscrep_odrf c " + WITH_NOLOCK + @"
        WHERE a.itemcode = b.itemcode and a.docentry = c.docentry and a.CompanyId = c.CompanyId and b.whscode = c.fromwhscode and c.CompanyId = '{0}'
        ) xx left join
        (select FromLoc as filler, itemcode, Qty_intransit_out as qty
          from SmmIntransitOut " + WITH_NOLOCK + @"
        ) yy
        on xx.whscode = yy.filler and xx.itemcode = yy.itemcode
    ) zz 
    where convert(int,zz.onhand + zz.DraftQuantity)>0
)";
    }

    /// <summary>
    /// Returns the SQL WITH sentences to get SmmTransfersDetails. It also has SmmOdrfTransfers,
    /// SmmTransdiscrepTransfers, SmmTransfers WITH sentences. 
    /// They could be used anywhere in the query you are building.
    /// In a string.Format function, it has the following indexes:
    ///  {0} - Company Id
    /// </summary>
    /// <returns>string</returns>
    public static string With_SmmTransfersDetails()
    {
        return @"WITH SmmOdrfTransfers AS (
SELECT a.CompanyId, a.DocEntry, a.DocStatus, a.DocDate, a.Filler As FromWhsCode, c.WhsName As FromWhsName, b.WhsCode As ToWhsCode, d.WhsName as ToWhsName, 'N' as Dispatched, NULL as DispCompleted, 'N' as Received, NULL ReceCompleted, NUll as DocEntryTraDis, NULL as DocEntryTraRec, NULL as DocEntryTraRec2, NULL as UserDispatch, NULL as UserReceive, a.CreateDate, NULL as Created_By, NULL as InputType, NULL as ScanStatus, NULL as UserRecScanner, b.LineNum, b.ItemCode, e.itemName, e.ItmsGrpCod, ee.ItmsGrpNam, b.Quantity as DraftQuantity, 0 DispatchQuantity, 0 as ReceivedQuantity, b.Price, f.OnHand AS FromWhsCode_OnHand, g.OnHand AS ToWhsCode_OnHand 
FROM SMM_ODRF AS a " + WITH_NOLOCK + @" 
INNER JOIN SMM_DRF1 AS b " + WITH_NOLOCK + @" ON a.CompanyId = b.CompanyId and a.DocEntry = b.DocEntry
INNER JOIN {0}..OWHS AS c " + WITH_NOLOCK + @" ON a.Filler = c.WhsCode 
INNER JOIN {0}..OWHS AS d " + WITH_NOLOCK + @" ON b.WhsCode = d.WhsCode 
INNER JOIN {0}..OITM AS e " + WITH_NOLOCK + @" ON b.ItemCode = e.ItemCode 
INNER JOIN {0}..OITB AS ee " + WITH_NOLOCK + @" ON e.ItmsGrpCod = ee.ItmsGrpCod
INNER JOIN {0}..OITW AS f " + WITH_NOLOCK + @" ON f.whscode = a.Filler and f.itemcode = b.ItemCode
INNER JOIN {0}..OITW AS g " + WITH_NOLOCK + @" ON g.whscode = b.WhsCode and g.itemcode = b.ItemCode 
WHERE a.CompanyId = '{0}' and Not exists (select 1 from smm_Transdiscrep_odrf aa " + WITH_NOLOCK + @" where aa.CompanyId = a.CompanyId and aa.docentry = a.DocEntry)
), 
SmmTransdiscrepTransfers AS (
SELECT a.CompanyId, a.DocEntry, a.DocStatus, a.DocDate, a.FromWhsCode, a.FromWhsName, a.ToWhsCode, a.ToWhsName,  a.Dispatched, a.DispCompleted, a.Received, a.ReceCompleted, a.DocEntryTraDis, a.DocEntryTraRec, a.DocEntryTraRec2, a.UserDispatch, a.UserReceive, a.Date_Created, a.Created_By, a.InputType, a.ScanStatus, a.UserRecScanner, b.LineNum, b.ItemCode, b.ItemName,  e.ItmsGrpCod, ee.ItmsGrpNam, b.DraftQuantity, b.DispatchQuantity, b.ReceivedQuantity, b.Price, f.OnHand AS FromWhsCode_OnHand, g.OnHand AS ToWhsCode_OnHand 
FROM smm_Transdiscrep_odrf AS a " + WITH_NOLOCK + @" 
INNER JOIN smm_Transdiscrep_drf1 AS b " + WITH_NOLOCK + @" ON a.CompanyId = b.CompanyId and a.DocEntry = b.DocEntry
INNER JOIN {0}..OITM AS e " + WITH_NOLOCK + @" ON b.ItemCode = e.ItemCode 
INNER JOIN {0}..OITB AS ee " + WITH_NOLOCK + @" ON e.ItmsGrpCod = ee.ItmsGrpCod 
INNER JOIN {0}..OITW AS f " + WITH_NOLOCK + @" ON f.whscode = a.FromWhsCode and f.itemcode = b.ItemCode
INNER JOIN {0}..OITW AS g " + WITH_NOLOCK + @" ON g.whscode = a.ToWhsCode and g.itemcode = b.ItemCode 
WHERE a.CompanyId = '{0}'
),
SmmTransfers AS (
SELECT CompanyId, DocEntry, DocStatus, DocDate, FromWhsCode, FromWhsName, ToWhsCode, ToWhsName, Dispatched, DispCompleted, Received, ReceCompleted, DocEntryTraDis, DocEntryTraRec, DocEntryTraRec2, UserDispatch, UserReceive, CreateDate, Created_By, InputType, ScanStatus, UserRecScanner, LineNum, ItemCode, itemName, ItmsGrpCod, ItmsGrpNam, DraftQuantity, DispatchQuantity, ReceivedQuantity, Price, FromWhsCode_OnHand, ToWhsCode_OnHand 
FROM (
    SELECT * FROM SmmOdrfTransfers " + WITH_NOLOCK + @"
    UNION
    SELECT * FROM SmmTransdiscrepTransfers " + WITH_NOLOCK + @"
) t
), 
SmmTransfersDetails AS (
select CompanyId as company, DocEntry as transfer, ItemCode as item, itemName, ItmsGrpCod as Category, ItmsGrpNam As CategoryName, FromWhsCode as from_loc, FromWhsName AS from_locName, FromWhsCode_OnHand AS from_loc_soh, ToWhsCode as to_loc, ToWhsName AS to_locName, ToWhsCode_OnHand AS to_loc_soh, 
isnull(case when DocStatus = 'O' then case when isnull(Dispatched,'N') = 'N' then DraftQuantity else 0 end end, 0) AS Reserved, 
isnull(case when DocStatus = 'O' then case when isnull(Dispatched,'N') = 'Y' then DispatchQuantity else 0 end end, 0) AS In_Transit, 
isnull(case when DocStatus = 'C' then case when isnull(Received,'N') = 'Y' then ReceivedQuantity else 0 end end, 0) AS Received,
DocStatus As Transfer_OpenClose, DocDate As TransferDate, 
Case when isnull(Dispatched, 'N') = 'N' 
then 'In_Draft' 
else 
    Case when isnull(Dispatched, 'Y') = 'Y' 
    then 
        Case when isnull(Received,'N') = 'N' 
        then 'In_Transit' 
        else 'Received' 
        end 
    end 
end AS Transfer_Status,UserDispatch, UserReceive
from SmmTransfers " + WITH_NOLOCK + @"
)";
    }

    /// <summary>
    /// Returns the SQL WITH sentences to get SmmReserved. It also has every WITH 
    /// sentences from the function With_SmmTransfersDetails(). 
    /// They could be used anywhere in the query you are building.
    /// In a string.Format function, it has the following indexes:
    ///  {0} - Company Id
    /// </summary>
    /// <returns>string</returns>
    public static string With_SmmReserved()
    {
        return With_SmmTransfersDetails() + @", 
SmmReserved AS (
select Company, from_loc loc, item, sum(reserved) AS reserved
from SmmTransfersDetails " + WITH_NOLOCK + @"
GROUP BY Company, from_loc, Item
having sum(reserved) > 0
)";
    }

    /// <summary>
    /// Extends With_WmsWhsItemBin to also include items present in OITW for the warehouse
    /// but not yet assigned a BIN (bin = NULL). Used when browsing by category so the user
    /// can assign bins to unassigned items. Unassigned items sort after assigned ones.
    /// </summary>
    public static string With_WmsWhsItemBinFull(string companyId)
    {
        return @"(SELECT w.WhsCode AS whscode, o.WhsName AS whsname, w.ItemCode AS itemcode,
                         i.ItemName AS itemname, i.ItmsGrpCod AS itmsgrpcod, g.ItmsGrpNam AS itmsgrpnam,
                         i.U_Brand AS u_brand, w.BIN AS bin
                  FROM WMS_Whs_Item_Bin w " + WITH_NOLOCK + @"
                  INNER JOIN " + companyId + @".dbo.OWHS o " + WITH_NOLOCK + @" ON w.WhsCode = o.WhsCode
                  INNER JOIN " + companyId + @".dbo.OITM i " + WITH_NOLOCK + @" ON w.ItemCode = i.ItemCode
                  INNER JOIN " + companyId + @".dbo.OITB g " + WITH_NOLOCK + @" ON i.ItmsGrpCod = g.ItmsGrpCod
                  UNION ALL
                  SELECT iw.WhsCode, o.WhsName, iw.ItemCode, i.ItemName, i.ItmsGrpCod, g.ItmsGrpNam, i.U_Brand, NULL AS bin
                  FROM " + companyId + @".dbo.OITW iw " + WITH_NOLOCK + @"
                  INNER JOIN " + companyId + @".dbo.OWHS o " + WITH_NOLOCK + @" ON iw.WhsCode = o.WhsCode
                  INNER JOIN " + companyId + @".dbo.OITM i " + WITH_NOLOCK + @" ON iw.ItemCode = i.ItemCode
                  INNER JOIN " + companyId + @".dbo.OITB g " + WITH_NOLOCK + @" ON i.ItmsGrpCod = g.ItmsGrpCod
                  WHERE i.Canceled = 'N'
                    AND NOT EXISTS (
                        SELECT 1 FROM WMS_Whs_Item_Bin w2 " + WITH_NOLOCK + @"
                        WHERE w2.WhsCode = iw.WhsCode AND w2.ItemCode = iw.ItemCode
                    )
                 ) AS wib";
    }

    /// <summary>
    /// Replaces Wms_Whs_Item_Bin_vw, which had NEWPCANA hardcoded.
    /// Returns a derived-table subquery aliased as [wib] joining WMS_Whs_Item_Bin
    /// with OWHS, OITM and OITB from the given company database.
    /// </summary>
    public static string With_WmsWhsItemBin(string companyId)
    {
        return @"(SELECT w.WhsCode AS whscode, o.WhsName AS whsname, w.ItemCode AS itemcode,
                         i.ItemName AS itemname, i.ItmsGrpCod AS itmsgrpcod, g.ItmsGrpNam AS itmsgrpnam,
                         i.U_Brand AS u_brand, w.BIN AS bin
                  FROM WMS_Whs_Item_Bin w " + WITH_NOLOCK + @"
                  INNER JOIN " + companyId + @".dbo.OWHS o " + WITH_NOLOCK + @" ON w.WhsCode = o.WhsCode
                  INNER JOIN " + companyId + @".dbo.OITM i " + WITH_NOLOCK + @" ON w.ItemCode = i.ItemCode
                  INNER JOIN " + companyId + @".dbo.OITB g " + WITH_NOLOCK + @" ON i.ItmsGrpCod = g.ItmsGrpCod) AS wib";
    }

    /// <summary>
    /// Returns the SQL WITH sentences to get NetInventory.
    /// In a string.Format function, it has the following indexes:
    ///  {0} - Company Id
    /// </summary>
    /// <returns>string</returns>
    public static string With_NetInventory()
    {
        return @"WITH NetInventory AS (
    SELECT '{0}' AS CompanyId, a.locked, a.ItemCode, a.WhsCode, ISNULL(a.OnHand, 0) AS OnHand, a.OnOrder, ISNULL(b.in_transit, 0) + ISNULL(d.in_transit, 0) + ISNULL(In_Transit_Colon,0) AS in_transit_in, ISNULL(c.in_transit, 0) + ISNULL(e.in_transit, 0) AS in_transit_out, ISNULL(a.OnHand, 0) + ISNULL(b.in_transit, 0) + ISNULL(d.in_transit, 0) + ISNULL(In_Transit_Colon,0) - ISNULL(c.in_transit, 0) - ISNULL(e.in_transit, 0) AS net_inventory, ISNULL(d.in_transit, 0) AS dintra, ISNULL(e.in_transit, 0) As eIntra, In_Transit_Colon
    FROM  {0}.dbo.OITW AS a " + WITH_NOLOCK + @" 
    LEFT OUTER JOIN (
        SELECT drf1.WhsCode AS ToLoc, drf1.ItemCode, SUM(drf1.Quantity) AS in_transit
        FROM SMM_ODRF AS odrf " + WITH_NOLOCK + @" 
        INNER JOIN SMM_DRF1 AS drf1 " + WITH_NOLOCK + @" ON odrf.DocEntry = drf1.DocEntry and odrf.CompanyId = drf1.CompanyId and drf1.CompanyId = '{0}'
        WHERE (odrf.ObjType = 67) AND (odrf.DocStatus = 'O') AND (odrf.CANCELED = 'N')
        GROUP BY  drf1.WhsCode, drf1.ItemCode
    ) AS b ON a.ItemCode = b.ItemCode AND a.WhsCode = b.ToLoc 
    LEFT OUTER JOIN (
        SELECT odrf.Filler AS FromLoc, drf1.ItemCode, SUM(drf1.Quantity) AS in_transit
        FROM SMM_ODRF AS odrf " + WITH_NOLOCK + @"
        INNER JOIN SMM_DRF1 AS drf1 " + WITH_NOLOCK + @" ON odrf.DocEntry = drf1.DocEntry and odrf.CompanyId = drf1.CompanyId and drf1.CompanyId = '{0}'
        WHERE (odrf.ObjType = 67) AND (odrf.DocStatus = 'O') AND (odrf.CANCELED = 'N')
        GROUP BY odrf.Filler, drf1.ItemCode
    ) AS c 
       ON a.ItemCode = c.ItemCode AND a.WhsCode = c.FromLoc 
    LEFT OUTER JOIN (
        select sd.ToWhsCode AS ToLoc, sd.ItemCode, sum(isnull(sd.dispatchquantity,0)) AS in_transit
	    from smm_Transdiscrep_odrf sh " + WITH_NOLOCK + @"
        inner join smm_Transdiscrep_drf1 sd " + WITH_NOLOCK + @" on sh.docentry = sd.DocEntry and sh.CompanyId = sd.CompanyId and sd.CompanyId = '{0}'
	    where sh.DocStatus = 'O' and sh.docentry in (select docentry from dbo.smm_TransXsap_Drafts " + WITH_NOLOCK + @")
	    group by sd.ToWhsCode, sd.ItemCode
    ) AS d
    ON a.ItemCode = d.ItemCode AND a.WhsCode = d.ToLoc 
    LEFT OUTER JOIN (
	    select sh.FromWhsCode AS FromLoc, sd.ItemCode, sum(isnull(sd.dispatchquantity,0)) AS in_transit
	    from smm_Transdiscrep_odrf sh " + WITH_NOLOCK + @" 
        inner join smm_Transdiscrep_drf1 sd " + WITH_NOLOCK + @" on sh.docentry = sd.DocEntry and sh.CompanyId = sd.CompanyId and sd.CompanyId = '{0}'
	    where sh.DocStatus = 'O' and sh.docentry in (select docentry from dbo.smm_TransXsap_Drafts)
	    group by sh.FromWhsCode, sd.ItemCode
    ) AS e ON a.ItemCode = e.ItemCode AND a.WhsCode = e.FromLoc 
    LEFT OUTER JOIN (
        select b.CompanyId, a.CardCode, a.WhsCode, a.ItemCode, a.Quantity AS In_Transit_Colon 
        from smm_in_transit_from_colon_vw a " + WITH_NOLOCK + @", Smm_CustomerCardCode_CompWhs_Xref b " + WITH_NOLOCK + @" 
        where a.WhsCode = b.WhsCode and a.CardCode = b.CustomerCardCode and b.CompanyId = '{0}'
    ) AS f ON f.ItemCode = a.ItemCode AND f.WhsCode = a.WhsCode 
    where a.locked <> 'Y'
)";
    }
}