using System;
using System.Configuration;
using System.Data;
using System.Collections;
using System.Data.SqlClient;

public partial class Login1 : BasePage
{
    protected override bool RequiresLogin { get { return false; } }

    protected SqlDb db = new SqlDb();

    protected void Page_Load(object sender, EventArgs e)
    {
        companyDDL.Attributes.Add("translate", "no");
        if (!IsPostBack)
        {
            if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
            {
                string Lmsg = "Please log into the system.";
                divMessage.InnerHtml = Lmsg;

                LogInBtn1.Visible  = true;
                LogOutBtn1.Visible = false;
                Label1.Visible     = true;
                lblCompany.Visible = true;
                UserField1.Visible = true;
                Label2.Visible     = true;
                Passwd1.Visible    = true;
                companyDDL.Visible = true;
                liBranch.Visible   = false;   // Branch is auto-set from SMM_COMPANIES.Branch
                UserField1.Focus();
            }
            else
            {
                ClearSession();
                Response.Redirect("Login1.aspx");
            }
        }
    }

    protected void LogOutBtn1_Click(object sender, EventArgs e)
    {
        ClearSession();
        Response.Redirect("Login1.aspx");
    }

    protected void LogInBtn1_Click(object sender, EventArgs e)
    {
        Session["FlagNoPerPag"] = "Y";

        int selectedCompanyId;
        if (!int.TryParse(companyDDL.SelectedValue, out selectedCompanyId) || selectedCompanyId == 0)
        {
            divMessage.InnerText = "Select the Company";
            return;
        }

        if (Passwd1.Text == "")
        {
            divMessage.InnerText = "Enter your Password";
            return;
        }

        if (UserField1.Text == "")
        {
            divMessage.InnerText = "Enter your Username";
            return;
        }

        db.Connect();

        // ── Load company row (Companycode + Branch) from the numeric CompanyId ──
        SqlCommand cmdCompany = new SqlCommand(
            @"SELECT Companycode, CompanyName, ISNULL(tienda_db, ''), ISNULL(Branch, 0)
              FROM dbo.SMM_COMPANIES
              WHERE CompanyId = @numId",
            db.Conn);
        cmdCompany.Parameters.AddWithValue("@numId", selectedCompanyId);

        string companyCode = "";
        string companyName = "";
        string tiendaDb    = "";
        int    branchId    = 0;

        using (SqlDataReader rdr = cmdCompany.ExecuteReader())
        {
            if (!rdr.Read())
            {
                divMessage.InnerText = "Company not found.";
                return;
            }
            companyCode = rdr[0].ToString();
            companyName = rdr[1].ToString();
            tiendaDb    = rdr[2].ToString();
            branchId    = Convert.ToInt32(rdr[3]);
        }

        // Session["CompanyId"] stores Companycode (the SAP B1 DB name used in queries)
        Session["CompanyId"]   = companyCode;
        Session["CompanyName"] = companyName;
        Session["smm_db"]      = ConfigurationManager.AppSettings["smm_db"];
        Session["tienda_db"]   = tiendaDb != ""
            ? tiendaDb
            : ConfigurationManager.AppSettings["tienda_db"];

        // ── Auto-set branch from SMM_COMPANIES.Branch (BPLId) ──
        if (branchId > 0)
        {
            Session["BranchId"] = branchId.ToString();

            SqlCommand cmdBranch = new SqlCommand(
                @"SELECT BranchName, ISNULL(IsSellerBranch, 0)
                  FROM dbo.SMM_BRANCHES
                  WHERE CompanyId = @cid AND BranchId = @bid",
                db.Conn);
            cmdBranch.Parameters.AddWithValue("@cid", companyCode);
            cmdBranch.Parameters.AddWithValue("@bid", branchId);

            using (SqlDataReader rdrB = cmdBranch.ExecuteReader())
            {
                if (rdrB.Read())
                {
                    Session["BranchName"]     = rdrB[0].ToString();
                    Session["IsSellerBranch"] = rdrB[1].ToString() == "1" ? "Y" : "N";
                }
                else
                {
                    Session["BranchName"]     = "";
                    Session["IsSellerBranch"] = "N";
                }
            }
        }
        else
        {
            Session["BranchId"]       = "0";
            Session["BranchName"]     = "";
            Session["IsSellerBranch"] = "N";
        }

        Label3.Text = "Company: " + companyCode;

        // ── Validate credentials ──
        AccessRepository accessRepo = new AccessRepository(db);
        string sTextOut;
        DataTable storeDataTable = accessRepo.ValidateLogin(
            UserField1.Text, Passwd1.Text, companyCode, out sTextOut);

        if (sTextOut != "Login Okay.")
        {
            divMessage.InnerHtml = sTextOut;
            return;
        }

        Label3.Text = "Welcome, " + UserField1.Text + " to " + companyCode;

        ArrayList controles   = new ArrayList();
        ArrayList roles       = new ArrayList();
        ArrayList permissions = new ArrayList();

        foreach (DataRow storeDataRow in storeDataTable.Rows)
        {
            if (Convert.ToString(storeDataRow["ErrorId"]) == "1")
            {
                Session["UserId"] = UserField1.Text;
                if (string.IsNullOrEmpty((string)this.Session["UserId"]))
                {
                    Label3.Text = "Enter your Username.";
                    return;
                }
                LogInBtn1.Visible = false;
            }
            if (Convert.ToString(storeDataRow["ErrorId"]) == "2")
                controles.Add(Convert.ToString(storeDataRow["ErrorMsg"]));
            if (Convert.ToString(storeDataRow["ErrorId"]) == "3")
                roles.Add(Convert.ToString(storeDataRow["ErrorMsg"]));
            if (Convert.ToString(storeDataRow["ErrorId"]) == "4")
                permissions.Add(Convert.ToString(storeDataRow["ErrorMsg"]));
        }

        LogInBtn1.Visible  = false;
        Label1.Visible     = false;
        lblCompany.Visible = false;
        UserField1.Visible = false;
        Label2.Visible     = false;
        Passwd1.Visible    = false;
        companyDDL.Visible = false;
        liBranch.Visible   = false;
        LogOutBtn1.Enabled = true;
        LogOutBtn1.Visible = true;

        Session["Controles"]   = controles;
        Session["Roles"]       = roles;
        Session["Permissions"] = permissions;

        Response.Redirect("Default.aspx");
    }

    private void ClearSession()
    {
        Session["FlagNoPerPag"]   = "Y";
        Session["UserId"]         = "";
        Session["CompanyId"]      = "";
        Session["CompanyName"]    = "";
        Session["smm_db"]         = "";
        Session["tienda_db"]      = "";
        Session["BranchId"]       = "";
        Session["BranchName"]     = "";
        Session["IsSellerBranch"] = "";
    }

    protected void LogOutBtn1_PreRender(object sender, EventArgs e)
    {
        Session["FlagNoPerPag"] = "Y";
    }

    protected void LogInBtn1_PreRender(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty((string)this.Session["UserId"]))
        {
            Label1.Visible     = true;
            lblCompany.Visible = true;
            UserField1.Visible = true;
            Label2.Visible     = true;
            Passwd1.Visible    = true;
            LogInBtn1.Visible  = true;
            companyDDL.Visible = true;
        }
    }
}
