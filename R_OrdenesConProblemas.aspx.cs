using System;
using System.Data;
using Telerik.Web.UI;

public partial class R_OrdenesConProblemas : BasePage
{
    public DataTable dt = new DataTable();
    //public DataManager dm = new DataManager();
    public SqlDb dm = new SqlDb();

    protected void Page_Load(object sender, EventArgs e)
    {
        ValidaSesionNullOrEmpty(new string[] { "UserId", "CompanyId" });

        try
        {
            if (!IsPostBack)
            {
                hfDate.Value = "-";
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed in Page_Load", ex.Message.ToString());
            return;
        }
    }

    private void GoToLogin()
    {
        Response.Redirect("Login1.aspx");
    }

    private void ValidaSesionNullOrEmpty(string[] keyNames)
    {
        bool r = false;
        foreach (string keyName in keyNames)
        {
            if (Session[keyName] == null || (string)Session[keyName] == "")
            {
                r = true;
                break;
            }
        }

        if (r)
        {
            GoToLogin();
        }
    }

    private bool ValidaSesionRptDataNull(string keyName, bool goToLogin)
    {
        bool r = false;
        if (Session[keyName] == null || (DataTable)Session[keyName] == null)
        {
            r = true;

            if (goToLogin)
            {
                GoToLogin();
            }
        }

        return r;
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

    private DataTable GetOrdenesConProblemas(DateTime? v_Period, string sap_db)
    {
        try
        {
            dt = dm.GetOrdenesConProblemas(v_Period, sap_db);
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
        }

        return dt;
    }

    protected void rbtnView_Click(object sender, EventArgs e)
    {
        if (hfDate.Value != (rdpDate.SelectedDate.HasValue && rdpDate.SelectedDate.Value != null ? rdpDate.SelectedDate.Value.ToString("yyyy-MM-dd") : ""))
        {
            hfDate.Value = (rdpDate.SelectedDate.HasValue && rdpDate.SelectedDate.Value != null ? rdpDate.SelectedDate.Value.ToString("yyyy-MM-dd") : "");
            Session["dtOCP"] = null;
            Session["dtDetail"] = null;
        }

        rgHead.Rebind();
    }

    protected void rgHead_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        ValidaSesionNullOrEmpty(new string[] { "UserId", "CompanyId" });

        try
        {
            DataTable dtOrdenes = new DataTable();

            if (Session["dtOCP"] == null)
            {
                Session["dtOCP"] = GetOrdenesConProblemas(rdpDate.SelectedDate, Session["CompanyId"].ToString());
            }

            dtOrdenes = Session["dtOCP"] as DataTable;

            if (dtOrdenes.Columns[0].ColumnName.ToString() == "ErrMsg")
            {
                ShowMasterPageMessage("Error", "Failed to load data", dt.Rows[0]["ErrMsg"].ToString());
            }
            else
            {
                rgHead.DataSource = dtOrdenes;
                divHeading.Visible = true;
                rgHead.Visible = true;
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
            return;
        }
    }

    //protected void rgHead_ExcelMLExportRowCreated(object sender, Telerik.Web.UI.GridExcelBuilder.GridExportExcelMLRowCreatedArgs e)
    //{
    //    try
    //    {
    //        //e.Row.Cells.GetCellByName("Avance").StyleValue = "pctStyle";
    //        //e.Row.Cells.GetCellByName("PesoTotal").StyleValue = "numberStyle";
    //        //e.Row.Cells.GetCellByName("CUBE_CRTN").StyleValue = "numberStyle";
    //        //e.Row.Cells.GetCellByName("CantidaddeUnidades").StyleValue = "numberStyle";
    //        //e.Row.Cells.GetCellByName("LineTotal").StyleValue = "currencyStyle";
    //    }
    //    catch (Exception ex)
    //    {
    //        ShowMasterPageMessage("Error", "Failed when export row created", ex.Message.ToString());
    //        return;
    //    }
    //}

    //protected void rgHead_ExcelMLExportStylesCreated(object sender, Telerik.Web.UI.GridExcelBuilder.GridExportExcelMLStyleCreatedArgs e)
    //{
    //    try
    //    {
    //        //StyleElement pctStyle = new StyleElement("pctStyle");
    //        //pctStyle.NumberFormat.FormatType = NumberFormatType.Percent;
    //        ////pctStyle.FontStyle.Bold = true;
    //        //e.Styles.Add(pctStyle);

    //        //StyleElement numberStyle = new StyleElement("numberStyle");
    //        //numberStyle.NumberFormat.FormatType = NumberFormatType.General;
    //        //e.Styles.Add(numberStyle);

    //        //StyleElement currencyStyle = new StyleElement("currencyStyle");
    //        //currencyStyle.NumberFormat.FormatType = NumberFormatType.Currency;
    //        //e.Styles.Add(currencyStyle);

    //        //StyleElement headerStyle = new StyleElement("headerStyle");
    //        //headerStyle.NumberFormat.FormatType = NumberFormatType.Currency;
    //        //e.Styles.Add(headerStyle);
    //    }
    //    catch (Exception ex)
    //    {
    //        ShowMasterPageMessage("Error", "Failed when export style created", ex.Message.ToString());
    //        return;
    //    }
    //}

    protected void rgHead_ItemCommand(object sender, GridCommandEventArgs e)
    {
        //rgHead.MasterTableView.Caption = "Corte de Ordenes de Ventas y Etiquetado - TOCUMEN";
        if (e.CommandName == RadGrid.ExportToExcelCommandName)
        {
            foreach (GridDataItem item in rgHead.MasterTableView.Items)
            {
                item.Expanded = true;
            }

            rgHead.ExportSettings.IgnorePaging = true;
            rgHead.ExportSettings.ExportOnlyData = true;
            rgHead.ExportSettings.OpenInNewWindow = true;
            rgHead.MasterTableView.UseAllDataFields = false;
            rgHead.ExportSettings.Excel.Format = GridExcelExportFormat.ExcelML;

            rgHead.MasterTableView.HierarchyDefaultExpanded = false;

            rgHead.MasterTableView.HierarchyLoadMode = GridChildLoadMode.Client;

            rgHead.MasterTableView.ExportToExcel();
        }

        if (e.CommandName == RadGrid.RebindGridCommandName)
        {
            Session["dtOCP"] = null;
            Session["dtDetail"] = null;
            hfDate.Value = "-";
        }
    }
}