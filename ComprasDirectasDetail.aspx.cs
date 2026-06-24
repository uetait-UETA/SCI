using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Net;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using Telerik.Web.UI;

public partial class ComprasDirectasDetail : BasePage
{
    private readonly GoodsReceipt _gr = new GoodsReceipt();
    private bool _allowReceive = false;

    private int DocEntry
    {
        get
        {
            int val;
            return int.TryParse(Request.QueryString["docEntry"], out val) ? val : 0;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty((string)Session["UserId"]) ||
            string.IsNullOrEmpty((string)Session["CompanyId"]))
        {
            Response.Redirect("Login1.aspx");
            return;
        }

        int docEntry = DocEntry;
        if (docEntry <= 0)
        {
            Response.Redirect("ComprasDirectas.aspx");
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
            Response.Redirect("ComprasDirectas.aspx");
            return;
        }

        _allowReceive      = (accessType == "F");
        btnConfirm.Enabled = _allowReceive;

        if (!IsPostBack)
            LoadHeader((string)Session["CompanyId"], docEntry);
    }

    private void LoadHeader(string sapDb, int docEntry)
    {
        DataRow row = _gr.GetApReserveInvoiceHeader(sapDb, docEntry);
        if (row == null)
        {
            ShowMessage("Error", "Not Found", "AP Reserve Invoice not found.");
            return;
        }
        lblApriNum.Text  = row["DocNum"].ToString();
        lblDate.Text     = Convert.ToDateTime(row["DocDate"]).ToString("MM/dd/yyyy");
        lblVendor.Text   = row["CardName"].ToString();
        lblRefNum.Text   = row["NumAtCard"].ToString();
        lblTotal.Text    = Convert.ToDecimal(row["DocTotal"]).ToString("N2");
        lblCurrency.Text = row["DocCur"].ToString();
    }

    protected void rgLines_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        string sapDb = (string)Session["CompanyId"];
        rgLines.DataSource = _gr.GetApReserveInvoiceLines(sapDb, DocEntry);
    }

    protected void rgLines_ItemDataBound(object sender, GridItemEventArgs e)
    {
        if (!(e.Item is GridDataItem)) return;
        var item = (GridDataItem)e.Item;

        // Highlight Duty Paid rows in yellow
        object whsTypeObj = DataBinder.Eval(item.DataItem, "WhsType");
        if (whsTypeObj != null && whsTypeObj.ToString() == "Duty Paid")
            item.BackColor = System.Drawing.Color.FromArgb(255, 255, 210);

        // Default received qty to APRI quantity
        TextBox txt = item.FindControl("txtReceivedQty") as TextBox;
        if (txt != null)
        {
            object qty = DataBinder.Eval(item.DataItem, "Quantity");
            if (qty != null)
                txt.Text = Convert.ToDecimal(qty)
                    .ToString("0.######", CultureInfo.InvariantCulture);
        }
    }

    protected void btnConfirm_Click(object sender, EventArgs e)
    {
        if (!_allowReceive) return;

        string sapDb    = (string)Session["CompanyId"];
        int    bplId    = BranchId;
        string userId   = (string)Session["UserId"];
        int    docEntry = DocEntry;

        // Collect entered quantities from grid rows
        var receivedQtys = new Dictionary<int, decimal>();
        foreach (GridDataItem item in rgLines.Items)
        {
            int lineNum = Convert.ToInt32(item.GetDataKeyValue("LineNum"));
            TextBox txt = item.FindControl("txtReceivedQty") as TextBox;
            decimal qty = 0;
            if (txt != null)
                decimal.TryParse(txt.Text, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out qty);
            receivedQtys[lineNum] = qty;
        }

        bool hasQty = false;
        foreach (var kv in receivedQtys)
            if (kv.Value > 0) { hasQty = true; break; }

        if (!hasQty)
        {
            ShowMessage("Warning", "No Quantity",
                "Enter a received quantity greater than 0 for at least one item.");
            return;
        }

        DataRow header = _gr.GetApReserveInvoiceHeader(sapDb, docEntry);
        if (header == null)
        {
            ShowMessage("Error", "Error",
                _gr.LastError ?? "Could not load AP Reserve Invoice header.");
            return;
        }
        string cardCode   = header["CardCode"].ToString();
        int    opchDocNum = Convert.ToInt32(header["DocNum"]);

        DataTable dtLines = _gr.GetApReserveInvoiceLines(sapDb, docEntry);
        if (_gr.LastError != null || dtLines.Rows.Count == 0)
        {
            ShowMessage("Error", "No Lines",
                _gr.LastError ?? "No lines found for this AP Reserve Invoice.");
            return;
        }

        string payload = _gr.BuildGrpoFromOpchWithQty(
            cardCode, bplId, docEntry, opchDocNum, dtLines, receivedQtys);

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

            btnConfirm.Enabled = false;
            btnCancel.Text     = "Back to List";

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

    protected void btnCancel_Click(object sender, EventArgs e)
    {
        Response.Redirect("ComprasDirectas.aspx");
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
