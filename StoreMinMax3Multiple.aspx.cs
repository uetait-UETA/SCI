using System;
using System.Collections; //Mod MINMAXUPDATE
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;
using System.IO;
using Telerik;
using Telerik.Web.UI;
using System.Drawing;


public partial class StoreMinMax3Multiple : BasePage
{
    protected SqlDb db = new SqlDb();
    protected string usr;
    protected string Loc;
    protected string Item;
    protected string sap_db;
    protected string lCurUser; //Mod MINMAXUPDATE
    protected int newFirstItem = 0;
    protected int updMins = 0;
    protected string lControlName = "StoreMinMax3.aspx";
    protected DataTable ciaWhs;

    protected void Page_Init(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty((string)Session["CompanyId"]))
        {
            Response.Redirect("Login1.aspx");
        }

        sap_db = (string)Session["CompanyId"];

        int branchId = 0;
        int.TryParse(Session["BranchId"] as string, out branchId);

        db.Connect();
        ciaWhs = db.GetWhsByCiaIdAndControl(sap_db, "SETMINMAX", "BODEGA", branchId);
        db.Disconnect();

        GridTemplateColumn tCol = (GridTemplateColumn)GridView1.MasterTableView.Columns.FindByUniqueName("TemplateColumn1");
        tCol.ItemTemplate = new WhsItemTemplate(ciaWhs);

