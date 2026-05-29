using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Configuration;
using Telerik.Web.UI;
using Telerik.Windows.Documents.Spreadsheet.Model;
using WebControls = System.Web.UI.WebControls;
using Telerik.Web;
using Telerik.Web.UI.Skins;
//using static System.Net.Mime.MediaTypeNames;
//using System.Windows.Forms;
//using Telerik.Web.UI.Skins;System.Windows.Forms.TextBox
//using Telerik.Web.UI.Skins;
//using SAPbobsCOM;


public partial class UETAUpdateBarcodeSap : BasePage
{
    protected SqlDb db = new SqlDb();
    public DataTable dt = new DataTable();
    protected string lCurUser;
    public string exdescription = "";
    public int exNumbers = 0;

    private string CurrentItemSearch
    {
        get { return ViewState["CurrentItemSearch"] as string ?? ""; }
        set { ViewState["CurrentItemSearch"] = value; }
    }

    //public SapBoBusiness.UpdateOITM UdpBarcode = new SapBoBusiness.UpdateOITM();
    protected void Page_Load(object sender, EventArgs e)
    {
        if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }
        lCurUser = (string)this.Session["UserId"];
        char flagokay = 'Y';
        db.Connect();
        string lControlName = "UETAUpdateBarcodeSap.aspx";

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
            strAccessType = db.cmd.Parameters["@AccessType"].Value.ToString();
            strRole_Description = db.cmd.Parameters["@Role_Description"].Value.ToString();
        }

        catch (Exception ex)
        {
            throw new Exception("Error when SISINV_GET_ACCESSTYPE_PRC was called: " + ex.Message);
        }

        db.Conn.Close();

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



            lblTitule.InnerText = "Update Barcode by Items. (Read-Only Access)"; //////////<<<<<<<<


        }

        if (strAccessType == "F")
        {


            lblTitule.InnerText = "Update Barcode by Items. (Full Access)"; //////////<<<<<<<<


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
                }
            }
            catch (Exception ex)
            {
                ShowMessage  ("Error", "Failed in Page_Load", ex.Message.ToString());
                return;
            }
        }


    }

    protected void btnSearck_Click(object sender, EventArgs e)
    {
        ulData.Visible = false;
        rgHead.DataSource = String.Empty;
        if (this.txtItemCode.Text.Length == 0)
        {
            ShowMessage("Warning", "Validation", "Item Code is required for the search.");
            lblCodSAP.InnerHtml = "";
            lblDesc.InnerHtml = "";
            lblBarCode.InnerHtml = "";
            CurrentItemSearch = "";
            ViewState["lblCodSAP"] = "";
            ViewState["lblDesc"] = "";
            ViewState["lblBarCode"] = "";
            rgHead.Rebind();
            rgHead.Visible = false;
            this.txtItemCode.Focus();
        }
        else
        {
            CurrentItemSearch = txtItemCode.Text;
            lblCodSAP2.InnerHtml = CurrentItemSearch;
            GetData();
            this.txtItemCode.Text = "";
        }
    }

    protected void ShowMessage(string MessageType, string MessageTitle, string MessageBody)
    {
        try
        {
            SiteMaster sm = (SiteMaster)this.Master;
            sm.ShowDivMessage(MessageType, MessageTitle, MessageBody);
        }
        catch { }
    }

    private void GetDataFronLine()
    {
        try
        {
            string v_CompanyID = Session["CompanyId"].ToString(); //DFATOCUMEN
            string v_Item = lblCodSAP.InnerHtml;
            string v_Bar = lblBarCode.InnerHtml;
            string v_Sql = "";
            DataTable dtData = new DataTable();


                v_Sql = @"select a.ItemCode,ItemName, b.BcdCode from " + v_CompanyID + @"..OITM a with(NOLOCK)
                                    inner join " + v_CompanyID + @"..OBCD b with(NOLOCK) on b.ItemCode = a.ItemCode
                                    where b.BcdCode = '" + v_Item + @"' and a.ItemCode = '" + v_Item + @"'";

            db.Connect();
            db.adapter = new SqlDataAdapter(v_Sql, db.Conn);
            db.adapter.Fill(dtData);

            if (dtData.Rows.Count > 0)
            {
                lblCodSAP.InnerHtml = dtData.Rows[0]["itemcode"].ToString(); //dtData.Rows[0]["CodigoSap"].ToString();
                lblDesc.InnerHtml = dtData.Rows[0]["ItemName"].ToString();
                lblBarCode.InnerHtml = dtData.Rows[0]["CodeBars"].ToString();


            }
            else
            {

                ShowMessage("Warning", "Validation", "Item Code does not exist.");

                this.txtItemCode.Focus();
            }
        }
        catch (Exception ex)
        {
            ShowMessage("Error", "Failed to GetDataFronLine", ex.Message.ToString());
        }
    }

    private void GetData()
    {
        try
        {
            string v_CompanyID = Session["CompanyId"].ToString();
            string v_Item = txtItemCode.Text.Trim();
            string v_sqlBarcode = "";
            DataTable dtData = new DataTable();
            DataTable dtDataGrid = new DataTable();

            db.Connect();

            // 1. Try as ItemCode first
            string v_Sql = "select ItemCode, ItemName, CodeBars, ISNULL(U_Type,'') AS ItemType from " + v_CompanyID + "..OITM with(NOLOCK) where ItemCode = '" + v_Item + "'";
            db.adapter = new SqlDataAdapter(v_Sql, db.Conn);
            db.adapter.Fill(dtData);

            // 2. If not found as ItemCode, try as barcode
            if (dtData.Rows.Count == 0)
            {
                v_Sql = "select top 1 ItemCode, ItemName, CodeBars, ISNULL(U_Type,'') AS ItemType from " + v_CompanyID + "..OITM with(NOLOCK) where ItemCode in (select ItemCode from " + v_CompanyID + "..OBCD with(NOLOCK) where BcdCode = '" + v_Item + "')";
                dtData = new DataTable();
                db.adapter = new SqlDataAdapter(v_Sql, db.Conn);
                db.adapter.Fill(dtData);
            }

            if (dtData.Rows.Count > 0)
            {
                string resolvedItemCode = dtData.Rows[0]["ItemCode"].ToString();
                lblCodSAP.InnerHtml = resolvedItemCode;
                lblDesc.InnerHtml = dtData.Rows[0]["ItemName"].ToString();
                lblBarCode.InnerHtml = dtData.Rows[0]["CodeBars"].ToString();
                lblItemType.InnerHtml = dtData.Rows[0]["ItemType"].ToString();
                ViewState["lblCodSAP"] = lblCodSAP.InnerHtml;
                ViewState["lblDesc"] = lblDesc.InnerHtml;
                ViewState["lblBarCode"] = lblBarCode.InnerHtml;
                ViewState["lblItemType"] = lblItemType.InnerHtml;
                CurrentItemSearch = resolvedItemCode;

                v_sqlBarcode = "select BcdEntry, ItemCode, BcdCode, createDate, Updatedate from " + v_CompanyID + "..OBCD with(NOLOCK) where ItemCode = '" + resolvedItemCode + "'";
                db.adapter = new SqlDataAdapter(v_sqlBarcode, db.Conn);
                db.adapter.Fill(dtDataGrid);

                rgHead.DataSource = dtDataGrid;
                rgHead.DataBind();
                rgHead.Visible = true;
                ulData.Visible = true;


                //}
                //rgHead.DataSource = dtDataGrid;
                //rgHead.DataBind();

            }
            else
            {
                lblCodSAP.InnerHtml = "";
                lblDesc.InnerHtml = "";
                lblBarCode.InnerHtml = "";
                GetDataGrid();
                rgHead.DataBind();
                ulData.Visible = false;
                //rgHead.Visible = false;

                ShowMessage("Warning", "Validation", "Item Code does not exist.");

                this.txtItemCode.Focus();
            }
        }
        catch (Exception ex)
        {
            ShowMessage("Error", "Failed to Get data", ex.Message.ToString());
        }
    }

    private void GetDataGrid()
    {
        try
        {
            string v_CompanyID = Session["CompanyId"].ToString();
            string v_Item = CurrentItemSearch;

            if (string.IsNullOrEmpty(v_Item))
            {
                rgHead.DataSource = new DataTable();
                return;
            }

            // CurrentItemSearch is always the resolved ItemCode (set in GetData after lookup)
            string v_sqlBarcode = "select BcdEntry, ItemCode, BcdCode, createDate, Updatedate from " + v_CompanyID + "..OBCD with(NOLOCK) where ItemCode = '" + v_Item + "'";

            DataTable dtDataGrid = new DataTable();
            db.Connect();
            db.adapter = new SqlDataAdapter(v_sqlBarcode, db.Conn);
            db.adapter.Fill(dtDataGrid);

            rgHead.DataSource = dtDataGrid;

            if (!string.IsNullOrEmpty(ViewState["lblCodSAP"] as string))
            {
                lblCodSAP.InnerHtml = ViewState["lblCodSAP"].ToString();
                lblDesc.InnerHtml = ViewState["lblDesc"] as string ?? "";
                lblBarCode.InnerHtml = ViewState["lblBarCode"] as string ?? "";
                lblItemType.InnerHtml = ViewState["lblItemType"] as string ?? "";
                ulData.Visible = true;
            }
        }
        catch (Exception ex)
        {
            ShowMessage("Error", "Failed to Get data", ex.Message.ToString());
        }
    }

    public int GetBcdEntry()
    {
        DataTable dt = new DataTable();
        string v_CompanyID = Session["CompanyId"].ToString();
        string sql = @"select AutoKey FROM " + v_CompanyID + @".[dbo].[ONNM] T0 WHERE T0.[ObjectCode] = N'1470000062'";

        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in GetBcdEntry" + ex.Message + ' ' + sql);
        }
        finally
        {
            db.Conn.Close();
        }

        if (dt.Rows.Count > 0)
            return Convert.ToInt32(dt.Rows[0]["AutoKey"]);
        else
            return -1;
    }

    private bool ValidateCodeBar(string v_barcode, string v_ItemCode)   
    {
        bool result = false;

        try
        {
            string v_CompanyID = Session["CompanyId"].ToString(); //DFATOCUMEN
         
            string v_Sql = "";
         
            DataTable dtData = new DataTable();
            
            v_Sql = "select BcdCode, createDate, Updatedate from " + v_CompanyID + "..OBCD with(NOLOCK) where ItemCode = '" + v_ItemCode + "'  and BcdCode ='" + v_barcode + "' ";
      
            db.Connect();
            db.adapter = new SqlDataAdapter(v_Sql, db.Conn);
            db.adapter.Fill(dtData);

            if (dtData.Rows.Count > 0)
            {
                result = true; 
            }
            else
            {
                result = false;
            }        
        }
        catch (Exception ex)
        {
            ShowMessage("Error", "Failed to Get data", ex.Message.ToString());
        }
        return result ;
    }


    protected void rgHead_ItemCreated(object sender, Telerik.Web.UI.GridItemEventArgs e)
    {
        if (e.Item is Telerik.Web.UI.GridEditableItem && (e.Item.IsInEditMode))
        {
            Telerik.Web.UI.GridEditableItem editableItem = (Telerik.Web.UI.GridEditableItem)e.Item;
            SetupInputManager(editableItem);
        }
    }

    protected void rgHead_InsertCommand(object sender, Telerik.Web.UI.GridCommandEventArgs e)
    {
        string v_Barcode = "";
        bool Result = false;
        var editableItem = ((Telerik.Web.UI.GridEditableItem)e.Item);
         Hashtable values = new Hashtable();
        editableItem.ExtractValues(values);

        v_Barcode = (String)values["BcdCode"];

        if (v_Barcode != null )
        {

            Result = ValidateCodeBar(v_Barcode, this.lblCodSAP.InnerText);
            if (Result == true)
            {
                ShowMessage("Warning", "Validation", "Barcode already exists for this item.");
            }
            else
            {
                //UPdateBarcodeSap(v_Barcode, this.lblCodSAP.InnerText);
                InsertBarcode(v_Barcode, this.lblCodSAP.InnerText);
                UpdateOITMDefault(v_Barcode, this.lblCodSAP.InnerText, 1);
                UpdateONNM();

                this.txtItemCode.Text = lblCodSAP2.InnerHtml;
                GetData();
                this.txtItemCode.Text = "";
            }
        }
        else
        {
            ShowMessage("Warning", "Validation", "Barcode is a required value.");
        }

    }

    protected void rgHead_ItemDataBound(object sender, GridItemEventArgs e)
    {
        //if (e.Item is GridEditableItem && e.Item.IsInEditMode)
        if (e.Item is GridEditFormInsertItem || e.Item is GridDataInsertItem)
        {
            GridEditableItem editItem = e.Item as GridEditableItem;

            // Accede al control del campo que deseas modificar (por ejemplo, un TextBox).
            TextBox textBox = editItem.FindControl("ItemCodeTxt") as TextBox;

            if (textBox != null)
            {
                // Asigna el valor deseado al control del campo.
                textBox.Text = this.lblCodSAP.InnerText;
            }
        }

        //if (e.Item is GridDataItem)
        //{
        //    GridDataItem item = (GridDataItem)e.Item;
        //    string barcode = item["BcdCodeTxt"].Text;
        //    HyperLink link = (HyperLink)item["Codigo de Barra"].Controls[0];
        //    link.NavigateUrl = "https://www.google.com/search?q="; //+ barcode;
        //}

        //if (e.Item is GridDataItem)
        //{
        //    GridDataItem item = (GridDataItem)e.Item;
        //    string barcode = item["BcdCodeTxt"].Text;
        //    HyperLink link = (HyperLink)item["BcdCodeTxt"].Controls[0];
        //    link.NavigateUrl = "https://www.google.com/search?q=" + barcode;
        //}
    }

    //protected void rgHead_SelectedIndexChanged(object sender, EventArgs e)
    //{
    //    GridDataItem selectedItem = rgHead.SelectedItems[0] as GridDataItem;
    //    //string name = selectedItem["ItemCode"].Text;
    //    //string address = selectedItem["BcdCode"].Text;

    //    lblCodSAP.InnerHtml = selectedItem["ItemCode"].Text;
    //    lblBarCode.InnerHtml = selectedItem["BcdCode"].Text;

    //    GetDataFronLine();
    //}

    protected void rgHead_NeedDataSource(object sender, Telerik.Web.UI.GridNeedDataSourceEventArgs e)
    {
        GetDataGrid();
        //GetData();
    }


    private void SetupInputManager(Telerik.Web.UI.GridEditableItem editableItem)
    {
        
        
        //var textBox =((Telerik.Web.UI.GridTextBoxColumnEditor)editableItem.EditManager.GetColumnEditor("BcdCode")).TextBoxControl;
        //Telerik.Web.UI.InputSetting inputSetting = RadInputManager1.GetSettingByBehaviorID("txtBarcodeSettings");

        //inputSetting.TargetControls.Add(new Telerik.Web.UI.TargetInput(textBox.UniqueID, true));
        //inputSetting.InitializeOnClient = true;

        //var textIDBox = ((Telerik.Web.UI.GridTextBoxColumnEditor)editableItem.EditManager.GetColumnEditor("BcdEntry")).TextBoxControl;
        //Telerik.Web.UI.InputSetting  inputSetting1 = RadInputManager1.GetSettingByBehaviorID("txtBarcodeIdSettings");

        //inputSetting.TargetControls.Add(new Telerik.Web.UI.TargetInput(textIDBox.UniqueID, true));
        //inputSetting.InitializeOnClient = true;

        //inputSetting.Validation.IsRequired = true;

    }

    private void InsertBarcode(string v_barcode, string v_ItemCode)
    {

        int BcdEntry = GetBcdEntry();

        string companydb = Session["CompanyId"].ToString();

     
            string sql = @"insert into " + companydb + @".[dbo].[OBCD] (BcdEntry,
                                                BcdCode,BcdName,ItemCode,UomEntry,DataSource,
                                                UserSign,LogInstanc,UserSign2,UpdateDate,CreateDate) 
                                                select '" + BcdEntry + @"','" + v_barcode + @"','', '" + v_ItemCode + @"', -1, 
                                                'N', NULL , 0, 1, CONVERT(DATE,GETDATE()), NULL";

            try
            {
                db.adapter = new SqlDataAdapter(sql, db.Conn);
                db.adapter.Fill(dt);

            }

            catch (Exception ex)
            {
                throw new Exception("Caught exception in InsertBarcode" + ex.Message + ' ' + sql);
            }

            finally
            {
                db.Conn.Close();
            }

     }

    public void UpdateONNM()
    {
        int BcdEntry = GetBcdEntry() + 1;
        string companydb = Session["CompanyId"].ToString();
        string sql = @"update " + companydb + @".[dbo].[ONNM] set AutoKey = " + BcdEntry + 
                        @" WHERE [ObjectCode] = N'1470000062'";

        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);

        }

        catch (Exception ex)
        {
            throw new Exception("Caught exception in UpdateONNM" + ex.Message + ' ' + sql);
        }

        finally
        {
            db.Conn.Close();
        }
    }

    private void UpdateBarcode(string v_absentry, string v_barcode, string v_ItemCode)
    {

        string companydb = Session["CompanyId"].ToString();

        string sql = @"UPDATE " + companydb + @".[dbo].[OBCD] SET 
                                                BcdCode='" + v_barcode + 
                                                @"',UserSign2=1
                                                ,UpdateDate = CONVERT(DATE,GETDATE()) 
                                                WHERE BcdEntry = " + v_absentry ;

        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);

        }

        catch (Exception ex)
        {
            throw new Exception("Caught exception in UpdateBarcode" + ex.Message + ' ' + sql);
        }

        finally
        {
            db.Conn.Close();
        }


    }

    private void UpdateOITMDefault(string v_barcode, string v_ItemCode, int v_Type)
    {

        string companydb = Session["CompanyId"].ToString();
        string sql = "";

        if (v_Type == 1)
        {
             sql = @"UPDATE " + companydb + @".[dbo].[OITM] SET 
                                                    CodeBars='" + v_barcode + @"'
                                                    WHERE ItemCode = '" + v_ItemCode + @"'";
        }
        else
        {
             sql = @"UPDATE " + companydb + @".[dbo].[OITM] SET 
                                                    CodeBars=''
                                                    WHERE ItemCode IN (select ItemCode from " + companydb + @".[dbo].OBCD with(nolock) where BcdEntry = " + v_Type + @") 
                                                    and 
                                                    CodeBars IN (select BcdCode from " + companydb + @".[dbo].OBCD with(nolock) where BcdEntry = " + v_Type + @")" ;
        }

        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);

        }

        catch (Exception ex)
        {
            throw new Exception("Caught exception in UpdateOITMDefault" + ex.Message + ' ' + sql);
        }

        finally
        {
            db.Conn.Close();
        }


    }

    protected void rgHead_UpdateCommand(object sender, Telerik.Web.UI.GridCommandEventArgs e)
    {
        string v_Barcode = "";
        string  v_BarcodeId ="";
        string v_ItemCode = "";

        var editableItem = ((Telerik.Web.UI.GridEditableItem)e.Item);
        Hashtable values = new Hashtable();
        editableItem.ExtractValues(values);

        v_Barcode = (String)values["BcdCode"];
        v_BarcodeId = (string)values["BcdEntry"];
        v_ItemCode = (string)values["ItemCode"];

        if (v_Barcode != null)
        {
            UpdateBarcode(v_BarcodeId, v_Barcode, v_ItemCode);
            UpdateOITMDefault(v_Barcode, v_ItemCode,1);

            this.txtItemCode.Text = lblCodSAP2.InnerHtml;
            GetData();
            this.txtItemCode.Text = "";
        }
        else
        {
            ShowMessage("Warning", "Validation", "Barcode is a required value.");
        }
    }

    protected void rgHead_DeleteCommand(object sender, Telerik.Web.UI.GridCommandEventArgs e)
    {

        var dataItem = e.Item as GridDataItem;
        if (dataItem != null)
        {
            int itemID = Convert.ToInt32(dataItem.GetDataKeyValue("BcdEntry"));
            string v_Barcode = dataItem["BcdCode"].Text;
            string v_ItemCode = dataItem["ItemCode"].Text;
            
            UpdateOITMDefault(v_Barcode, v_ItemCode, itemID);
            DeleteBarcode(itemID, this.lblCodSAP.InnerText); 
            rgHead.Rebind();

            this.txtItemCode.Text = lblCodSAP2.InnerHtml;
            GetData();
            this.txtItemCode.Text = "";
        }

    }


    private void DeleteBarcode(int v_absentry, string v_ItemCode)
    {

        string companydb = Session["CompanyId"].ToString();
        string sql = @"DELETE " + companydb + @".[dbo].[OBCD] 
                                                WHERE BcdEntry = " + v_absentry;

        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);

        }

        catch (Exception ex)
        {
            throw new Exception("Caught exception in DeleteBarcode" + ex.Message + ' ' + sql);
        }

        finally
        {
            db.Conn.Close();
        }
    }


    protected void rgHead_PreRender(object sender, EventArgs e)
    {
        if (!this.IsPostBack && this.rgHead.MasterTableView.Items.Count > 1)
        {
            this.rgHead.MasterTableView.Items[1].Edit = true;
            this.rgHead.MasterTableView.Rebind();
        }
    }
}