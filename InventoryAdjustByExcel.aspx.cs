using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;

public partial class InventoryAdjustByExcel : BasePage
{
    protected SqlDb db     = new SqlDb();
    protected string lCurUser;
    protected string sap_db;
    private string lfullFilePath = "";
    private string lFileExt      = "";

    private int invalidQty      = 0;
    private int dupQty          = 0;
    private int wrongTypeQty    = 0;
    private int notInWhsQty     = 0;
    private int noAvgPriceQty   = 0;
    private int insufficientQty = 0;

    private bool IsReceipt { get { return rblType.SelectedValue == "OIGN"; } }

    // Session key for the validated DataTable (used by btnConfirm)
    private const string SESSION_DATA = "InventoryAdjust_ValidatedData";

    protected void Page_Load(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty((string)Session["UserId"]))
        {
            Response.Redirect("Login1.aspx");
            return;
        }

        lCurUser = (string)Session["UserId"];
        sap_db   = (string)Session["CompanyId"];

        string strAccessType = "", strRole = "";
        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, "InventoryAdjustByExcel.aspx", ref strAccessType, ref strRole);
        db.Disconnect();

        if (strAccessType == "N")
        {
            string msg = "User " + lCurUser + ", with Role " + strRole +
                         " does not have permissions to access this screen.";
            ScriptManager.RegisterStartupScript(Page, Page.GetType(), "noAccess",
                "{ alert('" + msg.Replace("'", "\\'") + "'); window.location='Default.aspx'; }", true);
            return;
        }

        if (strAccessType == "R")
        {
            btnConfirm.Enabled   = false;
            labelForm.InnerText  = "Inventory Adjustment by Excel (Read-Only Access)";
        }
        else if (strAccessType == "F")
        {
            labelForm.InnerText = "Inventory Adjustment by Excel (Full Access)";
        }

        if (!IsPostBack)
        {
            db.Connect();
            LoadWarehouses();
            db.Disconnect();
            Session.Remove(SESSION_DATA);
        }
        else
        {
            ApplyWhsUTypeAttributes();
        }

        // Auto-validate triggered by file selection (JS sets hdnAction = "validate")
        if (IsPostBack && hdnAction.Value == "validate")
        {
            hdnAction.Value = "";
            RunValidation();
        }
    }

    private void LoadWarehouses()
    {
        string branchFilter = BranchId > 0 ? " AND O.BPLId = " + BranchId : "";
        string hideUType    = System.Configuration.ConfigurationManager.AppSettings["HideWhsUType"] ?? "";
        string hideFilter   = !string.IsNullOrEmpty(hideUType)
            ? " AND ISNULL(O.U_Type,'') <> '" + hideUType.Replace("'", "''") + "'"
            : "";

        string sql = string.Format(
            @"SELECT O.WhsCode,
                     ISNULL(O.U_Type,'') AS U_Type,
                     CONVERT(nvarchar(30), ISNULL(O.U_POSCode,'')) + ' - ' + O.WhsCode + ' - ' + O.WhsName AS WHS
              FROM   [{0}].dbo.OWHS O WITH(NOLOCK)
              JOIN   RSS_OWHS_CONTROL R WITH(NOLOCK)
                     ON  R.WhsCode   = O.WhsCode
                     AND R.Control   = 'SETMINMAX'
                     AND R.CompanyId = '{0}'
              WHERE  1=1{1}{2}
              ORDER  BY O.U_POSCode",
            sap_db, branchFilter, hideFilter);

        DataTable dt = new DataTable();
        db.adapter = new SqlDataAdapter(sql, db.Conn);
        db.adapter.Fill(dt);

        drpWhsCode.DataSource     = dt;
        drpWhsCode.DataTextField  = "WHS";
        drpWhsCode.DataValueField = "WhsCode";
        drpWhsCode.DataBind();
        drpWhsCode.Items.Insert(0, new ListItem("Select a warehouse", "0"));

        var map = new System.Collections.Hashtable();
        foreach (DataRow r in dt.Rows)
            map[r["WhsCode"].ToString()] = r["U_Type"].ToString();
        ViewState["WhsUTypeMap"] = map;
        ApplyWhsUTypeAttributes();
    }

    private void ApplyWhsUTypeAttributes()
    {
        var map = ViewState["WhsUTypeMap"] as System.Collections.Hashtable;
        if (map == null) return;
        foreach (System.Collections.DictionaryEntry kvp in map)
        {
            var item = drpWhsCode.Items.FindByValue(kvp.Key.ToString());
            if (item != null) item.Attributes.Add("data-utype", kvp.Value.ToString());
        }
    }

    // ── Step 1: Auto-validate on file select ─────────────────────────────────

    private void RunValidation()
    {
        lblMessage.Text    = "";
        pnlConfirm.Visible = false;
        Session.Remove(SESSION_DATA);

        if (drpWhsCode.SelectedValue == "0")
        {
            SetMessage("Please select a warehouse before choosing the file.", Color.DarkOrange);
            return;
        }

        if (!FileUpload1.HasFile)
        {
            SetMessage("No file received. Please try again.", Color.DarkOrange);
            return;
        }

        if (!SaveFile()) return;

        string connStr = lFileExt == ".xls"
            ? "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + lfullFilePath + ";Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1;\""
            : "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + lfullFilePath + ";Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1;\"";

        DataTable excelData = LoadExcelData(connStr);
        if (excelData == null) return;

        if (excelData.Rows.Count == 0)
        {
            SetMessage("The Excel file has no data rows.", Color.DarkOrange);
            return;
        }

        // Bind to grid for display + validation
        gvData.DataSource = excelData;
        gvData.DataBind();

        if (!ValidateGrid(drpWhsCode.SelectedValue)) return;

        // All validations passed
        Session[SESSION_DATA] = excelData;
        pnlConfirm.Visible    = true;
        SetMessage("Validation passed. Click \"Create in SAP B1\" to proceed.", Color.DarkGreen);
    }

    // ── Step 2: Confirm → create in SAP ──────────────────────────────────────

    protected void btnConfirm_Click(object sender, EventArgs e)
    {
        DataTable excelData = Session[SESSION_DATA] as DataTable;
        if (excelData == null || excelData.Rows.Count == 0)
        {
            SetMessage("Session expired or no validated data. Please re-select the Excel file.", Color.DarkOrange);
            pnlConfirm.Visible = false;
            return;
        }

        // Re-bind grid so the user can see the data while the result message is shown
        gvData.DataSource = excelData;
        gvData.DataBind();

        // GTK validation for Duty Free warehouses
        string whsUType  = (hfWhsUType.Value ?? "").Trim();
        string gtkHeader = (tbGTKHeader.Text ?? "").Trim();
        if (string.Equals(whsUType, "Duty Free", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(gtkHeader))
            {
                SetMessage("This warehouse is Duty Free — GTK confirmation number is required.", Color.DarkRed);
                pnlConfirm.Visible = true;
                return;
            }
            string configGtk = System.Configuration.ConfigurationManager.AppSettings["GTKConfirmationCode"] ?? "";
            if (!string.IsNullOrEmpty(configGtk) &&
                !string.Equals(gtkHeader, configGtk, StringComparison.OrdinalIgnoreCase))
            {
                SetMessage("GTK confirmation number is not valid.", Color.DarkRed);
                pnlConfirm.Visible = true;
                return;
            }
        }

        CreateInSap(drpWhsCode.SelectedValue, excelData, gtkHeader);
        Session.Remove(SESSION_DATA);
        pnlConfirm.Visible = false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool SaveFile()
    {
        string ext = Path.GetExtension(FileUpload1.FileName).ToLower();
        if (ext != ".xls" && ext != ".xlsx")
        {
            SetMessage("The file must be Excel format (.xls or .xlsx).", Color.DarkRed);
            return false;
        }
        string filePath = Server.MapPath("temp") + "\\" + FileUpload1.FileName;
        FileUpload1.SaveAs(filePath);
        lfullFilePath = filePath;
        lFileExt      = ext;
        return true;
    }

    private DataTable LoadExcelData(string connStr)
    {
        try
        {
            using (OleDbConnection conn = new OleDbConnection(connStr))
            {
                conn.Open();
                DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                if (schema == null || schema.Rows.Count == 0)
                {
                    SetMessage("Could not read the Excel file structure.", Color.DarkRed);
                    return null;
                }
                string sheetName = schema.Rows[0]["TABLE_NAME"].ToString();
                DataSet ds = new DataSet();
                new OleDbDataAdapter("SELECT * FROM [" + sheetName + "]", conn).Fill(ds, "data");
                return ds.Tables[0];
            }
        }
        catch (Exception ex)
        {
            SetMessage("Error reading file: " + ex.Message, Color.DarkRed);
            return null;
        }
    }

    // Returns true if all validations pass.
    private bool ValidateGrid(string whsCode)
    {
        CheckRows();
        if (invalidQty > 0)
        {
            SetMessage("There are invalid quantities (must be numeric and > 0). Rows highlighted in yellow.", Color.DarkOrange);
            return false;
        }

        CheckDuplicates();
        if (dupQty > 0)
        {
            SetMessage("There are duplicate items. Rows highlighted in yellow.", Color.DarkOrange);
            return false;
        }

        db.Connect();
        try
        {
            // Validate item U_Type matches warehouse U_Type (same logic as MinMaxByExcel)
            string whsUType = GetWhsUType(whsCode);
            if (!string.IsNullOrEmpty(whsUType))
            {
                CheckItemTypes(whsUType);
                if (wrongTypeQty > 0)
                {
                    SetMessage("Some items do not belong to the '" + whsUType +
                               "' operation type. Rows highlighted in red.", Color.DarkRed);
                    return false;
                }
            }

            CheckItemsInWarehouse(whsCode);
            if (notInWhsQty > 0)
            {
                SetMessage("Some items were not found in warehouse [" + whsCode + "]. Rows highlighted in red.", Color.DarkRed);
                return false;
            }

            if (IsReceipt)
            {
                CheckAvgPrice(whsCode);
                if (noAvgPriceQty > 0)
                {
                    SetMessage("Some items have no average price (AvgPrice = 0) in warehouse [" + whsCode +
                               "]. A positive AvgPrice is required for a Goods Receipt. Rows highlighted in red.", Color.DarkRed);
                    return false;
                }
            }
            else
            {
                CheckOnHand(whsCode);
                if (insufficientQty > 0)
                {
                    SetMessage("Some items have insufficient stock (OnHand < Quantity) in warehouse [" + whsCode +
                               "]. Rows highlighted in red.", Color.DarkRed);
                    return false;
                }
            }
        }
        finally
        {
            db.Disconnect();
        }

        return true;
    }

    // ── Validation checks ─────────────────────────────────────────────────────

    private void CheckRows()
    {
        for (int i = 0; i < gvData.Rows.Count; i++)
        {
            string item = gvData.Rows[i].Cells[0].Text;
            if (item == "&nbsp;" || string.IsNullOrEmpty(item)) continue;

            decimal qty;
            bool valid = decimal.TryParse(gvData.Rows[i].Cells[1].Text,
                NumberStyles.Any, CultureInfo.InvariantCulture, out qty) && qty > 0;
            if (!valid) { gvData.Rows[i].BackColor = Color.Yellow; invalidQty++; }
        }
    }

    private void CheckDuplicates()
    {
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < gvData.Rows.Count; i++)
        {
            string item = gvData.Rows[i].Cells[0].Text;
            if (item == "&nbsp;" || string.IsNullOrEmpty(item)) continue;

            if (seen.ContainsKey(item))
            {
                gvData.Rows[i].BackColor = Color.Yellow;
                if (seen[item] >= 0) { gvData.Rows[seen[item]].BackColor = Color.Yellow; seen[item] = -1; }
                dupQty++;
            }
            else seen[item] = i;
        }
    }

    private string GetWhsUType(string whsCode)
    {
        using (SqlCommand cmd = new SqlCommand(
            string.Format("SELECT ISNULL(U_Type,'') FROM [{0}].dbo.OWHS WITH(NOLOCK) WHERE WhsCode=@whs", sap_db),
            db.Conn))
        {
            cmd.Parameters.AddWithValue("@whs", whsCode);
            object result = cmd.ExecuteScalar();
            return result != null ? result.ToString() : "";
        }
    }

    private void CheckItemTypes(string whsUType)
    {
        List<string> items = GetItemCodesFromGrid();
        if (items.Count == 0) return;

        DataTable dt = new DataTable();
        using (SqlCommand cmd = new SqlCommand(
            string.Format("SELECT ItemCode, ISNULL(U_Type,'') AS U_Type FROM [{0}].dbo.OITM WITH(NOLOCK) WHERE ItemCode IN ({1})",
                sap_db, BuildInList(items)), db.Conn))
        using (SqlDataAdapter adp = new SqlDataAdapter(cmd))
            adp.Fill(dt);

        var itemTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataRow row in dt.Rows)
            itemTypes[row["ItemCode"].ToString()] = row["U_Type"].ToString();

        for (int i = 0; i < gvData.Rows.Count; i++)
        {
            string item = gvData.Rows[i].Cells[0].Text;
            if (item == "&nbsp;" || string.IsNullOrEmpty(item)) continue;

            string itemType = itemTypes.ContainsKey(item) ? itemTypes[item] : "";
            bool isNonSell  = string.Equals(itemType, "NON SELL", StringComparison.OrdinalIgnoreCase);
            if (!isNonSell && !string.Equals(itemType, whsUType, StringComparison.OrdinalIgnoreCase))
            {
                gvData.Rows[i].BackColor = Color.LightCoral;
                wrongTypeQty++;
            }
        }
    }

    private void CheckItemsInWarehouse(string whsCode)
    {
        List<string> items = GetItemCodesFromGrid();
        if (items.Count == 0) return;

        DataTable dt = new DataTable();
        using (SqlCommand cmd = new SqlCommand(
            string.Format("SELECT ItemCode FROM [{0}].dbo.OITW WITH(NOLOCK) WHERE WhsCode=@whs AND ItemCode IN ({1})",
                sap_db, BuildInList(items)), db.Conn))
        {
            cmd.Parameters.AddWithValue("@whs", whsCode);
            using (SqlDataAdapter adp = new SqlDataAdapter(cmd)) adp.Fill(dt);
        }

        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataRow row in dt.Rows) found.Add(row["ItemCode"].ToString());

        for (int i = 0; i < gvData.Rows.Count; i++)
        {
            string item = gvData.Rows[i].Cells[0].Text;
            if (item == "&nbsp;" || string.IsNullOrEmpty(item)) continue;
            if (!found.Contains(item)) { gvData.Rows[i].BackColor = Color.LightCoral; notInWhsQty++; }
        }
    }

    private void CheckAvgPrice(string whsCode)
    {
        List<string> items = GetItemCodesFromGrid();
        if (items.Count == 0) return;

        DataTable dt = new DataTable();
        using (SqlCommand cmd = new SqlCommand(
            string.Format("SELECT ItemCode, AvgPrice FROM [{0}].dbo.OITW WITH(NOLOCK) WHERE WhsCode=@whs AND ItemCode IN ({1})",
                sap_db, BuildInList(items)), db.Conn))
        {
            cmd.Parameters.AddWithValue("@whs", whsCode);
            using (SqlDataAdapter adp = new SqlDataAdapter(cmd)) adp.Fill(dt);
        }

        var avgPrices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (DataRow row in dt.Rows)
            avgPrices[row["ItemCode"].ToString()] = Convert.ToDecimal(row["AvgPrice"]);

        for (int i = 0; i < gvData.Rows.Count; i++)
        {
            string item = gvData.Rows[i].Cells[0].Text;
            if (item == "&nbsp;" || string.IsNullOrEmpty(item)) continue;
            decimal avg;
            if (!avgPrices.TryGetValue(item, out avg) || avg <= 0)
            { gvData.Rows[i].BackColor = Color.LightCoral; noAvgPriceQty++; }
        }
    }

    private void CheckOnHand(string whsCode)
    {
        List<string> items = GetItemCodesFromGrid();
        if (items.Count == 0) return;

        DataTable dt = new DataTable();
        using (SqlCommand cmd = new SqlCommand(
            string.Format("SELECT ItemCode, OnHand FROM [{0}].dbo.OITW WITH(NOLOCK) WHERE WhsCode=@whs AND ItemCode IN ({1})",
                sap_db, BuildInList(items)), db.Conn))
        {
            cmd.Parameters.AddWithValue("@whs", whsCode);
            using (SqlDataAdapter adp = new SqlDataAdapter(cmd)) adp.Fill(dt);
        }

        var onHands = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (DataRow row in dt.Rows)
            onHands[row["ItemCode"].ToString()] = Convert.ToDecimal(row["OnHand"]);

        for (int i = 0; i < gvData.Rows.Count; i++)
        {
            string item = gvData.Rows[i].Cells[0].Text;
            if (item == "&nbsp;" || string.IsNullOrEmpty(item)) continue;
            decimal qty;
            decimal.TryParse(gvData.Rows[i].Cells[1].Text, NumberStyles.Any, CultureInfo.InvariantCulture, out qty);
            decimal onHand;
            if (!onHands.TryGetValue(item, out onHand) || onHand < qty)
            { gvData.Rows[i].BackColor = Color.LightCoral; insufficientQty++; }
        }
    }

    // ── SAP Service Layer ─────────────────────────────────────────────────────

    private void CreateInSap(string whsCode, DataTable excelData, string gtk = "")
    {
        var lines = new JArray();
        foreach (DataRow row in excelData.Rows)
        {
            string item = row[0].ToString();
            if (string.IsNullOrEmpty(item)) continue;
            decimal qty;
            decimal.TryParse(row[1].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out qty);
            lines.Add(new JObject(
                new JProperty("ItemCode",      item),
                new JProperty("Quantity",      qty),
                new JProperty("WarehouseCode", whsCode)
            ));
        }

        string docType = IsReceipt ? "Goods Receipt (OIGN)" : "Goods Issue (OIGE)";
        var payload = new JObject(
            new JProperty("BPL_IDAssignedToInvoice", BranchId),
            new JProperty("Comments", "Adjustment by " + lCurUser + " via SCI"),
            new JProperty("DocumentLines", lines)
        );
        if (!string.IsNullOrEmpty(gtk))
            payload.Add(new JProperty("U_GTK_CONFIRMATION", gtk));

        var sl = new SapServiceLayer();
        try
        {
            sl.Login(sap_db);
            string response = IsReceipt
                ? sl.CreateInventoryGenEntry(payload.ToString(Newtonsoft.Json.Formatting.None))
                : sl.CreateInventoryGenExit(payload.ToString(Newtonsoft.Json.Formatting.None));

            int docNum = 0;
            try { var r = JObject.Parse(response); if (r["DocNum"] != null) docNum = Convert.ToInt32(r["DocNum"]); } catch { }

            ShowMessage("Success", docType + " Created",
                "SAP B1 " + docType + " #" + docNum + " created successfully for warehouse [" + whsCode + "].");

            gvData.DataSource = new DataTable();
            gvData.DataBind();
            lblMessage.Text = "";
        }
        catch (WebException wex)
        {
            ShowMessage("Error", "SAP Error", SapServiceLayer.GetSlErrorMessage(wex));
        }
        catch (Exception ex)
        {
            ShowMessage("Error", "Unexpected Error", ex.Message);
        }
        finally
        {
            sl.Logout();
        }
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private List<string> GetItemCodesFromGrid()
    {
        var items = new List<string>();
        for (int i = 0; i < gvData.Rows.Count; i++)
        {
            string item = gvData.Rows[i].Cells[0].Text;
            if (item != "&nbsp;" && !string.IsNullOrEmpty(item))
                items.Add(item);
        }
        return items;
    }

    private static string BuildInList(List<string> items)
    {
        return string.Join(",", items.ConvertAll(c => "'" + c.Replace("'", "''") + "'"));
    }

    private void SetMessage(string text, Color color)
    {
        lblMessage.Text      = text;
        lblMessage.ForeColor = color;
    }

    private void ShowMessage(string type, string title, string body)
    {
        try { ((SiteMaster)Master).ShowDivMessage(type, title, body); } catch { }
    }
}
