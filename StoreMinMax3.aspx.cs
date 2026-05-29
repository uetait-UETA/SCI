using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using Telerik.Web.UI;
using System.Drawing;

public partial class StoreMinMax3 : BasePage
{
    protected SqlDb db = new SqlDb();
    protected string usr;
    protected string Loc;
    protected string Item;
    protected string sap_db;
    protected string lCurUser;
    protected int newFirstItem = 0;
    protected int updMins = 0;

    protected void Page_Load(object sender, EventArgs e)
    {
        lCurUser = (string)Session["UserId"];

        if ((string)Session["UserId"] == "" || (string)Session["UserId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

        if ((string)Session["CompanyId"] == "" || (string)Session["CompanyId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

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
		    btnSaveChanges.Enabled = false;
		    btnSaveChanges.ForeColor = Color.Silver;
		    MinUpdBotton.Enabled = false;
		    MinUpdBotton.ForeColor = Color.Silver; 
		    labelForm.InnerText = "Minimum-Maximum Values (Read-Only Access)";
	    }
		
	    if (strAccessType == "F")
	    {
		    btnSaveChanges.Enabled = true;
		    MinUpdBotton.Enabled = true;
		    labelForm.InnerText = "Minimum-Maximum Values  (Full Access)";
	    }
        ///////////////End  New Control de acceso por Roles

        sap_db = (string)Session["CompanyId"];

        string x = User.Identity.Name.ToLower();
        usr = (string)Session["UserId"];

        divMessage.InnerHtml = "";

        // Runs on every request (after ViewState is loaded) to ensure TOR columns
        // have correct DataField values and no ViewState duplicates.
        BuildTorColumns();

        if (!IsPostBack)
        {
            db.Connect();
            LoadWarehouses();
            LoadItemGroup();
            InitializeForm();
            db.Disconnect();
        }
    }

    protected void Page_Init(object sender, EventArgs e)
    {
        if ((string)Session["CompanyId"] == "" || (string)Session["CompanyId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

        sap_db = (string)Session["CompanyId"];
        int bplId = 0;
        int.TryParse(Session["BranchId"] as string, out bplId);
        int torBplId = 0;
        int.TryParse(System.Configuration.ConfigurationManager.AppSettings["TorBPLId"], out torBplId);

        db.Connect();
        DataTable ciaWhs = db.GetWhsByCiaIdAndControl(sap_db, "SETMINMAX", "BODEGA", bplId);
        DataTable torWhs = db.GetWhsByUStoreAndBplId(sap_db, "", torBplId);
        db.Disconnect();

        // ITemplate must be set in Page_Init so hidden field ViewState loads correctly
        // before Page_Load (needed for Save to read hdnITEM, hdnLOC, etc.)
        Telerik.Web.UI.GridTemplateColumn tCol = (Telerik.Web.UI.GridTemplateColumn)GridView1.MasterTableView.Columns.FindByUniqueName("TemplateColumn1");
        tCol.ItemTemplate = new TemplateColumn1(ciaWhs);

        // TOR columns MUST be added here so that Telerik can locate them by UniqueName
        // when it loads ViewState (Telerik updates existing column objects from ViewState —
        // it never creates new ones — so they must exist before LoadViewState runs).
        // BuildTorColumns() in Page_Load will re-apply DataField after ViewState potentially
        // blanks out design-time properties.
        foreach (DataRow item in torWhs.Rows)
        {
            string wc = (string)item["WhsCode"];
            Telerik.Web.UI.GridBoundColumn gCol = new Telerik.Web.UI.GridBoundColumn
            {
                HeaderText = wc,
                HeaderButtonType = GridHeaderButtonType.TextButton,
                SortExpression = "TOR_" + wc,
                DataField = "TOR_" + wc,
                UniqueName = "TOR_" + wc,
                DataFormatString = "{0:N0}",
                ColumnGroupName = "TorOnHand",
                Display = true
            };
            gCol.HeaderStyle.Width = new Unit(80.00, UnitType.Pixel);
            gCol.HeaderStyle.HorizontalAlign = HorizontalAlign.Center;
            GridView1.MasterTableView.Columns.Add(gCol);
        }
    }

    private void BuildTorColumns()
    {
        int torBplId = 0;
        int.TryParse(System.Configuration.ConfigurationManager.AppSettings["TorBPLId"], out torBplId);

        db.Connect();
        DataTable torWhs = db.GetWhsByUStoreAndBplId(sap_db, "", torBplId);

        // Determine selected store U_Type to filter TOR columns by matching type.
        // Empty storeUType (no store selected or no type set) → show all TOR columns.
        string selectedStore = drpToWhsCode.SelectedValue;
        string storeUType = "";
        if (!string.IsNullOrEmpty(selectedStore) && selectedStore != "0")
            storeUType = db.GetWhsUType(sap_db, selectedStore);

        db.Disconnect();

        // Update the TOR group header text from web.config
        string torGroupName = System.Configuration.ConfigurationManager.AppSettings["TorColumnGroupName"] ?? "TOR";
        for (int ci = 0; ci < GridView1.MasterTableView.ColumnGroups.Count; ci++)
        {
            if (GridView1.MasterTableView.ColumnGroups[ci].Name == "TorOnHand")
            {
                GridView1.MasterTableView.ColumnGroups[ci].HeaderText = torGroupName;
                break;
            }
        }

        // ViewState APPENDS new column objects for every saved column on each postback,
        // so after Page_Init adds 2 TOR columns and ViewState adds 2 more we get 4,
        // then 6, then 8… We must deduplicate.
        // Strategy: keep the FIRST occurrence of each TOR UniqueName (the one Page_Init
        // added with correct DataField) and remove all later ViewState-appended copies.
        var firstOccurrenceIdx = new Dictionary<string, int>();
        var indicesToRemove = new List<int>();

        for (int i = 0; i < GridView1.MasterTableView.Columns.Count; i++)
        {
            string un = GridView1.MasterTableView.Columns[i].UniqueName ?? "";
            if (!un.StartsWith("TOR_")) continue;

            if (firstOccurrenceIdx.ContainsKey(un))
                indicesToRemove.Add(i);
            else
                firstOccurrenceIdx[un] = i;
        }

        // Remove duplicates backwards so earlier indices stay valid
        for (int i = indicesToRemove.Count - 1; i >= 0; i--)
            GridView1.MasterTableView.Columns.RemoveAt(indicesToRemove[i]);

        // Re-apply design-time properties on the surviving first occurrences.
        // ViewState may have blanked DataField (a design-time property it does not save).
        // Also filter visibility: show only TOR columns whose U_Type matches the selected store.
        // When no store is selected (storeUType empty), show all TOR columns.
        foreach (DataRow item in torWhs.Rows)
        {
            string wc = (string)item["WhsCode"];
            string torUType = item["U_Type"].ToString();
            string uniqueName = "TOR_" + wc;

            bool shouldDisplay = string.IsNullOrEmpty(storeUType)
                || string.IsNullOrEmpty(torUType)
                || torUType == storeUType;

            Telerik.Web.UI.GridBoundColumn existing =
                (Telerik.Web.UI.GridBoundColumn)GridView1.MasterTableView.Columns.FindByUniqueName(uniqueName);

            if (existing != null)
            {
                existing.DataField = "TOR_" + wc;
                existing.HeaderText = wc;
                existing.DataFormatString = "{0:N0}";
                existing.ColumnGroupName = "TorOnHand";
                existing.Display = shouldDisplay;
                existing.HeaderStyle.Width = new Unit(80.00, UnitType.Pixel);
                existing.HeaderStyle.HorizontalAlign = HorizontalAlign.Center;
            }
            else
            {
                // Safety net: add if somehow missing after deduplication
                Telerik.Web.UI.GridBoundColumn gCol = new Telerik.Web.UI.GridBoundColumn
                {
                    HeaderText = wc,
                    HeaderButtonType = GridHeaderButtonType.TextButton,
                    SortExpression = "TOR_" + wc,
                    DataField = "TOR_" + wc,
                    UniqueName = "TOR_" + wc,
                    DataFormatString = "{0:N0}",
                    ColumnGroupName = "TorOnHand",
                    Display = shouldDisplay
                };
                gCol.HeaderStyle.Width = new Unit(80.00, UnitType.Pixel);
                gCol.HeaderStyle.HorizontalAlign = HorizontalAlign.Center;
                GridView1.MasterTableView.Columns.Add(gCol);
            }
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

            drpToWhsCode.DataSource = dt;
            drpToWhsCode.DataBind();

            ListItem li = new ListItem("Select a location", "0");
            drpToWhsCode.Items.Insert(0, li);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadWarehouses. ERROR MESSAGE : " + ex.Message);
        }
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

        ListItem li = new ListItem("Select a category", "0");

        DropDownItmGrp.Items.Insert(0, li);
    }

    private void LoadBrands()
    {
        DataTable dt = new DataTable();

        string itmsgrpcods = "";

        foreach (ListItem li in DropDownItmGrp.Items)
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
    }

    protected void btnSelectAll_Click(object sender, EventArgs e)
    {
        foreach (ListItem li in lstItemGroups.Items)
        {
            li.Selected = true;
        }
    }

    //2019-ABR-09: Modificado por Aldo Reina, para la b�squeda por c�digo de barras:
    //protected void btnCreateWorksheet_Click(object sender, EventArgs e)
    //{
    //    string lwhscode = drpToWhsCode.SelectedValue;
    //    string ItemGroups = "", brands = "";

    //    if (lwhscode == "0")
    //    {
    //        divMessage.InnerHtml = "Please select the Operation location.";
    //        drpToWhsCode.Focus();
    //        return;
    //    }

    //    if (ItemTextBox.Text == "" || ItemTextBox.Text == null)
    //    {
    //        foreach (ListItem li in DropDownItmGrp.Items)
    //        {
    //            if (li.Selected)
    //            {
    //                ItemGroups += li.Value + ",";
    //            }
    //        }

    //        if (ItemGroups.Length > 0)
    //        {
    //            ItemGroups = ItemGroups.Substring(0, ItemGroups.Length - 1);
    //        }

    //        if (ItemGroups == "0")
    //        {
    //            divMessage.InnerHtml = "Please select a Category or an Item.";
    //            DropDownItmGrp.Focus();
    //            return;
    //        }

    //        foreach (ListItem li in lstItemGroups.Items)
    //        {
    //            if (li.Selected)
    //            {
    //                brands += "'" + li.Value.ToString() + "',";
    //            }
    //        }

    //        if (brands.Length > 0)
    //        {
    //            brands = brands.Substring(0, brands.Length - 1);
    //        }
    //        else
    //        {
    //            divMessage.InnerHtml = "Please select a Brand or an Item.";
    //            lstItemGroups.Focus();
    //            return;
    //        }
    //    }

    //    newFirstItem = 0;

    //    bindGridView1();

    //    if (newFirstItem > 0)
    //    {
    //        GreenLabel.Visible = true;
    //    }
    //    else
    //    {
    //        GreenLabel.Visible = false;
    //    }

    //    newFirstItem = 0;
    //}

    //2019-ABR-09: Agregado por Aldo Reina, para la b�squeda por c�digo de barras:
    protected void btnCreateWorksheet_Click(object sender, EventArgs e)
    {
        try
        {
            db.Connect();

            string lwhscode = drpToWhsCode.SelectedValue;
            string ItemGroups = "", brands = "";

            if (lwhscode == "0")
            {
                divMessage.InnerHtml = "Please select the Operation location.";
                drpToWhsCode.Focus();
                return;
            }

            if (string.IsNullOrEmpty(ItemTextBox.Text))
            {
                foreach (ListItem li in DropDownItmGrp.Items)
                {
                    if (li.Selected)
                    {
                        ItemGroups += li.Value + ",";
                    }
                }

                if (ItemGroups.Length > 0)
                {
                    ItemGroups = ItemGroups.Substring(0, ItemGroups.Length - 1);
                }

                if (ItemGroups == "0")
                {
                    divMessage.InnerHtml = "Please select a Category or an Item.";
                    DropDownItmGrp.Focus();
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
                    dtRow["ItemName"] = "SELECCIONE ARTICULO";

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

            BindGridView1();

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
        catch (Exception)
        {
            throw;
        }
        finally
        {
            db.Disconnect();
        }
    }

    protected void BindGridView1()
    {
        string itmsgrpcods = "", brands = "", displayAll = "", NoPlanned = "", NoInSap = "";

        foreach (ListItem di in DropDownItmGrp.Items)
        {
            if (di.Selected)
            {
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
        int bplIdBind = 0;
        int.TryParse(Session["BranchId"] as string, out bplIdBind);
        ObjectDataSource1.SelectParameters["bplId"].DefaultValue = bplIdBind.ToString();

        GridView1.DataBind();

        GridView1.Visible = true;
    }

    protected void btnSaveChanges_Click(object sender, EventArgs e)
    {
        MinmaxUpdate();
    }

    protected void MinmaxUpdate()
    {
        string msg = "";
        int dummy = 0;
        string actionPerm = "0";
        string lLoc = drpToWhsCode.Text.ToString();
        string lCategory = lstItemGroups.Text.ToString();

        //Mod MINMAXUPDATE >>>>
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
            db.Connect();
            db.cmd.Parameters.Clear();
            db.cmd.CommandText = "SMM_UACTIONS_CNT";
            db.cmd.CommandType = CommandType.StoredProcedure;
            db.cmd.Connection = db.Conn;

            db.cmd.Parameters.Add(new SqlParameter("@LoginId", SqlDbType.NVarChar));
            db.cmd.Parameters["@LoginId"].Value = lCurUser;

            db.cmd.Parameters.Add(new SqlParameter("@Action", SqlDbType.NVarChar));
            db.cmd.Parameters["@Action"].Value = "MINMAXUPDATE";

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
            db.Disconnect();
            throw new Exception("Caught exception in call procedure SMM_UACTIONS_CNT. ERROR MESSAGE : " + ex.Message);
        }

        if (actionPerm == "0")
        {
            Response.Write("<script type=\"text/javascript\">alert('" + "User " + lCurUser + " does not have permissions to modify values in location " + lLoc + " and Category " + lItmsgrpcods + ".');</script>");
            //Response.End();
            return;
        }

        //Mod MINMAXUPDATE  <<<<<<
        using (SqlTransaction tran = db.Conn.BeginTransaction())
        {
            try
            {
                db.cmd.Transaction = tran;

                foreach (GridDataItem row in GridView1.Items)
                {
                    bool hold = ((CheckBox)row.FindControl("chkHold")).Checked;
                    bool prior = ((CheckBox)row.FindControl("chkPriotity")).Checked;
                    string min = ((TextBox)row.FindControl("txtMin")).Text.Trim();
                    string max = ((TextBox)row.FindControl("txtMax")).Text.Trim();
                    string item = ((HiddenField)row.FindControl("hdnITEM")).Value;
                    string loc = ((HiddenField)row.FindControl("hdnLOC")).Value;
                    string case_pack = ((HiddenField)row.FindControl("hdnCASE_PACK")).Value;
                    string replacementItem = ((TextBox)row.FindControl("txtReplacementItem")).Text.Trim();
                    string comment = ((TextBox)row.FindControl("txtComment")).Text.Trim();

                    if (comment.Length > 100)
                        comment = comment.Substring(0, 100);

                    if (replacementItem.Length > 20)
                    {
                        msg += "<br>ERROR: Invalid replacement item number (over 20 characters long). ITEM #" + item + " was not saved.";
                        continue;
                    }

                    if (updMins == 1)
                        min = "1";

                    if (max != "" && min != "")
                    {
                        if (int.TryParse(min, out dummy) && int.TryParse(max, out dummy))
                        {
                            if (int.Parse(min) <= int.Parse(max))
                            {
                                if (updMins == 1)
                                {
                                    double lmax = Double.Parse(max);
                                    double lmintmp = Convert.ToDouble(PorcentajeTBox.Text);
                                    int lmin = Convert.ToInt32(Math.Round(((lmax * lmintmp) / 100), 0));
                                    min = lmin.ToString();
                                }
                                SetMinMax(item, loc, min, max, hold, prior, replacementItem, comment, case_pack);
                            }
                            else
                            {
                                msg += "<br>ERROR: Max quantity must be greater than min quantity. ITEM #" + item + " was not saved.";
                            }
                        }
                        else
                        {
                            msg += "<br>ERROR: Min and max quantities must be integer values. ITEM #" + item + " was not saved.";
                        }
                    }
                    else
                    {
                        DeleteMinMax(item, loc);
                    }
                }

                tran.Commit();
            }
            catch (Exception ex)
            {
                tran.Rollback();
                msg += "<br>ERROR saving changes: " + ex.Message;
            }
            finally
            {
                db.cmd.Transaction = null;
            }
        }

        updMins = 0;

        if (string.IsNullOrEmpty(msg))
            divMessage.InnerHtml = "<span style='color:green;font-weight:bold;'>Changes saved successfully.</span>";
        else
            AppendMessage(msg);

        GridView1.DataBind();
        db.Disconnect();
    }

    private void SetMinMax(string item, string loc, string min, string max, bool hold, bool prior, string replacementItem, string comment, string case_pack)
    {
        string sql;

        try
        {
            // do update if item already exists in rss_store_item_min_max
            sql = @"update
                        rss_store_item_min_max
                    set
                        min_qty = " + min + @" * " + case_pack + @",
                        max_qty = " + max + @" * " + case_pack + @",
                        hold = " + (hold ? "1" : "0") + @",
                        prior = " + (prior ? "1" : "0") + @",
                        replacement_item = " + (hold ? "'" + replacementItem + "'" : "null") + @",
                        comment = '" + comment.Replace("'", "''") + @"',
                        updated_by = '" + usr + @"',
                        date_updated = getdate()
                    where
                        CompanyId = '" + sap_db + @"' and
                        item = '" + item + @"' and
                        loc = '" + loc + "'";

            db.cmd.Parameters.Clear();  //Mod MINMAXUPDATE  <<<<<<
            db.cmd.CommandType = CommandType.Text; //Mod MINMAXUPDATE  <<<<<<
            db.cmd.CommandText = sql;
            db.cmd.ExecuteNonQuery();


            // do insert in case item does not already exist in rss_store_item_min_max
            sql = @"insert into
                        rss_store_item_min_max
                    (
                        CompanyId,
                        item,
                        loc,
                        min_qty,
                        max_qty,
                        hold,
                        prior,
                        created_by,
                        date_created,
                        updated_by,
                        date_updated
                    )
                    select
                        '" + sap_db + @"',
                        '" + item + @"',
                        '" + loc + @"',
                        " + min + @" * " + case_pack + @",
                        " + max + @" * " + case_pack + @",
                        " + (hold ? "1" : "0") + @",
                        " + (prior ? "1" : "0") + @",
                        '" + usr + @"',
                        getdate(),
                        '" + usr + @"',
                        getdate()
                    where
                        not exists
                            (
                                select
                                    1
                                from
                                    rss_store_item_min_max
                                where
                                    CompanyId = '" + sap_db + @"' and
                                    item = '" + item + @"' and
                                    loc = '" + loc + @"'
                            )";

            db.cmd.CommandText = sql;
            db.cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function SetMinMax. ERROR MESSAGE : " + ex.Message);
        }
    }

    private void DeleteMinMax(string item, string loc)
    {
        try
        {
            // do update if item already exists in rss_store_item_min_max
            string sql = @"delete from 
                        rss_store_item_min_max
                    where 
                        CompanyId = '" + sap_db + @"' and 
                        item = '" + item + @"' and 
                        loc = '" + loc + "'";

            db.cmd.Parameters.Clear(); //Mod MINMAXUPDATE  <<<<<<
            db.cmd.CommandType = CommandType.Text;  //Mod MINMAXUPDATE  <<<<<<
            db.cmd.CommandText = sql;
            db.cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function DeleteMinMax. ERROR MESSAGE : " + ex.Message);
        }
    }

    protected void AppendMessage(string msg)
    {
        divMessage.InnerHtml += msg;
    }

    protected void ObjectDataSource1_Selected(object sender, ObjectDataSourceStatusEventArgs e)
    {
        if (e.Exception != null)
        {
            e.ExceptionHandled = true;
            string msg = e.Exception.Message;
            if (msg.Contains("offline") || msg.Contains("cannot be opened"))
                divMessage.InnerHtml = "<span style='color:red;font-weight:bold;'>La base de datos SAP '" + (string)Session["CompanyId"] + "' está temporalmente fuera de línea. Contacte al administrador de base de datos.</span>";
            else
                divMessage.InnerHtml = "<span style='color:red;font-weight:bold;'>Error al cargar datos: " + msg + "</span>";
            return;
        }

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

    protected void DropDownItmGrp_SelectedIndexChanged(object sender, EventArgs e)
    {
        LoadBrands();
        ObjectDataSource1.SelectParameters["depts"].DefaultValue = "";
        ObjectDataSource1.SelectParameters["brands"].DefaultValue = "";
        GridView1.DataBind();
        divMessage.InnerHtml = "";
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
        newFirstItem = 0;

        BindGridView1();

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
        BindGridView1();
    }
    //protected void GridView1_RowDataBound(object sender, GridViewRowEventArgs e)
    //{
    //    if (e.Row.RowType == DataControlRowType.DataRow)
    //    {
    //        string daysAgeS = "";
    //        int daysAgeI = 1000;

    //        if (e.Row.RowType == DataControlRowType.DataRow)
    //        {
    //            TextBox txtMin = (TextBox)e.Row.FindControl("txtMin");
    //            TextBox txtMax = (TextBox)e.Row.FindControl("txtMax");
    //            HiddenField hdnItem = (HiddenField)e.Row.FindControl("hdnItem");
    //        }
    //    }
    //}
    //protected void GridView1_RowCreated(object sender, GridViewRowEventArgs e)
    //{
    //    if (e.Row.RowType == DataControlRowType.DataRow)
    //    {
    //        string daysAgeS = "";
    //        int daysAgeI = 1000;

    //        if (e.Row.RowType == DataControlRowType.DataRow)
    //        {
    //            TextBox txtMin = (TextBox)e.Row.FindControl("txtMin");
    //            TextBox txtMax = (TextBox)e.Row.FindControl("txtMax");
    //            HiddenField hdnItem = (HiddenField)e.Row.FindControl("hdnItem");
    //        }
    //    }
    //}
    protected void MinUpdBotton_Click(object sender, EventArgs e)
    {
        string lPorc = PorcentajeTBox.Text;
        //int lintPorc = Convert.ToInt32(PorcentajeTBox.Text);
        int dummy = 0;

        if (lPorc != "" || lPorc != null)
        {
            // min/max entered - save info
            if (int.TryParse(lPorc, out dummy))
            {
                if (int.Parse(lPorc) > 99)
                {
                    divMessage.InnerHtml = "The percentage must be numeric, greater than zero and less than 100";
                    return;
                }

                else
                {
                    if (int.Parse(lPorc) < 1)
                    {
                        divMessage.InnerHtml = "The percentage must be numeric, greater than zero and less than 100";
                        return;
                    }

                }
            }
            else
            {
                divMessage.InnerHtml = "The percentage must be numeric, greater than zero and less than 100";
                return;
            }
        }
        else
        {
            divMessage.InnerHtml = "The percentage must be numeric, greater than zero and less than 100";
            return;

        }


        updMins = 1;


        MinmaxUpdate();

        updMins = 0;

    }

    protected void btnExport_Click(object sender, EventArgs e)
    {
        try
        {
            if (GridView1.Items.Count > 0)
            {
                DataRow dr1;
                db.Connect();
                int bplIdExp = 0;
                int.TryParse(Session["BranchId"] as string, out bplIdExp);
                DataTable ciaWhs = db.GetWhsByCiaIdAndControl(sap_db, "SETMINMAX", "BODEGA", bplIdExp);
                db.Disconnect();

                DataTable dt = new DataTable();
                dt.Columns.Add("Loc", typeof(string));
                dt.Columns.Add("Item", typeof(string));
                dt.Columns.Add("Description", typeof(string));
                dt.Columns.Add("Case Pack", typeof(string));
                dt.Columns.Add("OnHand", typeof(string));
                dt.Columns.Add("Min", typeof(string));
                dt.Columns.Add("Max", typeof(string));
                dt.Columns.Add("Hold", typeof(string));
                dt.Columns.Add("Replacement Item", typeof(string));
                dt.Columns.Add("Comment", typeof(string));

                foreach (DataRow item in ciaWhs.Rows)
                {
                    dt.Columns.Add((string)item["WhsCode"], typeof(string));
                }

                foreach (GridDataItem row in GridView1.Items)
                {
                    //if (row.RowType == DataControlRowType.DataRow)
                    //{
                    dr1 = dt.NewRow();
                    dr1["Loc"] = ((HiddenField)row.FindControl("hdnLOC")).Value;
                    dr1["Item"] = ((HiddenField)row.FindControl("hdnITEM")).Value;
                    dr1["Description"] = ((HiddenField)row.FindControl("hdnITEMNAME")).Value;
                    dr1["Case Pack"] = ((HiddenField)row.FindControl("hdnCASE_PACK")).Value;
                    dr1["OnHand"] = ((HiddenField)row.FindControl("hdnOnHand")).Value;
                    dr1["Min"] = ((TextBox)row.FindControl("txtMin")).Text;
                    dr1["Max"] = ((TextBox)row.FindControl("txtMax")).Text;
                    dr1["Hold"] = ((CheckBox)row.FindControl("chkHold")).Checked ? "Y" : "N";
                    dr1["Replacement Item"] = ((TextBox)row.FindControl("txtReplacementItem")).Text;
                    dr1["Comment"] = ((TextBox)row.FindControl("txtComment")).Text;

                    foreach (DataRow item in ciaWhs.Rows)
                    {
                        dr1[(string)item["WhsCode"]] = ((HiddenField)row.FindControl("hdn" + (string)item["WhsCode"])).Value;
                    }

                    dt.Rows.Add(dr1);
                    //}
                }

                //foreach (DataRow item in ciaWhs.Rows)
                //{
                //    dt.Columns.Remove((string)item["WhsCode"]);
                //}

                if (dt.Rows.Count > 0)
                {
                    string attachment = "attachment; filename=MinMaxExport_" + dt.Rows[0]["Loc"].ToString() + ".xls";
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

    //2019-ABR-09: Agregado por Aldo Reina, para la b�squeda por c�digo de barras:
    protected void ItemList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (ItemList.SelectedValue != "-")
        {
            //Here we go, but let's do the validations again, just in the case
            //the user changed something on some field value.

            ItemTextBox.Text = ItemList.SelectedValue;
            ItemList.Visible = false;
            rbtnCancel.Visible = false;

            string lwhscode = drpToWhsCode.SelectedValue;
            string ItemGroups = "", brands = "";

            if (lwhscode == "0")
            {
                divMessage.InnerHtml = "Please select the Operation location.";
                drpToWhsCode.Focus();
                return;
            }

            if (ItemTextBox.Text == "" || ItemTextBox.Text == null)
            {
                foreach (ListItem li in DropDownItmGrp.Items)
                {
                    if (li.Selected)
                    {
                        ItemGroups += li.Value + ",";
                    }
                }

                if (ItemGroups.Length > 0)
                {
                    ItemGroups = ItemGroups.Substring(0, ItemGroups.Length - 1);
                }


                if (ItemGroups == "0")
                {
                    divMessage.InnerHtml = "Please select a Category or an Item.";
                    DropDownItmGrp.Focus();
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

            newFirstItem = 0;

            BindGridView1();

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
    }

    //2019-ABR-09: Agregado por Aldo Reina, para la b�squeda por c�digo de barras:
    protected void RbtnCancel_Click(object sender, EventArgs e)
    {
        ItemList.Visible = false;
        rbtnCancel.Visible = false;
        ItemTextBox.Text = "";
    }

    protected void GridView1_ItemDataBound(object sender, GridItemEventArgs e)
    {
        //TODO: Crear controles en la columna template existente TemplateColumn1
        if(e.Item.ItemType == GridItemType.Item)
        {

        }
    }
}

public class TemplateColumn1 : ITemplate
{
    private readonly DataTable ciaWhs;
    protected Label field;
    protected HiddenField hiddenField;

    public TemplateColumn1(DataTable ciaWhs)
    {
        this.ciaWhs = ciaWhs;
    }

    public void InstantiateIn(Control container)
    {
        field = new Label
        {
            ID = "lblITEM"
        };
        field.DataBinding += new EventHandler(Field_DataBinding);
        container.Controls.Add(field);

        hiddenField = new HiddenField
        {
            ID = "hdnLOC"
        };
        hiddenField.DataBinding += new EventHandler(HiddenField_DataBinding);
        container.Controls.Add(hiddenField);

        hiddenField = new HiddenField
        {
            ID = "hdnITEM"
        };
        hiddenField.DataBinding += new EventHandler(HiddenField_DataBinding);
        container.Controls.Add(hiddenField);

        hiddenField = new HiddenField
        {
            ID = "hdnITEMNAME"
        };
        hiddenField.DataBinding += new EventHandler(HiddenField_DataBinding);
        container.Controls.Add(hiddenField);

        hiddenField = new HiddenField
        {
            ID = "hdnCASE_PACK"
        };
        hiddenField.DataBinding += new EventHandler(HiddenField_DataBinding);
        container.Controls.Add(hiddenField);

        hiddenField = new HiddenField
        {
            ID = "hdnOnHand"
        };
        hiddenField.DataBinding += new EventHandler(HiddenField_DataBinding);
        container.Controls.Add(hiddenField);

        foreach (DataRow item in ciaWhs.Rows)
        {
            hiddenField = new HiddenField
            {
                ID = "hdn" + item["WhsCode"]
            };
            hiddenField.DataBinding += new EventHandler(HiddenField_DataBinding);
            container.Controls.Add(hiddenField);
        }
    }

    public void Field_DataBinding(object sender, EventArgs e)
    {
        try
        {
            Label field = (Label)sender;
            string colName = field.ID.Replace("lbl", "");
            GridDataItem container = (GridDataItem)field.NamingContainer;
            field.Text = ((DataRowView)container.DataItem)[colName].ToString();
        }
        catch (Exception)
        {

        }
    }

    public void HiddenField_DataBinding(object sender, EventArgs e)
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
        catch (Exception)
        {
        }
    }
}