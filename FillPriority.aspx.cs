using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.OleDb;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using System.IO;
using Telerik;
using Telerik.Web.UI;
using System.Data.SqlClient;
using System.Drawing;

public partial class FillPriority : BasePage
{
    protected SqlDb db = new SqlDb();
    protected DFBUYINGdb dfbDB = new DFBUYINGdb();
    protected string lCurUser; 
    public DataTable dt = new DataTable();
    protected string sap_db;
    private int WarehouseCount
    {
        get { return ViewState["WarehouseCount"] != null ? (int)ViewState["WarehouseCount"] : 0; }
        set { ViewState["WarehouseCount"] = value; }
    }
    private bool IsNewCategory
    {
        get { return ViewState["IsNewCategory"] != null && (bool)ViewState["IsNewCategory"]; }
        set { ViewState["IsNewCategory"] = value; }
    }
    private static string TOOLTIP_TEMPLATE = @"
                <div class=""container"" style=""width:200px;margin-left:2px;"">
                    <div class=""row"" style=""margin-left:2px;"">Store: {0}</div>
                    <div class=""row"" style=""margin-left:2px;"">Active Promos: {1}</div>
                </div>";
    protected void Page_Load(object sender, EventArgs e)
    {
        if ((string)Session["UserId"] == "" || (string)Session["UserId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

        if ((string)Session["CompanyId"] == "" || (string)Session["CompanyId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

        sap_db = (string)Session["CompanyId"];

        ///////////////Begin New  Control de acceso por Roles
        lCurUser = (string)Session["UserId"];
        char flagokay = 'Y';
	    string lControlName = "FillPriority.aspx";
        string strAccessType = "";
        string strRole_Description = "";

        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, lControlName, ref strAccessType, ref strRole_Description);
        db.Disconnect();

        if (strAccessType == "N")
	    {
	        flagokay = 'N';
	        string message = "User " + lCurUser + ", with Role " + strRole_Description + " does not have permissions to access this screen.";
	        string url = string.Format("Default.aspx");
	        string script = "{ alert('";
	        script += message;
	        script += "');";
	        script += "window.location = '";
	        script += url;
	        script += "'; }";
	        ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "alert", script, true);
	    }

	    if (strAccessType == "R")
		{
		    rbtnSave.Enabled = false;
		    rbtnSave.ForeColor = Color.Silver;	    
		    labelForm.InnerText = "Fill Priority (Read-Only Access)";
		}
		
	    if (strAccessType == "F")
		{
		    rbtnSave.Enabled = true;
		    labelForm.InnerText = "Fill Priority (Full Access)";
		}		
        ///////////////End  New Control de acceso por Roles

	    if (flagokay == 'Y')
        {
		    try
		    {
		        if (!IsPostBack)
		        {
			        if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
			        {
			            Response.Redirect("Login1.aspx");
			        }
			        else
			        {
                        LoadItemGroup();
			            LoadLocations();
			        }
		        }
		    }
		    catch (Exception ex)
		    {
		        ShowMasterPageMessage("Error", "Failed in Page_Load", ex.Message.ToString());
		        return;
		    }
        }
    }
    
    private void ShowMasterPageMessage(string v_Message_Type, string v_Message_Title, string v_Message)
    {
        try
        {
            SiteMaster sm = (SiteMaster)this.Master;
            sm.ShowDivMessage(v_Message_Type, v_Message_Title, v_Message);
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed in ShowMasterPageMessage", ex.Message.ToString());
            return;
        }
    }
    private void LoadItemGroup()
    {
        DataTable dt = new DataTable();

        try
        {
            string sql =
            @"select 
               ItmsGrpCod GroupCode, 
                lower(ItmsGrpNam) GroupID,
               cast(ItmsGrpCod as varchar) + ' - ' + [dbo].[InitCap] (ItmsGrpNam) GroupName 
               --[dbo].[InitCap] (ItmsGrpNam) GroupName 
             from " + sap_db + @".dbo.oitb
             order by GroupName ";
            db.Connect();
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);

            rcbCategory.DataTextField = "GroupName";
            rcbCategory.DataValueField = "GroupCode";
            rcbCategory.DataSource = dt;
            rcbCategory.DataBind();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadItemGroup. ERROR MESSAGE : " + ex.Message);
        }
    }
    private void LoadLocations()
    {

        DataTable dt = new DataTable();

        try
        {
            //string sql = "SELECT COMPANYCODE, WHSCODE, WHSNAME, WHSCODE + ' - ' + WHSNAME WHS FROM COMPANY_WHS_VW WHERE LOWER(COMPANYCODE) = '" + sap_db.ToLower() + "'";
            //db.Connect();
            //db.adapter = new SqlDataAdapter(sql, db.Conn);
            //db.adapter.Fill(dt);

            //rcbWhs.DataTextField = "WHS";
            //rcbWhs.DataValueField = "WHSCODE";
            //rcbWhs.DataSource = dt;
            //rcbWhs.DataBind();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadItemGroup. ERROR MESSAGE : " + ex.Message);
        }
    }

    protected void rcbCategory_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        try
        {
            if (rcbCategory.SelectedValue != "")
            {
                DataTable dtExisting = GetFillPriority();
                if (dtExisting.Rows.Count == 0)
                {
                    IsNewCategory = true;
                    rgPriority.DataSource = GetWarehousesAsNewPriority();
                }
                else
                {
                    IsNewCategory = false;
                    rgPriority.DataSource = dtExisting;
                }
                rgPriority.Rebind();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function Category Index Changed. ERROR MESSAGE : " + ex.Message);
        }
    }
    private DataTable GetWarehousesAsNewPriority()
    {
        DataTable dt = new DataTable();
        try
        {
            string sql = @"SELECT b.WhsCode AS [Location], b.WhsName AS LocationName, b.U_POSCode
                           FROM " + sap_db + @".dbo.OWHS b " + Queries.WITH_NOLOCK + @"
                           INNER JOIN dbo.SMM_WHSTYPE wt " + Queries.WITH_NOLOCK + @"
                               ON wt.WHSCODE = b.WhsCode AND wt.COMPANYID = '" + sap_db + @"' AND wt.BPLID = " + BranchId + @"
                           ORDER BY b.U_POSCode, b.WhsCode";
            db.Connect();
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);

            dt.Columns.Add("Company", typeof(string));
            dt.Columns.Add("Dept", typeof(int));
            dt.Columns.Add("Priority", typeof(int));

            foreach (DataRow row in dt.Rows)
            {
                row["Company"] = sap_db;
                row["Dept"] = Convert.ToInt32(rcbCategory.SelectedValue);
                row["Priority"] = 99;  // 99 = no replenishment by default
            }

            WarehouseCount = dt.Rows.Count;
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in GetWarehousesAsNewPriority. ERROR MESSAGE: " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
        return dt;
    }
    private DataTable GetFillPriority()
    {
        DataTable dt = new DataTable();

        try
        {
            string sql = @"SELECT a.Company, a.[Location], b.WhsName AS LocationName, a.Dept, a.[Priority], b.U_POSCode
                           FROM Repln_Location_Priority a " + Queries.WITH_NOLOCK + @"
                           INNER JOIN " + sap_db + @".dbo.OWHS b " + Queries.WITH_NOLOCK + @"
                               ON a.[Location] = b.WhsCode
                           INNER JOIN dbo.SMM_WHSTYPE wt " + Queries.WITH_NOLOCK + @"
                               ON wt.WHSCODE = a.[Location] AND wt.COMPANYID = '" + sap_db + @"' AND wt.BPLID = " + BranchId + @"
                           WHERE lower(a.Company) = lower('" + sap_db + @"')
                             AND a.Dept = " + rcbCategory.SelectedValue + @"
                           ORDER BY a.Dept, a.[Priority], b.U_POSCode";
            db.Connect();
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
            WarehouseCount = dt.Rows.Count;

            //rgPriority.DataSource = dt;
            //rgPriority.Rebind();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadItemGroup. ERROR MESSAGE : " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
        return dt;
    }

    //protected void rcbCompany_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    //{
    //    if (rcbCompany.SelectedValue != "" && rcbCategory.SelectedValue != "")
    //    {
    //        rgPriority.DataSource = GetFillPriority();
    //        rgPriority.Rebind();
    //    }
    //}

    protected void rgPriority_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        if (rcbCategory.SelectedValue != "")
        {
            divGrid.Attributes.Add("style", "display:block");
            rgPriority.DataSource = IsNewCategory ? GetWarehousesAsNewPriority() : GetFillPriority();
        }
    }
    protected void rgPriority_ItemDataBound(object sender, GridItemEventArgs e)
    {
        if (e.Item is GridDataItem)
        {
            GridDataItem dataItem = e.Item as GridDataItem;
            RadComboBox rcbPriority = (RadComboBox)dataItem["Priority"].FindControl("rcbPriority");
            HiddenField hfPriority = (HiddenField)dataItem["Priority"].FindControl("hfPriority");

            rcbPriority.Items.Clear();
            for (int i = 1; i <= WarehouseCount; i++)
                rcbPriority.Items.Add(new RadComboBoxItem(i.ToString(), i.ToString()));
            rcbPriority.Items.Add(new RadComboBoxItem("99 - No reabastece", "99"));

            rcbPriority.SelectedValue = hfPriority.Value;
        }
    }
    protected void rbtnSave_Click(object sender, EventArgs e)
    {
        if (rgPriority.MasterTableView.Items.Count > 0)
        {
            DataTable dt = new DataTable();
            dt.Columns.AddRange(new DataColumn[2]{
                           new DataColumn("Location", typeof(string )),
                           new DataColumn("Priority", typeof(string))});

            foreach (GridDataItem item in rgPriority.MasterTableView.Items)
            {
                string v_Loc = item["LOCATION"].Text;
                RadComboBox rcbPriority = (RadComboBox)item["Priority"].FindControl("rcbPriority");
                string v_Priority = rcbPriority.SelectedValue;

                dt.Rows.Add(v_Loc, v_Priority);
            }

            // Priority 99 means "no replenishment" and is allowed to repeat; only check 1-98
            var duplicates = dt.AsEnumerable()
                .Where(r => r["Priority"].ToString() != "99")
                .GroupBy(r => r["Priority"])
                .Where(gr => gr.Count() > 1)
                .ToList();
            if (duplicates.Count > 0)
            {
                ShowMasterPageMessage("Error", "Failed to save priorities", "Priorities cannot be saved as there are some duplicates.");
            }
            else
            {
                if (rcbCategory.SelectedValue != "")
                {
                    db.Connect();
                    foreach (DataRow dr in dt.Rows)
                    {
                        string sql;
                        if (IsNewCategory)
                            sql = "INSERT INTO Repln_Location_Priority (Company, Location, Dept, Priority, Branch) VALUES ('" + sap_db + "', '" + dr["Location"] + "', " + rcbCategory.SelectedValue + ", " + dr["Priority"] + ", " + BranchId + ")";
                        else
                            sql = "UPDATE Repln_Location_Priority SET Priority = " + dr["Priority"] + " WHERE lower(company) = lower('" + sap_db + "') AND dept = " + rcbCategory.SelectedValue + " AND Location = '" + dr["Location"] + "'";

                        db.cmd.CommandText = sql;
                        db.cmd.CommandType = CommandType.Text;
                        db.cmd.ExecuteNonQuery();
                    }
                    db.Disconnect();

                    string successMsg = IsNewCategory ? "Category priorities created successfully." : "Priorities updated successfully.";
                    IsNewCategory = false;
                    ShowMasterPageMessage("Ok", "Success", successMsg);
                    rgPriority.Rebind();
                }
                else
                {
                    ShowMasterPageMessage("Error", "Error", "Please refresh the page and try again.");
                }
            }
        }
        else
        {
            ShowMasterPageMessage("Error", "Error", "No data to save.");
        }
    }

    // ── Import / Export ───────────────────────────────────────────────────────

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

    protected void btnExport_Click(object sender, EventArgs e)
    {
        string sql = @"SELECT b.ItmsGrpCod AS CategoryCode,
                              b.ItmsGrpNam AS CategoryName,
                              w.WhsCode,
                              w.WhsName,
                              ISNULL(w.U_POSCode, '') AS U_POSCode,
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

        DataTable dtExport = new DataTable();
        db.Connect();
        try
        {
            using (SqlDataAdapter a = new SqlDataAdapter(sql, db.Conn))
            {
                a.SelectCommand.Parameters.AddWithValue("@company", sap_db);
                a.SelectCommand.Parameters.AddWithValue("@branch",  BranchId);
                a.Fill(dtExport);
            }
        }
        finally { db.Disconnect(); }

        Response.Clear();
        Response.ContentType = "text/csv";
        Response.AddHeader("Content-Disposition", "attachment; filename=FillPriority.csv");
        Response.Write("CategoryCode,CategoryName,POSCode,WhsName,Priority\r\n");
        foreach (DataRow row in dtExport.Rows)
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
            lblImportMsg.Text             = dtPreview.Rows.Count + " row(s) ready to import.";
            btnImport.Enabled             = true;
            Session["FillPriorityImport"] = dtPreview;
        }
    }

