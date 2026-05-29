using System;
using System.Data;
using Telerik.Web.UI;

public partial class R_DetalledeItemsporBins : System.Web.UI.Page
{ 
    public DataTable dt = new DataTable();
    public DataManager dm = new DataManager();
    protected string sap_db;

    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            if (!IsPostBack)
            {
                if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
                {
                    Response.Redirect("Login1.aspx");
                }

                if (string.IsNullOrEmpty((string)Session["CompanyId"]))
                {
                    Response.Redirect("Login1.aspx");
                }

                sap_db = (string)Session["CompanyId"];

                if (Request.QueryString["Bin"] != null)
                {
                    rtbBin.Text = Request.QueryString["Bin"].ToString();
                    rtbOrden.Text = Request.QueryString["Orden"].ToString();
                    this.rbtnView_Click(null, null);
                    Session["NumBin"] = "";
                    Session["NumOrder"] = "";
                }
                
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed in Page_Load", ex.Message.ToString());
            return;
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

    
    private DataTable GetDetalledeItemsporBins(string v_Orden, string v_Bin, string CompanyId)
    {
        DataTable dtBar = new DataTable();
        dtBar.Columns.Add("PRODUCT", typeof(string));
        dtBar.Columns.Add("BARCODE", typeof(string));
        dtBar.Columns.Add("DESCRIPT", typeof(string));
        dtBar.Columns.Add("QTY", typeof(string));

        try
        {
            dt = dm.GetDetalledeItemsporBins(v_Orden, v_Bin, CompanyId);
             
            foreach(DataRow dr in dt.Rows)
            {
                DataRow dtBarRow = dtBar.NewRow();

                dtBarRow[0] = dr[0].ToString();
                dtBarRow[1] = dm.GetBarcodeByProduct(dr[0].ToString());
                dtBarRow[2] = dr[1].ToString();
                dtBarRow[3] = dr[2].ToString();
                //rlTest.Text = dr[0].ToString();
                dtBar.Rows.Add(dtBarRow);
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data1", ex.Message.ToString());
            //dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
        }
        return dtBar;
    }

    protected void rbtnView_Click(object sender, EventArgs e)
    {
        if (hfOrden.Value != rtbOrden.Text)
        {
            hfOrden.Value = rtbOrden.Text;
            Session["dtDDIPB"] = null;
        }
        rgHead.Rebind();
    }

    protected void rcbCorte_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        Session["dtDDIPB"] = null;

        rgHead.Visible = false;
        divHeading.Visible = false;
    }

    protected void rgHead_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        try
        {
            if (rtbOrden.Text != "")
            {
                DataTable dtOrdenes = new DataTable();
                
                if (Session["dtDDIPB"] == null)
                {
                    if ((string)Session["NumBin"] != rtbBin.Text || (string)Session["NumOrder"] != rtbOrden.Text)
                    {
                        Session["NumBin"] = rtbBin.Text;
                        Session["NumOrder"] = rtbOrden.Text;
                        Session["dtDDIPB"] = GetDetalledeItemsporBins(rtbOrden.Text, rtbBin.Text, sap_db);
                    }
                }

                dtOrdenes = Session["dtDDIPB"] as DataTable;

                if (dtOrdenes.Columns[0].ColumnName.ToString() == "ErrMsg")
                {
                    ShowMasterPageMessage("Error", "Failed to load data", dtOrdenes.Rows[0]["ErrMsg"].ToString());
                }
                else
                {
                    rgHead.DataSource = dtOrdenes;
                    divHeading.Visible = true;
                    rgHead.Visible = true;
                }
            }
            else
            {
                divHeading.Visible = false;
                rgHead.Visible = false;
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
            return;
        }
    }
    protected void rgHead_ExcelMLExportRowCreated(object sender, Telerik.Web.UI.GridExcelBuilder.GridExportExcelMLRowCreatedArgs e)
    {
        try
        {
            //e.Row.Cells.GetCellByName("Avance").StyleValue = "pctStyle";
            //e.Row.Cells.GetCellByName("PesoTotal").StyleValue = "numberStyle";
            //e.Row.Cells.GetCellByName("CUBE_CRTN").StyleValue = "numberStyle";
            //e.Row.Cells.GetCellByName("CantidaddeUnidades").StyleValue = "numberStyle";
            //e.Row.Cells.GetCellByName("LineTotal").StyleValue = "currencyStyle";
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed when export row created", ex.Message.ToString());
            return;
        }
    }

    protected void rgHead_ExcelMLExportStylesCreated(object sender, Telerik.Web.UI.GridExcelBuilder.GridExportExcelMLStyleCreatedArgs e)
    {
        try
        {
            //StyleElement pctStyle = new StyleElement("pctStyle");
            //pctStyle.NumberFormat.FormatType = NumberFormatType.Percent;
            ////pctStyle.FontStyle.Bold = true;
            //e.Styles.Add(pctStyle);

            //StyleElement numberStyle = new StyleElement("numberStyle");
            //numberStyle.NumberFormat.FormatType = NumberFormatType.General;
            //e.Styles.Add(numberStyle);

            //StyleElement currencyStyle = new StyleElement("currencyStyle");
            //currencyStyle.NumberFormat.FormatType = NumberFormatType.Currency;
            //e.Styles.Add(currencyStyle);

            //StyleElement headerStyle = new StyleElement("headerStyle");
            //headerStyle.NumberFormat.FormatType = NumberFormatType.Currency;
            //e.Styles.Add(headerStyle);
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed when export style created", ex.Message.ToString());
            return;
        }
    }
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
            Session["dtDDIPB"] = null;
            Session["NumBin"] = "";
            Session["NumOrder"] = "";
        }
    }
}