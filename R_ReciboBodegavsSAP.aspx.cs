using System;
using System.Data;
using Telerik.Web.UI;

public partial class R_ReciboBodegavsSAP : BasePage
{
    public DataTable dt = new DataTable();
    public DataManager dm = new DataManager();

    protected void Page_Load(object sender, EventArgs e)
    {
        ValidaSesionNullOrEmpty(new string[] { "UserId", "CompanyId" });

        try
        {
            if (!IsPostBack)
            {
                GetCias();
            }
            rcbCorte.SelectedValue = Session["CompanyId"].ToString();
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

    private void GetCias()
    {
        try
        {
            DataTable dtCia = dm.GetCIAs();

            rcbCorte.DataSource = dtCia;
            rcbCorte.DataValueField = "CiaId";
            rcbCorte.DataTextField = "CiaName";
            rcbCorte.DataBind();

            rcbCorte.SelectedValue = Session["CompanyId"].ToString();
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get CIA data", ex.Message.ToString());
        }
    }

    private DataTable GetData()
    {
        try
        {
            dt = dm.ReciboBodegaVSSAP(rtbOrden.Text, rcbCorte.SelectedValue);
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
        if (hfOrden.Value != rtbOrden.Text)
        {
            hfOrden.Value = rtbOrden.Text;
        }
        rgHead.Rebind();
    }

    protected void rcbCorte_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        rgHead.Visible = false;
        divHeading.Visible = false;
        divHeading1.Visible = false;
    }

    protected void rgHead_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        try
        {
            if (rtbOrden.Text != "" && rcbCorte.SelectedIndex >= 0)
            {
                DataTable dtOrdenes = new DataTable();
                dtOrdenes = GetData();

                DataTable dtData = new DataTable();
                dtData.Columns.Add("DocNum", typeof(string));
                dtData.Columns.Add("OrderNum", typeof(string));
                dtData.Columns.Add("NumAtCard", typeof(string));
                dtData.Columns.Add("ItemCode", typeof(string));
                dtData.Columns.Add("BarCode", typeof(string));
                dtData.Columns.Add("status", typeof(string));
				
				//2019-JUN-12, Modificación para v3.1, por Aldo Reina: Agregando el campo ScannedBarCode y StatusSap:
                dtData.Columns.Add("ScannedBarCode", typeof(string));
                dtData.Columns.Add("StatusSap", typeof(string));
                //2019-JUN-12, Fin Modificación para v3.1, por Aldo Reina: Agregando el campo ScannedBarCode y StatusSap

                dtData.Columns.Add("itembrand", typeof(string));
                dtData.Columns.Add("itemname", typeof(string));
                dtData.Columns.Add("CantSAP", typeof(double));
                dtData.Columns.Add("CantReciboBodega", typeof(double));
                dtData.Columns.Add("u_bot", typeof(double));
                dtData.Columns.Add("IsInOriginDoc", typeof(string));
                dtData.Columns.Add("LineNum", typeof(string));
                dtData.Columns.Add("Dif", typeof(double));

                if (dtOrdenes.Columns[0].ColumnName.ToString() == "ErrMsg")
                {
                    ShowMasterPageMessage("Error", "Failed to load data", dt.Rows[0]["ErrMsg"].ToString());
                }
                else
                {
                    foreach(DataRow dr in dtOrdenes.Rows)
                    {
                        DataRow drData = dtData.NewRow();
                        drData["DocNum"] = dr["DocNum"].ToString();
                        drData["OrderNum"] = dr["OrderNum"].ToString();
                        drData["NumAtCard"] = dr["NumAtCard"].ToString();
                        drData["ItemCode"] = dr["ItemCode"].ToString();
                        drData["BarCode"] = dr["BarCode"].ToString();

                        //2019-JUN-12, Modificación para v3.1, por Aldo Reina: Agregando el campo ScannedBarCode y StatusSap:
                        drData["ScannedBarCode"] = dr["ScannedBarCode"].ToString();
                        drData["StatusSap"] = dr["StatusSap"].ToString();
                        //2019-JUN-12, Fin Modificación para v3.1, por Aldo Reina: Agregando el campo ScannedBarCode y StatusSap

                        drData["status"] = dr["status"].ToString();
                        drData["itembrand"] = dr["itembrand"].ToString();
                        drData["itemname"] = dr["itemname"].ToString();
                        drData["CantSAP"] = dr["CantSAP"];
                        drData["CantReciboBodega"] = dr["CantReciboBodega"];
                        drData["u_bot"] = dr["u_bot"];
                        drData["IsInOriginDoc"] = dr["IsInOriginDoc"].ToString();
                        drData["LineNum"] = dr["LineNum"].ToString();
                        //drData["Dif"] = (Convert.ToInt32(dr["CantReciboBodega"]) / Convert.ToInt32(dr["u_bot"])) - Convert.ToInt32(dr["CantSAP"]);
                        drData["Dif"] = (Convert.ToDouble(dr["CantReciboBodega"])) - (Convert.ToDouble(dr["CantSAP"]));
                        dtData.Rows.Add(drData);
                    }

                    lblAPRI.Text = dtData.Rows[0]["DocNum"].ToString();
                    lblFactura.Text = dtData.Rows[0]["NumAtCard"].ToString();
                    lblStatus.Text = dtData.Rows[0]["status"].ToString();
					
					//2019-JUN-12, Modificación para v3.1, por Aldo Reina: Agregando el campo ScannedBarCode y StatusSap:
                    lblStatusSap.Text = dtData.Rows[0]["StatusSap"].ToString();
                    //2019-JUN-12, Fin Modificación para v3.1, por Aldo Reina: Agregando el campo ScannedBarCode y StatusSap

                    lblOrder.Text = dtData.Rows[0]["OrderNum"].ToString();

                    rgHead.DataSource = dtData;
                    divHeading.Visible = true;
                    divHeading1.Visible = true;
                    rgHead.Visible = true;
                }
            }
            else
            {
                divHeading.Visible = false;
                divHeading1.Visible = false;
                rgHead.Visible = false;
                ShowMasterPageMessage("Error", "Failed to Get data", "Please enter the value for the parameter '#APRI / #Orden'. The parameter cannot be blank.");
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
        }
    }
}