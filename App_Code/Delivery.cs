using System;
using System.Collections.Generic;
using System.Web;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

/// <summary>
/// Summary description for Delivery
/// </summary>
public class Delivery
{
    protected SqlDb db;
    protected string sap_db;
    protected string tienda_db;
    protected string whs_code;
    protected string branchId;

	public Delivery()
	{
        db = new SqlDb();
        db.Connect();

        System.Web.HttpContext ctx = System.Web.HttpContext.Current;
        sap_db = (ctx != null && ctx.Session != null) ? ctx.Session["CompanyId"] as string : null;
        tienda_db = (ctx != null && ctx.Session != null) ? ctx.Session["tienda_db"] as string : null;
        branchId = (ctx != null && ctx.Session != null) ? ctx.Session["BranchId"] as string : null;
        if (string.IsNullOrEmpty(sap_db)) sap_db = ConfigurationManager.AppSettings.Get("smm_db");
        if (string.IsNullOrEmpty(tienda_db)) tienda_db = ConfigurationManager.AppSettings.Get("tienda_db");
        whs_code = ConfigurationManager.AppSettings.Get("whs_code");
	}

    public DataTable GetDeliveryErrors(string companyId)
    {
        sap_db = companyId;
        DataTable dt = new DataTable();
        string sql = "";

        try
        {

		sql = @"
                select
	                a.id,
                    b.id AS sales_id,
	                a.transnum,
	                a.itemnum,
	                (select TOP 1 bb.whsname
                        from dbo.ADR_TIENDAS_VW aa,
                             " + sap_db + @".dbo.owhs bb
                        where aa.whscode = bb.whscode
                        and aa.storenum =  a.storenum) storenum,
	                CONVERT(smalldatetime,CONVERT(varchar,a.itemdatetime,101)) itemdatetime,
                    OldBarCode=STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=a.skunum FOR XML PATH ('')), 1, 3, ''),
	                a.skunum,
                    NewBarCode=IIF(b.skunum=a.skunum, '', STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=b.skunum FOR XML PATH ('')), 1, 3, '')),
                    case when b.skunum = a.skunum then '' else b.skunum end new_sku,
                    [dbo].[InitCap] (case when d.itemcode is not null then d.itemname else a.pludesc end) description,
	                a.qty sale_qty,
                      isnull(c.onhand,0)  whs_qty,
                    tt.WhsCode whs_code,
	                a.error_message
                from
	                dbo.la_delivery_errors a " + Queries.WITH_NOLOCK + @"
				cross apply (SELECT TOP 1 WhsCode FROM ADR_TIENDAS_VW " + Queries.WITH_NOLOCK + @" WHERE storenum = a.storenum) tt
				inner join dbo.la_store_sales b " + Queries.WITH_NOLOCK + @"  on  a.transnum = b.transnum
                                                                                and a.itemnum  = b.itemnum
                                                                                and a.storenum = b.storenum
				left outer join " + sap_db + @".dbo.oitw c " + Queries.WITH_NOLOCK + @"  on b.skunum = c.itemcode and c.WhsCode = tt.WhsCode COLLATE SQL_Latin1_General_CP850_CI_AS
				left outer join " + sap_db + @".dbo.oitm d " + Queries.WITH_NOLOCK + @"  on c.itemcode = d.itemcode
                where
	                ISNULL(b.DeliveryDocNum,-1) < 0
                    and a.CompanyId = '" + branchId + @"'
                    and b.CompanyId = '" + branchId + @"'
               order by CONVERT(smalldatetime,CONVERT(varchar,a.itemdatetime,101)), a.storenum,a.itemnum
                ";


            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure GetDeliveryErrors.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    //id, skunum, storenum, new_sku, transnum, itemnum, itemdatetime, sale_qty, whs_qty, error_message