    private DataTable ReadExcel(string path, string ext)
    {
        string props = ext == ".xlsx" ? "Excel 12.0 Xml;HDR=YES" : "Excel 8.0;HDR=YES";
        string conn  = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path +
                       ";Extended Properties='" + props + "'";
        DataTable dtExcel = new DataTable();
        using (OleDbConnection oleConn = new OleDbConnection(conn))
        {
            oleConn.Open();
            DataTable sheets = oleConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
            string sheet = sheets.Rows[0]["TABLE_NAME"].ToString();
            using (OleDbDataAdapter a = new OleDbDataAdapter("SELECT * FROM [" + sheet + "]", oleConn))
                a.Fill(dtExcel);
        }
        return dtExcel;
    }

    private DataTable ValidateImportData(DataTable dtRaw)
    {
        var validCategories = new Dictionary<int, string>();
        var posToWhs        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        db.Connect();
        try
        {
            using (SqlDataAdapter a = new SqlDataAdapter(
                "SELECT ItmsGrpCod, ItmsGrpNam FROM " + sap_db + ".dbo.oitb", db.Conn))
            {
                DataTable dtCat = new DataTable();
                a.Fill(dtCat);
                foreach (DataRow r in dtCat.Rows)
                    validCategories[Convert.ToInt32(r["ItmsGrpCod"])] = r["ItmsGrpNam"].ToString();
            }
            using (SqlDataAdapter a = new SqlDataAdapter(
                "SELECT WhsCode, ISNULL(U_POSCode,'') U_POSCode FROM " + sap_db +
                ".dbo.OWHS WHERE BPLId = @branch", db.Conn))
            {
                a.SelectCommand.Parameters.AddWithValue("@branch", BranchId);
                DataTable dtWhs = new DataTable();
                a.Fill(dtWhs);
                foreach (DataRow r in dtWhs.Rows)
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
        preview.Columns.Add("WhsCode",      typeof(string));

        var usedPriorities = new Dictionary<int, HashSet<int>>();

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

        foreach (DataRow raw in dtRaw.Rows)
        {
            string catStr  = catColName != null && raw[catColName] != DBNull.Value ? raw[catColName].ToString().Trim() : "";
            string posCode = posColName != null && raw[posColName] != DBNull.Value ? raw[posColName].ToString().Trim() : "";
            string priStr  = priColName != null && raw[priColName] != DBNull.Value ? raw[priColName].ToString().Trim() : "";
            string error   = null;
            string whsCode = "";

            int catCode = 0;
            if (!int.TryParse(catStr, out catCode))
                error = "Invalid CategoryCode";
            else if (!validCategories.ContainsKey(catCode))
                error = "CategoryCode not found";

            if (string.IsNullOrEmpty(posCode))
                error = error ?? "POSCode is empty";
            else if (!posToWhs.TryGetValue(posCode, out whsCode))
                error = error ?? "POSCode not found in branch";

            int priority = 0;
            if (!int.TryParse(priStr, out priority) || priority < 1 || priority > 99)
                error = error ?? "Priority must be 1-99";

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

    protected void btnImport_Click(object sender, EventArgs e)
    {
        DataTable dtPreview = Session["FillPriorityImport"] as DataTable;
        if (dtPreview == null || dtPreview.Rows.Count == 0)
        {
            ShowMasterPageMessage("Error", "Error", "No data to import. Please preview first.");
            return;
        }

        DataTable dtMerge = new DataTable();
        dtMerge.Columns.Add("WhsCode",      typeof(string));
        dtMerge.Columns.Add("CategoryCode", typeof(int));
        dtMerge.Columns.Add("Priority",     typeof(int));
        foreach (DataRow r in dtPreview.Rows)
            dtMerge.Rows.Add(r["WhsCode"].ToString(),
                             Convert.ToInt32(r["CategoryCode"]),
                             Convert.ToInt32(r["Priority"]));

        string msg;
        if (MergePriorities(dtMerge, out msg))
        {
            Session.Remove("FillPriorityImport");
            btnImport.Enabled = false;
            ShowMasterPageMessage("Ok", "Success", msg);
        }
    }
}