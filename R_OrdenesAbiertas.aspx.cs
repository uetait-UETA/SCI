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

public partial class R_OrdenesAbiertas : BasePage
{
    public DataTable dt = new DataTable();
    public DataManager dm = new DataManager();

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
                else
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
    
    private DataTable GetOrdenesAbiertas(string v_Corte, string v_Ordenes)
    {
        try
        {
            dt = dm.GetOrdenesAbiertas(v_Corte, v_Ordenes);
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
            Session["dtOrdenes"] = null;
            Session["dtOrdenesDetail"] = null;
        }
        rgOrdenes.Rebind();
    }

    protected void rcbCorte_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        Session["dtOrdenes"] = null;
        Session["dtOrdenesDetail"] = null;

        rgOrdenes.Visible = false;
        divHeading.Visible = false;
    }

    protected void rgOrdenes_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        try
        {
            if (rcbCorte.SelectedValue != "")
            {
                DataTable dtOrdenes = new DataTable();
                if (Session["dtOrdenes"] == null)
                {
                    Session["dtOrdenes"] = GetOrdenesAbiertas(rcbCorte.SelectedValue, rtbOrden.Text.ToString());
                }
                dtOrdenes = Session["dtOrdenes"] as DataTable;

                if (dtOrdenes.Columns[0].ColumnName.ToString() == "ErrMsg")
                {
                    ShowMasterPageMessage("Error", "Failed to load data", dt.Rows[0]["ErrMsg"].ToString());
                }
                else
                {
                    DataTable dtHeader = new DataTable();
                    dtHeader.Columns.Add("OrdenDeVenta", typeof(int));
                    dtHeader.Columns.Add("DESP", typeof(string));
                    dtHeader.Columns.Add("Delivery", typeof(int));
                    dtHeader.Columns.Add("A_R_Invoice", typeof(int));
                    dtHeader.Columns.Add("ItmsGrpNam", typeof(string));
                    dtHeader.Columns.Add("CardCode", typeof(string));
                    dtHeader.Columns.Add("Comments", typeof(string));
                    dtHeader.Columns.Add("Fecha", typeof(string));
                    dtHeader.Columns.Add("CUBE_CRTN", typeof(decimal));
                    dtHeader.Columns.Add("TOTLABEL", typeof(string));
                    dtHeader.Columns.Add("PesoTotal", typeof(decimal));
                    dtHeader.Columns.Add("CantidaddeUnidades", typeof(decimal));
                    dtHeader.Columns.Add("LineTotal", typeof(decimal));
                    dtHeader.Columns.Add("Avance", typeof(decimal));

                    var v_Query = from rows in dtOrdenes.AsEnumerable()
                                  group rows by new
                                  {
                                      OrdenDeVenta = rows["Orden De Venta"],
                                      DESP = rows["DESP"],
                                      Delivery = rows["Delivery"],
                                      A_R_Invoice = rows["A/R Invoice"],
                                      ItmsGrpNam = rows["ItmsGrpNam"],
                                      CardCode = rows["CardCode"],
                                      Comments = rows["Comments"],
                                      Fecha = rows["Fecha"],
                                      CUBE_CRTN = rows["CUBE_CRTN"]
                                  } into grp
                                  orderby grp.Key.OrdenDeVenta
                                  select new
                                  {
                                      OrdenDeVenta = grp.Key.OrdenDeVenta,
                                      DESP = grp.Key.DESP,
                                      Delivery = grp.Key.Delivery,
                                      A_R_Invoice = grp.Key.A_R_Invoice,
                                      ItmsGrpNam = grp.Key.ItmsGrpNam,
                                      CardCode = grp.Key.CardCode,
                                      Comments = grp.Key.Comments,
                                      Fecha = grp.Key.Fecha,
                                      CUBE_CRTN = grp.Key.CUBE_CRTN,
                                      TOTLABEL = grp.Select(r => r.Field<string>("TOTLABEL")).Distinct().Count(),
                                      PesoTotal = grp.Sum(r => r.Field<Decimal?>("WEIGHT")),
                                      CantidaddeUnidades = grp.Sum(r => r.Field<Decimal?>("QUANTITY")),
                                      LineTotal = grp.Sum(r => r.Field<Decimal?>("LineTotal")),
                                      Avance = grp.Sum(r => r.Field<Decimal>("Quantity")) / grp.Sum(r => r.Field<Decimal>("QTY_TO_PICK"))
                                  };
                    
                    foreach (var item in v_Query)
                    {
                        DataRow dr = dtHeader.NewRow();
                        dr["OrdenDeVenta"] = item.OrdenDeVenta;
                        dr["DESP"] = item.DESP;
                        dr["Delivery"] = item.Delivery;
                        dr["A_R_Invoice"] = item.A_R_Invoice;
                        dr["ItmsGrpNam"] = item.ItmsGrpNam;
                        dr["CardCode"] = item.CardCode;
                        dr["Comments"] = item.Comments;
                        dr["Fecha"] = item.Fecha;
                        dr["CUBE_CRTN"] = item.CUBE_CRTN;
                        if (item.TOTLABEL.ToString() == "0")
                        {
                            dr["TOTLABEL"] = "";
                        }
                        else
                        {
                            dr["TOTLABEL"] = item.TOTLABEL;
                        }
                        dr["PesoTotal"] = item.PesoTotal;
                        dr["CantidaddeUnidades"] = item.CantidaddeUnidades;
                        dr["LineTotal"] = item.LineTotal;
                        dr["Avance"] = item.Avance;
                        dtHeader.Rows.Add(dr);

                    }

                    rgOrdenes.DataSource = dtHeader;
                    divHeading.Visible = true;
                    rgOrdenes.Visible = true;
                }
            }
            else
            {
                divHeading.Visible = false;
                rgOrdenes.Visible = false;
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
            return;
        }

    }

    protected void rgOrdenes_DetailTableDataBind(object sender, GridDetailTableDataBindEventArgs e)
    {
        try
        {
            GridDataItem dataItem = (GridDataItem)e.DetailTableView.ParentItem;
            string v_OrdenDeVenta = dataItem.GetDataKeyValue("OrdenDeVenta").ToString();

            //DataTable dtOrdenesDetail = dm.GetOrdenesAbiertasDetail(rcbCorte.SelectedValue, v_OrdenDeVenta);
            if (Session["dtOrdenesDetail"] == null)
            {
                Session["dtOrdenesDetail"] = dm.GetOrdenesAbiertasDetail(rcbCorte.SelectedValue, v_OrdenDeVenta);
            }
            DataTable dtOrdenesDetail = Session["dtOrdenesDetail"] as DataTable;

            if (dtOrdenesDetail.Columns[0].ColumnName.ToString() == "ErrMsg")
            {
                ShowMasterPageMessage("Error", "Failed to load data", dt.Rows[0]["ErrMsg"].ToString());
            }
            else
            {
                DataTable dtDetail = new DataTable();
                dtDetail.Columns.Add("ItemCode", typeof(string));
                dtDetail.Columns.Add("Dscription", typeof(string));
                dtDetail.Columns.Add("Quantity", typeof(int));
                dtDetail.Columns.Add("Price", typeof(int));
                dtDetail.Columns.Add("LineTotal", typeof(decimal));

                var v_Query = from rows in dtOrdenesDetail.AsEnumerable()
                              where rows["Orden De Venta"].ToString() == v_OrdenDeVenta.ToString()
                              group rows by new
                              {
                                  ItemCode = rows["ItemCode"],
                                  Dscription = rows["Dscription"],
                                  Quantity = rows["Quantity"],
                                  Price = rows["Price"],
                                  LineTotal = rows["LineTotal"]
                              } into grp
                              orderby grp.Key.ItemCode
                              select new
                              {
                                  ItemCode = grp.Key.ItemCode,
                                  Dscription = grp.Key.Dscription,
                                  Quantity = grp.Key.Quantity,
                                  Price = grp.Key.Price,
                                  LineTotal = grp.Key.LineTotal
                              };

                foreach (var item in v_Query)
                {
                    DataRow dr = dtDetail.NewRow();
                    dr["ItemCode"] = item.ItemCode;
                    dr["Dscription"] = item.Dscription;
                    dr["Quantity"] = item.Quantity;
                    dr["Price"] = item.Price;
                    dr["LineTotal"] = item.LineTotal;
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

    protected void rgOrdenes_ExcelMLExportRowCreated(object sender, Telerik.Web.UI.GridExcelBuilder.GridExportExcelMLRowCreatedArgs e)
    {
        try
        {
            e.Row.Cells.GetCellByName("Avance").StyleValue = "pctStyle";
            e.Row.Cells.GetCellByName("PesoTotal").StyleValue = "numberStyle";
            e.Row.Cells.GetCellByName("CUBE_CRTN").StyleValue = "numberStyle";
            e.Row.Cells.GetCellByName("CantidaddeUnidades").StyleValue = "numberStyle";
            e.Row.Cells.GetCellByName("LineTotal").StyleValue = "currencyStyle";
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed when export row created", ex.Message.ToString());
            return;
        }        
    }

    protected void rgOrdenes_ExcelMLExportStylesCreated(object sender, Telerik.Web.UI.GridExcelBuilder.GridExportExcelMLStyleCreatedArgs e)
    {        
        try
        {
            StyleElement pctStyle = new StyleElement("pctStyle");
            pctStyle.NumberFormat.FormatType = NumberFormatType.Percent;
            //pctStyle.FontStyle.Bold = true;
            e.Styles.Add(pctStyle);

            StyleElement numberStyle = new StyleElement("numberStyle");
            numberStyle.NumberFormat.FormatType = NumberFormatType.General;
            e.Styles.Add(numberStyle);

            StyleElement currencyStyle = new StyleElement("currencyStyle");
            currencyStyle.NumberFormat.FormatType = NumberFormatType.Currency;
            e.Styles.Add(currencyStyle);

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
    protected void rgOrdenes_ItemCommand(object sender, GridCommandEventArgs e)
    {
        //rgOrdenes.MasterTableView.Caption = "Corte de Ordenes de Ventas y Etiquetado - TOCUMEN";
        if (e.CommandName == RadGrid.ExportToExcelCommandName)
        {
            foreach (GridDataItem item in rgOrdenes.MasterTableView.Items)
            {
                item.Expanded = true;
            }

            rgOrdenes.ExportSettings.IgnorePaging = true;
            rgOrdenes.ExportSettings.ExportOnlyData = true;
            rgOrdenes.ExportSettings.OpenInNewWindow = true;
            rgOrdenes.MasterTableView.UseAllDataFields = false;
            rgOrdenes.ExportSettings.Excel.Format = GridExcelExportFormat.ExcelML;

            rgOrdenes.MasterTableView.HierarchyDefaultExpanded = false;
            rgOrdenes.MasterTableView.DetailTables[0].HierarchyDefaultExpanded = false;

            rgOrdenes.MasterTableView.HierarchyLoadMode = GridChildLoadMode.Client;
            rgOrdenes.MasterTableView.DetailTables[0].HierarchyLoadMode = GridChildLoadMode.Client;

            

            rgOrdenes.MasterTableView.ExportToExcel();
        }
        if (e.CommandName == RadGrid.RebindGridCommandName)
        {
            Session["dtOrdenes"] = null;
            Session["dtOrdenesDetail"] = null;
        }
    }
}