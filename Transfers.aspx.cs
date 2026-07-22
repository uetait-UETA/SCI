using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using Telerik.Web.UI;
using System.Linq;

public partial class Transfers : BasePage
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
    protected string appUserName;
    protected string commentsMsg;
    protected string docdateDraft;
    protected string lCurUser;
    protected string strAccessType = "";
	
	
	#region 2021-MAR-22: Agregado para la impresión masiva, por Aldo Reina:
    //2021-MAR-22: Agregado para la impresión masiva, por Aldo Reina:
    protected string Status;
    protected string DocDate;
    protected string FromLoc;
    protected string ToLoc;
    protected string DocEntry;
    protected string colspan;
    protected string UserApp;
    private string LabelPrintDate;
    private string LabelDocEntry;
    protected string DraftUser;
    protected string DespUser;
    protected string RecUser;
    protected string order_multiple;
    protected string LvUser = "";
    protected string OriCopy = "PANTALLA del ";
    protected string Titulo = "";
    protected string SubTitulo = "";
    protected string TotProds = "";
    protected DataTable dt; 
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        ValidaSesionUsuarioCia();

        sap_db = (string)Session["CompanyId"];
        CompanyLabel.Text = sap_db;
        
        ///////////////Begin New  Control de acceso por Roles
		lCurUser = (string)Session["UserId"];
				
		#region 2021-MAR-24: Agregado para la impresión masiva, por Aldo Reina:
		LvUser = lCurUser;
		UserApp = lCurUser;
		//if (!IsPostBack)
		//{
		//	LoadCiaUserPrinters(sap_db, UserApp);
		//} 
		#endregion
				
		char flagokay = 'Y';
        string lControlName = "Transfers.aspx";
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
			labelForm.InnerText = "View Transfers (Read-Only Access)";
		}
				
		if (strAccessType == "F")
		{
			labelForm.InnerText = "View Transfers (Full Access)";
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

            if (!IsPostBack)
            {
                try
                {
                    Session["RptData"] = null;
                    Session["Status"] = "-";
                    Session["DocNum"] = "-";
                    Session["FromDate"] = "-";
                    Session["ToDate"] = "-";
                    Session["FromWhsCode"] = "-";
                    Session["ToWhsCode"] = "-";
                    Session["ItemGroups"] = "-";
                    Session["andOr1"] = "-";
                    Session["andOr2"] = "-";
                    Session["andOr3"] = "-";
                    Session["CiaLabel"] = "-";

                    db.Connect();
                    LoadWarehouses();
                    LoadItemGroups();
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
    protected void btnForceGridRefresh_Click(object sender, EventArgs e)
    {
        Session["CiaLabel"] = "";
        rgHead.Rebind();
    }

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        //ObjectDataSource1.SelectParameters["companyId"].DefaultValue = sap_db;
        CompanyLabel.Text = sap_db;
        try
        {
            //GridView1.DataBind();
            rgHead.Rebind();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in GridView1.DataBind(). ERROR MESSAGE : " + ex.Message);
        }

    }


    protected void txtDocNum_PreRender(object sender, EventArgs e)
    {
        //if ((string)this.Session["UserId"] == "x")
        //{
        //    string Lmsg = "Favor de registrarse en el sistema.";
        //    Alert.Show(Lmsg);

        //    Response.Redirect("Login1.aspx");
        //    //Response.Close();

        //}
    }
    protected void GridView1_DataBound(object sender, EventArgs e)
    {
        //StatusRadioButtonList.Items.FindByValue("All").Selected = true;


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
                     R.Control
                 from " + sap_db + @".dbo.owhs O " + Queries.WITH_NOLOCK + @" , RSS_OWHS_CONTROL R " + Queries.WITH_NOLOCK + @"
                 where O.WhsCode = R.WhsCode
                   and R.Control = 'VIEWTRA'
                   AND R.CompanyId = '" + sap_db + @"'" + branchFilter + @"
              ORDER BY CASE WHEN O.BPLId = 1 THEN 0 ELSE 1 END, O.U_POSCode";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadWarehouses. ERROR MESSAGE : " + ex.Message);
        }

        drpFromWhsCode.DataSource = dt;
        drpFromWhsCode.DataBind();

        drpToWhsCode.DataSource = dt;
        drpToWhsCode.DataBind();

        ListItem li = new ListItem("Select a location", "0");

        drpFromWhsCode.Items.Insert(0, li);
        drpToWhsCode.Items.Insert(0, li);
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
             from " + sap_db + @".dbo.oitb " + Queries.WITH_NOLOCK + @" 
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
    protected void FromDateTxt_SelectedDateChanged(object sender, Telerik.Web.UI.Calendar.SelectedDateChangedEventArgs e)
    {
        hfFromDate.Value = FromDateTxt.SelectedDate.Value.ToShortDateString().ToString();
    }

    protected void toDateTxt_SelectedDateChanged(object sender, Telerik.Web.UI.Calendar.SelectedDateChangedEventArgs e)
    {
        hfToDate.Value = toDateTxt.SelectedDate.Value.ToShortDateString().ToString();
    }

    protected void rgHead_NeedDataSource(object sender, Telerik.Web.UI.GridNeedDataSourceEventArgs e)
    {
        try
        {
            ValidaSesionUsuarioCia();

            if(Session["Status"].ToString() != StatusRadioButtonList.SelectedValue
                || Session["DocNum"].ToString() != txtDocNum.Text
                || Session["FromDate"].ToString() != hfFromDate.Value
                || Session["ToDate"].ToString() != hfToDate.Value
                || Session["FromWhsCode"].ToString() != drpFromWhsCode.SelectedValue
                || Session["ToWhsCode"].ToString() != drpToWhsCode.SelectedValue
                || Session["ItemGroups"].ToString() != drpItemGroups.SelectedValue
                || Session["andOr1"].ToString() != andOrDropDownList1.SelectedValue
                || Session["andOr2"].ToString() != andOrDropDownList2.SelectedValue
                || Session["andOr3"].ToString() != andOrDropDownList3.SelectedValue
                || Session["CiaLabel"].ToString() != CompanyLabel.Text)
            {
                int branchId = 0;
                int.TryParse(Session["BranchId"] as string, out branchId);
                Transfer ts = new Transfer();
                DataTable dt = ts.GetTransferDrafts(StatusRadioButtonList.SelectedValue, txtDocNum.Text, hfFromDate.Value, hfToDate.Value, drpFromWhsCode.SelectedValue, drpToWhsCode.SelectedValue, drpItemGroups.SelectedValue, andOrDropDownList1.SelectedValue, andOrDropDownList2.SelectedValue, andOrDropDownList3.SelectedValue, CompanyLabel.Text, branchId);
                Session["RptData"] = dt;
                Session["Status"] = StatusRadioButtonList.SelectedValue;
                Session["DocNum"] = txtDocNum.Text;
                Session["FromDate"] = hfFromDate.Value;
                Session["ToDate"] = hfToDate.Value;
                Session["FromWhsCode"] = drpFromWhsCode.SelectedValue;
                Session["ToWhsCode"] = drpToWhsCode.SelectedValue;
                Session["ItemGroups"] = drpItemGroups.SelectedValue;
                Session["andOr1"] = andOrDropDownList1.SelectedValue;
                Session["andOr2"] = andOrDropDownList2.SelectedValue;
                Session["andOr3"] = andOrDropDownList3.SelectedValue;
                Session["CiaLabel"]= CompanyLabel.Text;
            }

            if((DataTable)Session["RptData"] != null)
            {
                rgHead.DataSource = (DataTable)Session["RptData"];
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function Transfers.rgHead_NeedDataSource - ERROR MESSAGE: " + ex.Message);
        }
    }

    protected void rgHead_ItemCommand(object sender, Telerik.Web.UI.GridCommandEventArgs e)
    {
        try
        {
            ValidaSesionUsuarioCia();

            if (e.CommandName == "Details")
            {
                GridDataItem item = (GridDataItem)e.Item;
                string v_DocEntry = (e.Item as GridDataItem).GetDataKeyValue("DocEntry").ToString();
				
				#region 2021-MAR-24: Comentado para modificación de la impresión masiva, por Aldo Reina:
				//2021-MAR-24: Comentado para modificación de la impresión masiva, por Aldo Reina:
				//rwTransfers.NavigateUrl = "TransferDetails.aspx?DocEntry=" + v_DocEntry.ToString();
				#endregion
				
				#region 2021-MAR-24: Modificación para la impresión masiva, por Aldo Reina:
                //2021-MAR-24: Modificación para la impresión masiva, por Aldo Reina:
				rwTransfers.NavigateUrl = "TransferDetailsPrint.aspx?DocEntry=" + v_DocEntry.ToString();
                #endregion
                
                rwTransfers.Width = Unit.Pixel(Convert.ToInt32(Math.Round(Convert.ToDouble(hfScreenWidth.Value) * 0.98)));
                rwTransfers.Height = Unit.Pixel(Convert.ToInt32(Math.Round(Convert.ToDouble(hfScreenHeight.Value) * 0.90)));
                string script = "function f(){$find(\"" + rwTransfers.ClientID + "\").show(); Sys.Application.remove_load(f);}Sys.Application.add_load(f);";
                ScriptManager.RegisterStartupScript(Page, Page.GetType(), "key", script, true);
            }

            if (e.CommandName == "Dispatch")
            {
                if (strAccessType == "F") // Role Control LC
		        {
			        GridDataItem item = (GridDataItem)e.Item;
			        string v_DocEntry = (e.Item as GridDataItem).GetDataKeyValue("DocEntry").ToString();

			        rwTransfers.NavigateUrl = "TransferDiscreOrdf.aspx?DocEntry=" + v_DocEntry.ToString();
			        rwTransfers.Width = Unit.Pixel(Convert.ToInt32(Math.Round(Convert.ToDouble(hfScreenWidth.Value) * 0.98)));
			        rwTransfers.Height = Unit.Pixel(Convert.ToInt32(Math.Round(Convert.ToDouble(hfScreenHeight.Value) * 0.90)));

			        string script = "function f(){$find(\"" + rwTransfers.ClientID + "\").show(); Sys.Application.remove_load(f);}Sys.Application.add_load(f);";
			        ScriptManager.RegisterStartupScript(Page, Page.GetType(), "key", script, true);
		        }
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
            return;
        }
    }
	
	#region 2021-MAR-19: Agregado para la impresión masiva, por Aldo Reina:
    //2021-MAR-19: Agregado para la impresión masiva, por Aldo Reina:
    protected void PrintSelected_Click(object sender, EventArgs e)
    {
        try
        {
            ValidaSesionUsuarioCia();

            //string ipAddr;
            //int port;
            string v_DocEntry = "";
            CheckBox ch = new CheckBox();
            TextBox tx;
            string fPath = string.Empty;
            string fName = string.Empty;
            string uId = string.Empty;
            string cId = string.Empty;

            uId = (string)Session["UserId"];
            cId = (string)Session["CompanyId"];

            //if (DropDownListPrinters.SelectedItem.Value == "-1")
            //{
            //    Alert.Show("Please select a printer.");
            //}
            //else
            //{
            int checks;

                checks = (from GridDataItem d in rgHead.Items
                          select d).Where(x => (x["ChkTemplatePrint"].FindControl("CheckBoxPrintDetail") as CheckBox).Checked == true)
                             .Count();

                if (checks > 0)
                {
                    //ipAddr = DropDownListPrinters.SelectedItem.Value.Split(":".ToCharArray())[0];
                    //port = int.Parse(DropDownListPrinters.SelectedItem.Value.Split(":".ToCharArray())[1]);
                    //System.Net.IPAddress iPAddress = System.Net.IPAddress.Parse(ipAddr);
                    //System.Net.IPEndPoint iPEndPoint = new System.Net.IPEndPoint(iPAddress, port);
                    //Transfer tsf = new Transfer();
                    //DataSet dataSet = new DataSet();

                    //string currentDate = DateTime.Now.ToString("d/M/yyyy");
                    //CrystalDecisions.CrystalReports.Engine.ReportDocument reportDocument;
                    //string rptFile = "";

                    List<GridDataItem> itemsDistinct;
                    int copies;

                    itemsDistinct = (from GridDataItem d in rgHead.Items
                                     select d)
                                 .Where(x => (x["ChkTemplatePrint"].FindControl("CheckBoxPrintDetail") as CheckBox).Checked == true)
                                 .GroupBy(g => g["DocEntryPrint"].Text)
                                 .Select(s => s.First()).ToList();

                    //string msg = "";
                    string prtMsg = "";
                    bool prtRes = false;

                    //reportDocument = new CrystalDecisions.CrystalReports.Engine.ReportDocument();

                    List<DocumentsPrint> docEntries = new List<DocumentsPrint>();
                    string uri = "";

                    foreach (GridDataItem item in itemsDistinct)
                    {
                        ch = item["ChkTemplatePrint"].FindControl("CheckBoxPrintDetail") as CheckBox;

                        if (ch.Checked)
                        {
                            v_DocEntry = item["DocEntryPrint"].Text.ToString();
                            
                            if (RadioButtonListType.SelectedValue.ToString() == "detail")
                            {
                                prtRes = print_off(v_DocEntry, ref prtMsg);
                                copies = 1;
                                uri = "TransferDetailsPrint.aspx?DocEntry=";
                            }
                            else
                            {
                                if (CheckBoxSelectAllCurrentPageForPrint.Checked && int.Parse(TextBoxPrintAllCopies.Text) != 0)
                                {
                                    copies = int.Parse(TextBoxPrintAllCopies.Text);
                                }
                                else
                                {
                                    tx = item["TxtTemplatePrintCopies"].FindControl("TxtPrintCopies") as TextBox;
                                    copies = int.Parse(tx.Text);
                                }

                                uri = "DisTransferDetailsPrint.aspx?DocEntry=";
                            }

                            docEntries.Add(new DocumentsPrint(v_DocEntry, copies));
                        }
                    }

                    Session["docEntries"] = docEntries;

                    if (docEntries.Count > 0)
                    {
                        if (docEntries.Count == 1)
                        {
                            rwTransfers.NavigateUrl = uri + v_DocEntry.ToString();
                        }
                        else
                        {
                            rwTransfers.NavigateUrl = uri + "MASIVO";
                        }
                        
                        rwTransfers.Width = Unit.Pixel(Convert.ToInt32(Math.Round(Convert.ToDouble(hfScreenWidth.Value) * 0.98)));
                        rwTransfers.Height = Unit.Pixel(Convert.ToInt32(Math.Round(Convert.ToDouble(hfScreenHeight.Value) * 0.90)));
                        string script = "function f(){$find(\"" + rwTransfers.ClientID + "\").show(); Sys.Application.remove_load(f);}Sys.Application.add_load(f);";
                        ScriptManager.RegisterStartupScript(Page, Page.GetType(), "key", script, true);
                    }

                    //foreach (GridDataItem item in itemsDistinct)
                    //{
                    //    ch = item["ChkTemplatePrint"].FindControl("CheckBoxPrintDetail") as CheckBox;

                    //    if (ch.Checked)
                    //    {
                    //        v_DocEntry = item["DocEntryPrint"].Text.ToString();

                    //        fPath = Request.PhysicalApplicationPath.TrimEnd('\\') + "\\temp\\";

                    //        if (RadioButtonListType.SelectedValue.ToString() == "detail")
                    //        {
                    //            prtRes = print_off(v_DocEntry, ref prtMsg);

                    //            //If user selected Order:
                    //            fName = "PRINT_" + cId + "_" + uId + "_Ord_" + v_DocEntry + ".pdf";
                    //            rptFile = this.Server.MapPath("CrystalReportTransferOrderPrint.rpt");
                    //            dataSet.DataSetName = "DataSetPrintOrd";
                    //            copies = 1;

                    //            dt = tsf.GetTransferDetails(v_DocEntry, sap_db);
                    //            dt.TableName = "DataOrder";
                    //            dt.Columns.Add("Qty_Control");

                    //            if (dataSet.Tables.Contains(dt.TableName))
                    //            {
                    //                dataSet.Tables.Remove(dt.TableName);
                    //            }

                    //            dataSet.Tables.Add(dt);

                    //            if (dt != null && dt.Rows.Count > 0)
                    //            {
                    //                DocEntry = (DocEntry == "") ? DocEntry = "ERROR: Document not found." : dt.Rows[0]["DocEntry"].ToString().Trim().ToUpper();
                    //                Status = dt.Rows[0]["Status"].ToString().Trim().ToUpper();
                    //                DocDate = dt.Rows[0]["DocDate"].ToString().Trim().ToUpper();
                    //                FromLoc = dt.Rows[0]["FromLocName"].ToString().Trim().ToUpper();
                    //                ToLoc = dt.Rows[0]["ToLocName"].ToString().Trim().ToUpper();
                    //                order_multiple = dt.Rows[0]["order_multiple"].ToString().Trim().ToUpper();
                    //                DraftUser = dt.Rows[0]["DraftUser"].ToString().Trim().ToUpper();
                    //                DespUser = dt.Rows[0]["DespUser"].ToString().Trim().ToUpper();
                    //                OriCopy = dt.Rows[0]["oricopy"].ToString().ToUpper() + " Documento No.";
                    //                TotProds = dt.Rows[0]["TotalProds"].ToString().ToUpper();
                    //            }
                    //            else
                    //            {
                    //                DocEntry = "ERROR: Could not find document #" + DocEntry;
                    //                Status = "_";
                    //                DocDate = "_";
                    //                FromLoc = "_";
                    //                ToLoc = "_";
                    //                order_multiple = "_";
                    //                DraftUser = "_";
                    //                DespUser = "_";
                    //                OriCopy = "_";
                    //                TotProds = "_";
                    //            }

                    //            Titulo = "Dispatch Document No. " + DocEntry;

                    //            reportDocument.Load(rptFile);

                    //            if (!DocEntry.StartsWith("ERROR"))
                    //            {
                    //                reportDocument.SetDataSource(dataSet);
                    //            }

                    //            reportDocument.SetParameterValue("uId", UserApp);
                    //            reportDocument.SetParameterValue("totalProds", TotProds);
                    //            reportDocument.SetParameterValue("origen", FromLoc);
                    //            reportDocument.SetParameterValue("destino", ToLoc);
                    //            reportDocument.SetParameterValue("FechaDraft", DocDate);
                    //            reportDocument.SetParameterValue("estado", Status);
                    //            reportDocument.SetParameterValue("usuarioDraft", DraftUser);
                    //            reportDocument.SetParameterValue("usuarioDespacho", DespUser);
                    //            reportDocument.SetParameterValue("OriCopy", OriCopy);
                    //            reportDocument.SetParameterValue("Titulo", Titulo);
                    //            reportDocument.SetParameterValue("FechaPrint", currentDate);
                    //            reportDocument.SetParameterValue("DocEntry", DocEntry);
                    //        }
                    //        else
                    //        {
                    //            //If user selected Dispatch:
                    //            fName = "PRINT_" + cId + "_" + uId + "_Dis_" + v_DocEntry + ".pdf";
                    //            rptFile = this.Server.MapPath("CrystalReportTransferDispatchPrint.rpt");
                    //            dataSet.DataSetName = "DataSetPrintDis";

                    //            if (CheckBoxSelectAllCurrentPageForPrint.Checked && int.Parse(TextBoxPrintAllCopies.Text) != 0)
                    //            {
                    //                copies = int.Parse(TextBoxPrintAllCopies.Text);
                    //            }
                    //            else
                    //            {
                    //                tx = item["TxtTemplatePrintCopies"].FindControl("TxtPrintCopies") as TextBox;
                    //                copies = int.Parse(tx.Text);
                    //            }

                    //            dt = tsf.GetDisTransferDetails(v_DocEntry, sap_db);
                    //            dt.TableName = "DataDispatch";

                    //            if (dataSet.Tables.Contains(dt.TableName))
                    //            {
                    //                dataSet.Tables.Remove(dt.TableName);
                    //            }

                    //            dataSet.Tables.Add(dt);

                    //            if (dt != null && dt.Rows.Count > 0)
                    //            {
                    //                DocEntry = (DocEntry == "") ? DocEntry = "ERROR: Document not found." : dt.Rows[0]["DocEntry"].ToString().Trim().ToUpper();
                    //                Status = dt.Rows[0]["Status"].ToString().Trim();
                    //                DocDate = String.Format("{0:MM/dd/yyyy hh:mm}", dt.Rows[0]["DocDate"]);
                    //                FromLoc = dt.Rows[0]["FromLocName"].ToString().Trim();
                    //                ToLoc = dt.Rows[0]["ToLocName"].ToString().Trim();
                    //                order_multiple = dt.Rows[0]["order_multiple"].ToString().Trim();
                    //            }
                    //            else
                    //            {
                    //                DocEntry = "ERROR: Could not find document #" + DocEntry;
                    //                Status = "_";
                    //                DocDate = "_";
                    //                FromLoc = "_";
                    //                ToLoc = "_";
                    //                order_multiple = "_";
                    //            }

                    //            SubTitulo = "Dispatch/Receive Document #" + DocEntry;
                    //            Titulo = currentDate + " | " + SubTitulo;

                    //            reportDocument.Load(rptFile);
                    //            if (!DocEntry.StartsWith("ERROR"))
                    //            {
                    //                reportDocument.SetDataSource(dataSet);
                    //            }

                    //            reportDocument.SetParameterValue("Titulo", Titulo);
                    //            reportDocument.SetParameterValue("SubTitulo", SubTitulo);
                    //            reportDocument.SetParameterValue("origen", FromLoc);
                    //            reportDocument.SetParameterValue("destino", ToLoc);
                    //            reportDocument.SetParameterValue("Fecha", DocDate);
                    //            reportDocument.SetParameterValue("estado", Status);
                    //            reportDocument.SetParameterValue("uId", UserApp);
                    //        }

                    //        if (System.IO.File.Exists(fPath + fName))
                    //        {
                    //            System.IO.File.Delete(fPath + fName);
                    //        }

                    //        reportDocument.ExportToDisk(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat, fPath + fName);

                    //        for (int i = 0; i < copies; i++)
                    //        {
                    //            if (RadioButtonListType.SelectedValue == "detail")
                    //            {
                    //                if (prtRes)
                    //                {
                    //                    PrintSocket(iPEndPoint, fPath + fName);
                    //                }
                    //            }
                    //            else
                    //            {
                    //                PrintSocket(iPEndPoint, fPath + fName);
                    //            }

                    //            System.Threading.Thread.Sleep(500);

                    //            if (!DocEntry.StartsWith("ERROR"))
                    //            {
                    //                if (RadioButtonListType.SelectedValue == "detail")
                    //                {
                    //                    SetAsPrinted(DocEntry, uId, ref msg);
                    //                }
                    //            }
                    //        }

                    //        if (System.IO.File.Exists(fPath + fName))
                    //        {
                    //            System.IO.File.Delete(fPath + fName);
                    //        }
                    //    }
                    //}
                }
                else
                {
                    Alert.Show("Please select one or more orders to print.");
                }
            //}
        }
        catch (Exception ex)
        {
            Alert.Show("Caught exception in PrintSelected_Click. ERROR MESSAGE 3: " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
    }
	
	private bool SetAsPrinted(string docEntry, string usr, ref string msg)
    {
        bool res;
        string sql = @"insert into smm_Print_Control
                        (DocEntry, DocNum, PrintType, PrintUser, Date_Created, Created_By) values
                        (" + docEntry + @" , NULL, 'DESPATCH', '" + usr + @"', getdate(), '" + usr + @"')";
        
        try
        {
            db.Connect();
            db.cmd.Parameters.Clear();
            db.cmd.CommandType = CommandType.Text;
            db.cmd.CommandText = sql;
            db.cmd.Connection = db.Conn;
            db.cmd.ExecuteNonQuery();
            
            res = true;
        }
        catch (Exception ex)
        {
            msg = ex.Message;
            res = false;
        }
        finally
        {
            db.Disconnect();
        }

        return res;
    }

    private void PrintSocket(System.Net.IPEndPoint iPEndPoint, string fPath)
    {
        System.Net.Sockets.Socket cSocket = null;

        try
        {
            cSocket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp)
            {
                NoDelay = false
            };

            cSocket.Connect(iPEndPoint);
            cSocket.SendFile(fPath);
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            if (cSocket != null)
            {
                try
                {
                    cSocket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                }
                catch (Exception)
                {
                }

                try
                {
                    cSocket.Close();
                }
                catch (Exception)
                {
                }
            }
        }
    }

    protected bool print_off(string dEntry, ref string msg)
    {
        bool res;
        int lNumPrintsDone = 0;
        int lPrintsPermitted = 0;

        if (LvUser == "" || LvUser == null)
        {
            Response.Write("<script type=\"text/javascript\">alert('" + "Please log in to the system." + "');</script>");
            Response.End();
        }


        string sql = @"SELECT  cast(A.NumPrintsDone as char) NumPrintsDone, cast(B.PrintsPermitted as char) PrintsPermitted
                            from
                            (select count(a.DocEntry) NumPrintsDone
                            from smm_Print_Control a 
                            where  a.docentry =  " + dEntry + @"
                              and a.PrintType = 'DESPATCH'                                                 
                            ) A,
                            (
                            select  isnull(NumPrints,0) as PrintsPermitted
                            from SMM_LOGIN 
                            where  UPPER(LoginId) = UPPER('" + LvUser + @"')) B ";

        try
        {
            db.Connect();
            db.cmd.Parameters.Clear();
            db.cmd.CommandType = CommandType.Text;
            db.cmd.CommandText = sql;
            db.cmd.Connection = db.Conn;

            SqlDataReader lReader = db.cmd.ExecuteReader();

            while (lReader.Read())
            {
                lNumPrintsDone = Convert.ToInt32(lReader.GetString(0));
                lPrintsPermitted = Convert.ToInt32(lReader.GetString(1));
            }

            lReader.Close();
        }
        catch (Exception ex)
        {
            res = false;
            msg = "Caught exception in  print_off. ERROR MESSAGE : " + ex.Message;
        }
        finally
        {
            db.Disconnect();
        }

        if (lNumPrintsDone == 0)
        {
            res = true;
            OriCopy = "PANTALLA del ";
        }
        else
        {
            if (lNumPrintsDone > lPrintsPermitted)
            {
                res = false;
            }
            else
            {
                res = true;
            }
        }

        return res;
    }

    private void LoadCiaUserPrinters(string ciaId, string userId)
    {
        ListItem listItem;
        
        try
        {
            db.Connect();
            db.cmd.Parameters.Clear();
            db.cmd.CommandText = "S_Cia_iPrinters_ByUserAndSapCia";
            db.cmd.CommandType = CommandType.StoredProcedure;
            db.cmd.Connection = db.Conn;
            db.cmd.Parameters.Add(new SqlParameter("@UserId", SqlDbType.NVarChar));
            db.cmd.Parameters["@UserId"].Value = userId;
            db.cmd.Parameters.Add(new SqlParameter("@SapCiaId", SqlDbType.NVarChar));
            db.cmd.Parameters["@SapCiaId"].Value = ciaId;

            DataTable dtbl = new DataTable("S_Cia_iPrinters_ByUserAndSapCia");
            db.adapter.SelectCommand = db.cmd;
            db.adapter.Fill(dtbl);

            if (dtbl != null)
            {
                DropDownListPrinters.DataTextField = "IpName";
                DropDownListPrinters.DataValueField = "IpCode";
                DropDownListPrinters.DataSource = dtbl;
                DropDownListPrinters.DataBind();

                if (dtbl.Rows.Count == 1)
                {
                    DropDownListPrinters.SelectedIndex = 0;
                }
                else if (dtbl.Rows.Count > 0)
                {
                    listItem = new ListItem("Select Printer", "-1");
                    DropDownListPrinters.Items.Insert(0, listItem);
                }
            }
            else
            {
                throw new Exception("Web App could not load the printers list.");
            }
        }
        catch (Exception ex)
        {
            
            throw new Exception("Error when S_Cia_iPrinters_ByUserAndSapCia was called: " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
    }
    

    protected void CheckBoxPrintAll_CheckedChanged(object sender, EventArgs e)
    {
        CheckBox checkBox = (CheckBox)sender;
        if(RadioButtonListType.SelectedValue == "detail")
        {
            LabelPrintAllCopies.Visible = false;
            TextBoxPrintAllCopies.Visible = false;
        }
        else
        {
            LabelPrintAllCopies.Visible = checkBox.Checked;
            TextBoxPrintAllCopies.Visible = checkBox.Checked;
        }
    }
    #endregion
}