/*
    public void UpdateDeliveryItemNumber(string id, string new_sku)
    {
        string sql = "";

        try
        {
            sql = @"
                update
	                dbo.la_store_sales
                set 
                    skunum = '" + new_sku.Trim() + @"'
                where 
	                id = " + id;

            db.cmd.CommandText = sql;
            db.cmd.CommandType = CommandType.Text;
            db.cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure UpdateDeliveryItemNumber.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
    }
	*/
	
	public void UpdateDeliveryItemNumber(string id, string new_sku, string companyId)
    {
        string sql = "";
        int result = 0;
        string message = "";
        sap_db = companyId;

        try
        {
            sql = "SMM_UpdateDeliveryItemNumber_PRC";

            db.cmd.CommandText = sql;
            db.cmd.CommandType = CommandType.StoredProcedure;

            db.cmd.Parameters.Clear();

            /*INPUT PARAMS:*/
            db.cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int));
            db.cmd.Parameters["@Id"].Value = int.Parse(id);

            db.cmd.Parameters.Add(new SqlParameter("@new_sku", SqlDbType.VarChar));
            db.cmd.Parameters["@new_sku"].Value = new_sku.Trim();

            db.cmd.Parameters.Add(new SqlParameter("@sap_db", SqlDbType.VarChar));
            db.cmd.Parameters["@sap_db"].Value = sap_db.Trim();

            /*OUTPUT PARAMS:*/
            SqlParameter resultParam = new SqlParameter("@Result", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            db.cmd.Parameters.Add(resultParam);

            SqlParameter messageParam = new SqlParameter("@Message", SqlDbType.VarChar)
            {
                Direction = ParameterDirection.Output,
                Size = 1000
            };
            db.cmd.Parameters.Add(messageParam);


            db.cmd.ExecuteNonQuery();

            result = int.Parse(db.cmd.Parameters["@Result"].Value.ToString());
            message = db.cmd.Parameters["@Message"].Value.ToString();

            if(result <= 0)
            {
                throw new Exception(message);
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure UpdateDeliveryItemNumber.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
    }

    public DataTable GetDeliveryErrorsItems(string companyId)
    {
        sap_db = companyId;
        DataTable dt = new DataTable();
        string sql = "";

        try
        {

            sql = @"
                with ErrosDel as
(
select 
                    OldBarCode=STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1 with(nolock) WHERE a1.ItemCode=a.skunum FOR XML PATH ('')), 1, 3, ''),
	                a.skunum,  
                    NewBarCode=IIF(b.skunum=a.skunum, '', STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM " + sap_db + @".dbo.OBCD a1  with(nolock) WHERE a1.ItemCode=b.skunum FOR XML PATH ('')), 1, 3, '')),
                    case when b.skunum = a.skunum then '' else b.skunum end new_sku,
                    [dbo].[InitCap] (case when d.itemcode is not null then d.itemname else a.pludesc end) description,
	                a.qty sale_qty, 
                      isnull(c.onhand,0)  whs_qty,
                    tt.WhsCode whs_code,
                    tt.STORENUM

                from 
	                dbo.la_delivery_errors a  with(nolock)
				inner join ADR_TIENDAS_VW tt  with(nolock)  ON a.storenum=tt.storenum and a.CompanyId =  '" + branchId + @"'
				inner join dbo.la_store_sales b  with(nolock)  on a.id = b.id
				left outer join " + sap_db + @".dbo.oitw c  with(nolock)  on b.skunum = c.itemcode and c.WhsCode = tt.WhsCode COLLATE SQL_Latin1_General_CP850_CI_AS
				left outer join " + sap_db + @".dbo.oitm d  with(nolock)  on c.itemcode = d.itemcode
                where
	                ISNULL(b.DeliveryDocNum,-1) < 0
                    and a.CompanyId =  '" + branchId + @"'  
			   )

			   select OldBarCode,	skunum,	NewBarCode,	new_sku,	description,	sum(sale_qty) sale_qty,	sum(whs_qty) whs_qty,	whs_code,STORENUM
			   from ErrosDel
			   group by OldBarCode,	skunum,	NewBarCode,	new_sku,	description,	whs_code,STORENUM
                ";


            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure GetDeliveryErrors.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public void UpdateDeliveryItemCode(string skunum, string new_sku, string whs_code, string companyId)
    {
        string sql = "";
        int result = 0;
        string message = "";
        sap_db = companyId;

        try
        {
            sql = "SMM_UpdateDeliveryItemCode_PRC";

            db.cmd.CommandText = sql;
            db.cmd.CommandType = CommandType.StoredProcedure;

            db.cmd.Parameters.Clear();

            /*INPUT PARAMS:*/
            db.cmd.Parameters.Add(new SqlParameter("@sku", SqlDbType.VarChar));
            db.cmd.Parameters["@sku"].Value = skunum.Trim(); ;

            db.cmd.Parameters.Add(new SqlParameter("@new_sku", SqlDbType.VarChar));
            db.cmd.Parameters["@new_sku"].Value = new_sku.Trim();

            db.cmd.Parameters.Add(new SqlParameter("@whs_code", SqlDbType.VarChar));
            db.cmd.Parameters["@whs_code"].Value = whs_code.Trim();

            db.cmd.Parameters.Add(new SqlParameter("@sap_db", SqlDbType.VarChar));
            db.cmd.Parameters["@sap_db"].Value = sap_db.Trim();

            /*OUTPUT PARAMS:*/
            SqlParameter resultParam = new SqlParameter("@Result", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            db.cmd.Parameters.Add(resultParam);

            SqlParameter messageParam = new SqlParameter("@Message", SqlDbType.VarChar)
            {
                Direction = ParameterDirection.Output,
                Size = 1000
            };
            db.cmd.Parameters.Add(messageParam);


            db.cmd.ExecuteNonQuery();

            result = int.Parse(db.cmd.Parameters["@Result"].Value.ToString());
            message = db.cmd.Parameters["@Message"].Value.ToString();

            if (result <= 0)
            {
                throw new Exception(message);
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure UpdateDeliveryItemCode.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
    }


    public DataTable GetDocumentTransactions(string DocNum, string DocType, string companyId)
    {
        DataTable dt = new DataTable();
        string sql = "";
        sap_db = companyId;

        if (DocNum == null)
        {
            return dt;
        }

        // ***** NOTE ********
        // Setup parameter in web.config to determine whether to use DocTotal or DocTotalFC for field 2 below
        //********************
        try
        {
            sql = @"select 
    			    d.DocNum Delivery,
    			    d.DocTotalFC DeliveryTotal,    
    			    a.transnum, 
    			    a.itemnum linenum, 
    			    a.storenum, 
    			    c.WhsCode whs_code,
    			    a.itemdatetime, 
    			    a.skunum item,
    			    b.itemname description,
    			    a.qty sale_qty,
    			    a.stdunitprice,
    			    a.extsellprice
		    from 
    			    dbo.la_store_sales a inner join
			        " + tienda_db + @".dbo.trans tt " + Queries.WITH_NOLOCK + @"  ON a.storenum=tt.storenum inner join
    			    " + sap_db + @".dbo.oitm b on a.skunum = b.itemcode inner join 
    			    " + sap_db + @".dbo.oitw c on b.itemcode = c.itemcode and c.WhsCode = tt.WhsCode COLLATE SQL_Latin1_General_CP850_CI_AS inner join
    			    " + sap_db + @".dbo.";
                sql += (DocType.ToLower() == "delivery") ? "odln" : "ordn";
                sql += @"    d on d.DocNum = a.DeliveryDocNum
		    where 
    			    a.txnmodifier = ";
                sql += (DocType.ToLower() == "delivery") ? "0" : "1";
    	        sql += "	and d.DocNum = " + DocNum + @" 
		    order by 
    			a.transnum, 
    			a.itemnum";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in procedure GetDeliveryTransactions.  ERROR MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }
}
