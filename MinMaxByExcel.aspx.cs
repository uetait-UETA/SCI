using System;
using System.Data;
using System.IO;
using System.Data.OleDb;
using System.Configuration;
using System.Collections;
using System.Drawing;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Xml;

public partial class MinMaxByExcel : BasePage
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
    protected string lfullFilePath = "";
    protected string lFileExt = "";
    protected string appUserName;
    protected int qtyMinGteMax = 0;
    protected int negQty = 0;
    protected int dupQty = 0;
    protected int wrongTypeQty = 0;
    protected string lCurUser; 

    protected void Page_Load(object sender, EventArgs e)
    {
        if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

        appUserName = (string)this.Session["UserId"];

        ///////////////Begin New  Control de acceso por Roles
        lCurUser = (string)this.Session["UserId"];
        char flagokay = 'Y';
	    string lControlName = "MinMaxByExcel.aspx";
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
		    labelForm.InnerText = "Upload Min-Max by Excel (Read-Only Access)";//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
		}
		
	    if (strAccessType == "F")
		{
		    btnCreateDraft.Enabled = true;
		    labelForm.InnerText = "Upload Min-Max by Excel (Full Access)";//<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
		}		
        ///////////////End  New Control de acceso por Roles

        if (flagokay == 'Y')
        {
            sap_db = sap_db = (string)Session["CompanyId"];
            serverIP = ConfigurationManager.AppSettings.Get("serverIP");
            serverUserName = ConfigurationManager.AppSettings.Get("serverUserName");
            serverPwd = ConfigurationManager.AppSettings.Get("serverPwd");
            dbUserName = ConfigurationManager.AppSettings.Get("dbUserName");
            dbPwd = ConfigurationManager.AppSettings.Get("dbPwd");
            licenseServerIP = ConfigurationManager.AppSettings.Get("licenseServerIP");

            //usr = User.Identity.Name.ToLower().Replace("lgihome\\", "");
            string x = User.Identity.Name.ToLower();
            usr = x.Substring(x.IndexOf("\\") + 1, x.Length - x.IndexOf("\\") - 1);

            db.Connect();
            divMessage.InnerHtml = "";

            if (!IsPostBack)
            {
                LoadWarehouses();
                InitializeForm();
                db.Disconnect();
            }
        }
