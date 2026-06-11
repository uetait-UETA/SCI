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
            string v_Message = string.Empty;
            GridEditableItem item = e.Item as GridEditableItem;
            Hashtable values = new Hashtable();
            item.ExtractValues(values);

            string v_SalesId = item.GetDataKeyValue("sales_id").ToString();

            Delivery dy = new Delivery();
            dy.UpdateDeliveryItemNumber(v_SalesId, hfNewSku.Value, (string)Session["BranchId"]);
            }


        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Faled to update data", ex.Message.ToString());
            return;
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
