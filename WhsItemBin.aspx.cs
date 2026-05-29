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

public partial class WhsItemBin : System.Web.UI.Page
{
    protected SqlDb db = new SqlDb();
    protected string usr; 
    protected string Loc;
    protected string Item;
    protected string sap_db;
    protected string lCurUser; //Mod MINMAXUPDATE
    protected int newFirstItem = 0;

    protected void Page_Load(object sender, EventArgs e)
    {
        //Mod MINMAXUPDATE >>>

        lCurUser = (string)this.Session["UserId"];

        if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

        ArrayList controles = new ArrayList();
        controles = (ArrayList)this.Session["Controles"];

        ArrayList roles = new ArrayList();
        roles = (ArrayList)this.Session["Roles"];

        ArrayList permissions = new ArrayList();
        permissions = (ArrayList)this.Session["Permissions"];

        string thisrole = "";
        char flagokay = 'N';

        for (int i = 0; i < roles.Count; i++)
        {
            thisrole = (roles[i].ToString());
            if ((thisrole == "ATOTAL"))
            {
                flagokay = 'Y';
            }

        }

        if (flagokay == 'N')
        {
            string thiscontrol = "";

            for (int i = 0; i < controles.Count; i++)
            {
                thiscontrol = (controles[i].ToString());
                if ((thiscontrol == "WhsItemBin.aspx") || (thiscontrol == "ATOTAL"))
                {
                    flagokay = 'Y';
                }

            }
        }
        

        if (flagokay == 'N')
        {

            //Response.Write("<script type=\"text/javascript\">alert('" + "This User does not have permissions for this option, please log in with another user." + "');</script>");
            //Response.End();
            Session["FlagNoPerPag"] = "N";
            Response.Redirect("Login1.aspx");
            return;
        }


        //Mod MINMAXUPDATE  <<<<
        sap_db = (string)Session["CompanyId"];
        //sap_db = ConfigurationSettings.AppSettings.Get("sap_db");

        //usr = User.Identity.Name.ToLower().Replace("lgihome\\", "");
        string x = User.Identity.Name.ToLower();
        usr = x.Substring(x.IndexOf("\\") + 1, x.Length - x.IndexOf("\\") - 1);

        db.Connect();
        divMessage.InnerHtml = "";

        if (!IsPostBack)
        {
            LoadWarehouses();
            LoadItemGroup();
            //LoadBrands();
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
            object val = Session["BranchId"];
            if (val != null && val.ToString() != "")
                branchId = Convert.ToInt32(val);
            string branchFilter = branchId > 0 ? " AND O.BPLId = " + branchId : "";

            string sql =
                  @"SELECT O.WhsCode,
                         CONVERT(nvarchar(30), ISNULL(O.U_POSCode, '')) + ' - ' + O.WhsCode + ' - ' + O.WhsName AS WHS
                     FROM " + sap_db + @".dbo.OWHS O
                     WHERE 1=1" + branchFilter + @"
                     ORDER BY O.U_POSCode, O.WhsCode";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadWarehouses. ERROR MESSAGE : " + ex.Message);
        }

        drpToWhsCode.DataSource = dt;
        drpToWhsCode.DataBind();

