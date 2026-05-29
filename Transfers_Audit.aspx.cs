using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;



public partial class Transfers_Audit : BasePage
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

    protected string fileName;



    protected void Page_Load(object sender, EventArgs e)
    {
        if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

        if (string.IsNullOrEmpty((string)this.Session["CompanyId"]))
        {
            Response.Redirect("Login1.aspx");
        }

        sap_db = (string)this.Session["CompanyId"];
        CompanyIdLabel.Text = sap_db;

        ///////////////Begin New  Control de acceso por Roles

        lCurUser = (string)this.Session["UserId"];
        char flagokay = 'Y';
	    string lControlName = "Transfers_Audit.aspx";  //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

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
		    labelForm.InnerText = "Transfer Audit (Read-Only Access)";          
	    }
		
	    if (strAccessType == "F")
	    {
		    labelForm.InnerText = "Transfer Audit (Full Access)";        
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
                db.Connect();
                LoadWarehouses();
                LoadItemGroups();
                db.Disconnect();
            }
        }
    }

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        doQry.Text = "1";
        rgHead.Rebind();
    }

    protected void txtDocNum_PreRender(object sender, EventArgs e)
    {

    }

    protected void GridView1_DataBound(object sender, EventArgs e)
    {

    }

    private void LoadWarehouses()
    {
        DataTable dt = new DataTable();

        int branchId = 0;
        int.TryParse(Session["BranchId"] as string, out branchId);
        string branchFilter = branchId > 0 ? " AND O.BPLid IN (1, " + branchId + ")" : "";
        try
        {
            string sql =
            @"select O.WhsCode,
                     CONVERT(nvarchar(30), ISNULL(O.U_POSCode, '')) + ' - ' + O.WhsCode + ' - ' + O.WhsName AS WHS
                 from " + sap_db + @".dbo.owhs O, RSS_OWHS_CONTROL R
                 where O.WhsCode = R.WhsCode
                   and R.Control = 'VIEWTRA'
                   AND R.CompanyId = '" + sap_db + @"'" + branchFilter + @" ORDER BY CASE WHEN O.BPLId = 1 THEN 0 ELSE 1 END, O.U_POSCode";

            //where WhsCode in ('BODEGA', 'TIENDA')

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            db.Disconnect();
            throw new Exception("Caught exception in function LoadWarehouses. ERROR MESSAGE : " + ex.Message);
        }

        drpFromWhsCode.DataSource = dt;
        drpFromWhsCode.DataBind();

        //--------------

        DataTable dt2 = new DataTable();

        try
        {
            string sql2 =
            @"select O.WhsCode,
                     CONVERT(nvarchar(30), ISNULL(O.U_POSCode, '')) + ' - ' + O.WhsCode + ' - ' + O.WhsName AS WHS
                 from " + sap_db + @".dbo.owhs O, RSS_OWHS_CONTROL R
                 where O.WhsCode = R.WhsCode
                   and R.Control = 'VIEWTRA'
                   AND R.CompanyId = '" + sap_db + @"'" + branchFilter + @" ORDER BY CASE WHEN O.BPLId = 1 THEN 0 ELSE 1 END, O.U_POSCode";

            //where WhsCode in ('BODEGA', 'TIENDA')

            db.adapter = new SqlDataAdapter(sql2, db.Conn);
            db.adapter.Fill(dt2);
        }
        catch (Exception ex)
        {
            db.Disconnect();
            throw new Exception("Caught exception in function LoadWarehouses for toloc. ERROR MESSAGE : " + ex.Message);
        }


        drpToWhsCode.DataSource = dt2;
        drpToWhsCode.DataBind();

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
             from " + sap_db + @".dbo.oitb 
            order by ItmsGrpCod";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            db.Disconnect();
            throw new Exception("Caught exception in function LoadItemGroups. ERROR MESSAGE : " + ex.Message);
        }
        drpItemGroups.DataSource = dt;
        drpItemGroups.DataBind();

        ListItem li = new ListItem("Select a Group", "0");

        drpItemGroups.Items.Insert(0, li);

    }


    protected void ExportToExcel_Click(object sender, EventArgs e)
    {
        DataTable dt = new DataTable();
        fileName = "Transferencias.csv";
        string sFileName = fileName;
        Response.ContentType = "Application/csv";
        Response.AddHeader("Content-Disposition", "filename=" + sFileName + ";");

        try
        {
            Transfer trf = new Transfer();

            string lstatusDoc = StatusRadioButtonList.SelectedValue;
            string ltxtDocNum = txtDocNum.Text ;
            string lFromDateTxt = hfFromDate.Value;// FromDateTxt.Text;
            string ltoDateTxt = hfToDate.Value;// toDateTxt.Text;
            string lfromLocTxt = drpFromWhsCode.SelectedValue ;
            string ltoLocTxt = drpToWhsCode.SelectedValue ;
            string lcategoryTxt =  drpItemGroups.SelectedValue ;
            string landOr1 = andOrDropDownList1.SelectedValue ;
            string landOr2 = andOrDropDownList2.SelectedValue ;
            string landOr3 = andOrDropDownList3.SelectedValue ;
            string ldoQry = "1";

            int branchIdExp = 0;
            int.TryParse(Session["BranchId"] as string, out branchIdExp);
            dt = trf.GetTransferAudit( lstatusDoc,  ltxtDocNum,
                                        lFromDateTxt,  ltoDateTxt,
                                        lfromLocTxt,  ltoLocTxt,  lcategoryTxt,
                                        landOr1,  landOr2,  landOr3,  ldoQry, CompanyIdLabel.Text, branchIdExp);


            // output header row
            string hdr = "";
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                hdr += dt.Columns[i].ColumnName.ToString() + ",";
            }
            hdr += "New Min,New Max,";
            Response.Write(hdr.Substring(0, hdr.Length - 1) + '\r' + '\n');
            //output all data rows
            foreach (DataRow r in dt.Rows)
            {
                Response.Write(FormatCsvString(r));
            }
        }
        catch (Exception ex)
        {
            Response.Write("Caught exception in function ShowWorksheet.<br>ERROR MESSAGE: " + ex.Message);
        }
        Response.End();
    }

    protected string FormatCsvString(DataRow r)
    {
        string s = "";
        for (int i = 0; i < r.ItemArray.Length; i++)
        {
            s += "\"" + r[i].ToString().Replace("\"", "\"\"") + "\",";
        }
        return s.Substring(0, s.Length - 1) + '\r' + '\n';
    }
    protected void FromDateTxt_SelectedDateChanged(object sender, Telerik.Web.UI.Calendar.SelectedDateChangedEventArgs e)
    {
        hfFromDate.Value = FromDateTxt.SelectedDate.Value.ToShortDateString().ToString();
    }

    protected void toDateTxt_SelectedDateChanged(object sender, Telerik.Web.UI.Calendar.SelectedDateChangedEventArgs e)
    {
        hfToDate.Value = toDateTxt.SelectedDate.Value.ToShortDateString().ToString();
    }
    protected void rgHead_NeedDataSource(object sender, Telerik.Web.UI.GridNeedDataSourceEventArgs e)
    {
        int branchId = 0;
        int.TryParse(Session["BranchId"] as string, out branchId);
        Transfer ts = new Transfer();
        DataTable dt = ts.GetTransferAudit(StatusRadioButtonList.SelectedValue, txtDocNum.Text, hfFromDate.Value, hfToDate.Value, drpFromWhsCode.SelectedValue, drpToWhsCode.SelectedValue, drpItemGroups.SelectedValue, andOrDropDownList1.SelectedValue, andOrDropDownList2.SelectedValue, andOrDropDownList3.SelectedValue, doQry.Text, CompanyIdLabel.Text, branchId);
        rgHead.DataSource = dt;
    }
}


