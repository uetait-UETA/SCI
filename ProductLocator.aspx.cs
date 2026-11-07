using System;
using System.Data;
using System.Web.UI;
using Telerik.Web.UI;
using System.Data.SqlClient;

public partial class ProductLocator : BasePage
{
    protected SqlDb db = new SqlDb();
    public DataTable dt = new DataTable();
    protected string sap_db;
    private static string TOOLTIP_TEMPLATE = @"
                <div class=""container"" style=""width:200px;margin-left:2px;"">
                    <div class=""row"" style=""margin-left:2px;"">Store: {0}</div>
                    <div class=""row"" style=""margin-left:2px;"">Active Promos: {1}</div>
                </div>";
    protected string lCurUser;
    private int _ftSoh = 0, _ftCommitted = 0, _ftReserved = 0, _ftSohAvail = 0;
    private int _ftLocalTransit = 0, _ftColonTransit = 0, _ftTotalTransit = 0;
    private int _ftNetInventory = 0, _ftMin = 0, _ftMax = 0;
    protected void Page_Load(object sender, EventArgs e)
    {
        ///////////////Begin New  Control de acceso por Roles
        if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

        lCurUser = (string)this.Session["UserId"];
        char flagokay = 'Y';
        string lControlName = "ProductLocator.aspx";
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

            //btnCreateTransfer.Enabled = false;
            //btnCreateTransfer.ForeColor = Color.Silver;

            labelForm.InnerText = "Product Locator (Read-Only Access)"; //////////<<<<<<<<


        }

        if (strAccessType == "F")
        {

            //btnCreateTransfer.Enabled = true;

            labelForm.InnerText = "Product Locator (Full Access)"; //////////<<<<<<<<


        }


        ///////////////End  New Control de acceso por Roles