//        else
//        {
//            Session["FlagNoPerPag"] = "N";
//            Response.Redirect("Login1.aspx");
//        }

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

            //where WhsCode in ('BODEGA', 'TIENDA')

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadWarehouses. ERROR MESSAGE : " + ex.Message);
        }

        drpFromWhsCode.DataSource = dt;
        drpFromWhsCode.DataBind();        

        ListItem li = new ListItem("Select a location", "0");

        drpFromWhsCode.Items.Insert(0, li);

    }
    protected void btnCreateDraft_Click(object sender, EventArgs e) //Upload Min-Max Button
    {
        string fromLoc = drpFromWhsCode.SelectedValue;
        Loc = fromLoc;

        qtyMinGteMax = 0;
        negQty = 0;
        dupQty = 0;
        wrongTypeQty = 0;

        if (fromLoc == "0")
        {
            divMessage.InnerHtml = "Please Select the 'Operation'";
            //Alert.Show("Please Select the 'Operation'");
            drpFromWhsCode.Focus();
            return;
        }
        
        UploadExcelFile();
    }

   protected void UploadExcelFile()
    {
        try
        {
            if (FileUpload1.HasFile)
            {
                if (!SaveFile())
                {
                    divMessage.InnerHtml += "<br>*The File was not uploaded.";
                    Alert.Show("The File was not uploaded");
                    return;
                }
                else
                {
                    string lFile = lfullFilePath;

                    string connStrXls  = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source= "  + lFile + ";Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1;\"";

                    string connStrXlsx = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source= " + lFile + ";Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1;\"";

                    if (lFileExt == ".xls")
                    {
                        OleDbConnection oledbConn = new OleDbConnection(connStrXls);
                        importData(oledbConn);
                    }
                    else
                    {
                        if (lFileExt == ".xlsx")
                        {
                            OleDbConnection oledbConn = new OleDbConnection(connStrXlsx);
                            importData(oledbConn);
                        }
                        else
                        {
                            divMessage.InnerHtml = "<br>*The File was not uploaded because it is not Excel format.";
                            Alert.Show("The File was not uploaded because it is not Excel format");
                        }
                    }
                }
            }
            else
            {
                Alert.Show("Please select the file to upload");
            }
        }
        catch (Exception ex)
        {
            divMessage.InnerHtml = "Error processing file: " + ex.Message;
        }
    }

    protected void importData(OleDbConnection loledbConn)
    {

        try
        {
            //divMessage.InnerHtml += "<br> TP1 ";
            loledbConn.Open();
            DataTable dtExcelSchema;
            dtExcelSchema = loledbConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
            string SheetName = string.Empty;

            if (dtExcelSchema.Rows.Count > 0)
            {
                SheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
            }
            else
            {
                divMessage.InnerHtml = "Not able to load Excel";
                //ShowMasterPageMessage("Error", "Error", "Not able to load Excel");
            }
            // Create OleDbCommand object and select data from worksheet Sheet1
            //cmdExcel.CommandText = "SELECT * From [" + SheetName + "]";
            //OleDbCommand cmd = new OleDbCommand("SELECT * FROM [Import$]", loledbConn);
            OleDbCommand cmd = new OleDbCommand("SELECT * From [" + SheetName + "]", loledbConn);

            // Create new OleDbDataAdapter
            //divMessage.InnerHtml += "<br> TP2 ";
            OleDbDataAdapter oleda = new OleDbDataAdapter();

            //divMessage.InnerHtml = "<br> TP3 ";
            oleda.SelectCommand = cmd;

            // Create a DataSet which will hold the data extracted from the worksheet.
            //divMessage.InnerHtml += "<br> TP4 ";
            DataSet ds = new DataSet();

            // Fill the DataSet from the data extracted from the worksheet.
            //divMessage.InnerHtml = "<br> TP5 ";
            oleda.Fill(ds, "myExcelData");

            // Bind the data to the GridView
            //divMessage.InnerHtml += "<br> TP6 ";
            GridView2.DataSource = ds.Tables[0].DefaultView;
            //divMessage.InnerHtml += "<br> TP7 ";
            GridView2.DataBind();
        }
        catch (Exception theException)
        {
            divMessage.InnerHtml = divMessage.InnerHtml + theException.Message;
        }
        finally
        {
            // Close connection
            loledbConn.Close();
        }

        CheckRows();
        
        if (qtyMinGteMax==2)
        {
	        Alert.Show("There are Non-Numeric Values. Review lines highlighted in yellow.");
	        Label2.Text = "There are Non-Numeric Values. Review lines highlighted in yellow.";
	        return;
        }

        if (qtyMinGteMax==3)
	    {
	        Alert.Show("There are Blank Values. Review lines highlighted in yellow.");
	        Label2.Text = "There are Blank Values. Review lines highlighted in yellow.";
	        return;
	    }

	    if (qtyMinGteMax==1)
	    {
		    Alert.Show("There are Minimums Greater than the Maximum. Review lines highlighted in yellow.");
		    Label2.Text = "There are Minimums Greater than the Maximum. Review lines highlighted in yellow.";
		    return;
	    }

	    if (negQty == 1)
	    {
		    Alert.Show("There are Negative or Zero Values. Review lines highlighted in yellow.");
		    Label2.Text = "There are Negative or Zero Values. Review lines highlighted in yellow.";
		    return;
	    }

	    CheckDuplicated();
	    if (dupQty==1)
	    {
		    Alert.Show("There are Duplicate Products. Review lines highlighted in yellow.");
		    Label2.Text = "There are Duplicate Products. Review lines highlighted in yellow.";
		    return;
	    }

        string whsUType = GetWhsUType();
        if (!string.IsNullOrEmpty(whsUType))
        {
            CheckItemTypes(whsUType);
            if (wrongTypeQty == 1)
            {
                Alert.Show("Some items do not belong to the '" + whsUType + "' operation type. Review lines highlighted in red.");
                Label2.Text = "Some items do not belong to the '" + whsUType + "' operation type. Review lines highlighted in red.";
                return;
            }
        }

	    UploadDataToDataBase();
	    Label2.Text = "Products Updated in the System.";
				    
    }

    protected void UploadDataToDataBase()
    {
        int LvLinNum = 0;

        db.Connect();

        int rg2count = GridView2.Rows.Count;

        string lItem = "";
        //string lItemDesc = "";
        int lQty = 0;
        int lMin = 0;
        int lMax = 0;


        for (int i = 0; i < rg2count; i++)
        {
            LvLinNum++;
            lItem = Convert.ToString(GridView2.Rows[i].Cells[0].Text);
            if (lItem == "&nbsp;")
            {
                continue;
            }

            try
            {
                lMin = Convert.ToInt32(GridView2.Rows[i].Cells[1].Text);
		        lMax = Convert.ToInt32(GridView2.Rows[i].Cells[2].Text);

		        //TextBox2.Text = TextBox2.Text + '-' + GloVarDocEntry + '*' + LvLinNum + '*' + LvDQty+'*'+TmpQty;
		        SqlCommand sqlCommand = new SqlCommand();
		        sqlCommand.Parameters.Clear();
		        sqlCommand.CommandText = "SMM_UPLOAD_MINMAX"; 
		        sqlCommand.CommandType = CommandType.StoredProcedure;
		        sqlCommand.Connection = db.Conn;

		        sqlCommand.Parameters.Add(new SqlParameter("@CompanyId", SqlDbType.VarChar));
		        sqlCommand.Parameters["@CompanyId"].Value = sap_db;

		        sqlCommand.Parameters.Add(new SqlParameter("@item", SqlDbType.VarChar));
		        sqlCommand.Parameters["@item"].Value = lItem;

		        sqlCommand.Parameters.Add(new SqlParameter("@loc", SqlDbType.VarChar));
		        sqlCommand.Parameters["@loc"].Value = Loc;

		        sqlCommand.Parameters.Add(new SqlParameter("@min", SqlDbType.VarChar));
		        sqlCommand.Parameters["@min"].Value = lMin;

		        sqlCommand.Parameters.Add(new SqlParameter("@max", SqlDbType.VarChar));
		        sqlCommand.Parameters["@max"].Value = lMax;

		        sqlCommand.Parameters.Add(new SqlParameter("@user", SqlDbType.VarChar));
		        sqlCommand.Parameters["@user"].Value = appUserName;

		        sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Response.Write("Error when SMM_UPLOAD_MINMAX. Min:("+GridView2.Rows[i].Cells[1].Text+"), Max:("+GridView2.Rows[i].Cells[2].Text+")");
                Response.Write(ex.Message);
            }
        }
    }

    protected bool SaveFile()
    {
        string fileName = FileUpload1.FileName;
        string filePath = Server.MapPath("temp") + "\\" + fileName;
        Boolean fileOK = false;
        string ext = System.IO.Path.GetExtension(FileUpload1.FileName).ToLower();

        //divMessage.InnerText += "<br> ext: " + ext;

        if (ext==".xls" || ext==".xlsx")
        {
            fileOK = true;
        }
        else
        {
            divMessage.InnerText += "Error1: The file must be Excel type .xls or .xlsx.";
            return false;
        }

        if (FileUpload1.HasFile)
        {
           

            if (ext == ".xls" || ext == ".xlsx")
            {
                // save the file for processing
                FileUpload1.SaveAs(filePath);
                lfullFilePath = filePath;
                lFileExt = ext;
                return true; // InsertUserItems(filePath);
            }
            else
            {
                divMessage.InnerText += "Error2: The file must be Excel type .xls or .xlsx.";
                return false;
            }
        }
        else
        {
            divMessage.InnerHtml += "Error3: The File was not uploaded";
            return false;
        }
    }



    
    protected void CheckRows()
    {       

        int LvLinNum = 0;

        int rg2count = GridView2.Rows.Count;

        string lItem = "";
        //string lItemDesc = "";
        int lQty = 0;
        int lMin = 0;
        int lMax = 0;
        
        string lsMin = null;
        string lsMax = null;


        for (int i = 0; i < rg2count; i++)
        {

            LvLinNum++;
            lItem = Convert.ToString(GridView2.Rows[i].Cells[0].Text);
            if (lItem == "&nbsp;")
            {
                continue;
            }
            
            lsMin = GridView2.Rows[i].Cells[1].Text;
            lsMax = GridView2.Rows[i].Cells[2].Text;
                        
            
            if (!(String.IsNullOrEmpty(lsMin)) && !(String.IsNullOrEmpty(lsMax)))
	        {
		        if (int.TryParse(lsMin, out lMin) && int.TryParse(lsMax, out lMax))
                {       
		            if (lMin>lMax)
			        {
			            GridView2.Rows[i].BackColor = System.Drawing.Color.Yellow;
			            qtyMinGteMax = 1;

			        }
			        else
			        {
				        if (lMin < 0 || lMax < 0)
				        {
				            GridView2.Rows[i].BackColor = System.Drawing.Color.Yellow;
				            negQty = 1;
				        }
			        }
                }
                else
                {
	                GridView2.Rows[i].BackColor = System.Drawing.Color.Yellow;
	                qtyMinGteMax = 2;
                }
            }
            else
            {
                GridView2.Rows[i].BackColor = System.Drawing.Color.Yellow;
		        qtyMinGteMax = 3;
            }
        }
    }

    private string GetWhsUType()
    {
        string sql = "SELECT ISNULL(U_Type, '') FROM " + sap_db + ".dbo.OWHS WHERE WhsCode = '" + Loc + "'";
        using (SqlCommand localCmd = new SqlCommand(sql, db.Conn))
        {
            object result = localCmd.ExecuteScalar();
            return result != null ? result.ToString() : "";
        }
    }

    protected void CheckItemTypes(string whsUType)
    {
        int rowCount = GridView2.Rows.Count;
        if (rowCount == 0) return;

        List<string> itemParams = new List<string>();
        for (int i = 0; i < rowCount; i++)
        {
            string itemCode = GridView2.Rows[i].Cells[0].Text;
            if (itemCode != "&nbsp;" && !string.IsNullOrEmpty(itemCode))
                itemParams.Add("'" + itemCode.Replace("'", "''") + "'");
        }
        if (itemParams.Count == 0) return;

        string sql = "SELECT ItemCode, ISNULL(U_Type, '') AS U_Type FROM " + sap_db
                     + ".dbo.OITM WHERE ItemCode IN (" + string.Join(",", itemParams) + ")";
        DataTable dtItems = new DataTable();
        using (SqlDataAdapter localAdapter = new SqlDataAdapter(sql, db.Conn))
        {
            localAdapter.Fill(dtItems);
        }

        Dictionary<string, string> itemTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataRow row in dtItems.Rows)
            itemTypes[row["ItemCode"].ToString()] = row["U_Type"].ToString();

        for (int i = 0; i < rowCount; i++)
        {
            string itemCode = GridView2.Rows[i].Cells[0].Text;
            if (itemCode == "&nbsp;" || string.IsNullOrEmpty(itemCode)) continue;

            string itemType = itemTypes.ContainsKey(itemCode) ? itemTypes[itemCode] : "";
            if (!itemType.Equals(whsUType, StringComparison.OrdinalIgnoreCase))
            {
                GridView2.Rows[i].BackColor = Color.LightCoral;
                wrongTypeQty = 1;
            }
        }
    }

    protected void CheckDuplicated()
    {
        int LvLinNum = 0;
        int LvLinNum2 = 0;

        int rg2count = GridView2.Rows.Count;
        int rgcount = rg2count;

        string lItem = null;
        string lItem2 = null;
        
        int lMin1 = 0;
        int lMax1 = 0;
        
        int lMin2 = 0;
	    int lMax2 = 0;


        for (int i = 0; i < rg2count; i++)
        {

            LvLinNum++;
            lItem = Convert.ToString(GridView2.Rows[i].Cells[0].Text);
            if (lItem == "&nbsp;")
            {
                continue;
            }
            else
            {
                LvLinNum2 = 0;
                lMin1 = Convert.ToInt32(GridView2.Rows[i].Cells[1].Text);
                lMax1 = Convert.ToInt32(GridView2.Rows[i].Cells[2].Text);
                
                for (int ii = 0; ii < rgcount; ii++)
                {

                    LvLinNum2++;
                    lItem2 = Convert.ToString(GridView2.Rows[ii].Cells[0].Text);
		            lMin2 = Convert.ToInt32(GridView2.Rows[ii].Cells[1].Text);
		            lMax2 = Convert.ToInt32(GridView2.Rows[ii].Cells[2].Text);


                    if( (lItem == lItem2) && (i != ii) )
                    {
                        
                        if((lMin1 != lMin2))
                        {
				            GridView2.Rows[ii].BackColor = System.Drawing.Color.Yellow;
				            dupQty = 1;                        
                        }
                        
                        if((lMax1 != lMax2))
                        {
				            GridView2.Rows[ii].BackColor = System.Drawing.Color.Yellow;
				            dupQty = 1;                        
                        }
                    }
                }
            }
        }
    }
}


