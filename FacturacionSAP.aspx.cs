using System;
using System.Collections.Generic;
using System.Data;
using System.Web.UI;
using Telerik.Web.UI;

// Manual-review screen for Factura OINV / Nota de Crédito ORIN creation in SAP B1 from POS
// sales. Same layout and behaviour as ADA's FacturacionSAP.aspx, but the preview is calculated
// live by App_Code/InvoicePreview.cs (no la_invoice_preview staging table / scheduled dry-run in
// DFS), scoped to the user's branch (Session["BranchId"] = SAP BPL), and split by DutyType
// (la_store_sales.TRANSTYPE DF/DP) — each CardCode/DutyType/Day/Sale-or-Return is its own document.
public partial class FacturacionSAP : BasePage
{
    private readonly InvoicePreview _preview = new InvoicePreview();
    protected SqlDb db = new SqlDb();
    protected string lCurUser;
    protected string sap_db;
    protected int bplId;
    private bool _hasFullAccess;
    private bool _retryPaymentsEnabled;
    // appSetting FacturacionSAP_UploadMode: "Queue" = "Upload Selected" leaves the groups In Progress
    // for the night process (FacturacionSAPConsole --process-queue); anything else = upload immediately.
    private bool _queueMode;

    protected void Page_Load(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty((string)Session["UserId"]))
        {
            Response.Redirect("Login1.aspx");
            return;
        }

        lCurUser = (string)Session["UserId"];
        sap_db = (string)Session["CompanyId"];
        bplId = 0;
        int.TryParse((string)Session["BranchId"] ?? "0", out bplId);

