using System;
using System.Linq;
using System.Data;
using Telerik.Web.UI;

public partial class R_ReciboBodegaSAPAPRIvsGRPO : BasePage
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
            dt = dm.GetReciboBodegaSAPAPRIvsGPRO(rtbOrden.Text, rcbCorte.SelectedValue);
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
            Session["dtRBSAG"] = null;
            Session["dtRBSAGDetail"] = null;
        }
        rgHead.Rebind();
    }

    protected void rcbCorte_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        Session["dtRBSAG"] = null;
        Session["dtRBSAGDetail"] = null;

        rgHead.Visible = false;
        divHeading.Visible = false;
    }

    protected void rgHead_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        try
        {
            if (rcbCorte.SelectedValue != "")
            {
                DataTable dtOrdenes = new DataTable();
                if (Session["dtRBSAG"] == null)
                {
                    Session["dtRBSAG"] = GetData();
                }
                dtOrdenes = Session["dtRBSAG"] as DataTable;

                if (dtOrdenes.Columns[0].ColumnName.ToString() == "ErrMsg")
                {
                    ShowMasterPageMessage("Error", "Failed to load data", dt.Rows[0]["ErrMsg"].ToString());
                }
                else
                {
                    DataTable dtHeader = new DataTable();
                    dtHeader.Columns.Add("APRIDocNum", typeof(string));
                    dtHeader.Columns.Add("OrderNum", typeof(string));
                    dtHeader.Columns.Add("GRPO_1_DocNum", typeof(string));
                    dtHeader.Columns.Add("GRPO_2_DocNum", typeof(string));
                    dtHeader.Columns.Add("ItemCount", typeof(double));
                    dtHeader.Columns.Add("APRIQtyUnits", typeof(double));
                    dtHeader.Columns.Add("RECIBOQtyUnits", typeof(double));
                    dtHeader.Columns.Add("GRPO_1_TotalQtyUnits", typeof(double));
                    dtHeader.Columns.Add("GRPO_2_TotalQtyUnits", typeof(double));
                    dtHeader.Columns.Add("Dif", typeof(double));

                    var v_Query = from rows in dtOrdenes.AsEnumerable()
                                  group rows by new
                                  {
                                      APRIDocNum = rows["APRIDocNum"],
                                      OrderNum = rows["OrderNum"],
                                      GRPO_1_DocNum = rows["GRPO_1_DocNum"] == null ? "-1" : rows["GRPO_1_DocNum"],
                                      GRPO_2_DocNum = rows["GRPO_2_DocNum"] == null ? "-1" : rows["GRPO_2_DocNum"]
                                  } into grp
                                  orderby grp.Key.APRIDocNum
                                  select new
                                  {
                                      APRIDocNum = grp.Key.APRIDocNum,
                                      OrderNum = grp.Key.OrderNum,
                                      GRPO_1_DocNum = grp.Key.GRPO_1_DocNum,
                                      GRPO_2_DocNum = grp.Key.GRPO_2_DocNum,
                                      ItemCount = grp.Select(r => r.Field<string>("APRIItemCode")).Count(),
                                      APRIQtyUnits = grp.Select(r => r.Field<decimal?>("APRIQtyUnits")).Sum(),
                                      RECIBOQtyUnits = grp.Select(r => r.Field<decimal?>("RECIBOQtyUnits")).Sum(),
                                      GRPO_1_TotalQtyUnits = grp.Select(r => r.Field<decimal?>("GRPO_1_TotalQtyUnits")).Sum(),
                                      GRPO_2_TotalQtyUnits = grp.Select(r => r.Field<decimal?>("GRPO_2_TotalQtyUnits")).Sum(),
                                      Dif = grp.Select(r => r.Field<decimal?>("Dif")).Sum()
                                  };

                    foreach (var item in v_Query)
                    {
                        DataRow dr = dtHeader.NewRow();
                        dr["APRIDocNum"] = item.APRIDocNum;
                        dr["OrderNum"] = item.OrderNum;
                        dr["GRPO_1_DocNum"] = item.GRPO_1_DocNum;
                        dr["GRPO_2_DocNum"] = item.GRPO_2_DocNum;
                        dr["ItemCount"] = item.ItemCount;
                        dr["APRIQtyUnits"] = item.APRIQtyUnits;
                        dr["RECIBOQtyUnits"] = item.RECIBOQtyUnits;
                        dr["GRPO_1_TotalQtyUnits"] = item.GRPO_1_TotalQtyUnits;
                        dr["GRPO_2_TotalQtyUnits"] = item.GRPO_2_TotalQtyUnits;
                        dr["Dif"] = item.Dif;
                        dtHeader.Rows.Add(dr);
                    }

                    rgHead.DataSource = dtHeader;
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

    protected void rgHead_DetailTableDataBind(object sender, GridDetailTableDataBindEventArgs e)
    {
        try
        {
            GridDataItem dataItem = (GridDataItem)e.DetailTableView.ParentItem;
            string v_APRIDocNum = dataItem.GetDataKeyValue("APRIDocNum").ToString();
            DataTable dtOrdenesDetail = dm.GetReciboBodegaSAPAPRIvsGPRO(v_APRIDocNum, rcbCorte.SelectedValue);

            if (dtOrdenesDetail.Columns[0].ColumnName.ToString() == "ErrMsg")
            {
                ShowMasterPageMessage("Error", "Failed to load data", dt.Rows[0]["ErrMsg"].ToString());
            }
            else
            {
                DataTable dtDetail = new DataTable();
                dtDetail.Columns.Add("APRIDocNum", typeof(string));
                dtDetail.Columns.Add("OrderNum", typeof(string));
                dtDetail.Columns.Add("APRIItemCode", typeof(string));
                dtDetail.Columns.Add("BarCode", typeof(string));
                dtDetail.Columns.Add("APRIItemName", typeof(string));
                dtDetail.Columns.Add("APRIQtyUnits", typeof(double));
                dtDetail.Columns.Add("RECIBOQtyUnits", typeof(double));
                dtDetail.Columns.Add("GRPO_1_TotalQtyUnits", typeof(double));
                dtDetail.Columns.Add("GRPO_2_TotalQtyUnits", typeof(double));
                dtDetail.Columns.Add("Dif", typeof(double));

                var v_Query = from rows in dtOrdenesDetail.AsEnumerable()
                              where rows["APRIDocNum"].ToString() == v_APRIDocNum.ToString()
                              group rows by new
                              {
                                  APRIDocNum = rows["APRIDocNum"],
                                  OrderNum = rows["OrderNum"],
                                  APRIItemCode = rows["APRIItemCode"],
                                  BarCode = rows["BarCode"],
                                  APRIItemName = rows["APRIItemName"]
                              } into grp
                              orderby grp.Key.APRIDocNum
                              select new
                              {
                                  APRIDocNum = grp.Key.APRIDocNum,
                                  OrderNum = grp.Key.OrderNum,
                                  APRIItemCode = grp.Key.APRIItemCode,
                                  BarCode = grp.Key.BarCode,
                                  APRIItemName = grp.Key.APRIItemName,
                                  APRIQtyUnits = grp.Select(r => r.Field<decimal?>("APRIQtyUnits")).Sum(),
                                  RECIBOQtyUnits = grp.Select(r => r.Field<decimal?>("RECIBOQtyUnits")).Sum(),
                                  GRPO_1_TotalQtyUnits = grp.Select(r => r.Field<decimal?>("GRPO_1_TotalQtyUnits")).Sum(),
                                  GRPO_2_TotalQtyUnits = grp.Select(r => r.Field<decimal?>("GRPO_2_TotalQtyUnits")).Sum(),
                                  Dif = grp.Select(r => r.Field<decimal?>("Dif")).Sum()
                              };

                foreach (var item in v_Query)
                {
                    DataRow dr = dtDetail.NewRow();
                    dr["APRIDocNum"] = item.APRIDocNum;
                    dr["OrderNum"] = item.OrderNum;
                    dr["APRIItemCode"] = item.APRIItemCode;
                    dr["BarCode"] = item.BarCode;
                    dr["APRIItemName"] = item.APRIItemName;
                    dr["APRIQtyUnits"] = item.APRIQtyUnits;
                    dr["RECIBOQtyUnits"] = item.RECIBOQtyUnits;
                    dr["GRPO_1_TotalQtyUnits"] = item.GRPO_1_TotalQtyUnits;
                    dr["GRPO_2_TotalQtyUnits"] = item.GRPO_2_TotalQtyUnits;
                    dr["Dif"] = item.Dif;
                    dtDetail.Rows.Add(dr);
                }

                e.DetailTableView.DataSource = dtDetail;
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Error", ex.Message.ToString());
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
            rgHead.MasterTableView.DetailTables[0].HierarchyDefaultExpanded = false;

            rgHead.MasterTableView.HierarchyLoadMode = GridChildLoadMode.Client;
            rgHead.MasterTableView.DetailTables[0].HierarchyLoadMode = GridChildLoadMode.Client;

            

            rgHead.MasterTableView.ExportToExcel();
        }
        if (e.CommandName == RadGrid.RebindGridCommandName)
        {
            Session["dtRBSAG"] = null;
            Session["dtRBSAGDetail"] = null;
        }
    }
}