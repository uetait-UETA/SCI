using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Text;
using System.Web.UI.WebControls;

public partial class PrintPriceLabel : BasePage
{
    protected SqlDb  db       = new SqlDb();
    protected string lCurUser = "";
    protected string sap_db   = "";

    // ── Page lifecycle ────────────────────────────────────────────────────────

    // Captures any unhandled exception at page level before Application_Error
    // redirects to login — shows the real error on screen instead.
    protected override void OnError(EventArgs e)
    {
        Exception ex = Server.GetLastError();
        if (ex != null)
        {
            Server.ClearError();
            try
            {
                pnlItem.Visible = true;
                SetResult("ERROR: " + ex.GetType().Name + " — " + ex.Message +
                    (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : ""),
                    Color.DarkRed);
            }
            catch { Response.Write("<b>Error:</b> " + ex.Message); }
        }
        base.OnError(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty((string)Session["UserId"]))
        { Response.Redirect("Login1.aspx"); return; }

        lCurUser = (string)Session["UserId"];
        sap_db   = (string)Session["CompanyId"];

        string strAccessType = "", strRole = "";
        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, "PrintPriceLabel.aspx", ref strAccessType, ref strRole);
        db.Disconnect();

        if (strAccessType == "N")
        { Response.Redirect("Default.aspx"); return; }

        labelForm.InnerText = "Print Price Label";

