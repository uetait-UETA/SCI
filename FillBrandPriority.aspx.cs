using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telerik.Web.UI;

public partial class FillBrandPriority : BasePage
{
    protected SqlDb  db       = new SqlDb();
    protected string lCurUser;
    protected string sap_db;

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
        db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, "FillBrandPriority.aspx", ref strAccessType, ref strRole_Description);
        db.Disconnect();

        if (strAccessType == "N")
        {
            string msg = "User " + lCurUser + ", with Role " + strRole_Description +
                         " does not have permissions to access this screen.";
            ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "alert",
                "{ alert('" + msg + "'); window.location = 'Default.aspx'; }", true);
            return;
        }

        if (strAccessType == "R")
        {
            rbtnSave.Enabled    = false;
            rbtnSave.ForeColor  = Color.Silver;
            rbtnAdd.Enabled     = false;
            rbtnAdd.ForeColor   = Color.Silver;
            labelForm.InnerText = "Brand Replenishment Priority (Read-Only Access)";
        }
        else if (strAccessType == "F")
        {
            rbtnSave.Enabled    = true;
            labelForm.InnerText = "Brand Replenishment Priority (Full Access)";
        }

        if (!IsPostBack)
        {
            try   { LoadLocations(); }
            catch (Exception ex) { ShowMsg("Error", "Failed in Page_Load", ex.Message); }
        }
        else if (!string.IsNullOrEmpty(rcbLocation.SelectedValue))
        {
            // Items.Clear() inside LoadAvailableBrands wipes the postback-restored Text/SelectedValue.
            // Save the brand text first, then restore it after DataBind so Telerik re-matches the item.
            string savedBrand = rcbBrand.Text;
            try { LoadAvailableBrands(); } catch { }
            if (!string.IsNullOrEmpty(savedBrand))
                rcbBrand.Text = savedBrand;
        }
    }

    private void ShowMsg(string type, string title, string message)
    {
        try { ((SiteMaster)this.Master).ShowDivMessage(type, title, message); }
        catch { }
    }

    // ── Data load ─────────────────────────────────────────────────────────────

    private void LoadLocations()
    {
        string sql = @"SELECT w.WhsCode,
                              ISNULL(CONVERT(nvarchar(30), w.U_POSCode), '') + ' - ' + w.WhsCode + ' - ' + w.WhsName AS DisplayName
                       FROM " + sap_db + @".dbo.OWHS w " + Queries.WITH_NOLOCK + @"
                       WHERE w.BPLId = @branch
                         AND ISNULL(w.Block, '') <> 'R'
                         AND ISNULL(w.U_POSCode, '') <> ''
                       ORDER BY w.U_POSCode, w.WhsCode";

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

        rcbLocation.DataTextField  = "DisplayName";
        rcbLocation.DataValueField = "WhsCode";
        rcbLocation.DataSource     = dt;
        rcbLocation.DataBind();
    }

    // Loads brands from OITM that are NOT yet configured for the selected location.
    private void LoadAvailableBrands()
    {
        string sql = @"SELECT DISTINCT d.U_Brand
                       FROM " + sap_db + @".dbo.OITM d " + Queries.WITH_NOLOCK + @"
                       WHERE d.U_Brand IS NOT NULL AND d.U_Brand <> ''
                         AND NOT EXISTS (
                             SELECT 1 FROM dbo.Repln_Brand_Priority bp " + Queries.WITH_NOLOCK + @"
                             WHERE bp.Company  = @cid
                               AND bp.Location = @loc
                               AND bp.Brand COLLATE DATABASE_DEFAULT
                                 = d.U_Brand   COLLATE DATABASE_DEFAULT)
                       ORDER BY d.U_Brand";

        DataTable dt = new DataTable();
        db.Connect();
        try
        {
            using (SqlDataAdapter a = new SqlDataAdapter(sql, db.Conn))
            {
                a.SelectCommand.Parameters.AddWithValue("@cid", sap_db);
                a.SelectCommand.Parameters.AddWithValue("@loc", rcbLocation.SelectedValue);
                a.Fill(dt);
            }
        }
        finally { db.Disconnect(); }

        rcbBrand.Items.Clear();
        rcbBrand.DataTextField  = "U_Brand";
        rcbBrand.DataValueField = "U_Brand";
        rcbBrand.DataSource     = dt;
        rcbBrand.DataBind();
    }

    private DataTable GetBrandPriorities()
    {
        string sql = @"SELECT Brand, [Priority]
                       FROM dbo.Repln_Brand_Priority " + Queries.WITH_NOLOCK + @"
                       WHERE Company = @cid AND Location = @loc
                       ORDER BY [Priority], Brand";

        DataTable dt = new DataTable();
        db.Connect();
        try
        {
            using (SqlDataAdapter a = new SqlDataAdapter(sql, db.Conn))
            {
                a.SelectCommand.Parameters.AddWithValue("@cid", sap_db);
                a.SelectCommand.Parameters.AddWithValue("@loc", rcbLocation.SelectedValue);
                a.Fill(dt);
            }
        }
        finally { db.Disconnect(); }
        return dt;
    }

    // ── Grid events ───────────────────────────────────────────────────────────

    protected void rcbLocation_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        if (rcbLocation.SelectedValue == "") return;
        LoadAvailableBrands();
        rgBrands.Rebind();
    }

    protected void rgBrands_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        if (rcbLocation.SelectedValue == "") return;
        divGrid.Attributes.Add("style", "display:block");
        divAddBrand.Attributes.Add("style", "display:block; margin-top:12px;");
        rgBrands.DataSource = GetBrandPriorities();
    }

    protected void rgBrands_ItemCommand(object sender, GridCommandEventArgs e)
    {
        if (e.CommandName != "DeleteBrand") return;

        GridDataItem item = (GridDataItem)e.Item;
        string brand = item["Brand"].Text;

        db.Connect();
        try
        {
            using (SqlCommand cmd = new SqlCommand(
                "DELETE FROM dbo.Repln_Brand_Priority " +
                "WHERE Company=@cid AND Location=@loc AND Brand=@brand", db.Conn))
            {
                cmd.Parameters.AddWithValue("@cid",   sap_db);
                cmd.Parameters.AddWithValue("@loc",   rcbLocation.SelectedValue);
                cmd.Parameters.AddWithValue("@brand", brand);
                cmd.ExecuteNonQuery();
            }
        }
        finally { db.Disconnect(); }

        LoadAvailableBrands();
        rgBrands.Rebind();
    }

    // ── Save (inline priority edit) ───────────────────────────────────────────

    protected void rbtnSave_Click(object sender, EventArgs e)
    {
        if (rcbLocation.SelectedValue == "")
        {
            ShowMsg("Error", "Error", "Please select a location.");
            return;
        }
        if (rgBrands.MasterTableView.Items.Count == 0)
        {
            ShowMsg("Error", "Error", "No brands to save. Add at least one brand first.");
            return;
        }

        var rows = new List<KeyValuePair<string, int>>();
        foreach (GridDataItem item in rgBrands.MasterTableView.Items)
        {
            string brand = item["Brand"].Text;
            TextBox txt  = (TextBox)item["Priority"].FindControl("txtPriority");
            int priority;
            if (!int.TryParse(txt.Text.Trim(), out priority) || priority < 1 || priority > 99)
            {
                ShowMsg("Error", "Invalid priority",
                    "Priority for '" + brand + "' must be a number between 1 and 99.");
                return;
            }
            rows.Add(new KeyValuePair<string, int>(brand, priority));
        }

        string mergeSql = @"
            MERGE dbo.Repln_Brand_Priority AS tgt
            USING (SELECT @cid AS Company, @loc AS Location,
                          @brand AS Brand, @priority AS Priority, @branch AS Branch) AS src
                ON tgt.Company = src.Company AND tgt.Location = src.Location AND tgt.Brand = src.Brand
            WHEN MATCHED THEN
                UPDATE SET tgt.Priority = src.Priority
            WHEN NOT MATCHED THEN
                INSERT (Company, Location, Brand, Priority, Branch)
                VALUES (src.Company, src.Location, src.Brand, src.Priority, src.Branch);";

        db.Connect();
        using (SqlTransaction tx = db.Conn.BeginTransaction())
        {
            try
            {
                foreach (var kv in rows)
                {
                    using (SqlCommand cmd = new SqlCommand(mergeSql, db.Conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@cid",      sap_db);
                        cmd.Parameters.AddWithValue("@loc",      rcbLocation.SelectedValue);
                        cmd.Parameters.AddWithValue("@brand",    kv.Key);
                        cmd.Parameters.AddWithValue("@priority", kv.Value);
                        cmd.Parameters.AddWithValue("@branch",   BranchId);
                        cmd.ExecuteNonQuery();
                    }
                }
                tx.Commit();
                ShowMsg("Ok", "Success", rows.Count + " brand priorities saved successfully.");
                rgBrands.Rebind();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                ShowMsg("Error", "Failed to save priorities", ex.Message);
            }
            finally { db.Disconnect(); }
        }
    }

    // ── Add brand ─────────────────────────────────────────────────────────────

    protected void rbtnAdd_Click(object sender, EventArgs e)
    {
        if (rcbLocation.SelectedValue == "")
        {
            ShowMsg("Error", "Error", "Please select a location first.");
            return;
        }
        string brandValue = !string.IsNullOrEmpty(rcbBrand.SelectedValue)
            ? rcbBrand.SelectedValue
            : rcbBrand.Text.Trim();
        if (string.IsNullOrEmpty(brandValue))
        {
            ShowMsg("Error", "Error", "Please select a brand to add.");
            return;
        }

        int priority;
        if (!int.TryParse(txtNewPriority.Text.Trim(), out priority) || priority < 1 || priority > 99)
        {
            ShowMsg("Error", "Invalid priority", "Priority must be a number between 1 and 99.");
            return;
        }

        db.Connect();
        try
        {
            using (SqlCommand cmd = new SqlCommand(@"
                MERGE dbo.Repln_Brand_Priority AS tgt
                USING (SELECT @cid AS Company, @loc AS Location,
                              @brand AS Brand, @priority AS Priority, @branch AS Branch) AS src
                    ON tgt.Company = src.Company AND tgt.Location = src.Location AND tgt.Brand = src.Brand
                WHEN MATCHED THEN
                    UPDATE SET tgt.Priority = src.Priority, tgt.Branch = src.Branch
                WHEN NOT MATCHED THEN
                    INSERT (Company, Location, Brand, Priority, Branch)
                    VALUES (src.Company, src.Location, src.Brand, src.Priority, src.Branch);",
                db.Conn))
            {
                cmd.Parameters.AddWithValue("@cid",      sap_db);
                cmd.Parameters.AddWithValue("@loc",      rcbLocation.SelectedValue);
                cmd.Parameters.AddWithValue("@brand",    brandValue);
                cmd.Parameters.AddWithValue("@priority", priority);
                cmd.Parameters.AddWithValue("@branch",   BranchId);
                cmd.ExecuteNonQuery();
            }
        }
        finally { db.Disconnect(); }

        txtNewPriority.Text = "99";
        LoadAvailableBrands();
        rgBrands.Rebind();
    }
}
