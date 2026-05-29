using System;
using System.Data;
using Telerik.Web.UI;

public partial class R_Kardex : BasePage
{
    public DataTable dt = new DataTable();
    public DataManager dm = new DataManager();
    public decimal total = 0;
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            ValidaSesionNullOrEmpty(new string[] { "UserId", "CompanyId" });

            if (!IsPostBack)
            {
                Session["SearchItemByBarCodesData"] = null;
                Session["SearchItemByBarCodesGroup"] = "-";
                Session["SearchItemByBarCodesBarCode"] = "-";
                Session["RptData"] = null;
                Session["RptFromDateTxt"] = "-";
                Session["RpttoDateTxt"] = "-";
                Session["RptrtbItem"] = "-";
                Session["RptrcbGrupo"] = "-";
                Session["RptrcbCorte"] = "-";

                GetCias();

                rcbCorte.SelectedValue = Session["CompanyId"].ToString();
                rcbCorte_SelectedIndexChanged(null, null);
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
    private void GetGrupos()
    {
        try
        {
            DataTable dtGrupo = dm.GetGruposKardex(rcbCorte.SelectedValue.ToString());

            rcbGrupo.DataSource = dtGrupo;
            rcbGrupo.DataValueField = "ItmsGrpCod";
            rcbGrupo.DataTextField = "ItmsGrpNam";
            rcbGrupo.DataBind();

            rcbGrupo.ClearSelection();
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get Grupo data", ex.Message.ToString());
        }
    }

    private DataTable GetData(string sFromDate, string sToDate, string sRtbItem, string sRcbGrupo, string sRcbCorte)
    {
        try
        {
            //dt = dm.GetInventoryKardex(rcbCorte.SelectedValue, rcbGrupo.SelectedValue, rtbItem.Text, hfFromDate.Value.ToString(), hfToDate.Value.ToString());
            dt = dm.GetInventoryKardex(sRcbCorte, sRcbGrupo, sRtbItem, sFromDate, sToDate);
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
        }

        return dt;
    }

    //2019-ABR-11: Modificado por Aldo Reina, para la búsqueda por código de barras:
    protected void rbtnView_Click(object sender, EventArgs e)
    {
        bool goToRebind;
        try
        {
            if (FromDateTxt.SelectedDate == null && toDateTxt.SelectedDate == null)
            {
                rgHead.ShowFooter = true;
                rgHead.MasterTableView.ShowFooter = true;
                rgHead.MasterTableView.ShowGroupFooter = true;
            }
            else
            {
                rgHead.ShowFooter = false;
                rgHead.MasterTableView.ShowFooter = false;
                rgHead.MasterTableView.ShowGroupFooter = false;
            }

            if (rcbCorte.SelectedIndex >= 0 && !string.IsNullOrEmpty(rcbGrupo.SelectedValue) && !string.IsNullOrEmpty(rtbItem.Text) && !string.IsNullOrWhiteSpace(rtbItem.Text))
            {
                goToRebind = ValidateByBarCode("", rtbItem.Text);
            }
            else
            {
                goToRebind = true;
            }
        }
        catch (Exception ex)
        {
            goToRebind = true;
        }

        if (goToRebind)
        {
            rgHead.Rebind();
        }
    }

    protected void rcbCorte_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        //Session["dtKardex"] = null;
        GetGrupos();
        rgHead.Visible = false;
        divHeading.Visible = false;
    }

    //protected void rcbGrupo_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    //{
    //    //Session["dtKardex"] = null;
    //    rgHead.Visible = false;
    //    divHeading.Visible = false;
    //}

    protected void rgHead_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        ValidaSesionNullOrEmpty(new string[] { "UserId", "CompanyId" });