        if (flagokay == 'Y')
        {
            try
            {
                if (!IsPostBack)
                {
                    if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
                    {
                        Response.Redirect("Login1.aspx");
                    }

                    string qsItem = Request.QueryString["item"];
                    if (!string.IsNullOrEmpty(qsItem))
                    {
                        rtbItem.Text = qsItem;
                        rbtnSearch_Click(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMasterPageMessage("Error", "Failed in Page_Load", ex.Message.ToString());
                return;
            }
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

    //2019-ABR-09: Comentado por Aldo Reina, para su modificación de búsqueda por código de barras:
    //public void BindGrid()
    //{
    //    try
    //    {
    //        string v_CompanyID = Session["CompanyId"].ToString(); //DFATOCUMEN
    //        if (v_CompanyID == "" || v_CompanyID == null || v_CompanyID == string.Empty)
    //        {
    //            Response.Redirect("Login1.aspx");
    //        }
    //        else
    //        {
    //            string v_Sql = GetSqlByCompany(v_CompanyID, rtbItem.Text);

    //            db.Connect();
    //            db.adapter = new SqlDataAdapter(v_Sql, db.Conn);
    //            db.adapter.Fill(dt);

    //            if (dt.Rows.Count > 0)
    //            {
    //                rgHead.DataSource = dt;
    //                rgHead.DataBind();

    //                rgHead.Visible = true;
    //            }
    //            else
    //            {
    //                rgHead.Visible = false;
    //                ShowMasterPageMessage("Standard", "No Data", "No records for the item: " + rtbItem.Text + " in company: " + v_CompanyID);
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
    //        return;
    //    }
    //}

    //2019-ABR-09: Modificado por Aldo Reina, para la búsqueda por código de barras:
    public void BindGrid()
    {
        try
        {
            string v_CompanyID = "";
            if (string.IsNullOrEmpty((string)Session["CompanyId"]))
            {
                Response.Redirect("Login1.aspx");
            }
            else
            {
                v_CompanyID = (string)Session["CompanyId"];

                int bplId = 0;
                int.TryParse(Session["BranchId"] as string, out bplId);

                string v_Sql = GetSqlByCompany(v_CompanyID, rtbItem.Text, bplId);

                db.Connect();
                db.adapter = new SqlDataAdapter(v_Sql, db.Conn);
                db.adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    rgHead.DataSource = dt;
                    rgHead.DataBind();
                    rgHead.Visible = true;
                    //ulData.Visible = true;
                }
                else
                {
                    //If the item is not found, by itemCode, then search by barcode:
                    string barCode = rtbItem.Text;
                    dt = null; //This is just for reuse dt variable (trying to save the whole Earth by this :D)
                    dt = db.SearchItemByBarCodes(v_CompanyID, barCode);
                    DataRow row;
                    //ulData.Visible = false;
                    bool isEn = !string.Equals((string)Session["Language"], "es", StringComparison.OrdinalIgnoreCase);
                    string selectItemText = isEn ? "SELECT ITEM" : "SELECCIONE ARTICULO";

                    if (dt.Rows.Count <= 0)
                    {
                        // Barcode not found — try U_CSKU_ID as last fallback
                        DataTable dtCsku = SearchByCsku(rtbItem.Text, v_CompanyID);

                        if (dtCsku.Rows.Count == 0)
                        {
                            ItemList.Visible = false;
                            rbtnCancel.Visible = false;
                            rgHead.Visible = false;
                            ShowMasterPageMessage("Standard", "No Data", "No records for the item: " + rtbItem.Text + " in company: " + v_CompanyID);
                        }
                        else if (dtCsku.Rows.Count == 1)
                        {
                            rtbItem.Text = dtCsku.Rows[0]["ItemCode"].ToString();
                            ItemList.Visible = false;
                            rbtnCancel.Visible = false;
                            GetData();
                            BindGrid();
                        }
                        else
                        {
                            DataRow dtRow = dtCsku.NewRow();
                            dtRow["ItemCode"] = "-";
                            dtRow["ItemName"] = selectItemText;
                            dtCsku.Rows.InsertAt(dtRow, 0);

                            ItemList.DataSource = dtCsku;
                            ItemList.DataMember = "ItemCode";
                            ItemList.DataValueField = "ItemCode";
                            ItemList.DataTextField = "ItemName";
                            ItemList.DataBind();
                            ItemList.Visible = true;
                            ItemList.Width = 177;
                            ItemList.Focus();
                            ItemList.ToolTip = selectItemText;
                            rbtnCancel1.Visible = true;
                            rtbItem.Visible = false;
                            rbtnSearch.Enabled = false;
                        }
                    }
                    else if (dt.Rows.Count == 1)
                    {
                        row = dt.Rows[0];
                        rtbItem.Text = row["ItemCode"].ToString();
                        ItemList.Visible = false;
                        rbtnCancel.Visible = false;
                        GetData();
                        BindGrid();
                    }
                    else
                    {
                        DataTable dTable = dt;
                        DataRow dtRow = dTable.NewRow();
                        dtRow["ItemCode"] = "-";
                        dtRow["ItemName"] = selectItemText;

                        dt.Rows.InsertAt(dtRow, 0);

                        ItemList.DataSource = dt;
                        ItemList.DataMember = "ItemCode";
                        ItemList.DataValueField = "ItemCode";
                        ItemList.DataTextField = "ItemName";
                        ItemList.DataBind();
                        ItemList.Visible = true;
                        rbtnCancel.Visible = true;
                        rbtnSearch.Visible = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
            return;
        }
        finally
        {
            db.Disconnect();
        }
    }

    //2019-ABR-09: Agregado por Aldo Reina, para la búsqueda por código de barras:
    protected void ItemList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (ItemList.SelectedValue != "-")
        {
            rtbItem.Text = ItemList.SelectedValue;
            ClearStyleColorSize();
            ItemList.Visible = false;
            rbtnCancel.Visible = false;
            rbtnCancel1.Visible = false;
            rtbItem.Visible = true;
            rbtnSearch.Visible = true;
            rbtnSearch.Enabled = true;
            GetData();
            BindGrid();
        }
    }

    // Once a search resolves to a specific item, clear the Style/Color/Size filters so
    // they don't look like they're still part of the active search.
    private void ClearStyleColorSize()
    {
        rtbStyle.Text = "";
        rtbColor.Text = "";
        rtbSize.Text  = "";
    }

    //2019-ABR-09: Agregado por Aldo Reina, para la búsqueda por código de barras:
    protected void RbtnCancel_Click(object sender, EventArgs e)
    {
        ItemList.Visible = false;
        rbtnCancel.Visible = false;
        rbtnSearch.Visible = true; 
        rbtnSearch.Enabled = true;
        rtbItem.Text = "";

        rbtnCancel1.Visible = false;
        rtbItem.Visible = true;
        ulData.Visible = false;
        rgHead.Visible = false;
    }

    public string GetSqlByCompany(string v_CompanyID, string v_Item, int bplId = 0)
    {
        try
        {
            // Always include the default branch (BPLId=1) plus the user's selected branch.
            string bplIds = bplId > 1 ? "1, " + bplId : "1";

            // Warehouses hidden via Web.config "ProductLocatorExcludeWhs" (comma-separated codes).
            string excludeWhsClause = "";
            string excludeCfg = System.Configuration.ConfigurationManager.AppSettings["ProductLocatorExcludeWhs"] ?? "";
            string excludeList = "";
            foreach (string w in excludeCfg.Split(','))
            {
                string code = w.Trim();
                if (code.Length == 0) continue;
                excludeList += (excludeList.Length > 0 ? "," : "") + "'" + code.Replace("'", "''") + "'";
            }
            if (excludeList.Length > 0)
                excludeWhsClause = "\nAND loc NOT IN (" + excludeList.Replace("{", "{{").Replace("}", "}}") + ")";
            //string sql = @"select loc, loc_name, category_name category, item, item_desc, soh, reserved, soh_avail, local_in_transit, colon_in_transit, total_in_transit, net_inventory, min, max 
            //from dfatocumen_stock_locator_vw where item = '{0}' 
            //order by loc_name";

            string sql = Queries.With_SmmReserved() + @"
select loc, loc_name, pos_code, u_sap_store, item_type, category_name category, item, item_desc, soh, committed, reserved, soh_avail, local_in_transit, colon_in_transit, total_in_transit, net_inventory, min, max
from (
    select '{0}' company, a.WhsCode loc, dbo.InitCap(c.WhsName) loc_name, ISNULL(c.U_POSCode, '') AS pos_code, ISNULL(c.U_SAP_Store, '') AS u_sap_store, c.BPLId, b.ItmsGrpCod category_id, dbo.InitCap(d.ItmsGrpNam) category_name, a.ItemCode item, dbo.InitCap(b.ItemName) item_desc, cast(a.OnHand as int)soh, cast(isnull(a.IsCommited,0) as int) committed,cast(isnull(f.reserved,0) as int) reserved, cast(a.OnHand - isnull(f.reserved,0) as int) soh_avail, cast(isnull(e.in_transit,0) as int)local_in_transit, cast(isnull(g.Quantity,0) as int) colon_in_transit, cast(isnull(e.in_transit,0) as int) + cast(isnull(g.Quantity,0) as int) total_in_transit, cast(a.OnHand - isnull(f.reserved,0) as int) + cast(isnull(e.in_transit,0) as int) + cast(isnull(g.Quantity,0) as int) net_inventory, cast(isnull(h.Min_Qty,0) as int) min, cast(isnull(h.Max_qty,0) as int) max,
        ISNULL(b.U_Type,'') AS item_type, ISNULL(c.U_Type,'') AS whs_type
    from {0}..oitw a " + Queries.WITH_NOLOCK + @"  join {0}..oitm b " + Queries.WITH_NOLOCK + @"  on a.itemCode = b.itemCode 
    join {0}..owhs c " + Queries.WITH_NOLOCK + @" on a.WhsCode = c.WhsCode 
    join {0}..oitb d " + Queries.WITH_NOLOCK + @" on b.ItmsGrpCod = d.ItmsGrpCod 
    left outer join (
        select company, to_loc loc, item, sum(case when Reserved > 0 then Reserved else In_Transit end) In_Transit
        from (
            select CompanyId as company, DocEntry as [transfer], ItemCode as item, itemName, ItmsGrpCod as Category, ItmsGrpNam As CategoryName, FromWhsCode as from_loc, FromWhsName AS from_locName, FromWhsCode_OnHand AS from_loc_soh, ToWhsCode as to_loc, ToWhsName AS to_locName, ToWhsCode_OnHand AS to_loc_soh, isnull(case when DocStatus = 'O' then case when isnull(Dispatched,'N') = 'N' then DraftQuantity else 0 end end, 0) AS Reserved, isnull(case when DocStatus = 'O' then case when isnull(Dispatched,'N') = 'Y' then DispatchQuantity else 0 end end, 0) AS In_Transit, isnull(case when DocStatus = 'C' then case when isnull(Received,'N') = 'Y' then ReceivedQuantity else 0 end end, 0) AS Received, DocStatus As Transfer_OpenClose, DocDate As TransferDate, Case when isnull(Dispatched, 'N') = 'N' then 'In_Draft' else Case when isnull(Dispatched, 'Y') = 'Y' then Case when isnull(Received,'N') = 'N'   then 'In_Transit' else 'Received' end end end AS Transfer_Status,UserDispatch, UserReceive
            from (
                SELECT CompanyId, DocEntry, DocStatus, DocDate, FromWhsCode, FromWhsName, ToWhsCode, ToWhsName, Dispatched, DispCompleted, Received, ReceCompleted, DocEntryTraDis, DocEntryTraRec, DocEntryTraRec2, UserDispatch, UserReceive, CreateDate, Created_By, InputType, ScanStatus, UserRecScanner, LineNum, ItemCode, itemName, ItmsGrpCod, ItmsGrpNam, DraftQuantity, DispatchQuantity, ReceivedQuantity, Price, FromWhsCode_OnHand, ToWhsCode_OnHand 
                FROM (
                    SELECT a.CompanyId, a.DocEntry, a.DocStatus, a.DocDate,  a.Filler As FromWhsCode, c.WhsName As FromWhsName, b.WhsCode As ToWhsCode, d.WhsName as ToWhsName, 'N' as Dispatched, NULL as DispCompleted, 'N' as Received, NULL ReceCompleted, NUll as DocEntryTraDis, NULL as DocEntryTraRec, NULL as DocEntryTraRec2, NULL as UserDispatch, NULL as UserReceive, a.CreateDate, NULL as Created_By, NULL as InputType, NULL as ScanStatus, NULL as UserRecScanner, b.LineNum, b.ItemCode, e.itemName, e.ItmsGrpCod, ee.ItmsGrpNam, b.Quantity as DraftQuantity, 0 DispatchQuantity, 0 as ReceivedQuantity, b.Price, f.OnHand AS FromWhsCode_OnHand, g.OnHand AS ToWhsCode_OnHand 
                    FROM dbo.SMM_ODRF AS a " + Queries.WITH_NOLOCK + @" 
                    INNER JOIN dbo.SMM_DRF1 AS b " + Queries.WITH_NOLOCK + @" ON a.DocEntry = b.DocEntry
                    INNER JOIN {0}..OWHS AS c " + Queries.WITH_NOLOCK + @" ON a.Filler = c.WhsCode 
                    INNER JOIN {0}..OWHS AS d " + Queries.WITH_NOLOCK + @" ON b.WhsCode = d.WhsCode 
                    INNER JOIN {0}..OITM AS e " + Queries.WITH_NOLOCK + @" ON b.ItemCode = e.ItemCode 
                    INNER JOIN {0}..OITB AS ee " + Queries.WITH_NOLOCK + @" ON e.ItmsGrpCod = ee.ItmsGrpCod 
                    INNER JOIN {0}..OITW AS f " + Queries.WITH_NOLOCK + @" ON f.whscode = a.Filler and f.itemcode = b.ItemCode 
                    INNER JOIN {0}..OITW AS g " + Queries.WITH_NOLOCK + @" ON g.whscode = b.WhsCode and g.itemcode = b.ItemCode 
                    WHERE a.CompanyId = '{0}' AND Not exists (select 1 from smm_Transdiscrep_odrf aa " + Queries.WITH_NOLOCK + @" where aa.docentry = a.DocEntry)
                    UNION
                    SELECT a.CompanyId, a.DocEntry, a.DocStatus, a.DocDate, a.FromWhsCode, a.FromWhsName, a.ToWhsCode, a.ToWhsName,  a.Dispatched, a.DispCompleted, a.Received, a.ReceCompleted, a.DocEntryTraDis, a.DocEntryTraRec, a.DocEntryTraRec2, a.UserDispatch, a.UserReceive, a.Date_Created, a.Created_By, a.InputType, a.ScanStatus, a.UserRecScanner, b.LineNum, b.ItemCode, b.ItemName,  e.ItmsGrpCod, ee.ItmsGrpNam, b.DraftQuantity, b.DispatchQuantity, b.ReceivedQuantity, b.Price, f.OnHand AS FromWhsCode_OnHand, g.OnHand AS ToWhsCode_OnHand 
                    FROM smm_Transdiscrep_odrf AS a " + Queries.WITH_NOLOCK + @" 
                    INNER JOIN smm_Transdiscrep_drf1 AS b" + Queries.WITH_NOLOCK + @" ON a.DocEntry = b.DocEntry
                    INNER JOIN {0}..OITM AS e " + Queries.WITH_NOLOCK + @" ON b.ItemCode = e.ItemCode
                    INNER JOIN {0}..OITB AS ee " + Queries.WITH_NOLOCK + @" ON e.ItmsGrpCod = ee.ItmsGrpCod
                    INNER JOIN {0}..OITW AS f " + Queries.WITH_NOLOCK + @" ON f.whscode = a.FromWhsCode and f.itemcode = b.ItemCode
                    INNER JOIN {0}..OITW AS g " + Queries.WITH_NOLOCK + @" ON g.whscode = a.ToWhsCode and g.itemcode = b.ItemCode 
                    where a.CompanyId = '{0}'
                ) T4
            ) T3
        ) T2
        group by company, item, to_loc
        having sum(case when Reserved > 0 then Reserved else In_Transit end) > 0
    ) e on a.WhsCode = e.loc and e.company = '{0}' and a.ItemCode = e.item 
    left outer join SmmReserved f " + Queries.WITH_NOLOCK + @" on a.WhsCode = f.loc and f.company = '{0}' and a.ItemCode = f.item 
    left outer join smm_in_transit_from_colon_vw g " + Queries.WITH_NOLOCK + @" on a.WhsCode = g.WhsCode and a.ItemCode = g.ItemCode
    left outer join rss_store_item_min_max h " + Queries.WITH_NOLOCK + @" on a.WhsCode = h.LOC and a.ItemCode = h.Item
) T1
where item = '{1}'
AND loc IN (SELECT WhsCode FROM {0}..OWHS " + Queries.WITH_NOLOCK + @" WHERE BPLId IN (" + bplIds + @"))
AND (item_type IN ('', 'NON SELL') OR item_type = whs_type)" + excludeWhsClause + @"
order by CASE WHEN BPLId = 1 THEN 0 ELSE 1 END, pos_code, loc_name";

            sql = string.Format(sql, v_CompanyID, v_Item);

            return sql;
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
            return "-1";
        }
    }
    protected void rbtnSearch_Click(object sender, EventArgs e)
    {
        ulData.Visible = false;

        // If Item/Barcode is empty but any of Style/Color/Size was filled, search by
        // those instead — they aren't a unique key, so this can land on a picker list
        // just like the barcode/CSKU fallback in BindGrid() does.
        if (string.IsNullOrWhiteSpace(rtbItem.Text) &&
            (!string.IsNullOrWhiteSpace(rtbStyle.Text) || !string.IsNullOrWhiteSpace(rtbColor.Text) || !string.IsNullOrWhiteSpace(rtbSize.Text)))
        {
            string v_CompanyID = (string)Session["CompanyId"];
            DataTable dtMatch = SearchByStyleColorSize(
                rtbStyle.Text.Trim(), rtbColor.Text.Trim(), rtbSize.Text.Trim(), v_CompanyID);
            HandleMultiItemResult(dtMatch, "No items found for the given Style/Color/Size.");
            return;
        }

        bool goToRebind = ValidateByBarCode("", rtbItem.Text);
        if (goToRebind)
        {
            GetData();
            BindGrid();
        }
    }

    // Shared 0/1/many-matches branching, same UX as the barcode/CSKU fallback in
    // BindGrid(): none found → message; exactly one → go straight to it; several →
    // populate the ItemList picker.
    private void HandleMultiItemResult(DataTable dtResult, string notFoundMessage)
    {
        bool isEn = !string.Equals((string)Session["Language"], "es", StringComparison.OrdinalIgnoreCase);
        string selectItemText = isEn ? "SELECT ITEM" : "SELECCIONE ARTICULO";

        if (dtResult.Rows.Count == 0)
        {
            ItemList.Visible = false;
            rbtnCancel.Visible = false;
            rgHead.Visible = false;
            ShowMasterPageMessage("Standard", "No Data", notFoundMessage);
        }
        else if (dtResult.Rows.Count == 1)
        {
            rtbItem.Text = dtResult.Rows[0]["ItemCode"].ToString();
            ClearStyleColorSize();
            ItemList.Visible = false;
            rbtnCancel.Visible = false;
            GetData();
            BindGrid();
        }
        else
        {
            DataRow dtRow = dtResult.NewRow();
            dtRow["ItemCode"] = "-";
            dtRow["ItemName"] = selectItemText;
            dtResult.Rows.InsertAt(dtRow, 0);

            ItemList.DataSource = dtResult;
            ItemList.DataMember = "ItemCode";
            ItemList.DataValueField = "ItemCode";
            ItemList.DataTextField = "ItemName";
            ItemList.DataBind();
            ItemList.Visible = true;
            ItemList.Width = 177;
            ItemList.Focus();
            ItemList.ToolTip = selectItemText;
            rbtnCancel1.Visible = true;
            rtbItem.Visible = false;
            rbtnSearch.Enabled = false;
        }
    }

    private DataTable SearchByStyleColorSize(string style, string color, string size, string companyId)
    {
        var dtResult = new DataTable();
        var localDb = new SqlDb();
        localDb.Connect();
        try
        {
            string sql =
                "SELECT ItemCode, " +
                "LTRIM(RTRIM(ItemCode)) + ' | ' + LTRIM(RTRIM(ItemName)) " +
                "+ ' | CSKU: '  + ISNULL(U_CSKU_ID,'') " +
                "+ ' | Style: ' + ISNULL(U_VendorRefNum,'') " +
                "+ ' | Color: ' + ISNULL(U_VendorColorDesc,'') " +
                "+ ' | Size: '  + ISNULL(U_VendorSizeDesc,'') AS ItemName " +
                "FROM " + companyId + "..OITM WITH(NOLOCK) WHERE 1=1" +
                (string.IsNullOrEmpty(style) ? "" : " AND U_VendorRefNum   LIKE @style") +
                (string.IsNullOrEmpty(color) ? "" : " AND U_VendorColorDesc LIKE @color") +
                (string.IsNullOrEmpty(size)  ? "" : " AND U_VendorSizeDesc  LIKE @size");

            using (var cmd = new SqlCommand(sql, localDb.Conn))
            {
                if (!string.IsNullOrEmpty(style)) cmd.Parameters.AddWithValue("@style", "%" + style + "%");
                if (!string.IsNullOrEmpty(color)) cmd.Parameters.AddWithValue("@color", "%" + color + "%");
                if (!string.IsNullOrEmpty(size))  cmd.Parameters.AddWithValue("@size",  "%" + size  + "%");
                new SqlDataAdapter(cmd).Fill(dtResult);
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message);
        }
        finally { localDb.Disconnect(); }
        return dtResult;
    }

    protected void rgHead_PageIndexChanged(object sender, GridPageChangedEventArgs e)
    {
        GetData();
        BindGrid();
    }

    protected void rgHead_SortCommand(object sender, GridSortCommandEventArgs e)
    {
        GetData();
        BindGrid();
    }
    private void GetData()
    {
        try
        {
            string v_CompanyID = "";

            if(string.IsNullOrEmpty((string)Session["CompanyId"]))
            {
                Response.Redirect("Login1.aspx");
            }

            v_CompanyID = (string)Session["CompanyId"];

            string v_Item = rtbItem.Text;
            string v_Sql = "";
            DataTable dtData = new DataTable();

        //    v_Sql = @"select distinct 
        //    STUFF((SELECT ' - ' + BcdCode FROM {0}.dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=om.itemcode FOR XML PATH ('')), 1, 3, '') AS Posibles_CodigoBarras, 
        //    om.itemcode, 
        //    STUFF((SELECT '|' + a1.ItemCode FROM {0}.dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.BcdCode = ob.BcdCode FOR XML PATH ('')), 1, 1, '') AS CodigoSap, 
        //    max(om.ItemName) AS Description
        //from {0}..OBCD AS ob " + Queries.WITH_NOLOCK + @" , {0}..OITM AS om " + Queries.WITH_NOLOCK + @" 
        //where ob.itemcode = om.itemcode and (om.itemcode = '{1}' or ob.BcdCode = '{1}')
        //group by om.itemcode, ob.BcdCode /*, [dbo].[FULLBCODEPRODS] (om.itemcode), [dbo].[SAP_PRODS_BY_BARCODE] (ob.BcdCode)*/";

            v_Sql = @"select distinct
            ISNULL(STUFF((SELECT ' | ' + b.BcdCode FROM {0}..OBCD b " + Queries.WITH_NOLOCK + @" WHERE b.ItemCode = om.ItemCode FOR XML PATH('')), 1, 3, ''), om.CodeBars) AS Posibles_CodigoBarras,
            om.itemcode,
            om.ItemCode AS CodigoSap,
            om.ItemName AS Description,
            ISNULL(om.U_Type,'') AS ItemType,
            ISNULL(grp.ItmsGrpNam,'') AS ItmsGrpNam,
            ISNULL(om.U_CSKU_ID,'') AS Csku_ID,
            ISNULL(om.U_VendorRefNum,'') AS Style,
            ISNULL(om.U_VendorColorDesc,'') AS ColorDesc,
            ISNULL(om.U_VendorSizeDesc,'') AS SizeDesc,
            ISNULL(LEFT(om.U_Hierarchy, CHARINDEX(' - ', om.U_Hierarchy + ' - ') - 1), '') AS SubclassValue,
            ISNULL(dcs.U_PRDCT_CLSS_NM,'') AS ClassValue,
            ISNULL(om.U_HSCODE,'') AS HsCode,
            ISNULL(org.Name,'') AS OriginName,
            ISNULL(om.U_Bot, 0) AS UnitsPerCase,
            ISNULL(pl.Price, 0) AS RetailPrice
        from {0}..OITM AS om " + Queries.WITH_NOLOCK + @"
        left join {0}..OITB AS grp " + Queries.WITH_NOLOCK + @" on grp.ItmsGrpCod = om.ItmsGrpCod
        left join {0}..ITM1 AS pl " + Queries.WITH_NOLOCK + @" on pl.ItemCode = om.ItemCode and pl.PriceList = {2}
        left join {0}..[@ORIGIN] AS org " + Queries.WITH_NOLOCK + @" on org.Code = om.U_Origin
        left join {0}..[@DCS_HIERARCHY] AS dcs " + Queries.WITH_NOLOCK + @" on dcs.CODE = om.U_Hierarchy
        where om.itemcode = '{1}' ";

            // Retail price list by branch: 4 (SFO) → list 2 "SFO Retail Price";
            // anything else, incl. 3 (LAX) → list 1 "LAX Retail Price".
            int bplId = 0;
            int.TryParse(Session["BranchId"] as string, out bplId);
            int priceList = bplId == 4 ? 2 : 1;

            v_Sql = string.Format(v_Sql, v_CompanyID, v_Item, priceList);

            db.Connect();
            db.adapter = new SqlDataAdapter(v_Sql, db.Conn);
            db.adapter.Fill(dtData);

            if (dtData.Rows.Count > 0)
            {
                lblCodSAP.InnerHtml = dtData.Rows[0]["itemcode"].ToString();
                lblDesc.InnerHtml = dtData.Rows[0]["Description"].ToString();
                lblItemType.InnerHtml = dtData.Rows[0]["ItemType"].ToString();
                lblItmsGrpNam.InnerHtml = dtData.Rows[0]["ItmsGrpNam"].ToString();
                lblBarCode.InnerHtml = dtData.Rows[0]["Posibles_CodigoBarras"].ToString();
                lblCskuId.InnerHtml = dtData.Rows[0]["Csku_Id"].ToString();
                lblStyle.InnerHtml = dtData.Rows[0]["Style"].ToString();
                lblColor.InnerHtml = dtData.Rows[0]["ColorDesc"].ToString();
                lblSize.InnerHtml = dtData.Rows[0]["SizeDesc"].ToString();
                lblClass.InnerHtml = dtData.Rows[0]["ClassValue"].ToString();
                lblSubclass.InnerHtml = dtData.Rows[0]["SubclassValue"].ToString();
                lblHsCode.InnerHtml = dtData.Rows[0]["HsCode"].ToString();
                lblOrigin.InnerHtml = dtData.Rows[0]["OriginName"].ToString();
                lblUnitsPerCase.InnerHtml = Convert.ToInt32(dtData.Rows[0]["UnitsPerCase"]).ToString();
                lblRetailPrice.InnerHtml = Convert.ToDecimal(dtData.Rows[0]["RetailPrice"]).ToString("N2");
                ulData.Visible = true;
            }
            else
            {
                ulData.Visible = false;
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
        }
        finally
        {
            db.Disconnect();
        }
    }

    private bool ValidateByBarCode(string grpId, string barCode)
    {
        bool res = false;
        DataTable dt = null;
        DataRow row;
        try
        {
            DataManager dm = new DataManager();
            dt = dm.SearchItemByBarCodes((string)this.Session["CompanyId"], grpId, barCode);

            if (dt.Rows.Count <= 0)
            {
                ItemList.Visible = false;
                rbtnCancel1.Visible = false;

                rtbItem.Visible = true;

                rbtnSearch.Enabled = true;

                //If the item is not found, just go on for the binding. Then, it won't show the
                //table if the item code provided is a bar code (probably user will faint here :D)
                //because no messages are showed here o.o!
                res = true;
            }
            else if (dt.Rows.Count == 1)
            {
                row = dt.Rows[0];
                rtbItem.Text = row["ItemCode"].ToString();
                ItemList.Visible = false;
                rbtnCancel1.Visible = false;

                rtbItem.Visible = true;

                rbtnSearch.Enabled = true;

                //Here just go on to the bind function.
                res = true;
            }
            else
            {
                bool isEn = !string.Equals((string)Session["Language"], "es", StringComparison.OrdinalIgnoreCase);
                string selectItemText = isEn ? "SELECT ITEM" : "SELECCIONE ARTICULO";

                DataTable dTable = dt;
                DataRow dtRow = dTable.NewRow();
                dtRow["ItemCode"] = "-";
                dtRow["ItemName"] = selectItemText;

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
                ItemList.ToolTip = selectItemText;

                rtbItem.Visible = false;

                rbtnSearch.Enabled = false;

                res = false;
            }
            return res;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    // Uses the existing db.Conn (already open from BindGrid) — no Connect/Disconnect.
    private DataTable SearchByCsku(string csku, string companyId)
    {
        var dtResult = new DataTable();
        try
        {
            string sql =
                "SELECT ItemCode, " +
                "CASE WHEN U_Type='Duty Free' THEN 'DF | ' " +
                "     WHEN U_Type='Duty Paid'  THEN 'DP | ' ELSE '' END " +
                "+ LTRIM(RTRIM(ItemCode)) + ' | ' + LTRIM(RTRIM(ItemName)) AS ItemName " +
                "FROM " + companyId + "..OITM WITH(NOLOCK) WHERE U_CSKU_ID = @csku";
            using (var cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@csku", csku);
                new SqlDataAdapter(cmd).Fill(dtResult);
            }
        }
        catch { }
        return dtResult;
    }

    protected void rgHead_ItemDataBound(object sender, GridItemEventArgs e)
    {
        if (e.Item is GridDataItem)
        {
            var drv = (DataRowView)e.Item.DataItem;
            _ftSoh          += Convert.ToInt32(drv["soh"]);
            _ftCommitted    += Convert.ToInt32(drv["committed"]);
            _ftReserved     += Convert.ToInt32(drv["reserved"]);
            _ftSohAvail     += Convert.ToInt32(drv["soh_avail"]);
            _ftLocalTransit += Convert.ToInt32(drv["local_in_transit"]);
            _ftColonTransit += Convert.ToInt32(drv["colon_in_transit"]);
            _ftTotalTransit += Convert.ToInt32(drv["total_in_transit"]);
            _ftNetInventory += Convert.ToInt32(drv["net_inventory"]);
            _ftMin          += Convert.ToInt32(drv["min"]);
            _ftMax          += Convert.ToInt32(drv["max"]);
        }
        else if (e.Item is GridFooterItem)
        {
            var footer = (GridFooterItem)e.Item;
            footer["pos_code"].Text         = "Total";
            footer["soh"].Text              = _ftSoh.ToString("N0");
            footer["committed"].Text        = _ftCommitted.ToString("N0");
            footer["reserved"].Text         = _ftReserved.ToString("N0");
            footer["soh_avail"].Text        = _ftSohAvail.ToString("N0");
            footer["local_in_transit"].Text = _ftLocalTransit.ToString("N0");
            footer["colon_in_transit"].Text = _ftColonTransit.ToString("N0");
            footer["total_in_transit"].Text = _ftTotalTransit.ToString("N0");
            footer["net_inventory"].Text    = _ftNetInventory.ToString("N0");
            footer["min"].Text              = _ftMin.ToString("N0");
            footer["max"].Text              = _ftMax.ToString("N0");
        }
    }

    //protected void rtbItem_DataSourceSelect(object sender, SearchBoxDataSourceSelectEventArgs e)
    //{
    //    SqlDataSource ds = (SqlDataSource)e.DataSource;
    //    RadSearchBox searchBox = (RadSearchBox)sender;
    //    string query = searchBox.Text;
    //    string sq3 = "SELECT TOP 1000 ItemCode FROM " + Session["CompanyId"].ToString() + ".dbo.OITM " + Queries.WITH_NOLOCK + @"  WHERE ItemCode LIKE '%" + searchBox.Text + "%' UNION SELECT TOP 1000 ItemCode FROM " + Session["CompanyId"].ToString() + ".dbo.OBCD " + Queries.WITH_NOLOCK + @"  WHERE BcdCode LIKE '%" + searchBox.Text + "%'";

    //    ds.SelectCommand = sq3;
    //}
}