        foreach (DataRow whs in ciaWhs.Rows)
        {
            string wc = (string)whs["WhsCode"];
            GridBoundColumn gCol = new GridBoundColumn
            {
                HeaderText = wc,
                HeaderButtonType = GridHeaderButtonType.TextButton,
                SortExpression = "WHS_" + wc,
                DataField = wc,
                UniqueName = "WHS_" + wc,
                DataFormatString = "{0:N0}",
                ColumnGroupName = "WhsOnHand",
                Display = true
            };
            gCol.HeaderStyle.Width = new Unit(80.00, UnitType.Pixel);
            GridView1.MasterTableView.Columns.Add(gCol);
        }
    }

    private void BuildWhsColumns()
    {
        int branchId = 0;
        int.TryParse(Session["BranchId"] as string, out branchId);

        db.Connect();
        ciaWhs = db.GetWhsByCiaIdAndControl(sap_db, "SETMINMAX", "BODEGA", branchId);
        db.Disconnect();

        var firstOccurrenceIdx = new Dictionary<string, int>();
        var indicesToRemove = new List<int>();

        for (int i = 0; i < GridView1.MasterTableView.Columns.Count; i++)
        {
            string un = GridView1.MasterTableView.Columns[i].UniqueName ?? "";
            if (!un.StartsWith("WHS_")) continue;

            if (firstOccurrenceIdx.ContainsKey(un))
                indicesToRemove.Add(i);
            else
                firstOccurrenceIdx[un] = i;
        }

        for (int i = indicesToRemove.Count - 1; i >= 0; i--)
            GridView1.MasterTableView.Columns.RemoveAt(indicesToRemove[i]);

        foreach (DataRow whs in ciaWhs.Rows)
        {
            string wc = (string)whs["WhsCode"];
            string uniqueName = "WHS_" + wc;

            GridBoundColumn existing = (GridBoundColumn)GridView1.MasterTableView.Columns.FindByUniqueName(uniqueName);

            if (existing != null)
            {
                existing.DataField = wc;
                existing.HeaderText = wc;
                existing.DataFormatString = "{0:N0}";
                existing.ColumnGroupName = "WhsOnHand";
                existing.Display = true;
                existing.HeaderStyle.Width = new Unit(80.00, UnitType.Pixel);
            }
            else
            {
                GridBoundColumn gCol = new GridBoundColumn
                {
                    HeaderText = wc,
                    HeaderButtonType = GridHeaderButtonType.TextButton,
                    SortExpression = "WHS_" + wc,
                    DataField = wc,
                    UniqueName = "WHS_" + wc,
                    DataFormatString = "{0:N0}",
                    ColumnGroupName = "WhsOnHand",
                    Display = true
                };
                gCol.HeaderStyle.Width = new Unit(80.00, UnitType.Pixel);
                GridView1.MasterTableView.Columns.Add(gCol);
            }
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        //Mod MINMAXUPDATE >>>
        lCurUser = (string)this.Session["UserId"];

        if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

///////////////Begin New  Control de acceso por Roles
	db.Connect();
	 //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

	db.cmd.Parameters.Clear();
	db.cmd.CommandText = "dbo.SISINV_GET_ACCESSTYPE_PRC";
	db.cmd.CommandType = CommandType.StoredProcedure;
	db.cmd.Connection = db.Conn;

	db.cmd.Parameters.Add(new SqlParameter("@LoginID", SqlDbType.VarChar));
	db.cmd.Parameters["@LoginID"].Value = lCurUser;

	db.cmd.Parameters.Add(new SqlParameter("@ControlName", SqlDbType.VarChar));
	db.cmd.Parameters["@ControlName"].Value = lControlName; 

	SqlParameter lAccessType = new SqlParameter("@AccessType", SqlDbType.VarChar);
	lAccessType.Direction = ParameterDirection.Output;
	lAccessType.Size = 100000;
	db.cmd.Parameters.Add(lAccessType);


	string strAccessType = "";
	
	SqlParameter lRole_Description = new SqlParameter("@Role_Description", SqlDbType.VarChar);
	lRole_Description.Direction = ParameterDirection.Output;
	lRole_Description.Size = 100000;
	db.cmd.Parameters.Add(lRole_Description);
	
	
	string strRole_Description = "";



	try
	{
	    db.cmd.ExecuteNonQuery();
	    strAccessType       = db.cmd.Parameters["@AccessType"].Value.ToString();
	    strRole_Description = db.cmd.Parameters["@Role_Description"].Value.ToString();
	}

	catch (Exception ex)
	{
	    throw new Exception("Error when SISINV_GET_ACCESSTYPE_PRC was called: " + ex.Message);
	}

	db.Conn.Close();

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
		    labelForm.InnerHtml = "Minimum-Maximum by Category and Brand (Read-Only Access)"; //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
		}
		
	if (strAccessType == "F")
		{
            labelForm.InnerHtml = "Minimum-Maximum by Category and Brand (Full Access)"; //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
		}		


///////////////End  New Control de acceso por Roles


        //Mod MINMAXUPDATE  <<<<
        sap_db = (string)this.Session["CompanyId"];//ConfigurationSettings.AppSettings.Get("sap_db");

        string x = User.Identity.Name.ToLower();
        usr = (string)this.Session["UserId"]; // x.Substring(x.IndexOf("\\") + 1, x.Length - x.IndexOf("\\") - 1);

        BuildWhsColumns();

        db.Connect();
        divMessage.InnerHtml = "";

        if (!IsPostBack)
        {
            LoadWarehouses();
            LoadItemGroup();
            InitializeForm();
            db.Conn.Close();
        }
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
                --select 1 as sortorder, 'Todas las marcas' brand, '-' AS valor
                --union 
                select distinct 2 as sortorder, (CONVERT(NVARCHAR(5), a.itmsgrpcod) + ' - ' + replace(a.u_brand, '''','_')) AS brand, replace(a.u_brand, '''','_') AS valor 
                from " + sap_db + @".dbo.oitm a WITH (NOLOCK) 
                inner join " + sap_db + @".dbo.oitb b WITH (NOLOCK) 
                    ON a.ItmsGrpCod = b.ItmsGrpCod
                where	a.itmsgrpcod in (" + itmsgrpcods + @") 
                and a.u_brand is not null 
                ) a
              order by sortorder, brand";

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

        if (newFirstItem > 0)
        {
            GreenLabel.Visible = true;
        }
        else
        {
            GreenLabel.Visible = false;
        }

        newFirstItem = 0;

        lstItemGroups.DataBind();
    }

    protected void bindGridView1(string stores, string itmsgrpcods, string brands)
    {
        string displayAll = "", NoPlanned = "", NoInSap = "";

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

        displayAll = (radioAllItems.Checked ? "true" : "false");
        ObjectDataSource1.SelectParameters["displayAll"].DefaultValue = displayAll;

        NoPlanned = (NotMinMaxPlannedCheckBox.Checked ? "true" : "false");
        ObjectDataSource1.SelectParameters["NoPlanned"].DefaultValue = NoPlanned;

        NoInSap = (NotInSapCheckBox.Checked ? "true" : "false");
        ObjectDataSource1.SelectParameters["NoInSap"].DefaultValue = NoInSap;

        ObjectDataSource1.SelectParameters["Item"].DefaultValue = ItemTextBox.Text;

        ObjectDataSource1.SelectParameters["companyId"].DefaultValue = sap_db;

        ObjectDataSource1.SelectParameters["control"].DefaultValue = "SETMINMAX";

        ObjectDataSource1.SelectParameters["whsTypes"].DefaultValue = "BODEGA";

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

    //protected void DropDownItmGrp_SelectedIndexChanged(object sender, EventArgs e)
    //{
    //    LoadBrands();
    //}
    protected void DropDownItmGrp_DataBound(object sender, EventArgs e)
    {
        //LoadBrands();
    }
    protected void Button1_Click(object sender, EventArgs e)
    {
        //LoadBrands();
    }
    protected void DropDownItmGrp_TextChanged(object sender, EventArgs e)
    {
        //LoadBrands();
    }
    protected void DropDownItmGrp_DataBinding(object sender, EventArgs e)
    {
        //LoadBrands();
    }
    protected void DropDownItmGrp_Load(object sender, EventArgs e)
    {
        //LoadBrands();
    }
    protected void DropDownItmGrp_PreRender(object sender, EventArgs e)
    {
        //LoadBrands();
    }
    protected void NotMinMaxPlannedCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        newFirstItem = 0;
        string lwhscode = "", ItemGroups = "", brands = "";

        lwhscode = GetCheckedStores();
        if (lwhscode.Length > 0)
        {
            lwhscode = lwhscode.Substring(0, lwhscode.Length - 1);
        }

        ItemGroups = GetCheckedCategories();
        if (ItemGroups.Length > 0)
        {
            ItemGroups = ItemGroups.Substring(0, ItemGroups.Length - 1);
        }
        
        brands = GetCheckedBrands();
        if (brands.Length > 0)
        {
            brands = brands.Substring(0, brands.Length - 1);
        }

        bindGridView1(lwhscode, ItemGroups, brands);

        if (newFirstItem > 0)
        {
            GreenLabel.Visible = true;
        }
        else
        {
            GreenLabel.Visible = false;
        }

        newFirstItem = 0;
    }
    protected void NotInSapCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        string lwhscode = "", ItemGroups = "", brands = "";

        lwhscode = GetCheckedStores();
        if (lwhscode.Length > 0)
        {
            lwhscode = lwhscode.Substring(0, lwhscode.Length - 1);
        }

        ItemGroups = GetCheckedCategories();
        if (ItemGroups.Length > 0)
        {
            ItemGroups = ItemGroups.Substring(0, ItemGroups.Length - 1);
        }

        brands = GetCheckedBrands();
        if (brands.Length > 0)
        {
            brands = brands.Substring(0, brands.Length - 1);
        }
        bindGridView1(lwhscode, ItemGroups, brands);
    }

    protected void btnExport_Click(object sender, EventArgs e)
    {
        try
        {
            if (GridView1.Items.Count > 0)
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("Loc", typeof(string));
                dt.Columns.Add("Marca", typeof(string));
                dt.Columns.Add("Item", typeof(string));
                dt.Columns.Add("Description", typeof(string));
                dt.Columns.Add("Case Pack", typeof(string));
                dt.Columns.Add("OnHand", typeof(string));
                dt.Columns.Add("Min", typeof(string));
                dt.Columns.Add("Max", typeof(string));
                dt.Columns.Add("Replacement Item", typeof(string));
                dt.Columns.Add("Comment", typeof(string));

                foreach (DataRow whs in ciaWhs.Rows)
                    dt.Columns.Add((string)whs["WhsCode"], typeof(string));

                foreach (GridDataItem row in GridView1.Items)
                {
                    HiddenField hdnLoc = (HiddenField)row.FindControl("hdnLoc");
                    HiddenField hdnMarca = (HiddenField)row.FindControl("hdnMarca");
                    HiddenField hdnItem = (HiddenField)row.FindControl("hdnItem");
                    HiddenField hdnItemDesc = (HiddenField)row.FindControl("hdnItemDesc");
                    HiddenField hdnCasePack = (HiddenField)row.FindControl("hdnCasePack");
                    HiddenField hdnOnHand = (HiddenField)row.FindControl("hdnOnHand");
                    HiddenField hdnMin = (HiddenField)row.FindControl("hdnMin");
                    HiddenField hdnMax = (HiddenField)row.FindControl("hdnMax");
                    HiddenField hdnReplacementItem = (HiddenField)row.FindControl("hdnReplacementItem");
                    HiddenField hdnComment = (HiddenField)row.FindControl("hdnComment");

                    DataRow dr = dt.NewRow();
                    dr["Loc"] = hdnLoc != null ? hdnLoc.Value : "";
                    dr["Marca"] = hdnMarca != null ? hdnMarca.Value : "";
                    dr["Item"] = hdnItem != null ? hdnItem.Value : "";
                    dr["Description"] = hdnItemDesc != null ? hdnItemDesc.Value : "";
                    dr["Case Pack"] = hdnCasePack != null ? hdnCasePack.Value : "";
                    dr["OnHand"] = hdnOnHand != null ? hdnOnHand.Value : "";
                    dr["Min"] = hdnMin != null ? hdnMin.Value : "";
                    dr["Max"] = hdnMax != null ? hdnMax.Value : "";
                    dr["Replacement Item"] = hdnReplacementItem != null ? hdnReplacementItem.Value : "";
                    dr["Comment"] = hdnComment != null ? hdnComment.Value : "";

                    foreach (DataRow whs in ciaWhs.Rows)
                    {
                        string wc = (string)whs["WhsCode"];
                        HiddenField hdnWhs = (HiddenField)row.FindControl("hdn" + wc);
                        if (hdnWhs != null) dr[wc] = hdnWhs.Value;
                    }

                    dt.Rows.Add(dr);
                }

                if (dt.Rows.Count > 0)
                {
                    string attachment = "attachment; filename=MinMaxMultExport_" + dt.Rows[0]["Loc"].ToString() + ".xls";
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

            if (newFirstItem > 0)
            {
                GreenLabel.Visible = true;
            }
            else
            {
                GreenLabel.Visible = false;
            }

            newFirstItem = 0;

            lstItemGroups.DataBind();

        }
    }

    private string GetCheckedStores()
    {
        string res = "";

        if(drpToWhsCode.CheckBoxes)
        {
            foreach (RadComboBoxItem li in drpToWhsCode.CheckedItems)
            {
                res += "'" + li.Value.ToString() + "',";
            }
        }
        else
        {
            if(!String.IsNullOrEmpty(drpToWhsCode.SelectedValue))
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

        if(lstItemGroups.CheckBoxes)
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

    protected void RbtnCancel_Click(object sender, EventArgs e)
    {
        ItemList.Visible = false;
        rbtnCancel.Visible = false;
        ItemTextBox.Text = "";
    }
}

public class WhsItemTemplate : ITemplate
{
    private readonly DataTable ciaWhs;

    private static readonly Dictionary<string, string> s_map = new Dictionary<string, string>
    {
        { "hdnLoc",             "LOC" },
        { "hdnItem",            "ITEM" },
        { "hdnItemDesc",        "ITEMNAME" },
        { "hdnCasePack",        "CASE_PACK" },
        { "hdnOnHand",          "OnHand" },
        { "hdnMarca",           "marca" },
        { "hdnMin",             "MIN_QTY" },
        { "hdnMax",             "MAX_QTY" },
        { "hdnReplacementItem", "replacement_item" },
        { "hdnComment",         "COMMENT" }
    };

    public WhsItemTemplate(DataTable ciaWhs)
    {
        this.ciaWhs = ciaWhs;
    }

    public void InstantiateIn(Control container)
    {
        Label lbl = new Label { ID = "lblItem" };
        lbl.DataBinding += Label_DataBinding;
        container.Controls.Add(lbl);

        foreach (string hdnId in s_map.Keys)
        {
            HiddenField hf = new HiddenField { ID = hdnId };
            hf.DataBinding += StaticHidden_DataBinding;
            container.Controls.Add(hf);
        }

        foreach (DataRow whs in ciaWhs.Rows)
        {
            HiddenField hf = new HiddenField { ID = "hdn" + (string)whs["WhsCode"] };
            hf.DataBinding += WhsHidden_DataBinding;
            container.Controls.Add(hf);
        }
    }

    private void Label_DataBinding(object sender, EventArgs e)
    {
        try
        {
            Label lbl = (Label)sender;
            GridDataItem container = (GridDataItem)lbl.NamingContainer;
            lbl.Text = ((DataRowView)container.DataItem)["item"].ToString().Trim();
        }
        catch { }
    }

    private void StaticHidden_DataBinding(object sender, EventArgs e)
    {
        try
        {
            HiddenField field = (HiddenField)sender;
            GridDataItem container = (GridDataItem)field.NamingContainer;
            DataRowView drv = (DataRowView)container.DataItem;
            string colName = s_map[field.ID];
            if (drv.Row.Table.Columns.Contains(colName))
                field.Value = drv[colName].ToString().Trim();
        }
        catch { }
    }

    private void WhsHidden_DataBinding(object sender, EventArgs e)
    {
        try
        {
            HiddenField field = (HiddenField)sender;
            string colName = field.ID.Replace("hdn", "");
            GridDataItem container = (GridDataItem)field.NamingContainer;
            DataRowView drv = (DataRowView)container.DataItem;
            if (drv.Row.Table.Columns.Contains(colName))
                field.Value = drv[colName].ToString();
        }
        catch { }
    }
}