        try
        {
            if (rcbCorte.SelectedIndex >= 0 && !string.IsNullOrEmpty(rcbGrupo.SelectedValue) && !string.IsNullOrEmpty(rtbItem.Text) && !string.IsNullOrWhiteSpace(rtbItem.Text))
            {
                DataTable dtOrdenes = new DataTable();
                string sFromDate;
                string sToDate;
                string sRtbItem = rtbItem.Text;
                string sRcbGrupo = rcbGrupo.SelectedValue;
                string sRcbCorte = rcbCorte.SelectedValue;
                
                if (FromDateTxt.SelectedDate == null)
                {
                    sFromDate = "";
                    rgHead.ShowFooter = true;
                }
                else
                {
                    sFromDate = FromDateTxt.SelectedDate.Value.ToShortDateString().ToString();
                    rgHead.ShowFooter = false;
                }

                if (toDateTxt.SelectedDate == null)
                {
                    sToDate = "";
                    rgHead.ShowFooter = true;
                }
                else
                {
                    sToDate = toDateTxt.SelectedDate.Value.ToShortDateString();
                    rgHead.ShowFooter = false;
                }

                if (Session["RptFromDateTxt"].ToString() != sFromDate ||
                    Session["RpttoDateTxt"].ToString() != sToDate ||
                    Session["RptrtbItem"].ToString() != sRtbItem ||
                    Session["RptrcbGrupo"].ToString() != sRcbGrupo ||
                    Session["RptrcbCorte"].ToString() != sRcbCorte)
                {
                    Session["RptData"] = GetData(sFromDate, sToDate, sRtbItem, sRcbGrupo, sRcbCorte);
                    Session["RptFromDateTxt"] = sFromDate;
                    Session["RpttoDateTxt"] = sToDate;
                    Session["RptrtbItem"] = sRtbItem;
                    Session["RptrcbGrupo"] = sRcbGrupo;
                    Session["RptrcbCorte"] = sRcbCorte;
                }

                if(!ValidaSesionRptDataNull("RptData", false))
                {
                    dtOrdenes = (DataTable)Session["RptData"];
                }

                if (dtOrdenes.Columns[0].ColumnName.ToString() == "ErrMsg")
                {
                    pnlInfo.Visible = false;
                    ShowMasterPageMessage("Error", "Failed to load data", dt.Rows[0]["ErrMsg"].ToString());
                }
                else
                {
                    if (dtOrdenes.Rows.Count > 0)
                    {
                        rlblItem.Text = dtOrdenes.Rows[0]["ItemCode"].ToString();
                        rlblDesc.Text = dtOrdenes.Rows[0]["Dscription"].ToString();
                        rlblBarCode.Text = dtOrdenes.Rows[0]["BarCode"].ToString();
                        pnlInfo.Visible = true;
                        rgHead.DataSource = dtOrdenes;
                        divHeading.Visible = true;
                        rgHead.Visible = true;
                    }
                    else
                    {
                        pnlInfo.Visible = false;
                        ShowMasterPageMessage("Standard", "No data", "No data for your selection");
                    }
                }
            }
            else
            {
                divHeading.Visible = false;
                rgHead.Visible = false;
                string dbg = string.Format("CIA idx={0} val='{1}' | GRP idx={2} val='{3}' | Item='{4}'",
                    rcbCorte.SelectedIndex, rcbCorte.SelectedValue,
                    rcbGrupo.SelectedIndex, rcbGrupo.SelectedValue,
                    rtbItem.Text);
                ShowMasterPageMessage("Error", "Failed to Get data", "Please select CIA, Grupo & Item. [" + dbg + "]");
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
    //        e.Row.Cells.GetCellByName("Avance").StyleValue = "pctStyle";
    //        e.Row.Cells.GetCellByName("PesoTotal").StyleValue = "numberStyle";
    //        e.Row.Cells.GetCellByName("CUBE_CRTN").StyleValue = "numberStyle";
    //        e.Row.Cells.GetCellByName("CantidaddeUnidades").StyleValue = "numberStyle";
    //        e.Row.Cells.GetCellByName("LineTotal").StyleValue = "currencyStyle";
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
    //        StyleElement pctStyle = new StyleElement("pctStyle");
    //        pctStyle.NumberFormat.FormatType = NumberFormatType.Percent;
    //        //pctStyle.FontStyle.Bold = true;
    //        e.Styles.Add(pctStyle);

    //        StyleElement numberStyle = new StyleElement("numberStyle");
    //        numberStyle.NumberFormat.FormatType = NumberFormatType.General;
    //        e.Styles.Add(numberStyle);

    //        StyleElement currencyStyle = new StyleElement("currencyStyle");
    //        currencyStyle.NumberFormat.FormatType = NumberFormatType.Currency;
    //        e.Styles.Add(currencyStyle);

    //        StyleElement headerStyle = new StyleElement("headerStyle");
    //        headerStyle.NumberFormat.FormatType = NumberFormatType.Currency;
    //        e.Styles.Add(headerStyle);
    //    }
    //    catch (Exception ex)
    //    {
    //        ShowMasterPageMessage("Error", "Failed when export style created", ex.Message.ToString());
    //        return;
    //    }
    //}
    protected void rgHead_ItemCommand(object sender, GridCommandEventArgs e)
    {
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
            Session["RptData"] = null;
            Session["RptFromDateTxt"] = "-";
            Session["RpttoDateTxt"] = "-";
            Session["RptrtbItem"] = "-";
            Session["RptrcbGrupo"] = "-";
            Session["RptrcbCorte"] = "-";

            Session["SearchItemByBarCodesData"] = null;
            Session["SearchItemByBarCodesGroup"] = "-";
            Session["SearchItemByBarCodesBarCode"] = "-";
        }
    }

    //protected void rgHead_ItemDataBound(object sender, GridItemEventArgs e)
    //{
    //    try
    //    {
    //        if (e.Item is GridDataItem)
    //        {
    //            GridDataItem dataItem = e.Item as GridDataItem;
    //            string QtyTrans = dataItem["QtyTrans"].Text;
    //            decimal fieldValue = decimal.Parse(QtyTrans);
    //            total += fieldValue;
    //            dataItem["Balance"].Text = total.ToString();
    //        }

    //        if (e.Item is GridGroupFooterItem)
    //        {
    //            total = 0;
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        ShowMasterPageMessage("Error", "Failed in item databound event", ex.Message.ToString());
    //        return;
    //    }
    //}

    //2019-ABR-10: Agregado por Aldo Reina, para la búsqueda por código de barras:
    protected void ItemList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (ItemList.SelectedValue != "-")
        {
            rtbItem.Text = ItemList.SelectedValue;
            Session["RptrtbItem"] = rtbItem.Text;
            rtbItem.Visible = true;
            ItemList.Visible = false;
            rbtnCancel1.Visible = false;
            rbtnView.Enabled = true;

            rgHead.Rebind();
        }
    }

