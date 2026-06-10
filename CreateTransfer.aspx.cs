using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;

public partial class CreateTransfer : BasePage
{
    protected SqlDb db = new SqlDb();
    DataTable dtToWhs;
    protected string usr;
    protected string Loc;
    protected string Item;
    protected string sap_db;
    protected string serverIP;
    protected string serverUserName;
    protected string serverPwd;
    protected string dbUserName;
    protected string dbPwd;
    protected string licenseServerIP;
    protected string xmlPath;
    protected string appUserName;
    protected string commentsMsg;
    protected string docdateDraft;
    protected string lCurUser;
    protected int serieDoc;

    protected void Page_Load(object sender, EventArgs e)
    {
        ValidaSesionNullOrEmpty(new string[] { "UserId", "CompanyId" });

        divMessage.InnerHtml = "";
        divMessage.Attributes["class"] = "alert-danger";

        appUserName = (string)Session["UserId"];

        lCurUser = (string)Session["UserId"];
        sap_db = (string)Session["CompanyId"];
        char flagokay = 'Y';
	    string lControlName = "CreateTransfer.aspx";
        string strRole_Description = "";
        string strAccessType = "";

        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, lControlName, ref strAccessType, ref strRole_Description);
        db.Disconnect();

        if (strAccessType == "N")
	    {
	        flagokay = 'N';
	        string message = "User " + lCurUser + ", with Role " + strRole_Description + " does not have permissions to access this screen.";
	        string url = string.Format("Default.aspx");
	        string script = "{ alert('";
	        script += message;
	        script += "');";
	        script += "window.location = '";
	        script += url;
	        script += "'; }";
	        ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "alert", script, true);
	    }

	    if (strAccessType == "R")
		{
		    btnCreateTransfer.Enabled = false;
		    btnCreateTransfer.ForeColor = Color.Silver;		    
		    labelForm.InnerText = "Create Min-Max Transfer (Read-Only Access)";
		}
		
	    if (strAccessType == "F")
		{
		    btnCreateTransfer.Enabled = true; 
		    labelForm.InnerText = "Create Min-Max Transfer (Full Access)";
		}		
        ///////////////End  New Control de acceso por Roles

        if (flagokay == 'Y')
        {
            serverIP = ConfigurationManager.AppSettings.Get("serverIP");
            serverUserName = ConfigurationManager.AppSettings.Get("serverUserName");
            serverPwd = ConfigurationManager.AppSettings.Get("serverPwd");
            dbUserName = ConfigurationManager.AppSettings.Get("dbUserName");
            dbPwd = ConfigurationManager.AppSettings.Get("dbPwd");
            licenseServerIP = ConfigurationManager.AppSettings.Get("licenseServerIP");
            serieDoc = int.Parse(ConfigurationManager.AppSettings.Get("CreateTransferSerieDoc"));

            //usr = User.Identity.Name.ToLower().Replace("lgihome\\", "");
            string x = User.Identity.Name.ToLower();
            usr = x.Substring(x.IndexOf("\\") + 1, x.Length - x.IndexOf("\\") - 1);

            divMessage.InnerHtml = "";

            if (!IsPostBack)
            {
                try
                {
                    db.Connect();
                    LoadWarehouses();
                    LoadItemGroups();
                }
                catch (Exception)
                {
                    
                }
                finally
                {
                    db.Disconnect();
                }
                
                InitializeForm();
            }   
        }
    }

    private void GoToLogin()
    {
        Response.Redirect("Login1.aspx");
    }

    private void ValidaSesionNullOrEmpty(string[] keyNames)
    {
        bool r = false;
        foreach (string keyName in keyNames)
        {
            if(Session[keyName] == null || (string)Session[keyName] == "")
            {
                r = true;
                break;
            }
        }
        
        if (r)
        {
            GoToLogin();
        }
    }

    private void ValidaSesionRptData(string keyName)
    {
        if (Session[keyName] == null || (DataTable)Session[keyName] == null)
        {
            GoToLogin();
        }
    }

    private void InitializeForm()
    {
        divMessage.InnerHtml = "";
    }

    private void LoadWarehouses()
    {
        DataTable dt = new DataTable();

        try
        {
            int branchId = 0;
            int.TryParse(Session["BranchId"] as string, out branchId);
            string branchFilter = branchId > 0 ? " AND O.BPLid IN (1, " + branchId + ")" : "";
            string sql =
            @"select O.WhsCode,
                     CONVERT(nvarchar(30), ISNULL(O.U_POSCode, '')) + ' - ' + O.WhsCode + ' - ' + O.WhsName AS WHS,
                     R.Control,
                     ISNULL(O.U_Type, '') AS WhsType
                 from " + sap_db + @".dbo.owhs O " + Queries.WITH_NOLOCK + @"
                 INNER JOIN RSS_OWHS_CONTROL R " + Queries.WITH_NOLOCK + @" ON O.WhsCode = R.WhsCode
                 where R.Control IN ('CRETRAFROM', 'CRETRATO')
                   AND R.CompanyId = '" + sap_db + @"'" + branchFilter + @"
              ORDER BY CASE WHEN O.BPLId = 1 THEN 0 ELSE 1 END, O.U_POSCode";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadWarehouses. ERROR MESSAGE: " + ex.Message);
        }

        DataTable dt1 = dt.AsEnumerable()
            .Where(x => x.Field<string>("Control") == "CRETRAFROM")
            .CopyToDataTable();

        drpFromWhsCode.DataSource = dt1;
        drpFromWhsCode.DataBind();

        DataTable dt2 = dt.AsEnumerable()
            .Where(x => x.Field<string>("Control") == "CRETRATO")
            .CopyToDataTable();

        drpToWhsCode.DataSource = dt2;
        drpToWhsCode.DataBind();

        Session["fromWhs"] = dt1;
        Session["toWhs"] = dt2;

        ListItem li = new ListItem("Select a location", "0");

        drpFromWhsCode.Items.Insert(0, li);
        drpToWhsCode.Items.Insert(0, li);
    }

    private void LoadItemGroups()
    {
        DataTable dt = new DataTable();

        try
        {
            string sql =
            @"select 
               ItmsGrpCod GroupCode, 
               cast(ItmsGrpCod as varchar) + ' - ' + ItmsGrpNam GroupName 
             from " + sap_db + @".dbo.oitb " + Queries.WITH_NOLOCK + @" 
            order by ItmsGrpCod";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadItemGroups. ERROR MESSAGE : " + ex.Message);
        }

        drpItemGroups.DataSource = dt;
        drpItemGroups.DataBind();

        ListItem li = new ListItem("Select a Group", "0");

        drpItemGroups.Items.Insert(0, li);
    }


    protected void btnSelectAll_Click(object sender, EventArgs e)
    {
        ValidaSesionNullOrEmpty(new string[] { "UserId", "CompanyId" });

        foreach (ListItem li in drpItemGroups.Items)
        {
            li.Selected = true;
        }
    }

    protected void btnCreateTransfer_Click(object sender, EventArgs e)
    {
        ValidaSesionNullOrEmpty(new string[] { "UserId", "CompanyId" });

        int execRes = 0;
        string FromWhsCode = drpFromWhsCode.SelectedValue;
        string ToWhsCode = drpToWhsCode.SelectedValue;
        string ItemGroups = "", brands = "";

        if (FromWhsCode == "0")
        {
            divMessage.InnerHtml = "You must select a 'From Location'";
            drpFromWhsCode.Focus();
            return;
        }

        if (ToWhsCode == "0")
        {
            divMessage.InnerHtml = "You must select a 'To Location'";
            drpToWhsCode.Focus();
            return;
        }

        foreach (ListItem li in drpItemGroups.Items)
        {
            if (li.Selected)
            {
                ItemGroups += li.Value + ",";
            }
        }

        if (ItemGroups.Length > 0)
        {
            ItemGroups = ItemGroups.Substring(0, ItemGroups.Length - 1);
        }
        else
        {
            divMessage.InnerHtml = "You must select at least one Item Group";
            drpItemGroups.Focus();
            return;
        }

        foreach (ListItem li in lstItemGroups.Items)
        {
            if (li.Selected)
            {
                brands += "'" + li.Value.ToString() + "',";
            }
        }

        if (brands.Length > 0)
        {
            brands = brands.Substring(0, brands.Length - 1);
        }
        else
        {
            divMessage.InnerHtml = "You must select at least one Brand";
            lstItemGroups.Focus();
            return;
        }

        try
        {
            db.Connect();
            string sql = "";

            if (brands != "'All Brands'")
            {
                sql = "delete from rss_results where companyId = '"+ sap_db + "' and FromWhsCode = '" + FromWhsCode + "' and ToWhsCode = '" + ToWhsCode + "' and itemcode COLLATE DATABASE_DEFAULT in (select itemcode from " + sap_db + " .dbo.oitm where replace(u_brand, '''','_') in (" + brands + "))";
            }
            else
            {
                sql = "delete from rss_results where companyId = '" + sap_db + "' and FromWhsCode = '" + FromWhsCode + "' and ToWhsCode = '" + ToWhsCode + "'";
            }

            db.cmd.CommandText = sql;
            db.cmd.CommandType = CommandType.Text;

            execRes = db.cmd.ExecuteNonQuery();

            //*************************************************************
            // NOTE - Removed case pack from transfer quantity for Punta Cana Test.  Now using 'each' as unit of me - 9/30/2010
            //case when (a.OnHand * isnull(d.u_bot,1)) >= (b.max_qty - c.OnHand) then round((b.max_qty - c.OnHand)/isnull(d.u_bot,1),0) else a.OnHand  end Transfer_Quantity,
            //*************************************************************
            // created smm_net_inventory_vw which includes in_transit quantities (on open transfer draft)  
            // 2011-02-16
            //*************************************************************
            sql = Queries.With_NetInventory() + @"
INSERT INTO rss_results
	SELECT distinct '{0}' AS CompanyId, 
	a.WhsCode AS FromWhsCode, 
	c.WhsCode AS ToWhsCode, 
	d.ItemCode, 
	d.ItemName AS ItemDescription, 
	d.ItmsGrpCod AS GroupCode, 
    (b.max_qty 
    + case when c.net_inventory + isnull(e.net_inventory,0) < b.max_qty + isnull(e.min_qty,0) then isnull(e.max_qty,0) else 0 end
    - c.net_inventory  
    + case when c.net_inventory + isnull(e.net_inventory,0) < b.max_qty + isnull(e.min_qty,0) then isnull(e.net_inventory,0) else 0 end)
    - 
    (
    (b.max_qty 
    + case when c.net_inventory + isnull(e.net_inventory,0) < b.max_qty + isnull(e.min_qty,0) then isnull(e.max_qty,0) else 0 end
    - c.net_inventory  
    + case when c.net_inventory + isnull(e.net_inventory,0) < b.max_qty + isnull(e.min_qty,0) then isnull(e.net_inventory,0) else 0 end
    ) % case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(d.u_bot,1) end )             
    AS Requested_Quantity,
    CASE 
        WHEN a.OnHand - a.in_transit_out >= 
            (b.max_qty 
		    + case when c.net_inventory + isnull(e.net_inventory,0) < b.max_qty + isnull(e.min_qty,0) then isnull(e.max_qty,0) else 0 end
		    - c.net_inventory  
		    + case when c.net_inventory + isnull(e.net_inventory,0) < b.max_qty + isnull(e.min_qty,0) then isnull(e.net_inventory,0) else 0 end)
		    - 
		    (
		    (b.max_qty 
		    + case when c.net_inventory + isnull(e.net_inventory,0) < b.max_qty + isnull(e.min_qty,0) then isnull(e.max_qty,0) else 0 end
		    - c.net_inventory  
		    + case when c.net_inventory + isnull(e.net_inventory,0) < b.max_qty + isnull(e.min_qty,0) then isnull(e.net_inventory,0) else 0 end
		    )
		    % case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(d.u_bot,1) end )             
        THEN 
            (b.max_qty 
		    + case when c.net_inventory + isnull(e.net_inventory,0) < b.max_qty + isnull(e.min_qty,0) then isnull(e.max_qty,0) else 0 end
		    - c.net_inventory  
		    + case when c.net_inventory + isnull(e.net_inventory,0) < b.max_qty + isnull(e.min_qty,0) then isnull(e.net_inventory,0) else 0 end)
		    - 
		    (
		    (b.max_qty 
		    + case when c.net_inventory + isnull(e.net_inventory,0) < b.max_qty + isnull(e.min_qty,0) then isnull(e.max_qty,0) else 0 end
		    - c.net_inventory  
		    + case when c.net_inventory + isnull(e.net_inventory,0) < b.max_qty + isnull(e.min_qty,0) then isnull(e.net_inventory,0) else 0 end
		    ) % case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(d.u_bot,1) end 
        ) 
        ELSE (a.OnHand - a.in_transit_out) - ((a.OnHand - a.in_transit_out) % case when isnull(mult.ORDER_MULTIPLE, 'E') = 'E' then 1 else isnull(d.u_bot,1) end
    )
    END AS Transfer_Quantity, getdate() Date_Created, '{1}' Created_By
	FROM NetInventory AS a " + Queries.WITH_NOLOCK + @"  
    INNER JOIN rss_store_item_min_max AS b " + Queries.WITH_NOLOCK + @"  ON a.ItemCode = b.item and a.CompanyId = b.CompanyId 
	INNER JOIN NetInventory AS c " + Queries.WITH_NOLOCK + @"  ON B.LOC = C.WHSCODE AND b.Item = c.ItemCode and b.CompanyId = c.CompanyId 
	INNER JOIN {0}.dbo.OITM AS d  " + Queries.WITH_NOLOCK + @"  ON a.ItemCode = d.ItemCode 
	INNER JOIN rss_loc_dept_multiple mult " + Queries.WITH_NOLOCK + @"  on d.ItmsGrpCod = mult.dept and mult.companyId = a.CompanyId and mult.LOC = b.LOC
	LEFT OUTER JOIN (
	    SELECT replacement_item AS item, SUM(b.MIN_QTY) min_qty, SUM(b.MAX_QTY) max_qty, SUM(c.net_inventory) AS net_inventory
	    FROM rss_store_item_min_max AS b " + Queries.WITH_NOLOCK + @" 
        INNER JOIN NetInventory AS c " + Queries.WITH_NOLOCK + @"  ON b.loc = c.whscode and b.item = c.ItemCode and b.CompanyId = c.CompanyId and c.CompanyId = '{0}'  
	    WHERE isnull(replacement_item,'') <> '' and (b.hold = 1) AND (c.net_inventory < b.min_qty) AND (b.loc = c.WhsCode) AND (c.WhsCode = '{2}')
	    GROUP BY replacement_item
	) AS e ON e.item = b.item
	WHERE  1=1 and a.whscode = '{3}' and b.loc = '{2}' and a.CompanyId = '{0}' and a.OnHand - a.in_transit_out > 0 and b.hold = 0 and d.ItmsGrpCod IN ({4}) 
	and (c.net_inventory < b.min_qty OR (c.net_inventory + isnull(e.net_inventory,0) < b.max_qty + isnull(e.min_qty,0) and e.item is not null))";

            if (brands != "'All Brands'")
            {
                sql += " and replace(d.u_brand, '''','_') in ({5})";
            }

            sql = string.Format(sql, sap_db, usr, ToWhsCode, FromWhsCode, ItemGroups, brands);

            db.cmd.CommandText = sql;
            db.cmd.CommandType = CommandType.Text;

            execRes = db.cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function btnCreateTransfer_Click. ERROR MESSAGE : " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        // ********************************************************************
        // create the draft now based on the data in rss_results
        // ********************************************************************

        //divMessage.InnerHtml += "<br>FromWhsCode: " + FromWhsCode + " ToWhsCode: " + ToWhsCode + " ItemGroups: " + ItemGroups + " brands: " + brands;

        //divMessage.InnerHtml = "Preparando XML";
        //return;

        int draftDocEntry;
        if (CreateSAPDraft(FromWhsCode, ToWhsCode, ItemGroups, brands, out draftDocEntry))
        {
            if (draftDocEntry > 0)
            {
                int sapDocNum;
                string dispErr = TransferAutoDispatch.RunAutoDispatch(
                    draftDocEntry, sap_db, sap_db, appUserName, out sapDocNum);

                if (dispErr != null)
                {
                    divMessage.InnerHtml = "SAP B1 Error: " + dispErr + " — The transfer was reverted.";
                    divMessage.Attributes["class"] = "alert-danger";
                    if (db.DbConnectionState == ConnectionState.Open) db.Disconnect();
                    return;
                }

                string sapRef = sapDocNum > 0
                    ? " Transfer Request #" + sapDocNum + " created in SAP B1."
                    : "";
                divMessage.InnerHtml += "<br>Transfer creado!" + sapRef;
            }
            divMessage.Attributes["class"] = "alert-success";
        }
        else
        {
            divMessage.InnerHtml += "<br>Transfer Failed.";
        }

        if (db.DbConnectionState == ConnectionState.Open)
        {
            db.Disconnect();
        }
    }

    protected bool CreateSAPDraft(string FromWhsCode, string ToWhsCode, string ItemGroups, string brands, out int docEntry)
    {
        int execRes = 0;
        docEntry = 0;
        string sqlcmd = "";

        if (brands == "'All Brands'")
        {
            sqlcmd = "SELECT a.CompanyId, a.FromWhsCode, a.ToWhsCode, a.ItemCode, a.ItemDescription, a.GroupCode, a.Requested_Quantity, a.Transfer_Quantity, a.Date_Created, a.Created_By , b.U_BRAND  FROM rss_results a, "   + sap_db + ".dbo.OITM b where a.companyId = '" + sap_db + "' and a.FromWhsCode = '" + FromWhsCode + "' and a.ToWhsCode = '" + ToWhsCode + "' and a.groupcode in (" + ItemGroups + ") and a.requested_quantity > 0 and a.transfer_quantity > 0  and a.ItemCode COLLATE DATABASE_DEFAULT = B.Itemcode COLLATE DATABASE_DEFAULT order by b.U_BRAND";
        }
        else
        {
            sqlcmd = "SELECT a.CompanyId, a.FromWhsCode, a.ToWhsCode, a.ItemCode, a.ItemDescription, a.GroupCode, a.Requested_Quantity, a.Transfer_Quantity, a.Date_Created, a.Created_By , b.U_BRAND  FROM rss_results a,   " + sap_db + ".dbo.OITM b where a.companyId = '" + sap_db + "' and a.FromWhsCode = '" + FromWhsCode + "' and a.ToWhsCode = '" + ToWhsCode + "' and a.groupcode in (" + ItemGroups + ") and a.itemcode COLLATE DATABASE_DEFAULT in (select itemcode from " + sap_db + " .dbo.oitm where replace(u_brand, '''','_') in (" + brands + ")) and a.requested_quantity > 0 and a.transfer_quantity > 0 and a.ItemCode COLLATE DATABASE_DEFAULT = B.Itemcode COLLATE DATABASE_DEFAULT order by b.U_BRAND ";
        }

        string lSql1 = "--"; string lSql2 = "--"; string lSql3 = "--"; string lSql4 = "--"; string lSql5 = "--";
        string lSql6 = "--"; string lSql7 = "--"; string lSql8 = "--"; string lSql9 = "--"; string lSql10 = "--";

        int lSqlLen = sqlcmd.Length;
        int lLen = 120;
        int lSqlDiv = sqlcmd.Length / lLen;
        int lSqlMod = sqlcmd.Length % lLen;

        int i = 1;

        while (i <= lSqlDiv)
        {
            switch (i)
            {
                case 1:
                    lSql1 = sqlcmd.Substring(0, lLen);
                    break;
                case 2:
                    lSql2 = sqlcmd.Substring(lLen, lLen);
                    break;
                case 3:
                    lSql3 = sqlcmd.Substring(2 * lLen, lLen);
                    break;
                case 4:
                    lSql4 = sqlcmd.Substring(3 * lLen, lLen);
                    break;
                case 5:
                    lSql5 = sqlcmd.Substring(4 * lLen, lLen);
                    break;
                case 6:
                    lSql6 = sqlcmd.Substring(5 * lLen, lLen);
                    break;
                case 7:
                    lSql7 = sqlcmd.Substring(6 * lLen, lLen);
                    break;
                case 8:
                    lSql8 = sqlcmd.Substring(7 * lLen, lLen);
                    break;
                case 9:
                    lSql9 = sqlcmd.Substring(8 * lLen, lLen);
                    break;
                case 10:
                    lSql10 = sqlcmd.Substring(9 * lLen, lLen);
                    break;

                default:

                    lSql1 = sqlcmd.Substring(1, lLen);
                    break;
            }

            i++;
        }

        lSql10 = sqlcmd.Substring(lSqlLen - lSqlMod, lSqlMod);

        string lSqlTmp = lSql1 + lSql2 + lSql3 + lSql4 + lSql5 + lSql6 + lSql7 + lSql8 + lSql9 + lSql10;

        try
        {
            if(db.DbConnectionState == ConnectionState.Closed)
            {
                db.Connect();
            }

            db.cmd = new SqlCommand
            {
                Connection = db.Conn,
                CommandType = CommandType.StoredProcedure,
                CommandText = "smm_populate_Smm_Draft",
                Parameters =
                {
                    new SqlParameter
                    {
                        ParameterName = "@SerieDoc",
                        SqlDbType = SqlDbType.Int,
                        Value = serieDoc
                    },
                    new SqlParameter
                    {
                        ParameterName = "@pSelect1",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = lSql1
                    },
                    new SqlParameter
                    {
                        ParameterName = "@pSelect2",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = lSql2
                    },
                    new SqlParameter
                    {
                        ParameterName = "@pSelect3",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = lSql3
                    },
                    new SqlParameter
                    {
                        ParameterName = "@pSelect4",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = lSql4
                    },
                    new SqlParameter
                    {
                        ParameterName = "@pSelect5",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = lSql5
                    },
                    new SqlParameter
                    {
                        ParameterName = "@pSelect6",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = lSql6
                    },
                    new SqlParameter
                    {
                        ParameterName = "@pSelect7",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = lSql7
                    },
                    new SqlParameter
                    {
                        ParameterName = "@pSelect8",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = lSql8
                    },
                    new SqlParameter
                    {
                        ParameterName = "@pSelect9",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = lSql9
                    },
                    new SqlParameter
                    {
                        ParameterName = "@pSelect10",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = lSql10
                    },
                    new SqlParameter
                    {
                        ParameterName = "@pUser",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = appUserName
                    },
                    new SqlParameter
                    {
                        ParameterName = "@pRowsInserted",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Output
                    },
                    new SqlParameter
                    {
                        ParameterName = "@pDrafNum",
                        SqlDbType = SqlDbType.Int,
                        Direction = ParameterDirection.Output
                    }
                }
            };

            int lRowsInserted = 0;
            int lDrafNum = 0;

            execRes =  db.cmd.ExecuteNonQuery();

            lRowsInserted = Convert.ToInt32(db.cmd.Parameters["@pRowsInserted"].Value);
            lDrafNum = Convert.ToInt32(db.cmd.Parameters["@pDrafNum"].Value);
            // Get DocEntry by DocNum for auto-dispatch
            using (var qCmd = new SqlCommand(
                "SELECT TOP 1 DocEntry FROM SMM_odrf WHERE CompanyId=@cid AND DocNum=@dnum ORDER BY DocEntry DESC",
                db.Conn))
            {
                qCmd.Parameters.AddWithValue("@cid",  sap_db);
                qCmd.Parameters.AddWithValue("@dnum", lDrafNum);
                var qVal = qCmd.ExecuteScalar();
                if (qVal != null && qVal != DBNull.Value)
                    docEntry = Convert.ToInt32(qVal);
            }
            if (docEntry == 0) docEntry = lDrafNum;       

            if (lRowsInserted > 0)
            {
                divMessage.InnerHtml += "<br>Transfer completed successfully. Number: " + lDrafNum;
            }
            else
            {
                divMessage.InnerHtml += "<br>Transfer not completed. Please verify that there is sufficient inventory in the source warehouse, that the MIN values are less than the current stock at the destination location, and check if there are any items in transit.";
                db.Disconnect();
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in call procedure [smm_populate_Smm_Draft]. ERROR MESSAGE : " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return true;
    }

    protected XmlElement XmlTextNode(XmlDocument doc, string nodeName, string nodeText)
    {
        XmlElement result = doc.CreateElement(nodeName);
        result.AppendChild(doc.CreateTextNode(nodeText));
        return result;
    }

    protected void drpItemGroups_SelectedIndexChanged(object sender, EventArgs e)
    {
        ValidaSesionNullOrEmpty(new string[] { "UserId", "CompanyId" });

        db.Connect();
        try
        {
            LoadBrands();
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            db.Disconnect();
        }
    }

    private void LoadBrands()
    {
        DataTable dt = new DataTable();
        string itmsgrpcods = "";

        foreach (ListItem li in drpItemGroups.Items)
        {
            if (li.Selected)
            {
                itmsgrpcods = itmsgrpcods + li.Value.ToString() + ",";
            }
        }

        itmsgrpcods = itmsgrpcods.Remove(itmsgrpcods.Length - 1);

        try
        {
            string sql =
            @"select brand from
                (
                select 1 as sortorder, 'All Brands' brand 
                union
                select distinct 2 as sortorder, replace(u_brand, '''','_') brand 
                from " + sap_db + @".dbo.oitm " + Queries.WITH_NOLOCK + @" 
                where itmsgrpcod in (" + itmsgrpcods + @") 
                and u_brand is not null 
                ) a
              order by sortorder, brand";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadBrands. ERROR MESSAGE : " + ex.Message + "itmsgrpcods: " + itmsgrpcods);
        }
        lstItemGroups.DataSource = dt;
        lstItemGroups.DataBind();
    }

    protected void btnCreateTransfer_Load(object sender, EventArgs e)
    {
        ArrayList roles = new ArrayList();

        roles = (ArrayList)this.Session["Roles"];

        string thiscontrol = null;

        for (int i = 0; i < roles.Count; i++)
        {
            thiscontrol = (roles[i].ToString());
            if ((thiscontrol == "READONLY"))
            {
                btnCreateTransfer.Visible = false;
            }

        }
    }

    protected void drpFromWhsCode_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            ValidaSesionNullOrEmpty(new string[] { "UserId", "CompanyId" });
            ValidaSesionRptData("toWhs");

            dtToWhs = (DataTable)Session["toWhs"];

            if (dtToWhs.Rows.Count <= 0)
            {
                Alert.Show("No destinations available");
            }
            else
            {
                string selectedFromWhs = drpFromWhsCode.SelectedValue;
                string fromWhsType = "";

                DataTable dtFromWhs = (DataTable)Session["fromWhs"];
                if (dtFromWhs != null)
                {
                    DataRow[] fromRows = dtFromWhs.Select("WhsCode = '" + selectedFromWhs + "'");
                    if (fromRows.Length > 0)
                        fromWhsType = fromRows[0]["WhsType"].ToString();
                }

                var filtered = dtToWhs.AsEnumerable()
                    .Where(x => x.Field<string>("WhsCode") != selectedFromWhs);

                if (!string.IsNullOrEmpty(fromWhsType))
                    filtered = filtered.Where(x => x.Field<string>("WhsType") == fromWhsType);

                drpToWhsCode.DataSource = filtered.Any() ? filtered.CopyToDataTable() : dtToWhs.Clone();
                drpToWhsCode.DataBind();
            }

            ListItem li = new ListItem("Select a location", "0");
            drpToWhsCode.Items.Insert(0, li);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadWarehouses for toloc. ERROR MESSAGE : " + ex.Message);
        }
    }
}
