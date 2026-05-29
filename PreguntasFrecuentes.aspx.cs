using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Web.UI;
//using System.Web.UI.WebControls;
using System.Xml;
using Telerik;
using Telerik.Web.UI;
using System.Data.SqlClient;
using System.IO;
using System.Drawing;

public partial class PreguntasFrecuentes : BasePage
{
    public DataTable dt = new DataTable();
    public DataManager dm = new DataManager();

    protected void Page_Load(object sender, EventArgs e)
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

    protected void rgHead_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        try
        {
            DataTable dtFAQ = new DataTable();
            dtFAQ = dm.GetFAQsByCase(rtbCasos.Text);

            rgHead.DataSource = dtFAQ;
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
            return;
        }
    }

    protected void rbtnSearch_Click(object sender, EventArgs e)
    {
        rgHead.Rebind();
    }

    protected void rgHead_ItemDataBound(object sender, GridItemEventArgs e)
    {
        try
        {
            if (e.Item is GridDataItem)
            {
                GridDataItem item = (GridDataItem)e.Item;
                RadBinaryImage rb = item.FindControl("Image1") as RadBinaryImage;
                
                //string hjk = rb.AlternateText;
                if (rb.AlternateText == "")
                {
                    rb.Visible = false;
                }
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
            return;
        }
    }
}