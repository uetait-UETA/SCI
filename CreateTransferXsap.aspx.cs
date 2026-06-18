using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;

public partial class CreateTransferXsap : BasePage
{
	protected SqlDb db = new SqlDb();
    DataTable dtToWhs;
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
	protected string appUserName;
    protected string lCurUser;
	
	protected void Page_Load(object sender, EventArgs e)
	{
        try
        {
            ValidaSesionUsuarioCia();

            appUserName = (string)this.Session["UserId"];

            sap_db = (string)this.Session["CompanyId"];
            CompanyIdLabel.Text = sap_db;

            char flagokay = 'N';

            ///////////////Begin New  Control de acceso por Roles
            lCurUser = (string)this.Session["UserId"];
            flagokay = 'Y';
            string lControlName = "CreateTransferXsap.aspx";
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
                btnCreateDraft.Enabled = false;
                btnCreateDraft.ForeColor = Color.Silver;
                labelForm.InnerText = "Create Manual Transfer (Read-Only Access)";
            }

            if (strAccessType == "F")
            {
                btnCreateDraft.Enabled = true;
                labelForm.InnerText = "Create Manual Transfer (Full Access)";
            }
            ///////////////End  New Control de acceso por Roles

            if (flagokay == 'Y')
            {
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
                    db.Connect();
                    LoadWarehouses();
                    LoadItemGroups();
                    InitializeForm();
                    db.Disconnect();

                    btnCreateTransfer.Visible = false;
                    btnUdateDraft.Visible = false;
                    btnCancel.Visible = false;
                }
            }
        }
        catch (Exception)
        {
            db.Disconnect();
            throw;
        }
	}

    private void GoToLogin()
    {
        Response.Redirect("Login1.aspx");
    }

    private void ValidaSesionUsuarioCia()
    {
        if (Session["UserId"] == null || (string)Session["UserId"] == "" || Session["CompanyId"] == null || (string)Session["CompanyId"] == "")
        {
            GoToLogin();
        }
    }

    private void ValidaSesionBodegas()
    {
        if (Session["toWhs"] == null || (DataTable)Session["toWhs"] == null)
        {
            GoToLogin();
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
            string branchFilter = branchId > 0 ? " AND O.BPLid IN (1, " + branchId + ")" : "";
            string sql =
            @"select O.WhsCode,
                     CONVERT(nvarchar(30), ISNULL(O.U_POSCode, '')) + ' - ' + O.WhsCode + ' - ' + O.WhsName AS WHS,
                     R.Control,
                     ISNULL(O.U_Type, '') AS WhsType,
                     ISNULL(sw.TYPEWHS, '') AS TypeWhs
                 from " + sap_db + @".dbo.owhs O " + Queries.WITH_NOLOCK + @"
                 JOIN RSS_OWHS_CONTROL R " + Queries.WITH_NOLOCK + @" ON O.WhsCode = R.WhsCode AND R.CompanyId = '" + sap_db + @"'
                 LEFT JOIN SMM_WHSTYPE sw " + Queries.WITH_NOLOCK + @" ON sw.WHSCODE = O.WhsCode AND sw.COMPANYID = R.CompanyId
                 where R.Control IN ('CRETRAFROMXSAP', 'CRETRATOXSAP')" + branchFilter + @"
              ORDER BY CASE WHEN O.BPLId = 1 THEN 0 ELSE 1 END, O.U_POSCode";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadWarehouses. ERROR MESSAGE : " + ex.Message);
        }

        string userTypeWhs = "";
        try
        {
            string userId = Session["UserId"] != null ? Session["UserId"].ToString() : "";
            if (!string.IsNullOrEmpty(userId))
            {
                using (var cmd = new SqlCommand(
                    "SELECT ISNULL(MAX(TypeWhs),'') FROM smm_login WHERE LoginID = @lid", db.Conn))
                {
                    cmd.Parameters.AddWithValue("@lid", userId);
                    object val = cmd.ExecuteScalar();
                    userTypeWhs = val != null && val != DBNull.Value ? val.ToString() : "";
                }
            }
        }
        catch { }
        Session["UserTypeWhs"] = userTypeWhs;
        bool isBodtie = string.Equals(userTypeWhs, "BODTIE", StringComparison.OrdinalIgnoreCase);

        var dt1Rows = dt.AsEnumerable().Where(x =>
            x.Field<string>("Control") == "CRETRAFROMXSAP" &&
            (isBodtie || string.IsNullOrEmpty(userTypeWhs) ||
             string.IsNullOrEmpty(x.Field<string>("TypeWhs")) ||
             string.Equals(x.Field<string>("TypeWhs"), userTypeWhs, StringComparison.OrdinalIgnoreCase)));
        DataTable dt1 = dt1Rows.Any() ? dt1Rows.CopyToDataTable() : dt.Clone();

        drpFromWhsCode.DataSource = dt1;
        drpFromWhsCode.DataBind();

        Session["fromWhs"] = dt1;

        var dt2Rows = dt.AsEnumerable().Where(x =>
            x.Field<string>("Control") == "CRETRATOXSAP" &&
            (isBodtie || string.IsNullOrEmpty(userTypeWhs) ||
             string.IsNullOrEmpty(x.Field<string>("TypeWhs")) ||
             string.Equals(x.Field<string>("TypeWhs"), userTypeWhs, StringComparison.OrdinalIgnoreCase)));
        DataTable dt2 = dt2Rows.Any() ? dt2Rows.CopyToDataTable() : dt.Clone();

        drpToWhsCode.DataSource = dt2;
        drpToWhsCode.DataBind();

        Session["toWhs"] = dt2;

        ListItem li = new ListItem("Select a location", "0");

        drpFromWhsCode.Items.Insert(0, li);
        drpToWhsCode.Items.Insert(0, li);
    }


    //private void LoadWarehouses()
    //{
    //    DataTable dt = new DataTable();

    //    try
    //    {
    //        string sql =
    //        @"select 
    //		        O.WhsCode ,
    //		        O.WhsName 
    //	            from " + sap_db + @".dbo.owhs O " + Queries.WITH_NOLOCK + @" , RSS_OWHS_CONTROL R " + Queries.WITH_NOLOCK + @" 
    //	            where O.WhsCode  = R.WhsCode
    //	            and R.Control = 'CRETRAFROMXSAP'
    //	            AND R.CompanyId = '" + sap_db + @"' order by o.WhsCode";

    //           //where WhsCode in ('BODEGA', 'TIENDA')


    //           db.adapter = new SqlDataAdapter(sql, db.Conn);
    //        db.adapter.Fill(dt);
    //    }
    //    catch (Exception ex)
    //    {
    //        throw new Exception("Caught exception in function LoadWarehouses. ERROR MESSAGE : " + ex.Message);
    //    }

    //    drpFromWhsCode.DataSource = dt;
    //    drpFromWhsCode.DataBind();

    //       //--------------

    //       DataTable dt2 = new DataTable();

    //    try
    //    {
    //        string sql2 =
    //        @"select 
    //		        O.WhsCode ,
    //		        O.WhsName 
    //	                from " + sap_db + @".dbo.owhs O " + Queries.WITH_NOLOCK + @" , RSS_OWHS_CONTROL R " + Queries.WITH_NOLOCK + @" 
    //	                where O.WhsCode  = R.WhsCode
    //	                and R.Control = 'CRETRATOXSAP'
    //	                AND R.CompanyId = '" + sap_db + @"' order by o.WhsCode";

    //           //where WhsCode in ('BODEGA', 'TIENDA')


    //           db.adapter = new SqlDataAdapter(sql2, db.Conn);
    //        db.adapter.Fill(dt2);
    //    }
    //    catch (Exception ex)
    //    {
    //        throw new Exception("Caught exception in function LoadWarehouses for toloc. ERROR MESSAGE : " + ex.Message);
    //    }


    //    drpToWhsCode.DataSource = dt2;
    //    drpToWhsCode.DataBind();

    //       Session["toWhs"] = dt2;

    //       ListItem li = new ListItem("Select a location", "0");

    //    drpFromWhsCode.Items.Insert(0, li);
    //    drpToWhsCode.Items.Insert(0, li);
    //}

    private void LoadItemGroups()
	{
        try
        {
            DataTable dt = new DataTable();

            try
            {
                string sql =
                @"select 
	            ItmsGrpCod GroupCode, 
	            cast(ItmsGrpCod as varchar) + ' - ' + ItmsGrpNam GroupName 
	            from " + sap_db + @".dbo.oitb " + Queries.WITH_NOLOCK + @" 
	            where 1=1
	        order by ItmsGrpCod";

                db.adapter = new SqlDataAdapter(sql, db.Conn);
                db.adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                throw new Exception("Caught exception in function LoadItemGroups. ERROR MESSAGE : " + ex.Message);
            }
            drpItemGroups.DataSource = dt;
            drpItemGroups.DataBind();

            ListItem li = new ListItem("Select a Group", "0");

            drpItemGroups.Items.Insert(0, li);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadItemGroups for toloc. ERROR MESSAGE : " + ex.Message);
        }
	
	}
	
	protected void drpItemGroups_SelectedIndexChanged(object sender, EventArgs e)
	{
        ValidaSesionUsuarioCia();

        db.Connect();
        try
        {
            LoadBrands();
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
	
	private void LoadBrands()
	{
	    DataTable dt = new DataTable();

	    string itmsgrpcods = "";
        
	    foreach (ListItem li in drpItemGroups.Items)
	    {
	        if (li.Selected)
	        {
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
                from " + sap_db + @".dbo.oitm " + Queries.WITH_NOLOCK + @" 
                where itmsgrpcod in (" + itmsgrpcods + @") 
                and u_brand is not null 
                ) a
                order by sortorder, brand";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
	        db.adapter.Fill(dt);

            lstItemGroups.DataSource = dt;
            lstItemGroups.DataBind();
        }
	    catch (Exception ex)
	    {
	        throw new Exception("Caught exception in function LoadBrands. ERROR MESSAGE : " + ex.Message + "itmsgrpcods: " + itmsgrpcods);
	    }
	}
	
	protected void btnCreateTransfer_Click(object sender, EventArgs e)
	{
        ValidaSesionUsuarioCia();

        string lFlag = Validate1();
        if (lFlag == "N") return;

        UpdateQties();

        if (db.DbConnectionState == ConnectionState.Closed)
            db.Connect();

        bool submitOk = false;
        try
        {
            SqlCommand sqlCommand2 = new SqlCommand
            {
                Connection     = db.Conn,
                CommandType    = CommandType.StoredProcedure,
                CommandText    = "submit_DraftXsap",
                CommandTimeout = 240,
                Parameters =
                {
                    new SqlParameter { ParameterName = "@CompanyId", SqlDbType = SqlDbType.NVarChar, Value = CompanyIdLabel.Text },
                    new SqlParameter { ParameterName = "@DocEntry",  SqlDbType = SqlDbType.Int,      Value = DocEntry.Text },
                    new SqlParameter { ParameterName = "@userDraft", SqlDbType = SqlDbType.NVarChar, Value = appUserName }
                }
            };
            sqlCommand2.ExecuteNonQuery();
            submitOk = true;
        }
        catch (Exception ex)
        {
            Response.Write("Error when submit_DraftXsap. " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        if (!submitOk) return;

        int docEntry = Convert.ToInt32(DocEntry.Text);

        int sapDocNum;
        string dispErr = TransferAutoDispatch.RunAutoDispatch(
            docEntry, CompanyIdLabel.Text, sap_db, appUserName, out sapDocNum);

        if (dispErr != null)
        {
            LabDocEntry.Text          = "SAP B1 Error: " + dispErr + " — The transfer has been reversed.";
            btnCreateDraft.Visible    = true;
            btnCreateTransfer.Visible = false;
            btnUdateDraft.Visible     = false;
            btnCancel.Visible         = true;
            GridView1.DataBind();
            return;
        }

        string sapRef = sapDocNum > 0
            ? " Sales Order #" + sapDocNum + " created in SAP B1."
            : "";
        LabDocEntry.Text = " Draft " + DocEntry.Text + " completed." + sapRef;

        btnCreateDraft.Visible    = false;
        btnCreateTransfer.Visible = false;
        btnUdateDraft.Visible     = false;
        btnCancel.Text    = "Create New Draft";
        btnCancel.Visible = true;
        GridView1.DataBind();
    }

	protected void btnCreateDraft_Click(object sender, EventArgs e)
	{
        ValidaSesionUsuarioCia();

        if (db.DbConnectionState == ConnectionState.Closed)
        {
            db.Connect();
        }

        string fromLoc = drpFromWhsCode.SelectedValue;
        string toLoc = drpToWhsCode.SelectedValue;
        string itemGroups = null, brands = null;

        try
        {
            if (fromLoc == "0")
            {
                divMessage.InnerHtml = "Select the Origin";
                Alert.Show("Select the Origin");
                drpFromWhsCode.Focus();
                return;
            }
            //else fromLoc = "'" + fromLoc + "'";

            if (toLoc == "0")
            {
                divMessage.InnerHtml = "Select the Destination'";
                Alert.Show("Select the Destination");
                drpToWhsCode.Focus();
                return;
            }
            //else toLoc = "'" + toLoc + "'";

            if (fromLoc == toLoc)
            {
                divMessage.InnerHtml = "The Origin and Destination must be different";
                Alert.Show("The Origin and Destination must be different");
                drpToWhsCode.Focus();
                return;
            }

            if (string.IsNullOrEmpty(ItemTextBox.Text))
            {
                foreach (ListItem li in drpItemGroups.Items)
                {
                    if (li.Selected)
                    {
                        itemGroups += li.Value + ",";
                    }
                }

                if (itemGroups.Length > 0)
                {
                    itemGroups = itemGroups.Substring(0, itemGroups.Length - 1);
                }

                if (itemGroups == "0")
                {
                    divMessage.InnerHtml = "Please select at least one Category";
                    Alert.Show("Please select at least one Category");
                    drpItemGroups.Focus();
                    return;
                }

                foreach (ListItem li in lstItemGroups.Items)
                {
                    if (li.Selected)
                    {
                        brands += "'" + li.Value.ToString() + "',";
                    }
                }

                if (brands == null)
                {
                    divMessage.InnerHtml = "Please select at least one Brand";
                    Alert.Show("Please select at least one Brand");
                    lstItemGroups.Focus();
                    return;
                }
                else
                {
                    brands = brands.Substring(0, brands.Length - 1);
                }
            }
            else
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

            // Validate auto-dispatch prerequisites before creating anything in the DB.
            string preErr = TransferAutoDispatch.ValidateAutoDispatch(fromLoc, toLoc, CompanyIdLabel.Text, sap_db);
            if (preErr != null)
            {
                db.Disconnect();
                Alert.Show("Cannot create transfer: " + preErr);
                return;
            }

            if(ItemTextBox.Text != null && ItemTextBox.Text != "")
            {
                BindGridView1(funcionInvocadora: "btnCreateDraft_Click", fromLoc: fromLoc, toLoc: toLoc, ItemCode: ItemTextBox.Text);
            }
            else
            {
                BindGridView1(funcionInvocadora: "btnCreateDraft_Click", fromLoc: fromLoc, toLoc: toLoc, itemGroups: itemGroups, brands: brands);
            }

            int rgcount = GridView1.Rows.Count;

            if (rgcount > 0)
            {
                btnCreateDraft.Visible = false;
                btnCreateTransfer.Visible = true;
                btnUdateDraft.Visible = true;
                btnCancel.Visible = true;
            }
            else
            {
                divMessage.InnerHtml = "No products found with OnHand";
                Alert.Show("No products found with OnHand");
                LabDocEntry.Text = null;
                drpFromWhsCode.Focus();
                return;
            }
        }
        catch (Exception ex)
        {
            Response.Write("Error when submit_DraftXsap.");
            Response.Write(ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
	}

    protected void ItemList_SelectedIndexChanged(object sender, EventArgs e)
    {
        ValidaSesionUsuarioCia();

        if (ItemList.SelectedValue != "-")
        {
            //Here we go, but let's do the validations again, just in the case
            //the user changed something on some field value.

            ItemTextBox.Text = ItemList.SelectedValue;
            ItemList.Visible = false;
            rbtnCancel.Visible = false;

            string fromLoc = drpFromWhsCode.SelectedValue;
            string toLoc = drpToWhsCode.SelectedValue;
            string itemGroups = null, brands = null;

            if (fromLoc == "0")
            {
                divMessage.InnerHtml = "Select the Origin";
                //Alert.Show("Select the Origin");
                drpFromWhsCode.Focus();
                return;
            }

            if (toLoc == "0")
            {
                divMessage.InnerHtml = "Select the Destination'";
                //Alert.Show("Select the Destination");
                drpToWhsCode.Focus();
                return;
            }

            if (fromLoc == toLoc)
            {
                divMessage.InnerHtml = "The Origin and Destination must be different";
                //Alert.Show("The Origin and Destination must be different");
                drpToWhsCode.Focus();
                return;
            }

            if (ItemTextBox.Text == "" || ItemTextBox.Text == null)
            {
                foreach (ListItem li in drpItemGroups.Items)
                {
                    if (li.Selected)
                    {
                        itemGroups += li.Value + ",";
                    }
                }

                if (itemGroups.Length > 0)
                {
                    itemGroups = itemGroups.Substring(0, itemGroups.Length - 1);
                }

                if (itemGroups == "0")
                {
                    divMessage.InnerHtml = "Please select at least one Category";
                    //Alert.Show("Please select at least one Category");
                    drpItemGroups.Focus();
                    return;
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
                }
                else
                {
                    divMessage.InnerHtml = "Please select a Brand or an Item.";
                    lstItemGroups.Focus();
                    return;
                }
            }

            //BindGridView1(funcionInvocadora: "ItemList_SelectedIndexChanged", fromLoc: fromLoc, toLoc: toLoc, ItemCode: ItemTextBox.Text);
        }
    }

    protected void RbtnCancel_Click(object sender, EventArgs e)
    {
        ValidaSesionUsuarioCia();

        ItemList.Visible = false;
        rbtnCancel.Visible = false;
        ItemTextBox.Text = "";
    }

    protected void BindGridView1(string funcionInvocadora, string fromLoc, string toLoc, string itemGroups = null, string brands = null, string ItemCode = null)
    {
        int execRes = 0;
        int lDocEntry = 0;

        try
        {
            if (db.DbConnectionState == ConnectionState.Closed)
            {
                db.Connect();
            }

            SqlCommand admCmd = new SqlCommand
            { 
                Connection = db.Conn,
                CommandType = CommandType.StoredProcedure,
                CommandText = "get_TransXsap_sequence",
                CommandTimeout = 240
            };

            SqlDataReader admDr = admCmd.ExecuteReader();

            while (admDr.Read())
            {
                lDocEntry = Convert.ToInt32((admDr.GetString(0)));
            }

            admDr.Close();
        }
        catch (Exception ex)
        {
            db.Disconnect();
            Response.Write("Error when get_TransXsap_sequence: ");
            Response.Write(ex.Message);
        }

        DocEntry.Text = lDocEntry.ToString();
        LabDocEntry.Text = "Draft Number: " + lDocEntry.ToString();

        if(db.DbConnectionState == ConnectionState.Closed)
        {
            db.Connect();
        }

        try
        {
            SqlCommand sqlCommand = new SqlCommand
            {
                Connection = db.Conn,
                CommandType = CommandType.StoredProcedure,
                CommandText = "smm_populate_TransXsap_odrf",
                CommandTimeout = 240,
                Parameters = {
                    new SqlParameter
                    {
                        ParameterName = "@CompanyId",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = CompanyIdLabel.Text
                    },
                    new SqlParameter
                    {
                        ParameterName = "@fromLoc",
                        SqlDbType = SqlDbType.VarChar,
                        Value = fromLoc
                    },
                    new SqlParameter
                    {
                        ParameterName = "@toLoc",
                        SqlDbType = SqlDbType.VarChar,
                        Value = toLoc
                    },
                    new SqlParameter
                    {
                        ParameterName = "@itemGroups",
                        SqlDbType = SqlDbType.VarChar,
                        Value = (itemGroups ?? "-")
                    },
                    new SqlParameter
                    {
                        ParameterName = "@brands",
                        SqlDbType = SqlDbType.VarChar,
                        Value = (brands ?? "-")
                    },
                    new SqlParameter
                    {
                        ParameterName = "@DocEntry",
                        SqlDbType = SqlDbType.Int,
                        Value = lDocEntry
                    },
                    new SqlParameter
                    {
                        ParameterName = "@userDraft",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = appUserName
                    },
                    new SqlParameter
                    {
                        ParameterName = "@item",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = (ItemCode ?? "-")
                    }
                }
            };

            execRes = sqlCommand.ExecuteNonQuery();
            GridView1.DataBind();
        }
        catch (Exception ex)
        {
            Response.Write("Error when " + funcionInvocadora + " was called.");
            Response.Write(ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
    }

    protected void btnUdateDraft_Click(object sender, EventArgs e)
	{
        ValidaSesionUsuarioCia();

        string lFlag = null;
	    lFlag = Validate1();
	
	    if (lFlag == "N")
        {
            return;
        }
	        
	    UpdateQties();
	}
	
	protected void UpdateQties()
	{
        int execRes = 0;
	    int TmpQty = 0;
	    int LvLinNum = 0;
	    string qtystr = null;
        string strTmpQty = null;
	    int charpos = 0;

	    int rg2count = GridView1.Rows.Count;

        SqlCommand sqlCommand, sqlCommand2, sqlCommand3;

        for (int i = 0; i < rg2count; i++)
	    {
	        LvLinNum = Convert.ToInt32(GridView1.Rows[i].Cells[0].Text);
            strTmpQty = ((TextBox)(GridView1.Rows[i].Cells[6].Controls[1])).Text;

	        qtystr = strTmpQty;
	
	        charpos = qtystr.IndexOf(".");
	
	        if (charpos != -1)
	        {
                strTmpQty = qtystr.Substring(0, charpos);
	        }

            TmpQty = Convert.ToInt32(strTmpQty);

	        if (TmpQty != 0)
	        {
                if(db.DbConnectionState == ConnectionState.Closed)
                {
                    db.Connect();
                }

                try
	            {
                    sqlCommand = new SqlCommand
                    {
                        Connection = db.Conn,
                        CommandType = CommandType.StoredProcedure,
                        CommandText = "update_TransXsap_drf1",
                        CommandTimeout = 240,
                        Parameters =
                        {
                            new SqlParameter
                            {
                                ParameterName = "@DocEntry",
                                SqlDbType = SqlDbType.Int,
                                Value = DocEntry.Text
                            },
                            new SqlParameter
                            {
                                ParameterName = "@Linenum",
                                SqlDbType = SqlDbType.Int,
                                Value = LvLinNum
                            },
                            new SqlParameter
                            {
                                ParameterName = "@TmpQty",
                                SqlDbType = SqlDbType.Int,
                                Value = TmpQty
                            },
                            new SqlParameter
                            {
                                ParameterName = "@CompanyId",
                                SqlDbType = SqlDbType.NVarChar,
                                Value = CompanyIdLabel.Text
                            }
                        }
                    };

                    execRes = sqlCommand.ExecuteNonQuery();
	            }
	            catch (Exception ex)
	            {
                    db.Disconnect();
                    Response.Write("Error when update_TransXsap_drf1.");
	                Response.Write(ex.Message);
	            }
	        }
        }

        if (db.DbConnectionState == ConnectionState.Closed)
        {
            db.Connect();
        }

        try
        {
            sqlCommand2 = new SqlCommand
            {
                Connection = db.Conn,
                CommandType = CommandType.StoredProcedure,
                CommandText = "delete_TransXsap_drf1",
                CommandTimeout = 240,
                Parameters =
                {
                    new SqlParameter
                    {
                        ParameterName = "@DocEntry",
                        SqlDbType = SqlDbType.Int,
                        Value = DocEntry.Text
                    },
                    new SqlParameter
                    {
                        ParameterName = "@CompanyId",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = CompanyIdLabel.Text
                    }
                }
            };

	        execRes = sqlCommand2.ExecuteNonQuery();
	    }
	    catch (Exception ex)
	    {
            db.Disconnect();
            Response.Write("Error when delete_TransXsap_drf1.");
	        Response.Write(ex.Message);
	    }

        if (db.DbConnectionState == ConnectionState.Closed)
        {
            db.Connect();
        }

        try
	    {
            sqlCommand3 = new SqlCommand
            {
                Connection = db.Conn,
                CommandType = CommandType.StoredProcedure,
                CommandText = "reseq_TransXsap_drf1",
                CommandTimeout = 240,
                Parameters =
                {
                    new SqlParameter
                    {
                        ParameterName = "@DocEntry",
                        SqlDbType = SqlDbType.Int,
                        Value = DocEntry.Text
                    },
                    new SqlParameter
                    {
                        ParameterName = "@CompanyId",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = CompanyIdLabel.Text
                    }
                }
            };

            execRes = sqlCommand3.ExecuteNonQuery();
            GridView1.DataBind();
        }
	    catch (Exception ex)
	    {
            Response.Write("Error when reseq_TransXsap_drf1.");
	        Response.Write(ex.Message);
	    }
        finally
        {
            db.Disconnect();
        }
    }

    protected string Validate1()
    {
        //string ErrEx = null;
        string strTmpQty = null;
        int TmpQty = 0;
        int LvLinNum = 0;
        int acumQty = 0;
        int OnHand = 0;
        char LvFlag1;
        char LvFlagIntNumbers = 'Y';
        char LvFlagMoreOrEqualsToZero = 'Y';
        char LvFlagLessOrEqualsToOnHand = 'Y';

        int rg2count = GridView1.Rows.Count;

        string Lmsg = "Please review quantities on lines ";

        for (int i = 0; i < rg2count; i++)
        {
            LvFlag1 = 'Y';

            LvLinNum = Convert.ToInt32(GridView1.Rows[i].Cells[0].Text);
            OnHand = Convert.ToInt32(GridView1.Rows[i].Cells[4].Text);
            strTmpQty = ((TextBox)GridView1.Rows[i].Cells[6].Controls[1]).Text;

            if (strTmpQty == "0.000000")
            {
                strTmpQty = "0";
            }

            if (!int.TryParse(strTmpQty, out TmpQty))
            {
                Lmsg = Lmsg + " " + LvLinNum + ",";
                LvFlag1 = 'N';
                LvFlagIntNumbers = 'N';
                //ErrEx = "Invalid number: " + strTmpQty;
            }

            if(LvFlag1 == 'Y')
            {
                if (TmpQty < 0)
                {
                    Lmsg = Lmsg + " " + LvLinNum + ",";
                    LvFlag1 = 'N';
                    LvFlagMoreOrEqualsToZero = 'N';
                }

                if (TmpQty > OnHand)
                {
                    Lmsg = Lmsg + ' ' + LvLinNum + ',';
                    LvFlag1 = 'N';
                    LvFlagLessOrEqualsToOnHand = 'N';
                }

                if (LvFlag1 == 'Y')
                {
                    acumQty = acumQty + TmpQty;
                }
            }
        }

        if (LvFlagIntNumbers == 'N')
        {
            Lmsg = Lmsg + ". Please enter whole numbers only. ";
            Alert.Show(Lmsg);
            return ("N");
        }

        if (LvFlagMoreOrEqualsToZero == 'N')
        {
            Lmsg = Lmsg + ". These must be greater than or equal to zero. ";
            Alert.Show(Lmsg);
            return ("N");
        }

        if (LvFlagLessOrEqualsToOnHand == 'N')
        {
            Lmsg = Lmsg + ". These must be less than or equal to the OnHand quantity. ";
            Alert.Show(Lmsg);
            return ("N");
        }

        if (acumQty == 0)
        {
            Lmsg = "Please enter a quantity greater than zero for at least one item.";
            Alert.Show(Lmsg);
            LvFlag1 = 'N';
            return ("N");
        }

        btnUdateDraft.Visible = true;

        return ("Y");
    }

    //protected string Validate1()
    //{
    //    int TmpQty = 0;
    //    int LvLinNum = 0;
    //    char LvFlag1 = 'Y';
    //    string ErrEx = null;
    //       int x = 0;

    //       TextBox LvTxB1 = new TextBox();

    //    int rg2count = GridView1.Rows.Count;

    //    string Lmsg = "Please review quantities on lines ";

    //    for (int i = 0; i < rg2count; i++)
    //    {
    //        LvLinNum = Convert.ToInt32(GridView1.Rows[i].Cells[0].Text);
    //        LvTxB1.Text = ((TextBox)(GridView1.Rows[i].Cells[6].Controls[1])).Text; //LC LvTxB1.Text = ((TextBox)(GridView1.Rows[i].Cells[5].Controls[1])).Text;

    //        if (LvTxB1.Text == "0.000000")
    //        {
    //            LvTxB1.Text = "0";
    //        }

    //        try
    //        {
    //            x = int.Parse(LvTxB1.Text);
    //        }
    //        catch (Exception ex)
    //        {
    //            Lmsg = Lmsg + ' ' + LvLinNum + ',';
    //            LvFlag1 = 'N';
    //            ErrEx = ex.Message;
    //        }
    //    }

    //    if (LvFlag1 == 'N')
    //    {
    //        Lmsg = Lmsg + ", Please enter whole numbers only. ";
    //        Alert.Show(Lmsg);
    //        return ("N");
    //    }

    //    {
    //        int acumQty = 0;
    //        for (int i = 0; i < rg2count; i++)
    //        {
    //            LvLinNum = Convert.ToInt32(GridView1.Rows[i].Cells[0].Text);
    //            LvTxB1.Text = ((TextBox)(GridView1.Rows[i].Cells[6].Controls[1])).Text; //LC ((TextBox)(GridView1.Rows[i].Cells[5].Controls[1])).Text;

    //            if (LvTxB1.Text == "0.000000")
    //            {
    //                LvTxB1.Text = "0";
    //            }

    //            TmpQty = Convert.ToInt32(LvTxB1.Text);
    //            acumQty = acumQty + TmpQty;

    //            if (TmpQty < 0)
    //            {
    //                Lmsg = Lmsg + ' ' + LvLinNum + ',';
    //                LvFlag1 = 'N';
    //            }
    //        }

    //        if (LvFlag1 == 'N')
    //        {
    //            Lmsg = Lmsg + ". These must be greater than or equal to zero";
    //            Alert.Show(Lmsg);
    //            return ("N");
    //        }

    //        if (acumQty == 0)
    //        {
    //            Lmsg = "Please enter a quantity greater than zero for at least one item.";
    //            Alert.Show(Lmsg);
    //            LvFlag1 = 'N';
    //            return ("N");
    //        }
    //    }

    //    int OnHand = 0;

    //    for (int i = 0; i < rg2count; i++)
    //    {
    //        LvLinNum = Convert.ToInt32(GridView1.Rows[i].Cells[0].Text);
    //        LvTxB1.Text = ((TextBox)(GridView1.Rows[i].Cells[6].Controls[1])).Text; //LC ((TextBox)(GridView1.Rows[i].Cells[5].Controls[1])).Text;

    //        OnHand = Convert.ToInt32(GridView1.Rows[i].Cells[4].Text); //LC Convert.ToInt32(GridView1.Rows[i].Cells[3].Text);

    //        if (LvTxB1.Text == "0.000000") //LC
    //	    {
    //		    LvTxB1.Text = "0";
    //	    }

    //	    TmpQty = Convert.ToInt32(LvTxB1.Text);

    //        if (TmpQty > OnHand)
    //        {
    //            Lmsg = Lmsg + ' ' + LvLinNum + ',';
    //            LvFlag1 = 'N';
    //        }
    //    }

    //    if (LvFlag1 == 'N')
    //    {
    //        Lmsg = Lmsg + ". These must be less than or equal to the OnHand quantity";
    //        Alert.Show(Lmsg);
    //        return ("N");
    //    }

    //    btnUdateDraft.Visible = true;

    //    return ("Y");
    //}

    protected void btnCancel_Click(object sender, EventArgs e)
	{
        ValidaSesionUsuarioCia();

        int exeRes = 0;

        if (btnCancel.Text == "Delete Draft")
	    {
	        if(db.DbConnectionState == ConnectionState.Closed)
            {
                db.Connect();
            }

            try
            {
                SqlCommand sqlCommand1 = new SqlCommand
                {
                    Connection = db.Conn,
                    CommandType = CommandType.StoredProcedure,
                    CommandText = "delete_TransXsap",
                    CommandTimeout = 240,
                    Parameters =
                    {
                        new SqlParameter
                        {
                            ParameterName = "@DocEntry",
                            SqlDbType = SqlDbType.Int,
                            Value = DocEntry.Text
                        },
                        new SqlParameter
                        {
                            ParameterName = "@CompanyId",
                            SqlDbType = SqlDbType.NVarChar,
                            Value = CompanyIdLabel.Text
                        }
                    }
                };

                exeRes = sqlCommand1.ExecuteNonQuery();
	        }
	        catch (Exception ex)
	        {
	            Response.Write("Error when delete_TransXsap");
	            Response.Write(ex.Message);
	        }
            finally
            {
                db.Disconnect();
            }
	
	        string url = HttpContext.Current.Request.Url.AbsoluteUri;

	        Response.AppendHeader("Refresh", "0; URL=" + url);
	    }
	
	    if (btnCancel.Text == "Create New Draft")
	    {
	        string url = HttpContext.Current.Request.Url.AbsoluteUri;
	        Response.AppendHeader("Refresh", "0; URL=" + url);
	    }
	}
    protected void drpFromWhsCode_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            ValidaSesionUsuarioCia();
            ValidaSesionBodegas();

            dtToWhs = (DataTable)Session["toWhs"];

            if (dtToWhs.Rows.Count <= 0)
            {
                Alert.Show("No destinations available");
            }
            else
            {
                string selectedFromWhs = drpFromWhsCode.SelectedValue;
                string fromWhsType = "";

                DataTable dtFromWhs = (DataTable)Session["fromWhs"];
                if (dtFromWhs != null)
                {
                    DataRow[] fromRows = dtFromWhs.Select("WhsCode = '" + selectedFromWhs + "'");
                    if (fromRows.Length > 0)
                        fromWhsType = fromRows[0]["WhsType"].ToString();
                }

                var filtered = dtToWhs.AsEnumerable()
                    .Where(x => x.Field<string>("WhsCode") != selectedFromWhs);

                if (!string.IsNullOrEmpty(fromWhsType))
                    filtered = filtered.Where(x => x.Field<string>("WhsType") == fromWhsType);

                drpToWhsCode.DataSource = filtered.Any() ? filtered.CopyToDataTable() : dtToWhs.Clone();
                drpToWhsCode.DataBind();
            }

            ListItem li = new ListItem("Select a location", "0");
            drpToWhsCode.Items.Insert(0, li);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadWarehouses for toloc. ERROR MESSAGE : " + ex.Message);
        }


        //db.Connect();
        //try
        //{
        //    DataTable dt2 = new DataTable();
        //    string sql2 =
        //    "select O.WhsCode, O.WhsName " +
        //    "from " + sap_db + @".dbo.owhs O " + Queries.WITH_NOLOCK + @" , RSS_OWHS_CONTROL R " + Queries.WITH_NOLOCK + @"  " +
        //    "where O.WhsCode  = R.WhsCode " +
        //    "and R.Control = 'CRETRATOXSAP' " +
        //    "and O.WhsCode <> '" + drpFromWhsCode.SelectedValue + "' " +
        //    "AND R.CompanyId = '" + sap_db + @"' order by o.WhsCode";

        //    db.adapter = new SqlDataAdapter(sql2, db.Conn);
        //    db.adapter.Fill(dt2);

        //    drpToWhsCode.DataSource = dt2;
        //    drpToWhsCode.DataBind();

        //    ListItem li = new ListItem("Select a location", "0");

        //    drpToWhsCode.Items.Insert(0, li);
        //}
        //catch (Exception ex)
        //{
        //    throw new Exception("Caught exception in function LoadWarehouses for toloc. ERROR MESSAGE : " + ex.Message);
        //}
        //finally
        //{
        //    db.Disconnect();
        //}
    }
}
