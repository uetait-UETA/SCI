/*
  Fix: submit_DraftXsap descuenta dos veces el stock de transferencias que ya tienen ITR (OWTQ) en SAP.

  Problema:
    El SP calcula Inventory = OnHand - IsCommited - Qty_intransit_out.
    Un ITR abierto en SAP compromete el stock (OITW.IsCommited) y SCI tambien lo cuenta
    como "en transito" (smm_Transdiscrep_odrf/drf1). La misma unidad se resta dos veces,
    Inventory queda <= 0, no se insertan lineas (log TP7), la draft se borra y la pantalla
    dice "Draft XXXX completed." sin crear el ITR.
    Ej. (PRO_DFSC, 1161, item 18334326): OnHand 4, IsCommited 3 (ITR #662, #1126),
    en transito SCI 3 -> 4 - 3 - 3 = -2 (real: 1).

  Cambio:
    En InTransitOutSubmitCheck, la rama de smm_Transdiscrep_odrf excluye las transferencias
    con DocEntryITR > 0 (su stock ya esta en IsCommited). Nada mas cambia.

  Aplicar en: SMM_PROD y SMM_DFC (misma definicion), y en la BD SMM de produccion.
  SMM_LAX / SMM_SFO tienen una version de 2022: revisar antes de aplicar.
*/
-- USE SMM_PROD;   -- <-- cambiar a la BD correspondiente
GO
ALTER PROCEDURE [dbo].[submit_DraftXsap]
    @CompanyId nvarchar(30),
	@DocEntry int,
    @userDraft nvarchar(50)
as
declare @ErrMsg nvarchar(4000), @ErrSeverity int, @lcount int, @lMsg nvarchar(4000), @FromWhsCode varchar(50)
DECLARE @SQL_EX NVARCHAR(MAX);

BEGIN TRY
 set @lMsg = 'TP1 Company: ' + @CompanyId + ' Docentry: ' +convert(varchar,@DocEntry)
 print @lMsg
 exec [dbo].[SMM_LOGTRACE_PRC] 'submit_DraftXsap', @lMsg

select 
	 @FromWhsCode = FromWhsCode 
	from smm_TransXsap_odrf with (nolock) 
	where CompanyId = @CompanyId and docentry = @DocEntry

if object_id('tempdb..#smm_Inventory_Available_submitcheck') IS NOT NULL
		DROP TABLE #smm_Inventory_Available_submitcheck
  
    Create table #smm_Inventory_Available_submitcheck
	(CompanyId varchar(30), 
	 whscode varchar(8), 
	 ItemCode varchar(50), 
	 Qty_intransit_out int, 
	 OnHand int, 
	 isCommited int, 
	 OnOrder int, 
	 Consig int, 
	 Inventory int
	)
	
	DECLARE @TXT1 NVARCHAR(50);
	
	SET @TXT1 = 'TP2 - ' + @CompanyId;
	exec [dbo].[SMM_LOGTRACE_PRC] 'submit_DraftXsap', @TXT1;
	   

	   SET @SQL_EX = N'