    //2019-ABR-10: Agregado por Aldo Reina, para la búsqueda por código de barras:
    protected void RbtnCancel_Click(object sender, EventArgs e)
    {
        ItemList.Visible = false;
        rbtnCancel1.Visible = false;
        rbtnView.Enabled = true;

        rtbItem.Text = "";
        rtbItem.Visible = true;
    }


    //2019-ABR-10: Agregado por Aldo Reina, para la búsqueda por código de barras:
    private bool ValidateByBarCode(string grpId, string barCode)
    {
        bool res = false;
        DataTable dt = null;
        DataRow row;
        string sRcbCorte = rcbCorte.SelectedValue;

        try
        {
            if (Session["RptrcbCorte"].ToString() != sRcbCorte ||
                Session["SearchItemByBarCodesGroup"].ToString() != grpId ||
                Session["SearchItemByBarCodesBarCode"].ToString() != barCode)
            {
                Session["SearchItemByBarCodesData"] = dm.SearchItemByBarCodes(sRcbCorte, grpId, barCode);
                Session["RptrcbCorte"] = sRcbCorte;
                Session["SearchItemByBarCodesGroup"] = grpId;
                Session["SearchItemByBarCodesBarCode"] = barCode;
            }

            if (!ValidaSesionRptDataNull("SearchItemByBarCodesData", false))
            {
                dt = (DataTable)Session["SearchItemByBarCodesData"];
            }

            if (dt.Rows.Count <= 0)
            {
                ItemList.Visible = false;
                rbtnCancel1.Visible = false;

                rtbItem.Visible = true;

                rbtnView.Enabled = true;

                //If the item is not found, just go on for the binding. Then, it won't show the
                //table if the item code provided is a bar code (probably user will faint here :D)
                //because no messages are showed here o.o!
                res = true;
            }
            else if (dt.Rows.Count == 1)
            {
                row = dt.Rows[0];
                rtbItem.Text = row["ItemCode"].ToString();
                Session["RptrtbItem"] = row["ItemCode"].ToString();
                ItemList.Visible = false;
                rbtnCancel1.Visible = false;

                rtbItem.Visible = true;

                rbtnView.Enabled = true;

                //Here just go on to the bind function.
                res = true;
            }
            else
            {
                DataTable dTable = dt;
                DataRow dtRow = dTable.NewRow();
                dtRow["ItemCode"] = "-";
                dtRow["ItemName"] = "SELECT ITEM";

                dt.Rows.InsertAt(dtRow, 0);

                ItemList.DataSource = dt;
                ItemList.DataMember = "ItemCode";
                ItemList.DataValueField = "ItemCode";
                ItemList.DataTextField = "ItemName";
                ItemList.DataBind();
                ItemList.Visible = true;
                rbtnCancel1.Visible = true;

                ItemList.Width = 177;

                ItemList.Focus();
                ItemList.ToolTip = "SELECT ITEM";

                rtbItem.Visible = false;

                rbtnView.Enabled = false;

                res = false;
            }
            return res;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
    //protected void FromDateTxt_SelectedDateChanged(object sender, Telerik.Web.UI.Calendar.SelectedDateChangedEventArgs e)
    //{
    //    if (FromDateTxt.SelectedDate == null)
    //    {
    //        hfFromDate.Value = "";
    //        rgHead.ShowFooter = true;
    //    }
    //    else
    //    {
    //        hfFromDate.Value = FromDateTxt.SelectedDate.Value.ToShortDateString().ToString();
    //        rgHead.ShowFooter = false;
    //    }
    //}

    //protected void toDateTxt_SelectedDateChanged(object sender, Telerik.Web.UI.Calendar.SelectedDateChangedEventArgs e)
    //{
    //    if (toDateTxt.SelectedDate == null)
    //    {
    //        hfToDate.Value = "";
    //        rgHead.ShowFooter = true;
    //    }
    //    else
    //    {
    //        hfToDate.Value = toDateTxt.SelectedDate.Value.ToShortDateString().ToString();
    //        rgHead.ShowFooter = false;
    //    }
    //}

    //protected void rtbItem_DataSourceSelect(object sender, SearchBoxDataSourceSelectEventArgs e)
    //{
    //    SqlDataSource ds = (SqlDataSource)e.DataSource;
    //    RadSearchBox searchBox = (RadSearchBox)sender;
    //    string query = searchBox.Text;
    //    string sq3 = "SELECT TOP 1000 ItemCode FROM " + rcbCorte.SelectedValue + ".dbo.OITM " + Queries.WITH_NOLOCK + @"  WHERE ItemCode LIKE '%" + searchBox.Text  + "%' UNION SELECT TOP 1000 ItemCode FROM " + rcbCorte.SelectedValue + ".dbo.OBCD " + Queries.WITH_NOLOCK + @"  WHERE BcdCode LIKE '%" + searchBox.Text + "%'";

    //    ds.SelectCommand = sq3;
    //}
}