using System;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.Web;


/// <summary>
/// Summary description for Transfer
/// </summary>
public class Transfer
{
    protected SqlDb db;
    protected string sap_db;
    protected string whs_code;
    
    public Transfer()
    {
        db = new SqlDb();
        db.Connect();

        HttpContext ctx = HttpContext.Current;
        sap_db = (ctx != null && ctx.Session != null) ? ctx.Session["CompanyId"] as string : null;
        if (string.IsNullOrEmpty(sap_db)) sap_db = ConfigurationManager.AppSettings.Get("smm_db");
        whs_code = ConfigurationManager.AppSettings.Get("whs_code");
    }


    public DataTable GetTransferDrafts(string statusDoc, string txtDocNum, string  FromDateTxt, string toDateTxt, string fromLocTxt, string toLocTxt, string categoryTxt, string andOr1, string andOr2, string andOr3, string companyId, int branchId = 0)
    {
        sap_db = companyId;
        DataTable dt = new DataTable();
        string sql = "";
        int dummy = 0;
        int Lflag1 = 0;
        string sDate = null;
        string sLoc = null;
        string sStatus = null;
        string sDocNum = null;

        try
        {
            sql = Queries.With_SmmDraftHeader() + @"
    SELECT t1.*, isnull(t2.dispatched,'P') dispatched, isnull(t2.received,'P') received, isnull(t2.DispCompleted,'P') DispCompleted, isnull(t2.ReceCompleted,'P') ReceCompleted, isnull(substring(t2.InputType,1,1),'I') InputType, isnull(substring(t2.ScanStatus,1,1),'N') ScanStatus, ISNULL(t2.DocNumITR,0) DocNumITR,
           CASE ISNULL(t2.TransferType,'') WHEN 'APRI' THEN 'APRI' WHEN 'SO' THEN 'SO' ELSE CASE WHEN ISNULL(t2.DocNumITR,0)>0 THEN 'ITR' ELSE '' END END SapDocType
    FROM SmmDraftHeader t1 " + Queries.WITH_NOLOCK + @"
    LEFT JOIN TransdiscrepODRF t2 " + Queries.WITH_NOLOCK + @"  ON t1.docentry = t2.docentry AND t1.CompanyId = t2.CompanyId
    WHERE t1.CompanyId = '{0}' AND ";

            sql = string.Format(sql, sap_db);

            ///---FromDateTxt

            if (string.IsNullOrEmpty(FromDateTxt) && string.IsNullOrEmpty(toDateTxt))
            {
                if (andOr1 == "OR")
                {
                    sDate = " (1=2) ";
                }
                else
                {
                    sDate = " (1=1) ";
                }
            }
            else
            {
                if (string.IsNullOrEmpty(FromDateTxt))
                {
                    FromDateTxt = "01/01/2007";
                }
                else
                {
                    if (FromDateTxt.Length < 1)
                    {
                        FromDateTxt = "01/01/2007";
                    }
                }
                
                if (string.IsNullOrEmpty(toDateTxt))
                {
                    toDateTxt = " getdate() + 1 ";
                    sDate = " (t1.DocDate between '"+FromDateTxt+"' and "+toDateTxt+") ";
                }
                else
                {
                    if (toDateTxt.Length < 1)
                    {
                        toDateTxt = " getdate() + 1 ";
                        sDate = " (t1.DocDate between '"+FromDateTxt+"' and "+toDateTxt+") ";
                    }
                    else
                    {
                        sDate = " (t1.DocDate between '"+FromDateTxt+"' and DATEADD(day,1,cast('"+toDateTxt+"' as date))) ";
                    }     
                }
            }

            ///---toLocTxt

            if (fromLocTxt == "0" && toLocTxt == "0")
            {
                if (andOr2 == "OR")
                {
                    sLoc = " (1=2) ";
                }
                else
                {
                    sLoc = " (1=1) ";
                }
            }
            else
            {
                if (fromLocTxt == "0")
                {
                    if (andOr2 == "OR")
                    {
                        sLoc = " (1=2 and ";
                    }
                    else
                    {
                        sLoc = " (1=1 and ";
                    } 
                }
                else
                {
                    sLoc = " ( t1.fromLoc =  '" + fromLocTxt + "' and " ;
                }

                if (toLocTxt == "0")
                {
                    if (andOr2 == "OR")
                    {
                        sLoc = sLoc + " (1 = 2)) ";
                    }
                    else
                    {
                        sLoc = sLoc + " (1 = 1)) ";                        
                    } 
                }
                else
                {
                    sLoc = sLoc + " t1.toLoc =  '" + toLocTxt + "' ) ";
                }
            }

            ///---status and categoryTxt

            if (categoryTxt == "0" && statusDoc == "All")
            {
                if (andOr3 == "OR")
                {
                    sStatus = " (1=2) ";

                }
                else
                {
                    sStatus = " (1=1) ";
                }
            }
            else
            {
                if (categoryTxt == "0")
                {
                    if (andOr3 == "OR")
                    {
                        sStatus = " (1=2  ";
                    }
                    else
                    {
                        sStatus = " (1=1  ";
                    }
                }
                else
                {
                    sStatus = " (t1.itmsGrpCod = '" + categoryTxt + "'";
                }

                if (statusDoc == "All")
                {
                    sStatus = sStatus + ") ";                                         
                }
                else
                {
                    if (andOr3 == "OR")
                    {
                        sStatus = sStatus + "  OR t1.docstatus =  '" + statusDoc  + "' ) ";
                    }
                    else
                    {
                        sStatus = sStatus + "  AND t1.docstatus =  '" + statusDoc + "' ) ";
                    }
                }
            }

            /////-------------sDocNum         
                
            if (int.TryParse(txtDocNum, out dummy))
            {
                sDocNum =  " (t1.docentry =  " + txtDocNum + ") " ;
                Lflag1++;
            }
            else
            {
                sDocNum = " (1=1) ";
            }

            sql +=  sDocNum + andOr1 + sDate + andOr2 + sLoc + andOr3 + sStatus;

            if (branchId > 1)
            {
                sql += string.Format(
                    " AND EXISTS (" +
                    "   SELECT 1 FROM {0}.dbo.OWHS w WITH(NOLOCK)" +
                    "   WHERE (w.WhsCode = t1.fromLoc OR w.WhsCode = t1.toLoc)" +
                    "     AND w.BPLId = {1}" +
                    ")", sap_db, branchId);
            }

            sql += " order by t1.DocEntry Desc";

            if(db.DbConnectionState == ConnectionState.Closed)
            {
                db.Connect();
            }
            
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure GetTransferDrafts.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public DataTable GetTransferDraftsToDelete(string CompanyId)
    {
        DataTable dt = new DataTable();
        string sql = "";

        int branchId = 0;
        HttpContext ctx = HttpContext.Current;
        if (ctx != null && ctx.Session != null)
            int.TryParse(ctx.Session["BranchId"] as string, out branchId);

        try
        {
            //sql = @"select * from smm_draft_header_vw ";
            //sql = @"select t1.*, isnull((select isnull(dispatched,'_') + '/' + isnull(received,'_') + ' /   ' + isnull(DispCompleted,'_') + '/' + isnull(ReceCompleted,'_') from smm_Transdiscrep_odrf where docentry = t1.docentry),'___/___') drfst from smm_draft_header_vw t1  ";
            sql = Queries.With_SmmDraftHeader() + @"
             select t1.*, isnull((select isnull(a1.dispatched,'P')
		    from smm_Transdiscrep_odrf a1 where a1.CompanyId = t1.CompanyId and a1.docentry = t1.docentry),'N') dispatched,
		    isnull((select isnull(a1.received,'P')
		    from smm_Transdiscrep_odrf a1 where a1.CompanyId = t1.CompanyId and a1.docentry = t1.docentry),'N') received,
		    isnull((select isnull(a1.DispCompleted,'P')
		    from smm_Transdiscrep_odrf a1 where a1.CompanyId = t1.CompanyId and a1.docentry = t1.docentry),'N') DispCompleted,
		    isnull((select isnull(a1.ReceCompleted,'P')
                    from smm_Transdiscrep_odrf a1 where a1.CompanyId = t1.CompanyId and a1.docentry = t1.docentry),'N') ReceCompleted
                    --from smm_draft_header_vw t1
                    from SmmDraftHeader t1
                    left outer join smm_Transdiscrep_odrf t2
                    on t1.CompanyId = t2.CompanyId and t1.docentry = t2.docentry
                    where  t1.DocStatus = 'O'
                    --and isnull(t2.dispatched,'P') <> 'Y'
                    and isnull(t2.received,'P') <> 'Y'
                    and EXISTS (
                        SELECT 1 FROM {0}.dbo.OWHS ow WITH(NOLOCK)
                        WHERE ow.WhsCode IN (t1.FromLoc, t1.ToLoc) AND ow.BPLId = @branchId
                    )
                    order by t1.docentry ";

            sql = string.Format(sql, CompanyId);

            using (var cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@branchId", branchId);
                db.adapter = new SqlDataAdapter(cmd);
                db.adapter.Fill(dt);
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure GetTransferDrafts.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }
    
    public DataTable GetTransferErrors(bool ShowAll, string CompanyId, int BranchId)
    {
        sap_db = CompanyId;
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            string branchFilter = BranchId > 0
                ? @" AND EXISTS (SELECT 1 FROM [" + sap_db + @"]..OWHS bw " + Queries.WITH_NOLOCK +
                  @" WHERE bw.WhsCode = o.ToWhsCode AND bw.BPLId = " + BranchId + ")"
                : "";

            sql = @"select DocEntryOri, line, docdate, fromwhscode,
                    towhscode, tooriwhscode, itemcode+' - '+ [dbo].[InitCap] (pludesc) itemcode, /*[dbo].[FIVEBCODEPRODS] (itemcode)*/ STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=la_transfer_errors.itemcode FOR XML PATH ('')), 1, 3, '') BarCode,
                    convert(int,quantity) as quantity, userapp, error_message, case when fixed = 'N' then 0 else 1 end fixed
                    from la_transfer_errors
                    where DocEntryOri in (select o.DocEntry from smm_Transdiscrep_odrf o
                                         where o.CompanyId = '" + sap_db + "'" + branchFilter + @") ";

            if (!ShowAll)
                sql += " and fixed = 'N'";

            sql += " order by DocEntryOri Desc, line";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure GetTransferErrors.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public DataTable GetStoreTransferDrafts(bool ShowAll, string CompanyId)
    {
        DataTable dt = new DataTable();
        string sql = "";

        try
        {

            sql = Queries.With_SmmDraftHeader() + @"
                select * from SmmDraftHeader";

            sql = string.Format(sql, CompanyId);

            if (!ShowAll)
                sql += " where DocStatus = 'O' and ToLoc like '%TIENDA%'";

            sql += " order by DocEntry Desc";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure GetTransferDrafts.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public DataTable GetTransferDetails(string DocEntry, string companyId)
    {
        sap_db = companyId;
        DataTable dt = new DataTable();
        string sql = "";
    
        try
        {
            sql = Queries.With_SmmDraftDetail() + Queries.With_LatTransfersAudit() + @"
SELECT
    a.DocEntry,
    a.DocDate,
    a.FromLoc,
    a.ToLoc,
    a.FromLocName,
    a.ToLocName,
    a.DocStatus AS Status,
    ROW_NUMBER() OVER (ORDER BY ws.bins, a.LineNumber ASC) AS LineNumber,
    a.DeptCode,
    a.DeptName,
    CASE WHEN oi.U_brand='REEBOK' AND oi.U_class='FOOTWEAR' THEN oi.U_SIZE+'-'+oi.U_SUBCLASS ELSE oi.ItemCode END AS ItemCode,
    CASE WHEN oi.U_brand='REEBOK' THEN oi.U_class ELSE oi.U_brand END AS U_brand,
    CASE WHEN LEN(a.Dscription) > 60 THEN LEFT(a.Dscription,60)+'...' ELSE a.Dscription END AS Description,
    a.Qty,
    a.Price,
    b.order_multiple,
    a.U_BOT AS u_bot,
    CASE WHEN b.order_multiple='C' THEN ISNULL(a.U_BOT,1) ELSE 1 END AS case_pack,
    a.Qty / CASE WHEN b.order_multiple='C' THEN ISNULL(a.U_BOT,1) ELSE NULL END AS cases,
    ISNULL(lta.DraftUser, '_') AS DraftUser,
    ISNULL(lta.DespUser, '_') AS DespUser,
    ISNULL(lta.RecUser, '_') AS RecUser,
    (SELECT CASE WHEN COUNT(*)=0 THEN 'ORIGINAL of ' ELSE 'COPY of ' END FROM smm_Print_Control WITH(NOLOCK) WHERE docentry=a.DocEntry) AS oricopy,
    ws.bins,
    CASE WHEN oi.U_brand='REEBOK' THEN STUFF((SELECT ' - '+BcdCode FROM {0}.dbo.OBCD a1 WITH(NOLOCK) WHERE a1.ItemCode=oi.ItemCode AND a1.BcdCode<>a1.ItemCode FOR XML PATH('')),1,3,'') ELSE STUFF((SELECT ' - '+RIGHT(BcdCode,5) FROM {0}.dbo.OBCD a1 WITH(NOLOCK) WHERE a1.ItemCode=oi.ItemCode FOR XML PATH('')),1,3,'') END AS BarCode,
    (SELECT CONVERT(INT,SUM(TotalQty)) FROM SmmDraftHeader WHERE DocEntry={1}) AS TotalProds
FROM SmmDraftDetail a
    LEFT JOIN LatTransfersAudit lta ON lta.DocEntry = a.DocEntry
    LEFT JOIN rss_loc_dept_multiple b WITH(NOLOCK) ON a.ToLoc=b.loc AND a.DeptCode=b.dept AND a.CompanyId=b.CompanyId AND b.companyId='{0}'
    LEFT JOIN {0}.dbo.oitm oi WITH(NOLOCK) ON oi.ItemCode=a.ItemCode
    LEFT JOIN WMS_Item_Bins_Cons ws WITH(NOLOCK) ON ws.whscode=a.FromLoc AND ws.ItemCode=oi.ItemCode
ORDER BY ws.bins, a.LineNumber";

            sql = string.Format(sql, sap_db, DocEntry);

            db.Connect();
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure GetTransferDetails.  ERROR MESSAGE :" + ex.Message + " - " + sql);
        }
        finally
        {
            db.Disconnect();
        }
    
        return dt;
    }

    public DataTable GetDisTransferDetails(string DocEntry, string companyId)
    {
        sap_db = companyId;
        DataTable dt = new DataTable();
        string sql = "";

        try
        {

            sql = @"select 
                    th.DocEntry ,
                    th.DocDate ,
                    th.FromWhsCode FromLoc ,
                    th.ToWhsCode ToLoc ,
                    th.FromWhsName FromLocName ,
                    th.ToWhsName ToLocName ,
                    th.DocStatus Status,
                    (td.LineNum + 1) LineNumber ,
                    'DeptCode' DeptCode, 
                    'DeptName' DeptName ,
                    --td.ItemCode ,
                    --oi.U_BRAND,
				td.ItemCode,
			    --CASE WHEN oi.U_brand='REEBOK' and oi.U_class='FOOTWEAR' THEN oi.U_SIZE +'-'+oi.U_SUBCLASS ELSE td.ItemCode  END ItemCode,
			    oi.U_brand,
				--CASE WHEN oi.U_brand='REEBOK' THEN oi.U_class ELSE oi.U_brand  END U_brand,

                    case when len(td.ItemName) > 35 then left(td.ItemName,35) + '...' else td.ItemName end  Description,
                    case th.Received
                         when 'Y' then td.receivedQuantity
                         else case th.Dispatched
                                   when 'Y' then td.DispatchQuantity
                                   else td.draftQuantity
                              end
                    end Qty ,
                    td.Price Price,
                    'om' order_multiple,
                    'ub' u_bot,
                    0 cases,
                    /*[dbo].[FIVEBCODEPRODS](td.ItemCode) STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=td.ItemCode FOR XML PATH ('')), 1, 3, '') BarCode 
                */
			CASE WHEN oi.U_brand='REEBOK' THEN STUFF((SELECT ' - ' + BcdCode FROM " + sap_db + @".dbo.OBCD a1 WITH(NOLOCK) WHERE a1.ItemCode=td.ItemCode and  a1.BcdCode <> a1.ItemCode FOR XML PATH ('')), 1, 3, '') ELSE
			STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 with(nolock) WHERE a1.ItemCode=td.ItemCode FOR XML PATH ('')), 1, 3, '') END BarCode
			from 
                    smm_Transdiscrep_odrf th,
                    smm_Transdiscrep_drf1 td,
                    {0}.dbo.oitm oi
                where th.DocEntry  = td.DocEntry 
                    and oi.ItemCode = td.ItemCode
                    and th.DocEntry = {1} order by td.LineNum";
            //and th.DocEntry = " + DocEntry + " order by oi.U_brand, Description";

            sql = string.Format(sql, sap_db, DocEntry);

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure GetDisTransferDetails.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

public DataTable GetTransferAudit(string statusDoc, string txtDocNum,
                                       string FromDateTxt, string toDateTxt,
                                       string fromLocTxt, string toLocTxt, string categoryTxt,
                                       string andOr1, string andOr2, string andOr3, string doQry, string CompanyId, int branchId = 0)
    {
        sap_db = CompanyId;
        DataTable dt = new DataTable();
        string sql = "";
        int dummy = 0;
        int Lflag1 = 0;
        string sDate = null;
        string sLoc = null;
        string sStatus = null;
        string sDocNum = null;

        try
        {

            sql = Queries.With_TransfersAudit() + @"
                select 
                    CompanyId,
                    Draft_Numero,
                    Codigo_Origen, 
                    Nombre_Origen, 
                    Codigo_Destino, 
                    Nombre_Destino, 
                    Estatus, 
                    Despachado, 
                    Recibido, 
                    Usuario_Originador,
                    Usuario_Despacho, 
                    Usuario_Recibo, 
                    Doc_Entry_Sap, 
                    Doc_Num_Sap,
                    Fecha_Originacion, 
                    Fecha_Despacho, 
                    Fecha_Recibo, 
                    Tiempo_Despachar,
                    Tiempo_Recibir,
                    substring(SistemaDes,4,3) SistemaDes,
		    substring(SistemaRec,4,3) SistemaRec,
		    DocNumITR
                    --from SMM_LAT_TRANSFERS_AUDIT_VW
                    FROM TransfersAudit
                    where";

            if (int.TryParse(txtDocNum, out dummy))
            {
                Lflag1++;
                sql = string.Format(sql, sap_db, txtDocNum);
            }
            else
            {
                sql = string.Format(sql, sap_db, "-1");
            }
            

            ///---FromDateTxt

            if (string.IsNullOrEmpty(FromDateTxt) && string.IsNullOrEmpty(toDateTxt))
            {
                if (andOr1 == "OR")
                {
                    sDate = " (1=2) ";

                }
                else
                {
                    sDate = " (1=1) ";
                }

            }
            else
            {

                if (string.IsNullOrEmpty(FromDateTxt))
                {

                    FromDateTxt = "01/01/2013";
                }
                else
                {

                    if (FromDateTxt.Length < 1)
                    {
                        FromDateTxt = "01/01/2013";
                    }

                }


                if (string.IsNullOrEmpty(toDateTxt))
                {

                    toDateTxt = " getdate() + 1 ";
                    sDate = " (Fecha_Originacion between '" + FromDateTxt + "' and " + toDateTxt + ") ";
                }
                else
                {

                    if (toDateTxt.Length < 1)
                    {
                        toDateTxt = " getdate() + 1 ";
                        sDate = " (Fecha_Originacion between '" + FromDateTxt + "' and " + toDateTxt + ") ";
                    }
                    else
                    {
                        sDate = " (Fecha_Originacion between '" + FromDateTxt + "' and DATEADD(day,1,cast('" + toDateTxt + "' as date))) ";

                    }


                }

            }

            ///---toLocTxt

            if (fromLocTxt == "0" && toLocTxt == "0")
            {
                if (andOr2 == "OR")
                {
                    sLoc = " (1=2) ";

                }
                else
                {
                    sLoc = " (1=1) ";
                }
            }
            else
            {

                if (fromLocTxt == "0")
                {

                    if (andOr2 == "OR")
                    {
                        sLoc = " (1=2 and ";
                    }
                    else
                    {
                        sLoc = " (1=1 and ";
                    }



                }
                else
                {

                    sLoc = " ( Codigo_Origen =  '" + fromLocTxt + "' and ";

                }


                if (toLocTxt == "0")
                {

                    if (andOr2 == "OR")
                    {
                        sLoc = sLoc + " (1 = 2)) ";
                    }
                    else
                    {
                        sLoc = sLoc + " (1 = 1)) ";
                    }


                }
                else
                {

                    sLoc = sLoc + " Codigo_Destino =  '" + toLocTxt + "' ) ";

                }

            }


            ///---status and categoryTxt
            ///

            categoryTxt = "0";

            if (categoryTxt == "0" && statusDoc == "All")
            {
                if (andOr3 == "OR")
                {
                    sStatus = " (1=2) ";

                }
                else
                {
                    sStatus = " (1=1) ";
                }
            }
            else
            {

                if (categoryTxt == "0")
                {

                    if (andOr3 == "OR")
                    {
                        sStatus = " (1=2  ";
                    }
                    else
                    {
                        sStatus = " (1=1  ";
                    }



                }
                else
                {

                    sStatus = " (itmsGrpCod = '" + categoryTxt + "'";


                }


                if (statusDoc == "All")
                {

                    sStatus = sStatus + ") ";

                }
                else
                {


                    if (andOr3 == "OR")
                    {
                        sStatus = sStatus + "  OR Estatus =  '" + statusDoc + "' ) ";
                    }
                    else
                    {
                        sStatus = sStatus + "  AND Estatus =  '" + statusDoc + "' ) ";
                    }



                }

            }


            /////-------------sDocNum


            if (int.TryParse(txtDocNum, out dummy))
            {
                sDocNum = " (Draft_Numero =  " + txtDocNum + ") ";
                Lflag1++;


            }
            else
            {
                sDocNum = " (1=1) ";

            }



            if (doQry == "0")
            {
                doQry = " and 1=2 ";


            }
            else
            {
                doQry = " and 1=1 ";

            }


            if (Lflag1 == 1)
            {
                sql += sDocNum + doQry;
            }
            else
            {
                sql += sDocNum + andOr1 + sDate + andOr2 + sLoc + andOr3 + sStatus + doQry;
            }

            if (branchId > 0)
            {
                sql += string.Format(
                    " and EXISTS (" +
                    "   SELECT 1 FROM {0}.dbo.OWHS w WITH(NOLOCK)" +
                    "   WHERE (" +
                    "       w.WhsCode = Codigo_Origen" +
                    "       OR w.WhsCode IN (SELECT d.ToWhsCode FROM smm_Transdiscrep_drf1 d WITH(NOLOCK) WHERE d.DocEntry = Draft_Numero)" +
                    "   )" +
                    "   AND w.BPLId = {1}" +
                    ")", sap_db, branchId);
            }

            sql += " and CompanyId = '" + sap_db + "' order by Draft_Numero ";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure GetTransferAudit.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    
    public DataTable GetTransferAuditItem(string statusDoc, string txtDocNum, string ItemCodeTbox,
                                       string FromDateTxt, string toDateTxt,
                                       string fromLocTxt, string toLocTxt, string categoryTxt,
                                       string andOr1, string andOr2, string andOr3, string doQry, string CompanyId, int branchId = 0)
    {
        sap_db = CompanyId;
        DataTable dt = new DataTable();
        string sql = "";
        int dummy = 0;
        int Lflag1 = 0;
        string sDate = null;
        string sLoc = null;
        string sStatus = null;
        string sDocNum = null;
    
        try
        {
    
            sql = Queries.With_SmmTransferItems() + @"select             
		a.CompanyId,
		a.Draft_Numero, 
		a.Fecha_Originacion, 
		a.Codigo_Origen, 
		a.Nombre_Origen, 
		a.Codigo_Destino, 
		a.Nombre_Destino, 
		a.Estatus,
		a.Dispatched , 
		a.Received, 
		a.DocEntryTraRec2, 
		a.UserDispatch, 
		a.UserReceive, 
		a.Date_Created, 
		a.Created_By,
		cast(a.LineNum as int) LineNum, 
		a.ItemCode, 
        /*[dbo].[FIVEBCODEPRODS] (a.ItemCode)*/ STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=a.ItemCode FOR XML PATH ('')), 1, 3, '') BarCode, 
		substring(a.ItemName,1,60) ItemName, 
		cast(a.DraftQuantity as int) DraftQuantity, 
		case when a.Dispatched = 'Y' then cast(a.DispatchQuantity as int) else 0 end DispatchQuantity,
		case when a.Received <> 'N' then cast(isnull(a.ReceivedQuantity, 0) as int) else 0 end ReceivedQuantity,
		cast(a.Price as int) Price
		--from SMM_TRANSFER_ITEMS_VIEW a
        from SmmTransferItems a
		where";

            if (int.TryParse(txtDocNum, out dummy))
            {
                Lflag1++;
                sql = string.Format(sql, sap_db, txtDocNum);
            }
            else
            {
                sql = string.Format(sql, sap_db, "-1");
            }
            
    
            ///---FromDateTxt
    
            if (string.IsNullOrEmpty(FromDateTxt) && string.IsNullOrEmpty(toDateTxt))
            {
                if (andOr1 == "OR")
                {
                    sDate = " (1=2) ";
    
                }
                else
                {
                    sDate = " (1=1) ";
                }
    
            }
            else
            {
    
                if (string.IsNullOrEmpty(FromDateTxt))
                {
    
                    FromDateTxt = "01/01/2013";
                }
                else
                {
    
                    if (FromDateTxt.Length < 1)
                    {
                        FromDateTxt = "01/01/2013";
                    }
    
                }
    
    
                if (string.IsNullOrEmpty(toDateTxt))
                {
    
                    toDateTxt = " getdate() + 1 ";
                    sDate = " (a.Fecha_Originacion between '" + FromDateTxt + "' and " + toDateTxt + ") ";
                }
                else
                {
    
                    if (toDateTxt.Length < 1)
                    {
                        toDateTxt = " getdate() + 1 ";
                        sDate = " (a.Fecha_Originacion between '" + FromDateTxt + "' and " + toDateTxt + ") ";
                    }
                    else
                    {
                        sDate = " (a.Fecha_Originacion between '" + FromDateTxt + "' and DATEADD(day,1,cast('" + toDateTxt + "' as date))) ";
    
                    }
    
    
                }
    
            }
    
            ///---toLocTxt
    
            if (fromLocTxt == "0" && toLocTxt == "0")
            {
                if (andOr2 == "OR")
                {
                    sLoc = " (1=2) ";
    
                }
                else
                {
                    sLoc = " (1=1) ";
                }
            }
            else
            {
    
                if (fromLocTxt == "0")
                {
    
                    if (andOr2 == "OR")
                    {
                        sLoc = " (1=2 and ";
                    }
                    else
                    {
                        sLoc = " (1=1 and ";
                    }
    
    
    
                }
                else
                {
    
                    sLoc = " ( a.Codigo_Origen =  '" + fromLocTxt + "' and ";
    
                }
    
    
                if (toLocTxt == "0")
                {
    
                    if (andOr2 == "OR")
                    {
                        sLoc = sLoc + " (1 = 2)) ";
                    }
                    else
                    {
                        sLoc = sLoc + " (1 = 1)) ";
                    }
    
    
                }
                else
                {
    
                    sLoc = sLoc + " a.Codigo_Destino =  '" + toLocTxt + "' ) ";
    
                }
    
            }
    
    
            ///---status and categoryTxt
            ///
    
            categoryTxt = "0";
    
            if (categoryTxt == "0" && statusDoc == "All")
            {
                if (andOr3 == "OR")
                {
                    sStatus = " (1=2) ";
    
                }
                else
                {
                    sStatus = " (1=1) ";
                }
            }
            else
            {
    
                if (categoryTxt == "0")
                {
    
                    if (andOr3 == "OR")
                    {
                        sStatus = " (1=2  ";
                    }
                    else
                    {
                        sStatus = " (1=1  ";
                    }
    
    
    
                }
                else
                {
    
                    sStatus = " (a.itmsGrpCod = '" + categoryTxt + "'";
    
    
                }
    
    
                if (statusDoc == "All")
                {
    
                    sStatus = sStatus + ") ";
    
                }
                else
                {
    
    
                    if (andOr3 == "OR")
                    {
                        sStatus = sStatus + "  OR a.Estatus =  '" + statusDoc + "' ) ";
                    }
                    else
                    {
                        sStatus = sStatus + "  AND a.Estatus =  '" + statusDoc + "' ) ";
                    }
    
    
    
                }
    
            }
    
    
            /////-------------sDocNum
    
    
            if (int.TryParse(txtDocNum, out dummy))
            {
                sDocNum = " (a.Draft_Numero =  " + txtDocNum + ") ";
                Lflag1++;
    
    
            }
            else
            {
                sDocNum = " (1=1) ";
    
            }
    
    
    
            if (doQry == "0")
            {
                doQry = " and 1=2 ";
    
    
            }
            else
            {
                doQry = " and 1=1 ";
    
            }
    
    
            if (Lflag1 == 1)
            {
                sql += sDocNum + doQry;
            }
            else
            {
                sql += sDocNum + andOr1 + sDate + andOr2 + sLoc + andOr3 + sStatus + doQry;
            }
    
    
            string sItem = null;
    
            if (string.IsNullOrEmpty (ItemCodeTbox))
            {
                sItem = " and 1=1 ";
            }
            else
            {
                sItem = " and a.ItemCode = '" + ItemCodeTbox.Trim() + "' ";
            }
    
            sql += sItem;

            if (branchId > 0)
            {
                sql += string.Format(
                    " and EXISTS (" +
                    "   SELECT 1 FROM {0}.dbo.OWHS w WITH(NOLOCK)" +
                    "   WHERE (w.WhsCode = a.Codigo_Origen OR w.WhsCode = a.Codigo_Destino)" +
                    "     AND w.BPLId = {1}" +
                    ")", sap_db, branchId);
            }

            sql += " and CompanyId = '" + sap_db + "' order by Draft_Numero, LineNum ";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure GetTransferAuditItem.  ERROR MESSAGE :" + ex.Message + " Sql: " + sql);
        }
        finally
        {
            db.Disconnect();
        }
    
        return dt;
}
    public DataTable MobS_GetToReceive(string txtDocNum,
                                       //string FromDateTxt, string toDateTxt,
                                       string fromLocTxt,  string toLocTxt,
                                       string CompanyId
                                   )
    {
        sap_db = CompanyId;
        DataTable dt = new DataTable();
        string sql = "";
        int Lflag1 = 0;

        try
        {
            //// quitar , tempo
            //sql = @"select t1.*                        
            //        from smm_draft_header_vw t1,
            //        smm_Transdiscrep_odrf t2
            //        where t1.docentry = t2.docentry
            //        and t2.docentry = 101097 ";
            //Lflag1++;  
            //// hasta aqui quitar , tempo


            sql = Queries.With_SmmDraftHeader() + @"select t1.*                        
                    --from smm_draft_header_vw t1,
                    from SmmDraftHeader t1,
                    --smm_Transdiscrep_odrf t2
                    TransdiscrepODRF t2
                    where t1.CompanyId = t2.CompanyId and t1.docentry = t2.docentry
                    and t2.DocStatus = 'O'  
                    and t2.dispatched = 'Y'
                    and isnull(t2.ScanStatus,'N') <> 'C'
                    and isnull(t2.Received,'N') <> 'Y' 
                    and t2.CompanyId = '{0}' ";

            sql = string.Format(sql, sap_db);

            if (!string.IsNullOrEmpty(txtDocNum))
            {
                sql += @" and t1.docentry =  " + txtDocNum + " ";
                Lflag1++;
            }
            else
            {

                if (fromLocTxt != "0")
                {

                    sql += @" and t1.FromLoc = '" + fromLocTxt + "' ";
                    Lflag1++;
                }

                if (toLocTxt != "0")
                {

                    sql += @" and t1.ToLoc = '" + toLocTxt + "' ";
                    Lflag1++;
                }

            }

            sql += @" order by t1.DocEntry Desc";
            
            db.adapter = new SqlDataAdapter(sql, db.Conn);

            if(Lflag1 > 0)
            {
                db.adapter.Fill(dt);
            }
            
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure MobS_GetToReceive.  ERROR MESSAGE :" + ex.Message +' '+ sql);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public DataTable MobS_GetEntryToReceive(string txtDocNum, string CompanyId)
    {
        sap_db = CompanyId;
        DataTable dt = new DataTable();
        string sql = "";

        if (String.IsNullOrEmpty(txtDocNum))
        {
            return dt;
        }

        try
        {

            sql = @"SELECT LineNum, ToWhsCode+' - '+ToWhsName ToWhs, 
                    ItemCode+' - '+ItemName Item,
                    convert(smallint,DraftQuantity) DraftQuantity, 
                    convert(smallint,DispatchQuantity) DispatchQuantity, 
                    convert(smallint,ReceivedQuantity) ReceivedQuantity, 
                    tmpQuantity, DocEntry 
                    FROM smm_Transdiscrep_drf1
                    WHERE CompanyId = '{0}' and DocEntry = " + txtDocNum + " order by LineNum";

            sql = string.Format(sql, sap_db);

            db.adapter = new SqlDataAdapter(sql, db.Conn);

                db.adapter.Fill(dt);

        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure MobS_GetToReceive.  ERROR MESSAGE :" + ex.Message + ' ' + sql);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public DataTable MobS_GetToDispatch(string txtDocNum,
                                       //string FromDateTxt, string toDateTxt,
                                       string fromLocTxt, string toLocTxt
                                   )
    {
        DataTable dt = new DataTable();
        string sql = "";
        int Lflag1 = 0;

        try
        {
            sql = Queries.With_SmmDraftHeader() + @"select t1.*
                    --from smm_draft_header_vw t1
                    from SmmDraftHeader t1
                    left join
                    --smm_Transdiscrep_odrf t2
                    TransdiscrepODRF t2
                    on t1.CompanyId = t2.CompanyId and t1.docentry = t2.docentry                  
                    WHERE t1.DocStatus = 'O'   
					and isnull(t2.dispatched,'N') = 'N'
                    and isnull(t2.ScanDesStatus,'O') = 'O'
                    and isnull(t2.Received,'N') = 'N' 
                    and t2.CompanyId = '{0}' ";

            sql = string.Format(sql, sap_db);

            if (!string.IsNullOrEmpty(txtDocNum))
            {
                sql += @" and t1.docentry =  " + txtDocNum + " ";
                Lflag1++;
            }
            else
            {

                if (fromLocTxt != "0")
                {

                    sql += @" and t1.FromLoc = '" + fromLocTxt + "' ";
                    Lflag1++;
                }

                if (toLocTxt != "0")
                {

                    sql += @" and t1.ToLoc = '" + toLocTxt + "' ";
                    Lflag1++;
                }

            }

            sql += @" order by t1.DocEntry Desc";


            db.adapter = new SqlDataAdapter(sql, db.Conn);

            if (Lflag1 > 0)
            {
                db.adapter.Fill(dt);
            }

        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure MobS_GetToDispatch.  ERROR MESSAGE :" + ex.Message + ' ' + sql);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public DataTable MobS_getDispatch()
    {
        DataTable dt = new DataTable();

        string sql = "";

        sql = @"select DispatchId from MOB_DISPATCH where DispatchStatus = 'O' order by DispatchId desc";

        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);

        }
        catch (Exception)
        {
            throw new Exception("Caught exception in procedure MobS_getDispatch");
        }
        finally
        {
            db.Disconnect();
        }
        
        return dt;
    }

    public DataTable GetTransdiscrepOrder(string DocEntry, string companyId)
    {
        sap_db = companyId;
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"select h.DocEntry, h.DocNum, h.DocDate,
                    h.FromWhsCode+' - '+h.FromWhsName FromWhs,
                    h.ToWhsCode+' - '+h.ToWhsName ToWhs,
                    h.DocStatus, h.Dispatched, h.DispCompleted, h.Received, h.ReceCompleted,
                    rtrim(convert(char(10),h.DocNumTraDis)) DocDisDis,
                    rtrim(convert(char(10),h.DocNumTraRec)) DocDisRec,
                    CASE WHEN ISNULL(h.DocNumTraRec2,0) > 0 THEN rtrim(convert(char(10),h.DocNumTraRec2)) ELSE '' END DocDisRec2,
                    ISNULL(h.DocEntryTraRec2, 0) DocEntryTraRec2,
                    h.userdispatch, h.userreceive, h.DispatchType, h.ReceiveType,
                    ISNULL(h.U_GTK_CONFIRMATION, '') U_GTK_CONFIRMATION,
                    ISNULL(h.DocEntryITR, 0) DocEntryITR,
                    ISNULL(h.DocNumITR,   0) DocNumITR,
                    ISNULL(wf.U_Type, '') AS FromWhsType
                    FROM smm_Transdiscrep_odrf h
                    LEFT JOIN [{0}]..OWHS wf WITH(NOLOCK) ON wf.WhsCode = h.FromWhsCode
                    WHERE h.CompanyId = '{0}' AND h.DocEntry = {1}";

            sql = string.Format(sql, sap_db, DocEntry);

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure GetTransdiscrepOrder.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public DataTable GetTransdiscrepOrderDtl(string DocEntry, string companyId)
    {
        sap_db = companyId;
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"SELECT d.LineNum, d.ToWhsCode+' - '+d.ToWhsName ToWhs,
                    d.ItemCode+' - '+d.ItemName Item, STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=d.ItemCode FOR XML PATH ('')), 1, 3, '') BarCode,
                    convert(int,d.DraftQuantity) DraftQuantity,
                    convert(int,d.DispatchQuantity) DispatchQuantity,
                    convert(int,d.ReceivedQuantity) ReceivedQuantity,
                    convert(int,d.tmpQuantity) tmpQuantity, d.userrecscanner, d.DocEntry
                    FROM smm_Transdiscrep_drf1 d
                    WHERE d.CompanyId = '{0}' AND d.DocEntry = {1}";

            sql = string.Format(sql, sap_db, DocEntry);

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure GetTransdiscrepOrderDtl.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public DataTable GetTransXsapDtl(string DocEntry, string companyId)
    {
        sap_db = companyId;
        DataTable dt = new DataTable();
        string sql = "";

        try
        {

            sql = Queries.With_SmmTranSapDRF1() + @"
SELECT [LineNum], [ItemCode], /*[dbo].[FIVEBCODEPRODS] (ItemCode)*/ STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=SmmTranSapDRF1.ItemCode FOR XML PATH ('')), 1, 3, '') BarCode, [ItemName], [DraftQuantity], [DocEntry], [onhand] 
FROM SmmTranSapDRF1 " + Queries.WITH_NOLOCK + @" 
WHERE DocEntry = {1} and CompanyId = '{0}' ORDER BY [LineNum]";

            sql = string.Format(sql, sap_db, DocEntry);
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.SelectCommand.CommandTimeout = 600; // 10 minute timeout
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure GetTransXsapDtl.  ERROR MESSAGE :" + ex.Message+" - "+sql);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }
    public DataTable TocLec_getEntriesBines(string PackingId)
    {
        DataTable dt = new DataTable();
        string sql = "";

        if (string.IsNullOrEmpty(PackingId))
        {
            return dt;
        }

        try
        {
            sql = @"select PackingOrdBinId, PackingId, WmsBin,CintilloBin1,CintilloBin2, OSDocNum 
                      from TOCLEC_PACKING_ORDEN_BINS 
                     WHERE PackingId = " + PackingId + @"
                       ORDER BY WmsBin";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);

        }

        catch (Exception ex)
        {
            throw new Exception("Caught exception in function TocLec_getEntriesBines" + ex.Message + ' ' + sql);

        }

        finally
        {

            db.Disconnect();
        }

        return dt;


    }

    public DataTable TocLec_getReceivedBins(string ReceivingId)
    {
        DataTable dt = new DataTable();
        string sql = "";


        try
        {
            sql = @"select a.OSDocNum, a.WmsBin, a.CintilloBin1, a.CintilloBin2, 
                      b.ReceivingId, b.ReceivingOrdBinId, b.PackingOrdBinId
	                    from dbo.TOCLEC_PACKING_ORDEN_BINS a,
                                dbo.TOCLEC_RECEIVING_ORDEN_BINS b
	                    where a.[PackingOrdBinId] = b.[PackingOrdBinId]
	                      and b.ReceivingId = " + ReceivingId + @" Order by a.OSDocNum, a.WmsBin ";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);

        }

        catch (Exception ex)
        {
            throw new Exception("Caught exception in function TocLec_getReceivedBins" + ex.Message + ' ' + sql);

        }

        finally
        {

            db.Disconnect();
        }

        return dt;


    }

    public DataTable TocLec_getUbiOrigen()
    {
        DataTable dt = new DataTable();

        string sql = "";

        sql = @"select WHSCODE from [dbo].[TOCLEC_OWHS_CONTROL]
                WHERE Control = 'FROM_PACKING'
                 order by WHSCODE ";

        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);

        }

        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure TocLec_getUbiOrigen" + ex.Message + ' ' + sql);


        }

        finally
        {
            db.Disconnect();
        }

        return dt;


    }

    public DataTable TocLec_GetOpenPackingId()
    {
        DataTable dt = new DataTable();

        
        string sql = "";

        sql = @"select PackingId from [dbo].[TOCLEC_PACKING]
                WHERE PackingStatus = 'O'  order by PackingId desc";

        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);

        }

        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure TocLec_GetOpenPackingId" + ex.Message + ' ' + sql);


        }

        finally
        {
            db.Disconnect();
        }

        return dt;


    }

    public DataTable TocLec_GetOpenReceivingId(string ReceivingWhscode)
    {
        DataTable dt = new DataTable();


        string sql = "";

        sql = @"select ReceivingId from [dbo].[TOCLEC_Receiving]
                WHERE ReceivingStatus = 'O' and ReceivingWhscode = '"+ ReceivingWhscode + @"' order by ReceivingId desc";

        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);

        }

        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure TocLec_GetOpenReceivingId" + ex.Message + ' ' + sql);
        }

        finally
        {
            db.Disconnect();
        }

        return dt;


    }

}
