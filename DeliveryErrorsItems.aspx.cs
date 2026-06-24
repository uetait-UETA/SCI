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
        Response.Redirect("DeliveryErrors.aspx");
        return;

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
            string v_Message = string.Empty;
            GridEditableItem item = e.Item as GridEditableItem;
            Hashtable values = new Hashtable();
            item.ExtractValues(values);

            string v_ID = item.GetDataKeyValue("skunum").ToString();
            //string v_NewItem = values["new_sku"].ToString();
            //string v_wh = (Label)item["whs_code"].FindControl("whs_code");
            //v_wh = (String)values["whs_code"]; 
            string v_wh = ((TextBox)item.FindControl("STORENUM")).Text.Trim();
            //Telerik.Web.UI.GridDataItem item2 = (Telerik.Web.UI.GridDataItem)e.Item;
            //string v_wh = item2["sknum"].Text;
            //v_wh = "ACC209";

            //ShowMasterPageMessage("Error", v_wh, "3444");

            Delivery dy = new Delivery();
            //dy.UpdateDeliveryItemNumber(v_ID, v_NewItem);
            dy.UpdateDeliveryItemCode(v_ID, hfNewSku.Value, v_wh, CompanyIdLabel.Text);
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Faled to update data ", ex.Message.ToString());
            //ShowMasterPageMessage("Error", vwh, ex.Message.ToString());
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
        string sq3 = "SELECT TOP 1000 ItemCode, ItemCode + ' - ' + ItemName ItemName FROM " + (string)this.Session["CompanyId"] + ".dbo.OITM " + Queries.WITH_NOLOCK + @"  WHERE ItemCode LIKE '%" + searchBox.Text + "%' UNION SELECT TOP 1000 a.ItemCode, a.ItemCode + ' - ' + b.ItemName ItemName FROM " + (string)this.Session["CompanyId"] + ".dbo.OBCD a " + Queries.WITH_NOLOCK + @"  INNER JOIN  " + (string)this.Session["CompanyId"] + ".dbo.OITM b " + Queries.WITH_NOLOCK + @"  ON a.ItemCode = b.ItemCode WHERE BcdCode LIKE '%" + searchBox.Text + "%' UNION SELECT '" +  searchBox.Text + "','Select here for free text, maximum 18 characters.'";
        ds.SelectCommand = sq3;
    }

    protected void rtbItem_Search(object sender, SearchBoxEventArgs e)
    {
        hfNewSku.Value = e.Value;
    }
}
