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

    private static string CdDocType
    {
        get { return System.Configuration.ConfigurationManager.AppSettings["ComprasDirectasDocType"] ?? "OPCH"; }
    }
    private static int CdBaseType { get { return CdDocType == "OPOR" ? 22 : 18; } }

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
                WHERE  O.BPLId IN ({3}, 1)
                  AND  EXISTS (
                    SELECT 1 FROM {2}.dbo.VendorStoreMapping vm
                    WHERE  vm.WhsCode   = O.WhsCode
                      AND  vm.CompanyId = '{0}'
                      AND  vm.IsActive  = 1
                )
                ORDER  BY O.U_POSCode",
                sapDb, Queries.WITH_NOLOCK, smmDb, BranchId);

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

    protected void chkShowReceived_CheckedChanged(object sender, EventArgs e)
    {
        rgInvoices.Rebind();
    }

    protected void rgInvoices_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        string sapDb       = (string)Session["CompanyId"];
        int    bplId       = BranchId;
        bool   showReceived = chkShowReceived.Checked;

        int docNum = 0;
        int.TryParse(txtDocNum.Text.Trim(), out docNum);
        string toWhsCode = rcbToLocation.SelectedValue;

        DataTable dt = _gr.GetPendingDirectPurchaseInvoices(
            sapDb, bplId, rdpFromDate.SelectedDate, rdpToDate.SelectedDate,
            docNum, toWhsCode, showReceived, CdDocType);

        if (_gr.LastError != null)
            ShowMessage("Error", "Query Error", _gr.LastError);

        // Toggle columns visibility
        var col = rgInvoices.MasterTableView.GetColumn("ReceivedAt");
        if (col != null) col.Visible = showReceived;
        col = rgInvoices.MasterTableView.GetColumn("ReceivedBy");
        if (col != null) col.Visible = showReceived;

        rgInvoices.DataSource = dt;
    }

    protected void rgInvoices_ItemDataBound(object sender, GridItemEventArgs e)
    {
        if (!(e.Item is GridDataItem)) return;
        var item = (GridDataItem)e.Item;

        int grpoDocNum = 0;
        object grpoObj = DataBinder.Eval(item.DataItem, "GrpoDocNum");
        if (grpoObj != null && grpoObj != DBNull.Value)
            grpoDocNum = Convert.ToInt32(grpoObj);

        Button btnReceive = item.FindControl("btnReceive") as Button;
        Label  lblGrpo    = item.FindControl("lblGrpo")    as Label;

        if (grpoDocNum > 0)
        {
            if (btnReceive != null) btnReceive.Visible = false;
            if (lblGrpo    != null) { lblGrpo.Text = "GRPO #" + grpoDocNum; lblGrpo.Visible = true; }
        }
        else
        {
            if (btnReceive != null) btnReceive.Enabled = _allowReceive;
            if (lblGrpo    != null) lblGrpo.Visible = false;
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

        // Safety check — do not re-receive an already received invoice
        if (_gr.IsAlreadyReceived((string)Session["CompanyId"], docEntry))
        {
            ShowMessage("Warning", "Already Received",
                "This invoice has already been received.");
            rgInvoices.Rebind();
            return;
        }
        string cardCode   = keys["CardCode"].ToString();
        int    opchDocNum = Convert.ToInt32(keys["DocNum"]);
        string sapDb      = (string)Session["CompanyId"];
        string userId     = (string)Session["UserId"];

        // Read header early — needed for BPLId and ToWhsCode validation
        System.Data.DataRow hdr = _gr.GetApReserveInvoiceHeader(sapDb, docEntry, CdDocType);
        int bplId = (hdr != null && hdr["BPLId"] != DBNull.Value)
            ? Convert.ToInt32(hdr["BPLId"]) : BranchId;

        // For OPOR: validate that U_ToWhsCode exists and belongs to the session branch
        string toWhsCode = hdr != null ? hdr["ToWhsCode"].ToString() : "";
        if (CdDocType == "OPOR")
        {
            string whsErr;
            if (!_gr.IsWhsValidForBranch(sapDb, toWhsCode, BranchId, out whsErr))
            {
                ShowMessage("Error", "Invalid Destination Warehouse", whsErr);
                rgInvoices.Rebind();
                return;
            }
        }

        // Duty Paid items present → detail page for quantity entry
        if (_gr.HasDutyPaidLines(sapDb, docEntry, CdDocType))
        {
            Response.Redirect("ComprasDirectasDetail.aspx?docEntry=" + docEntry, false);
            Context.ApplicationInstance.CompleteRequest();
            return;
        }

        // All Duty Free → receive with full document quantities
        try
        {
            DataTable dtLines = _gr.GetApReserveInvoiceLines(sapDb, docEntry, CdDocType);
            if (_gr.LastError != null || dtLines.Rows.Count == 0)
            {
                ShowMessage("Error", "No Lines Found",
                    _gr.LastError ?? "No lines found for this document.");
                rgInvoices.Rebind();
                return;
            }

            System.Collections.Generic.Dictionary<int, decimal> allQtys = null;
            string payload;
            if (CdDocType == "OPOR")
            {
                allQtys = new System.Collections.Generic.Dictionary<int, decimal>();
                foreach (System.Data.DataRow r in dtLines.Rows)
                    allQtys[Convert.ToInt32(r["LineNum"])] = Convert.ToDecimal(r["Quantity"]);
                payload = _gr.BuildGrpoFromOpchWithQty(cardCode, bplId,
                    docEntry, opchDocNum, dtLines, allQtys, baseType: CdBaseType);
            }
            else
            {
                payload = _gr.BuildGrpoFromOpch(cardCode, bplId,
                    docEntry, opchDocNum, dtLines);
            }

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

                string successMsg = "Goods Receipt PO #" + grpoDocNum + " created in SAP B1.";

                // For OPOR: create OWTR from receipt warehouse to U_ToWhsCode
                if (CdDocType == "OPOR" && allQtys != null && !string.IsNullOrEmpty(toWhsCode))
                {
                    string fromWhs = dtLines.Rows.Count > 0 ? dtLines.Rows[0]["WhsCode"].ToString() : "";
                    string owtrPayload = _gr.BuildOwtrPayload(bplId, fromWhs, toWhsCode, dtLines, allQtys);
                    try
                    {
                        string owtrResp = sl.CreateInventoryTransfer(owtrPayload);
                        int owtrDocNum = 0;
                        try { var ow = JObject.Parse(owtrResp); if (ow["DocNum"] != null) owtrDocNum = Convert.ToInt32(ow["DocNum"]); } catch { }
                        successMsg += " Transfer #" + owtrDocNum + " → " + toWhsCode + " created.";
                    }
                    catch (WebException wexOw)
                    {
                        successMsg += " Warning: Transfer failed — " + SapServiceLayer.GetSlErrorMessage(wexOw);
                    }
                }

                ShowMessage("Success", "Goods Receipt Created", successMsg);
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