WITH InTransitOutSubmitCheck AS (
	SELECT a.CompanyId, a.FromLoc, a.ItemCode, SUM(a.Qty_intransit) AS Qty_intransit_out
	FROM (
		SELECT sh.CompanyId, sh.FromWhsCode AS FromLoc, sd.ToWhsCode AS ToLoc, sd.ItemCode, sum(isnull(sd.dispatchquantity,0)) AS Qty_intransit
		FROM smm_Transdiscrep_odrf sh with(nolock) 
		INNER JOIN smm_Transdiscrep_drf1 sd with(nolock) ON sh.CompanyId = sd.CompanyId AND sh.docentry = sd.DocEntry
		WHERE sh.CompanyId = ''' + @CompanyId + N''' AND (sh.DocStatus = ''O'' OR (sh.DocStatus = ''C'' AND (isnull(sh.DocEntryTraRec2,0) + isnull(sh.DocEntryTraRec,0) = 0)))
		AND ISNULL(sh.DocEntryITR,0) = 0   -- FIX: ITR ya comprometido en OITW.IsCommited; no contarlo dos veces
		GROUP BY sh.CompanyId, sh.FromWhsCode, sd.ToWhsCode, sd.ItemCode
		UNION ALL
		SELECT odrf.CompanyId, odrf.Filler AS FromLoc, drf1.WhsCode AS ToLoc, drf1.ItemCode, SUM(drf1.Quantity) AS Qty_intransit
		FROM SMM_ODRF AS odrf with(nolock) 
		INNER JOIN SMM_DRF1 AS drf1 with(nolock) ON odrf.CompanyId = drf1.CompanyId AND odrf.DocEntry = drf1.DocEntry
		WHERE odrf.CompanyId = ''' + @CompanyId + N''' AND (odrf.ObjType = 67) AND (odrf.DocStatus = ''O'') AND (odrf.CANCELED = ''N'')
		AND NOT EXISTS (
			SELECT 1 
			FROM smm_Transdiscrep_odrf xx with(nolock) 
			WHERE xx.CompanyId = odrf.CompanyId AND xx.docentry = odrf.docentry
		)
		GROUP BY odrf.CompanyId, odrf.Filler, drf1.WhsCode, drf1.ItemCode
	) A
	GROUP BY a.CompanyId, a.FromLoc, a.ItemCode
), InventoryAvaiableSubmitCheck AS (
	SELECT ''' + @CompanyId + N''' CompanyId, b.whscode, b.ItemCode, ISNULL(a.Qty_intransit_out,0) Qty_intransit_out, b.OnHand, isCommited, OnOrder, Consig, (isnull(b.OnHand,0) - isnull(b.isCommited,0) - isnull(a.Qty_intransit_out,0)) as Inventory
	FROM ' + @CompanyId + N'..OITW b WITH(NOLOCK) 
	LEFT JOIN InTransitOutSubmitCheck a WITH(NOLOCK) ON b.whscode = a.fromloc AND b.ItemCode = a.ItemCode AND a.CompanyId = ''' + @CompanyId + N'''
)
SELECT CompanyId, whscode, ItemCode, Qty_intransit_out, OnHand, isCommited, OnOrder, Consig, Inventory 
FROM InventoryAvaiableSubmitCheck WITH(NOLOCK)
WHERE CompanyID = ''' + @CompanyId + N''' AND whscode = ''' + @FromWhsCode + N''' AND inventory > 0
';

		insert into #smm_Inventory_Available_submitcheck 
		(CompanyId, whscode, ItemCode, Qty_intransit_out, OnHand, isCommited, OnOrder, Consig, Inventory) 
		EXEC(@SQL_EX);

--IF (@CompanyId = 'PUNTA_CANA')
--BEGIN
--       exec [dbo].[SMM_LOGTRACE_PRC] 'submit_DraftXsap', 'TP2 - PUNTA_CANA'
--	   insert into #smm_Inventory_Available_submitcheck
--	   (CompanyId, whscode, ItemCode, Qty_intransit_out, OnHand, isCommited, OnOrder, Consig, Inventory) 
--		select CompanyId, whscode, ItemCode, Qty_intransit_out, OnHand, isCommited, OnOrder, Consig, Inventory
--		from [dbo].[smm_Inventory_Available_submitcheck_DFATOCUMEN_vw] with (nolock)
--		where CompanyID = @CompanyId
--		and whscode = @FromWhsCode
--		and inventory > 0
--END