        ListItem li = new ListItem("Select a location", "0");
        drpToWhsCode.Items.Insert(0, li);
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
             where 
                ItmsGrpCod not in (100, 112)
            order by ItmsGrpCod";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadItemGroup. ERROR MESSAGE : " + ex.Message);
        }

        DropDownItmGrp.DataSource = dt;
        DropDownItmGrp.DataBind();

        ListItem li = new ListItem("Select a category", "0");

        DropDownItmGrp.Items.Insert(0, li);

    }

    private void LoadBrands()
    {
        DataTable dt = new DataTable();


        string itmsgrpcods = ""; //, displayAll = "";

        foreach (ListItem li in DropDownItmGrp.Items)
        {
            if (li.Selected)
            {
                //itmsgrpcods += li.Value.ToString() + ",";
                itmsgrpcods = itmsgrpcods + li.Value.ToString() + ",";
            }
        }

        itmsgrpcods = itmsgrpcods.Remove(itmsgrpcods.Length - 1);

        try
        {
            string sql =
      @"select brand from
                (
                select 1 as sortorder, 'All Brands' brand 
                union
                select distinct 2 as sortorder, replace(u_brand, '''','_') brand 
                from " + sap_db + @".dbo.oitm 
                where	itmsgrpcod in (" + itmsgrpcods + @") 
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

        //foreach (ListItem li in lstItemGroups.Items)
        //{
        //    li.Selected = true;
        //}

    }


    protected void btnSelectAll_Click(object sender, EventArgs e)
    {
        foreach (ListItem li in lstItemGroups.Items)
        {
            li.Selected = true;
        }
    }

    protected void btnCreateWorksheet_Click(object sender, EventArgs e)
    {


        bindGridView1();

  
    }

    protected void bindGridView1()
    {
        string itmsgrpcods = "", brands = "";

        foreach (ListItem di in DropDownItmGrp.Items)
        {
            if (di.Selected)
            {
                //itmsgrpcods += li.Value.ToString() + ",";
                itmsgrpcods = itmsgrpcods + di.Value.ToString() + ",";
            }
        }

        if (itmsgrpcods.Length > 0)
        {
            itmsgrpcods = itmsgrpcods.Remove(itmsgrpcods.Length - 1);
            ObjectDataSource1.SelectParameters["depts"].DefaultValue = itmsgrpcods;
        }


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
            ObjectDataSource1.SelectParameters["brands"].DefaultValue = brands;
        }

        
        GridView1.DataBind();

    }

    protected void btnSaveChanges_Click(object sender, EventArgs e)
    {
        string msg = "";
        int dummy = 0;
        string actionPerm = "0";
        string lLoc = drpToWhsCode.Text.ToString();
        string lCategory = lstItemGroups.Text.ToString();


        
        string lItmsgrpcods = "";

        int ii = 0;

        foreach (ListItem li in DropDownItmGrp.Items)
        {



            if (li.Selected)
            {
                ii++;
                if (ii == 1)
                {
                    //itmsgrpcods += li.Value.ToString() + ",";
                    lItmsgrpcods = lItmsgrpcods + li.Value.ToString();
                }
                else
                {
                    //itmsgrpcods += li.Value.ToString() + ",";
                    lItmsgrpcods = lItmsgrpcods + "," + li.Value.ToString();
                }
            }
        }

        try
        {

            db.cmd.Parameters.Clear();
            db.cmd.CommandText = "SMM_UACTIONS_CNT";
            db.cmd.CommandType = CommandType.StoredProcedure;
            db.cmd.Connection = db.Conn;

            db.cmd.Parameters.Add(new SqlParameter("@LoginId", SqlDbType.NVarChar));
            db.cmd.Parameters["@LoginId"].Value = lCurUser;

            db.cmd.Parameters.Add(new SqlParameter("@Action", SqlDbType.NVarChar));
            db.cmd.Parameters["@Action"].Value = "BINUPDATE";

            db.cmd.Parameters.Add(new SqlParameter("@WhsCode", SqlDbType.NVarChar));
            db.cmd.Parameters["@WhsCode"].Value = lLoc;

            db.cmd.Parameters.Add(new SqlParameter("@Category", SqlDbType.NVarChar));
            db.cmd.Parameters["@Category"].Value = lItmsgrpcods;

            //db.cmd.ExecuteNonQuery();

            SqlDataAdapter storeSqlDataAdapter = new SqlDataAdapter();
            storeSqlDataAdapter.SelectCommand = db.cmd;
            DataSet storeDataSet = new DataSet();
            storeSqlDataAdapter.Fill(storeDataSet, "CNT");
            DataTable storeDataTable = storeDataSet.Tables["CNT"];

            foreach (DataRow errorRow in storeDataTable.Rows)
            {
                actionPerm = errorRow["CNT"].ToString();
            }

        }
        catch (Exception ex)
        {
            db.Conn.Close();
            throw new Exception("Caught exception in call procedure SMM_UACTIONS_CNT. ERROR MESSAGE : " + ex.Message);
        }



        if (actionPerm == "0")
        {

            Response.Write("<script type=\"text/javascript\">alert('" + "User " + lCurUser + " does not have permissions to modify values in location " + lLoc + " and Category " + lItmsgrpcods + ".');</script>");
            //Response.End();
            return;

        }


  


        foreach (GridViewRow row in GridView1.Rows)
        {
            if (row.RowType == DataControlRowType.DataRow)
            {

                HiddenField hdnItem = (HiddenField)row.FindControl("hdnItem");
                HiddenField hdnLoc = (HiddenField)row.FindControl("hdnLoc");
                HiddenField lHiddenBin = (HiddenField)row.FindControl("HiddenBin");                
                TextBox tbBin = (TextBox)row.FindControl("txtbin");

                string NuevoBin = tbBin.Text.Trim();
                string ViejoBin = lHiddenBin.Value;
                string item = hdnItem.Value;
                string loc = hdnLoc.Value;

                SetMinMax(item, loc, NuevoBin, ViejoBin);

            }
        }


        AppendMessage(msg);
        GridView1.DataBind();
        db.Conn.Close();
    }

    private void SetMinMax(string item, string loc, string NuevoBin, string ViejBin)
    {
        string sql;

        if (string.IsNullOrEmpty(NuevoBin))
        {
            return;
        }



        try
        {

            sql = @"update WMS_Whs_Item_Bin
                       set BIN = '" + NuevoBin + @"',
                           updated_by = '" + lCurUser + @"', 
					       date_updated = getdate() 
                     where Whscode  =  '" + loc + @"'
                       and ItemCode = '" + item + @"'
                       and BIN = '" + ViejBin + @"'";


            db.cmd.Parameters.Clear();  
            db.cmd.CommandType = CommandType.Text; 
            db.cmd.CommandText = sql;
            db.cmd.ExecuteNonQuery();


            // do insert in case item does not already exist in rss_store_item_min_max 
            sql = @"insert into WMS_Whs_Item_Bin
                    (Whscode, 
					 ItemCode, 
					 BIN, 
					 created_by, 
					 date_created, 
					 updated_by, 
					 date_updated                        
                    )
                    select
                        '" + loc + @"',
						'" + item + @"',                        
                        '" + NuevoBin + @"',                                                
                        '" + lCurUser + @"',
                        getdate(),
                        '" + lCurUser + @"',
                        getdate()
                    where  
                        not exists
                            (
                                select 
                                    1 
                                from 
                                    WMS_Whs_Item_Bin 
                                where 
                                    ItemCode = '" + item + @"' and 
                                    Whscode = '" + loc + @"'  and
									BIN = '" + NuevoBin + @"'
                            )";


            db.cmd.CommandText = sql;
            db.cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function SetMinMax. ERROR MESSAGE : " + ex.Message);
        }


        try
        {

            db.cmd.Parameters.Clear();
            db.cmd.CommandText = "Item_Bin_Gen_Prc";
            db.cmd.CommandType = CommandType.StoredProcedure;
            //db.cmd.Connection = db.Conn;

            db.cmd.Parameters.Add(new SqlParameter("@Whscode", SqlDbType.NVarChar));
            db.cmd.Parameters["@Whscode"].Value = loc;

            db.cmd.Parameters.Add(new SqlParameter("@ItemCode", SqlDbType.NVarChar));
            db.cmd.Parameters["@ItemCode"].Value = item;

            db.cmd.Parameters.Add(new SqlParameter("@lUser", SqlDbType.NVarChar));
            db.cmd.Parameters["@lUser"].Value = lCurUser;            
            
            
            db.cmd.ExecuteNonQuery();

            

        }
        catch (Exception ex)
        {
            db.Conn.Close();
            throw new Exception("Caught exception in call procedure Item_Bin_Gen_Prc. ERROR MESSAGE : " + ex.Message);
        }


    }

//    private void DeleteMinMax(string item, string loc)
//    {
//        try
//        {
//            // do update if item already exists in rss_store_item_min_max
//            string sql = @"delete from 
//                        rss_store_item_min_max
//                    where 
//                        item = '" + item + @"' and 
//                        loc = '" + loc + "'";

//            db.cmd.Parameters.Clear(); //Mod MINMAXUPDATE  <<<<<<
//            db.cmd.CommandType = CommandType.Text;  //Mod MINMAXUPDATE  <<<<<<
//            db.cmd.CommandText = sql;
//            db.cmd.ExecuteNonQuery();
//        }
//        catch (Exception ex)
//        {
//            throw new Exception("Caught exception in function DeleteMinMax. ERROR MESSAGE : " + ex.Message);
//        }
//    }

    protected void AppendMessage(string msg)
    {
        divMessage.InnerHtml += msg;
    }

    protected void ObjectDataSource1_Selected(object sender, ObjectDataSourceStatusEventArgs e)
    {
       
    }

    protected void DropDownItmGrp_SelectedIndexChanged(object sender, EventArgs e)
    {
        LoadBrands();
    }
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

        //GridView1.Columns[9].Visible = true;
        //GridView1.Columns[10].Visible = true;
        //GridView1.Columns[11].Visible = true;
        //GridView1.Columns[12].Visible = true;
        //GridView1.Columns[13].Visible = true;
        //GridView1.Columns[14].Visible = true;



        //switch (drpToWhsCode.Text)
        //{
        //    case "TIENDA":
        //        GridView1.Columns[9].Visible = false;
        //        break;
        //    case "TIENDA2":
        //        GridView1.Columns[10].Visible = false;
        //        break;
        //    case "TIENDA3":
        //        GridView1.Columns[11].Visible = false;
        //        break;
        //    case "TIENDA4":
        //        GridView1.Columns[12].Visible = false;
        //        break;
        //    case "TIENDA5":
        //        GridView1.Columns[13].Visible = false;
        //        break;
        //    case "TIENDA6":
        //        GridView1.Columns[14].Visible = false;
        //        break;
        //    default:
        //        GridView1.Columns[9].Visible = true;
        //        GridView1.Columns[10].Visible = true;
        //        GridView1.Columns[11].Visible = true;
        //        GridView1.Columns[12].Visible = true;
        //        GridView1.Columns[13].Visible = true;
        //        GridView1.Columns[14].Visible = true;
        //        break;

        //}

  

        bindGridView1();

       
    }
    protected void GridView1_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        

    }
    protected void GridView1_RowCreated(object sender, GridViewRowEventArgs e)
    {


        
    }
    protected void GridView1_SelectedIndexChanged(object sender, EventArgs e)
    {

    }

    //protected void Button_click_event(Object sender, EventArgs e)
    //{
    //    string lIndex;

    //    Button btn = (Button)sender;
    //    switch (btn.CommandName)
    //    {
    //        case "see_Stock":
    //            {

    //                //StockLabel.Visible = true;
    //                //idArticuloLabel.Visible = true;

    //                lIndex = (btn.CommandArgument.ToString());
    //                int index = Convert.ToInt32(lIndex);

    //                GridViewRow row = GridView1.Rows[index];

    //                HiddenField hdnItem = (HiddenField)row.FindControl("hdnItem");
    //                string item = hdnItem.Value;

    //                idArticuloLabel.Text = item;

    //                GridView2.DataBind();
    //            }
    //            break;
            
    //    }


    //}

    //protected void GridView1_RowCommand(object sender, GridViewCommandEventArgs e)
    //{
    //    if (e.CommandName == "see_Stock")
    //    {
    //        // Retrieve the row index stored in the 
    //        // CommandArgument property.
    //        int index = Convert.ToInt32(e.CommandArgument);

    //        // Retrieve the row that contains the button 
    //        // from the Rows collection.
    //        GridViewRow row = GridView1.Rows[index];

    //        HiddenField hdnItem = (HiddenField)row.FindControl("hdnItem");
    //        string item = hdnItem.Value;

    //        idArticuloLabel.Text = item;

    //        GridView2.DataBind();


    //    }

    //}

    protected void GridView1_RowDeleting(object sender, GridViewDeleteEventArgs e)
    {
        //SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["exam_moduleConnectionString"].ConnectionString);
        //SqlCommand cmd = new SqlCommand();
        //cmd.CommandText = "DELETE FROM quest_categories WHERE cat_id=@cat_id";
        //cmd.Parameters.Add("@cat_id", SqlDbType.Int).Value = Convert.ToInt32(GridView1.Rows[e.RowIndex].Cells[1].Text);

        //cmd.Connection = con;
        //con.Open();
        //cmd.ExecuteNonQuery();
        //con.Close();

        BindData();

    }


    private void BindData()
    {
        //SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["exam_moduleConnectionString"].ConnectionString);
        //SqlDataAdapter da = new SqlDataAdapter("SELECT Cat_id, cat_name from quest_categories", con);
        //DataTable dt = new DataTable();
        //da.Fill(dt);
        //GridView1.DataSource = dt;
        GridView1.DataBind();
    }


    protected void GridView1_RowEditing(object sender, GridViewEditEventArgs e)
    {
        GridView1.EditIndex = e.NewEditIndex;
        BindData();
    }
    
    protected void GridView1_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
    {
        GridView1.EditIndex = -1;
        BindData();
    }
    protected void GridView1_RowUpdating(object sender, GridViewUpdateEventArgs e)
    {

        if (((LinkButton)GridView1.Rows[0].Cells[0].Controls[0]).Text == "Insert")
        {
            SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["exam_moduleConnectionString"].ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "INSERT INTO quest_categories(cat_name) VALUES(@cat_name)";
            cmd.Parameters.Add("@cat_name", SqlDbType.VarChar).Value = ((TextBox)GridView1.Rows[0].Cells[2].Controls[0]).Text;

            cmd.Connection = con;
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
        }
        else
        {
            SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["exam_moduleConnectionString"].ConnectionString);
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "UPDATE quest_categories SET cat_name=@cat_name WHERE cat_id=@cat_id";
            cmd.Parameters.Add("@cat_name", SqlDbType.VarChar).Value = ((TextBox)GridView1.Rows[e.RowIndex].Cells[2].Controls[0]).Text;
            cmd.Parameters.Add("@cat_id", SqlDbType.Int).Value = Convert.ToInt32(GridView1.Rows[e.RowIndex].Cells[1].Text);
            cmd.Connection = con;
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();

        }


        GridView1.EditIndex = -1;
        BindData();
    }

    protected void DeleteBtn_Click(object sender, EventArgs e)
    {
        //In order to get the current index code:

GridViewRow gridViewRow = (GridViewRow)(sender as Control).Parent.Parent;
        int index = gridViewRow.RowIndex;

//In order to get the control code:

        HiddenField lhdnLoc = (HiddenField)this.GridView1.Rows[index].FindControl("hdnLoc");   //row.FindControl("HiddenDocentry");
        string lwhscode = lhdnLoc.Value;

        HiddenField lhdnItem = (HiddenField)this.GridView1.Rows[index].FindControl("hdnItem");  //row.FindControl("HiddenLine");
        string lItemCode = lhdnItem.Value;

        HiddenField lHiddenBin = (HiddenField)this.GridView1.Rows[index].FindControl("HiddenBin");  //row.FindControl("HiddenLine");
        string lBin = lHiddenBin.Value;

        if (string.IsNullOrEmpty(lBin))
        {
            return;
        }
        else
        {
            try
            {
                
                string sql = @"delete WMS_Whs_Item_Bin                    
                    where Whscode = '" + lwhscode + @"' and 
                        ItemCode = '" + lItemCode + @"' and 
                        BIN = '" + lBin + "'";

                db.cmd.Parameters.Clear();
                db.cmd.Connection = db.Conn;


                db.cmd.CommandType = CommandType.Text; 
                db.cmd.CommandText = sql;
                db.cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                db.Conn.Close(); 
                throw new Exception("Caught exception in function DeleteMinMax. ERROR MESSAGE : " + ex.Message);
            }


            try
            {

                db.cmd.Parameters.Clear();
                db.cmd.CommandText = "Item_Bin_Gen_Prc";
                db.cmd.CommandType = CommandType.StoredProcedure;
                //db.cmd.Connection = db.Conn;

                db.cmd.Parameters.Add(new SqlParameter("@Whscode", SqlDbType.NVarChar));
                db.cmd.Parameters["@Whscode"].Value = lwhscode;

                db.cmd.Parameters.Add(new SqlParameter("@ItemCode", SqlDbType.NVarChar));
                db.cmd.Parameters["@ItemCode"].Value = lItemCode;

                db.cmd.Parameters.Add(new SqlParameter("@lUser", SqlDbType.NVarChar));
                db.cmd.Parameters["@lUser"].Value = lCurUser;


                db.cmd.ExecuteNonQuery();



            }
            catch (Exception ex)
            {
                db.Conn.Close();
                throw new Exception("Caught exception in call procedure Item_Bin_Gen_Prc. ERROR MESSAGE : " + ex.Message);
            }


            db.Conn.Close();
        }

        GridView1.DataBind();

        return;

    }

    protected void InsertBtn_Click(object sender, EventArgs e)
    {

        //In order to get the current index code:

        GridViewRow gridViewRow = (GridViewRow)(sender as Control).Parent.Parent;
        int index = gridViewRow.RowIndex;

        //In order to get the control code:

        HiddenField lhdnLoc = (HiddenField)this.GridView1.Rows[index].FindControl("hdnLoc");   //row.FindControl("HiddenDocentry");
        string lwhscode = lhdnLoc.Value;

        HiddenField lhdnItem = (HiddenField)this.GridView1.Rows[index].FindControl("hdnItem");  //row.FindControl("HiddenLine");
        string lItemCode = lhdnItem.Value;

        TextBox lTxtBoxbin = (TextBox)this.GridView1.Rows[index].FindControl("txtInsbin");  //row.FindControl("HiddenLine");
        string lBin = lTxtBoxbin.Text;


        if (string.IsNullOrEmpty(lBin))
        {
            return;
        }
        else
        {
            SetMinMax(lItemCode, lwhscode, lBin, lBin); 
            GridView1.DataBind(); 
            return;
        }

    }



}
