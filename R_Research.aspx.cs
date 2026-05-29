using System;
using System.Web.UI;
using System.Data;
using System.Configuration;

public partial class R_Research : BasePage
{
    protected SqlDb db = new SqlDb();

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
    protected string strAccessType = "";



    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["UserId"] == null || (string)Session["UserId"] == "" || Session["CompanyId"] == null || (string)Session["CompanyId"] == "")
        {
            Response.Redirect("Login1.aspx");
        }


        try
        {
            sap_db = (string)Session["CompanyId"];
            CompanyLabel.Text = sap_db;

            ///////////////Begin New  Control de acceso por Roles
            lCurUser = (string)Session["UserId"];
            char flagokay = 'Y';
            string lControlName = "Transfers.aspx";
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
            ///////////////End  New Control de acceso por Roles

            if (flagokay == 'Y')
            {
                //ObjectDataSource1.SelectParameters["ShowAll"].DefaultValue = radioShowAll.Checked.ToString(); Mod Nov2013
                //sap_db = ConfigurationSettings.AppSettings.Get("sap_db");            
                serverIP = ConfigurationManager.AppSettings.Get("serverIP");
                serverUserName = ConfigurationManager.AppSettings.Get("serverUserName");
                serverPwd = ConfigurationManager.AppSettings.Get("serverPwd");
                dbUserName = ConfigurationManager.AppSettings.Get("dbUserName");
                dbPwd = ConfigurationManager.AppSettings.Get("dbPwd");
                licenseServerIP = ConfigurationManager.AppSettings.Get("licenseServerIP");

                if (!IsPostBack)
                {
                    Session["RptData"] = null;
                    Session["Item"] = "-";
                    Session["CiaLabel"] = "-";
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in Page_Load. ERROR MESSAGE: " + ex.Message);
        }
        finally
        {
            db.Disconnect();
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
            if (Session[keyName] == null || (string)Session[keyName] == "")
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
    protected void btnSearch_Click(object sender, EventArgs e)
    {
        try
        {
            rgHead.Rebind();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in GridView1.DataBind(). ERROR MESSAGE : " + ex.Message);
        }
    }

    protected void rgHead_NeedDataSource(object sender, Telerik.Web.UI.GridNeedDataSourceEventArgs e)
    {
        ValidaSesionNullOrEmpty(new string[] { "UserId", "CompanyId" });

        if (Session["CiaLabel"].ToString() != CompanyLabel.Text 
            || Session["Item"].ToString() != rtbItem.Text)
        {
            DataManager dm = new DataManager();
            DataTable dt = dm.GetResearchData(CompanyLabel.Text, rtbItem.Text);
            Session["RptData"] = dt;
            Session["CiaLabel"] = CompanyLabel.Text;
            Session["Item"] = rtbItem.Text;
        }

        if((DataTable)Session["RptData"] != null)
        {
            rgHead.DataSource = (DataTable)Session["RptData"];
        }
    }

    protected void rgHead_ItemCommand(object sender, Telerik.Web.UI.GridCommandEventArgs e)
    {
        switch (e.CommandName)
        {
            case "RebindGrid": /*Refresh*/
                Session["RptData"] = null;
                Session["Item"] = "-";
                Session["CiaLabel"] = "-";
                rtbItem.Text = "";
                break;
            default:
                break;
        }
    }
}
