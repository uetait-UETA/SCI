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
//using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

public partial class DeliveryErrorsItems : BasePage
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
	    string lControlName = "DeliveryErrorsItems.aspx";
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
		    		    
		    labelForm.InnerText = "Delivery Errors by Items (Read-Only Access)"; //////////<<<<<<<<
		            
		            
		}
		
	    if (strAccessType == "F")
		{
	
		    //btnCreateTransfer.Enabled = true;
		    
		    labelForm.InnerText = "Delivery Errors by Items (Full Access)"; //////////<<<<<<<<
		            
		            
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
    protected void drpFilter_SelectedIndexChanged(object sender, EventArgs e)
    {
        rgHead.CurrentPageIndex = 0;
        rgHead.Rebind();
    }

    protected void btnItemFilter_Click(object sender, EventArgs e)
    {
        rgHead.CurrentPageIndex = 0;
        rgHead.Rebind();
    }

    protected void btnClearItemFilter_Click(object sender, EventArgs e)
    {
        txtItemFilter.Text = "";
        rgHead.CurrentPageIndex = 0;
        rgHead.Rebind();
    }

    private void ShowMasterPageMessage(string v_Message_Type, string v_Message_Title, string v_Message)
    {
        try
        {
            SiteMaster sm = (SiteMaster)this.Master;
            sm.ShowDivMessage(v_Message_Type, v_Message_Title, v_Message);
        }
        catch
        {
            // this.Master as SiteMaster can return null on some pages (known, pre-existing
            // issue elsewhere in this app). Swallow rather than recurse into this same
            // method again — that would loop forever and crash the whole app pool.
        }
    }

    protected void rgHead_UpdateCommand(object sender, Telerik.Web.UI.GridCommandEventArgs e)
    {
        try
        {
            // Re-check access at save time — Page_Load only relabels the header for Read-Only,
            // it doesn't stop the save. Mirrors DeliveryErrors.aspx's own update handler.
            lCurUser = (string)this.Session["UserId"];
            string strAccessType = "";
            string strRole_Description = "";

            db.Connect();
            db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, "DeliveryErrorsItems.aspx", ref strAccessType, ref strRole_Description);
            db.Disconnect();

            if (strAccessType == "R")
            {
                string message = "User " + lCurUser + " does not have permissions to update in this screen.";
                string script = "{ alert('" + message + "'); window.location = 'DeliveryErrorsItems.aspx'; }";
                ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "alert", script, true);
                e.Canceled = true;
                return;
            }

            GridEditableItem item = e.Item as GridEditableItem;
            string v_ID = item.GetDataKeyValue("skunum").ToString();
            string v_wh = ((TextBox)item.FindControl("STORENUM")).Text.Trim();
            string newSku = hfNewSku.Value.Trim();

            if (string.IsNullOrEmpty(newSku))
            {
                ShowMasterPageMessage("Error", "No Item Selected", "Please select a new item before updating.");
                e.Canceled = true;
                return;
            }

            // Validate the new item code exists in SAP before saving.
            db.Connect();
            using (SqlCommand cmd = new SqlCommand(
                "SELECT COUNT(1) FROM " + CompanyIdLabel.Text + ".dbo.OITM WHERE ItemCode = @ItemCode", db.Conn))
            {
                cmd.Parameters.AddWithValue("@ItemCode", newSku);
                int cnt = (int)cmd.ExecuteScalar();
                db.Disconnect();
                if (cnt == 0)
                {
                    ShowMasterPageMessage("Error", "Invalid Item", "Item '" + newSku + "' does not exist.");
                    e.Canceled = true;
                    return;
                }
            }

            // The stored procedure's "@sap_db"/"@whs_code" names are misleading — it actually
            // filters la_delivery_errors/la_store_sales by BranchId and store number, not the
            // real SAP company DB name. Matches DeliveryErrors.aspx's own working call.
            Delivery dy = new Delivery();
            dy.UpdateDeliveryItemCode(v_ID, newSku, v_wh, (string)Session["BranchId"]);
            hfNewSku.Value = "";
            ShowMasterPageMessage("Success", "Updated", "Item '" + v_ID + "' was corrected to '" + newSku + "'.");
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to update data", ex.Message.ToString());
            e.Canceled = true;
            return;
        }
    }

    protected void ItemList_SelectedIndexChanged(object sender, EventArgs e)
    {
        //if (ItemList.SelectedValue != "-")
        //{
        //    rtbItem.Text = ItemList.SelectedValue;
        //    ItemList.Visible = false;
        //    rbtnCancel.Visible = false;
        //    rbtnSearch.Visible = true;
        //    GetData();
        //    BindGrid();
        //    rtbItem.Text = "";
        //    ItemList.Visible = true;
        //    rbtnCancel.Visible = false;
        //    rbtnSearch.Visible = true;
        //}
    }

    protected void rtbItem_DataSourceSelect(object sender, SearchBoxDataSourceSelectEventArgs e)
    {
        SqlDataSource ds = (SqlDataSource)e.DataSource;
        RadSearchBox searchBox = (RadSearchBox)sender;
        string query = searchBox.Text;
        //string sq3 = "SELECT TOP 1000 ItemCode FROM " + (string)this.Session["CompanyId"] + ".dbo.OITM " + Queries.WITH_NOLOCK + @"  WHERE ItemCode LIKE '%" + searchBox.Text + "%' UNION SELECT TOP 1000 ItemCode FROM " + (string)this.Session["CompanyId"] + ".dbo.OBCD " + Queries.WITH_NOLOCK + @"  WHERE BcdCode LIKE '%" + searchBox.Text + "%'";
        //string sq3 = "SELECT TOP 1000 ItemCode, ItemCode + ' - ' + ItemName ItemName FROM " + (string)this.Session["CompanyId"] + ".dbo.OITM " + Queries.WITH_NOLOCK + @"  WHERE ItemCode LIKE '%" + searchBox.Text + "%' UNION SELECT TOP 1000 a.ItemCode, a.ItemCode + ' - ' + b.ItemName ItemName FROM " + (string)this.Session["CompanyId"] + ".dbo.OBCD a " + Queries.WITH_NOLOCK + @"  INNER JOIN dfatocumen.dbo.OITM b " + Queries.WITH_NOLOCK + @"  ON a.ItemCode = b.ItemCode WHERE BcdCode LIKE '%" + searchBox.Text + "%'";
        string sq3 = "SELECT TOP 1000 ItemCode COLLATE DATABASE_DEFAULT AS ItemCode, (ItemCode + ' - ' + ItemName) COLLATE DATABASE_DEFAULT AS ItemName FROM " + (string)this.Session["CompanyId"] + ".dbo.OITM " + Queries.WITH_NOLOCK + @"  WHERE ItemCode LIKE '%" + searchBox.Text + "%' UNION SELECT TOP 1000 a.ItemCode COLLATE DATABASE_DEFAULT, (a.ItemCode + ' - ' + b.ItemName) COLLATE DATABASE_DEFAULT FROM " + (string)this.Session["CompanyId"] + ".dbo.OBCD a " + Queries.WITH_NOLOCK + @"  INNER JOIN  " + (string)this.Session["CompanyId"] + ".dbo.OITM b " + Queries.WITH_NOLOCK + @"  ON a.ItemCode = b.ItemCode WHERE BcdCode LIKE '%" + searchBox.Text + "%' UNION SELECT '" +  searchBox.Text + "','Select here for free text, maximum 18 characters.'";
        ds.SelectCommand = sq3;
    }

    protected void rtbItem_Search(object sender, SearchBoxEventArgs e)
    {
        hfNewSku.Value = e.Value;
    }
}
