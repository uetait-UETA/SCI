using System;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Net;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using Telerik.Web.UI;

public partial class GoodsReceiptPO : BasePage
{
    private readonly GoodsReceipt _gr = new GoodsReceipt();
    private bool _allowReceive = false;

    protected void Page_Load(object sender, EventArgs e)
    {
        ValidateSession(new[] { "UserId", "CompanyId" });

        string userId = Session["UserId"].ToString();

        SqlDb db = new SqlDb();
        string accessType = "N", roleDesc = "";
        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(userId, "GoodsReceiptPO.aspx", ref accessType, ref roleDesc);
        db.Disconnect();

        if (accessType == "N" && roleDesc.ToUpper() != "NO DEFINIDO")
        {
            string msg = string.Format(
                "User {0} with role {1} does not have permission to access this screen.",
                userId, roleDesc);
            ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "noAccess",
                "alert('" + msg.Replace("'", "\\'") + "'); window.location='Default.aspx';", true);
            return;
        }

        _allowReceive = (accessType == "F" || roleDesc.ToUpper() == "NO DEFINIDO");
        labelForm.InnerText = _allowReceive
            ? "Inventory Transfer Receive (Full Access)"
            : "Inventory Transfer Receive (Read-Only)";

        if (!IsPostBack)
        {
            string sapDb = Session["CompanyId"].ToString();
            LoadWarehouses(sapDb);
            rdpFromDate.SelectedDate = DateTime.Today.AddMonths(-3);
            rdpToDate.SelectedDate   = DateTime.Today;
        }
    }

    // ── Warehouse combo-box ──────────────────────────────────────────────────

    private void LoadWarehouses(string sapDb)
    {
        DataTable dt = _gr.GetWarehouses(sapDb);
        if (_gr.LastError != null)
        {
            ShowMessage("Error", "Warehouse Load Error", _gr.LastError);
            return;
        }
        rcbWarehouse.DataSource     = dt;
        rcbWarehouse.DataTextField  = "WhsDisplay";
        rcbWarehouse.DataValueField = "WhsCode";
        rcbWarehouse.DataBind();
    }

    // ── Auto-load grid when warehouse is selected ────────────────────────────

    protected void rcbWarehouse_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        rgInvoices.Rebind();
    }

    // ── Search button (date filter refresh) ─────────────────────────────────

    protected void rbtnSearch_Click(object sender, EventArgs e)
    {
        rgInvoices.Rebind();
    }

    // ── Grid data source ─────────────────────────────────────────────────────

    protected void rgInvoices_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        string whsCode = rcbWarehouse.SelectedValue;
        string sapDb   = Session["CompanyId"].ToString();

        if (string.IsNullOrEmpty(whsCode))
        {
            // Return schema-correct empty table so RadGrid finds DataKeyNames
            rgInvoices.DataSource = _gr.GetPendingTransferRequests(sapDb, "<<none>>");
            return;
        }

        DataTable dt = _gr.GetPendingTransferRequests(sapDb, whsCode);
        if (_gr.LastError != null)
            ShowMessage("Error", "Query Error", _gr.LastError);
        rgInvoices.DataSource = dt;
    }

    // ── Grid item data-bound ─────────────────────────────────────────────────

    protected void rgInvoices_ItemDataBound(object sender, GridItemEventArgs e)
    {
        if (!(e.Item is GridDataItem)) return;
        Button btn = e.Item.FindControl("btnReceive") as Button;
        if (btn != null) btn.Enabled = _allowReceive;
    }

    // ── Receive action ───────────────────────────────────────────────────────

    protected void rgInvoices_ItemCommand(object sender, GridCommandEventArgs e)
    {
        if (e.CommandName != "Receive") return;

        if (!_allowReceive)
        {
            ShowMessage("Warning", "Access Denied",
                "You do not have permission to receive transfers.");
            return;
        }

        try
        {
            var  keys          = e.Item.OwnerTableView.DataKeyValues[e.Item.ItemIndex];
            int  localDocEntry = Convert.ToInt32(keys["LocalDocEntry"]);
            int  sapTrReqEntry = Convert.ToInt32(keys["SapTrReqEntry"]);
            string userId      = Session["UserId"].ToString();
            string sapDb       = Session["CompanyId"].ToString();
            string companyDb   = ConfigurationManager.AppSettings["SL_CompanyDB"] ?? sapDb;

            // 1. Get dispatch lines (DispatchQuantity)
            DataTable dtLines = _gr.GetTransferRequestLines(sapDb, localDocEntry);
            if (dtLines.Rows.Count == 0)
            {
                ShowMessage("Error", "No Lines Found",
                    "No dispatched lines found for this transfer.");
                return;
            }

            string fromWhs = dtLines.Rows[0]["FromWhsCode"].ToString();
            string toWhs   = dtLines.Rows[0]["ToWhsCode"].ToString();

            // 2. Build StockTransfer lines referencing the Transfer Request
            var lines = new JArray();
            for (int i = 0; i < dtLines.Rows.Count; i++)
            {
                DataRow row = dtLines.Rows[i];
                lines.Add(new JObject(
                    new JProperty("ItemCode",          row["ItemCode"].ToString()),
                    new JProperty("Quantity",          Convert.ToInt32(row["Qty"])),
                    new JProperty("FromWarehouseCode", fromWhs),
                    new JProperty("WarehouseCode",     row["ToWhsCode"].ToString()),
                    new JProperty("BaseType",          1250000001),
                    new JProperty("BaseEntry",         sapTrReqEntry),
                    new JProperty("BaseLine",          i)
                ));
            }

            // 3. Build payload
            var payload = new JObject(
                new JProperty("FromWarehouse",      fromWhs),
                new JProperty("ToWarehouse",        toWhs),
                new JProperty("U_BOL",              localDocEntry.ToString()),
                new JProperty("U_RECEIVE",          userId),
                new JProperty("U_ORITOWHS",         fromWhs),
                new JProperty("StockTransferLines", lines)
            );

            // 4. Create Stock Transfer in SAP B1
            var sl = new SapServiceLayer();
            try
            {
                sl.Login(companyDb);
                string response = sl.CreateInventoryTransfer(
                    payload.ToString(Newtonsoft.Json.Formatting.None));

                int sapStEntry  = 0, sapStDocNum = 0;
                try
                {
                    var resp = JObject.Parse(response);
                    sapStEntry  = resp["DocEntry"] != null ? Convert.ToInt32(resp["DocEntry"]) : 0;
                    sapStDocNum = resp["DocNum"]   != null ? Convert.ToInt32(resp["DocNum"])   : 0;
                }
                catch { }

                // 5. Update local record: Received='Y', store ST DocEntry/DocNum
                _gr.MarkTransferReceived(sapDb, localDocEntry, sapStEntry, sapStDocNum, userId);

                ShowMessage("Success", "Transfer Received",
                    string.Format("Inventory Transfer #{0} created in SAP.", sapStDocNum));
            }
            catch (WebException wex)
            {
                ShowMessage("Error", "SAP Error",
                    SapServiceLayer.GetSlErrorMessage(wex));
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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void ValidateSession(string[] keys)
    {
        foreach (string key in keys)
        {
            if (Session[key] == null || Session[key].ToString() == "")
            {
                Response.Redirect("Login1.aspx");
                return;
            }
        }
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
