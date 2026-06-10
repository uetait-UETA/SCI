using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using Telerik.Web.UI;


public partial class R_StoreMinMax3Multiple_ColSiTocNo : BasePage
{
    protected SqlDb db = new SqlDb();
    protected string usr;
    protected string Loc;
    protected string Item;
    protected string sap_db;
    protected string lCurUser; //Mod MINMAXUPDATE
    protected int newFirstItem = 0;
    protected int updMins = 0;

    protected void Page_Load(object sender, EventArgs e)
    {
        ValidaSesionNullOrEmpty(new string[] { "UserId", "CompanyId" });

        lCurUser = (string)this.Session["UserId"];
        sap_db = (string)this.Session["CompanyId"];

        ///////////////Begin New  Control de acceso por Roles
        string lControlName = "StoreMinMax3.aspx";
        string strAccessType = "";
        string strRole_Description = "";

        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, lControlName, ref strAccessType, ref strRole_Description);
        db.Disconnect();

	    if(strAccessType == "N")
        {
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
		    labelForm.InnerHtml = "Analysis of References Without Stock in Operation (Read-Only Access)"; //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
		}
		
	    if (strAccessType == "F")
		{
            labelForm.InnerHtml = "Analysis of References Without Stock in Operation (Full Access)"; //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
		}		
///////////////End  New Control de acceso por Roles

        string x = User.Identity.Name.ToLower();
        usr = lCurUser; // x.Substring(x.IndexOf("\\") + 1, x.Length - x.IndexOf("\\") - 1);

        divMessage.InnerHtml = "";

        if (!IsPostBack)
        {
            db.Connect();
            LoadWarehouses();
            LoadItemGroup();
            InitializeForm();
            db.Disconnect();
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

    private void InitializeForm()
    {
        divMessage.InnerHtml = "";
    }

    private void LoadWarehouses()
    {
        DataTable dt = new DataTable();

        try
        {
            int branchId = 0;
            int.TryParse(Session["BranchId"] as string, out branchId);
            string branchFilter = branchId > 0 ? " AND O.BPLid = " + branchId : "";
            string sql =
                   @"select O.WhsCode,
                            CONVERT(nvarchar(30), ISNULL(O.U_POSCode, '')) + ' - ' + O.WhsCode + ' - ' + O.WhsName AS WHS
                        from " + sap_db + @".dbo.owhs O, RSS_OWHS_CONTROL R
                        where O.WhsCode = R.WhsCode
                          and R.Control = 'SETMINMAX'
                          AND R.CompanyId = '" + sap_db + @"'" + branchFilter + @" ORDER BY CASE WHEN O.BPLId = 1 THEN 0 ELSE 1 END, O.U_POSCode";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadWarehouses. ERROR MESSAGE : " + ex.Message);
        }

        drpToWhsCode.DataSource = dt;
        drpToWhsCode.DataBind();

    }

    private void LoadItemGroup()
    {
        DataTable dt = new DataTable();

        try
        {
            string sql =
            @"select 
               ItmsGrpCod GroupCode, 
               cast(ItmsGrpCod as varchar) + ' - ' + ItmsGrpNam GroupName 
             from " + sap_db + @".dbo.oitb
             order by ItmsGrpCod ";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadItemGroup. ERROR MESSAGE : " + ex.Message);
        }

