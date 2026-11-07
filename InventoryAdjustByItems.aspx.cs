using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Net;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;

public partial class InventoryAdjustByItems : BasePage
{
    protected SqlDb  db       = new SqlDb();
    protected string lCurUser = "";
    protected string sap_db   = "";

    private const string SESSION_KEY = "IABItems";
    private bool IsReceipt { get { return rblType.SelectedValue == "OIGN"; } }

    // ── Page lifecycle ────────────────────────────────────────────────────────

    protected void Page_Load(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty((string)Session["UserId"]))
        { Response.Redirect("Login1.aspx"); return; }

        lCurUser = (string)Session["UserId"];
        sap_db   = (string)Session["CompanyId"];

        string strAccessType = "", strRole = "";
        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, "InventoryAdjustByExcel.aspx", ref strAccessType, ref strRole);
        db.Disconnect();

        if (strAccessType == "N")
        { Response.Redirect("Default.aspx"); return; }

        labelForm.InnerText = strAccessType == "R"
            ? "Inventory Adjustment by Items (Read-Only)"
            : "Inventory Adjustment by Items";

        if (strAccessType == "R")
            btnCreate.Enabled = false;

        if (!IsPostBack)
        {
            ClearSessionItems();
            db.Connect();
            LoadWarehouses();
            db.Disconnect();
            BindGrid();
        }
        else
        {
            ApplyWhsUTypeAttributes();
        }
    }

    // ── Session item list ─────────────────────────────────────────────────────

    private DataTable GetSessionItems()
    {
        if (Session[SESSION_KEY] == null)
        {
            var dt = new DataTable();
            dt.Columns.Add("ItemCode",   typeof(string));
            dt.Columns.Add("U_CSKU_ID", typeof(string));
            dt.Columns.Add("ItemName",   typeof(string));
            dt.Columns.Add("OnHand",     typeof(decimal));
            dt.Columns.Add("AvgPrice",   typeof(decimal));
            dt.Columns.Add("U_Type",     typeof(string));
            dt.Columns.Add("Qty",        typeof(int));
            Session[SESSION_KEY] = dt;
        }
        return (DataTable)Session[SESSION_KEY];
    }

    private void ClearSessionItems() { Session.Remove(SESSION_KEY); }

    private void BindGrid()
    {
        gvItems.DataSource = GetSessionItems();
        gvItems.DataBind();
    }

    // Reads current TextBox values from grid rows into the Session DataTable
    // before any re-bind, preserving quantities the user has already entered.
    private void SyncQtysToSession()
    {
        DataTable dt = GetSessionItems();
        foreach (GridViewRow row in gvItems.Rows)
        {
            if (row.RowType != DataControlRowType.DataRow) continue;
            var hfItemCode = (HiddenField)row.FindControl("hfItemCode");
            if (hfItemCode == null) continue;
            DataRow[] rows = dt.Select("ItemCode='" + hfItemCode.Value.Replace("'", "''") + "'");
            if (rows.Length == 0) continue;

            var tbQty      = (TextBox)row.FindControl("tbQty");
            var tbAvgPrice = (TextBox)row.FindControl("tbAvgPrice");

            if (tbQty != null)      { int qty = 0; int.TryParse((tbQty.Text ?? "").Trim(), out qty); rows[0]["Qty"] = qty; }
            if (tbAvgPrice != null) { decimal avg = 0; decimal.TryParse((tbAvgPrice.Text ?? "").Trim(), out avg); rows[0]["AvgPrice"] = avg; }
        }
    }

    // ── Data loaders ──────────────────────────────────────────────────────────

    private void LoadWarehouses()
    {
        string branchFilter = BranchId > 0 ? " AND O.BPLId = " + BranchId : "";
        string hideUType    = ConfigurationManager.AppSettings["HideWhsUType"] ?? "";
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
        drpWhsCode.Items.Insert(0, new System.Web.UI.WebControls.ListItem("Select a warehouse", "0"));

        // Build WhsCode→U_Type map, store in ViewState so it survives postbacks
        var map = new System.Collections.Hashtable();
        foreach (DataRow r in dt.Rows)
            map[r["WhsCode"].ToString()] = r["U_Type"].ToString();
        ViewState["WhsUTypeMap"] = map;
        ApplyWhsUTypeAttributes();
    }

    // Re-applies data-utype attributes to dropdown options (lost after every postback).
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

    // ── Event handlers ────────────────────────────────────────────────────────

    protected void rblType_SelectedIndexChanged(object sender, EventArgs e)
    {
        ClearSessionItems();
        ViewState.Remove("WhsUType");
        hfWhsUType.Value = "";
        BindGrid();
        HideActionButtons();
        lblMessage.Text = "";
    }

    protected void drpWhsCode_SelectedIndexChanged(object sender, EventArgs e)
    {
        string oldUType = (ViewState["WhsUType"] ?? "").ToString();
        string newWhsCode = drpWhsCode.SelectedValue;
        string newUType = "";
        var map = ViewState["WhsUTypeMap"] as System.Collections.Hashtable;
        if (map != null && map.ContainsKey(newWhsCode))
            newUType = (map[newWhsCode] ?? "").ToString();

        bool typeDiffers = !string.Equals(oldUType, newUType, StringComparison.OrdinalIgnoreCase);
        bool hasItems    = GetSessionItems().Rows.Count > 0;

        if (typeDiffers && hasItems)
        {
            ClearSessionItems();
            tbGTKHeader.Text = "";
            HideActionButtons();
            lblMessage.Text = "";
        }

        ViewState["WhsUType"] = newUType;
        hfWhsUType.Value      = newUType;
        BindGrid();
    }

    protected void gvItems_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType != DataControlRowType.DataRow) return;

        // AvgPrice: ReadOnly (posts value back) when > 0; editable when = 0
        var tbAvgPrice = (TextBox)e.Row.FindControl("tbAvgPrice");
        if (tbAvgPrice != null)
        {
            var drv     = e.Row.DataItem as System.Data.DataRowView;
            decimal avg = drv != null ? Convert.ToDecimal(drv["AvgPrice"]) : 0;
            bool editable        = avg <= 0;
            tbAvgPrice.ReadOnly  = !editable;
            tbAvgPrice.BackColor = editable ? Color.White : Color.LightGray;
        }
    }

    protected void gvItems_RowDeleting(object sender, GridViewDeleteEventArgs e)
    {
        SyncQtysToSession();
        DataTable dt = GetSessionItems();
        dt.Rows[e.RowIndex].Delete();
        dt.AcceptChanges();
        BindGrid();
        HideActionButtons();
        if (dt.Rows.Count > 0)
        {
            SetMessage(dt.Rows.Count + " item(s) in list. Enter quantities and click Preview.", Color.DarkGreen);
            btnPreview.Style["display"] = "inline-block";
        }
        else
            lblMessage.Text = "";
    }

    protected void btnAddItem_Click(object sender, EventArgs e)
    {
        HideActionButtons();
        lblMessage.Text = "";

        if (drpWhsCode.SelectedValue == "0")
        { SetMessage("Please select a warehouse first.", Color.DarkOrange); return; }

        string input = (tbItemCode.Text ?? "").Trim();
        if (string.IsNullOrEmpty(input))
        { SetMessage("Please enter an item code or barcode.", Color.DarkOrange); return; }

        string whsCode = drpWhsCode.SelectedValue;

        db.Connect();
        try
        {
            string itemCode = ResolveItemCode(input);
            if (string.IsNullOrEmpty(itemCode))
            {
                SetMessage("Item not found: [" + input + "]", Color.DarkOrange);
                return;
            }

            DataTable dt = GetSessionItems();

            if (dt.Select("ItemCode='" + itemCode.Replace("'", "''") + "'").Length > 0)
            {
                SetMessage("Item [" + itemCode + "] is already in the list.", Color.DarkOrange);
                tbItemCode.Text = "";
                if (dt.Rows.Count > 0) btnPreview.Style["display"] = "inline-block";
                return;
            }

            // Sync existing qtys before re-bind triggered by adding new row
            SyncQtysToSession();

            // Query item metadata + adjusted OnHand
            string intransit = BuildIntransitSubquery();
            string sql = string.Format(
                @"SELECT i.ItemCode,
                         ISNULL(i.U_CSKU_ID,'') AS U_CSKU_ID,
                         i.ItemName,
                         ISNULL(iw.OnHand,0) - ISNULL(it.Qty_intransit_out,0) AS OnHand,
                         ISNULL(iw.AvgPrice,0) AS AvgPrice,
                         ISNULL(i.U_Type,'') AS U_Type
                  FROM   [{0}].dbo.OITM i WITH(NOLOCK)
                  LEFT   JOIN [{0}].dbo.OITW iw WITH(NOLOCK)
                         ON  iw.ItemCode = i.ItemCode AND iw.WhsCode = @whs
                  LEFT   JOIN {1} it ON it.FromLoc = @whs AND it.ItemCode = i.ItemCode
                  WHERE  i.ItemCode = @item",
                sap_db, intransit);

            DataTable dtItem = new DataTable();
            using (SqlCommand cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@whs",  whsCode);
                cmd.Parameters.AddWithValue("@item", itemCode);
                using (SqlDataAdapter adp = new SqlDataAdapter(cmd)) adp.Fill(dtItem);
            }

            if (dtItem.Rows.Count == 0)
            { SetMessage("Item [" + itemCode + "] not found in the system.", Color.DarkOrange); return; }

            DataRow r        = dtItem.Rows[0];
            decimal onHand   = Convert.ToDecimal(r["OnHand"]);
            decimal avgPrice = Convert.ToDecimal(r["AvgPrice"]);
            string  uType    = r["U_Type"].ToString();

            // OIGE: must have available stock
            if (!IsReceipt && onHand <= 0)
            {
                SetMessage("Item [" + itemCode + "] has no available stock (OnHand = 0) in warehouse [" +
                    whsCode + "]. Cannot add for a Goods Issue.", Color.DarkOrange);
                return;
            }

            // Store WhsUType for RowDataBound (GTK/AvgPrice enable) and JS preview validation
            string whsUType = GetWhsUType(whsCode);
            ViewState["WhsUType"] = whsUType;
            hfWhsUType.Value      = whsUType;
            if (!string.IsNullOrEmpty(whsUType) &&
                !string.Equals(uType, "NON SELL", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(uType) &&
                !string.Equals(uType, whsUType, StringComparison.OrdinalIgnoreCase))
            {
                SetMessage("Item [" + itemCode + "] belongs to operation type '" + uType +
                    "' but the selected warehouse is '" + whsUType + "'.", Color.DarkOrange);
                return;
            }

            DataRow newRow      = dt.NewRow();
            newRow["ItemCode"]  = r["ItemCode"].ToString();
            newRow["U_CSKU_ID"] = r["U_CSKU_ID"].ToString();
            newRow["ItemName"]  = r["ItemName"].ToString();
            newRow["OnHand"]    = onHand;
            newRow["AvgPrice"]  = avgPrice;
            newRow["U_Type"]    = uType;
            newRow["Qty"] = 0;
            dt.Rows.Add(newRow);

            BindGrid();
            tbItemCode.Text = "";

            SetMessage(dt.Rows.Count + " item(s) in list. Enter quantities and click Preview.", Color.DarkGreen);
            btnPreview.Style["display"] = "inline-block";
        }
        finally { db.Disconnect(); }
    }

    protected void btnCreate_Click(object sender, EventArgs e)
    {
        lblMessage.Text = "";
        string whsCode = drpWhsCode.SelectedValue;

        var selectedQtys  = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var selectedCosts = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var selectedOrder = new List<string>();
        bool qtyErrors = false;

        foreach (GridViewRow row in gvItems.Rows)
        {
            if (row.RowType != DataControlRowType.DataRow) continue;
            var tbQty = (TextBox)row.FindControl("tbQty");
            if (tbQty == null) continue;
            string qtyStr = (tbQty.Text ?? "").Trim();
            if (string.IsNullOrEmpty(qtyStr)) continue;

            int qty;
            if (!int.TryParse(qtyStr, out qty) || qty <= 0)
            { row.BackColor = Color.Yellow; qtyErrors = true; continue; }

            var hfItemCode = (HiddenField)row.FindControl("hfItemCode");
            if (hfItemCode == null || string.IsNullOrEmpty(hfItemCode.Value)) continue;

            var tbAvgPrice = (TextBox)row.FindControl("tbAvgPrice");
            decimal cost = 0;
            if (tbAvgPrice != null) decimal.TryParse((tbAvgPrice.Text ?? "").Trim(), out cost);

            selectedQtys[hfItemCode.Value]  = qty;
            selectedCosts[hfItemCode.Value] = cost;
            selectedOrder.Add(hfItemCode.Value);
        }

        if (qtyErrors)
        {
            SetMessage("Quantities must be numeric and greater than 0. Rows highlighted in yellow.", Color.DarkOrange);
            ShowActionButtons();
            return;
        }

        if (selectedQtys.Count == 0)
        { SetMessage("Please enter a quantity greater than 0 for at least one item.", Color.DarkOrange); return; }

        // AvgPrice must be > 0 for all items with quantity
        bool avgErrors = false;
        foreach (GridViewRow row in gvItems.Rows)
        {
            if (row.RowType != DataControlRowType.DataRow) continue;
            var hfItemCode = (HiddenField)row.FindControl("hfItemCode");
            if (hfItemCode == null || !selectedQtys.ContainsKey(hfItemCode.Value)) continue;
            decimal cost;
            selectedCosts.TryGetValue(hfItemCode.Value, out cost);
            if (cost <= 0)
            { row.BackColor = Color.LightYellow; avgErrors = true; }
        }
        if (avgErrors)
        {
            SetMessage("Avg Price must be greater than 0 for all items with quantity. Rows highlighted in yellow.", Color.DarkRed);
            ShowActionButtons();
            return;
        }

        // GTK required and validated when warehouse is Duty Free
        string whsUType = (ViewState["WhsUType"] ?? "").ToString();
        string gtkHeader = (tbGTKHeader.Text ?? "").Trim();
        if (string.Equals(whsUType, "Duty Free", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(gtkHeader))
            {
                SetMessage("This warehouse is Duty Free — GTK confirmation number is required.", Color.DarkRed);
                ShowActionButtons();
                return;
            }
            string configGtk = ConfigurationManager.AppSettings["GTKConfirmationCode"] ?? "";
            if (!string.IsNullOrEmpty(configGtk) &&
                !string.Equals(gtkHeader, configGtk, StringComparison.OrdinalIgnoreCase))
            {
                SetMessage("GTK confirmation number is not valid.", Color.DarkRed);
                ShowActionButtons();
                return;
            }
        }

        string inList = BuildInList(selectedOrder);

        db.Connect();
        bool valid;
        try   { valid = ValidateSelected(whsCode, selectedQtys, inList); }
        finally { db.Disconnect(); }

        if (!valid) { ShowActionButtons(); return; }

        CreateInSap(whsCode, selectedQtys, gtkHeader, selectedCosts, selectedOrder);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private bool ValidateSelected(string whsCode,
        Dictionary<string, int> selectedQtys, string inList)
    {
        // U_Type check
        string whsUType = GetWhsUType(whsCode);
        if (!string.IsNullOrEmpty(whsUType))
        {
            DataTable dtTypes = new DataTable();
            using (SqlCommand cmd = new SqlCommand(
                string.Format(
                    "SELECT ItemCode, ISNULL(U_Type,'') AS U_Type FROM [{0}].dbo.OITM WITH(NOLOCK) WHERE ItemCode IN ({1})",
                    sap_db, inList), db.Conn))
            using (SqlDataAdapter adp = new SqlDataAdapter(cmd)) adp.Fill(dtTypes);

            var itemTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in dtTypes.Rows)
                itemTypes[r["ItemCode"].ToString()] = r["U_Type"].ToString();

            bool typeErrors = false;
            foreach (GridViewRow row in gvItems.Rows)
            {
                if (row.RowType != DataControlRowType.DataRow) continue;
                var tbQty      = (TextBox)row.FindControl("tbQty");
                var hfItemCode = (HiddenField)row.FindControl("hfItemCode");
                if (tbQty == null || string.IsNullOrEmpty((tbQty.Text ?? "").Trim())) continue;
                if (hfItemCode == null) continue;

                string iType;
                itemTypes.TryGetValue(hfItemCode.Value, out iType);
                iType = iType ?? "";

                bool isNonSell = string.Equals(iType, "NON SELL", StringComparison.OrdinalIgnoreCase);
                if (!isNonSell && !string.IsNullOrEmpty(iType) &&
                    !string.Equals(iType, whsUType, StringComparison.OrdinalIgnoreCase))
                {
                    row.BackColor = Color.LightCoral;
                    typeErrors    = true;
                }
            }
            if (typeErrors)
            {
                SetMessage("Some items do not belong to the '" + whsUType +
                    "' operation type. Rows highlighted in red.", Color.DarkRed);
                return false;
            }
        }

        if (!IsReceipt)
        {
            // OIGE: adjusted OnHand >= Qty
            string itSub = BuildIntransitSubquery();
            DataTable dtOh = new DataTable();
            using (SqlCommand cmd = new SqlCommand(
                string.Format(
                    @"SELECT iw.ItemCode,
                             ISNULL(iw.OnHand,0) - ISNULL(it.Qty_intransit_out,0) AS OnHand
                      FROM   [{0}].dbo.OITW iw WITH(NOLOCK)
                      LEFT   JOIN {1} it ON it.FromLoc = iw.WhsCode AND it.ItemCode = iw.ItemCode
                      WHERE  iw.WhsCode = @whs AND iw.ItemCode IN ({2})",
                    sap_db, itSub, inList), db.Conn))
            {
                cmd.Parameters.AddWithValue("@whs", whsCode);
                using (SqlDataAdapter adp = new SqlDataAdapter(cmd)) adp.Fill(dtOh);
            }

            var onHands = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in dtOh.Rows)
                onHands[r["ItemCode"].ToString()] = Convert.ToDecimal(r["OnHand"]);

            bool ohErrors = false;
            foreach (GridViewRow row in gvItems.Rows)
            {
                if (row.RowType != DataControlRowType.DataRow) continue;
                var tbQty      = (TextBox)row.FindControl("tbQty");
                var hfItemCode = (HiddenField)row.FindControl("hfItemCode");
                if (tbQty == null || string.IsNullOrEmpty((tbQty.Text ?? "").Trim())) continue;
                if (hfItemCode == null) continue;

                int qty = 0;
                int.TryParse((tbQty.Text ?? "").Trim(), out qty);
                decimal oh;
                onHands.TryGetValue(hfItemCode.Value, out oh);
                if (oh < qty)
                { row.BackColor = Color.LightCoral; ohErrors = true; }
            }
            if (ohErrors)
            {
                SetMessage("Some items have insufficient stock (OnHand < Quantity) in warehouse [" + whsCode +
                    "]. Rows highlighted in red.", Color.DarkRed);
                return false;
            }
        }

        return true;
    }

    // ── SAP Service Layer ─────────────────────────────────────────────────────

    private void CreateInSap(string whsCode,
        Dictionary<string, int> selectedQtys,
        string gtk,
        Dictionary<string, decimal> selectedCosts,
        List<string> selectedOrder)
    {
        var lines = new JArray();
        foreach (string itemCode in selectedOrder)
        {
            int qty;
            if (!selectedQtys.TryGetValue(itemCode, out qty)) continue;

            decimal cost = 0;
            selectedCosts.TryGetValue(itemCode, out cost);

            var line = new JObject(
                new JProperty("ItemCode",      itemCode),
                new JProperty("Quantity",      qty),
                new JProperty("WarehouseCode", whsCode)
            );
            if (cost > 0)
                line.Add(new JProperty("UnitPrice", cost));

            lines.Add(line);
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
            try
            {
                var r = JObject.Parse(response);
                if (r["DocNum"] != null) docNum = Convert.ToInt32(r["DocNum"]);
            }
            catch { }

            ShowMessage("Success", docType + " Created",
                "SAP B1 " + docType + " #" + docNum + " created successfully for warehouse [" + whsCode + "].");

            ClearSessionItems();
            tbGTKHeader.Text = "";
            ViewState.Remove("WhsUType");
            hfWhsUType.Value = "";
            BindGrid();
            HideActionButtons();
            lblMessage.Text = "";
        }
        catch (WebException wex)
        {
            ShowMessage("Error", "SAP Service Layer Error", SapServiceLayer.GetSlErrorMessage(wex));
        }
        catch (Exception ex)
        {
            ShowMessage("Error", "Unexpected Error", ex.Message);
        }
        finally { sl.Logout(); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Looks up an item code by exact ItemCode first, then by barcode (OBCD).
    private string ResolveItemCode(string input)
    {
        string sql = string.Format(
            @"SELECT TOP 1 ItemCode FROM (
                  SELECT i.ItemCode, 1 AS Priority
                  FROM   [{0}].dbo.OITM i WITH(NOLOCK) WHERE i.ItemCode = @input
                  UNION ALL
                  SELECT b.ItemCode, 2 AS Priority
                  FROM   [{0}].dbo.OBCD b WITH(NOLOCK) WHERE b.BcdCode = @input
              ) x ORDER BY Priority", sap_db);

        using (SqlCommand cmd = new SqlCommand(sql, db.Conn))
        {
            cmd.Parameters.AddWithValue("@input", input);
            object result = cmd.ExecuteScalar();
            return (result != null && result != DBNull.Value) ? result.ToString() : null;
        }
    }

    private string BuildIntransitSubquery()
    {
        string cid = sap_db.Replace("'", "''");
        return
            @"(SELECT FromLoc, ItemCode, SUM(Qty_intransit) AS Qty_intransit_out
               FROM (
                   SELECT sh.FromWhsCode AS FromLoc, sd.ItemCode,
                          SUM(ISNULL(sd.dispatchquantity,0)) AS Qty_intransit
                   FROM   smm_Transdiscrep_odrf sh WITH(NOLOCK)
                   INNER  JOIN smm_Transdiscrep_drf1 sd WITH(NOLOCK)
                          ON sh.CompanyId = sd.CompanyId AND sh.DocEntry = sd.DocEntry
                   WHERE  sh.CompanyId = '" + cid + @"'
                     AND (sh.DocStatus = 'O' OR (sh.DocStatus = 'C'
                          AND (ISNULL(sh.DocEntryTraRec2,0) + ISNULL(sh.DocEntryTraRec,0) = 0)))
                   GROUP  BY sh.FromWhsCode, sd.ItemCode
                   UNION ALL
                   SELECT odrf.Filler AS FromLoc, drf1.ItemCode,
                          SUM(drf1.Quantity) AS Qty_intransit
                   FROM   SMM_ODRF AS odrf WITH(NOLOCK)
                   INNER  JOIN SMM_DRF1 AS drf1 WITH(NOLOCK)
                          ON odrf.CompanyId = drf1.CompanyId AND odrf.DocEntry = drf1.DocEntry
                   WHERE  odrf.CompanyId = '" + cid + @"' AND odrf.ObjType = 67
                     AND  odrf.DocStatus = 'O' AND odrf.CANCELED = 'N'
                     AND  NOT EXISTS (SELECT 1 FROM smm_Transdiscrep_odrf xx WITH(NOLOCK)
                                      WHERE xx.CompanyId = odrf.CompanyId AND xx.DocEntry = odrf.DocEntry)
                   GROUP  BY odrf.Filler, drf1.ItemCode
                   UNION ALL
                   SELECT odrf.FromWhsCode AS FromLoc, drf1.ItemCode,
                          SUM(drf1.TmpQuantity) AS Qty_intransit
                   FROM   smm_TransXsap_odrf AS odrf WITH(NOLOCK)
                   INNER  JOIN smm_TransXsap_drf1 AS drf1 WITH(NOLOCK)
                          ON odrf.DocEntry = drf1.DocEntry AND odrf.CompanyId = drf1.CompanyId
                   WHERE  NOT EXISTS (
                       SELECT 1 FROM smm_Transdiscrep_odrf xx WITH(NOLOCK)
                       WHERE  xx.CompanyId = odrf.CompanyId AND xx.DocEntry = odrf.DocEntry
                       UNION ALL
                       SELECT 1 FROM smm_odrf xx WITH(NOLOCK)
                       WHERE  xx.CompanyId = odrf.CompanyId AND xx.DocEntry = odrf.DocEntry)
                   GROUP  BY odrf.FromWhsCode, drf1.ItemCode
               ) A
               GROUP BY FromLoc, ItemCode)";
    }

    private string GetWhsUType(string whsCode)
    {
        using (SqlCommand cmd = new SqlCommand(
            string.Format(
                "SELECT ISNULL(U_Type,'') FROM [{0}].dbo.OWHS WITH(NOLOCK) WHERE WhsCode=@whs",
                sap_db), db.Conn))
        {
            cmd.Parameters.AddWithValue("@whs", whsCode);
            object result = cmd.ExecuteScalar();
            return result != null ? result.ToString() : "";
        }
    }

    private static string BuildInList(List<string> items)
    {
        var parts = new List<string>(items.Count);
        foreach (string item in items)
            parts.Add("'" + item.Replace("'", "''") + "'");
        return string.Join(",", parts.ToArray());
    }

    private void HideActionButtons()
    {
        btnPreview.Style["display"] = "none";
        pnlCreate.Style["display"]  = "none";
    }

    private void ShowActionButtons()
    {
        btnPreview.Style["display"] = "inline-block";
        pnlCreate.Style["display"]  = "inline-block";
    }

    private void SetMessage(string text, Color color)
    {
        lblMessage.Text      = text;
        lblMessage.ForeColor = color;
    }

    private void ShowMessage(string type, string title, string body)
    {
        try { ((SiteMaster)Master).ShowDivMessage(type, title, body); }
        catch { SetMessage(title + ": " + body, type == "Success" ? Color.DarkGreen : Color.DarkRed); }
    }
}