        if (!IsPostBack)
            LoadPrinters();
    }

    // ── Loaders ───────────────────────────────────────────────────────────────

    private void LoadPrinters()
    {
        drpPrinter.Items.Clear();
        drpPrinter.Items.Add(new ListItem("-- Select printer --", ""));

        try
        {
            foreach (string p in RawPrinterHelper.GetInstalledPrinters())
                drpPrinter.Items.Add(new ListItem(p, p));
        }
        catch { /* no printers installed or access denied */ }
    }

    // ── Events ────────────────────────────────────────────────────────────────

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        lblMessage.Text = "";
        lblResult.Text  = "";
        string input = (tbItemCode.Text ?? "").Trim();
        if (string.IsNullOrEmpty(input))
        { SetMessage("Please enter an item code or barcode.", Color.DarkOrange); return; }

        SearchItem(input);
        tbItemCode.Text = "";
    }

    [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
    [System.Security.SecurityCritical]
    protected void btnPrint_Click(object sender, EventArgs e)
    {
        try
        {
            lblResult.Text = "";

            if (string.IsNullOrEmpty(drpPrinter.SelectedValue))
            { SetResult("Please select a printer.", Color.DarkOrange); return; }

            int qty = 1;
            int.TryParse((tbQty.Text ?? "1").Trim(), out qty);
            if (qty < 1) qty = 1;

            string itemCode  = lblBarcode.Text;
            string itemName  = lblItemName.Text;
            string priceText = lblPrice.Text;
            string currency  = lblCurrency.Text;
            string branch    = lblBranchName.Text;

            string zpl = BuildZpl(itemCode, itemName, priceText, currency, branch, qty);

            RawPrinterHelper.SendZpl(drpPrinter.SelectedValue, zpl);
            SetResult("OK: " + qty + " label(s) sent to [" + drpPrinter.SelectedValue + "].", Color.DarkGreen);
        }
        catch (Exception ex)
        {
            SetResult("Error al imprimir: " + ex.GetType().Name + " — " + ex.Message, Color.DarkRed);
        }
    }

    // ── Core logic ────────────────────────────────────────────────────────────

    private void SearchItem(string input)
    {
        int listNum = BranchId == 4 ? 2 : 1;

        db.Connect();
        try
        {
            // Resolve barcode → ItemCode
            string itemCode = ResolveItemCode(input);
            if (string.IsNullOrEmpty(itemCode))
            {
                pnlItem.Visible = false;
                SetMessage("Item not found: [" + input + "]", Color.DarkOrange);
                return;
            }

            // Query item + price
            string sql = string.Format(
                @"SELECT i.ItemCode,
                         i.ItemName,
                         ISNULL(p.Price,    0)     AS Price,
                         ISNULL(p.Currency, 'USD') AS Currency
                  FROM   [{0}].dbo.OITM i WITH(NOLOCK)
                  LEFT   JOIN [{0}].dbo.ITM1 p WITH(NOLOCK)
                         ON  p.ItemCode = i.ItemCode
                         AND p.PriceList = @list
                  WHERE  i.ItemCode = @item", sap_db);

            DataTable dt = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@list", listNum);
                cmd.Parameters.AddWithValue("@item", itemCode);
                using (SqlDataAdapter adp = new SqlDataAdapter(cmd)) adp.Fill(dt);
            }

            if (dt.Rows.Count == 0)
            {
                pnlItem.Visible = false;
                SetMessage("Item [" + itemCode + "] not found in SAP.", Color.DarkOrange);
                return;
            }

            DataRow r    = dt.Rows[0];
            decimal price = Convert.ToDecimal(r["Price"]);
            string  curr  = r["Currency"].ToString();
            string  name  = r["ItemName"].ToString();
            string  code  = r["ItemCode"].ToString();

            string listLabel = listNum == 2 ? "Branch 4 (Lista 2)" : "Branch 3 (Lista 1)";

            // Populate preview
            lblBranchName.Text = BranchName + "  —  " + listLabel;
            lblItemCode.Text   = code;
            lblItemName.Text   = name;
            lblBarcode.Text    = code;   // used as data-key for reloads
            lblPrice.Text      = price.ToString("N2");
            lblCurrency.Text   = curr;

            pnlItem.Visible = true;
            lblMessage.Text = "";
        }
        finally { db.Disconnect(); }
    }

    // Resolves ItemCode → first by exact OITM match, then by barcode (OBCD).
    private string ResolveItemCode(string input)
    {
        string sql = string.Format(
            @"SELECT TOP 1 ItemCode FROM (
                  SELECT ItemCode, 1 AS P FROM [{0}].dbo.OITM WITH(NOLOCK) WHERE ItemCode = @v
                  UNION ALL
                  SELECT ItemCode, 2 AS P FROM [{0}].dbo.OBCD WITH(NOLOCK) WHERE BcdCode  = @v
              ) x ORDER BY P", sap_db);

        using (SqlCommand cmd = new SqlCommand(sql, db.Conn))
        {
            cmd.Parameters.AddWithValue("@v", input);
            object r = cmd.ExecuteScalar();
            return (r != null && r != DBNull.Value) ? r.ToString() : null;
        }
    }

    // ── ZPL builder ───────────────────────────────────────────────────────────
    // Zebra 203 dpi — label 1.5" × 1.0" (305 × 203 dots).
    // Layout (Y positions):
    //   0-2   top border
    //   3-14  item code (12px)
    //   16-29 item name (13px, 1 line truncated)
    //   30-92 Code128 barcode (height=52) + human-readable (~10px below)
    //  103    separator
    //  106    "Precio / Price" (13px)
    //  121    price (52px) + currency small at right
    //  201    bottom border
    private static string BuildZpl(string itemCode, string itemName,
        string price, string currency, string branchName, int qty)
    {
        Func<string, string> esc = s => (s ?? "").Replace("^", "").Replace("~", "");

        string code = esc(itemCode);
        string name = esc(itemName);
        string pr   = esc(price);
        string cur  = esc(currency);

        var sb = new StringBuilder();
        sb.AppendLine("^XA");
        sb.AppendLine("^PW305");   // 1.5" × 203dpi
        sb.AppendLine("^LL203");   // 1.0" × 203dpi
        sb.AppendLine("^CI28");    // UTF-8
        sb.AppendLine("^LH160,160"); // shift origin 2cm right, 2cm down

        // Top border
        sb.AppendLine("^FO0,0^GB305,2,2^FS");

        // Item code (y=3, 12px)
        sb.AppendFormat("^FO3,3^A0N,12,11^FD{0}^FS{1}", code, Environment.NewLine);

        // Item name (y=16, 13px, 1 line max)
        sb.AppendFormat("^FO3,16^A0N,13,11^FB299,1,1,L^FD{0}^FS{1}", name, Environment.NewLine);

        // Code 128 barcode (y=30, height=52, human-readable below)
        // ^BY1,3 = module 1 dot, ratio 3; ^BCN,52,Y,N,N = normal, 52 dots tall, HRI yes, above no
        sb.AppendFormat("^FO3,30^BY1,3^BCN,52,Y,N,N^FD{0}^FS{1}", code, Environment.NewLine);

        // Separator (barcode + HRI ends ~y=100)
        sb.AppendLine("^FO0,103^GB305,2,2^FS");

        // Price large (y=106, height=52)
        sb.AppendFormat("^FO3,106^A0N,52,30^FD{0}^FS{1}", pr, Environment.NewLine);

        // Currency (small, aligned right-bottom of price)
        sb.AppendFormat("^FO250,163^A0N,15,14^FD{0}^FS{1}", cur, Environment.NewLine);

        // Bottom border
        sb.AppendLine("^FO0,201^GB305,2,2^FS");

        // Print quantity
        sb.AppendFormat("^PQ{0},0,1,Y{1}", qty, Environment.NewLine);

        sb.AppendLine("^XZ");
        return sb.ToString();
    }

    // ── UI helpers ────────────────────────────────────────────────────────────

    private void SetMessage(string text, Color color)
    { lblMessage.Text = text; lblMessage.ForeColor = color; }

    private void SetResult(string text, Color color)
    { lblResult.Text = text; lblResult.ForeColor = color; }
}
