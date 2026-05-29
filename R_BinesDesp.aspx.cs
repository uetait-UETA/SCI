using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using Telerik;
using Telerik.Web.UI;
using Telerik.Web.UI.GridExcelBuilder;
using System.Drawing;
using System.Data.SqlClient;



public partial class R_BinesDesp : System.Web.UI.Page
{
    public DataTable dt = new DataTable();
    public DataManager dm = new DataManager();
    protected string lCurUser; 
    protected string lControlName = null;
    protected SqlDb db = new SqlDb();
    protected string sap_db;

    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty((string)Session["UserId"]) || string.IsNullOrEmpty((string)Session["CompanyId"]))
            {
                Response.Redirect("Login1.aspx");
            }

            ///////////////Begin New  Control de acceso por Roles
            lCurUser = (string)Session["UserId"];
            sap_db = (string)Session["CompanyId"];
            char flagokay = 'Y';

            lControlName = "R_BinesDesp.aspx";  //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

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
		        labelForm.InnerText = "Bins Dispatched from Colon ZL and Received in Tocumen (Read-Only Access)";
		    }
		
	        if (strAccessType == "F")
		    {
		        labelForm.InnerText = "Bins Dispatched from Colon ZL and Received in Tocumen (Full Access)";
		    }		
    ///////////////End  New Control de acceso por Roles

            if (flagokay == 'Y')
            {
                if (!IsPostBack)
                {

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

    private DataTable GetOrdenesAbiertas(string v_Ordenes, string v_Corte)
    {
        try
        {
            dt = dm.GetBinesDesp(v_Ordenes, v_Corte);
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
            Session["dtHead"] = null;
            Session["dtDetail1"] = null;
        }
        rgHead.Rebind();
    }

    protected void rcbCorte_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        Session["dtHead"] = null;
        Session["dtDetail1"] = null;

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
                if (Session["dtHead"] == null)
                {
                    Session["dtHead"] = GetOrdenesAbiertas(rtbOrden.Text.ToString(), rcbCorte.SelectedValue);
                }
                dtOrdenes = Session["dtHead"] as DataTable;

                if (dtOrdenes.Columns[0].ColumnName.ToString() == "ErrMsg")
                {
                    ShowMasterPageMessage("Error", "Failed to load data", dt.Rows[0]["ErrMsg"].ToString());
                }
                else
                {
                    DataTable dtHeader = new DataTable();
                    dtHeader.Columns.Add("OSDocNum", typeof(string));
                    dtHeader.Columns.Add("ToCardCode", typeof(string));
                    dtHeader.Columns.Add("fechadesp", typeof(string));
                    dtHeader.Columns.Add("fecharec", typeof(string));
                    dtHeader.Columns.Add("factDFB", typeof(string));
                    dtHeader.Columns.Add("numapri", typeof(string));
                    dtHeader.Columns.Add("DocNum", typeof(string));
                    dtHeader.Columns.Add("fromapp", typeof(string));

                    //var v_Query = from rows in dtOrdenes.AsEnumerable()
                    //              group rows by new
                    //              {
                    //                  OSDocNum = rows["OSDocNum"],
                    //                  ToCardCode = rows["ToCardCode"],
                    //                  fechadesp = rows["fechadesp"],
                    //                  factDFB = rows["factDFB"] == null ? "-1" : rows["factDFB"],
                    //                  numapri = rows["numapri"] == null ? "-1" : rows["numapri"],
                    //                  DocNum = rows["DocNum"] == null ? "-1" : rows["DocNum"],
                    //                  fromapp = rows["fromapp"] == null ? "-1" : rows["fromapp"]
                    //              } into grp
                    //              orderby grp.Key.OSDocNum
                    //              select new
                    //              {
                    //                  OSDocNum = grp.Key.OSDocNum,
                    //                  ToCardCode = grp.Key.ToCardCode,
                    //                  fechadesp = grp.Key.fechadesp,
                    //                  factDFB = grp.Key.factDFB,
                    //                  numapri = grp.Key.numapri,
                    //                  DocNum = grp.Key.DocNum,
                    //                  fromapp = grp.Key.fromapp,
                    //                  fecharec = grp.Select(r => r.Field<string>("fecharec")).ToString().Max()
                    //              };

                    var v_Query = from rows in dtOrdenes.AsEnumerable()
                                  group rows by new
                                  {
                                      OSDocNum = rows["OSDocNum"],
                                      ToCardCode = rows["ToCardCode"],
                                      fechadesp = rows["fechadesp"],
                                      factDFB = rows["factDFB"] ?? "-1",
                                      numapri = rows["numapri"] ?? "-1",
                                      DocNum = rows["DocNum"] ?? "-1",
                                      fromapp = rows["fromapp"] ?? "-1"
                                  } into grp
                                  orderby grp.Key.OSDocNum
                                  select new
                                  {
                                      grp.Key.OSDocNum,
                                      grp.Key.ToCardCode,
                                      grp.Key.fechadesp,
                                      grp.Key.factDFB,
                                      grp.Key.numapri,
                                      grp.Key.DocNum,
                                      grp.Key.fromapp,
                                      fecharec = grp.Select(r => r.Field<string>("fecharec")).ToString().Max()
                                  };

                    foreach (var item in v_Query)
                    {
                        DataRow dr = dtHeader.NewRow();
                        dr["OSDocNum"] = item.OSDocNum;
                        dr["ToCardCode"] = item.ToCardCode;
                        dr["fechadesp"] = item.fechadesp;
                        dr["factDFB"] = item.factDFB;
                        dr["numapri"] = item.numapri;
                        dr["DocNum"] = item.DocNum;
                        dr["fromapp"] = item.fromapp;
                        dr["fecharec"] = item.fecharec;
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
            string v_OSDocNum = dataItem.GetDataKeyValue("OSDocNum").ToString();

            if (Session["dtDetail1"] == null)
            {
                Session["dtDetail1"] = dm.GetBinesDesp(v_OSDocNum, rcbCorte.SelectedValue);
            }
            DataTable dtOrdenesDetail = Session["dtDetail1"] as DataTable;

            if (dtOrdenesDetail.Columns[0].ColumnName.ToString() == "ErrMsg")
            {
                ShowMasterPageMessage("Error", "Failed to load data", dt.Rows[0]["ErrMsg"].ToString());
            }
            else
            {
                DataTable dtDetail = new DataTable();
                dtDetail.Columns.Add("OSDocNum", typeof(string));
                dtDetail.Columns.Add("WmsBin", typeof(string));
                dtDetail.Columns.Add("CintilloBin1", typeof(string));
                dtDetail.Columns.Add("CintilloBin2", typeof(string));
                dtDetail.Columns.Add("estatus", typeof(string));

                //var v_Query = from rows in dtOrdenesDetail.AsEnumerable()
                //              where rows["OSDocNum"].ToString() == v_OSDocNum.ToString()
                //              group rows by new
                //              {
                //                  OSDocNum = rows["OSDocNum"],
                //                  WmsBin = rows["WmsBin"],
                //                  CintilloBin1 = rows["CintilloBin1"],
                //                  CintilloBin2 = rows["CintilloBin2"],
                //                  estatus = rows["estatus"]
                //              } into grp
                //              orderby grp.Key.WmsBin
                //              select new
                //              {
                //                  OSDocNum = grp.Key.OSDocNum,
                //                  WmsBin = grp.Key.WmsBin,
                //                  CintilloBin1 = grp.Key.CintilloBin1,
                //                  CintilloBin2 = grp.Key.CintilloBin2,
                //                  estatus = grp.Key.estatus
                //              };

                var v_Query = from rows in dtOrdenesDetail.AsEnumerable()
                              where rows["OSDocNum"].ToString() == v_OSDocNum.ToString()
                              group rows by new
                              {
                                  OSDocNum = rows["OSDocNum"],
                                  WmsBin = rows["WmsBin"],
                                  CintilloBin1 = rows["CintilloBin1"],
                                  CintilloBin2 = rows["CintilloBin2"],
                                  estatus = rows["estatus"]
                              } into grp
                              orderby grp.Key.WmsBin
                              select new
                              {
                                  grp.Key.OSDocNum,
                                  grp.Key.WmsBin,
                                  grp.Key.CintilloBin1,
                                  grp.Key.CintilloBin2,
                                  grp.Key.estatus
                              };

                foreach (var item in v_Query)
                {
                    DataRow dr = dtDetail.NewRow();
                    dr["OSDocNum"] = item.OSDocNum;
                    dr["WmsBin"] = item.WmsBin;
                    dr["CintilloBin1"] = item.CintilloBin1;
                    dr["CintilloBin2"] = item.CintilloBin2;
                    dr["estatus"] = item.estatus;
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
            Session["dtHead"] = null;
            Session["dtDetail1"] = null;
        }
    }
}