exec [dbo].[SMM_LOGTRACE_PRC] 'submit_DraftXsap', 'TP3'
BEGIN TRANSACTION

	insert into smm_Transdiscrep_odrf with (rowlock) 
	(CompanyId, DocEntry, DocNum, DocDate, FromWhsCode, FromWhsName, ToWhsCode, ToWhsName, DocStatus, Dispatched, DispCompleted, Received, ReceCompleted, DocEntryTraDis, DocNumTraDis, DocEntryTraRec, DocNumTraRec, DocEntryTraRec2, DocNumTraRec2, UserDispatch, UserReceive, Date_Created, Created_By)
	select 
	 CompanyId, DocEntry, DocNum, DocDate, FromWhsCode, FromWhsName, ToWhsCode, ToWhsName, 'O',       'N',       NULL,            'N',    NULL,          NULL,           NULL,          NULL,         NULL,         NULL,            NULL,          NULL,         NULL,        getdate(), @userDraft
	from smm_TransXsap_odrf with (nolock) 
	where CompanyId = @CompanyId AND docentry = @DocEntry


exec [dbo].[SMM_LOGTRACE_PRC] 'submit_DraftXsap', 'TP4'

	insert into smm_Transdiscrep_drf1 with (rowlock) 
	(CompanyId, DocEntry, LineNum, DocNum, ToWhsCode, ToWhsName, ItemCode, ItemName, 
	DraftQuantity, 
	DispatchQuantity, 
	ReceivedQuantity, Price, 
	TmpQuantity,      
	Date_Created, Created_By)
	select 
	 c.CompanyId, a.DocEntry, a.LineNum, a.DocNum, a.ToWhsCode, a.ToWhsName, a.ItemCode, a.ItemName, 
	 case when a.DraftQuantity > c.Inventory then c.Inventory else a.DraftQuantity end DraftQuantity, 
	 case when a.DraftQuantity > c.Inventory then c.Inventory else a.DraftQuantity end DispatchQuantity, 
	 ReceivedQuantity, Price, 
	  case when a.DraftQuantity > c.Inventory then c.Inventory else a.DraftQuantity end TmpQuantity , 
	 getdate(), @userDraft
	from smm_TransXsap_drf1 a with (nolock) 
	inner join smm_TransXsap_odrf b with (nolock) on a.CompanyId = b.CompanyId and a.DocEntry = b.DocEntry 
	inner join #smm_Inventory_Available_submitcheck c on b.CompanyId = c.CompanyId COLLATE DATABASE_DEFAULT	and b.FromWhsCode = c.whscode COLLATE DATABASE_DEFAULT and a.ItemCode = c.ItemCode COLLATE DATABASE_DEFAULT		 
	where c.CompanyId = @CompanyId and a.docentry = @DocEntry


exec [dbo].[SMM_LOGTRACE_PRC] 'submit_DraftXsap', 'TP5'

select @lcount = count(1) from smm_Transdiscrep_drf1 with (nolock)
where CompanyId = @CompanyId and docentry = @DocEntry

if (@lcount > 0)
Begin
  exec [dbo].[SMM_LOGTRACE_PRC] 'submit_DraftXsap', 'TP6'
	insert into smm_TransXsap_Drafts with (rowlock) 
	(DocEntry, DocNum, UserDraft,  Date_Created, Created_By)
	select 
	 DocEntry, DocNum, @userDraft, getdate(), @userDraft
	from smm_TransXsap_odrf with (nolock) 
	where CompanyId = @CompanyId and docentry = @DocEntry
End
Else
Begin
  exec [dbo].[SMM_LOGTRACE_PRC] 'submit_DraftXsap', 'TP7'
	delete smm_Transdiscrep_odrf
	where CompanyId = @CompanyId and docentry = @DocEntry
End

COMMIT TRANSACTION

END TRY
BEGIN CATCH

-- Whoops, there was an error
  IF @@TRANCOUNT > 0
  Begin
     ROLLBACK TRANSACTION

	  -- Raise an error with the details of the exception
  
	  SELECT @ErrMsg = 'TP8 Error en Procedure submit_DraftXsap: ' + ERROR_MESSAGE(),
			 @ErrSeverity = ERROR_SEVERITY()

	  exec [dbo].[SMM_LOGTRACE_PRC] 'submit_DraftXsap',  @ErrMsg

	  --RAISERROR ('Error en Procedure submit_DraftXsap', 16,1) 
	  RAISERROR(@ErrMsg, @ErrSeverity, 1)

  End

END CATCH
GO
