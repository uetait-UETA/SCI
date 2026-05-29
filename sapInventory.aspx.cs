using System;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using Telerik.Web.UI;
using System.Collections;

public partial class sapInventory : BasePage
{
    protected SqlDb db = new SqlDb();
    protected string usr;
    protected string Loc;
    protected string Item;
    protected string sap_db;
    protected string serverIP;
    protected string serverUserName;
    protected string serverPwd;
    protected string dbUserName;
    protected string dbPwd;
    protected string licenseServerIP;
    protected string xmlPath;
    protected string lCurUser;

    protected void Page_Load(object sender, EventArgs e)
    {

        if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

        if (string.IsNullOrEmpty((string)Session["CompanyId"]))
        {
            Response.Redirect("Login1.aspx");
        }

        lCurUser = (string)Session["UserId"];

        sap_db = (string)Session["CompanyId"];
        CompanyIdLabel.Text = sap_db;

        ArrayList controles = (ArrayList)Session["Controles"];

        string thiscontrol = "";
        char flagokay = 'N';

        for (int i = 0; i < controles.Count; i++)
        {
            thiscontrol = (controles[i].ToString());
            if ((thiscontrol == "SapInventory.aspx") || (thiscontrol == "ATOTAL"))
            {
                flagokay = 'Y';
            }
        }

        //char flagokay = 'Y';
        //string lControlName = "SapInventory.aspx";
        //string strRole_Description = "";
        //string strAccessType = "";

        //db.Connect();
        //db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, lControlName, ref strAccessType, ref strRole_Description);
        //db.Disconnect();

        //if (strAccessType == "N")
        //{
        //    flagokay = 'N';
        //    string message = "User " + lCurUser + ", with Role " + strRole_Description + " does not have permissions to access this screen.";
        //    string url = string.Format("Default.aspx");
        //    string script = "{ alert('";
        //    script += message;
        //    script += "');";
        //    script += "window.location = '";
        //    script += url;
        //    script += "'; }";
        //    ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "alert", script, true);
        //}

        //if (strAccessType == "R")
        //{
        //    //Read Only Access
        //}

        //if (strAccessType == "F")
        //{
        //    //Full Access
        //}

        if (flagokay == 'Y')
        {
            //sap_db = ConfigurationSettings.AppSettings.Get("sap_db");
            serverIP = ConfigurationManager.AppSettings.Get("serverIP");
            serverUserName = ConfigurationManager.AppSettings.Get("serverUserName");
            serverPwd = ConfigurationManager.AppSettings.Get("serverPwd");
            dbUserName = ConfigurationManager.AppSettings.Get("dbUserName");
            dbPwd = ConfigurationManager.AppSettings.Get("dbPwd");
            licenseServerIP = ConfigurationManager.AppSettings.Get("licenseServerIP");

            //usr = User.Identity.Name.ToLower().Replace("lgihome\\", "");
            string x = User.Identity.Name.ToLower();
            usr = x.Substring(x.IndexOf("\\") + 1, x.Length - x.IndexOf("\\") - 1);

            divMessage.InnerHtml = "";

            if (!IsPostBack)
            {
                try
                {
                    db.Connect();
                    LoadWarehouses();
                    LoadItemGroups();
                    LoadCortes();
                    InitializeForm();

                    Session["SearchItemByBarCodesData"] = null;
                    Session["SearchItemByBarCodesarCode"] = "-";
                    Session["RptData"] = null;
                    Session["RptdrpToWhsCode"] = "-";
                    Session["RptdrpItemGroups"] = "-";
                    Session["RptbrandsLabel"] = "-";
                    Session["RptArticuloTextBox"] = "-";
                    Session["RptTypeSearchLabel"] = "-";
                    Session["RptdrpCortes"] = "-";
                    Session["CiaLabel"] = "-";
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    db.Disconnect();
                }
            }
        }
        else
        {
            Session["FlagNoPerPag"] = "N";
            Response.Redirect("Login1.aspx");
        }
    }

    private void InitializeForm()
    {
        divMessage.InnerHtml = "";
        GridView1.Enabled = false;
        //rgHead.Visible = false;
    }

