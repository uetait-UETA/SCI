using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections;
using Telerik;
using Telerik.Web.UI;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;

public partial class DeliveryErrors : BasePage
{
    protected SqlDb db = new SqlDb();
    public DataTable dt = new DataTable();
    protected string sap_db;
    protected string lCurUser;

    protected void Page_Load(object sender, EventArgs e)
    {
        
        
        ///////////////Begin New  Control de acceso por Roles
        if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

        lCurUser = (string)this.Session["UserId"];
	    string lControlName = "DeliveryErrors.aspx";
        string strAccessType = "";
        string strRole_Description = "";

        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, lControlName, ref strAccessType, ref strRole_Description);
        db.Disconnect();

        if (strAccessType == "N")
	    {
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
	
		    //btnCreateTransfer.Enabled = false;
		    //btnCreateTransfer.ForeColor = Color.Silver;
		    		    
		    labelForm.InnerText = "Delivery Errors (Read-Only Access)"; //////////<<<<<<<<
		            
		            
		}
		
	    if (strAccessType == "F")
		{
	
		    //btnCreateTransfer.Enabled = true;
		    
		    labelForm.InnerText = "Delivery Errors (Full Access)"; //////////<<<<<<<<
		            
		            
		}		


        ///////////////End  New Control de acceso por Roles

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
                    sap_db = (string)this.Session["CompanyId"];
                    CompanyIdLabel.Text = sap_db;
                }
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed in Page_Load", ex.Message.ToString());
            return;
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

    protected void rgHead_UpdateCommand(object sender, Telerik.Web.UI.GridCommandEventArgs e)
    {
        try
        {
            lCurUser = (string)this.Session["UserId"];
            char flagokay = 'Y';
            string lControlName = "DeliveryErrors.aspx";
            string strAccessType = "";
            string strRole_Description = "";

            db.Connect();
            db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, lControlName, ref strAccessType, ref strRole_Description);
            db.Disconnect();

            if (strAccessType == "R")
            {
                flagokay = 'N';
                string message = "User " + lCurUser + " does not have permissions to update in this screen.";
                //string url = string.Format("Default.aspx");
                string script = "{ alert('";
                script += message;
                script += "');";
                script += "window.location = '";
                script += lControlName;
                script += "'; }";
                ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "alert", script, true);

                return;
            }
            else
            {
            GridEditableItem item = e.Item as GridEditableItem;
            string sapDb          = (string)Session["CompanyId"];
            string v_SalesId      = item.GetDataKeyValue("sales_id").ToString();
            object _whsKey = item.GetDataKeyValue("whs_code");
            string currentWhsCode = _whsKey != null ? _whsKey.ToString() : "";

            // Read new item from hidden field (set by RadSearchBox)
            string newSku = hfNewSku.Value.Trim();

            // Read new whs_code directly from the TextBox in the edit row
            System.Web.UI.WebControls.TextBox txtWhs = item.FindControl("txtWhsCode") as System.Web.UI.WebControls.TextBox;
            string newWhsCode = txtWhs != null ? txtWhs.Text.Trim() : "";

            bool skuChanged = !string.IsNullOrEmpty(newSku);
            bool whsChanged = !string.IsNullOrEmpty(newWhsCode) &&
                              !string.Equals(newWhsCode, currentWhsCode, StringComparison.OrdinalIgnoreCase);

            if (!skuChanged && !whsChanged)
            {
                // Nothing changed — just close edit
                rgHead.Rebind();
                return;
            }

            db.Connect();

            // Get current item code to use in validation when only whs changes
            string currentSku = "";
            using (SqlCommand cmd = new SqlCommand(
                "SELECT ISNULL(skunum,'') FROM dbo.la_store_sales WHERE id = @Id", db.Conn))
            {
                cmd.Parameters.AddWithValue("@Id", v_SalesId);
                object r = cmd.ExecuteScalar();
                currentSku = r != null ? r.ToString() : "";
            }

            string effectiveItem = skuChanged ? newSku : currentSku;
            string effectiveWhs  = whsChanged ? newWhsCode : currentWhsCode;

            // Validate warehouse exists if it changed
            if (whsChanged)
            {
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM " + sapDb + ".dbo.OWHS WHERE WhsCode = @WhsCode", db.Conn))
                {
                    cmd.Parameters.AddWithValue("@WhsCode", newWhsCode);
                    int cnt = (int)cmd.ExecuteScalar();
                    if (cnt == 0)
                    {
                        db.Disconnect();
                        ShowMasterPageMessage("Error", "Invalid Warehouse",
                            "Warehouse '" + newWhsCode + "' does not exist.");
                        e.Canceled = true;
                        return;
                    }
                }
            }

            // Validate item U_Type vs effective warehouse U_Type
            if (!string.IsNullOrEmpty(effectiveItem) && !string.IsNullOrEmpty(effectiveWhs))
            {
                string itemUType = "";
                string whsUType  = "";
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(U_Type,'') FROM " + sapDb + ".dbo.OITM WHERE ItemCode = @ItemCode", db.Conn))
                {
                    cmd.Parameters.AddWithValue("@ItemCode", effectiveItem);
                    object r = cmd.ExecuteScalar();
                    itemUType = r != null ? r.ToString() : "";
                }
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(U_Type,'') FROM " + sapDb + ".dbo.OWHS WHERE WhsCode = @WhsCode", db.Conn))
                {
                    cmd.Parameters.AddWithValue("@WhsCode", effectiveWhs);
                    object r = cmd.ExecuteScalar();
                    whsUType = r != null ? r.ToString() : "";
                }
                if (!string.IsNullOrEmpty(itemUType) && !string.IsNullOrEmpty(whsUType) &&
                    !string.Equals(itemUType.Trim(), whsUType.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    db.Disconnect();
                    ShowMasterPageMessage("Error", "Type Mismatch",
                        "Item '" + effectiveItem + "' is " + itemUType +
                        " but warehouse '" + effectiveWhs + "' is " + whsUType +
                        ". They must be the same operation type.");
                    e.Canceled = true;
                    return;
                }
            }

            db.Disconnect();

            // Save item if changed
            if (skuChanged)
            {
                Delivery dy = new Delivery();
                dy.UpdateDeliveryItemNumber(v_SalesId, newSku, (string)Session["BranchId"]);
            }

            // Save whs_code if changed
            if (whsChanged)
            {
                db.Connect();
                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE dbo.la_store_sales SET WhsCode = @WhsCode WHERE id = @Id", db.Conn))
                {
                    cmd.Parameters.AddWithValue("@WhsCode", newWhsCode);
                    cmd.Parameters.AddWithValue("@Id", v_SalesId);
                    cmd.ExecuteNonQuery();
                }
                db.Disconnect();
            }

            hfNewSku.Value = "";
            rgHead.Rebind();
            }


        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Faled to update data", ex.Message.ToString());
            return;
        }
    }

    protected void drpFilter_SelectedIndexChanged(object sender, EventArgs e)
    {
        rgHead.Rebind();
    }

    protected void rgHead_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        try
        {
            string companyId    = (string)Session["CompanyId"] ?? "";
            bool   onlyProblems = drpFilter.SelectedValue == "PROBLEM";
            Delivery dy = new Delivery();
            rgHead.DataSource = dy.GetDeliveryErrors(companyId, onlyProblems);
        }
        catch (Exception ex)
        {
            try
            {
                System.IO.File.AppendAllText(
                    Server.MapPath("~/App_Data/delivery_errors_log.txt"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | " + ex.GetType().Name + ": " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + "---" + Environment.NewLine);
            }
            catch { }
            try { ShowMasterPageMessage("Error", "Failed to load Delivery Errors", ex.Message); }
            catch { }
            rgHead.DataSource = new System.Data.DataTable();
        }
    }

    protected void rtbItem_DataSourceSelect(object sender, SearchBoxDataSourceSelectEventArgs e)
    {
        SqlDataSource ds = (SqlDataSource)e.DataSource;
        RadSearchBox searchBox = (RadSearchBox)sender;
        string query = searchBox.Text;
        //string sq3 = "SELECT TOP 1000 ItemCode FROM " + (string)this.Session["CompanyId"] + ".dbo.OITM " + Queries.WITH_NOLOCK + @"  WHERE ItemCode LIKE '%" + searchBox.Text + "%' UNION SELECT TOP 1000 ItemCode FROM " + (string)this.Session["CompanyId"] + ".dbo.OBCD " + Queries.WITH_NOLOCK + @"  WHERE BcdCode LIKE '%" + searchBox.Text + "%'";
        //string sq3 = "SELECT TOP 1000 ItemCode, ItemCode + ' - ' + ItemName ItemName FROM " + (string)this.Session["CompanyId"] + ".dbo.OITM " + Queries.WITH_NOLOCK + @"  WHERE ItemCode LIKE '%" + searchBox.Text + "%' UNION SELECT TOP 1000 a.ItemCode, a.ItemCode + ' - ' + b.ItemName ItemName FROM " + (string)this.Session["CompanyId"] + ".dbo.OBCD a " + Queries.WITH_NOLOCK + @"  INNER JOIN dfatocumen.dbo.OITM b " + Queries.WITH_NOLOCK + @"  ON a.ItemCode = b.ItemCode WHERE BcdCode LIKE '%" + searchBox.Text + "%'";
        string sq3 = "SELECT TOP 1000 ItemCode, ItemCode + ' - ' + ItemName ItemName FROM " + (string)this.Session["CompanyId"] + ".dbo.OITM " + Queries.WITH_NOLOCK + @"  WHERE ItemCode LIKE '%" + searchBox.Text + "%' UNION SELECT TOP 1000 a.ItemCode, a.ItemCode + ' - ' + b.ItemName ItemName FROM " + (string)this.Session["CompanyId"] + ".dbo.OBCD a " + Queries.WITH_NOLOCK + @"  INNER JOIN  " + (string)this.Session["CompanyId"] + ".dbo.OITM b " + Queries.WITH_NOLOCK + @"  ON a.ItemCode = b.ItemCode WHERE BcdCode LIKE '%" + searchBox.Text + "%' UNION SELECT '" +  searchBox.Text + "','Select here for free text, maximum 18 characters.'";
        ds.SelectCommand = sq3;
    }

    protected void rtbItem_Search(object sender, SearchBoxEventArgs e)
    {
        hfNewSku.Value = e.Value;
    }
}
