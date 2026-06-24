using System;
using System.Data;
using System.Net;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using Telerik.Web.UI;

public partial class ComprasDirectas : BasePage
{
    private readonly GoodsReceipt _gr = new GoodsReceipt();
    private bool _allowReceive = false;

    protected void Page_Load(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty((string)Session["UserId"]) ||
            string.IsNullOrEmpty((string)Session["CompanyId"]))
        {
            Response.Redirect("Login1.aspx");
            return;
        }

        string userId = (string)Session["UserId"];
        string accessType = "N", roleDesc = "";
        SqlDb db = new SqlDb();
        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(userId, "ComprasDirectas.aspx", ref accessType, ref roleDesc);
        db.Disconnect();

        if (accessType == "N")
        {
            string msg = "User " + userId + " with role " + roleDesc +
                         " does not have permission to access this screen.";
            ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "noAccess",
                "alert('" + msg.Replace("'", "\\'") + "'); window.location='Default.aspx';", true);
            return;
        }

        _allowReceive = (accessType == "F");
        labelForm.InnerText = _allowReceive
            ? "Direct Purchase Receiving (Full Access)"
            : "Direct Purchase Receiving (Read-Only)";

        if (!IsPostBack)
        {
            rdpFromDate.SelectedDate = DateTime.Today.AddMonths(-3);
            rdpToDate.SelectedDate   = DateTime.Today;
            LoadToLocations();
        }
    }

    private void LoadToLocations()
    {
        string sapDb = (string)Session["CompanyId"];
        int    bplId = BranchId;
        SqlDb  db    = new SqlDb();
        db.Connect();
        try
        {
            string smmDb = System.Configuration.ConfigurationManager.AppSettings["smm_db"] ?? "SMM_PROD";
            string sql = string.Format(@"
                SELECT O.WhsCode,
                       CONVERT(nvarchar(30), ISNULL(O.U_POSCode,'')) + ' - ' + O.WhsCode + ' - ' + O.WhsName AS WhsDisplay
                FROM   {0}.dbo.OWHS O {1}
                JOIN   {2}.dbo.RSS_OWHS_CONTROL R {1}
                       ON  R.WhsCode   = O.WhsCode
                       AND R.Control   = 'VIEWTRA'
                       AND R.CompanyId = '{0}'
                WHERE  O.BPLId = {3}
                ORDER  BY O.U_POSCode",
                sapDb, Queries.WITH_NOLOCK, smmDb, bplId);

            db.cmd.CommandText = sql;
            db.cmd.CommandType = System.Data.CommandType.Text;
            System.Data.DataTable dt = new System.Data.DataTable();
            dt.Load(db.cmd.ExecuteReader());

            rcbToLocation.Items.Clear();
            rcbToLocation.Items.Add(new Telerik.Web.UI.RadComboBoxItem("Select a location", ""));
            foreach (System.Data.DataRow row in dt.Rows)
                rcbToLocation.Items.Add(
                    new Telerik.Web.UI.RadComboBoxItem(
                        row["WhsDisplay"].ToString(), row["WhsCode"].ToString()));
        }
        finally { db.Disconnect(); }
    }

    protected void rbtnSearch_Click(object sender, EventArgs e)
    {
        rgInvoices.Rebind();
    }

    protected void rgInvoices_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        string sapDb = (string)Session["CompanyId"];
        int    bplId = BranchId;

        int    docNum    = 0;
        int.TryParse(txtDocNum.Text.Trim(), out docNum);
        string toWhsCode = rcbToLocation.SelectedValue;

        DataTable dt = _gr.GetPendingDirectPurchaseInvoices(
            sapDb, bplId, rdpFromDate.SelectedDate, rdpToDate.SelectedDate,
            docNum, toWhsCode);

        if (_gr.LastError != null)
            ShowMessage("Error", "Query Error", _gr.LastError);

        rgInvoices.DataSource = dt;

        // Disable Receive button in read-only mode
        if (!_allowReceive)
        {
            foreach (GridDataItem item in rgInvoices.Items)
            {
                Button btn = item.FindControl("btnReceive") as Button;
                if (btn != null) btn.Enabled = false;
            }
        }
    }

    protected void rgInvoices_ItemCommand(object sender, GridCommandEventArgs e)
    {
        if (e.CommandName != "Receive") return;

        if (!_allowReceive)
        {
            ShowMessage("Warning", "Access Denied",
                "You do not have permission to receive AP Reserve Invoices.");
            return;
        }

        var    keys       = e.Item.OwnerTableView.DataKeyValues[e.Item.ItemIndex];
        int    docEntry   = Convert.ToInt32(keys["DocEntry"]);
        string cardCode   = keys["CardCode"].ToString();
        int    opchDocNum = Convert.ToInt32(keys["DocNum"]);
        string sapDb      = (string)Session["CompanyId"];
        int    bplId      = BranchId;
        string userId     = (string)Session["UserId"];

        // Duty Paid items present → detail page for quantity entry
        if (_gr.HasDutyPaidLines(sapDb, docEntry))
        {
            Response.Redirect("ComprasDirectasDetail.aspx?docEntry=" + docEntry, false);
            Context.ApplicationInstance.CompleteRequest();
            return;
        }

        // All Duty Free → receive with full OPCH quantities
        try
        {
            DataTable dtLines = _gr.GetApReserveInvoiceLines(sapDb, docEntry);
            if (_gr.LastError != null || dtLines.Rows.Count == 0)
            {
                ShowMessage("Error", "No Lines Found",
                    _gr.LastError ?? "No lines found for this AP Reserve Invoice.");
                rgInvoices.Rebind();
                return;
            }

            string payload = _gr.BuildGrpoFromOpch(cardCode, bplId,
                docEntry, opchDocNum, dtLines);

            var sl = new SapServiceLayer();
            try
            {
                sl.Login(sapDb);
                string response = sl.CreateGoodsReceiptPO(payload);

                int grpoEntry = 0, grpoDocNum = 0;
                try
                {
                    var resp = JObject.Parse(response);
                    if (resp["DocEntry"] != null) grpoEntry  = Convert.ToInt32(resp["DocEntry"]);
                    if (resp["DocNum"]   != null) grpoDocNum = Convert.ToInt32(resp["DocNum"]);
                }
                catch { }

                _gr.LogReceipt(sapDb, docEntry, opchDocNum,
                    grpoEntry, grpoDocNum, cardCode, "", userId, "SUCCESS", "");

                ShowMessage("Success", "Goods Receipt Created",
                    "Goods Receipt PO #" + grpoDocNum + " created in SAP B1.");
            }
            catch (WebException wex)
            {
                string errMsg = SapServiceLayer.GetSlErrorMessage(wex);
                _gr.LogReceipt(sapDb, docEntry, opchDocNum,
                    0, 0, cardCode, "", userId, "ERROR", errMsg);
                ShowMessage("Error", "SAP Error", errMsg);
            }
            finally
            {
                sl.Logout();
            }
        }
        catch (Exception ex)
        {
            ShowMessage("Error", "Unexpected Error", ex.Message);
        }

        rgInvoices.Rebind();
    }

    private void ShowMessage(string msgType, string title, string message)
    {
        try
        {
            var sm = this.Master as SiteMaster;
            if (sm != null) sm.ShowDivMessage(msgType, title, message);
        }
        catch { }
    }
}