    private void LoadWarehouses()
    {

        DataTable dt2 = new DataTable();

        try
        {
            int branchId = 0;
            int.TryParse(Session["BranchId"] as string, out branchId);
            string branchFilter = branchId > 1
                ? " WHERE O.BPLid IN (1," + branchId + ")"
                : branchId > 0 ? " WHERE O.BPLid = 1" : "";
            string sql2 =
             @"select O.WhsCode,
                      CONVERT(nvarchar(30), ISNULL(O.U_POSCode, '')) + ' - ' + O.WhsCode + ' - ' + O.WhsName AS WHS
                  from " + sap_db + @".dbo.owhs O " + Queries.WITH_NOLOCK + branchFilter + @"
                  ORDER BY CASE WHEN O.BPLId = 1 THEN 0 ELSE 1 END, O.U_POSCode";

            //where WhsCode in ('BODEGA', 'TIENDA')

            db.adapter = new SqlDataAdapter(sql2, db.Conn);
            db.adapter.Fill(dt2);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadWarehouses for toloc. ERROR MESSAGE : " + ex.Message);
        }


        drpToWhsCode.DataSource = dt2;
        drpToWhsCode.DataTextField = "WHS";
        drpToWhsCode.DataValueField = "WhsCode";
        drpToWhsCode.DataBind();

        drpToWhsCode.DefaultItem.Text = "Select a location";
        drpToWhsCode.DefaultItem.Value = "0";

        //ListItem li = new ListItem("Select a location", "0");

        //drpToWhsCode.Items.Insert(0, li);
    }

    private void LoadCortes()
    {

        DataTable dt2 = new DataTable();

        try
        {
            string sql2 =
             @"
		select DATEPART(HOUR, docdate) CorteCode, DATEPART(HOUR, docdate) CorteName 
        from SMM_ODRF_WK_HIS
		where CompanyId = '{0}' and convert(date,docdate,101) = convert(date,getdate(),101)
		group by DATEPART(HOUR, docdate)
	        order by 1
	            ";

            sql2 = string.Format(sql2, sap_db);
            db.adapter = new SqlDataAdapter(sql2, db.Conn);
            db.adapter.Fill(dt2);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadCortes. ERROR MESSAGE : " + ex.Message);
        }


        drpCortes.DataSource = dt2;
        drpCortes.DataTextField = "CorteName";
        drpCortes.DataValueField = "CorteCode";
        drpCortes.DataBind();

        drpCortes.DefaultItem.Text = "Select Period";
        drpCortes.DefaultItem.Value = "0";

    }

    private void LoadItemGroups()
    {
        DataTable dt = new DataTable();

        try
        {
            string sql =
            @"select 
               ItmsGrpCod GroupCode, 
               cast(ItmsGrpCod as varchar) + ' - ' + ItmsGrpNam GroupName 
             from " + sap_db + @".dbo.oitb
            order by ItmsGrpCod";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadItemGroups. ERROR MESSAGE : " + ex.Message);
        }
        

        drpItemGroups.DataSource = dt;
        drpItemGroups.DataTextField = "GroupName";
        drpItemGroups.DataValueField = "GroupCode";
        drpItemGroups.DataBind();

