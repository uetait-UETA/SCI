using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using Telerik.Web.UI;

public partial class FillPriority : BasePage
{
    protected SqlDb   db       = new SqlDb();
    protected string  lCurUser;
    protected string  sap_db;

    // ── Page lifecycle ────────────────────────────────────────────────────────
    protected void Page_Load(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty((string)Session["UserId"]) ||
            string.IsNullOrEmpty((string)Session["CompanyId"]))
        {
            Response.Redirect("Login1.aspx");
            return;
        }

        sap_db   = (string)Session["CompanyId"];
        lCurUser = (string)Session["UserId"];

        string strAccessType = "", strRole_Description = "";
        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, "FillPriority.aspx", ref strAccessType, ref strRole_Description);
        db.Disconnect();

        if (strAccessType == "N")
        {
            string msg = "User " + lCurUser + ", with Role " + strRole_Description + " does not have permissions to access this screen.";
            ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "alert",
                "{ alert('" + msg + "'); window.location = 'Default.aspx'; }", true);
            return;
        }

        if (strAccessType == "R")
        {
            rbtnSave.Enabled  = false;
            rbtnSave.ForeColor = Color.Silver;
            btnImport.Enabled  = false;
            labelForm.InnerText = "Fill Priority (Read-Only Access)";
        }
        else if (strAccessType == "F")
        {
            rbtnSave.Enabled    = true;
            labelForm.InnerText = "Fill Priority (Full Access)";
        }

        if (!IsPostBack)
        {
            try   { LoadItemGroup(); }
            catch (Exception ex) { ShowMasterPageMessage("Error", "Failed in Page_Load", ex.Message); }
        }
    }

    private void ShowMasterPageMessage(string type, string title, string message)
    {
        try { ((SiteMaster)this.Master).ShowDivMessage(type, title, message); }
        catch { }
    }

    // ── Data load ─────────────────────────────────────────────────────────────
    private void LoadItemGroup()
    {
        string sql = @"SELECT ItmsGrpCod GroupCode,
                              CAST(ItmsGrpCod AS varchar) + ' - ' + [dbo].[InitCap](ItmsGrpNam) GroupName
                       FROM " + sap_db + @".dbo.oitb
                       ORDER BY GroupName";
        DataTable dt = new DataTable();
        db.Connect();
        try   { using (SqlDataAdapter a = new SqlDataAdapter(sql, db.Conn)) a.Fill(dt); }
        finally { db.Disconnect(); }

        rcbCategory.DataTextField  = "GroupName";
        rcbCategory.DataValueField = "GroupCode";
        rcbCategory.DataSource     = dt;
        rcbCategory.DataBind();
    }

    private DataTable GetFillPriority()
    {
        string sql = @"SELECT a.Company, a.[Location], b.WhsName AS LocationName,
                              a.Dept, a.[Priority], b.U_POSCode
                       FROM dbo.Repln_Location_Priority a " + Queries.WITH_NOLOCK + @"
                       INNER JOIN " + sap_db + @".dbo.OWHS b " + Queries.WITH_NOLOCK + @"
                           ON a.[Location] = b.WhsCode
                       WHERE a.Company = @company AND a.Dept = @dept AND b.BPLId = @branch
                       ORDER BY a.[Priority], b.U_POSCode";

        DataTable dt = new DataTable();
        db.Connect();
        try
        {
            using (SqlDataAdapter a = new SqlDataAdapter(sql, db.Conn))
            {
                a.SelectCommand.Parameters.AddWithValue("@company", sap_db);
                a.SelectCommand.Parameters.AddWithValue("@dept",    Convert.ToInt32(rcbCategory.SelectedValue));
                a.SelectCommand.Parameters.AddWithValue("@branch",  BranchId);
                a.Fill(dt);
            }
        }
        finally { db.Disconnect(); }
        return dt;
    }

    private DataTable GetWarehousesAsNewPriority()
    {
        string sql = @"SELECT b.WhsCode AS [Location], b.WhsName AS LocationName, b.U_POSCode
                       FROM " + sap_db + @".dbo.OWHS b " + Queries.WITH_NOLOCK + @"
                       WHERE b.BPLId = @branch
                       ORDER BY b.U_POSCode, b.WhsCode";

        DataTable dt = new DataTable();
        db.Connect();
        try
        {
            using (SqlDataAdapter a = new SqlDataAdapter(sql, db.Conn))
            {
                a.SelectCommand.Parameters.AddWithValue("@branch", BranchId);
                a.Fill(dt);
            }
        }
        finally { db.Disconnect(); }

        dt.Columns.Add("Company",  typeof(string));
        dt.Columns.Add("Dept",     typeof(int));
        dt.Columns.Add("Priority", typeof(int));

        foreach (DataRow row in dt.Rows)
        {
            row["Company"]  = sap_db;
            row["Dept"]     = Convert.ToInt32(rcbCategory.SelectedValue);
            row["Priority"] = 99;   // default: no priority
        }
        return dt;
    }

    // ── Grid events ──────────────────────────────────────────────────────────
    protected void rcbCategory_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        if (rcbCategory.SelectedValue != "")
            rgPriority.Rebind();
    }

    protected void rgPriority_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        if (rcbCategory.SelectedValue == "") return;
        divGrid.Attributes.Add("style", "display:block");
        DataTable dtExisting = GetFillPriority();
        rgPriority.DataSource = dtExisting.Rows.Count > 0 ? dtExisting : GetWarehousesAsNewPriority();
    }

    // ── Save (manual grid) ───────────────────────────────────────────────────
    protected void rbtnSave_Click(object sender, EventArgs e)
    {
        if (rgPriority.MasterTableView.Items.Count == 0)
        {
            ShowMasterPageMessage("Error", "Error", "No data to save.");
            return;
        }
        if (rcbCategory.SelectedValue == "")
        {
            ShowMasterPageMessage("Error", "Error", "Please select a category.");
            return;
        }

        DataTable dt = new DataTable();
        dt.Columns.Add("Location", typeof(string));
        dt.Columns.Add("Priority", typeof(int));

        foreach (GridDataItem item in rgPriority.MasterTableView.Items)
        {
            string loc = item["Location"].Text;
            TextBox txtPriority = (TextBox)item["Priority"].FindControl("txtPriority");
            int priority;
            if (!int.TryParse(txtPriority.Text.Trim(), out priority) || priority < 1 || priority > 99)
            {
                ShowMasterPageMessage("Error", "Invalid priority", "Priority for '" + loc + "' must be a number between 1 and 99.");
                return;
            }
            dt.Rows.Add(loc, priority);
        }

        // Duplicate check — 99 can repeat, 1-98 cannot
        var dupes = dt.AsEnumerable()
            .Where(r => (int)r["Priority"] < 99)
            .GroupBy(r => r["Priority"])
            .Where(g => g.Count() > 1).ToList();
        if (dupes.Count > 0)
        {
            ShowMasterPageMessage("Error", "Duplicate priorities", "Priority values 1-98 must be unique per category.");
            return;
        }

        // Normalise to the column names MergePriorities expects
        DataTable dtMerge = new DataTable();
        dtMerge.Columns.Add("WhsCode",      typeof(string));
        dtMerge.Columns.Add("CategoryCode", typeof(int));
        dtMerge.Columns.Add("Priority",     typeof(int));
        foreach (DataRow r in dt.Rows)
            dtMerge.Rows.Add(r["Location"].ToString(), Convert.ToInt32(rcbCategory.SelectedValue), (int)r["Priority"]);

        string successMsg;
        if (MergePriorities(dtMerge, out successMsg))
        {
            ShowMasterPageMessage("Ok", "Success", successMsg);
            rgPriority.Rebind();
        }
    }

    // Shared MERGE engine — expects DataTable with columns: WhsCode, CategoryCode, Priority
    private bool MergePriorities(DataTable dt, out string message)
    {
        string mergeSql = @"
            MERGE dbo.Repln_Location_Priority AS tgt
            USING (SELECT @company AS Company, @location AS [Location],
                          @dept AS Dept, @priority AS Priority, @branch AS Branch) AS src
                ON  tgt.Company    = src.Company
                AND tgt.[Location] = src.[Location]
                AND tgt.Dept       = src.Dept
            WHEN MATCHED THEN
                UPDATE SET tgt.Priority = src.Priority
            WHEN NOT MATCHED THEN
                INSERT (Company, [Location], Dept, Priority, Branch)
                VALUES (src.Company, src.[Location], src.Dept, src.Priority, src.Branch);";

        db.Connect();
        using (SqlTransaction tx = db.Conn.BeginTransaction())
        {
            try
            {
                foreach (DataRow dr in dt.Rows)
                {
                    using (SqlCommand cmd = new SqlCommand(mergeSql, db.Conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@company",  sap_db);
                        cmd.Parameters.AddWithValue("@location", dr["WhsCode"].ToString());
                        cmd.Parameters.AddWithValue("@dept",     Convert.ToInt32(dr["CategoryCode"]));
                        cmd.Parameters.AddWithValue("@priority", Convert.ToInt32(dr["Priority"]));
                        cmd.Parameters.AddWithValue("@branch",   BranchId);
                        cmd.ExecuteNonQuery();
                    }
                }
                tx.Commit();
                message = dt.Rows.Count + " priorities saved successfully.";
                return true;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                message = ex.Message;
                ShowMasterPageMessage("Error", "Failed to save priorities", ex.Message);
                return false;
            }
            finally { db.Disconnect(); }
        }
    }

    // ── Export ───────────────────────────────────────────────────────────────
    protected void btnExport_Click(object sender, EventArgs e)
    {
        string sql = @"SELECT b.ItmsGrpCod AS CategoryCode,
                              b.ItmsGrpNam AS CategoryName,
                              w.WhsCode,
                              w.WhsName,
                              ISNULL(p.[Priority], 99) AS Priority
                       FROM " + sap_db + @".dbo.oitb b
                       CROSS JOIN " + sap_db + @".dbo.OWHS w " + Queries.WITH_NOLOCK + @"
                       LEFT JOIN dbo.Repln_Location_Priority p " + Queries.WITH_NOLOCK + @"
                           ON  p.Company    = @company
                           AND p.Dept       = b.ItmsGrpCod
                           AND p.[Location] = w.WhsCode
                           AND p.Branch     = @branch
                       WHERE w.BPLId = @branch
                       ORDER BY b.ItmsGrpNam, ISNULL(p.[Priority], 99), w.WhsCode";

        DataTable dt = new DataTable();
        db.Connect();
        try
        {
            using (SqlDataAdapter a = new SqlDataAdapter(sql, db.Conn))
            {
                a.SelectCommand.Parameters.AddWithValue("@company", sap_db);
                a.SelectCommand.Parameters.AddWithValue("@branch",  BranchId);
                a.Fill(dt);
            }
        }
        finally { db.Disconnect(); }

        Response.Clear();
        Response.ContentType = "text/csv";
        Response.AddHeader("Content-Disposition", "attachment; filename=FillPriority.csv");
        Response.Write("CategoryCode,CategoryName,POSCode,WhsName,Priority\r\n");
        foreach (DataRow row in dt.Rows)
        {
            Response.Write(string.Format("{0},\"{1}\",{2},\"{3}\",{4}\r\n",
                row["CategoryCode"],
                row["CategoryName"].ToString().Replace("\"", "\"\""),
                row["U_POSCode"],
                row["WhsName"].ToString().Replace("\"", "\"\""),
                row["Priority"]));
        }
        Response.End();
    }

    // ── Import preview ───────────────────────────────────────────────────────
    protected void btnPreview_Click(object sender, EventArgs e)
    {
        Session.Remove("FillPriorityImport");
        btnImport.Enabled = false;

        if (!fuExcel.HasFile)
        {
            lblImportMsg.Text = "Please select a file.";
            divImportGrid.Style["display"] = "block";
            return;
        }

        string ext = Path.GetExtension(fuExcel.FileName).ToLower();
        if (ext != ".xlsx" && ext != ".xls")
        {
            lblImportMsg.Text = "Only .xlsx or .xls files are accepted.";
            divImportGrid.Style["display"] = "block";
            return;
        }

        string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ext);
        fuExcel.SaveAs(tempPath);

        DataTable dtRaw;
        try   { dtRaw = ReadExcel(tempPath, ext); }
        finally { try { File.Delete(tempPath); } catch { } }

        DataTable dtPreview = ValidateImportData(dtRaw);

        gvImport.DataSource = dtPreview;
        gvImport.DataBind();

        // Highlight error rows
        for (int i = 0; i < gvImport.Rows.Count; i++)
        {
            if (dtPreview.Rows[i]["Status"].ToString() != "OK")
                gvImport.Rows[i].BackColor = Color.LightCoral;
        }

        divImportGrid.Style["display"] = "block";

        bool hasErrors = dtPreview.AsEnumerable().Any(r => r["Status"].ToString() != "OK");
        if (hasErrors)
        {
            lblImportMsg.Text = "Fix the errors highlighted in red before importing.";
        }
        else
        {
            lblImportMsg.Text     = dtPreview.Rows.Count + " row(s) ready to import.";
            btnImport.Enabled     = true;
            Session["FillPriorityImport"] = dtPreview;
        }
    }

    private DataTable ReadExcel(string path, string ext)
    {
        string props  = ext == ".xlsx" ? "Excel 12.0 Xml;HDR=YES" : "Excel 8.0;HDR=YES";
        string conn   = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Extended Properties='" + props + "'";
        DataTable dt  = new DataTable();
        using (OleDbConnection oleConn = new OleDbConnection(conn))
        {
            oleConn.Open();
            DataTable sheets = oleConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
            string sheet = sheets.Rows[0]["TABLE_NAME"].ToString();
            using (OleDbDataAdapter a = new OleDbDataAdapter("SELECT * FROM [" + sheet + "]", oleConn))
                a.Fill(dt);
        }
        return dt;
    }

    private DataTable ValidateImportData(DataTable dtRaw)
    {
        // Load reference data for validation
        var validCategories = new Dictionary<int, string>();             // ItmsGrpCod → name
        var posToWhs        = new Dictionary<string, string>(            // U_POSCode → WhsCode
                                  StringComparer.OrdinalIgnoreCase);

        db.Connect();
        try
        {
            using (SqlDataAdapter a = new SqlDataAdapter(
                "SELECT ItmsGrpCod, ItmsGrpNam FROM " + sap_db + ".dbo.oitb", db.Conn))
            {
                DataTable dt = new DataTable();
                a.Fill(dt);
                foreach (DataRow r in dt.Rows)
                    validCategories[Convert.ToInt32(r["ItmsGrpCod"])] = r["ItmsGrpNam"].ToString();
            }
            using (SqlDataAdapter a = new SqlDataAdapter(
                "SELECT WhsCode, ISNULL(U_POSCode,'') U_POSCode FROM " + sap_db + ".dbo.OWHS WHERE BPLId = @branch", db.Conn))
            {
                a.SelectCommand.Parameters.AddWithValue("@branch", BranchId);
                DataTable dt = new DataTable();
                a.Fill(dt);
                foreach (DataRow r in dt.Rows)
                {
                    string pos = r["U_POSCode"].ToString().Trim();
                    if (!string.IsNullOrEmpty(pos))
                        posToWhs[pos] = r["WhsCode"].ToString();
                }
            }
        }
        finally { db.Disconnect(); }

        DataTable preview = new DataTable();
        preview.Columns.Add("CategoryCode", typeof(string));
        preview.Columns.Add("CategoryName", typeof(string));
        preview.Columns.Add("POSCode",      typeof(string));
        preview.Columns.Add("Priority",     typeof(string));
        preview.Columns.Add("Status",       typeof(string));
        // internal column — not shown in grid, used by import
        preview.Columns.Add("WhsCode",      typeof(string));

        // Track used priorities (< 99) per category to detect duplicates
        var usedPriorities = new Dictionary<int, HashSet<int>>();

        // Find columns case-insensitively; accept alternate names from user-created sheets
        string catColName = null, posColName = null, priColName = null;
        foreach (DataColumn col in dtRaw.Columns)
        {
            string n = col.ColumnName.ToLower().Replace(" ", "").Replace("_", "");
            if (n == "categorycode" || n == "category" || n == "catcode" || n == "cat")
                catColName = col.ColumnName;
            else if (n == "poscode" || n == "pos" || n == "positioncode")
                posColName = col.ColumnName;
            else if (n == "priority" || n == "prio")
                priColName = col.ColumnName;
        }
        bool hasCatCol = catColName != null;
        bool hasPosCol = posColName != null;
        bool hasPriCol = priColName != null;

        foreach (DataRow raw in dtRaw.Rows)
        {
            string catStr  = hasCatCol && raw[catColName] != DBNull.Value ? raw[catColName].ToString().Trim() : "";
            string posCode = hasPosCol && raw[posColName] != DBNull.Value ? raw[posColName].ToString().Trim() : "";
            string priStr  = hasPriCol && raw[priColName] != DBNull.Value ? raw[priColName].ToString().Trim() : "";
            string error   = null;
            string whsCode = "";

            // Validate CategoryCode
            int catCode = 0;
            if (!int.TryParse(catStr, out catCode))
                error = "Invalid CategoryCode";
            else if (!validCategories.ContainsKey(catCode))
                error = "CategoryCode not found";

            // Resolve POSCode → WhsCode
            if (string.IsNullOrEmpty(posCode))
                error = error ?? "POSCode is empty";
            else if (!posToWhs.TryGetValue(posCode, out whsCode))
                error = error ?? "POSCode not found in branch";

            // Validate Priority
            int priority = 0;
            if (!int.TryParse(priStr, out priority) || priority < 1 || priority > 99)
                error = error ?? "Priority must be 1-99";

            // Duplicate priority check (< 99) within same category
            if (error == null && priority < 99)
            {
                if (!usedPriorities.ContainsKey(catCode))
                    usedPriorities[catCode] = new HashSet<int>();
                if (!usedPriorities[catCode].Add(priority))
                    error = "Duplicate priority " + priority + " for this category";
            }

            string catName = validCategories.ContainsKey(catCode) ? validCategories[catCode] : "";
            preview.Rows.Add(catStr, catName, posCode, priStr, error ?? "OK", whsCode);
        }

        return preview;
    }

    // ── Import save ──────────────────────────────────────────────────────────
    protected void btnImport_Click(object sender, EventArgs e)
    {
        DataTable dtPreview = Session["FillPriorityImport"] as DataTable;
        if (dtPreview == null || dtPreview.Rows.Count == 0)
        {
            ShowMasterPageMessage("Error", "Error", "No data to import. Please preview first.");
            return;
        }

        // Map preview columns to the names MergePriorities expects
        DataTable dtMerge = new DataTable();
        dtMerge.Columns.Add("WhsCode",      typeof(string));
        dtMerge.Columns.Add("CategoryCode", typeof(int));
        dtMerge.Columns.Add("Priority",     typeof(int));
        foreach (DataRow r in dtPreview.Rows)
            dtMerge.Rows.Add(r["WhsCode"].ToString(), Convert.ToInt32(r["CategoryCode"]), Convert.ToInt32(r["Priority"]));

        string msg;
        if (MergePriorities(dtMerge, out msg))
        {
            Session.Remove("FillPriorityImport");
            btnImport.Enabled = false;
            ShowMasterPageMessage("Ok", "Success", msg);
            if (rcbCategory.SelectedValue != "")
                rgPriority.Rebind();
        }
    }
}
