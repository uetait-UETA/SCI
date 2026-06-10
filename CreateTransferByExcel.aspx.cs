using System;
using System.Data;
using System.Data.OleDb;
using System.Configuration;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;


public partial class createTransferByExcel : BasePage
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
    protected string lfullFilePath = "";
    protected string lFileExt = "";
    protected string appUserName;
    protected string lCurUser;
    protected int wrongTypeQty = 0;

    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            ValidaSesionUsuarioCia();

            sap_db = (string)Session["CompanyId"];

            appUserName = (string)Session["UserId"];
            char flagokay = 'N';
            Label2.Visible = false;
            Label3.Visible = false;

            ///////////////Begin New  Control de acceso por Roles
            lCurUser = (string)Session["UserId"];
            flagokay = 'Y';

            string lControlName = "createTransferByExcel.aspx";
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
                labelForm.InnerText = "Create Transfer by Excel (Read-Only Access)";
            }

            if (strAccessType == "F")
            {
                btnCreateDraft.Enabled = true;
                labelForm.InnerText = "Create Transfer by Excel (Full Access)";
            }
            ///////////////End  New Control de acceso por Roles

            if (flagokay == 'Y')
            {
                SqlDataSource1.SelectParameters["DocEntry"].DefaultValue = "13";

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
        if(Session["toWhs"] == null || (DataTable)Session["toWhs"] == null)
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
                     ISNULL(O.U_Type, '') AS WhsType
                 from " + sap_db + @".dbo.owhs O " + Queries.WITH_NOLOCK + @" , RSS_OWHS_CONTROL R " + Queries.WITH_NOLOCK + @"
                 where O.WhsCode = R.WhsCode
                   and R.Control IN ('CRETRAFROMXSAP', 'CRETRATOEXCEL')
                   AND R.CompanyId = '" + sap_db + @"'" + branchFilter + @"
              ORDER BY CASE WHEN O.BPLId = 1 THEN 0 ELSE 1 END, O.U_POSCode";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function LoadWarehouses. ERROR MESSAGE : " + ex.Message);
        }

        DataTable dt1 = dt.AsEnumerable()
            .Where(x => x.Field<string>("Control") == "CRETRAFROMXSAP")
            .CopyToDataTable();

        drpFromWhsCode.DataSource = dt1;
        drpFromWhsCode.DataBind();

        Session["fromWhs"] = dt1;

        DataTable dt2 = dt.AsEnumerable()
            .Where(x => x.Field<string>("Control") == "CRETRATOEXCEL")
            .CopyToDataTable();

        drpToWhsCode.DataSource = dt2;
        drpToWhsCode.DataBind();

        Session["toWhs"] = dt2;

        ListItem li = new ListItem("Select a location", "0");

        drpFromWhsCode.Items.Insert(0, li);
        drpToWhsCode.Items.Insert(0, li);
    }

    protected void btnCreateTransfer_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty((string)Session["UserId"]) || string.IsNullOrEmpty((string)Session["CompanyId"]))
        {
            Response.Redirect("Login1.aspx");
        }

        sap_db = (string)Session["CompanyId"];

        string lFlag = Validate1();
        if (lFlag == "N") return;

        UpdateQties();

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
                    new SqlParameter { ParameterName = "@CompanyId", SqlDbType = SqlDbType.VarChar, Value = sap_db },
                    new SqlParameter { ParameterName = "@DocEntry",  SqlDbType = SqlDbType.Int,     Value = DocEntry.Text },
                    new SqlParameter { ParameterName = "@userDraft", SqlDbType = SqlDbType.Char,    Value = appUserName }
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
            docEntry, sap_db, sap_db, appUserName, out sapDocNum);

        if (dispErr != null)
        {
            LabDocEntry.Text          = "SAP B1 Error: " + dispErr + " — The transfer was reverted.";
            btnCreateDraft.Visible    = true;
            btnCreateTransfer.Visible = false;
            btnUdateDraft.Visible     = false;
            btnCancel.Visible         = true;
            Label2.Visible            = false;
            return;
        }

        string sapRef = sapDocNum > 0
            ? " Transfer Request #" + sapDocNum + " created in SAP B1."
            : "";
        LabDocEntry.Text = " Draft " + DocEntry.Text + " completed." + sapRef;

        btnCreateDraft.Visible    = false;
        btnCreateTransfer.Visible = false;
        btnUdateDraft.Visible     = false;
        btnCancel.Text    = "Create New Draft";
        btnCancel.Visible = true;
        Label2.Visible    = false;
    }

    protected void btnCreateDraft_Click(object sender, EventArgs e)
    {
        ValidaSesionUsuarioCia();

        int execRes = 0;
        SqlCommand sqlCommand;
        SqlCommand admCmd;
        SqlDataReader admDr;

        sap_db = (string)Session["CompanyId"];

        string fromLoc = drpFromWhsCode.SelectedValue;
        string toLoc = drpToWhsCode.SelectedValue;

        int lDocEntry = 0;


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

        // Validate Excel types BEFORE reserving a sequence number
        wrongTypeQty = 0;
        UploadExcelFile();

        if (wrongTypeQty == 1)
            return;

        if(db.DbConnectionState == ConnectionState.Closed)
        {
            db.Connect();
        }

        try
        {
            admCmd = new SqlCommand()
            {
                Connection = db.Conn,
                CommandType = CommandType.Text,
                CommandText = "get_TransXsap_sequence",
                CommandTimeout = 240
            };

            admDr = admCmd.ExecuteReader();

            while (admDr.Read())
            {
                lDocEntry = Convert.ToInt32((admDr.GetString(0)));
            }
            admDr.Close();
        }

        catch (Exception ex)
        {
            Response.Write("Error when get_TransXsap_sequence: ");
            Response.Write(ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        DocEntry.Text = lDocEntry.ToString();
        LabDocEntry.Text = "Draft Number: " + lDocEntry.ToString();

        // Now that DocEntry is set, upload Excel items to staging with the correct DocEntry
        UploadDataToDataBase();

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
                CommandText = "smm_populate_TransXexcel_odrf",
                CommandTimeout = 240,
                Parameters =
                {
                    new SqlParameter
                    {
                        ParameterName = "@CompanyId",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = sap_db
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
                        ParameterName = "@DocEntry",
                        SqlDbType = SqlDbType.BigInt,
                        Value = lDocEntry
                    },
                    new SqlParameter
                    {
                        ParameterName = "@userDraft",
                        SqlDbType = SqlDbType.NVarChar,
                        Value = appUserName
                    },
                }
            };

            execRes = sqlCommand.ExecuteNonQuery();
        }

        catch (Exception ex)
        {
            Response.Write("Error when btnCreateDraft_Click was called.");
            Response.Write(ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        GridView1.DataBind();
        GridView2.DataBind();

        int rgcount = GridView1.Rows.Count;

        Label2.Visible = true;

        if (rgcount > 0)
        {
            btnCreateDraft.Visible = false;
            btnCreateTransfer.Visible = true;
            btnUdateDraft.Visible = true;
            btnCancel.Visible = true;
            Label3.Visible = true;
            LabDocEntry.Visible = true;
            Label3.Text = "Products in SAP with OnHand";
        }
        else
        {
            btnCreateDraft.Visible = true;
            btnCreateTransfer.Visible = false;
            btnUdateDraft.Visible = false;
            btnCancel.Visible = false;
            Label3.Visible = true;
            LabDocEntry.Visible = false;
            Label3.Text = "No products found in SAP with OnHand";
            Alert.Show("No products found in SAP with OnHand");
        }
    }

    protected void btnUdateDraft_Click(object sender, EventArgs e)
    {
        ValidaSesionUsuarioCia();

        sap_db = (string)Session["CompanyId"];

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
        float TmpQty = 0;
        int LvLinNum = 0;
        string qtystr = null;
        string strTmpQty = null;
        int charpos = 0;

        SqlCommand sqlCommand, sqlCommand2, sqlCommand3;

        int rg2count = GridView1.Rows.Count;

        for (int i = 0; i < rg2count; i++)
        {
            LvLinNum = Convert.ToInt32(GridView1.Rows[i].Cells[0].Text);
            strTmpQty = ((TextBox)(GridView1.Rows[i].Cells[5].Controls[1])).Text;
            //HiddenField hdn = GridView1.Rows[i].FindControl("hdnDraftQuantity");
            //string hdnValue = hdn.Value;

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
                                Value = sap_db
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
                        Value = sap_db
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
                        Value = sap_db
                    }
                }
            };

            execRes = sqlCommand3.ExecuteNonQuery();

            GridView1.DataBind();

            Label2.Visible = false;
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
            OnHand = Convert.ToInt32(GridView1.Rows[i].Cells[3].Text);
            strTmpQty = ((TextBox)GridView1.Rows[i].Cells[5].Controls[1]).Text;

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

            if (LvFlag1 == 'Y')
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

    //    TextBox LvTxB1 = new TextBox();

    //    int rg2count = GridView1.Rows.Count;

    //    string Lmsg = "Please review quantities on lines ";

    //    for (int i = 0; i < rg2count; i++)
    //    {
    //        LvLinNum = Convert.ToInt32(GridView1.Rows[i].Cells[0].Text);
    //        LvTxB1.Text = ((TextBox)(GridView1.Rows[i].Cells[5].Controls[1])).Text;

    //        if (LvTxB1.Text == "0.000000")
    //        {
    //            LvTxB1.Text = "0";
    //        }

    //        try
    //        {
    //            int x = int.Parse(LvTxB1.Text);
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
    //            LvTxB1.Text = ((TextBox)(GridView1.Rows[i].Cells[5].Controls[1])).Text;


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
    //            Lmsg = Lmsg + ". Estas deben ser mayores o iguales a cero";
    //            Alert.Show(Lmsg);
    //            return ("N");
    //        }

    //        if (acumQty == 0)
    //        {
    //            Lmsg = "Ponga cantidad mayor a cero a al menos un artÃ­culo.";
    //            Alert.Show(Lmsg);
    //            LvFlag1 = 'N';
    //            return ("N");
    //        }
    //    }

    //    int OnHand = 0;

    //    for (int i = 0; i < rg2count; i++)
    //    {
    //        LvLinNum = Convert.ToInt32(GridView1.Rows[i].Cells[0].Text);
    //        LvTxB1.Text = ((TextBox)(GridView1.Rows[i].Cells[5].Controls[1])).Text;

    //        OnHand = Convert.ToInt32(GridView1.Rows[i].Cells[3].Text);

    //        TmpQty = Convert.ToInt32(LvTxB1.Text);

    //        if (TmpQty > OnHand)
    //        {
    //            Lmsg = Lmsg + ' ' + LvLinNum + ',';
    //            LvFlag1 = 'N';
    //        }
    //    }

    //    if (LvFlag1 == 'N')
    //    {
    //        Lmsg = Lmsg + ". Estas deben ser menores o iguales al ohHand";
    //        Alert.Show(Lmsg);
    //        return ("N");
    //    }

    //    btnUdateDraft.Visible = true;

    //    return ("Y");
    //}

    protected void btnCancel_Click(object sender, EventArgs e)
    {
        ValidaSesionUsuarioCia();

        int execRes = 0;

        sap_db = (string)Session["CompanyId"];

        if (btnCancel.Text == "Delete Draft")
        {
            db.Connect();
            SqlCommand sqlCommand1;

            try
            {
                sqlCommand1 = new SqlCommand
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
                            Value = sap_db
                        }
                    }
                };

                execRes = sqlCommand1.ExecuteNonQuery();
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

        Label2.Visible = false;
    }

    protected void UploadExcelFile()
    {
        try
        {
            if (FileUpload1.HasFile)
            {
                if (!SaveFile())
                {
                    divMessage.InnerHtml = "<br>*The file was not uploaded.";
                    return;
                }
                else
                {
                    string lFile = lfullFilePath;

                    lFileExt = System.IO.Path.GetExtension(FileUpload1.FileName).ToLower();

                    string connStrXls  = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + lFile + ";Extended Properties=\"Excel 8.0;HDR=YES;IMEX=1\"";

                    string connStrXlsx = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + lFile + ";Extended Properties=\"Excel 12.0 Xml;HDR=YES;IMEX=1\"";

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
                            divMessage.InnerHtml = "<br>*The file was not uploaded because it is not an Excel file.";
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            divMessage.InnerHtml = ex.Message.ToString();
            return;
        }
    }


    protected void Button1_Click(object sender, EventArgs e)
    {

    }

    protected void importData(OleDbConnection loledbConn)
    {
        try
        {
            loledbConn.Open();
            // Create OleDbCommand object and select data from worksheet Sheet1
            OleDbCommand cmd = new OleDbCommand("SELECT * FROM [Import$]", loledbConn);

            // Create new OleDbDataAdapter
            OleDbDataAdapter oleda = new OleDbDataAdapter();

            oleda.SelectCommand = cmd;

            // Create a DataSet which will hold the data extracted from the worksheet.
            DataSet ds = new DataSet();

            // Fill the DataSet from the data extracted from the worksheet.
            oleda.Fill(ds, "myExcelData");
            DataTable dt = new DataTable();

            dt = ds.Tables[0];

            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                if (dt.Rows[i][0] == DBNull.Value && dt.Rows[i][1] == DBNull.Value && dt.Rows[i][2] == DBNull.Value)
                {
                    dt.Rows[i].Delete();
                }
            }

            dt.AcceptChanges();

            // Bind the data to the GridView
            GridView2.DataSource = dt;
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

        string whsUType = GetFromWhsUType();
        if (!string.IsNullOrEmpty(whsUType))
        {
            CheckItemTypes(whsUType);
            if (wrongTypeQty == 1)
            {
                Alert.Show("Some items do not belong to the '" + whsUType + "' operation type. Review lines highlighted in red.");
                divMessage.InnerHtml = "Some items do not belong to the '" + whsUType + "' operation type. Review lines highlighted in red.";
                return;
            }
        }
        // UploadDataToDataBase() is called from btnCreateDraft_Click after the DocEntry sequence is obtained
    }

    private string GetFromWhsUType()
    {
        DataTable dtFromWhs = (DataTable)Session["fromWhs"];
        if (dtFromWhs == null) return "";
        DataRow[] rows = dtFromWhs.Select("WhsCode = '" + drpFromWhsCode.SelectedValue + "'");
        return rows.Length > 0 ? rows[0]["WhsType"].ToString() : "";
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

        if (db.DbConnectionState == ConnectionState.Closed)
            db.Connect();

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

    protected void UploadDataToDataBase()
    {
        int execRes = 0;
        int LvLinNum = 0;

        int rg2count = GridView2.Rows.Count;

        string lItem = "";
        string lItemDesc = "";
        int lQty = 0;
        SqlCommand sqlCommand;

        for (int i = 0; i < rg2count; i++)
        {
            LvLinNum++;
            lItem = Convert.ToString(GridView2.Rows[i].Cells[0].Text);

            if (lItem == "&nbsp;")
            {
                continue;
            }

            lItemDesc = Convert.ToString(GridView2.Rows[i].Cells[1].Text);
            lQty = Convert.ToInt32(GridView2.Rows[i].Cells[2].Text);

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
                    CommandText = "smm_insTransferXexcel_stg",
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
                            ParameterName = "@FromWhsCode",
                            SqlDbType = SqlDbType.VarChar,
                            Value = drpFromWhsCode.Text
                        },
                        new SqlParameter
                        {
                            ParameterName = "@ToWhsCode",
                            SqlDbType = SqlDbType.VarChar,
                            Value = drpToWhsCode.Text
                        },
                        new SqlParameter
                        {
                            ParameterName = "@ItemCode",
                            SqlDbType = SqlDbType.VarChar,
                            Value = lItem
                        },
                        new SqlParameter
                        {
                            ParameterName = "@ItemNameXls",
                            SqlDbType = SqlDbType.VarChar,
                            Value = lItemDesc
                        },
                        new SqlParameter
                        {
                            ParameterName = "@userSap",
                            SqlDbType = SqlDbType.VarChar,
                            Value = appUserName
                        },
                        new SqlParameter
                        {
                            ParameterName = "@TmpQty",
                            SqlDbType = SqlDbType.Int,
                            Value = lQty
                        }
                    }
                };

                execRes = sqlCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                db.Disconnect();
                Response.Write("Error when smm_insTransferXexcel_stg.");
                Response.Write(ex.Message);
            }
        }

        if(db.DbConnectionState == ConnectionState.Open)
        {
            db.Disconnect();
        }
    }

    protected bool SaveFile()
    {
        string fileName = FileUpload1.FileName;
        string filePath = Server.MapPath("temp") + "\\" + fileName;
        string ext = fileName.Substring(FileUpload1.FileName.LastIndexOf("."), 4).ToLower();


        if (FileUpload1.HasFile)
        {
            //check for correct file type
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
                divMessage.InnerText += "*The file must be an Excel file (.xls or .xlsx).";
                return false;
            }
        }
        else
        {
            divMessage.InnerHtml += "*The file was not uploaded";
            return false;
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
    }
}