        drpItemGroups.DefaultItem.Text = "Select a Group";
        drpItemGroups.DefaultItem.Value = "0";
    }

    private void LoadBrands()
    {
        DataTable dt = new DataTable();

        string itmsgrpcods = "";

        try
        {
            string sql =
            @"select brand from
                (
                select 1 as sortorder, 'All Brands' brand 
                union
                select distinct 2 as sortorder, replace(u_brand, '''','_') brand 
                from " + sap_db + @".dbo.oitm " + Queries.WITH_NOLOCK + @" 
                where itmsgrpcod in (" + AnyPourpuse.GetSelectedItems(drpItemGroups) + @") 
                and u_brand is not null 
                ) a
              order by sortorder, brand";


            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadBrands. ERROR MESSAGE : " + ex.Message + "itmsgrpcods: " + itmsgrpcods);
        }
        lstItemGroups.DataSource = dt;
        lstItemGroups.DataBind();
    }

    protected void drpItemGroups_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        db.Connect();
        LoadBrands();
        db.Disconnect();
    }


    protected void buscarButton_Load(object sender, EventArgs e)
    {
        //{
        //    ArrayList roles = new ArrayList();
        //
        //    roles = (ArrayList)this.Session["Roles"];
        //
        //    string thiscontrol = null;
        //
        //    for (int i = 0; i < roles.Count; i++)
        //    {
        //        thiscontrol = (roles[i].ToString());
        //        if ((thiscontrol == "READONLY"))
        //        {
        //            buscarButton.Visible = false;
        //        }
        //
        //    }
        //}
    }
    
    protected void buscarButton_Click(object sender, EventArgs e)
    {
        //string ToWhsCode = drpToWhsCode.SelectedValue;
        string drpToWhsCodeSelected = "", ItemGroups = "", brands = "", drpCortesSelected = "";

        int v_LocCount = drpToWhsCode.CheckedItems.Count;

        if (v_LocCount == 0)
        {
            divMessage.InnerHtml = "Please select a Location";
            Alert.Show("Please select a Location");
            drpToWhsCode.Focus();
            return;
        }

        drpToWhsCodeSelected = AnyPourpuse.GetSelectedItems(drpToWhsCode);
        drpCortesSelected = AnyPourpuse.GetSelectedItems(drpCortes);

        bool goToRebind;
        string lTypeSearch = "";

        if (ArticuloTextBox.Text == null || ArticuloTextBox.Text == "")
        {
            goToRebind = true;

            if (drpItemGroups.CheckedItems.Count == 0)
            {
                divMessage.InnerHtml = "Please select at least one Category, or enter an Item Code";
                Alert.Show("Please select at least one Category, or enter an Item Code");
                drpItemGroups.Focus();
                return;
            }
            else
            {
                lTypeSearch = "GRP";
                ItemGroups = AnyPourpuse.GetSelectedItems(drpItemGroups);
            }

            //if (lstItemGroups.SelectedValue != "0")
            {
                foreach (ListItem li in lstItemGroups.Items)
                {
                    if (li.Selected)
                    {
                        brands += "'" + li.Value.ToString() + "',";
                    }
                }

                if (brands.Length > 0)
                {
                    brands = brands.Substring(0, brands.Length - 1);
                    lTypeSearch = "GRPBRD";
                    brandsLabel.Text = brands;
                }
            }
        }
        else
        {
            lTypeSearch = "ITEM";

            try
            {
                goToRebind = ValidateByBarCode(ArticuloTextBox.Text);
            }
            catch (Exception)
            {
                goToRebind = true;
            }
        }

        TypeSearchLabel.Text = lTypeSearch;

        if (goToRebind == true)
        {
            if (Session["RptdrpToWhsCode"].ToString() != drpToWhsCodeSelected
                || Session["RptdrpItemGroups"].ToString() != ItemGroups
                || Session["RptbrandsLabel"].ToString() != brandsLabel.Text
                || Session["RptArticuloTextBox"].ToString() != ArticuloTextBox.Text
                || Session["RptTypeSearchLabel"].ToString() != TypeSearchLabel.Text
                || Session["RptdrpCortes"].ToString() != drpCortesSelected
                || Session["CiaLabel"].ToString() != CompanyIdLabel.Text)
            {
                Reports rs = new Reports();
                DataTable dtData = rs.getOnhandItems(drpToWhsCodeSelected, ItemGroups, brandsLabel.Text, ArticuloTextBox.Text, TypeSearchLabel.Text, drpCortesSelected, CompanyIdLabel.Text);

                Session["RptData"] = dtData;
                Session["RptdrpToWhsCode"] = drpToWhsCodeSelected;
                Session["RptdrpItemGroups"] = ItemGroups;
                Session["RptbrandsLabel"] = brandsLabel.Text;
                Session["RptArticuloTextBox"] = ArticuloTextBox.Text;
                Session["RptTypeSearchLabel"] = TypeSearchLabel.Text;
                Session["RptdrpCortes"] = drpCortesSelected;
                Session["CiaLabel"] = CompanyIdLabel.Text;
            }

            if ((DataTable)Session["RptData"] != null)
            {
                GridView1.DataSource = (DataTable)Session["RptData"];
                GridView1.DataBind();
                GridView1.Enabled = true;
            }
        }
    }

    protected void rgHead_ExportCellFormatting(object sender, ExportCellFormattingEventArgs e)
    {
        if (e.FormattedColumn.UniqueName == "Codigo_Articulo")
        {
            e.Cell.Style["mso-number-format"] = @"\@";
        }
    }
    protected void rgHead_ExcelExportCellFormatting(object sender, ExcelExportCellFormattingEventArgs e)
    {
        if (e.FormattedColumn.UniqueName == "Codigo_Articulo")
        {
            e.Cell.Style["mso-number-format"] = @"\@";
        }
    }

    protected void btnExport_Click(object sender, EventArgs e)
    {
        try
        {
            if (GridView1.Rows.Count > 0)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("POS Code", typeof(string));
                dt.Columns.Add("Location", typeof(string));
                dt.Columns.Add("Item Code", typeof(string));
                dt.Columns.Add("Bar Code", typeof(string));
                dt.Columns.Add("Item Name", typeof(string));
                dt.Columns.Add("Category", typeof(string));
                dt.Columns.Add("Category Name", typeof(string));
                dt.Columns.Add("Brand", typeof(string));
                dt.Columns.Add("Class", typeof(string));
                dt.Columns.Add("Stock", typeof(string));

                foreach (GridViewRow row in GridView1.Rows)
                {
                    if (row.RowType == DataControlRowType.DataRow)
                    {
                        DataRow dr = dt.NewRow();
                        dr["POS Code"] = row.Cells[0].Text;
                        dr["Location"] = row.Cells[1].Text;
                        dr["Item Code"] = row.Cells[2].Text;
                        dr["Bar Code"] = row.Cells[3].Text;
                        dr["Item Name"] = row.Cells[4].Text;
                        dr["Category"] = row.Cells[5].Text;
                        dr["Category Name"] = row.Cells[6].Text;
                        dr["Brand"] = row.Cells[7].Text;
                        dr["Class"] = row.Cells[8].Text;
                        dr["Stock"] = row.Cells[9].Text;

                        dt.Rows.Add(dr);
                    }
                }
                if (dt.Rows.Count > 0)
                {
                    string attachment = "attachment; filename=SAPStock_" + dt.Rows[0]["Location"].ToString() + ".xls";
                    Response.ClearContent();
                    Response.AddHeader("content-disposition", attachment);
                    Response.ContentType = "application/vnd.ms-excel";
                    string tab = "";
                    foreach (DataColumn dc in dt.Columns)
                    {
                        Response.Write(tab + dc.ColumnName);
                        tab = "\t";
                    }
                    Response.Write("\n");
                    int i;
                    foreach (DataRow dr in dt.Rows)
                    {
                        tab = "";
                        for (i = 0; i < dt.Columns.Count; i++)
                        {
                            Response.Write(tab + dr[i].ToString());
                            tab = "\t";
                        }
                        Response.Write("\n");
                    }
                    Response.End();
                }
            }
            else
            {
                divMessage.InnerHtml = "No data to export";
            }
        }
        catch (Exception ex)
        {
            divMessage.InnerHtml = ex.Message.ToString();
        }
    }

    protected void rgHead_NeedDataSource(object sender, Telerik.Web.UI.GridNeedDataSourceEventArgs e)
    {
        //this.buscarButton_Click(null, null);
    }

    //2019-ABR-10: Agregado por Aldo Reina, para la búsqueda por código de barras:
    protected void ItemList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (ItemList.SelectedValue != "-")
        {
            ArticuloTextBox.Text = ItemList.SelectedValue;
            ArticuloTextBox.Visible = true;
            ItemList.Visible = false;
            rbtnCancel.Visible = false;
            buscarButton.Enabled = true;
            btnExport.Enabled = true;


            //string ItemGroups = "", brands = "";

            //int v_LocCount = drpToWhsCode.CheckedItems.Count;

            //if (v_LocCount == 0)
            //{
            //    divMessage.InnerHtml = "Please select a Location";
            //    Alert.Show("Please select a Location");
            //    drpToWhsCode.Focus();
            //    return;
            //}

            //string lTypeSearch = "";

            //if (ArticuloTextBox.Text == null || ArticuloTextBox.Text == "")
            //{
            //    if (drpItemGroups.SelectedValue == "0")
            //    {
            //        divMessage.InnerHtml = "Please select at least one Category, or enter an Item Code";
            //        Alert.Show("Please select at least one Category, or enter an Item Code");
            //        drpItemGroups.Focus();
            //        return;
            //    }
            //    else
            //    {
            //        lTypeSearch = "GRP";
            //        ItemGroups = AnyPourpuse.GetSelectedItems(drpItemGroups);
            //    }

            //    foreach (ListItem li in lstItemGroups.Items)
            //    {
            //        if (li.Selected)
            //        {
            //            brands += "'" + li.Value.ToString() + "',";
            //        }
            //    }

            //    if (brands.Length > 0)
            //    {
            //        brands = brands.Substring(0, brands.Length - 1);
            //        lTypeSearch = "GRPBRD";
            //        brandsLabel.Text = brands;
            //    }
            //}
            //else
            //{
            //    lTypeSearch = "ITEM";
            //}

            //TypeSearchLabel.Text = lTypeSearch;

            //Reports rs = new Reports();
            //DataTable dtData;

            //dtData = rs.getOnhandItems(AnyPourpuse.GetSelectedItems(drpToWhsCode), GetSelectedItems(drpItemGroups), brandsLabel.Text, ArticuloTextBox.Text, TypeSearchLabel.Text, GetSelectedItems(drpCortes), CompanyIdLabel.Text);
            //GridView1.DataSource = dtData;
            //GridView1.DataBind();
            //GridView1.Enabled = true;
        }
    }

    //2019-ABR-10: Agregado por Aldo Reina, para la búsqueda por código de barras:
    protected void RbtnCancel_Click(object sender, EventArgs e)
    {
        ItemList.Visible = false;
        rbtnCancel.Visible = false;
        buscarButton.Enabled = true;
        btnExport.Enabled = true;
        ArticuloTextBox.Text = "";
        ArticuloTextBox.Visible = true;
    }

    //2019-ABR-10: Agregado por Aldo Reina, para la búsqueda por código de barras:
    private bool ValidateByBarCode(string barCode)
    {
        bool res = false;
        DataTable dt = null;
        DataRow row;
        try
        {
            if(Session["CiaLabel"].ToString() != sap_db
                || Session["SearchItemByBarCodesarCode"].ToString() != barCode)
            {
                dt = db.SearchItemByBarCodes(sap_db, barCode);
                Session["SearchItemByBarCodesData"] = dt;
                Session["CiaLabel"] = sap_db;
                Session["SearchItemByBarCodesarCode"] = barCode;
            }

            if((DataTable)Session["SearchItemByBarCodesData"] != null)
            {
                dt = (DataTable)Session["SearchItemByBarCodesData"];
            }

            if (dt.Rows.Count <= 0)
            {
                ItemList.Visible = false;
                rbtnCancel.Visible = false;

                ArticuloTextBox.Visible = true;

                buscarButton.Enabled = true;
                btnExport.Enabled = true;

                //If the item is not found, just go on for the binding. Then, it won't show the
                //table if the item code provided is a bar code (probably user will faint here :D)
                //because no messages are showed here o.o!
                res = true;
            }
            else if (dt.Rows.Count == 1)
            {
                row = dt.Rows[0];
                ArticuloTextBox.Text = row["ItemCode"].ToString();
                ItemList.Visible = false;
                rbtnCancel.Visible = false;

                ArticuloTextBox.Visible = true;

                buscarButton.Enabled = true;
                btnExport.Enabled = true;

                //Here just go on to the bind function.
                res = true;
            }
            else
            {
                DataTable dTable = dt;
                DataRow dtRow = dTable.NewRow();
                dtRow["ItemCode"] = "-";
                dtRow["ItemName"] = "SELECCIONE ARTICULO";

                dt.Rows.InsertAt(dtRow, 0);

                ItemList.DataSource = dt;
                ItemList.DataMember = "ItemCode";
                ItemList.DataValueField = "ItemCode";
                ItemList.DataTextField = "ItemName";
                ItemList.DataBind();
                ItemList.Visible = true;
                rbtnCancel.Visible = true;

                ItemList.Width = 184;

                ItemList.Focus();
                ItemList.ToolTip = "SELECCIONE ARTICULO";

                ArticuloTextBox.Visible = false;

                buscarButton.Enabled = false;
                btnExport.Enabled = false;

                res = false;
            }
            return res;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}