using System;
using System.Linq;
using System.Data;
using Telerik.Web.UI;

public partial class R_BinesDespTiendas : System.Web.UI.Page
{
    public DataTable dt = new DataTable();
    public DataManager dm = new DataManager();
    protected string lCurUser;
    protected string sap_db;

    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty((string)Session["UserId"]) || string.IsNullOrEmpty((string)Session["CompanyId"]))
            {
                Response.Redirect("Login1.aspx");
            }

            lCurUser = (string)Session["UserId"];
            sap_db = (string)Session["CompanyId"];

            if (!IsPostBack)
            {
                
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

    private DataTable GetBinesDespTiendas(string v_Ordenes, string v_Corte, string CompanyId)
    {
        try
        {
            dt = dm.GetBinesDespTiendas(v_Ordenes, v_Corte, CompanyId);
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
            Session["dtBDT"] = null;
            Session["dtBDTDetail"] = null;
        }
        rgHead.Rebind();
    }

    protected void rcbCorte_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        Session["dtBDT"] = null;
        Session["dtBDTDetail"] = null;

        rgHead.Visible = false;
        divHeading.Visible = false;
    }

    protected void rgHead_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        try
        {
            if (rcbCorte.SelectedValue != "")
            {
                //DataTable dtOrdenes = new DataTable();

                if (Session["dtBDT"] == null)
                {
                    Session["dtBDT"] = GetBinesDespTiendas(rtbOrden.Text.ToString(), rcbCorte.SelectedValue, sap_db);
                }

                DataTable dtOrdenes = Session["dtBDT"] as DataTable;

                if (dtOrdenes.Columns[0].ColumnName.ToString() == "ErrMsg")
                {
                    ShowMasterPageMessage("Error", "Failed to load data", dt.Rows[0]["ErrMsg"].ToString());
                }
                else
                {
                    DataTable dtHeader = new DataTable();
                    dtHeader.Columns.Add("DocNum", typeof(string));
                    dtHeader.Columns.Add("DocDate", typeof(string));
                    dtHeader.Columns.Add("FromWhsCode", typeof(string));
                    dtHeader.Columns.Add("ToWhsCode", typeof(string));
                    dtHeader.Columns.Add("DocStatus", typeof(string));
                    dtHeader.Columns.Add("docentrytrarec2", typeof(string));

                    //var v_Query = from rows in dtOrdenes.AsEnumerable()
                    //              group rows by new
                    //              {
                    //                  DocNum = rows["DocNum"],
                    //                  DocDate = rows["DocDate"],
                    //                  FromWhsCode = rows["FromWhsCode"],
                    //                  ToWhsCode = rows["ToWhsCode"],
                    //                  DocStatus = rows["DocStatus"],
                    //                  docentrytrarec2 = rows["docentrytrarec2"]
                    //                  //factDFB = rows["factDFB"] == null ? "-1" : rows["factDFB"],
                    //                  //numapri = rows["numapri"] == null ? "-1" : rows["numapri"],
                    //                  //DocNum = rows["DocNum"] == null ? "-1" : rows["DocNum"],
                    //                  //fromapp = rows["fromapp"] == null ? "-1" : rows["fromapp"]
                    //              } into grp
                    //              orderby grp.Key.DocNum
                    //              select new
                    //              {
                    //                  DocNum = grp.Key.DocNum,
                    //                  DocDate = grp.Key.DocDate,
                    //                  FromWhsCode = grp.Key.FromWhsCode,
                    //                  ToWhsCode = grp.Key.ToWhsCode,
                    //                  DocStatus = grp.Key.DocStatus,
                    //                  docentrytrarec2 = grp.Key.docentrytrarec2
                    //              };


                    var v_Query = from rows in dtOrdenes.AsEnumerable()
                                  group rows by new
                                  {
                                      DocNum = rows["DocNum"],
                                      DocDate = rows["DocDate"],
                                      FromWhsCode = rows["FromWhsCode"],
                                      ToWhsCode = rows["ToWhsCode"],
                                      DocStatus = rows["DocStatus"],
                                      docentrytrarec2 = rows["docentrytrarec2"]
                                  } into grp
                                  orderby grp.Key.DocNum
                                  select new
                                  {
                                      grp.Key.DocNum,
                                      grp.Key.DocDate,
                                      grp.Key.FromWhsCode,
                                      grp.Key.ToWhsCode,
                                      grp.Key.DocStatus,
                                      grp.Key.docentrytrarec2
                                  };

                    foreach (var item in v_Query)
                    {
                        DataRow dr = dtHeader.NewRow();
                        dr["DocNum"] = item.DocNum;
                        dr["DocDate"] = item.DocDate;
                        dr["FromWhsCode"] = item.FromWhsCode;
                        dr["ToWhsCode"] = item.ToWhsCode;
                        dr["DocStatus"] = item.DocStatus;
                        dr["docentrytrarec2"] = item.docentrytrarec2;
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
            GridDataItem dataItem = e.DetailTableView.ParentItem;
            string v_OSDocNum = dataItem.GetDataKeyValue("DocNum").ToString();

            if (Session["dtBDTDetail"] == null)
            {
                Session["dtBDTDetail"] = GetBinesDespTiendas(v_OSDocNum, rcbCorte.SelectedValue, sap_db);
                //Session["dtBDTDetail"] = dm.GetBinesDespTiendas(v_OSDocNum, rcbCorte.SelectedValue);
            }

            DataTable dtOrdenesDetail = Session["dtBDTDetail"] as DataTable;

            if (dtOrdenesDetail.Columns[0].ColumnName.ToString() == "ErrMsg")
            {
                ShowMasterPageMessage("Error", "Failed to load data", dt.Rows[0]["ErrMsg"].ToString());
            }
            else
            {
                DataTable dtDetail = new DataTable();
                dtDetail.Columns.Add("DocNum", typeof(string));
                dtDetail.Columns.Add("WmsBin", typeof(string));
                dtDetail.Columns.Add("CintilloBin1", typeof(string));
                dtDetail.Columns.Add("CintilloBin2", typeof(string));

                //var v_Query = from rows in dtOrdenesDetail.AsEnumerable()
                //              where rows["DocNum"].ToString() == v_OSDocNum.ToString()
                //              group rows by new
                //              {
                //                  DocNum = rows["DocNum"],
                //                  WmsBin = rows["WmsBin"],
                //                  CintilloBin1 = rows["CintilloBin1"],
                //                  CintilloBin2 = rows["CintilloBin2"]
                //              } into grp
                //              orderby grp.Key.WmsBin
                //              select new
                //              {
                //                  DocNum = grp.Key.DocNum,
                //                  WmsBin = grp.Key.WmsBin,
                //                  CintilloBin1 = grp.Key.CintilloBin1,
                //                  CintilloBin2 = grp.Key.CintilloBin2
                //              };

                var v_Query = from rows in dtOrdenesDetail.AsEnumerable()
                              where rows["DocNum"].ToString() == v_OSDocNum.ToString()
                              group rows by new
                              {
                                  DocNum = rows["DocNum"],
                                  WmsBin = rows["WmsBin"],
                                  CintilloBin1 = rows["CintilloBin1"],
                                  CintilloBin2 = rows["CintilloBin2"]
                              } into grp
                              orderby grp.Key.WmsBin
                              select new
                              {
                                  grp.Key.DocNum,
                                  grp.Key.WmsBin,
                                  grp.Key.CintilloBin1,
                                  grp.Key.CintilloBin2
                              };

                foreach (var item in v_Query)
                {
                    DataRow dr = dtDetail.NewRow();
                    dr["DocNum"] = item.DocNum;
                    dr["WmsBin"] = item.WmsBin;
                    dr["CintilloBin1"] = item.CintilloBin1;
                    dr["CintilloBin2"] = item.CintilloBin2;
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
            Session["dtBDT"] = null;
            Session["dtBDTDetail"] = null;
        }
    }
}