        string strAccessType = "", strRoleDescription = "";

        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, "FacturacionSAP.aspx", ref strAccessType, ref strRoleDescription);
        db.Disconnect();

        if (strAccessType == "N")
        {
            string message = "User " + lCurUser + ", with Role " + strRoleDescription + " does not have permissions to access this screen.";
            string script = "{ alert('" + message + "'); window.location = 'Default.aspx'; }";
            ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "alert", script, true);
            return;
        }

        _hasFullAccess = strAccessType == "F";
        labelForm.InnerText = _hasFullAccess ? "SAP Invoicing (OINV)" : "SAP Invoicing (OINV) (Read-Only Access)";
        btnUpload.Visible = _hasFullAccess;

        // "Retry Payments" is switched on/off from Web.config (appSetting FacturacionSAP_RetryPayments).
        _retryPaymentsEnabled = _hasFullAccess && string.Equals(
            System.Configuration.ConfigurationManager.AppSettings["FacturacionSAP_RetryPayments"], "true",
            StringComparison.OrdinalIgnoreCase);
        btnRetryPayments.Visible = _retryPaymentsEnabled;

        _queueMode = string.Equals(
            System.Configuration.ConfigurationManager.AppSettings["FacturacionSAP_UploadMode"], "Queue",
            StringComparison.OrdinalIgnoreCase);
        btnUpload.OnClientClick = "return confirmUpload(this, " + (_queueMode ? "true" : "false") + ");";

        if (!IsPostBack)
        {
            rdpDesde.SelectedDate = DateTime.Today.AddDays(-1);
            rdpHasta.SelectedDate = DateTime.Today.AddDays(-1);
        }
    }

    private DateTime Desde { get { return (rdpDesde.SelectedDate ?? DateTime.Today.AddDays(-1)).Date; } }
    private DateTime Hasta { get { return (rdpHasta.SelectedDate ?? Desde).Date; } }

    protected void btnPreview_Click(object sender, EventArgs e)
    {
        lblResult.Text = "";
        // A new search invalidates whatever Detail was open for a previous row.
        pnlDetail.Visible = false;
        rgHead.Rebind();
        rgRequests.Rebind();
        ShowLastRun();
    }

    private void ShowLastRun()
    {
        lblLastRun.Text = "Calculated live: " + DateTime.Now.ToString("MM/dd/yyyy HH:mm");
    }

    // Calls SAP Service Layer directly and synchronously (via InvoiceUploader) for each
    // selected store/duty type/day, then logs the result into dbo.la_invoice_upload_requests
    // (see the "Upload Requests" grid below).
    protected void btnUpload_Click(object sender, EventArgs e)
    {
        if (!_hasFullAccess) return;
        pnlDetail.Visible = false;

        if (bplId == 0)
        {
            ShowMessage("Error", "No Branch", "BranchId is not set in the session.");
            return;
        }

        // One (CardCode, TransType, FechaDoc) upload covers both the Sale and the Return side;
        // checking any row of that group queues the whole group once.
        var grupos = new List<Tuple<string, string, DateTime>>();
        foreach (GridDataItem item in rgHead.Items)
        {
            var chk = item.FindControl("chkSelect") as System.Web.UI.WebControls.CheckBox;
            if (chk == null || !chk.Checked) continue;

            // item.DataItem is only populated during data binding, not on a plain postback —
            // read from DataKeyNames instead, which RadGrid persists across postbacks.
            bool blocked = Convert.ToBoolean(item.GetDataKeyValue("Blocked"));
            if (blocked) continue;
            // Already queued: in Queue mode there is nothing else to do until the night process.
            if (_queueMode && Convert.ToBoolean(item.GetDataKeyValue("InProgress"))) continue;

            string cardCode = item.GetDataKeyValue("CardCode").ToString();
            string transType = item.GetDataKeyValue("TransType").ToString();
            DateTime fecha = Convert.ToDateTime(item.GetDataKeyValue("FechaDoc")).Date;

            var grupo = Tuple.Create(cardCode, transType, fecha);
            if (!grupos.Contains(grupo))
                grupos.Add(grupo);
        }

        if (grupos.Count == 0)
        {
            ShowMessage("Warning", "Nothing Selected", "Check at least one non-blocked row before uploading.");
            return;
        }

        if (_queueMode)
        {
            // Only leaves the groups "In Progress"; FacturacionSAPConsole --process-queue uploads them
            // at night (same InvoiceUploader.Upload, which re-validates everything at that moment).
            int queued = 0;
            var errores = new List<string>();
            foreach (var g in grupos)
            {
                if (_preview.QueueUploadRequest(g.Item1, g.Item2, g.Item3, lCurUser))
                    queued++;
                else if (_preview.LastError != null)
                    errores.Add(g.Item1 + " [" + g.Item2 + "] " + g.Item3.ToString("MM/dd") + ": " + _preview.LastError);
            }

            lblResult.Text = queued + " store/day group(s) set to In Progress - they will be uploaded to SAP by the night process."
                + (errores.Count > 0 ? " Errors: " + string.Join(" | ", errores) : "");

            rgHead.Rebind();
            rgRequests.Rebind();
            ShowLastRun();
            return;
        }

        var uploader = new InvoiceUploader();
        int ok = 0, fail = 0;
        var summaries = new List<string>();

        foreach (var g in grupos)
        {
            InvoiceUploader.UploadResult result = uploader.Upload(sap_db, g.Item1, g.Item3, g.Item2, lCurUser);
            _preview.RecordUploadResult(g.Item1, g.Item2, g.Item3, lCurUser, result.Success, result.Summary);
            summaries.Add(g.Item1 + " [" + g.Item2 + "] " + g.Item3.ToString("MM/dd") + ": " + result.Summary);
            if (result.Success) ok++; else fail++;
        }

        lblResult.Text = ok + " store/day group(s) processed" + (fail > 0 ? ", " + fail + " blocked/failed" : "")
            + ". " + string.Join(" | ", summaries);

        rgHead.Rebind();
        rgRequests.Rebind();
        ShowLastRun();
    }

    // Creates the missing cobro / reembolso for checked rows whose OINV/ORIN is already in SAP
    // but whose payment failed ("payment pending").
    protected void btnRetryPayments_Click(object sender, EventArgs e)
    {
        if (!_retryPaymentsEnabled) return;
        pnlDetail.Visible = false;

        var grupos = new List<Tuple<string, string, DateTime>>();
        foreach (GridDataItem item in rgHead.Items)
        {
            var chk = item.FindControl("chkSelect") as System.Web.UI.WebControls.CheckBox;
            if (chk == null || !chk.Checked) continue;
            if (!Convert.ToBoolean(item.GetDataKeyValue("PaymentPending"))) continue;

            var grupo = Tuple.Create(item.GetDataKeyValue("CardCode").ToString(),
                item.GetDataKeyValue("TransType").ToString(),
                Convert.ToDateTime(item.GetDataKeyValue("FechaDoc")).Date);
            if (!grupos.Contains(grupo))
                grupos.Add(grupo);
        }

        if (grupos.Count == 0)
        {
            ShowMessage("Warning", "Nothing Selected", "Check at least one row with \"payment pending\" before retrying.");
            return;
        }

        var uploader = new InvoiceUploader();
        int ok = 0, fail = 0;
        var summaries = new List<string>();

        foreach (var g in grupos)
        {
            InvoiceUploader.UploadResult result = uploader.RetryPayments(sap_db, g.Item1, g.Item3, g.Item2);
            _preview.RecordUploadResult(g.Item1, g.Item2, g.Item3, lCurUser, result.Success, result.Summary);
            summaries.Add(g.Item1 + " [" + g.Item2 + "] " + g.Item3.ToString("MM/dd") + ": " + result.Summary);
            if (result.Success) ok++; else fail++;
        }

        lblResult.Text = ok + " group(s) with payments created" + (fail > 0 ? ", " + fail + " blocked/failed" : "")
            + ". " + string.Join(" | ", summaries);

        rgHead.Rebind();
        rgRequests.Rebind();
        ShowLastRun();
    }

    protected void rgHead_ItemCommand(object sender, GridCommandEventArgs e)
    {
        if (e.CommandName != "ShowDetail") return;

        var item = e.Item as GridDataItem;
        if (item == null) return;

        string cardCode = item.GetDataKeyValue("CardCode").ToString();
        string cardName = item.GetDataKeyValue("CardName").ToString();
        string transType = item.GetDataKeyValue("TransType").ToString();
        DateTime fecha = Convert.ToDateTime(item.GetDataKeyValue("FechaDoc"));
        bool esDevolucion = Convert.ToBoolean(item.GetDataKeyValue("EsDevolucion"));

        lblDetailHeader.Text = cardCode + " - " + cardName + " - " + fecha.ToString("MM/dd/yyyy")
            + " - " + (esDevolucion ? "Return" : "Sale");

        DataTable mismatches = _preview.GetTransactionMismatches(cardCode, fecha, esDevolucion, transType);

        lblNoMismatches.Visible = mismatches.Rows.Count == 0;
        rgMismatches.Visible = mismatches.Rows.Count > 0;
        rgMismatches.DataSource = mismatches;
        rgMismatches.DataBind();

        // Lines still without ODLN/ORDN for this store/day (the "Pending deliveries" block),
        // each with its delivery error if it has one.
        DataTable pendientes = _preview.GetPendingDeliveryItems(cardCode, fecha, transType);
        pnlPendingItems.Visible = pendientes.Rows.Count > 0;
        rgPendingItems.DataSource = pendientes;
        rgPendingItems.DataBind();

        pnlDetail.Visible = true;
    }

    protected void chkHideManual_CheckedChanged(object sender, EventArgs e)
    {
        rgHead.Rebind();
    }

    protected void chkShowInvoiced_CheckedChanged(object sender, EventArgs e)
    {
        pnlDetail.Visible = false;
        rgHead.Rebind();
    }

    protected void rgHead_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        DataTable dt = _preview.GetPreview(sap_db, bplId, Desde, Hasta, chkShowInvoiced.Checked);
        if (_preview.LastError != null)
            ShowMessage("Error", "Could Not Load Preview", _preview.LastError);

        if (chkHideManual.Checked)
        {
            // "bost_Close" is a dead end — the ODLN/ORDN lines are already fully drawn in SAP,
            // so someone has to resolve it directly in SAP B1. Hidden by default.
            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                object reason = dt.Rows[i]["BlockReason"];
                if (reason != DBNull.Value && reason.ToString().Contains("Manual resolution required"))
                    dt.Rows[i].Delete();
            }
            dt.AcceptChanges();
        }

        // Groups queued for the night process (Upload Requests with ProcessedAt NULL).
        dt.Columns.Add("InProgress", typeof(bool));
        dt.Columns.Add("InProgressInfo", typeof(string));
        var pendientes = new Dictionary<string, string>();
        foreach (DataRow p in _preview.GetPendingUploadRequests(bplId).Rows)
        {
            string key = p["CardCode"] + "|" + p["TransType"] + "|" + Convert.ToDateTime(p["FechaDoc"]).ToString("yyyyMMdd");
            pendientes[key] = p["RequestedBy"] + " " + Convert.ToDateTime(p["RequestedAt"]).ToString("MM/dd HH:mm");
        }
        foreach (DataRow r in dt.Rows)
        {
            string key = r["CardCode"] + "|" + r["TransType"] + "|" + Convert.ToDateTime(r["FechaDoc"]).ToString("yyyyMMdd");
            string info;
            bool inProgress = pendientes.TryGetValue(key, out info);
            r["InProgress"] = inProgress;
            r["InProgressInfo"] = inProgress ? info : (object)DBNull.Value;
        }

        rgHead.DataSource = dt;
    }

    protected void rgRequests_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        rgRequests.DataSource = _preview.GetUploadRequests(Desde, Hasta, bplId);
    }

    protected void rgRequests_ItemDataBound(object sender, GridItemEventArgs e)
    {
        var item = e.Item as GridDataItem;
        if (item == null) return;

        var row = ((DataRowView)item.DataItem).Row;
        var lblStatus = item.FindControl("lblRequestStatus") as System.Web.UI.WebControls.Label;
        if (lblStatus == null) return;

        if (row["ProcessedAt"] == DBNull.Value)
        {
            lblStatus.Text = "In Progress";
            lblStatus.ForeColor = System.Drawing.Color.DarkOrange;
        }
        else if (row["Success"] != DBNull.Value && Convert.ToBoolean(row["Success"]))
        {
            lblStatus.Text = "Success";
            lblStatus.ForeColor = System.Drawing.Color.Green;
        }
        else
        {
            lblStatus.Text = "Failed";
            lblStatus.ForeColor = System.Drawing.Color.Firebrick;
        }
    }

    protected void rgHead_ItemDataBound(object sender, GridItemEventArgs e)
    {
        var item = e.Item as GridDataItem;
        if (item == null) return;

        var row = ((DataRowView)item.DataItem).Row;
        bool blocked = Convert.ToBoolean(row["Blocked"]);
        bool invoiced = Convert.ToBoolean(row["Invoiced"]);
        bool esDevolucion = Convert.ToBoolean(row["EsDevolucion"]);
        bool paymentPending = Convert.ToBoolean(row["PaymentPending"]);
        bool inProgress = Convert.ToBoolean(row["InProgress"]);

        var lblTipo = item.FindControl("lblTipo") as System.Web.UI.WebControls.Label;
        if (lblTipo != null) lblTipo.Text = esDevolucion ? "Return" : "Sale";

        var lblStatus = item.FindControl("lblStatus") as System.Web.UI.WebControls.Label;
        if (lblStatus != null)
        {
            if (invoiced && paymentPending)
            {
                lblStatus.Text = "Invoiced: " + (esDevolucion ? "ORIN #" : "OINV #") + row["InvoicedDocNum"]
                    + " - " + (esDevolucion ? "refund" : "payment") + " pending";
                lblStatus.ForeColor = System.Drawing.Color.DarkOrange;
            }
            else if (invoiced)
            {
                lblStatus.Text = "Invoiced: " + (esDevolucion ? "ORIN #" : "OINV #") + row["InvoicedDocNum"];
                lblStatus.ForeColor = System.Drawing.Color.SteelBlue;
            }
            else if (inProgress)
            {
                // Queued for the night upload; it re-validates then, so a Blocked group would come back Failed.
                lblStatus.Text = "In Progress (" + row["InProgressInfo"] + ")";
                lblStatus.ForeColor = System.Drawing.Color.DarkOrange;
            }
            else if (blocked)
            {
                lblStatus.Text = "Blocked: " + (row["BlockReason"] == DBNull.Value ? "" : row["BlockReason"].ToString());
                lblStatus.ForeColor = System.Drawing.Color.Firebrick;
            }
            else
            {
                // Cent difference vs payments that InvoiceUploader absorbs on the largest line.
                decimal adj = row["RoundingAdj"] == DBNull.Value ? 0m : Convert.ToDecimal(row["RoundingAdj"]);
                lblStatus.Text = adj != 0 ? "Ready (rounding adj. " + adj.ToString("+0.00;-0.00") + ")" : "Ready";
                lblStatus.ForeColor = System.Drawing.Color.Green;
            }
        }

        var chk = item.FindControl("chkSelect") as System.Web.UI.WebControls.CheckBox;
        if (chk != null)
            chk.Enabled = (!blocked && !(inProgress && _queueMode && !invoiced)) || (paymentPending && _retryPaymentsEnabled);
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
