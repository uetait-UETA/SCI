using System;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Returns data for reports 
/// </summary>
public class Reports
{
    protected SqlDb db;
    protected string sap_db;

    public Reports()
    {
        db = new SqlDb();
        db.Connect();
    }

    public DataTable getOnhandItems(string store, string itmgrp, string brand, string itemCode, string typeQry, string cortes, string CompanyId)
    {

        sap_db = CompanyId;
        DataTable dt = new DataTable();

        string sql = null;



        if (typeQry == "ITEM")
        {
            sql =
               @"select 
                ISNULL(owhs.U_POSCode, '') as U_POSCode, oitw.whscode as Ubicacion, cast(oitw.itemCode as varchar) as Codigo_Articulo, /*[dbo].[FIVEBCODEPRODS] (cast(oitw.itemCode as varchar))*/ STUFF((SELECT ' - ' + BcdCode FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=cast(oitw.itemCode as varchar) FOR XML PATH ('')), 1, 3, '') as BarCode, oitm.itemname as Nombre_Articulo, 
                oitm.ItmsGrpCod as Categoria, oitb.itmsgrpnam as Nombre_Categoria, cast(oitw.Onhand as int) as Existencia, oitm.U_BRAND Marca, oitm.U_CLASS Clase
                 from " + sap_db + @"..oitw as oitw, " + sap_db + @"..oitm as oitm, " + sap_db + @"..oitb as oitb, " + sap_db + @"..owhs as owhs
                where oitw.Onhand > 0 and oitw.itemcode = oitm.itemcode
                and oitb.itmsgrpcod = oitm.itmsgrpcod
                and owhs.whscode = oitw.whscode
                and oitw.whscode  in ('" + store.Replace(",", "','") + @"')
                and oitw.itemcode = '" + itemCode + @"'";
        }
        else
        {
            if (typeQry == "GRP")
            {
                if (itmgrp.Contains(","))
                {
                    sql =
                       @"select 
                        ISNULL(owhs.U_POSCode, '') as U_POSCode, oitw.whscode as Ubicacion, cast(oitw.itemCode as varchar) as Codigo_Articulo, /*[dbo].[FIVEBCODEPRODS] (cast(oitw.itemCode as varchar))*/ STUFF((SELECT ' - ' + BcdCode FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=cast(oitw.itemCode as varchar) FOR XML PATH ('')), 1, 3, '') as BarCode, oitm.itemname as Nombre_Articulo, 
                        oitm.ItmsGrpCod as Categoria, oitb.itmsgrpnam as Nombre_Categoria, cast(oitw.Onhand as int) as Existencia, oitm.U_BRAND Marca, oitm.U_CLASS Clase
                         from " + sap_db + @"..oitw as oitw, " + sap_db + @"..oitm as oitm, " + sap_db + @"..oitb as oitb, " + sap_db + @"..owhs as owhs
                        where oitw.Onhand > 0 and oitw.itemcode = oitm.itemcode
                        and oitb.itmsgrpcod = oitm.itmsgrpcod
                        and owhs.whscode = oitw.whscode
                        and oitw.whscode in ('" + store.Replace(",", "','") + @"')
                        and oitb.itmsgrpcod in (" + itmgrp + @")";
                }
                else
                {
                    sql =
                       @"select
                        ISNULL(owhs.U_POSCode, '') as U_POSCode, oitw.whscode as Ubicacion, cast(oitw.itemCode as varchar) as Codigo_Articulo, /*[dbo].[FIVEBCODEPRODS] (cast(oitw.itemCode as varchar))*/ STUFF((SELECT ' - ' + BcdCode FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=cast(oitw.itemCode as varchar) FOR XML PATH ('')), 1, 3, '') as BarCode, oitm.itemname as Nombre_Articulo,
                        oitm.ItmsGrpCod as Categoria, oitb.itmsgrpnam as Nombre_Categoria, cast(oitw.Onhand as int) as Existencia, oitm.U_BRAND Marca, oitm.U_CLASS Clase
                         from " + sap_db + @"..oitw as oitw, " + sap_db + @"..oitm as oitm, " + sap_db + @"..oitb as oitb, " + sap_db + @"..owhs as owhs
                        where oitw.Onhand > 0 and oitw.itemcode = oitm.itemcode
                        and oitb.itmsgrpcod = oitm.itmsgrpcod
                        and owhs.whscode = oitw.whscode
                        and oitw.whscode in ('" + store.Replace(",", "','") + @"')
                        and oitb.itmsgrpcod in (" + itmgrp + @")";
                }
            }
            else
            {

                if (typeQry == "GRPBRD")
                {


                    if (brand.Contains("All Brands"))
                    {
                        sql = @"select
                                ISNULL(owhs.U_POSCode, '') as U_POSCode, oitw.whscode as Ubicacion, cast(oitw.itemCode as varchar) as Codigo_Articulo, /*[dbo].[FIVEBCODEPRODS] (cast(oitw.itemCode as varchar))*/ STUFF((SELECT ' - ' + BcdCode FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=cast(oitw.itemCode as varchar) FOR XML PATH ('')), 1, 3, '') as BarCode, oitm.itemname as Nombre_Articulo,
                                oitm.ItmsGrpCod as Categoria, oitb.itmsgrpnam as Nombre_Categoria, cast(oitw.Onhand as int) as Existencia, oitm.U_BRAND Marca, oitm.U_CLASS Clase
                                 from " + sap_db + @"..oitw as oitw, " + sap_db + @"..oitm as oitm, " + sap_db + @"..oitb as oitb, " + sap_db + @"..owhs as owhs
                                where oitw.Onhand > 0 and oitw.itemcode = oitm.itemcode
                                and oitb.itmsgrpcod = oitm.itmsgrpcod
                                and owhs.whscode = oitw.whscode
                                and oitw.whscode in ('" + store.Replace(",", "','") + @"')
                                and oitb.itmsgrpcod in (" + itmgrp + @")";
                    }
                    else
                    {
                        sql = @"select 
                                ISNULL(owhs.U_POSCode, '') as U_POSCode, oitw.whscode as Ubicacion, cast(oitw.itemCode as varchar) as Codigo_Articulo, /*[dbo].[FIVEBCODEPRODS] (cast(oitw.itemCode as varchar))*/ STUFF((SELECT ' - ' + BcdCode FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=cast(oitw.itemCode as varchar) FOR XML PATH ('')), 1, 3, '') as BarCode, oitm.itemname as Nombre_Articulo, 
                                oitm.ItmsGrpCod as Categoria, oitb.itmsgrpnam as Nombre_Categoria, cast(oitw.Onhand as int) as Existencia, oitm.U_BRAND Marca, oitm.U_CLASS Clase
                                 from " + sap_db + @"..oitw as oitw, " + sap_db + @"..oitm as oitm, " + sap_db + @"..oitb as oitb, " + sap_db + @"..owhs as owhs
                                where oitw.Onhand > 0 and oitw.itemcode = oitm.itemcode
                                and oitb.itmsgrpcod = oitm.itmsgrpcod
                                and owhs.whscode = oitw.whscode
                                and oitw.whscode in ('" + store.Replace(",", "','") + @"')
                                and oitb.itmsgrpcod in (" + itmgrp + @")
                                and oitm.u_brand  in (" + brand + @")";
                    }

                }
                else
                    return dt;
            }
            
            if (!(String.IsNullOrEmpty(cortes)))
            {
		    if (cortes.Contains(","))
		    {
		       sql = sql + @" and exists (select 1 from SMM_DRF1_WK_HIS d1,
							  SMM_ODRF_WK_HIS od
							where d1.docentry = od.docentry
							and convert(date,od.docdate,101) = convert(date,getdate(),101)
							and DATEPART(HOUR, od.docdate) in (" + cortes + @")
							and d1.ItemCode = oitw.Itemcode
							and (d1.whscode = oitw.whscode or od.filler = oitw.whscode) 
						)";
		    }
		    else
		    {
		       sql = sql + @" and exists (select 1 from SMM_DRF1_WK_HIS d1,
								  SMM_ODRF_WK_HIS od
								where d1.docentry = od.docentry
								and convert(date,od.docdate,101) = convert(date,getdate(),101)
								--and DATEPART(HOUR, od.docdate) in (case when " + cortes + @" = 1 then DATEPART(HOUR, od.docdate) else " + cortes + @" end )
								and DATEPART(HOUR, od.docdate) = " + cortes + @"
								and d1.ItemCode = oitw.Itemcode
								and (d1.whscode = oitw.whscode or od.filler = oitw.whscode) 
						)";	    
		    }
		    
		    sql = sql.Replace("Onhand > 0", "Onhand >= 0");
		    
            }
            
            
        }


        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function getOnhandItems. MESSAGE : " + ex.Message + " Qry: " + sql);
        }
        finally
        {
            db.Disconnect();
        }
        return dt;

    }


    public DataTable GetDraftSummary()
    {
        DataTable dt = new DataTable();

        string sql =
            @"select  
                a.DocEntry, 
	            a.DocDate, 
	            b.Currency, 
	            a.filler FromLoc,
	            b.WhsCode ToLoc, 
	            max(b.LineNum) + 1 TotalLines,
	            sum(b.Quantity) CaseQty, 
	            sum(cast(b.Quantity * cast(case when c.U_BOT is null then c.numinsale else c.U_BOT end as int) as int)) UnitQty
            from	
                " + sap_db + @"..ODRF a,
	            " + sap_db + @"..DRF1 b,
	            " + sap_db + @"..oitm c
            where	
                    a.DocEntry = b.DocEntry
            and	    a.CANCELED = 'N' 
            and	    a.ObjType = 67 
            and	    a.DocStatus = 'O'
            and	    a.Comments like 'SMM%'
            and	    b.ItemCode = c.ItemCode
            group by 
                a.DocEntry, 
	            a.DocDate, 
	            b.Currency, 
	            a.filler,
	            b.WhsCode";

        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function GetDraftSummary. MESSAGE : " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
        return dt;
    }



    public DataTable GetDraftDetail(string DocEntry)
    {
        DataTable dt = new DataTable();

        string sql =
            @"select  
                a.DocEntry, 
	            a.DocDate, 
	            a.filler FromLoc,
	            b.WhsCode ToLoc, 
	            b.LineNum LineNumber, 
	            c.itmsgrpcod DeptCode, 
	            d.itmsgrpnam DeptName,
	            b.ItemCode, 
	            b.Dscription, 
	            cast(case when c.U_BOT is null then c.numinsale else c.U_BOT end as int) CasePack,
	            b.Quantity CaseQty, 
	            cast(b.Quantity * cast(case when c.U_BOT is null then c.numinsale else c.U_BOT end as int) as int) UnitQty
            from	
                " + sap_db + @"..ODRF a,
	            " + sap_db + @"..DRF1 b,
	            " + sap_db + @"..oitm c, 
	            " + sap_db + @"..oitb d
            where	
                a.DocEntry = b.DocEntry
                and	b.ItemCode = c.ItemCode
                and	c.itmsgrpcod = d.itmsgrpcod and
                a.DocEntry = " + DocEntry + @"
            order by b.LineNum";

        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function GetDraftDetail. MESSAGE : " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
        return dt;
    }

    public DataTable GetMinMaxWorksheet(string store, string depts)
    {
        DataTable dt = new DataTable();

        string sql =
            @"select	
                Loc, 
	            LocName, 
	            ItmsGrpCod, 
	            ItmsGrpNam, 
	            U_BRAND Marca,
	            Item, 
	            ItemName, 
	            (select cast(onhand as int) from 
	             " + sap_db + @"..oitw where whscode = 'BODEGA2' and itemcode = item) OHBodega2,
	            sum(UnitsOnHand) UnitsOnHand,    	            
	            sum(min_qty) MinQty, 
	            sum(max_qty) MaxQty
            from	(
	                select  
                        a.loc, 
		                d.whsname locname, 
		                b.itmsgrpcod, 
		                itmsgrpnam,
		                b.U_BRAND,
		                a.item, 
		                b.itemname, 
		                a.min_qty, 
		                a.max_qty,
		                0 UnitsOnHand		
	                from    
                        rss_store_item_min_max a, 
                        " + sap_db + @"..oitm b, 
                        " + sap_db + @"..oitb c, 
                        " + sap_db + @"..OWHS d
	                where   
                        a.item = b.itemcode and 
                        b.itmsgrpcod = c.itmsgrpcod and 
                        a.loc = d.whscode and
	                    max_qty > 0 and
                        a.loc = '" + store + @"' and
                        b.itmsgrpcod in (" + depts + @")    
	                union
	                select  
                        a.WhsCode loc, 
		                c.whsname locname,
		                b.itmsgrpcod,
		                d.itmsgrpnam,
		                b.U_BRAND,
		                a.ItemCode item,
		                b.itemname, 
		                0 min_qty,
		                0 max_qty,
		                a.onhand UnitsOnHand
	                from    
                        " + sap_db + @"..OITW a, 
                        " + sap_db + @"..OITM b, 
                        " + sap_db + @"..OWHS c, 
                        " + sap_db + @"..oitb d
	                where	
                        a.ItemCode = b.ItemCode and 
                        a.WhsCode = c.WhsCode and 
                        b.itmsgrpcod = d.itmsgrpcod and
	                    a.OnHand > 0 and
                        a.WhsCode = '" + store + @"' and   
                        b.itmsgrpcod in (" + depts + @")
	                ) a
            group by 
                Loc, 
	            LocName, 
	            ItmsGrpCod, 
	            ItmsGrpNam,
	            U_BRAND,
	            Item, 
	            ItemName";

        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function GetMinMaxWorksheet. MESSAGE : " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
        return dt;
    }

    public DataTable GetMinMaxReport(string store, string depts)
    {
        DataTable dt = new DataTable();

        string sql =
            @"select	
	            ItmsGrpCod GrpCode, 
	            U_BRAND Marca,
	            Item, 
	            case when len(ItemName) > 40 then left(ItemName, 40) + '...' else ItemName end Description,
	            (select cast(onhand as int) from 
	             " + sap_db + @"..oitw where whscode = 'BODEGA2' and itemcode = item) OHBodega2,
	            sum(UnitsOnHand) UnitsOnHand, 	            
	            sum(max_qty) MaxQty
            from	(
	                select  
		                b.itmsgrpcod, 
		                a.item, 
		                b.U_BRAND,
		                b.itemname, 
		                a.max_qty,
		                0 UnitsOnHand		
	                from    
                        rss_store_item_min_max a, 
                        " + sap_db + @"..oitm b, 
                        " + sap_db + @"..oitb c, 
                        " + sap_db + @"..OWHS d
	                where   
                        a.item = b.itemcode and 
                        b.itmsgrpcod = c.itmsgrpcod and 
                        a.loc = d.whscode and
	                    max_qty > 0 and
                        a.loc = '" + store + @"' and
                        b.itmsgrpcod in (" + depts + @")    
	                union
	                select  
		                b.itmsgrpcod,
		                a.ItemCode item,
		                b.U_BRAND,
		                b.itemname, 
		                0 max_qty,
		                a.onhand UnitsOnHand
	                from    
                        " + sap_db + @"..OITW a, 
                        " + sap_db + @"..OITM b, 
                        " + sap_db + @"..OWHS c, 
                        " + sap_db + @"..oitb d
	                where	
                        a.ItemCode = b.ItemCode and 
                        a.WhsCode = c.WhsCode and 
                        b.itmsgrpcod = d.itmsgrpcod and
	                    a.OnHand > 0 and
                        a.WhsCode = '" + store + @"' and   
                        b.itmsgrpcod in (" + depts + @")
	                ) a
            group by 
	            ItmsGrpCod, 
	            Item, 
	            U_BRAND,
	            ItemName";

        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function GetMinMaxReport. MESSAGE : " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
        return dt;
    }

    public DataTable GetMinMaxQuantities(string store, string depts, string brands, string displayAll, string NoPlanned, string NoInSap, string Item, string companyId, string control, string whsTypes, int bplId = 0, string uStore = "")
    {
        sap_db = companyId;
        string control1 = control;
        string whsTypes1 = whsTypes;
        string lItem = Item;
        string sql = "";
        DataTable dt = new DataTable();

        if (!(String.IsNullOrEmpty(lItem)))
        {
            lItem = Item.Replace(" ", String.Empty);
        }

        if (String.IsNullOrEmpty(store) || store == "0")
        {
            return dt;
        }

        if (String.IsNullOrEmpty(lItem) && (String.IsNullOrEmpty(depts) || String.IsNullOrEmpty(brands)))
        {
            return dt;
        }

        try
        {
            db.Connect();

            // Build TOR columns: OnHand - IsCommited for BPLId defined in TorBPLId web.config key
            string torWhsesA = "";
            int torBplId = 0;
            int.TryParse(System.Configuration.ConfigurationManager.AppSettings["TorBPLId"], out torBplId);
            DataTable torWhs = db.GetWhsByUStoreAndBplId(sap_db, "", torBplId);
            foreach (DataRow item in torWhs.Rows)
            {
                string wc = (string)item["WhsCode"];
                torWhsesA += ",(select isnull(cast(ia.OnHand as int), 0) from " + sap_db + ".dbo.OITW ia " + Queries.WITH_NOLOCK + " where ia.WhsCode='" + wc + "' and ia.ItemCode=a.ItemCode) AS 'TOR_" + wc + "'";
            }
            if (!string.IsNullOrEmpty(torWhsesA))
                torWhsesA = torWhsesA.Substring(1);

            if (NoInSap == "true")
            {
		        sql =
                @"select  
		        e.loc loc, 
		        a.whsname locname,
		        'No_Categoria_Aun' itmsgrpcod,
		        'No_Categoria_Aun' itmsgrpnam,
		        e.Item item,
                STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=e.Item FOR XML PATH ('')), 1, 3, '') BarCode, 
		        'No_Nombre_aun' itemname, 
		        e.min_qty min_qty,
		        e.max_qty max_qty,
		        0 OnHand,
			isnull(e.prior, 0) prior,
		        isnull(e.hold, 0) hold,
		        e.replacement_item,
		        e.comment,
		        'E' ORDER_MULTIPLE,
		        1 case_pack
	             from    
		        " + sap_db + @".DBO.OWHS a inner join
		        rss_store_item_min_max e 
		        on  e.loc = a.whscode  
		        and e.companyId = '" + sap_db + @"'
		        where e.LOC = '" + store + @"'
		        and Not exists(select 1 from " + sap_db + @".DBO.OITW
				         where itemcode = e.item
				           and WhsCode = e.loc)";

	            if (!(String.IsNullOrEmpty(lItem)))
	            {
                    sql += @" and e.Item = '" + lItem + "' ";
	            }
	        }	
	        else
	        {
			    if (NoPlanned == "true")
                {

			        sql =
                    @"select  
				    a.WhsCode loc, 
				    c.whsname locname,
				    b.itmsgrpcod,
				    d.itmsgrpnam,
				    a.ItemCode item,
                    STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=a.ItemCode FOR XML PATH ('')), 1, 3, '') BarCode, 
				    b.itemname, 
				    --e.min_qty as min_qty,
				    --e.max_qty as max_qty,
                    e.min_qty / case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(b.U_Bot,1) end min_qty,
                    e.max_qty / case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(b.U_Bot,1) end max_qty,
			    	   --cast(a.onhand as int) as OnHand,
                    case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then cast(a.onhand as int) else cast(a.onhand as int)/isnull(b.U_Bot,1) end OnHand,
				    (select cast(isnull(sum(onhand),0) as int) from " + sap_db + @".dbo.OITW with(nolock) where WhsCode IN (SELECT WhsCode FROM " + sap_db + @".dbo.OWHS with(nolock) WHERE BPLId=1) and itemcode = a.ItemCode) AS ColonOnHand,
                    --0 AS ColonOnHand,
				isnull(e.prior, 0) prior,
				    isnull(e.hold, 0) hold,
				    e.replacement_item,
				    e.comment,
				    mult.ORDER_MULTIPLE," +
                    @"case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(b.U_Bot,1) end case_pack" + (string.IsNullOrEmpty(torWhsesA) ? "" : "," + torWhsesA) + @"
			        from
				    " + sap_db + @".DBO.OITW a     
				    inner join 
				    " + sap_db + @".DBO.OITM b on a.ItemCode = b.ItemCode and a.WhsCode = '" + store + @"' -- and a.onhand<.01
				        inner join 
				    " + sap_db + @".DBO.OWHS c on a.WhsCode = c.WhsCode 
				        inner join  
				    " + sap_db + @".DBO.oitb d on b.itmsgrpcod = d.itmsgrpcod 
				        inner join 
				    rss_loc_dept_multiple mult on b.itmsgrpcod = mult.dept and mult.LOC =a.whscode and mult.companyId = '" + sap_db + @"' 
				    left outer join
				    rss_store_item_min_max e on e.item = a.itemcode
				    and e.loc = a.WhsCode  and e.companyId = '" + sap_db + @"'
				    where a.WhsCode = '" + store + @"'
				    and Not exists(select 1 from rss_store_item_min_max x
						     where x.item = a.itemcode
						       and x.loc = a.WhsCode
                               and isnull(x.min_qty, 0) + isnull(x.max_qty, 0) > 0
						       and x.loc = '" + store + @"')
                                and (select cast(isnull(sum(onhand),0) as int) from " + sap_db + @".dbo.OITW with(nolock) where WhsCode IN (SELECT WhsCode FROM " + sap_db + @".dbo.OWHS with(nolock) WHERE BPLId=1) and itemcode = a.itemcode)>0 --new code
				       ";


                    if (!(String.IsNullOrEmpty(lItem)))
                    {
				        sql = @" select  
						    a.WhsCode loc, 
						    c.whsname locname,
						    b.itmsgrpcod,
						    d.itmsgrpnam,
						    a.ItemCode item,
                            STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=a.ItemCode FOR XML PATH ('')), 1, 3, '') BarCode, 
						    b.itemname, 
						    --e.min_qty as min_qty,
						    --e.max_qty as max_qty,
                            e.min_qty / case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(b.U_Bot,1) end min_qty,
                            e.max_qty / case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(b.U_Bot,1) end max_qty,
			    	        --cast(a.onhand as int) as OnHand,
                            case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then cast(a.onhand as int) else cast(a.onhand as int)/isnull(b.U_Bot,1) end OnHand,
						    (select cast(isnull(sum(onhand),0) as int) from " + sap_db + @".dbo.OITW with(nolock) where WhsCode IN (SELECT WhsCode FROM " + sap_db + @".dbo.OWHS with(nolock) WHERE BPLId=1) and itemcode = a.ItemCode) AS ColonOnHand,
						    isnull(e.prior, 0) prior,
							isnull(e.hold, 0) hold,
						    e.replacement_item,
						    e.comment,
						    mult.ORDER_MULTIPLE,
						    case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(b.U_Bot,1) end as case_pack" + (string.IsNullOrEmpty(torWhsesA) ? "" : "," + torWhsesA) + @"
					        from    
						    " + sap_db + @".DBO.OITW a     
						    inner join 
						    " + sap_db + @".DBO.OITM b on a.ItemCode = b.ItemCode and a.WhsCode = '" + store + @"'  
						        inner join 
						    " + sap_db + @".DBO.OWHS c on a.WhsCode = c.WhsCode 
						        inner join  
						    " + sap_db + @".DBO.oitb d on b.itmsgrpcod = d.itmsgrpcod 
						        left outer join
						    rss_loc_dept_multiple mult on b.itmsgrpcod = mult.dept and mult.LOC =a.whscode and mult.companyId = '" + sap_db + @"'
					    left outer join rss_store_item_min_max e on e.item = a.itemcode and e.loc = a.WhsCode and e.companyId = '" + sap_db + @"'
				        where a.WhsCode = '" + store + @"'  and a.ItemCode = '" + lItem + @"'  AND (ISNULL(c.U_Type, '') = '' OR ISNULL(b.U_Type, '') = c.U_Type) ";
                    }
			        else
			        {
				        sql += @" and b.itmsgrpcod in (" + depts + @") ";

				        if (displayAll != "true")
                        {
                            sql += @" and e.hold = 1 ";
                        }

				        if (brands != "'All Brands'")
                        {
                            sql += @" and replace(u_brand, '''','_') in (" + brands + @")";
                        }

				        sql += @" AND (ISNULL(c.U_Type, '') = '' OR ISNULL(b.U_Type, '') = c.U_Type) ";
				        sql += @"    order by 1";
			        }
			    }
			    else
			    {
			        sql =
                    @"select  
				        a.WhsCode loc, 
				        c.whsname locname,
				        b.itmsgrpcod,
				        d.itmsgrpnam,
				        a.ItemCode item,
                        STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=a.ItemCode FOR XML PATH ('')), 1, 3, '') BarCode, 
				        b.itemname, 
				        --e.min_qty as min_qty,
				        --e.max_qty as max_qty,
                    e.min_qty / case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(b.U_Bot,1) end min_qty,
                    e.max_qty / case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(b.U_Bot,1) end max_qty,
			    	   --cast(a.onhand as int) as OnHand,
                    case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then cast(a.onhand as int) else cast(a.onhand as int)/isnull(b.U_Bot,1) end OnHand,
				        (select cast(isnull(sum(onhand),0) as int) from " + sap_db + @".dbo.OITW with(nolock) where WhsCode IN (SELECT WhsCode FROM " + sap_db + @".dbo.OWHS with(nolock) WHERE BPLId=1) and itemcode = a.ItemCode) AS ColonOnHand,
					isnull(e.prior, 0) prior,
				        isnull(e.hold, 0) hold,
				        e.replacement_item,
				        e.comment,
				        mult.ORDER_MULTIPLE,
				        case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(b.U_Bot,1) end as case_pack" + (string.IsNullOrEmpty(torWhsesA) ? "" : "," + torWhsesA) + @"
				    from " + sap_db + @".DBO.OITW a     
				    inner join " + sap_db + @".DBO.OITM b on a.ItemCode = b.ItemCode and a.WhsCode = '" + store + @"'  
				    inner join " + sap_db + @".DBO.OWHS c on a.WhsCode = c.WhsCode 
				    inner join " + sap_db + @".DBO.oitb d on b.itmsgrpcod = d.itmsgrpcod 
				    inner join rss_loc_dept_multiple mult on b.itmsgrpcod = mult.dept and mult.LOC =a.whscode and mult.companyId = '" + sap_db + @"' 
				    left outer join rss_store_item_min_max e on e.item = a.itemcode and e.loc = a.WhsCode and e.companyId = '" + sap_db + @"'
			        where a.WhsCode = '" + store + @"'
				    ";

			        if (!(String.IsNullOrEmpty(lItem)))
			        {
				    sql = @"select  
						a.WhsCode loc, 
						c.whsname locname,
						b.itmsgrpcod,
						d.itmsgrpnam,
						a.ItemCode item,
                        STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=a.ItemCode FOR XML PATH ('')), 1, 3, '') BarCode, 
						b.itemname, 
						--e.min_qty as min_qty,
						--e.max_qty as max_qty,
					e.min_qty / case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(b.U_Bot,1) end min_qty,
                    e.max_qty / case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(b.U_Bot,1) end max_qty,
			    	   --cast(a.onhand as int) as OnHand,
                    case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then cast(a.onhand as int) else cast(a.onhand as int)/isnull(b.U_Bot,1) end OnHand,
						(select cast(isnull(sum(onhand),0) as int) from " + sap_db + @".dbo.OITW with(nolock) where WhsCode IN (SELECT WhsCode FROM " + sap_db + @".dbo.OWHS with(nolock) WHERE BPLId=1) and itemcode = a.ItemCode) AS ColonOnHand,
						isnull(e.prior, 0) prior,
						isnull(e.hold, 0) hold,
						e.replacement_item,
						e.comment,
						mult.ORDER_MULTIPLE,
						case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(b.U_Bot,1) end as case_pack" + (string.IsNullOrEmpty(torWhsesA) ? "" : "," + torWhsesA) + @"
					from " + sap_db + @".DBO.OITW a     
					inner join " + sap_db + @".DBO.OITM b on a.ItemCode = b.ItemCode and a.WhsCode = '" + store + @"'  
					inner join " + sap_db + @".DBO.OWHS c on a.WhsCode = c.WhsCode 
					inner join " + sap_db + @".DBO.oitb d on b.itmsgrpcod = d.itmsgrpcod 
					left outer join rss_loc_dept_multiple mult on b.itmsgrpcod = mult.dept and mult.LOC =a.whscode and mult.companyId = '" + sap_db + @"'
					left outer join rss_store_item_min_max e on e.item = a.itemcode and e.loc = a.WhsCode and e.companyId = '" + sap_db + @"'
				    where a.WhsCode = '" + store + @"'  and a.ItemCode = '" + lItem + @"'  AND (ISNULL(c.U_Type, '') = '' OR ISNULL(b.U_Type, '') = c.U_Type) ";
			        }
			        else
			        {
				        sql += @" and b.itmsgrpcod in (" + depts + @") ";

				        if (displayAll != "true")
                        {
                            sql += @" and e.hold = 1 ";
                        }
				            
				        if (brands != "'All Brands'")
                        {
                            sql += @" and replace(u_brand, '''','_') in (" + brands + @")";
                        }

				        sql += @" AND (ISNULL(c.U_Type, '') = '' OR ISNULL(b.U_Type, '') = c.U_Type) ";
				        sql += @" order by isnull(e.max_qty,0) desc";
			        }
			    }
            }

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.SelectCommand.CommandTimeout = 300;
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function GetMinMaxQuantities. MESSAGE : " + ex.Message + "Sql:<" + sql + ">");
        }
        finally
        {
            db.Disconnect();
        }
        return dt;
    }
	
	public DataTable GetMinMaxQuantities_Mult(string store, string depts, string brands, string displayAll, string NoPlanned, string NoInSap, string Item, string companyId, string control, string whsTypes)
    {
        sap_db = companyId;
        string control1 = control;
        string whsTypes1 = whsTypes;

        DataTable dt = new DataTable();

        string lItem = Item;

        if (!(String.IsNullOrEmpty(lItem)))
        {
            lItem = Item.Replace(" ", String.Empty);
        }

        if (String.IsNullOrEmpty(store) || store == "0")
        {
            return dt;
        }

        if (String.IsNullOrEmpty(lItem) && (String.IsNullOrEmpty(depts) || String.IsNullOrEmpty(brands)))
        {
            return dt;
        }

        string sql = "";
        string whses = "";

        db.Connect();
        DataTable whs = db.GetWhsByCiaIdAndControl(sap_db, control1, whsTypes1);

        foreach (DataRow item in whs.Rows)
        {
            whses += ",cast((select onhand from [" + sap_db + "].dbo.OITW where WhsCode = '" + (string)item["WhsCode"] + "' and ItemCode = a.ItemCode ) / case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(b.u_bot,1) end as int) AS '" + (string)item["WhsCode"] + "'";
        }

        if (!string.IsNullOrEmpty(whses))
        {
            whses = whses.Substring(1);
        }

        if (NoInSap == "true")
        {
            sql =
            @"select  
		e.loc loc, 
		a.whsname locname,
		'No_Categoria_Aun' itmsgrpcod,
		'No_Categoria_Aun' itmsgrpnam,
		e.Item item,
        STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM [" + sap_db + @"].dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=e.Item FOR XML PATH ('')), 1, 3, '') BarCode, 
		'No_Nombre_aun' itemname, 
		e.min_qty min_qty,
		e.max_qty max_qty,
		0 OnHand,
		isnull(e.hold, 0) hold,
		e.replacement_item,
		e.comment,
		'E' ORDER_MULTIPLE,
		1 case_pack
	    ,'' AS marca  
        from    
		" + sap_db + @".DBO.OWHS a inner join
		rss_store_item_min_max e 
		on  e.loc = a.whscode  
		and e.companyId = '" + sap_db + @"'
        where e.LOC IN (" + store + @")
		and Not exists(select 1 from " + sap_db + @".DBO.OITW
				 where itemcode = e.item
				   and WhsCode = e.loc)";


            if (!(String.IsNullOrEmpty(lItem)))
            {
                sql += @" and e.Item = '" + lItem + "' ";
            }
            sql += @" order by item";
        }
        else //else1
        {

            if (NoPlanned == "true")
            {

                sql =
                @"select  
				a.WhsCode loc, 
				c.whsname locname,
				b.itmsgrpcod,
				d.itmsgrpnam,
				a.ItemCode item,
                STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=a.ItemCode FOR XML PATH ('')), 1, 3, '') BarCode, 
				b.itemname, 
				e.min_qty as min_qty,
				e.max_qty as max_qty,
				cast(a.onhand as int) as OnHand,
				(select cast(isnull(sum(onhand),0) as int) from " + sap_db + @".dbo.OITW with(nolock) where WhsCode IN (SELECT WhsCode FROM " + sap_db + @".dbo.OWHS with(nolock) WHERE BPLId=1) and itemcode = a.ItemCode) AS ColonOnHand,
				isnull(e.hold, 0) hold,
				e.replacement_item,
				e.comment,
				mult.ORDER_MULTIPLE," +
                    //case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(b.u_bot,1) end case_pack" + (string.IsNullOrEmpty(torWhsesA) ? "" : "," + torWhsesA) + @"
                    @"1 case_pack" + (string.IsNullOrEmpty(whses) ? "" : "," + whses) + @"					
			    ,replace(b.u_brand, '''','_') AS marca
                from    
				" + sap_db + @".DBO.OITW a     
				inner join 
                " + sap_db + @".DBO.OITM b on a.ItemCode = b.ItemCode and a.WhsCode IN (" + store + ")" +
                    @"inner join 
				" + sap_db + @".DBO.OWHS c on a.WhsCode = c.WhsCode 
				    inner join  
				" + sap_db + @".DBO.oitb d on b.itmsgrpcod = d.itmsgrpcod 
				    inner join 
				rss_loc_dept_multiple mult on b.itmsgrpcod = mult.dept and mult.LOC =a.whscode and mult.companyId = '" + sap_db + @"' 
				left outer join
				rss_store_item_min_max e on e.item = a.itemcode
				and e.loc = a.WhsCode  and e.companyId = '" + sap_db + @"'
			    where a.WhsCode IN (" + store + @")
				and Not exists(select 1 from rss_store_item_min_max x
						 where x.item = a.itemcode
						   and x.loc = a.WhsCode
						   and isnull(x.min_qty, 0) + isnull(x.max_qty, 0) > 0
                            and x.loc IN (" + store + @"))--new code
				   ";

                if (!(String.IsNullOrEmpty(lItem)))
                {
                    sql = @" select  
						a.WhsCode loc, 
						c.whsname locname,
						b.itmsgrpcod,
						d.itmsgrpnam,
						a.ItemCode item,
                        STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=a.ItemCode FOR XML PATH ('')), 1, 3, '') BarCode, 
						b.itemname, 
						e.min_qty as min_qty,
						e.max_qty as max_qty,
						cast(a.onhand as int) as OnHand,
						(select cast(isnull(sum(onhand),0) as int) from " + sap_db + @".dbo.OITW with(nolock) where WhsCode IN (SELECT WhsCode FROM " + sap_db + @".dbo.OWHS with(nolock) WHERE BPLId=1) and itemcode = a.ItemCode) AS ColonOnHand,
						isnull(e.hold, 0) hold,
						e.replacement_item,
						e.comment,
						mult.ORDER_MULTIPLE,
						1 as case_pack" + (string.IsNullOrEmpty(whses) ? "" : "," + whses) + @"
					    ,replace(b.u_brand, '''','_') AS marca
                        from    
						" + sap_db + @".DBO.OITW a     
						inner join 
                        " + sap_db + @".DBO.OITM b on a.ItemCode = b.ItemCode and a.WhsCode IN (" + store + ")" +
                        @"inner join
						    inner join 
						" + sap_db + @".DBO.OWHS c on a.WhsCode = c.WhsCode 
						    inner join  
						" + sap_db + @".DBO.oitb d on b.itmsgrpcod = d.itmsgrpcod 
						    inner join 
						rss_loc_dept_multiple mult on b.itmsgrpcod = mult.dept and mult.LOC =a.whscode and mult.companyId = '" + sap_db + @"' 
					left outer join
					rss_store_item_min_max e on e.item = a.itemcode
					and e.loc = a.WhsCode and mult.companyId = '" + sap_db + @"'
                 where a.WhsCode IN (" + store + @")  and a.ItemCode = '" + lItem + "' order by item ";
                }
                else
                {

                    sql += @" and b.itmsgrpcod in (" + depts + @") ";

                    if (displayAll != "true")
                        sql += @" and e.hold = 1 ";

                    if (brands != "'All Brands'")
                        sql += @" and replace(u_brand, '''','_') in (" + brands + @")";



                    sql += @"    order by item";
                }




            }
            else
            {
                sql =
                @"select  
				a.WhsCode loc, 
				c.whsname locname,
				b.itmsgrpcod,
				d.itmsgrpnam,
				a.ItemCode item,
                STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=a.ItemCode FOR XML PATH ('')), 1, 3, '') BarCode, 
				b.itemname, 
				e.min_qty as min_qty,
				e.max_qty as max_qty,
				cast(a.onhand as int) as OnHand,
				(select cast(isnull(sum(onhand),0) as int) from " + sap_db + @".dbo.OITW with(nolock) where WhsCode IN (SELECT WhsCode FROM " + sap_db + @".dbo.OWHS with(nolock) WHERE BPLId=1) and itemcode = a.ItemCode) AS ColonOnHand,
				isnull(e.hold, 0) hold,
				e.replacement_item,
				e.comment,
				mult.ORDER_MULTIPLE,
				1 as case_pack" + (string.IsNullOrEmpty(whses) ? "" : "," + whses) + @"
				,replace(b.u_brand, '''','_') AS marca
                from    
				" + sap_db + @".DBO.OITW a     
				inner join   
                " + sap_db + @".DBO.OITM b on a.ItemCode = b.ItemCode and a.WhsCode IN (" + store + @")  
				    inner join 
				" + sap_db + @".DBO.OWHS c on a.WhsCode = c.WhsCode 
				    inner join  
				" + sap_db + @".DBO.oitb d on b.itmsgrpcod = d.itmsgrpcod 
				    inner join 
				rss_loc_dept_multiple mult on b.itmsgrpcod = mult.dept and mult.LOC =a.whscode and mult.companyId = '" + sap_db + @"' 
				left outer join
				rss_store_item_min_max e on e.item = a.itemcode
				and e.loc = a.WhsCode and e.companyId = '" + sap_db + @"'
			    where
                a.WhsCode IN (" + store + @")
				";


                if (!(String.IsNullOrEmpty(lItem)))
                {
                    sql = @" select  
								a.WhsCode loc, 
								c.whsname locname,
								b.itmsgrpcod,
								d.itmsgrpnam,
								a.ItemCode item,
                                STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM [" + sap_db + @"].dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=a.ItemCode FOR XML PATH ('')), 1, 3, '') BarCode, 
								b.itemname, 
								e.min_qty as min_qty,
								e.max_qty as max_qty,								
								cast(a.onhand as int) as OnHand,
								(select cast(isnull(sum(onhand),0) as int) from " + sap_db + @".dbo.OITW with(nolock) where WhsCode IN (SELECT WhsCode FROM " + sap_db + @".dbo.OWHS with(nolock) WHERE BPLId=1) and itemcode = a.ItemCode) AS ColonOnHand,
								isnull(e.hold, 0) hold,
								e.replacement_item,
								e.comment,
								mult.ORDER_MULTIPLE,
								1 as case_pack" + (string.IsNullOrEmpty(whses) ? "" : "," + whses) + @"
							    ,replace(b.u_brand, '''','_') AS marca
                                from    
								" + sap_db + @".DBO.OITW a     
								inner join  
                                " + sap_db + @".DBO.OITM b on a.ItemCode = b.ItemCode and a.WhsCode IN (" + store + @")  
								    inner join 
								" + sap_db + @".DBO.OWHS c on a.WhsCode = c.WhsCode 
								    inner join  
								" + sap_db + @".DBO.oitb d on b.itmsgrpcod = d.itmsgrpcod 
								    inner join 
								rss_loc_dept_multiple mult on b.itmsgrpcod = mult.dept and mult.LOC =a.whscode and mult.companyId = '" + sap_db + @"' 

						left outer join
						rss_store_item_min_max e on e.item = a.itemcode

						and e.loc = a.WhsCode and e.companyId = '" + sap_db + @"'
                where a.WhsCode IN (" + store + @")  and a.ItemCode = '" + lItem + "' order by item ";
                }
                else
                {
                    sql += @" and b.itmsgrpcod in (" + depts + @") ";


                    if (displayAll != "true")
                        sql += @" and e.hold = 1 ";

                    if (brands != "'All Brands'")
                        sql += @" and replace(u_brand, '''','_') in (" + brands + @")";

                    sql += @"    order by item";

                }
            }
        } //else1

        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.SelectCommand.CommandTimeout = 300;
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function GetMinMaxQuantities_Mult. MESSAGE : " + ex.Message + "Sql:<" + sql + ">");
        }
        finally
        {
            db.Disconnect();
        }
        return dt;
    }
	
	public DataTable GetMinMaxQuantities_ColSiTocNo(string store, string depts, string brands, string displayAll, string NoPlanned, string NoInSap, string Item, string companyId)
    {
        sap_db = companyId;

        DataTable dt = new DataTable();

        string lItem = Item;

        if (!(String.IsNullOrEmpty(lItem)))
        {
            lItem = Item.Replace(" ", String.Empty);
        }

        if (String.IsNullOrEmpty(store))
            return dt;

        if (String.IsNullOrEmpty(lItem))
        {
            if ((String.IsNullOrEmpty(depts)) || (String.IsNullOrEmpty(brands)))
                return dt;
        }

        string sql = "";
        string whses = "";
        string TocumenOnHandWhsCodes = "";

        if (sap_db == "DFATOCUMEN")
        {
            TocumenOnHandWhsCodes += @"
                'CMN21',
                'CMN22',
                'CMN29',
                'CMN41',
                'CMN45',
                'CMN48',
                'CMN56'";
        }

        if (sap_db == "TOCUMEN")
        {
            TocumenOnHandWhsCodes += @"
                'BODEGA',
                'BODEGA2',
                'BODEGA3',
                'CMN41KS'";
        }

        if (sap_db == "TOCUBARINC")
        {
            TocumenOnHandWhsCodes += @"
                'BODEGA',
                'CMN44'";
        }

        if (sap_db == "LOPO")
        {
            TocumenOnHandWhsCodes += @"
                'BODEGA'";
        }

        sql =
        @"select  
		a.WhsCode loc, 
		c.whsname locname,
		b.itmsgrpcod,
		d.itmsgrpnam,
		a.ItemCode item,
        /*[dbo].[FIVEBCODEPRODS] (a.ItemCode)*/ STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=a.ItemCode FOR XML PATH ('')), 1, 3, '') BarCode, 
		b.itemname, 
		e.min_qty as min_qty,
		e.max_qty as max_qty,
        --TOCINV.TocOnHand AS OnHand,
        a.OnHand / CASE WHEN RTRIM(LTRIM(a.WhsCode)) = 'CMN21' THEN CASE WHEN ISNULL(mult.ORDER_MULTIPLE, 'E') = 'E' THEN 1 ELSE ISNULL(b.U_BOT, 1) END ELSE 1 END AS OnHand,
        cast(isnull((z.onhand),0) as int) AS ColonOnHand,
		isnull(e.hold, 0) hold,
		e.replacement_item,
		e.comment,
		mult.ORDER_MULTIPLE,
		1 as case_pack "
        + ((whses.Length > 0) ? "," + whses : "").ToString()
		+ @" , replace(b.u_brand, '''','_') AS marca
        from " + sap_db + @".DBO.OITW a " + Queries.WITH_NOLOCK + @"     
		inner join " + sap_db + @".DBO.OITM b " + Queries.WITH_NOLOCK + @"  on a.ItemCode = b.ItemCode
        inner join 
		" + sap_db + @".DBO.OWHS c " + Queries.WITH_NOLOCK + @"  on a.WhsCode = c.WhsCode 
			inner join  
		" + sap_db + @".DBO.oitb d " + Queries.WITH_NOLOCK + @"  on b.itmsgrpcod = d.itmsgrpcod 
			inner join 
		rss_loc_dept_multiple mult on b.itmsgrpcod = mult.dept and mult.LOC =a.whscode and mult.companyId = '" + sap_db + @"' 
		left outer join
		rss_store_item_min_max e on e.item = a.itemcode
		and e.loc = a.WhsCode and e.companyId = '" + sap_db + @"'
        INNER JOIN SMM_OITW_COLON_SI_TOC_NO_VW z 
            ON z.itemcode = a.ItemCode
		where a.WhsCode IN (" + store + ") AND a.OnHand <= 0";

        /*
         --INNER JOIN (SELECT a.ItemCode AS TocItemCode, SUM(a.onhand) AS TocOnHand 
            --FROM " + sap_db + @".DBO.OITW a " + Queries.WITH_NOLOCK + @" 
            --WHERE WhsCode IN (" + TocumenOnHandWhsCodes + @")
            --GROUP BY ItemCode) TOCINV
                --ON TOCINV.TocItemCode = a.ItemCode
                --AND TOCINV.TocOnHand <= 0
         */

        if (!(String.IsNullOrEmpty(lItem)))
        {
            sql += @" and a.ItemCode = '" + lItem + @"' ";
        }
        else
        {
            sql += @" and b.itmsgrpcod in (" + depts + @") ";

            if (displayAll != "true")
            {
                sql += @" and e.hold = 1 ";
            }

            if (brands != "'All Brands'")
            {
                sql += @" and replace(b.u_brand, '''','_') in (" + brands + @")";
            }

            sql += @" order by isnull(e.max_qty,0) desc";
        }

        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.SelectCommand.CommandTimeout = 300;
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function GetMinMaxQuantities_ColSiTocNo. MESSAGE : " + ex.Message + "Sql:<" + sql + ">");
        }
        finally
        {
            db.Disconnect();
        }
        return dt;
    }


    public DataTable UnoPickPack()
    {
        DataTable dt = new DataTable();
        string sql = "";

        try
        {

            sql = @" select PackingId, CreatedDate
                         from [dbo].[MOB_PACKING]
                        order by Packingid desc";


            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure UnoPickPack.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public DataTable UnoPickPackOrders(string packigId)
    {
        DataTable dt = new DataTable();

        if (String.IsNullOrEmpty(packigId))
        {
            return dt;
        }

        string sql = "";

        try
        {

            sql = @" 
                    select PackingOrdId, 
                           PackingId, BinId, 
	                       OrderId, PickedWhscode, 
	                       PickedUser, PickedDate, 
	                       ReceivedWhscode, ReceivedUser, 
	                       ReceivedDate
                    from [dbo].[MOB_PACKING_ORDENES]
                    where PackingId = " + packigId + @"  order by OrderId";


            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure UnoPickPackOrders.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public DataTable UnoPickPackOrdersItems(string packigId, string BinId)
    {
        DataTable dt = new DataTable();

        if (String.IsNullOrEmpty(packigId))
        {
            return dt;
        }

        string sql = "";

        try
        {

            sql = @"  select a.PackingBinOrdId, 
                       a.PackingId, a.BinId, b.Docentry, b.toWhsCode,
	                   b.ItemCode, a.Quantity CanPick, b.Draftquantity CanDraft, 
	                   a.PickedWhscode, 
	                   a.PickedUser, a.PickedDate, a.ReceivedWhscode, 
	                   a.ReceivedUser, a.ReceivedDate
                        from dbo.smm_Transdiscrep_drf1 b
                             left join 
                             MOB_PACKING_BIN_ORDENES_ITEMS a
                        on a.OrderId = b.Docentry
                        and a.ItemCode = b.ItemCode     
                        where a.PackingId =  " + packigId + @"
                        order by a.OrderId, b.ItemCode";



            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure UnoPickPackOrdersItems.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public DataTable cuatroReceiving(string ReceivingId)
    {
        DataTable dt = new DataTable();
        string sql = "";

        try
        {

            sql = @"select aa.ReceivingId Recibo,
                    aa.ReceivingWhscode Ubicacion, 
                    case when aa.ReceivingStatus='O' then 'Abierto' else 'Terminado' end  Estatus, 
                    aa.ReceivingUser Usuario, 
                    aa.ReceivingDate FechaRecibo,
                    bb.OrderId Orden, 
                    case when convert(varchar,bb.OrderId) is null then 'NO Escaneada' else 'Escaneada' end StatusOrden,
                    bb.ReceivedDate FechaOrdenRecibo,
                    cc.FromWhsCode DesdeUbicacion,
                    case when isnull(cc.DocStatus,'O') = 'O' then 'No Recibida en SAP' else 'Recibida en SAP' end StatusSAP,
                    cc.DocEntryTraRec2 NumeroSAP,
                    cc.UserReceive UsuarioRecibioOrden
                    from [dbo].[MOB_Receiving] aa
                    left join [dbo].[MOB_RECEIVING_ORDERS] bb
                    on aa.ReceivingId = bb.ReceivingId
                    left join smm_Transdiscrep_odrf cc
                    on bb.OrderId = cc.DocEntry
                    where aa.ReceivingId = case when '" + ReceivingId + @"' = 'Todos' then aa.ReceivingId else convert(int,'" + ReceivingId + @"')  end
                    order by case when aa.ReceivingStatus='O' then 'Abierto' else 'Terminado' end,
                    aa.ReceivingId desc";


            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure cuatroReceiving.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public DataTable cuatroReceivingOrderItems(string ReceivingId)
    {
        DataTable dt = new DataTable();
        string sql = "";

        try
        {

            sql = @"select cc.ReceivingId Recibo, 
                    aa.docentry Orden,  
                    case when bb.OrderId is null then 'Orden No Escaneada' else 'Orden Escaneada' end StatusOrden,
                    aa.ItemCode Producto, 
                    aa.itemname NombreProducto,
                    case when bb.ItemCode is null then 'Prod No Escaneado' else 'Prod No Escaneado' end StatusProducto,
                    case when oo.ScanStatus= 'O' then 'Abierto' else 'Terminado' end StatusLector,
                    case when oo.DocStatus = 'O' then 'No Recibida en SAP' else 'Recibida en SAP' end StatusSAP,
                    oo.DocNumTraRec2 NumeroSAP,
                    convert(int,aa.DispatchQuantity) CantidadDespachada, 
                    isnull(bb.Quantity,0) CantidadRecibidaScn,
                    isnull(aa.TmpQuantity,0) CantidadRecibida,
                    convert(int,isnull(aa.TmpQuantity,0) - isnull(aa.DispatchQuantity,0)) Diferencia,
                    bb.ReceivedUser UsuarioRecibo, 
                    bb.ReceivedDate FechaReciboProd,
                    bb.ReceivedWhscode UbicacionRecibo,
                    aa.UserRecScanner UsuarioReciboProd, 
                    convert(int,aa.ReceivedQuantity) CantidadSAP
                    --, bb.* , aa.*
                    from smm_Transdiscrep_drf1 aa
                    inner join smm_Transdiscrep_odrf oo
                    on aa.DocEntry = oo.DocEntry
                    inner join MOB_RECEIVING_ORDERS cc
                    on cc.OrderId = oo.DocEntry
                    left join [MOB_RECEIVING_ORDERS_ITEMS] bb
                    on cc.OrderId = bb.orderId
					and bb.Itemcode = aa.Itemcode
                    where cc.receivingId = case when '" + ReceivingId + @"' = 'Todos' then cc.receivingId else convert(int,'" + ReceivingId + @"')  end
                    order by cc.receivingId desc, isnull(aa.TmpQuantity,0)";


            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure cuatroReceivingOrderItems.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

}
