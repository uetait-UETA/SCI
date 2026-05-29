using System;
using System.Data;
using Telerik.Web.UI;
using System.Data.SqlClient;
using System.Configuration;

public partial class _Default : BasePage
{
    protected SqlDb db = new SqlDb();
    protected DFBUYINGdb dfbDB = new DFBUYINGdb();
    public DataTable dt = new DataTable();
    protected string sap_db;
    bool ConsultaCEDIPrincipal;
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            ValidaSesionUsuarioCia();

            sap_db = (string)Session["CompanyId"];

            ConsultaCEDIPrincipal = ConfigurationManager.AppSettings["ConsultaCEDIPrincipal"].ToString().ToUpper() == "Y" ? true : false;

            PanelExternalCEDIData.Visible = ConsultaCEDIPrincipal;

            if (!IsPostBack)
            {

                db.Connect();
                LoadItemGroup();
                LoadLocations();
                db.Disconnect();

                Session["rgHead_NeedDataSource_RptData"] = null;
                Session["rgHead_NeedDataSource_v_StatusSQL"] = "-";
                Session["rgHead_NeedDataSource_CiaLabel"] = "-";
                Session["rgDetail_NeedDataSource_RptData"] = null;
                Session["rgDetail_NeedDataSource_v_TransferID"] = "-1";
                Session["rgDetail_NeedDataSource_CiaLabel"] = "-";
                Session["v_TransferID"] = "0";
            }
        }
        catch (Exception ex)
        {
            db.Disconnect();
            ShowMasterPageMessage("Error", "Failed in Page_Load", ex.Message.ToString());
            return;
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
    private void LoadItemGroup()
    {
        DataTable dt = new DataTable();

        try
        {
            string sql =
            @"select 
               ItmsGrpCod GroupCode, 
                lower(ItmsGrpNam) GroupID,
               --cast(ItmsGrpCod as varchar) + ' - ' + [dbo].[InitCap] (ItmsGrpNam) GroupName 
               [dbo].[InitCap] (ItmsGrpNam) GroupName 
             from " + sap_db + @".dbo.oitb " + Queries.WITH_NOLOCK + @" 
             order by GroupID ";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);

            rcbCategory.DataTextField = "GroupName";
            rcbCategory.DataValueField = "GroupID";
            rcbCategory.DataSource = dt;
            rcbCategory.DataBind();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadItemGroup. ERROR MESSAGE : " + ex.Message);
        }
    }

    private void LoadLocations()
    {
        DataTable dt = new DataTable();

        try
        {

            //string sql = "SELECT COMPANYCODE, WHSCODE, WHSNAME, WHSCODE + ' - ' + WHSNAME WHS FROM COMPANY_WHS_VW WHERE LOWER(COMPANYCODE) = '" + sap_db.ToLower() + "'";
            int branchId = 0;
            int.TryParse(Session["BranchId"] as string, out branchId);
            string branchFilter = branchId > 0 ? " AND BPLid = " + branchId : "";
            string sql = @"select WHSCODE,
                                  CONVERT(nvarchar(30), ISNULL(U_POSCode, '')) + ' - ' + WHSCODE + ' - ' + WHSNAME AS WHS
                           from " + sap_db + @"..owhs " + Queries.WITH_NOLOCK + @"
                           where whscode not in ('900','R2','RESEARCH')" + branchFilter + @"
                           ORDER BY CASE WHEN BPLId = 1 THEN 0 ELSE 1 END, U_POSCode";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);

            rcbWhs.DataTextField = "WHS";
            rcbWhs.DataValueField = "WHSCODE";
            rcbWhs.DataSource = dt;
            rcbWhs.DataBind();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadItemGroup. ERROR MESSAGE : " + ex.Message);
        }
    }
    
    protected void rcbCategory_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        ValidaSesionUsuarioCia();

        rgHead.Rebind();
        rgDetail.Visible = false;

        if(ConsultaCEDIPrincipal)
        {
            rgCIHead.Rebind();
            rgCIDetail.Visible = false;
            rgOrdenes.Rebind();
            rgOrdenesDetail.Visible = false;
        }
    }
    protected void rcbWhs_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        ValidaSesionUsuarioCia();

        rgHead.Rebind();
        rgDetail.Visible = false;

        if (ConsultaCEDIPrincipal)
        {
            rgCIHead.Rebind();
            rgCIDetail.Visible = false;
            rgOrdenes.Rebind();
            rgOrdenesDetail.Visible = false;
        }
    }
    protected void rbStatus_SelectedIndexChanged(object sender, EventArgs e)
    {
        ValidaSesionUsuarioCia();

        rgHead.Rebind();
        rgDetail.Visible = false;

        if (ConsultaCEDIPrincipal)
        {
            rgCIHead.Rebind();
            rgCIDetail.Visible = false;
            rgOrdenes.Rebind();
            rgOrdenesDetail.Visible = false;
        }
    }

    protected void rgHead_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        try
        {
            ValidaSesionUsuarioCia();

            string v_StatusSQL = string.Empty;
            switch (rbStatus.SelectedValue.ToLower())
            {
                case "o":
                    v_StatusSQL = " and Transfer_OpenClose = 'O'";
                    break;
                case "c":
                    v_StatusSQL = " and Transfer_OpenClose = 'c'";
                    break;
                case "a":
                    v_StatusSQL = "";
                    break;
            }

            if (rcbCategory.CheckedItems.Count > 0)
            {
                v_StatusSQL = v_StatusSQL + " and LOWER(CategoryName) in ('" + AnyPourpuse.GetSelectedItems(rcbCategory).Replace(",", "','") + "')";
            }
            else
            {
                v_StatusSQL = v_StatusSQL + "";
            }

            if (rcbWhs.CheckedItems.Count > 0)
            {
                v_StatusSQL = v_StatusSQL + " and to_loc in ('" + AnyPourpuse.GetSelectedItems(rcbWhs).Replace(",", "','") + "')";
            }
            else
            {
                v_StatusSQL = v_StatusSQL + "";
            }

            int branchId = 0;
            int.TryParse(Session["BranchId"] as string, out branchId);
            if (branchId > 1)
            {
                v_StatusSQL += string.Format(
                    " AND EXISTS (SELECT 1 FROM {0}.dbo.OWHS w WITH(NOLOCK)" +
                    " WHERE (w.WhsCode = from_loc OR w.WhsCode = to_loc) AND w.BPLId = {1})",
                    sap_db, branchId);
            }

            if (Session["rgHead_NeedDataSource_v_StatusSQL"].ToString() != v_StatusSQL
                || Session["rgHead_NeedDataSource_CiaLabel"].ToString() != sap_db)
            {
                string sql = Queries.With_SmmTransfersDetails() + @"
    Select company, transfer, from_loc, [dbo].[InitCap] (from_locName) as from_locName, to_loc, [dbo].[InitCap] (to_locName) as to_locName, TransferDate, [dbo].[InitCap] (case Transfer_Status when 'In_Draft' then 'In Draft' else Transfer_Status end) Transfer_Status, UserDispatch, UserReceive
    from SmmTransfersDetails
    where company = '{0}' {1}
    group by company, transfer, from_loc, from_locName, to_loc, to_locName, TransferDate, Transfer_Status, UserDispatch, UserReceive";

                sql = string.Format(sql, sap_db, v_StatusSQL);

                db.Connect();
                db.adapter = new SqlDataAdapter(sql, db.Conn);
                db.adapter.Fill(dt);
                Session["rgHead_NeedDataSource_RptData"] = dt;
                Session["rgHead_NeedDataSource_v_StatusSQL"] = v_StatusSQL;
                Session["rgHead_NeedDataSource_CiaLabel"] = sap_db;
            }

            if ((DataTable)Session["rgHead_NeedDataSource_RptData"] != null)
            {
                rgHead.DataSource = (DataTable)Session["rgHead_NeedDataSource_RptData"];
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

    protected void rgDetail_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        try
        {
            ValidaSesionUsuarioCia();

            if(Session["rgDetail_NeedDataSource_v_TransferID"].ToString() != Session["v_TransferID"].ToString()
                || Session["rgDetail_NeedDataSource_CiaLabel"].ToString() != sap_db)
            {
                string sql = Queries.With_SmmTransfersDetails() + @"
                Select company, transfer, item, [dbo].[InitCap] (itemName) itemName, from_loc,
                [dbo].[InitCap] (from_locName) from_locName, from_loc_soh, to_loc, [dbo].[InitCap] (to_locName) to_locName,
                to_loc_soh, Reserved, In_Transit, Received, case LOWER(Transfer_OpenClose) when 'o' then 'Open' when 'c' then 'Close' end Transfer_OpenClose, 
                TransferDate,  [dbo].[InitCap] (case Transfer_Status when 'In_Draft' then 'In Draft' else Transfer_Status end) Transfer_Status, 
                UserDispatch, UserReceive 
                --from  [dbo].[SMM_TRANSFERS_DETAILS_VW] 
                from  SmmTransfersDetails 
                where company = '{0}' and transfer = {1}";

                sql = string.Format(sql, sap_db, Session["v_TransferID"].ToString());

                db.Connect();
                DataTable dt1 = new DataTable();
                db.adapter = new SqlDataAdapter(sql, db.Conn);
                db.adapter.Fill(dt1);

                Session["rgDetail_NeedDataSource_RptData"] = dt1;
                Session["rgDetail_NeedDataSource_v_TransferID"] = Session["v_TransferID"].ToString();
                Session["rgDetail_NeedDataSource_CiaLabel"] = sap_db;
            }

            if ((DataTable)Session["rgDetail_NeedDataSource_RptData"] != null)
            {
                rgDetail.DataSource = (DataTable)Session["rgDetail_NeedDataSource_RptData"];
                rgDetail.Visible = true;
            }
            else
            {
                rgDetail.Visible = false;
            }
        }
        catch (Exception ex)
        {
            rgDetail.Visible = false;
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
            return;
        }
        finally
        {
            db.Disconnect();
        }
    }
    protected void rgHead_ItemCommand(object sender, GridCommandEventArgs e)
    {
        try
        {
            ValidaSesionUsuarioCia();

            if (e.CommandName == "TRANSFER")
            {
                GridDataItem item = (GridDataItem)e.Item;
                string v_TransferID = (e.Item as GridDataItem).GetDataKeyValue("transfer").ToString();
                Session["v_TransferID"] = v_TransferID.ToString();
                rgDetail.Rebind();
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
            return;
        }
    }
    
    protected void rgCIHead_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        if (ConsultaCEDIPrincipal)
        {
            try
            {
                ValidaSesionUsuarioCia();

                DataTable dtCIHead = new DataTable();

                string v_StatusSQL = string.Empty;
                switch (rbStatus.SelectedValue.ToLower())
                {
                    case "o":
                        v_StatusSQL = " and a.InvntSttus = 'O' and b.InvntSttus = 'O'";
                        break;
                    case "c":
                        v_StatusSQL = " and a.InvntSttus = 'c' and b.InvntSttus = 'c'";
                        break;
                    case "a":
                        v_StatusSQL = "";
                        break;
                }

                if (rcbCategory.CheckedItems.Count > 0)
                {
                    v_StatusSQL = v_StatusSQL + " and LOWER(e.ItmsGrpNam) in ('" + AnyPourpuse.GetSelectedItems(rcbCategory).Replace(",", "','") + "')";
                }
                else
                {
                    v_StatusSQL = v_StatusSQL + "";
                }

                if (rcbWhs.CheckedItems.Count > 0)
                {
                    v_StatusSQL = v_StatusSQL + " and b.WhsCode in ('" + AnyPourpuse.GetSelectedItems(rcbWhs).Replace(",", "','") + "')";
                }
                else
                {
                    v_StatusSQL = v_StatusSQL + "";
                }

                string sql = "select    a.DocEntry APInvoiceDocEntry,a.DocNum APInvoiceNum,a.DocDate APInvoiceDate,a.CardCode, case when a.CardCode = 'INTV2314' then 'UETALAT' when a.CardCode = 'INTV2350' then 'DFBUYING' else a.CardName end SourceCompany, " +
                             "          a.u_bol  SourceSalesOrder,a.NumAtCard SourceInvoice,b.WhsCode ToLoc,[dbo].[InitCap] (c.WhsName) ToLocName,count(distinct b.ItemCode) NumberOfItems, sum(b.Quantity) TotalQuantity, e.ItmsGrpCod as Category, e.ItmsGrpNam As CategoryName   " +
                             " from     " + sap_db + ".dbo.opch a " + Queries.WITH_NOLOCK + @"  " +
                             " inner    join " + sap_db + ".dbo.pch1 b " + Queries.WITH_NOLOCK + @"  on a.DocEntry = b.DocEntry " +
                             " inner    join " + sap_db + ".dbo.owhs c on b.whscode = c.whscode " +
                             " inner    join " + sap_db + ".dbo.oitm d on b.ItemCode = d.ItemCode " +
                             " inner    join " + sap_db + ".dbo.oitb e on d.ItmsGrpCod = e.ItmsGrpCod " +
                             " WHERE 1=1 " + v_StatusSQL +
                             " and      a.UpdInvnt = 'O' " +
                             " group    by  a.DocEntry, a.DocNum, a.DocDate, case  when a.CardCode = 'INTV2314' then 'UETALAT' when a.CardCode = 'INTV2350' then 'DFBUYING' else a.CardName end,  a.u_bol,a.NumAtCard,a.CardCode, b.WhsCode,c.WhsName, e.ItmsGrpCod, e.ItmsGrpNam  " +
                             " order    by a.DocDate, b.WhsCode";

                db.Connect();
                db.adapter = new SqlDataAdapter(sql, db.Conn);
                db.adapter.Fill(dtCIHead);

                rgCIHead.DataSource = dtCIHead;
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
    }
    protected void rgCIHead_ItemCommand(object sender, GridCommandEventArgs e)
    {
        if (ConsultaCEDIPrincipal)
        { 
            try
            {
                ValidaSesionUsuarioCia();

                if (e.CommandName == "APInvoiceDocEntry")
                {
                    GridDataItem item = (GridDataItem)e.Item;
                    string v_APInvoiceDocEntry = (e.Item as GridDataItem).GetDataKeyValue("APInvoiceDocEntry").ToString();
                    Session["v_APInvoiceDocEntry"] = v_APInvoiceDocEntry.ToString();
                    rgCIDetail.Rebind();
                }
            }
            catch (Exception ex)
            {
                ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
                return;
            }
        }
    }
    protected void rgCIDetail_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        if(ConsultaCEDIPrincipal)
        {
            try
            {
                ValidaSesionUsuarioCia();

                DataTable dtCIDetail = new DataTable();

                string v_StatusSQL = string.Empty;
                switch (rbStatus.SelectedValue.ToLower())
                {
                    case "o":
                        v_StatusSQL = " and a.InvntSttus = 'O' and b.InvntSttus = 'O'";
                        break;
                    case "c":
                        v_StatusSQL = " and a.InvntSttus = 'c' and b.InvntSttus = 'c'";
                        break;
                    case "a":
                        v_StatusSQL = "";
                        break;
                }

                string sql = "select    a.DocEntry APInvoiceDocEntry,a.DocNum APInvoiceNum,a.DocDate APInvoiceDate,case when a.CardCode = 'INTV2314' then 'UETALAT' when a.CardCode = 'INTV2350' then 'DFBUYING' else a.CardName end SourceCompany," +
                             "          a.u_bol SourceSalesOrder, a.NumAtCard SourceInvoice, a.CardCode,  b.ItemCode, [dbo].[InitCap] (b.Dscription) Dscription, b.Quantity, b.WhsCode ToLoc, [dbo].[InitCap] (c.WhsName) ToLocName " +
                             "from      " + sap_db + ".dbo.opch a " + Queries.WITH_NOLOCK + @"  inner join  " + sap_db + ".dbo.pch1 b " + Queries.WITH_NOLOCK + @"  on a.DocEntry = b.DocEntry inner join " + sap_db + ".dbo.owhs c " + Queries.WITH_NOLOCK + @" on b.whscode = c.whscode " +
                             "WHERE     1=1 " + v_StatusSQL +
                             "and       a.DocEntry = " + Session["v_APInvoiceDocEntry"].ToString() +
                             " order    by a.DocDate, b.WhsCode";

                db.Connect();
                db.adapter = new SqlDataAdapter(sql, db.Conn);
                db.adapter.Fill(dtCIDetail);

                rgCIDetail.DataSource = dtCIDetail;
                rgCIDetail.Visible = true;
            }
            catch (Exception ex)
            {
                rgCIDetail.Visible = false;
                ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
                return;
            }
            finally
            {
                db.Disconnect();
            }
        }
    }
    
    protected void rgOrdenes_ItemCommand(object sender, GridCommandEventArgs e)
    {
        if (ConsultaCEDIPrincipal)
        {
            try
            {
                ValidaSesionUsuarioCia();

                if (e.CommandName == "OrdenVenta")
                {
                    GridDataItem item = (GridDataItem)e.Item;
                    string v_OrdenVenta = (e.Item as GridDataItem).GetDataKeyValue("OrdenVenta").ToString();
                    Session["OrdenVenta"] = v_OrdenVenta.ToString();
                    rgOrdenesDetail.Rebind();
                }
            }
            catch (Exception ex)
            {
                ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
                return;
            }
        }
    }
}