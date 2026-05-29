using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using Telerik;
using Telerik.Web.UI;
using System.Data.SqlClient;
using System.Drawing;

public partial class FillPriority : BasePage
{
    protected SqlDb db = new SqlDb();
    protected DFBUYINGdb dfbDB = new DFBUYINGdb();
    protected string lCurUser; 
    public DataTable dt = new DataTable();
    protected string sap_db;
    private int WarehouseCount
    {
        get { return ViewState["WarehouseCount"] != null ? (int)ViewState["WarehouseCount"] : 0; }
        set { ViewState["WarehouseCount"] = value; }
    }
    private bool IsNewCategory
    {
        get { return ViewState["IsNewCategory"] != null && (bool)ViewState["IsNewCategory"]; }
        set { ViewState["IsNewCategory"] = value; }
    }
    private static string TOOLTIP_TEMPLATE = @"
                <div class=""container"" style=""width:200px;margin-left:2px;"">
                    <div class=""row"" style=""margin-left:2px;"">Store: {0}</div>
                    <div class=""row"" style=""margin-left:2px;"">Active Promos: {1}</div>
                </div>";
    protected void Page_Load(object sender, EventArgs e)
    {
        if ((string)Session["UserId"] == "" || (string)Session["UserId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

        if ((string)Session["CompanyId"] == "" || (string)Session["CompanyId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

        sap_db = (string)Session["CompanyId"];

        ///////////////Begin New  Control de acceso por Roles
        lCurUser = (string)Session["UserId"];
        char flagokay = 'Y';
	    string lControlName = "FillPriority.aspx";
        string strAccessType = "";
        string strRole_Description = "";

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
		    rbtnSave.Enabled = false;
		    rbtnSave.ForeColor = Color.Silver;	    
		    labelForm.InnerText = "Fill Priority (Read-Only Access)";
		}
		
	    if (strAccessType == "F")
		{
		    rbtnSave.Enabled = true;
		    labelForm.InnerText = "Fill Priority (Full Access)";
		}		
        ///////////////End  New Control de acceso por Roles

	    if (flagokay == 'Y')
        {
		    try
		    {
		        if (!IsPostBack)
		        {
			        if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
			        {
			            Response.Redirect("Login1.aspx");
			        }
			        else
			        {
                        LoadItemGroup();
			            LoadLocations();
			        }
		        }
		    }
		    catch (Exception ex)
		    {
		        ShowMasterPageMessage("Error", "Failed in Page_Load", ex.Message.ToString());
		        return;
		    }
        }
    }
    
    private void ShowMasterPageMessage(string v_Message_Type, string v_Message_Title, string v_Message)
    {
        try
        {
            SiteMaster sm = (SiteMaster)this.Master;
            sm.ShowDivMessage(v_Message_Type, v_Message_Title, v_Message);
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed in ShowMasterPageMessage", ex.Message.ToString());
            return;
        }
    }
    private void LoadItemGroup()
    {
        DataTable dt = new DataTable();

        try
        {
            string sql =
            @"select 
               ItmsGrpCod GroupCode, 
                lower(ItmsGrpNam) GroupID,
               cast(ItmsGrpCod as varchar) + ' - ' + [dbo].[InitCap] (ItmsGrpNam) GroupName 
               --[dbo].[InitCap] (ItmsGrpNam) GroupName 
             from " + sap_db + @".dbo.oitb
             order by GroupName ";
            db.Connect();
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);

            rcbCategory.DataTextField = "GroupName";
            rcbCategory.DataValueField = "GroupCode";
            rcbCategory.DataSource = dt;
            rcbCategory.DataBind();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadItemGroup. ERROR MESSAGE : " + ex.Message);
        }
    }
    private void LoadLocations()
    {

        DataTable dt = new DataTable();

        try
        {
            //string sql = "SELECT COMPANYCODE, WHSCODE, WHSNAME, WHSCODE + ' - ' + WHSNAME WHS FROM COMPANY_WHS_VW WHERE LOWER(COMPANYCODE) = '" + sap_db.ToLower() + "'";
            //db.Connect();
            //db.adapter = new SqlDataAdapter(sql, db.Conn);
            //db.adapter.Fill(dt);

            //rcbWhs.DataTextField = "WHS";
            //rcbWhs.DataValueField = "WHSCODE";
            //rcbWhs.DataSource = dt;
            //rcbWhs.DataBind();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadItemGroup. ERROR MESSAGE : " + ex.Message);
        }
    }

    protected void rcbCategory_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        try
        {
            if (rcbCategory.SelectedValue != "")
            {
                DataTable dtExisting = GetFillPriority();
                if (dtExisting.Rows.Count == 0)
                {
                    IsNewCategory = true;
                    rgPriority.DataSource = GetWarehousesAsNewPriority();
                }
                else
                {
                    IsNewCategory = false;
                    rgPriority.DataSource = dtExisting;
                }
                rgPriority.Rebind();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function Category Index Changed. ERROR MESSAGE : " + ex.Message);
        }
    }
    private DataTable GetWarehousesAsNewPriority()
    {
        DataTable dt = new DataTable();
        try
        {
            string sql = "SELECT b.WhsCode AS [Location], b.WhsName AS LocationName, b.U_POSCode FROM " + sap_db + ".dbo.OWHS b " + Queries.WITH_NOLOCK + " WHERE b.BPLId = " + BranchId + " ORDER BY b.U_POSCode, b.WhsCode";
            db.Connect();
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);

            dt.Columns.Add("Company", typeof(string));
            dt.Columns.Add("Dept", typeof(int));
            dt.Columns.Add("Priority", typeof(int));

            int priority = 1;
            foreach (DataRow row in dt.Rows)
            {
                row["Company"] = sap_db;
                row["Dept"] = Convert.ToInt32(rcbCategory.SelectedValue);
                row["Priority"] = priority++;
            }

            WarehouseCount = dt.Rows.Count;
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in GetWarehousesAsNewPriority. ERROR MESSAGE: " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
        return dt;
    }
    private DataTable GetFillPriority()
    {
        DataTable dt = new DataTable();

        try
        {
            string sql = "select a.Company, a.[Location], b.WhsName AS LocationName, a.Dept, a.[Priority], b.U_POSCode from Repln_Location_Priority a " + Queries.WITH_NOLOCK + @"  INNER JOIN " + sap_db + ".dbo.OWHS b " + Queries.WITH_NOLOCK + @"  ON a.Location = b.WhsCode where lower(company) = lower('" + sap_db + "') and dept = " + rcbCategory.SelectedValue + " and b.BPLId = " + BranchId + " order by dept, priority, b.U_POSCode";
            db.Connect();
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
            WarehouseCount = dt.Rows.Count;

            //rgPriority.DataSource = dt;
            //rgPriority.Rebind();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadItemGroup. ERROR MESSAGE : " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
        return dt;
    }

    //protected void rcbCompany_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    //{
    //    if (rcbCompany.SelectedValue != "" && rcbCategory.SelectedValue != "")
    //    {
    //        rgPriority.DataSource = GetFillPriority();
    //        rgPriority.Rebind();
    //    }
    //}

    protected void rgPriority_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        if (rcbCategory.SelectedValue != "")
        {
            divGrid.Attributes.Add("style", "display:block");
            rgPriority.DataSource = IsNewCategory ? GetWarehousesAsNewPriority() : GetFillPriority();
        }
    }
    protected void rgPriority_ItemDataBound(object sender, GridItemEventArgs e)
    {
        if (e.Item is GridDataItem)
        {
            GridDataItem dataItem = e.Item as GridDataItem;
            RadComboBox rcbPriority = (RadComboBox)dataItem["Priority"].FindControl("rcbPriority");
            HiddenField hfPriority = (HiddenField)dataItem["Priority"].FindControl("hfPriority");

            rcbPriority.Items.Clear();
            for (int i = 1; i <= WarehouseCount; i++)
                rcbPriority.Items.Add(new RadComboBoxItem(i.ToString(), i.ToString()));

            rcbPriority.SelectedValue = hfPriority.Value;
        }
    }
    protected void rbtnSave_Click(object sender, EventArgs e)
    {
        if (rgPriority.MasterTableView.Items.Count > 0)
        {
            DataTable dt = new DataTable();
            dt.Columns.AddRange(new DataColumn[2]{
                           new DataColumn("Location", typeof(string )),
                           new DataColumn("Priority", typeof(string))});

            foreach (GridDataItem item in rgPriority.MasterTableView.Items)
            {
                string v_Loc = item["LOCATION"].Text;
                RadComboBox rcbPriority = (RadComboBox)item["Priority"].FindControl("rcbPriority");
                string v_Priority = rcbPriority.SelectedValue;

                dt.Rows.Add(v_Loc, v_Priority);
            }

            var duplicates = dt.AsEnumerable().GroupBy(r => r[1]).Where(gr => gr.Count() > 1).ToList();
            if (duplicates.Count > 0)
            {
                ShowMasterPageMessage("Error", "Failed to save priorities", "Priorities cannot be saved as there are some duplicates.");
            }
            else
            {
                if (rcbCategory.SelectedValue != "")
                {
                    db.Connect();
                    foreach (DataRow dr in dt.Rows)
                    {
                        string sql;
                        if (IsNewCategory)
                            sql = "INSERT INTO Repln_Location_Priority (Company, Location, Dept, Priority, Branch) VALUES ('" + sap_db + "', '" + dr["Location"] + "', " + rcbCategory.SelectedValue + ", " + dr["Priority"] + ", " + BranchId + ")";
                        else
                            sql = "UPDATE Repln_Location_Priority SET Priority = " + dr["Priority"] + " WHERE lower(company) = lower('" + sap_db + "') AND dept = " + rcbCategory.SelectedValue + " AND Location = '" + dr["Location"] + "'";

                        db.cmd.CommandText = sql;
                        db.cmd.CommandType = CommandType.Text;
                        db.cmd.ExecuteNonQuery();
                    }
                    db.Disconnect();

                    string successMsg = IsNewCategory ? "Category priorities created successfully." : "Priorities updated successfully.";
                    IsNewCategory = false;
                    ShowMasterPageMessage("Ok", "Success", successMsg);
                    rgPriority.Rebind();
                }
                else
                {
                    ShowMasterPageMessage("Error", "Error", "Please refresh the page and try again.");
                }
            }
        }
        else
        {
            ShowMasterPageMessage("Error", "Error", "No data to save.");
        }
    }
}