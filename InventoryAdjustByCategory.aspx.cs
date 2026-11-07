using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;

public partial class InventoryAdjustByCategory : BasePage
{
    protected SqlDb  db       = new SqlDb();
    protected string lCurUser = "";
    protected string sap_db   = "";

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
        {
            Response.Redirect("Default.aspx");
            return;
        }

        labelForm.InnerText = strAccessType == "R"
            ? "Inventory Adjustment by Category (Read-Only)"
            : "Inventory Adjustment by Category";

        if (strAccessType == "R")
            btnCreate.Enabled = false;

        if (!IsPostBack)
        {
            db.Connect();
            LoadWarehouses();
            LoadCategories();
            drpBrand.Items.Add(new System.Web.UI.WebControls.ListItem("All Brands", "All"));
            db.Disconnect();
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
    }

    private void LoadCategories()
    {
        string sql = string.Format(
            @"SELECT ItmsGrpCod AS GroupCode,
                     CAST(ItmsGrpCod AS varchar) + ' - ' + ItmsGrpNam AS GroupName
              FROM   [{0}].dbo.OITB WITH(NOLOCK)
              ORDER  BY ItmsGrpCod", sap_db);

        DataTable dt = new DataTable();
        db.adapter = new SqlDataAdapter(sql, db.Conn);
        db.adapter.Fill(dt);

        drpCategory.Items.Clear();
        drpCategory.Items.Add(new System.Web.UI.WebControls.ListItem("Select a category", "0"));
        drpCategory.DataSource     = dt;
        drpCategory.DataTextField  = "GroupName";
        drpCategory.DataValueField = "GroupCode";
        drpCategory.DataBind();
    }

    private void LoadBrands(string categoryCode)
    {
        drpBrand.Items.Clear();
        drpBrand.Items.Add(new System.Web.UI.WebControls.ListItem("All Brands", "All"));

        if (string.IsNullOrEmpty(categoryCode) || categoryCode == "0") return;

        DataTable dt = new DataTable();
        using (SqlCommand cmd = new SqlCommand(
            string.Format(
                @"SELECT DISTINCT ISNULL(U_Brand,'') AS Brand
                  FROM   [{0}].dbo.OITM WITH(NOLOCK)
                  WHERE  ItmsGrpCod = @cat AND U_Brand IS NOT NULL AND U_Brand <> ''
                  ORDER  BY Brand", sap_db), db.Conn))
        {
            cmd.Parameters.AddWithValue("@cat", categoryCode);
            using (SqlDataAdapter adp = new SqlDataAdapter(cmd)) adp.Fill(dt);
        }

        foreach (DataRow row in dt.Rows)
        {
            string brand = row["Brand"].ToString();
            drpBrand.Items.Add(new System.Web.UI.WebControls.ListItem(brand, brand));
        }
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    protected void rblType_SelectedIndexChanged(object sender, EventArgs e)
    {
        gvItems.DataSource = null;
        gvItems.DataBind();
        HideActionButtons();
        lblMessage.Text = "";
    }

    protected void drpCategory_SelectedIndexChanged(object sender, EventArgs e)
    {
        db.Connect();
        LoadBrands(drpCategory.SelectedValue);
        db.Disconnect();

        gvItems.DataSource = null;
        gvItems.DataBind();
        HideActionButtons();
        lblMessage.Text = "";
    }

    protected void btnLoad_Click(object sender, EventArgs e)
    {
        HideActionButtons();
        lblMessage.Text = "";

        if (drpWhsCode.SelectedValue == "0")
        { SetMessage("Please select a warehouse.", Color.DarkOrange); return; }

        if (drpCategory.SelectedValue == "0")
        { SetMessage("Please select a category.", Color.DarkOrange); return; }

        string whsCode  = drpWhsCode.SelectedValue;
        string category = drpCategory.SelectedValue;
        string brand    = drpBrand.SelectedValue;

        string brandFilter  = brand == "All" ? "" : " AND ISNULL(i.U_Brand,'') = @brand";
        string intransit    = BuildIntransitSubquery();
        string onHandFilter = IsReceipt
            ? " AND ISNULL(iw.AvgPrice,0) > 0"
            : " AND (ISNULL(iw.OnHand,0) - ISNULL(it.Qty_intransit_out,0)) > 0";

        string sql = string.Format(
            @"SELECT i.ItemCode, i.ItemName,
                     ISNULL(i.U_CSKU_ID,'') AS U_CSKU_ID,
                     ISNULL(iw.OnHand,0) - ISNULL(it.Qty_intransit_out,0) AS OnHand,
                     ISNULL(iw.AvgPrice,0) AS AvgPrice,
                     ISNULL(i.U_Type,  '') AS U_Type
              FROM   [{0}].dbo.OITM i WITH(NOLOCK)
              LEFT   JOIN [{0}].dbo.OITW iw WITH(NOLOCK)
                     ON  iw.ItemCode = i.ItemCode AND iw.WhsCode = @whs
              LEFT   JOIN {1} it ON it.FromLoc = @whs AND it.ItemCode = i.ItemCode
              WHERE  i.ItmsGrpCod = @cat{2}{3}
              ORDER  BY i.ItemName",
            sap_db, intransit, brandFilter, onHandFilter);

        db.Connect();
        DataTable dt = new DataTable();
        try
        {
            using (SqlCommand cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@whs", whsCode);
                cmd.Parameters.AddWithValue("@cat", category);
                if (brand != "All") cmd.Parameters.AddWithValue("@brand", brand);
                using (SqlDataAdapter adp = new SqlDataAdapter(cmd)) adp.Fill(dt);
            }
        }
        finally { db.Disconnect(); }

        gvItems.DataSource = dt;
        gvItems.DataBind();

        if (dt.Rows.Count == 0)
            SetMessage("No items found for the selected filters.", Color.DarkOrange);
        else
        {
            SetMessage(dt.Rows.Count + " item(s) loaded. Enter quantities and click Preview.", Color.DarkGreen);
            btnPreview.Style["display"] = "inline-block";
        }
    }

    protected void btnCreate_Click(object sender, EventArgs e)
    {
        lblMessage.Text = "";
        string whsCode = drpWhsCode.SelectedValue;

        // Collect items with qty > 0 — store itemCode → qty
        var selectedQtys  = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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
            {
                row.BackColor = Color.Yellow;
                qtyErrors = true;
                continue;
            }

            var hfItemCode = (HiddenField)row.FindControl("hfItemCode");
            if (hfItemCode == null || string.IsNullOrEmpty(hfItemCode.Value)) continue;

            string itemCode = hfItemCode.Value;
            selectedQtys[itemCode] = qty;
            selectedOrder.Add(itemCode);
        }

        if (qtyErrors)
        {
            SetMessage("Quantities must be numeric and greater than 0. Rows highlighted in yellow.", Color.DarkOrange);
            ShowActionButtons();
            return;
        }

        if (selectedQtys.Count == 0)
        {
            SetMessage("Please enter a quantity greater than 0 for at least one item.", Color.DarkOrange);
            return;
        }

        string inList = BuildInList(selectedOrder);

        db.Connect();
        bool valid;
        try   { valid = ValidateSelected(whsCode, selectedQtys, inList); }
        finally { db.Disconnect(); }

        if (!valid)
        {
            ShowActionButtons();
            return;
        }

        CreateInSap(whsCode, selectedQtys, selectedOrder);

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

        if (IsReceipt)
        {
            // OIGN: AvgPrice > 0
            DataTable dtAvg = new DataTable();
            using (SqlCommand cmd = new SqlCommand(
                string.Format(
                    "SELECT ItemCode, ISNULL(AvgPrice,0) AS AvgPrice FROM [{0}].dbo.OITW WITH(NOLOCK) WHERE WhsCode=@whs AND ItemCode IN ({1})",
                    sap_db, inList), db.Conn))
            {
                cmd.Parameters.AddWithValue("@whs", whsCode);
                using (SqlDataAdapter adp = new SqlDataAdapter(cmd)) adp.Fill(dtAvg);
            }

            var avgPrices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in dtAvg.Rows)
                avgPrices[r["ItemCode"].ToString()] = Convert.ToDecimal(r["AvgPrice"]);

            bool avgErrors = false;
            foreach (GridViewRow row in gvItems.Rows)
            {
                if (row.RowType != DataControlRowType.DataRow) continue;
                var tbQty      = (TextBox)row.FindControl("tbQty");
                var hfItemCode = (HiddenField)row.FindControl("hfItemCode");
                if (tbQty == null || string.IsNullOrEmpty((tbQty.Text ?? "").Trim())) continue;
                if (hfItemCode == null) continue;

                decimal avg;
                avgPrices.TryGetValue(hfItemCode.Value, out avg);
                if (avg <= 0)
                {
                    row.BackColor = Color.LightCoral;
                    avgErrors     = true;
                }
            }
            if (avgErrors)
            {
                SetMessage("Some items have no average price (AvgPrice = 0) in warehouse [" + whsCode +
                    "]. A positive AvgPrice is required for a Goods Receipt. Rows highlighted in red.", Color.DarkRed);
                return false;
            }
        }
        else
        {
            // OIGE: adjusted OnHand (SAP OnHand minus in-transit) >= Qty
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
                {
                    row.BackColor = Color.LightCoral;
                    ohErrors      = true;
                }
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
        Dictionary<string, int> selectedQtys, List<string> selectedOrder)
    {
        var lines = new JArray();
        foreach (string itemCode in selectedOrder)
        {
            int qty;
            if (!selectedQtys.TryGetValue(itemCode, out qty)) continue;
            lines.Add(new JObject(
                new JProperty("ItemCode",      itemCode),
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

            gvItems.DataSource = new DataTable();
            gvItems.DataBind();
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
        finally
        {
            sl.Logout();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Returns an inline subquery alias "it" with columns (FromLoc, ItemCode, Qty_intransit_out)
    // representing quantities already committed to open transfers from each warehouse+item.
    // Mirrors the SmmIntransitOut CTE in Queries.With_SmmTranSapDRF1().
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