        DropDownItmGrp.DataSource = dt;
        DropDownItmGrp.DataBind();
    }

    private void LoadBrands()
    {
        DataTable dt = new DataTable();
        string itmsgrpcods = "";

        itmsgrpcods = GetCheckedCategories();

        if (itmsgrpcods.Length > 0)
        {
            itmsgrpcods = itmsgrpcods.Remove(itmsgrpcods.Length - 1);

            try
            {
                string sql =
                @"select brand, valor from
                (
                --select 1 as sortorder, 'Todo' brand, '-' AS valor
                --union 
                select distinct 2 as sortorder, (CONVERT(NVARCHAR(5), a.itmsgrpcod) + ' - ' + replace(a.u_brand, '''','_')) AS brand, replace(a.u_brand, '''','_') AS valor 
                from " + sap_db + @".dbo.oitm a " + Queries.WITH_NOLOCK + @"  
                inner join " + sap_db + @".dbo.oitb b " + Queries.WITH_NOLOCK + @" 
                    ON a.ItmsGrpCod = b.ItmsGrpCod
                where	a.itmsgrpcod in (" + itmsgrpcods + @") 
                and a.u_brand is not null 
                ) a
              order by brand";

                db.adapter = new SqlDataAdapter(sql, db.Conn);
                db.adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                throw new Exception("Caught exception in function LoadBrands. ERROR MESSAGE : " + ex.Message + "itmsgrpcods: " + itmsgrpcods);
            }

            lstItemGroups.Items.Clear();
            lstItemGroups.DataSource = dt;
            lstItemGroups.DataBind();
        }
    }

    protected void btnCreateWorksheet_Click(object sender, EventArgs e)
    {
        string lwhscode = "", ItemGroups = "", brands = "";

        lwhscode = GetCheckedStores();

        if (lwhscode.Length > 0)
        {
            lwhscode = lwhscode.Substring(0, lwhscode.Length - 1);
        }
        else
        {
            divMessage.InnerHtml = "Please select the Operation location.";
            lstItemGroups.DataBind();
            drpToWhsCode.Focus();
            return;
        }

        if (ItemTextBox.Text == "" || ItemTextBox.Text == null)
        {

            ItemGroups = GetCheckedCategories();

            if (ItemGroups.Length > 0)
            {
                ItemGroups = ItemGroups.Substring(0, ItemGroups.Length - 1);
            }
            else
            { 
                divMessage.InnerHtml = "Please select a Category or an Item.";
                lstItemGroups.DataBind();
                DropDownItmGrp.Focus();
                return;
            }

            brands = GetCheckedBrands();

            if (brands.Length > 0)
            {
                brands = brands.Substring(0, brands.Length - 1);
            }
            else
            {
                divMessage.InnerHtml = "Please select a Brand or an Item.";
                lstItemGroups.DataBind();
                lstItemGroups.Focus();
                return;
            }
        }
        else //Do the search by bar code if the user type something in "Busqueda por Producto" field.
        {
            DataTable dt = db.SearchItemByBarCodes(sap_db, ItemTextBox.Text);
            DataRow row;
            if (dt.Rows.Count <= 0)
            {
                ItemList.Visible = false;
                rbtnCancel.Visible = false;
            }
            else if (dt.Rows.Count == 1)
            {
                row = dt.Rows[0];
                ItemTextBox.Text = row["ItemCode"].ToString();
                ItemList.Visible = false;
                rbtnCancel.Visible = false;
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
                rbtnCancel.Visible = true;
                GridView1.Visible = false;
                return;
            }
        }

        newFirstItem = 0;

        bindGridView1(lwhscode, ItemGroups, brands);

        newFirstItem = 0;

        lstItemGroups.DataBind();
    }

    private string GetCheckedStores()
    {
        string res = "";

        if (drpToWhsCode.CheckBoxes)
        {
            foreach (RadComboBoxItem li in drpToWhsCode.CheckedItems)
            {
                res += "'" + li.Value.ToString() + "',";
            }
        }
        else
        {
            if (!String.IsNullOrEmpty(drpToWhsCode.SelectedValue))
            {
                res = " '" + drpToWhsCode.SelectedValue.ToString() + "' ";
            }
        }

        return res;
    }

    private string GetCheckedCategories()
    {
        string res = "";
        if (DropDownItmGrp.CheckBoxes)
        {
            foreach (RadComboBoxItem li in DropDownItmGrp.CheckedItems)
            {
                res += li.Value.ToString() + ",";
            }
        }
        else
        {
            if (!String.IsNullOrEmpty(DropDownItmGrp.SelectedValue))
            {
                res = DropDownItmGrp.SelectedValue.ToString();
            }
        }

        return res;
    }

    private string GetCheckedBrands()
    {
        string res = "";

        if (lstItemGroups.CheckBoxes)
        {
            foreach (RadListBoxItem li in lstItemGroups.CheckedItems)
            {
                res += "'" + li.Value.ToString() + "',";
            }
        }
        else
        {
            if (!String.IsNullOrEmpty(lstItemGroups.SelectedValue))
            {
                res = "'" + lstItemGroups.SelectedValue.ToString() + "'";
            }
        }

        return res;
    }

    protected void bindGridView1(string stores, string itmsgrpcods, string brands)
    {
        ObjectDataSource1.Dispose();

        if (stores.Length > 0)
        {
            ObjectDataSource1.SelectParameters["store"].DefaultValue = stores;
        }

        if (itmsgrpcods.Length > 0)
        {
            ObjectDataSource1.SelectParameters["depts"].DefaultValue = itmsgrpcods;
        }

        if (brands.Length > 0)
        {
            ObjectDataSource1.SelectParameters["brands"].DefaultValue = brands;
        }

        ObjectDataSource1.SelectParameters["displayAll"].DefaultValue = "true";
        ObjectDataSource1.SelectParameters["NoPlanned"].DefaultValue = "";
        ObjectDataSource1.SelectParameters["NoInSap"].DefaultValue = "";
        ObjectDataSource1.SelectParameters["Item"].DefaultValue = ItemTextBox.Text;
        ObjectDataSource1.SelectParameters["companyId"].DefaultValue = sap_db;

        GridView1.DataBind();
        GridView1.Visible = true;

        lstItemGroups.DataBind();
    }

    protected void AppendMessage(string msg)
    {
        divMessage.InnerHtml += msg;
    }

    protected void ObjectDataSource1_Selected(object sender, ObjectDataSourceStatusEventArgs e)
    {
        // bubble exceptions before we touch e.ReturnValue
        if (e.Exception != null) throw e.Exception;

        // get the DataTable from the ODS select method
        if (e.ReturnValue != null)
        {
            DataTable dt = (DataTable)e.ReturnValue;
            if (dt.Rows.Count > 0)
            {
                string order_multiple = dt.Rows[0]["order_multiple"].ToString();

                if (order_multiple == "C")
                {
                    divMessage.InnerHtml += "QUANTITIES IN BOXES!!";
                }
            }
        }
    }

    protected void DropDownItmGrp_RadSelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        LoadBrands();
    }

    protected void btnExport_Click(object sender, EventArgs e)
    {
        try
        {
            if (GridView1.Items.Count > 0)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("Loc", typeof(string));
                dt.Columns.Add("Loc Name", typeof(string));
                dt.Columns.Add("Marca", typeof(string));
                dt.Columns.Add("Item", typeof(string));
                dt.Columns.Add("Description", typeof(string));
                dt.Columns.Add("Case Pack", typeof(string));
                dt.Columns.Add("Min", typeof(string));
                dt.Columns.Add("Max", typeof(string));
                dt.Columns.Add("Replacement Item", typeof(string));
                dt.Columns.Add("Comment", typeof(string));
                dt.Columns.Add("ColonOnHand", typeof(string));

                DataRow dr;

                foreach (GridDataItem row in GridView1.Items)
                {
                    HiddenField hdnLoc = (HiddenField)row.FindControl("hdnLoc");
                    HiddenField hdnLocName = (HiddenField)row.FindControl("hdnLocName");
                    HiddenField hdnItem = (HiddenField)row.FindControl("hdnItem");
                    HiddenField hdnItemDesc = (HiddenField)row.FindControl("hdnItemDesc");
                    HiddenField hdnCasePack = (HiddenField)row.FindControl("hdnCasePack");
                    HiddenField hdnMarca = (HiddenField)row.FindControl("hdnMarca");
                    HiddenField hdnMin = (HiddenField)row.FindControl("hdnMin");
                    HiddenField hdnMax = (HiddenField)row.FindControl("hdnMax");
                    HiddenField hdnReplacementItem = (HiddenField)row.FindControl("hdnReplacementItem");
                    HiddenField hdnComment = (HiddenField)row.FindControl("hdnComment");
                    HiddenField hdnColonOnHand = (HiddenField)row.FindControl("hdnColonOnHand");

                    dr = dt.NewRow();
                    dr["Loc"] = hdnLoc.Value;
                    dr["Loc Name"] = hdnLocName.Value;
                    dr["Marca"] = hdnMarca.Value;
                    dr["Item"] = hdnItem.Value;
                    dr["Description"] = hdnItemDesc.Value;
                    dr["Case Pack"] = hdnCasePack.Value;
                    dr["Min"] = hdnMin.Value;
                    dr["Max"] = hdnMax.Value;
                    dr["Replacement Item"] = hdnReplacementItem.Value;
                    dr["Comment"] = hdnComment.Value;
                    dr["ColonOnHand"] = hdnColonOnHand.Value;

                    dt.Rows.Add(dr);
                }

                if (dt.Rows.Count > 0)
                {
                    string attachment = "attachment; filename=MinMaxMulti_ItemsSinExistencia_Export" + ".xls";
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
                    foreach (DataRow dr1 in dt.Rows)
                    {
                        tab = "";
                        for (i = 0; i < dt.Columns.Count; i++)
                        {
                            Response.Write(tab + dr1[i].ToString());
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

    protected void ItemList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (ItemList.SelectedValue != "-")
        {
            ItemTextBox.Text = ItemList.SelectedValue;
            ItemList.Visible = false;
            rbtnCancel.Visible = false;

            string lwhscode = "", ItemGroups = "", brands = "";

            lwhscode = GetCheckedStores();

            if (lwhscode.Length > 0)
            {
                lwhscode = lwhscode.Substring(0, lwhscode.Length - 1);
            }
            else
            {
                divMessage.InnerHtml = "Please select the Operation location.";
                lstItemGroups.DataBind();
                drpToWhsCode.Focus();
                return;
            }

            if (ItemTextBox.Text == "" || ItemTextBox.Text == null)
            {
                ItemGroups = GetCheckedCategories();

                if (ItemGroups.Length > 0)
                {
                    ItemGroups = ItemGroups.Substring(0, ItemGroups.Length - 1);
                }
                else
                {
                    divMessage.InnerHtml = "Please select a Category or an Item.";
                    lstItemGroups.DataBind();
                    DropDownItmGrp.Focus();
                    return;
                }

                brands = GetCheckedBrands();

                if (brands.Length > 0)
                {
                    brands = brands.Substring(0, brands.Length - 1);
                }
                else
                {
                    divMessage.InnerHtml = "Please select a Brand or an Item.";
                    lstItemGroups.DataBind();
                    lstItemGroups.Focus();
                    return;
                }
            }

            newFirstItem = 0;

            bindGridView1(lwhscode, ItemGroups, brands);

            newFirstItem = 0;

            lstItemGroups.DataBind();
        }
    }

    protected void RbtnCancel_Click(object sender, EventArgs e)
    {
        ItemList.Visible = false;
        rbtnCancel.Visible = false;
        ItemTextBox.Text = "";
    }
}
