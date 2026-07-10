using System;
using System.Configuration;
using System.Data;
using System.Collections;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;

public partial class TransferDiscreOrdf : System.Web.UI.Page
{
    protected SqlDb db = new SqlDb();
    protected string sap_db;

    private static readonly HashSet<string> AllowedUsers =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "MPEGUEROBOD","CBERROABOD","LCONCEPCIONBOD","GCRUZBOD","GCRUZ","CCRUZBOD","YRIJOBOD","MPEGUERO","CBERROA","LCONCEPCION","CCRUZ","YRIJO", "YFELICIANO", "AGALVEZ", "MMARTINEZBOD", "AABREGO", "DGILTIE", "YFELICIANOBOD", "WFERNANDEZBOD", "WFERNANDEZ" ,"ICASTILLO" ,"ICASTILLOBOD"};

    string LvParameters = null;
    string LvDispatched = null;
    string LvReceived = null;
    string LvUserDisp = null;
    int GloVarDocEntry    = 0;
    int GloVarDocNum      = 0;
    int GloVarDocNumITR   = 0;
    int GloVarDocEntryITR = 0;
    char flagPerDeskay = 'N';
    char flagPerReckay = 'N';
    char GloVarDesRec = 'X';


    protected void Page_Load(object sender, EventArgs e)
    {
        LabelCurUser.Text = "User: " + (string)this.Session["UserId"];
	if (!IsPostBack){
            //var user = (string)this.Session["UserId"];
            //ZeroCheckBox.Visible = AllowedUsers.Contains(user);
            ZeroCheckBox.Visible = false;
			}
    }

    protected void Panel1_Load(object sender, EventArgs e)
    {
        if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

        sap_db = (string)this.Session["CompanyId"];
        CompanyLabel.Text = sap_db;

        ArrayList controles = new ArrayList();
        controles = (ArrayList)this.Session["Controles"];

        ArrayList roles = new ArrayList();
        roles = (ArrayList)this.Session["Roles"];

        ArrayList permissions = new ArrayList();
        permissions = (ArrayList)this.Session["Permissions"];

        string thiscontrol = "";
        char flagokay = 'N';

        for (int i = 0; i < controles.Count; i++)
        {
            thiscontrol = (controles[i].ToString());
            if ((thiscontrol == "TransferDiscreOrdf.aspx") || (thiscontrol == "ATOTAL"))
            {
                flagokay = 'Y';
            }
        }

        if (flagokay == 'N')
        {
            Response.Write("<script type=\"text/javascript\">alert('" + "This User does not have permissions for this option, please log in with another user." + "');</script>");
            Response.End();
        }

        string thispermission = "";

        for (int i = 0; i < permissions.Count; i++)
        {
            thispermission = "";
            thispermission = (permissions[i].ToString());

            if ((thispermission == "DESPATCH") || (thispermission == "ATOTAL"))
            {
                flagPerDeskay = 'Y';
            }
        }

        for (int i = 0; i < permissions.Count; i++)
        {
            thispermission = "";
            thispermission = (permissions[i].ToString());

            if ((thispermission == "RECEIVE") || (thispermission == "ATOTAL"))
            {
                flagPerReckay = 'Y';
            }
        }

        db.Connect();

        string DocEntry = "";

        if (Request.QueryString["DocEntry"] == null)
        {
            db.Disconnect();
            Response.Write("ERROR: No document entry number specified in querystring.<br>");
            Response.End();
        }
        else
        {
            DocEntry = Request.QueryString["DocEntry"].ToString();
            GloVarDocEntry = Convert.ToInt32(DocEntry);
            DocEntryLabel.Text = DocEntry;

            try
            {
                using (var rdCmd = new SqlCommand(
                    "SELECT ISNULL(DocNum,0), ISNULL(DocNumITR,0), ISNULL(DocEntryITR,0) FROM smm_Transdiscrep_odrf " +
                    "WHERE DocEntry=@de AND CompanyId=@cid", db.Conn))
                {
                    rdCmd.Parameters.AddWithValue("@de",  GloVarDocEntry);
                    rdCmd.Parameters.AddWithValue("@cid", sap_db);
                    using (var rdr = rdCmd.ExecuteReader())
                        if (rdr.Read()) { GloVarDocNum = rdr.GetInt32(0); GloVarDocNumITR = rdr.GetInt32(1); GloVarDocEntryITR = rdr.GetInt32(2); }
                }
            }
            catch { }

            //btnPrint.OnClientClick = "window.open('DisTransferDetails.aspx?DocEntry=" + DocEntry + "','PrintWindow','status=0,toolbar=0,resizable=1,scrollbars=1')";

            List<DocumentsPrint> docEntries = new List<DocumentsPrint>
            {
                new DocumentsPrint(DocEntry, 1)
            };

            Session["docEntries"] = docEntries;

            btnPrint.OnClientClick = "window.open('DisTransferDetailsPrint.aspx?DocEntry=" + DocEntry + "','PrintWindow','status=0,toolbar=0,resizable=1,scrollbars=1')";
			
            DataTable lDataTable;
            DataSet lDataSet = new DataSet();
            SqlCommand sqlCommand = new SqlCommand();

            string sql = Queries.With_SmmDraftHeader() + @"
select docstatus from SmmDraftHeader where docentry = {1}";

            sql = string.Format(sql, sap_db, DocEntry);

            sqlCommand.CommandText = sql;
            sqlCommand.CommandType = CommandType.Text;
            sqlCommand.Connection = db.Conn;

            SqlDataAdapter lSqlDataAdapter = new SqlDataAdapter
            {
                SelectCommand = sqlCommand
            };

            //lSqlDataAdapter.Fill(lDataSet, "smm_draft_header_vw");
            //lDataTable = lDataSet.Tables["smm_draft_header_vw"];

            lSqlDataAdapter.Fill(lDataSet, "SmmDraftHeader");
            lDataTable = lDataSet.Tables["SmmDraftHeader"];

            string docstatus = "";

            foreach (DataRow lDataRow in lDataTable.Rows)
            {
                docstatus = Convert.ToString(lDataRow["docstatus"]);
            }

            if (docstatus == "O")
            {
                try
                {
                    sqlCommand.Parameters.Clear();
                    sqlCommand.CommandText = "smm_populate_discrep_odrf";
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.Add(new SqlParameter("@DocEntry", SqlDbType.Int));
                    sqlCommand.Parameters["@DocEntry"].Value = Convert.ToInt32(DocEntry);
                    sqlCommand.Parameters.Add(new SqlParameter("@CompanyId", SqlDbType.NVarChar));
                    sqlCommand.Parameters["@CompanyId"].Value = sap_db;
                    sqlCommand.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Response.Write("Error when smm_populate_discrep_odrf was called.");
                    Response.Write(ex.Message);
                }
                finally
                {
                    db.Disconnect();
                }               
            }
            else
            {
                try
                {
                    
                    sqlCommand.CommandText = "select count(1) numrows from smm_Transdiscrep_odrf where CompanyId = '" + sap_db + "' and docentry = " + DocEntry;
                    sqlCommand.CommandType = CommandType.Text;
                    sqlCommand.Connection = db.Conn;

                    lSqlDataAdapter.SelectCommand = sqlCommand;

                    lSqlDataAdapter.Fill(lDataSet, "smm_Transdiscrep_odrf");
                    lDataTable = lDataSet.Tables["smm_Transdiscrep_odrf"];


                    string Lnumrows = "";

                    foreach (DataRow lDataRow in lDataTable.Rows)
                    {
                        Lnumrows = Convert.ToString(lDataRow["numrows"]);
                    }

                    int Lnmrows = Convert.ToInt32(Lnumrows);

                    if (Lnmrows == 0)
                    {
                        db.Disconnect();
                        Response.Write("Note: This Transfer was not processed through Discrepancy Control.<br>");
                        Response.End();
                    }
                    else
                    {
                        ObjectDataSource1.SelectParameters["DocEntry"].DefaultValue = DocEntry;
                        ObjectDataSource2.SelectParameters["DocEntry"].DefaultValue = DocEntry;
                    }
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

        // Sync U_GTK_CONFIRMATION and changed line quantities from SAP B1 on first load.
        if (!IsPostBack) SyncFromSapItr();

        db.Disconnect();
    }

    protected void GridView1_DataBound(object sender, EventArgs e)
    {
        iniviews(sender, e);
    }

    protected void GridView2_DataBound(object sender, EventArgs e)
    {
        bool isDutyPaid = string.Equals(GetFromWhsType(), "Duty Paid", StringComparison.OrdinalIgnoreCase);

        string lvDispatched = GridView1.Rows.Count > 0 ? GridView1.Rows[0].Cells[6].Text : "Y";
        bool isDispatch = lvDispatched == "N";
        string fromSmType = isDispatch ? GetFromWhsSmType() : "";
        // Dispatch: editable only for Duty Paid origin (Duty Free must dispatch full draft qty)
        bool isDispatchEditable = isDispatch && isDutyPaid && (
            string.Equals(fromSmType, "TIENDA", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(fromSmType, "BODEGA", StringComparison.OrdinalIgnoreCase)
        );
        // Receive: editable only for Duty Paid origin
        bool isReceiveEditable = !isDispatch && isDutyPaid;

        if (!isDispatchEditable && !isReceiveEditable) return;

        foreach (GridViewRow row in GridView2.Rows)
        {
            var tb = row.FindControl("TextBox1") as TextBox;
            if (tb != null)
            {
                tb.ReadOnly  = false;
                tb.BackColor = System.Drawing.Color.White;
            }
        }
    }

    protected void GridView1_PreRender(object sender, EventArgs e)
    {
        LvDispatched = GridView1.Rows[0].Cells[6].Text; // from header
        LvReceived = GridView1.Rows[0].Cells[8].Text; // from header
        LvUserDisp = GridView1.Rows[0].Cells[13].Text; // from header
        LabelMsg.Text = "";

        LabelCurUser.Text = "User: " + (string)this.Session["UserId"];

        string gtkVal = GetLocalGtkConfirmation();
        if (!string.IsNullOrWhiteSpace(gtkVal))
        {
            GtkConfLabel.Text    = "GTK Confirmation #: " + gtkVal;
            GtkConfLabel.Visible = true;
        }

        if (LvDispatched == "N")
        {
            GridView2.Columns[3].Visible = true;   // DraftQuantity
            GridView2.Columns[4].Visible = false;  // DispatchQuantity
            GridView2.Columns[5].Visible = false;  // ReceivedQuantity
            GridView2.Columns[6].Visible = false;  // tmpQuantity
            GridView2.Columns[7].Visible = true;   // Actual Quantity (TextBox)
            GridView2.Columns[8].Visible = false;  // userrecscanner

            if (GloVarDocEntryITR > 0)
            {
                // OWTQ already created; WMS handles the physical dispatch.
                Button1.Visible = false;
                GridView2.Columns[7].Visible = false;
                LabelMsg.Text = "Message: Transfer sent to SAP B1 (ITR #" + GloVarDocNumITR + "). Awaiting WMS dispatch confirmation.";
            }
            else if (flagPerDeskay != 'Y')
            {
                Button1.Visible = false;
                GridView2.Columns[7].Visible = false;
                LabelMsg.Text = "Message: " + "This user does not have permissions to dispatch.";
                Alert.Show(LabelMsg.Text);
            }
        }
        else
        {
            if (LvReceived == "N")
            {
                SyncDispatchQtyFromOpch(LvDispatched, LvReceived);
                GridView2.DataBind();

                GridView2.Columns[3].Visible = false;  // DraftQuantity
                GridView2.Columns[4].Visible = true;   // DispatchQuantity
                GridView2.Columns[5].Visible = false;  // ReceivedQuantity
                GridView2.Columns[6].Visible = false;  // tmpQuantity
                GridView2.Columns[7].Visible = true;   // Actual Quantity (TextBox)
                GridView2.Columns[8].Visible = true;   // userrecscanner

                if (flagPerReckay != 'Y')
                {
                    Button1.Visible = false;
                    GridView2.Columns[7].Visible = false;
                    Button2.Enabled = false;
                    btnPrint.Enabled = true;
                    LabelMsg.Text = "Message: " + "This user does not have permissions to receive.";
                    //Alert.Show(LabelMsg.Text);
                }

                //if ((string)this.Session["UserId"] == LvUserDisp)
		if (string.Equals((string)this.Session["UserId"], LvUserDisp, StringComparison.OrdinalIgnoreCase))
                {
                    Button1.Enabled = false;
                    Button2.Enabled = false;
                    btnPrint.Enabled = true;
                    LabelMsg.Text = "Message: Dispatch completed successfully. The receiving user must be different from the dispatching user.";
                    //Alert.Show(LabelMsg.Text);
                }

                string lToWhs = GridView1.Rows[0].Cells[4].Text;

                if (lToWhs == "R2 - RESEARCH STORES")
                {
                    Button1.Enabled = false;
                    Button2.Enabled = false;
                    btnPrint.Enabled = true;
                    LabelMsg.Text = "Message: Dispatch completed successfully. Receiving in R2 is not allowed.";
                }

                bool isFromBodega = string.Equals(GetFromWhsSmType(), "BODEGA", StringComparison.OrdinalIgnoreCase);
                if (GloVarDocEntryITR > 0 && isFromBodega && string.IsNullOrWhiteSpace(gtkVal))
                {
                    Button2.Enabled = false;
                    LabelMsg.Text = "Message: Awaiting GTK Confirmation to receive this transfer.";
                }
                else if (!string.IsNullOrWhiteSpace(gtkVal))
                {
                    LabelMsg.ForeColor = System.Drawing.Color.Green;
                    LabelMsg.Text = "GTK Confirmation #: " + gtkVal;
                }

                if (string.Equals(GetTransferType(), "SO", StringComparison.OrdinalIgnoreCase)
                    && GetOpchDocEntry() == 0)
                {
                    Button2.Enabled = false;
                    LabelMsg.Text = "Message: A Purchase Order (OPCH) must be linked to this transfer before receiving.";
                }
            }
            else
            {
                GridView2.Columns[3].Visible = true;   // DraftQuantity
                GridView2.Columns[4].Visible = true;   // DispatchQuantity
                GridView2.Columns[5].Visible = true;   // ReceivedQuantity
                GridView2.Columns[6].Visible = false;  // tmpQuantity
                GridView2.Columns[7].Visible = false;  // Actual Quantity (TextBox)
                GridView2.Columns[8].Visible = true;   // userrecscanner
                Button1.Enabled = false;
                LabelMsg.Text = "Message: This Order has been closed and sent to SAP BO.";
            }
        }
    }


    protected void DisRec()
    {
        string LvParameters = GridView1.Rows[0].Cells[0].Text;
        string disOrRec = null;
        string LvDispatched = GridView1.Rows[0].Cells[6].Text;
        string LvReceived = GridView1.Rows[0].Cells[8].Text;
        string LvUserDisp = GridView1.Rows[0].Cells[13].Text; // from header
        LabelMsg.Text = "";
        bool isEn = !string.Equals((string)Session["Language"], "es", StringComparison.OrdinalIgnoreCase);

        LabelCurUser.Text = "User: " + (string)this.Session["UserId"];
        sap_db = (string)Session["CompanyId"];

        string LvuserApp = (string)this.Session["UserId"];

        char LvFlag1 = 'Y';

        //logTrace("TransferDiscreOrdf-"+ GloVarDocEntry, "Tp1 LvDispatched: "+ LvDispatched);
        //logTrace("TransferDiscreOrdf-"+ GloVarDocEntry, "Tp2 LvReceived: " + LvReceived);

        string sloginTypeWhs = null;
        string sTypeWhs = null;

        ////////////////////
        ////Get type og user and warehose Bodega/Tienda
        try
        {
            db.Connect();

            db.cmd.Parameters.Clear();
            db.cmd.CommandText = "SMM_GET_LOGIN_WHS_TYPE_PRC";
            db.cmd.CommandType = CommandType.StoredProcedure;
            db.cmd.Connection = db.Conn;

            db.cmd.Parameters.Add(new SqlParameter("@LoginId", SqlDbType.NVarChar));
            db.cmd.Parameters["@LoginId"].Value = LvuserApp;

            db.cmd.Parameters.Add(new SqlParameter("@DocEntry", SqlDbType.NVarChar));
            db.cmd.Parameters["@DocEntry"].Value = GloVarDocEntry;

            db.cmd.Parameters.Add(new SqlParameter("@CompanyId", SqlDbType.NVarChar));
            db.cmd.Parameters["@CompanyId"].Value = sap_db;


            //db.cmd.ExecuteNonQuery();

            SqlDataAdapter sAdapter = new SqlDataAdapter
            {
                SelectCommand = db.cmd
            };

            DataSet dSet = new DataSet();
            sAdapter.Fill(dSet, "loginTypeWhs");
            DataTable dtable = dSet.Tables["loginTypeWhs"];

            foreach (DataRow errorRow in dtable.Rows)
            {
                sloginTypeWhs = errorRow["loginTypeWhs"].ToString();
                sTypeWhs = errorRow["TypeWhs"].ToString();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in call procedure SMM_GET_LOGIN_WHS_TYPE_PRC. ERROR MESSAGE : " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        if (string.Equals(sloginTypeWhs, "NOSETUP", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sTypeWhs,     "NOSETUP", StringComparison.OrdinalIgnoreCase))
        {
            Button1.Enabled = false;
            Button2.Enabled = false;
            btnPrint.Enabled = true;
            LvFlag1 = 'N';
            LabelMsg.Text = isEn
                ? "Message: User/Destination is not configured."
                : "Mensaje: Usuario/Destino no está configurado.";
            Alert.Show(LabelMsg.Text);
        }

        if (!string.Equals(sloginTypeWhs, "BODEGA", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(sloginTypeWhs, "TIENDA", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(sloginTypeWhs, "BODTIE", StringComparison.OrdinalIgnoreCase))
        {
            Button1.Enabled = false;
            Button2.Enabled = false;
            btnPrint.Enabled = true;
            LvFlag1 = 'N';
            LabelMsg.Text = isEn
                ? "Message: User is not configured in Warehouse or Store."
                : "Mensaje: Usuario no está configurado en Bodega o Tienda.";
            Alert.Show(LabelMsg.Text);
        }

        ////////////////////

        if (LvDispatched == "Y")
        {
            if (LvReceived == "N")
            {
                //if ((string)this.Session["UserId"] == LvUserDisp)
		if (string.Equals((string)this.Session["UserId"], LvUserDisp, StringComparison.OrdinalIgnoreCase))
                {
                    Button1.Enabled = false;
                    Button2.Enabled = false;
                    btnPrint.Enabled = true;
                    LvFlag1 = 'N';
                    LabelMsg.Text = isEn
                        ? "Message: The receiving user must be different from the dispatching user."
                        : "Mensaje: El usuario receptor debe ser diferente al usuario despachador.";
                    Alert.Show(LabelMsg.Text);
                }

                string lToWhs = GridView1.Rows[0].Cells[4].Text;

                if (lToWhs == "R2 - RESEARCH STORES")
                {
                    Button1.Enabled = false;
                    Button2.Enabled = false;
                    btnPrint.Enabled = true;
                    LvFlag1 = 'N';
                    LabelMsg.Text = isEn
                        ? "Message: Receiving in R2 is not allowed."
                        : "Mensaje: No se permite recibir en R2.";
                    Alert.Show(LabelMsg.Text);
                }

                if (!string.Equals(sloginTypeWhs, "BODTIE", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(sloginTypeWhs, sTypeWhs, StringComparison.OrdinalIgnoreCase))
                {
                    Button1.Enabled = false;
                    Button2.Enabled = false;
                    btnPrint.Enabled = true;
                    LvFlag1 = 'N';
                    LabelMsg.Text = isEn
                        ? "Message: " + sloginTypeWhs + " CANNOT RECEIVE IN " + sTypeWhs + "."
                        : "Mensaje: " + sloginTypeWhs + " no puede recibir en " + sTypeWhs + ".";
                    Alert.Show(LabelMsg.Text);
                }

                if (GloVarDocEntryITR > 0
                    && string.Equals(GetFromWhsSmType(), "BODEGA", StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrWhiteSpace(GetLocalGtkConfirmation()))
                {
                    Button1.Enabled = false;
                    Button2.Enabled = false;
                    btnPrint.Enabled = true;
                    LvFlag1 = 'N';
                    LabelMsg.Text = isEn
                        ? "Message: Awaiting GTK Confirmation to receive this transfer."
                        : "Mensaje: Esperando confirmación GTK para recibir esta transferencia.";
                    Alert.Show(LabelMsg.Text);
                }
            }
        }
        else
        {
            if (!string.Equals(sloginTypeWhs, "BODTIE", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(sloginTypeWhs, sTypeWhs, StringComparison.OrdinalIgnoreCase))
            {
                Button1.Enabled = false;
                Button2.Enabled = false;
                btnPrint.Enabled = true;
                LvFlag1 = 'N';
                LabelMsg.Text = isEn
                    ? "Message: " + sloginTypeWhs + " CANNOT DISPATCH IN " + sTypeWhs + "."
                    : "Mensaje: " + sloginTypeWhs + " no puede despachar en " + sTypeWhs + ".";
                Alert.Show(LabelMsg.Text);
            }
        }

        if (LvFlag1 == 'Y')
        {
            int TmpQty = 0;
            int LvDQty = 0;
            int LvLinNum = 0;
            TextBox LvTxB1 = new TextBox();
            int rg2count = GridView2.Rows.Count;
            string Lmsg = isEn ? "Please review quantities on lines " : "Por favor revise las cantidades en las líneas ";
            int x = 0;

            for (int i = 0; i < rg2count; i++)
            {
                LvLinNum = Convert.ToInt32(GridView2.Rows[i].Cells[0].Text);
                LvTxB1.Text = ((TextBox)(GridView2.Rows[i].Cells[7].Controls[1])).Text;

                //if(!int.TryParse(LvTxB1.Text, out x))
                //{
                //    Lmsg = Lmsg + ' ' + LvLinNum + ',';
                //    LvFlag1 = 'N';
                //}

                try
                {
                    x = int.Parse(LvTxB1.Text);
                }
                catch (Exception)
                {
                    Lmsg = Lmsg + ' ' + LvLinNum + ',';
                    LvFlag1 = 'N';
                }
            }

            if (LvFlag1 == 'N')
            {
                Lmsg = Lmsg + (isEn ? ", Please enter whole numbers only." : ", Por favor ingrese solo números enteros.");
                Alert.Show(Lmsg);
                return;
            }

            if (LvDispatched == "N")
            {
                for (int i = 0; i < rg2count; i++)
                {
                    LvLinNum = Convert.ToInt32(GridView2.Rows[i].Cells[0].Text);
                    LvTxB1.Text = ((TextBox)(GridView2.Rows[i].Cells[7].Controls[1])).Text;
                    TmpQty = Convert.ToInt32(LvTxB1.Text);

                    if (LvDispatched == "N")
                    {
                        LvTxB1.Text = GridView2.Rows[i].Cells[3].Text;
                        LvDQty = Convert.ToInt32(LvTxB1.Text);
                    }

                    if (LvDQty < TmpQty || TmpQty < 0)
                    {
                        Lmsg = Lmsg + ' ' + LvLinNum + ',';
                        LvFlag1 = 'N';
                    }
                }
            }

            if (LvFlag1 == 'N')
            {
                Lmsg = Lmsg + (isEn ? ". These must be less than or equal to the Draft quantity or zero" : ". Deben ser menores o iguales a la cantidad del borrador o cero");
                Alert.Show(Lmsg);
                return;
            }

            if (LvDispatched == "Y")
            {
                for (int i = 0; i < rg2count; i++)
                {
                    LvLinNum = Convert.ToInt32(GridView2.Rows[i].Cells[0].Text);
                    LvTxB1.Text = ((TextBox)(GridView2.Rows[i].Cells[7].Controls[1])).Text;
                    TmpQty = Convert.ToInt32(LvTxB1.Text);

                    if (TmpQty < 0)
                    {
                        Lmsg = Lmsg + ' ' + LvLinNum + ',';
                        LvFlag1 = 'N';
                    }
                }

                if (LvFlag1 == 'N')
                {
                    Lmsg = Lmsg + (isEn ? ". These must be greater than or equal to zero" : ". Deben ser mayores o iguales a cero");
                    Alert.Show(Lmsg);
                    return;
                }
            }

            if (LvFlag1 == 'N')
            {
                return;
            }

            db.Connect();

            for (int i = 0; i < rg2count; i++)
            {
                LvLinNum = Convert.ToInt32(GridView2.Rows[i].Cells[0].Text);
                LvTxB1.Text = ((TextBox)(GridView2.Rows[i].Cells[7].Controls[1])).Text;
                TmpQty = Convert.ToInt32(LvTxB1.Text);

                if (LvDispatched == "N")
                {
                    LvTxB1.Text = GridView2.Rows[i].Cells[3].Text;
                    LvDQty = Convert.ToInt32(LvTxB1.Text);
                }
                else
                {
                    if (LvReceived == "N")
                    {
                        LvTxB1.Text = GridView2.Rows[i].Cells[4].Text;
                        LvDQty = Convert.ToInt32(LvTxB1.Text);
                    }
                }

                SqlCommand sqlCommand = new SqlCommand();
                sqlCommand.Parameters.Clear();
                sqlCommand.CommandText = "update_discrep_drf1";
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Connection = db.Conn;

                
                sqlCommand.Parameters.Add(new SqlParameter("@CompanyId", SqlDbType.NVarChar));
                sqlCommand.Parameters["@CompanyId"].Value = CompanyLabel.Text;

                sqlCommand.Parameters.Add(new SqlParameter("@DocEntry", SqlDbType.Int));
                sqlCommand.Parameters["@DocEntry"].Value = GloVarDocEntry;

                sqlCommand.Parameters.Add(new SqlParameter("@Linenum", SqlDbType.SmallInt));
                sqlCommand.Parameters["@Linenum"].Value = LvLinNum;

                sqlCommand.Parameters.Add(new SqlParameter("@TmpQty", SqlDbType.SmallInt));
                sqlCommand.Parameters["@TmpQty"].Value = TmpQty;

                try
                {
                    sqlCommand.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Response.Write("Error when update_discrep_drf1 was called.");
                    Response.Write(ex.Message);
                }

                // During dispatch, persist DispatchQuantity so the shortage calculation
                // (DispatchQuantity > tmpQuantity) works at receive time even when no ITR exists.
                if (LvDispatched == "N")
                {
                    try
                    {
                        using (var dqCmd = new SqlCommand(
                            "UPDATE smm_Transdiscrep_drf1 SET DispatchQuantity = @qty " +
                            "WHERE CompanyId = @cid AND DocEntry = @de AND LineNum = @ln",
                            db.Conn))
                        {
                            dqCmd.Parameters.AddWithValue("@qty", TmpQty);
                            dqCmd.Parameters.AddWithValue("@cid", CompanyLabel.Text);
                            dqCmd.Parameters.AddWithValue("@de",  GloVarDocEntry);
                            dqCmd.Parameters.AddWithValue("@ln",  LvLinNum);
                            dqCmd.ExecuteNonQuery();
                        }
                    }
                    catch { }
                }
            }

            db.Disconnect();

            if (LvDispatched == "N")
            {
                LvParameters = LvParameters + ' ' + 'D';
                disOrRec = "D";
                LvFlag1 = 'Y';
                GloVarDesRec = 'D';

                //if (ZeroCheckBox.Checked)
                //{


                //}
                //else
                if (!ZeroCheckBox.Checked)
                {

                    db.Connect();

                    db.cmd.Parameters.Clear();
                    db.cmd.CommandText = "Smm_ValDispatching_Order_Prc";
                    db.cmd.CommandType = CommandType.StoredProcedure;
                    db.cmd.Connection = db.Conn;

                    db.cmd.Parameters.Add(new SqlParameter("@CompanyId", SqlDbType.NVarChar));
                    db.cmd.Parameters["@CompanyId"].Value = CompanyLabel.Text;

                    db.cmd.Parameters.Add(new SqlParameter("@DocEntry", SqlDbType.Int));
                    db.cmd.Parameters["@DocEntry"].Value = GloVarDocEntry;

                    SqlParameter textOut = new SqlParameter("@textOut", SqlDbType.VarChar);
                    textOut.Direction = ParameterDirection.Output;
                    textOut.Size = 250;
                    db.cmd.Parameters.Add(textOut);

                    string lTextOut = null;

                    try
                    {
                        db.cmd.ExecuteNonQuery();
                        lTextOut = db.cmd.Parameters["@textOut"].Value.ToString();

                        db.Disconnect();

                        if (lTextOut != "Orden correcta.")
                        {

                            Alert.Show(lTextOut);
                            LvFlag1 = 'N';
                            return;

                        }

                    }

                    catch (Exception ex)
                    {
                        db.Disconnect();
                        Response.Write("Error when Smm_ValDispatching_Order_Prc was called.");
                        Response.Write(ex.Message);
                    }
                }
            }
            else
            {
                if (LvReceived == "N")
                {
                    LvParameters = LvParameters + ' ' + 'R';
                    disOrRec = "R";
                    LvFlag1 = 'Y';
                    GloVarDesRec = 'R';

                    //if (ZeroCheckBox.Checked)
                    //{

                    //}
                    //else
                    if (!ZeroCheckBox.Checked)
                    {

                        db.Connect();

                        db.cmd.Parameters.Clear();
                        db.cmd.CommandText = "Smm_ValReciving_Order_Prc";
                        db.cmd.CommandType = CommandType.StoredProcedure;
                        db.cmd.Connection = db.Conn;

                        db.cmd.Parameters.Add(new SqlParameter("@CompanyId", SqlDbType.NVarChar));
                        db.cmd.Parameters["@CompanyId"].Value = CompanyLabel.Text;

                        db.cmd.Parameters.Add(new SqlParameter("@DocEntry", SqlDbType.Int));
                        db.cmd.Parameters["@DocEntry"].Value = GloVarDocEntry;

                        SqlParameter textOut = new SqlParameter("@textOut", SqlDbType.VarChar);
                        textOut.Direction = ParameterDirection.Output;
                        textOut.Size = 250;
                        db.cmd.Parameters.Add(textOut);

                        string lTextOut = null;

                        try
                        {
                            db.cmd.ExecuteNonQuery();
                            lTextOut = db.cmd.Parameters["@textOut"].Value.ToString();

                            db.Disconnect();

                            if (lTextOut != "Orden correcta.")
                            {
                                Alert.Show(lTextOut);
                                LvFlag1 = 'N';
                                return;
                            }

                        }
                        catch (Exception ex)
                        {
                            db.Disconnect();
                            Response.Write("Error when Smm_ValReciving_Order_Prc was called.");
                            Response.Write(ex.Message);
                        }
                    }

                }
                else
                {
                    LvFlag1 = 'N';
                }
            }

            //logTrace("TransferDiscreOrdf-"+ GloVarDocEntry, "Tp11 LvFlag1 < " + LvFlag1 + ">");



            /////////////////////////////////

            string url;
            string script;
            string message = null;

	//private static readonly HashSet<string> AllowedUsers =
       // new HashSet<string>(StringComparer.OrdinalIgnoreCase)
       // { "YFELICIANO", "AGALVEZ", "MMARTINEZBOD", "AABREGO", "WFERNANDEZ" ,"DGILTIE"};


            if (LvFlag1 == 'Y') // Empieza la parte batch
            {
                int sapDocNum = 0;
                int dispShortageDocNum = 0;
                string hoistedShortageErr = null;
                bool hoistedIsDutyPaid = false;
                string hoistedResearchWhs = "";
                bool receiveIsApri = false;
                string dispSapDocType = "";
                int surplusOwtrDocNum = 0;
                string surplusOwtrErr = null;

                if (disOrRec == "D")
                {
                    string sapError = CreateSapTransferRequest(LvuserApp, out sapDocNum, out dispSapDocType);
                    if (sapError != null)
                    {
                        Alert.Show(isEn
                            ? "Error creating Transfer Request in SAP B1: " + sapError
                            : "Error al crear Transfer Request en SAP B1: " + sapError);
                        return;
                    }

                    // Dispatch shortage IT (Duty Paid only) — must run BEFORE Smm_populate_whs_transfers_Batch
                    // because that SP resets tmpQuantity, making (DraftQuantity > tmpQuantity) return 0 rows.
                    // Duty Free: shortage is noted in the confirmation message only — no OWTR is created.
                    string fromWhsUType = GetFromWhsType();
                    hoistedIsDutyPaid = string.Equals(fromWhsUType, "Duty Paid", StringComparison.OrdinalIgnoreCase);
                    if (hoistedIsDutyPaid)
                    {
                        hoistedResearchWhs = GetResearchWarehouse("D", fromWhsUType);
                        if (string.IsNullOrEmpty(hoistedResearchWhs))
                            hoistedShortageErr = "Research warehouse not found in OWHS (BPLId=3, Block='D', U_Type='" + fromWhsUType + "')";
                        else
                            hoistedShortageErr = CreateSapDispatchShortageTransfer(LvuserApp, hoistedResearchWhs, out dispShortageDocNum);
                    }
                }

                if (disOrRec == "R")
                {
                    string sapError;
                    if (GetOpchDocEntry() > 0)
                    {
                        receiveIsApri = true;
                        Dictionary<string, decimal> surplusQtyByItem;
                        sapError = CreateApriOpdn(LvuserApp, out sapDocNum, out surplusQtyByItem);
                        if (sapError != null)
                        {
                            Alert.Show(isEn
                                ? "Error creating Goods Receipt PO in SAP B1: " + sapError
                                : "Error al crear Goods Receipt PO en SAP B1: " + sapError);
                            return;
                        }
                        // Duty Paid: create a second standalone OPDN for surplus quantities (received > OPCH qty)
                        if (string.Equals(GetFromWhsType(), "Duty Paid", StringComparison.OrdinalIgnoreCase)
                            && surplusQtyByItem != null && surplusQtyByItem.Count > 0)
                        {
                            int    surplusDocNum = 0;
                            string surplusError  = CreateSurplusOpdn(LvuserApp, surplusQtyByItem, out surplusDocNum);
                            if (surplusError != null)
                                Alert.Show("OPDN #1 created (DocNum " + sapDocNum + "). Error creating surplus OPDN: " + surplusError);
                        }
                    }
                    else
                    {
                        sapError = CreateSapInventoryTransfer(LvuserApp, out sapDocNum);
                        if (sapError != null)
                        {
                            LogMainItLinesToTransferErrors(LvuserApp, sapError);
                            Alert.Show(isEn
                                ? "Error creating Inventory Transfer in SAP B1: " + sapError
                                : "Error al crear Inventory Transfer en SAP B1: " + sapError);
                            return;
                        }

                        // Shortage transfer must run BEFORE Smm_populate_whs_transfers_Batch
                        // because that SP resets tmpQuantity, which would make the shortage
                        // query (DispatchQuantity > tmpQuantity) return 0 rows.
                        string fromWhsTypeDiag = GetFromWhsType();
                        hoistedIsDutyPaid = string.Equals(fromWhsTypeDiag, "Duty Paid", StringComparison.OrdinalIgnoreCase);
                        string diagInfo = string.Format("[{0}] DocEntry={1} FromWhsType='{2}' | ",
                            DateTime.Now.ToString("HH:mm:ss"), GloVarDocEntry, fromWhsTypeDiag);
                        if (hoistedIsDutyPaid)
                        {
                            try
                            {
                                db.Connect();
                                string diagSql = "SELECT d.LineNum, d.ItemCode, " +
                                    "ISNULL(CAST(d.DispatchQuantity AS NVARCHAR),'NULL') AS DQ, " +
                                    "ISNULL(CAST(d.tmpQuantity AS NVARCHAR),'NULL') AS TQ " +
                                    "FROM smm_Transdiscrep_drf1 d WHERE d.CompanyId=@cid AND d.DocEntry=@de";
                                using (var diagCmd = new SqlCommand(diagSql, db.Conn))
                                {
                                    diagCmd.Parameters.AddWithValue("@cid", sap_db);
                                    diagCmd.Parameters.AddWithValue("@de",  GloVarDocEntry);
                                    var diagDt = new DataTable();
                                    new SqlDataAdapter(diagCmd).Fill(diagDt);
                                    diagInfo += "Lines(DispatchQty/ReceivedQty):";
                                    foreach (DataRow r in diagDt.Rows)
                                        diagInfo += string.Format(" L{0}[{1}]:{2}/{3}", r["LineNum"], r["ItemCode"], r["DQ"], r["TQ"]);
                                }
                            }
                            catch (Exception exDiag) { diagInfo += " DiagQueryErr:" + exDiag.Message; }
                            finally { db.Disconnect(); }

                            hoistedResearchWhs = GetResearchWarehouse("R", fromWhsTypeDiag);
                            int shortageDocNum = 0;
                            if (string.IsNullOrEmpty(hoistedResearchWhs))
                            {
                                hoistedShortageErr = "Research warehouse not found in OWHS (BPLId=3, Block='R', U_Type='" + fromWhsTypeDiag + "')";
                                diagInfo += " | ShortageIT SKIPPED: " + hoistedShortageErr;
                            }
                            else
                            {
                                hoistedShortageErr = CreateSapDutyPaidShortageTransfer(LvuserApp, hoistedResearchWhs, out shortageDocNum);
                                if (hoistedShortageErr != null)
                                    diagInfo += " | ShortageIT ERROR: " + hoistedShortageErr;
                                else if (shortageDocNum == 0)
                                    diagInfo += " | ShortageIT: 0 shortage rows (DispatchQty<=ReceivedQty).";
                                else
                                    diagInfo += " | ShortageIT OK DocNum=" + shortageDocNum;
                            }

                            // Surplus OWTR: when received > dispatched, create a second OWTR for the extra qty.
                            surplusOwtrErr = CreateSapDutyPaidSurplusTransfer(LvuserApp, out surplusOwtrDocNum);
                            if (surplusOwtrErr != null)
                                diagInfo += " | SurplusIT ERROR: " + surplusOwtrErr;
                            else if (surplusOwtrDocNum > 0)
                                diagInfo += " | SurplusIT OK DocNum=" + surplusOwtrDocNum;
                        }
                        else
                        {
                            diagInfo += "ShortageIT SKIPPED (U_Type != 'Duty Paid').";
                        }
                        try { System.IO.File.AppendAllText(@"C:\Temp\sci_diag.txt", diagInfo + "\r\n"); } catch { }
                    }
                }

                //logTrace("TransferDiscreOrdf-" + GloVarDocEntry, " dentro del flag = Y");

                try
                {
                    db.Connect();

                    SqlCommand sqlCommand = new SqlCommand();
                    db.cmd.Parameters.Clear();
                    db.cmd.CommandText = "Smm_populate_whs_transfers_Batch";
                    db.cmd.CommandType = CommandType.StoredProcedure;

                    db.cmd.Parameters.Add(new SqlParameter("@CompanyId", SqlDbType.NVarChar));
                    db.cmd.Parameters["@CompanyId"].Value = CompanyLabel.Text;

                    db.cmd.Parameters.Add(new SqlParameter("@DocEntryDrf", SqlDbType.VarChar));
                    db.cmd.Parameters["@DocEntryDrf"].Value = GloVarDocEntry;

                    db.cmd.Parameters.Add(new SqlParameter("@TypeTran", SqlDbType.VarChar));
                    db.cmd.Parameters["@TypeTran"].Value = disOrRec;

                    db.cmd.Parameters.Add(new SqlParameter("@UserApp", SqlDbType.VarChar));
                    db.cmd.Parameters["@UserApp"].Value = LvuserApp;

                    db.cmd.Connection = db.Conn;
                    db.cmd.CommandTimeout = 0;
                    db.cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new Exception("Caught exception in procedure la_populate_whs_transfers, ERROR MESSAGE: " + ex.Message);
                }
                finally
                {
                    db.Disconnect();
                }

                if (disOrRec == "D")
                {
                    try
                    {
                        db.Connect();
                        using (var cmd = new SqlCommand(
                            "UPDATE smm_Transdiscrep_odrf SET DocEntryTraRec2 = NULL, DocNumTraRec2 = NULL " +
                            "WHERE CompanyId = @cid AND DocEntry = @de", db.Conn))
                        {
                            cmd.Parameters.AddWithValue("@cid", CompanyLabel.Text);
                            cmd.Parameters.AddWithValue("@de",  GloVarDocEntry);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { }
                    finally { db.Disconnect(); }
                }

                GridView1.DataBind();
                GridView2.DataBind();

                LvDispatched = GridView1.Rows[0].Cells[6].Text;

                //logTrace("TransferDiscreOrdf-" + GloVarDocEntry, " Antes de createUserAudit ");
                createUserAudit();

                if (disOrRec == "D")
                {
                    db.Connect();

                    db.cmd.Parameters.Clear();
                    db.cmd.CommandText = "Smm_Get_DispCompleted_Prc";
                    db.cmd.CommandType = CommandType.StoredProcedure;
                    db.cmd.Connection = db.Conn;

                    db.cmd.Parameters.Add(new SqlParameter("@CompanyId", SqlDbType.NVarChar));
                    db.cmd.Parameters["@CompanyId"].Value = CompanyLabel.Text;

                    db.cmd.Parameters.Add(new SqlParameter("@DocEntry", SqlDbType.Int));
                    db.cmd.Parameters["@DocEntry"].Value = Convert.ToInt32(GloVarDocEntry);

                    SqlParameter pDispCompleted = new SqlParameter("@DispCompleted", SqlDbType.VarChar);
                    pDispCompleted.Direction = ParameterDirection.Output;
                    pDispCompleted.Size = 100;
                    db.cmd.Parameters.Add(pDispCompleted);

                    string lDispCompleted = null;

                    try
                    {
                        db.cmd.ExecuteNonQuery();
                        lDispCompleted = db.cmd.Parameters["@DispCompleted"].Value.ToString();

                        string sapRef = "";
                        if (sapDocNum > 0)
                        {
                            string docLabel = dispSapDocType == "OWTQ"
                                ? "ITR #"
                                : (isEn ? "Sales Order #" : "Orden de Venta #");
                            sapRef = " " + docLabel + sapDocNum + " SAP.";
                        }

                        string dispShortagePart = "";
                        if (hoistedIsDutyPaid)
                        {
                            string resWhs = !string.IsNullOrEmpty(hoistedResearchWhs) ? hoistedResearchWhs : "research whs";
                            if (hoistedShortageErr != null)
                                dispShortagePart = isEn
                                    ? " WARNING: Dispatch shortage IT to " + resWhs + " failed: " + hoistedShortageErr
                                    : " ADVERTENCIA: Error al crear IT de faltante hacia " + resWhs + ": " + hoistedShortageErr;
                            else if (dispShortageDocNum > 0)
                                dispShortagePart = isEn
                                    ? " Shortage IT #" + dispShortageDocNum + " sent to " + resWhs + "."
                                    : " IT de Faltante #" + dispShortageDocNum + " enviado a " + resWhs + ".";
                        }

                        if (lDispCompleted == "N")
                        {
                            createUserAudit();
                            if (hoistedIsDutyPaid)
                            {
                                message = isEn
                                    ? "Order Dispatched. Due to discrepancies, the differences will be sent to the research warehouse." + sapRef + dispShortagePart
                                    : "Orden Despachada. Debido a discrepancias, las diferencias serán enviadas a la bodega de investigación." + sapRef + dispShortagePart;
                            }
                            else
                            {
                                message = isEn
                                    ? "Order Dispatched. Due to discrepancies, items not dispatched remain at origin." + sapRef
                                    : "Orden Despachada. Debido a discrepancias, los artículos no despachados permanecen en origen." + sapRef;
                            }
                        }
                        else
                        {
                            message = isEn
                                ? "Order Dispatched." + sapRef + dispShortagePart
                                : "Orden Despachada." + sapRef + dispShortagePart;
                        }
                        LabelMsg.Text = message;

                        db.Disconnect();

                    }
                    catch (Exception ex)
                    {
                        db.Disconnect();
                        Response.Write("Error when Smm_Get_DispCompleted_Prc was called.");
                        Response.Write(ex.Message);
                    }
                }

                if (disOrRec == "R")
                {
                    // sapDocNum may have been overwritten with the shortage doc if a
                    // Duty Paid shortage transfer was created; load the IT doc from the DB.
                    int itDocNum = 0;
                    try
                    {
                        db.Connect();
                        using (var cmd = new SqlCommand(
                            "SELECT ISNULL(DocNumTraRec2,0) FROM smm_Transdiscrep_odrf " +
                            "WHERE CompanyId=@c AND DocEntry=@e", db.Conn))
                        {
                            cmd.Parameters.AddWithValue("@c", sap_db);
                            cmd.Parameters.AddWithValue("@e", GloVarDocEntry);
                            object v = cmd.ExecuteScalar();
                            if (v != null && v != DBNull.Value) itDocNum = Convert.ToInt32(v);
                        }
                    }
                    catch { }
                    finally { db.Disconnect(); }

                    string itPart = itDocNum > 0
                        ? (receiveIsApri
                            ? (isEn ? " Goods Receipt PO #" + itDocNum + " created in SAP." : " Goods Receipt PO #" + itDocNum + " creada en SAP.")
                            : (isEn ? " Inventory Transfer #" + itDocNum + " created in SAP." : " Transferencia de Inventario #" + itDocNum + " creada en SAP."))
                        : "";

                    int shortageDocNumSaved = 0;
                    try
                    {
                        db.Connect();
                        using (var cmd = new SqlCommand(
                            "SELECT ISNULL(DocNumTraRec,0) FROM smm_Transdiscrep_odrf " +
                            "WHERE CompanyId=@c AND DocEntry=@e", db.Conn))
                        {
                            cmd.Parameters.AddWithValue("@c", sap_db);
                            cmd.Parameters.AddWithValue("@e", GloVarDocEntry);
                            object v = cmd.ExecuteScalar();
                            if (v != null && v != DBNull.Value) shortageDocNumSaved = Convert.ToInt32(v);
                        }
                    }
                    catch { }
                    finally { db.Disconnect(); }

                    int surplusOpdn2DocNum = 0;
                    try
                    {
                        db.Connect();
                        using (var cmd = new SqlCommand(
                            "SELECT ISNULL(DocNumOpdn2,0) FROM smm_Transdiscrep_odrf " +
                            "WHERE CompanyId=@c AND DocEntry=@e", db.Conn))
                        {
                            cmd.Parameters.AddWithValue("@c", sap_db);
                            cmd.Parameters.AddWithValue("@e", GloVarDocEntry);
                            object v = cmd.ExecuteScalar();
                            if (v != null && v != DBNull.Value) surplusOpdn2DocNum = Convert.ToInt32(v);
                        }
                    }
                    catch { }
                    finally { db.Disconnect(); }

                    string surplusOpdn2Part = surplusOpdn2DocNum > 0
                        ? (isEn
                            ? " Surplus Goods Receipt PO #" + surplusOpdn2DocNum + " created in SAP."
                            : " Goods Receipt PO de sobrante #" + surplusOpdn2DocNum + " creada en SAP.")
                        : "";

                    string researchWhsName = !string.IsNullOrEmpty(hoistedResearchWhs) ? hoistedResearchWhs : "research whs";
                    string shortagePart;
                    if (!hoistedIsDutyPaid)
                    {
                        shortagePart = "";
                    }
                    else if (hoistedShortageErr != null)
                    {
                        shortagePart = isEn
                            ? " WARNING: Shortage transfer to " + researchWhsName + " failed: " + hoistedShortageErr
                            : " ADVERTENCIA: Error al crear faltante hacia " + researchWhsName + ": " + hoistedShortageErr;
                    }
                    else if (shortageDocNumSaved > 0)
                    {
                        shortagePart = isEn
                            ? " Shortage Transfer #" + shortageDocNumSaved + " sent to " + researchWhsName + "."
                            : " Transferencia de Faltante #" + shortageDocNumSaved + " enviada a " + researchWhsName + ".";
                    }
                    else
                    {
                        shortagePart = isEn
                            ? " (Duty Paid: no shortage detected — received qty = dispatch qty)."
                            : " (Duty Paid: sin faltante detectado — cantidad recibida = despachada).";
                    }

                    string surplusOwtrPart = "";
                    if (surplusOwtrErr != null)
                        surplusOwtrPart = isEn
                            ? " WARNING: Surplus transfer failed (no inventory in origin): " + surplusOwtrErr
                            : " ADVERTENCIA: Error al crear transferencia de sobrante (sin inventario en origen): " + surplusOwtrErr;
                    else if (surplusOwtrDocNum > 0)
                        surplusOwtrPart = isEn
                            ? " Surplus Inventory Transfer #" + surplusOwtrDocNum + " created in SAP."
                            : " Transferencia de Sobrante #" + surplusOwtrDocNum + " creada en SAP.";

                    message = isEn
                        ? "Order Received." + itPart + surplusOpdn2Part + shortagePart + surplusOwtrPart
                        : "Orden Recibida." + itPart + surplusOpdn2Part + shortagePart + surplusOwtrPart;
                    LabelMsg.Text = message;
                }

                url = string.Format("TransferDiscreOrdf.aspx?Docentry={0}", GloVarDocEntry);
                script = "{ alert('";
                script += message;
                script += "');";
                script += "window.location = '";
                script += url;
                script += "'; }";
                ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "alert", script, true);
            }
        }
    }

    protected void Button1_Click(object sender, EventArgs e)
    {
        DisRec();
    }

    protected void Button2_Click(object sender, EventArgs e)
    {

    }
    protected void btnPrint_Click(object sender, EventArgs e)
    {

    }
    protected void Button2_Click1(object sender, EventArgs e)
    {
        DisRec();
    }

    protected void iniviews(object sender, EventArgs e)
    {
        GridView gridView = (GridView)sender;
        LvParameters = gridView.Rows[0].Cells[0].Text;
        LvDispatched = gridView.Rows[0].Cells[6].Text;
        LvReceived = gridView.Rows[0].Cells[8].Text;

        if (LvDispatched == "N")
        {
            Button1.Text = "Dispatch";
            btnPrint.Visible = false;
            Button2.Visible = false;
            Button1.Height = 38;
            Button1.Width = 65;

            if (GloVarDocEntryITR > 0)
            {
                // OWTQ was created by auto-dispatch; WMS handles the physical dispatch.
                // Hide the Dispatch button and show a waiting message.
                Button1.Visible = false;
                LabelMsg.Text = "Message: Transfer sent to SAP B1 (ITR #" + GloVarDocNumITR + "). Awaiting WMS dispatch confirmation.";
            }
        }
        else if (LvReceived == "N")
        {
            Button1.Text = "Receive";
            btnPrint.Visible = true;
            Button2.Height = 28;
            Button2.Width = 66;
            btnPrint.Height = 28;
            btnPrint.Width = 66;
            Button1.Visible = false;
            Button2.Visible = true;

           // var user = LvUserDisp;   // e.g. "YFELICIANO" HACE VISIBLE USUARIO PERMITIDOS
            //ZeroCheckBox.Visible = AllowedUsers.Contains(user);

            //if ((string)this.Session["UserId"] == LvUserDisp)
	    if (string.Equals((string)this.Session["UserId"], LvUserDisp, StringComparison.OrdinalIgnoreCase))
            {
                Button1.Enabled = false;
                Button2.Enabled = false;
                btnPrint.Enabled = true;
                LabelMsg.Text = "Message: Dispatch completed successfully. The receiving user must be different from the dispatching user.";
                //Alert.Show(LabelMsg.Text);
            }
            else
            {
                string lToWhs = GridView1.Rows[0].Cells[4].Text;

                if (lToWhs == "R2 - RESEARCH STORES")
                {
                    Button1.Enabled = false;
                    Button2.Enabled = false;
                    btnPrint.Enabled = true;
                    LabelMsg.Text = "Message: Dispatch completed successfully. Receiving in R2 is not allowed.";
                }
            }

        }
        else
        {
            Button1.Visible = false;
            Button2.Visible = false;
            btnPrint.Visible = false;
            GridView2.Columns[4].HeaderText = "Dispatched";
            GridView2.Columns[5].HeaderText = "Received";
            GridView2.Columns[3].Visible = true;
            LabelMsg.Text = "Message: This Order has been closed and sent to SAP BO.";
        }
    }

    protected void createUserAudit()
    {

        //logTrace("TransferDiscreOrdf-" + GloVarDocEntry, " Antes de createUserAudit sap_db " + sap_db);
        //logTrace("TransferDiscreOrdf-" + GloVarDocEntry, " Antes de createUserAudit DocEntry " + GloVarDocEntry);
        //logTrace("TransferDiscreOrdf-" + GloVarDocEntry, " Antes de createUserAudit GloVarDesRec " + GloVarDesRec);

        db.Connect();

        try
        {
            db.cmd.Parameters.Clear();
            db.cmd.CommandText = "smm_insert_Transdiscrep_audit_odrf";
            db.cmd.CommandType = CommandType.StoredProcedure;

            db.cmd.Parameters.Add(new SqlParameter("@CompanyId", SqlDbType.NVarChar));
            db.cmd.Parameters["@CompanyId"].Value = CompanyLabel.Text;

            db.cmd.Parameters.Add(new SqlParameter("@DocEntry", SqlDbType.Int));
            db.cmd.Parameters["@DocEntry"].Value = Convert.ToInt32(GloVarDocEntry);

            db.cmd.Parameters.Add(new SqlParameter("@TypeTrans", SqlDbType.NVarChar));
            db.cmd.Parameters["@TypeTrans"].Value = GloVarDesRec;

            db.cmd.Parameters.Add(new SqlParameter("@SourceTrans", SqlDbType.NVarChar));
            db.cmd.Parameters["@SourceTrans"].Value = "SISINV";

            db.cmd.Connection = db.Conn;
            db.cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in call procedure createUserAudit. ERROR MESSAGE : " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
    }



    private string GetLocalGtkConfirmation()
    {
        string gtk = "";
        db.Connect();
        try
        {
            using (SqlCommand cmd = new SqlCommand(
                "SELECT ISNULL(U_GTK_CONFIRMATION,'') FROM smm_Transdiscrep_odrf " +
                "WHERE CompanyId = @Company AND DocEntry = @Entry", db.Conn))
            {
                cmd.Parameters.AddWithValue("@Company", sap_db);
                cmd.Parameters.AddWithValue("@Entry",   GloVarDocEntry);
                object val = cmd.ExecuteScalar();
                if (val != null && val != DBNull.Value) gtk = val.ToString();
            }
        }
        catch { }
        finally { db.Disconnect(); }
        return gtk;
    }

    private string GetFromWhsType()
    {
        string whsType = "";
        db.Connect();
        try
        {
            string sql = string.Format(
                "SELECT ISNULL(w.U_Type,'') " +
                "FROM smm_Transdiscrep_odrf h WITH(NOLOCK) " +
                "JOIN [{0}]..OWHS w WITH(NOLOCK) ON w.WhsCode = h.FromWhsCode " +
                "WHERE h.CompanyId = @Company AND h.DocEntry = @Entry", sap_db);
            using (SqlCommand cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@Company", sap_db);
                cmd.Parameters.AddWithValue("@Entry",   GloVarDocEntry);
                object val = cmd.ExecuteScalar();
                if (val != null && val != DBNull.Value) whsType = val.ToString();
            }
        }
        catch { }
        finally { db.Disconnect(); }
        return whsType;
    }

    // Returns SMM_WHSTYPE.TYPEWHS ('BODEGA'/'TIENDA') for the FROM warehouse of this document.
    private string GetFromWhsSmType()
    {
        string whsType = "";
        db.Connect();
        try
        {
            using (SqlCommand cmd = new SqlCommand(
                "SELECT ISNULL(wt.TypeWhs,'') " +
                "FROM smm_Transdiscrep_odrf h WITH(NOLOCK) " +
                "LEFT JOIN SMM_WHSTYPE wt WITH(NOLOCK) ON wt.WhsCode = h.FromWhsCode " +
                "WHERE h.CompanyId = @Company AND h.DocEntry = @Entry", db.Conn))
            {
                cmd.Parameters.AddWithValue("@Company", sap_db);
                cmd.Parameters.AddWithValue("@Entry",   GloVarDocEntry);
                object val = cmd.ExecuteScalar();
                if (val != null && val != DBNull.Value) whsType = val.ToString();
            }
        }
        catch { }
        finally { db.Disconnect(); }
        return whsType;
    }

    // Returns the research warehouse code from OWHS (BPLId=3) for the given block type:
    // 'D' for dispatch operations, 'R' for receive operations.
    // uType: when provided, filters by matching U_Type so SAP item-type validation passes.
    private string GetResearchWarehouse(string blockType, string uType = null)
    {
        string whs = "";
        db.Connect();
        try
        {
            string typeClause = string.IsNullOrEmpty(uType)
                ? ""
                : " AND ISNULL(U_Type,'') = @utype";

            using (var cmd = new SqlCommand(
                "SELECT TOP 1 WhsCode FROM " + sap_db + "..OWHS WITH(NOLOCK) " +
                "WHERE BPLId=3 AND Block=@b" + typeClause,
                db.Conn))
            {
                cmd.Parameters.AddWithValue("@b", blockType);
                if (!string.IsNullOrEmpty(uType))
                    cmd.Parameters.AddWithValue("@utype", uType);
                object val = cmd.ExecuteScalar();
                if (val != null && val != DBNull.Value) whs = val.ToString();
            }
        }
        catch { }
        finally { db.Disconnect(); }
        return whs;
    }

    // Reads U_GTK_CONFIRMATION and line quantities directly from SAP B1.
    // Two modes:
    //   pendingWmsDispatch (Dispatched='N', DocEntryITR>0): OWTQ was created by auto-dispatch;
    //     reads GTK from OWTQ and if confirmed, completes local dispatch in SCI.
    //   activeReceive (Dispatched='Y', Received='N'): syncs line quantities from SAP B1.
    private void SyncFromSapItr()
    {
        db.Connect();
        try
        {
            string dispatched = null, received = null, transferType = null;
            int sapItrEntry = 0;

            using (SqlCommand cmd = new SqlCommand(
                "SELECT Dispatched, Received, ISNULL(DocEntryITR,0) DocEntryITR, ISNULL(TransferType,'') TransferType " +
                "FROM smm_Transdiscrep_odrf WHERE CompanyId = @Company AND DocEntry = @Entry",
                db.Conn))
            {
                cmd.Parameters.AddWithValue("@Company", sap_db);
                cmd.Parameters.AddWithValue("@Entry",   GloVarDocEntry);
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        dispatched   = rdr["Dispatched"].ToString();
                        received     = rdr["Received"].ToString();
                        sapItrEntry  = rdr["DocEntryITR"] != DBNull.Value ? Convert.ToInt32(rdr["DocEntryITR"]) : 0;
                        transferType = rdr["TransferType"].ToString();
                    }
                }
            }

            bool pendingWmsDispatch = dispatched == "N" && sapItrEntry > 0;
            bool activeReceive      = dispatched == "Y" && received == "N";
            if (!pendingWmsDispatch && !activeReceive) return;

            if (pendingWmsDispatch)
            {
                // OWTQ was created by auto-dispatch; WMS handles the physical dispatch.
                // Read GTK from the OWTQ. If confirmed, complete local dispatch in SCI.
                string gtk = null;
                using (var cmd = new SqlCommand(
                    "SELECT U_GTK_CONFIRMATION FROM " + sap_db +
                    "..OWTQ WITH(NOLOCK) WHERE DocEntry = @SapEntry", db.Conn))
                {
                    cmd.Parameters.AddWithValue("@SapEntry", sapItrEntry);
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) gtk = val.ToString();
                }

                using (var cmd = new SqlCommand(
                    "UPDATE smm_Transdiscrep_odrf SET U_GTK_CONFIRMATION = @Gtk " +
                    "WHERE CompanyId = @Company AND DocEntry = @Entry", db.Conn))
                {
                    cmd.Parameters.AddWithValue("@Gtk",     string.IsNullOrEmpty(gtk) ? (object)DBNull.Value : gtk);
                    cmd.Parameters.AddWithValue("@Company", sap_db);
                    cmd.Parameters.AddWithValue("@Entry",   GloVarDocEntry);
                    cmd.ExecuteNonQuery();
                }

                if (string.IsNullOrWhiteSpace(gtk)) return; // GTK not yet confirmed — nothing more to do

                // GTK confirmed: complete local dispatch so TransferDiscreOrdf shows the Receive button.
                string userApp = ((string)Session["UserId"]) ?? "";
                using (var cmd = new SqlCommand("Smm_populate_whs_transfers_Batch", db.Conn))
                {
                    cmd.CommandType    = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 0;
                    cmd.Parameters.AddWithValue("@CompanyId",   sap_db);
                    cmd.Parameters.AddWithValue("@DocEntryDrf", GloVarDocEntry);
                    cmd.Parameters.AddWithValue("@TypeTran",    "D");
                    cmd.Parameters.AddWithValue("@UserApp",     userApp);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand(
                    "UPDATE smm_Transdiscrep_odrf SET DocEntryTraRec2 = NULL, DocNumTraRec2 = NULL " +
                    "WHERE CompanyId = @cid AND DocEntry = @de", db.Conn))
                {
                    cmd.Parameters.AddWithValue("@cid", sap_db);
                    cmd.Parameters.AddWithValue("@de",  GloVarDocEntry);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand("smm_insert_Transdiscrep_audit_odrf", db.Conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CompanyId",   sap_db);
                    cmd.Parameters.AddWithValue("@DocEntry",    GloVarDocEntry);
                    cmd.Parameters.AddWithValue("@TypeTrans",   "D");
                    cmd.Parameters.AddWithValue("@SourceTrans", "SISINV");
                    cmd.ExecuteNonQuery();
                }

                return;
            }

            // activeReceive: sync GTK and line quantities from SAP B1.
            // Route by TransferType: 'SO' = old Branch-4 ORDR path (backward compat); otherwise OWTQ.
            bool isOrdrPath = string.Equals(transferType, "SO", StringComparison.OrdinalIgnoreCase);

            if (isOrdrPath)
            {
                // Old Branch 4 (ORDR/Sales Order) — kept for backward compatibility.
                if (sapItrEntry <= 0)
                {
                    string sqlFind = "SELECT TOP 1 DocEntry FROM " + sap_db +
                                     "..ORDR WITH(NOLOCK) WHERE CAST(U_BOL AS NVARCHAR) = @Bol" +
                                     " ORDER BY DocEntry DESC";
                    using (var cmd = new SqlCommand(sqlFind, db.Conn))
                    {
                        cmd.Parameters.AddWithValue("@Bol", GloVarDocEntry.ToString());
                        object val = cmd.ExecuteScalar();
                        if (val != null && val != DBNull.Value)
                            sapItrEntry = Convert.ToInt32(val);
                    }
                }

                if (sapItrEntry <= 0) return;

                string gtk = null; int docNum = 0;

                using (var cmd = new SqlCommand(
                    "SELECT DocNum FROM " + sap_db +
                    "..ORDR WITH(NOLOCK) WHERE DocEntry = @SapEntry", db.Conn))
                {
                    cmd.Parameters.AddWithValue("@SapEntry", sapItrEntry);
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) docNum = Convert.ToInt32(val);
                }

                // GTK on ORDR path is on the ODLN (Delivery) linked via DLN1.BaseEntry.
                using (var cmd = new SqlCommand(
                    "SELECT TOP 1 d.U_GTK_CONFIRMATION " +
                    "FROM " + sap_db + "..ODLN d WITH(NOLOCK) " +
                    "INNER JOIN " + sap_db + "..DLN1 l WITH(NOLOCK) ON l.DocEntry = d.DocEntry " +
                    "WHERE l.BaseType = 17 AND l.BaseEntry = @SapEntry " +
                    "ORDER BY d.DocEntry DESC", db.Conn))
                {
                    cmd.Parameters.AddWithValue("@SapEntry", sapItrEntry);
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) gtk = val.ToString();
                }

                using (var cmd = new SqlCommand(
                    "UPDATE smm_Transdiscrep_odrf " +
                    "SET U_GTK_CONFIRMATION = @Gtk, DocNumITR = @DocNum, " +
                    "    DocEntryITR = CASE WHEN ISNULL(DocEntryITR,0)=0 THEN @SapEntry ELSE DocEntryITR END " +
                    "WHERE CompanyId = @Company AND DocEntry = @Entry", db.Conn))
                {
                    cmd.Parameters.AddWithValue("@Gtk",      string.IsNullOrEmpty(gtk) ? (object)DBNull.Value : gtk);
                    cmd.Parameters.AddWithValue("@DocNum",   docNum);
                    cmd.Parameters.AddWithValue("@SapEntry", sapItrEntry);
                    cmd.Parameters.AddWithValue("@Company",  sap_db);
                    cmd.Parameters.AddWithValue("@Entry",    GloVarDocEntry);
                    cmd.ExecuteNonQuery();
                }

                string sqlSync = @"
                    UPDATE d
                    SET    d.DispatchQuantity = CAST(r1.Quantity AS INT),
                           d.tmpQuantity      = CAST(r1.Quantity AS INT)
                    FROM   smm_Transdiscrep_drf1 d WITH(NOLOCK)
                    JOIN   " + sap_db + @"..RDR1 r1 WITH(NOLOCK)
                           ON  r1.DocEntry = @SapEntry
                           AND r1.ItemCode = d.ItemCode
                    WHERE  d.CompanyId = @Company AND d.DocEntry = @Entry";

                using (var cmd = new SqlCommand(sqlSync, db.Conn))
                {
                    cmd.Parameters.AddWithValue("@SapEntry", sapItrEntry);
                    cmd.Parameters.AddWithValue("@Company",  sap_db);
                    cmd.Parameters.AddWithValue("@Entry",    GloVarDocEntry);
                    cmd.ExecuteNonQuery();
                }

                string sqlZero = @"
                    UPDATE d SET d.DispatchQuantity = 0, d.tmpQuantity = 0
                    FROM   smm_Transdiscrep_drf1 d
                    LEFT JOIN " + sap_db + @"..RDR1 r1 WITH(NOLOCK)
                           ON  r1.DocEntry = @SapEntry AND r1.ItemCode = d.ItemCode
                    WHERE  d.CompanyId = @Company AND d.DocEntry = @Entry
                      AND  r1.ItemCode IS NULL";

                using (var cmd = new SqlCommand(sqlZero, db.Conn))
                {
                    cmd.Parameters.AddWithValue("@SapEntry", sapItrEntry);
                    cmd.Parameters.AddWithValue("@Company",  sap_db);
                    cmd.Parameters.AddWithValue("@Entry",    GloVarDocEntry);
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                // OWTQ path — Branch 3 and new Branch 4.
                if (sapItrEntry <= 0)
                {
                    string sqlFind = "SELECT TOP 1 DocEntry FROM " + sap_db +
                                     "..OWTQ WITH(NOLOCK) WHERE CAST(U_BOL AS NVARCHAR) = @Bol" +
                                     " ORDER BY DocEntry DESC";
                    using (SqlCommand cmd = new SqlCommand(sqlFind, db.Conn))
                    {
                        cmd.Parameters.AddWithValue("@Bol", GloVarDocEntry.ToString());
                        object val = cmd.ExecuteScalar();
                        if (val != null && val != DBNull.Value)
                            sapItrEntry = Convert.ToInt32(val);
                    }
                }

                if (sapItrEntry <= 0) return;

                string gtk    = null;
                int    docNum = 0;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT U_GTK_CONFIRMATION, DocNum FROM " + sap_db +
                    "..OWTQ WITH(NOLOCK) WHERE DocEntry = @SapEntry", db.Conn))
                {
                    cmd.Parameters.AddWithValue("@SapEntry", sapItrEntry);
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            if (rdr["U_GTK_CONFIRMATION"] != DBNull.Value) gtk    = rdr["U_GTK_CONFIRMATION"].ToString();
                            if (rdr["DocNum"]              != DBNull.Value) docNum = Convert.ToInt32(rdr["DocNum"]);
                        }
                    }
                }

                using (SqlCommand cmd = new SqlCommand(
                    "UPDATE smm_Transdiscrep_odrf " +
                    "SET U_GTK_CONFIRMATION = @Gtk, " +
                    "    DocNumITR          = @DocNum, " +
                    "    DocEntryITR        = CASE WHEN ISNULL(DocEntryITR,0)=0 THEN @SapEntry ELSE DocEntryITR END " +
                    "WHERE CompanyId = @Company AND DocEntry = @Entry", db.Conn))
                {
                    cmd.Parameters.AddWithValue("@Gtk",      string.IsNullOrEmpty(gtk) ? (object)DBNull.Value : gtk);
                    cmd.Parameters.AddWithValue("@DocNum",   docNum);
                    cmd.Parameters.AddWithValue("@SapEntry", sapItrEntry);
                    cmd.Parameters.AddWithValue("@Company",  sap_db);
                    cmd.Parameters.AddWithValue("@Entry",    GloVarDocEntry);
                    cmd.ExecuteNonQuery();
                }

                string sqlSync = @"
                    UPDATE d
                    SET    d.DispatchQuantity = CAST(q1.Quantity AS INT),
                           d.tmpQuantity      = CAST(q1.Quantity AS INT)
                    FROM   smm_Transdiscrep_drf1 d WITH(NOLOCK)
                    JOIN   " + sap_db + @"..WTQ1 q1 WITH(NOLOCK)
                           ON  q1.DocEntry  = @SapEntry
                           AND q1.ItemCode  = d.ItemCode
                           AND q1.WhsCode   = d.ToWhsCode
                    WHERE  d.CompanyId = @Company
                    AND    d.DocEntry  = @Entry";

                using (SqlCommand cmd = new SqlCommand(sqlSync, db.Conn))
                {
                    cmd.Parameters.AddWithValue("@SapEntry", sapItrEntry);
                    cmd.Parameters.AddWithValue("@Company",  sap_db);
                    cmd.Parameters.AddWithValue("@Entry",    GloVarDocEntry);
                    cmd.ExecuteNonQuery();
                }

                string sqlZero = @"
                    UPDATE d
                    SET    d.DispatchQuantity = 0,
                           d.tmpQuantity      = 0
                    FROM   smm_Transdiscrep_drf1 d
                    LEFT JOIN " + sap_db + @"..WTQ1 q1 WITH(NOLOCK)
                           ON  q1.DocEntry = @SapEntry
                           AND q1.ItemCode = d.ItemCode
                           AND q1.WhsCode  = d.ToWhsCode
                    WHERE  d.CompanyId = @Company
                    AND    d.DocEntry  = @Entry
                    AND    q1.ItemCode IS NULL";

                using (SqlCommand cmd = new SqlCommand(sqlZero, db.Conn))
                {
                    cmd.Parameters.AddWithValue("@SapEntry", sapItrEntry);
                    cmd.Parameters.AddWithValue("@Company",  sap_db);
                    cmd.Parameters.AddWithValue("@Entry",    GloVarDocEntry);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch { }
        finally { db.Disconnect(); }
    }

    // Returns null on success, or an error message string on failure.
    // BODEGA→TIENDA (inter-branch): creates ORDR (Sales Order) with cost from OITW.AvgPrice.
    // Other paths: creates OWTQ (Inventory Transfer Request) as before.
    private string CreateSapTransferRequest(string despatchUser, out int sapDocNum, out string sapDocType)
    {
        sapDocNum  = 0;
        sapDocType = "";
        string fromWhs = "", toWhs = "", uStore = "", cardCode = "", fromWhsUType = "";
        bool   isBodegaToTienda = false;
        var    ordrLines = new JArray();
        var    owtqLines = new JArray();

        try
        {
            db.Connect();

            string sql = string.Format(@"
                SELECT h.FromWhsCode, h.ToWhsCode, ISNULL(w.U_Store, '') AS U_Store,
                       ISNULL(w.U_Type, '') AS WhsUType,
                       d.LineNum, d.ItemCode, d.ToWhsCode AS LineToWhs,
                       CAST(d.tmpQuantity AS int) AS Qty,
                       ISNULL(iw.AvgPrice, 0) AS UnitPrice
                FROM smm_Transdiscrep_odrf h WITH(NOLOCK)
                INNER JOIN smm_Transdiscrep_drf1 d WITH(NOLOCK)
                    ON h.DocEntry = d.DocEntry AND h.CompanyId = d.CompanyId
                LEFT JOIN [{0}]..OWHS w WITH(NOLOCK)
                    ON w.WhsCode = h.FromWhsCode
                LEFT JOIN [{0}]..OITW iw WITH(NOLOCK)
                    ON iw.ItemCode = d.ItemCode AND iw.WhsCode = h.FromWhsCode
                WHERE h.CompanyId = '{1}' AND h.DocEntry = {2}
                  AND d.tmpQuantity > 0
                ORDER BY d.LineNum", sap_db, sap_db, GloVarDocEntry);

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            DataTable dt = new DataTable();
            db.adapter.Fill(dt);

            if (dt.Rows.Count == 0) return null;

            fromWhs      = dt.Rows[0]["FromWhsCode"].ToString();
            toWhs        = dt.Rows[0]["ToWhsCode"].ToString();
            uStore       = dt.Rows[0]["U_Store"].ToString();
            fromWhsUType = dt.Rows[0]["WhsUType"].ToString();

            isBodegaToTienda = CheckIsBodegaToTienda(db.Conn);
            sapDocType = isBodegaToTienda ? "ORDR" : "OWTQ";

            foreach (DataRow row in dt.Rows)
            {
                ordrLines.Add(new JObject(
                    new JProperty("ItemCode",      row["ItemCode"].ToString()),
                    new JProperty("Quantity",      Convert.ToInt32(row["Qty"])),
                    new JProperty("UnitPrice",     Convert.ToDecimal(row["UnitPrice"])),
                    new JProperty("WarehouseCode", fromWhs)
                ));
                owtqLines.Add(new JObject(
                    new JProperty("ItemCode",          row["ItemCode"].ToString()),
                    new JProperty("Quantity",          Convert.ToInt32(row["Qty"])),
                    new JProperty("FromWarehouseCode", fromWhs),
                    new JProperty("WarehouseCode",     row["LineToWhs"].ToString())
                ));
            }

            if (isBodegaToTienda)
                cardCode = GetOrderCardCode(db.Conn, toWhs);
        }
        catch (Exception ex)
        {
            return "Error reading data: " + ex.Message;
        }
        finally
        {
            db.Disconnect();
        }

        if (isBodegaToTienda ? ordrLines.Count == 0 : owtqLines.Count == 0) return null;

        string companyDb = ConfigurationManager.AppSettings["SL_CompanyDB"] ?? sap_db;

        JObject payload;
        if (isBodegaToTienda)
        {
            if (string.IsNullOrEmpty(cardCode))
                return "No CardCode mapping found in ApriCardCodeMapping for destination warehouse " + toWhs;

            string today = DateTime.Today.ToString("yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture);

            payload = new JObject(
                new JProperty("CardCode",                  cardCode),
                new JProperty("BPL_IDAssignedToInvoice",   1),
                new JProperty("DocDate",                   today),
                new JProperty("DocDueDate",                today),
                new JProperty("TaxDate",                   today),
                new JProperty("U_BOL",                     GloVarDocNum.ToString()),
                new JProperty("U_DESPATCH",                despatchUser),
                new JProperty("U_ORITOWHS",                toWhs),
                new JProperty("U_Type",                    "Duty Paid"),
                new JProperty("DocumentLines",             ordrLines)
            );
        }
        else
        {
            string actionCode = string.Equals(uStore, "WHSE", StringComparison.OrdinalIgnoreCase)
                                ? "CREATE" : "NO_ENVIAR";
            payload = new JObject(
                new JProperty("FromWarehouse",      fromWhs),
                new JProperty("ToWarehouse",        toWhs),
                new JProperty("U_BOL",              GloVarDocNum.ToString()),
                new JProperty("U_DESPATCH",         despatchUser),
                new JProperty("U_ORITOWHS",         fromWhs),
                new JProperty("U_ACTION_CODE",      actionCode),
                new JProperty("U_Type",             "Duty Paid"),
                new JProperty("StockTransferLines", owtqLines)
            );
        }

        var sl = new SapServiceLayer();
        try
        {
            sl.Login(companyDb);
            string response = isBodegaToTienda
                ? sl.CreateSalesOrder(payload.ToString(Newtonsoft.Json.Formatting.None))
                : sl.CreateInventoryTransferRequest(payload.ToString(Newtonsoft.Json.Formatting.None));

            try
            {
                var respObj  = JObject.Parse(response);
                int sapEntry = respObj["DocEntry"] != null ? Convert.ToInt32(respObj["DocEntry"]) : 0;
                sapDocNum    = respObj["DocNum"]   != null ? Convert.ToInt32(respObj["DocNum"])   : 0;

                if (sapEntry > 0)
                {
                    db.Connect();
                    string setType = isBodegaToTienda ? ", TransferType = 'SO'" : "";
                    SqlCommand upd = new SqlCommand(
                        "UPDATE smm_Transdiscrep_odrf SET DocEntryITR = @Entry, DocNumITR = @Num" + setType + " " +
                        "WHERE CompanyId = @Company AND DocEntry = @LocalEntry",
                        db.Conn);
                    upd.Parameters.AddWithValue("@Entry",      sapEntry);
                    upd.Parameters.AddWithValue("@Num",        sapDocNum);
                    upd.Parameters.AddWithValue("@Company",    sap_db);
                    upd.Parameters.AddWithValue("@LocalEntry", GloVarDocEntry);
                    upd.ExecuteNonQuery();
                }
            }
            catch { }
            finally
            {
                db.Disconnect();
            }

            return null;
        }
        catch (System.Net.WebException wex)
        {
            return SapServiceLayer.GetSlErrorMessage(wex);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            sl.Logout();
        }
    }

    // Returns null on success, or an error message string on failure.
    // Reads tmpQuantity (set by update_discrep_drf1 for the receive step).
    private string CreateSapInventoryTransfer(string receiveUser, out int sapDocNum)
    {
        sapDocNum            = 0;
        string fromWhs       = "";
        string toWhs         = "";
        int    sapTrReqEntry = 0;
        int    mainItrDocNum = 0;
        var    lines         = new JArray();

        try
        {
            db.Connect();

            string sql = @"
                SELECT h.FromWhsCode, h.ToWhsCode, h.DocEntryITR, h.DocNumITR,
                       d.LineNum, d.ItemCode, d.ToWhsCode AS LineToWhs,
                       CAST(d.tmpQuantity AS int)                    AS TmpQty,
                       ISNULL(CAST(d.DispatchQuantity AS int), 0)    AS DispatchQty,
                       ISNULL(q1.LineNum, -1) AS SapLineNum
                FROM smm_Transdiscrep_odrf h WITH(NOLOCK)
                INNER JOIN smm_Transdiscrep_drf1 d WITH(NOLOCK)
                    ON h.DocEntry = d.DocEntry AND h.CompanyId = d.CompanyId
                LEFT JOIN " + sap_db + @"..WTQ1 q1 WITH(NOLOCK)
                    ON  q1.DocEntry = h.DocEntryITR
                    AND q1.ItemCode = d.ItemCode
                    AND q1.WhsCode  = d.ToWhsCode
                WHERE h.CompanyId = '" + sap_db + @"' AND h.DocEntry = " + GloVarDocEntry + @"
                  AND d.tmpQuantity > 0
                ORDER BY d.LineNum";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            DataTable dt = new DataTable();
            db.adapter.Fill(dt);

            if (dt.Rows.Count == 0) return null;

            fromWhs       = dt.Rows[0]["FromWhsCode"].ToString();
            toWhs         = dt.Rows[0]["ToWhsCode"].ToString();
            sapTrReqEntry = dt.Rows[0]["DocEntryITR"] != DBNull.Value
                            ? Convert.ToInt32(dt.Rows[0]["DocEntryITR"]) : 0;
            mainItrDocNum = dt.Rows[0]["DocNumITR"] != DBNull.Value
                         ? Convert.ToInt32(dt.Rows[0]["DocNumITR"]) : 0;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow row       = dt.Rows[i];
                int     tmpQty    = Convert.ToInt32(row["TmpQty"]);
                int     dispQty   = Convert.ToInt32(row["DispatchQty"]);
                // Cap at dispatched qty; when DispatchQuantity not recorded (old transfers), use full tmpQty.
                int     normalQty = dispQty > 0 ? Math.Min(tmpQty, dispQty) : tmpQty;
                if (normalQty <= 0) continue;

                var line = new JObject(
                    new JProperty("ItemCode",          row["ItemCode"].ToString()),
                    new JProperty("Quantity",          normalQty),
                    new JProperty("FromWarehouseCode", fromWhs),
                    new JProperty("WarehouseCode",     row["LineToWhs"].ToString())
                );
                if (sapTrReqEntry > 0)
                {
                    int sapLineNum = dt.Rows[i]["SapLineNum"] != DBNull.Value
                                     ? Convert.ToInt32(dt.Rows[i]["SapLineNum"]) : i;
                    line.Add("BaseType",  1250000001);
                    line.Add("BaseEntry", sapTrReqEntry);
                    line.Add("BaseLine",  sapLineNum >= 0 ? sapLineNum : i);
                }
                lines.Add(line);
            }
        }
        catch (Exception ex)
        {
            return "Error reading data: " + ex.Message;
        }
        finally
        {
            db.Disconnect();
        }

        if (lines.Count == 0) return null;

        string companyDb = ConfigurationManager.AppSettings["SL_CompanyDB"] ?? sap_db;

        string mainComments = mainItrDocNum > 0
            ? "Received - ITR #" + mainItrDocNum
            : "Received";

        var payload = new JObject(
            new JProperty("FromWarehouse",      fromWhs),
            new JProperty("ToWarehouse",        toWhs),
            new JProperty("U_BOL",              GloVarDocNum.ToString()),
            new JProperty("U_RECEIVE",          receiveUser),
            new JProperty("U_ORITOWHS",         fromWhs),
            new JProperty("U_Type",             "Duty Paid"),
            new JProperty("Comments",           mainComments),
            new JProperty("StockTransferLines", lines)
        );

        var sl = new SapServiceLayer();
        try
        {
            sl.Login(companyDb);
            string response = sl.CreateInventoryTransfer(payload.ToString(Newtonsoft.Json.Formatting.None));

            try
            {
                var respObj  = JObject.Parse(response);
                int sapEntry = respObj["DocEntry"] != null ? Convert.ToInt32(respObj["DocEntry"]) : 0;
                sapDocNum    = respObj["DocNum"]   != null ? Convert.ToInt32(respObj["DocNum"])   : 0;

                if (sapEntry > 0)
                {
                    db.Connect();
                    SqlCommand upd = new SqlCommand(
                        "UPDATE smm_Transdiscrep_odrf SET DocEntryTraRec2 = @Entry, DocNumTraRec2 = @Num " +
                        "WHERE CompanyId = @Company AND DocEntry = @LocalEntry",
                        db.Conn);
                    upd.Parameters.AddWithValue("@Entry",      sapEntry);
                    upd.Parameters.AddWithValue("@Num",        sapDocNum);
                    upd.Parameters.AddWithValue("@Company",    sap_db);
                    upd.Parameters.AddWithValue("@LocalEntry", GloVarDocEntry);
                    upd.ExecuteNonQuery();
                }
            }
            catch { }
            finally
            {
                db.Disconnect();
            }

            return null;
        }
        catch (System.Net.WebException wex)
        {
            return SapServiceLayer.GetSlErrorMessage(wex);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            sl.Logout();
        }
    }

    // Creates a SAP Inventory Transfer (OWTR) from the Duty Paid origin warehouse to the
    // research warehouse (queried from OWHS BPLId=3 Block='D') for each line where
    // DraftQuantity > tmpQuantity (shortage on dispatch: items not sent).
    // Saves DocEntry/DocNum to DocEntryTraDis / DocNumTraDis.
    // On SAP failure, logs each line to la_transfer_errors.
    // Returns null on success (including when there is no shortage), or error message.
    private string CreateSapDispatchShortageTransfer(string dispatchUser, string researchWhs, out int sapDocNum)
    {
        sapDocNum = 0;
        string fromWhs  = "";
        string toOriWhs = "";
        var    lines        = new JArray();
        var    shortageRows = new DataTable();

        try
        {
            db.Connect();

            string sql = @"
                SELECT h.FromWhsCode, h.ToWhsCode,
                       d.LineNum, d.ItemCode, d.ItemName,
                       CAST(d.DraftQuantity  AS int) AS DraftQty,
                       CAST(d.tmpQuantity    AS int) AS DispatchQty,
                       CAST(d.DraftQuantity - d.tmpQuantity AS int) AS ShortageQty
                FROM smm_Transdiscrep_odrf h WITH(NOLOCK)
                INNER JOIN smm_Transdiscrep_drf1 d WITH(NOLOCK)
                    ON h.DocEntry = d.DocEntry AND h.CompanyId = d.CompanyId
                WHERE h.CompanyId = @cid AND h.DocEntry = @de
                  AND d.DraftQuantity > d.tmpQuantity
                ORDER BY d.LineNum";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.SelectCommand.Parameters.AddWithValue("@cid", sap_db);
            db.adapter.SelectCommand.Parameters.AddWithValue("@de",  GloVarDocEntry);
            db.adapter.Fill(shortageRows);

            if (shortageRows.Rows.Count == 0) return null;

            fromWhs  = shortageRows.Rows[0]["FromWhsCode"].ToString();
            toOriWhs = shortageRows.Rows[0]["ToWhsCode"].ToString();

            foreach (DataRow row in shortageRows.Rows)
            {
                lines.Add(new JObject(
                    new JProperty("ItemCode",          row["ItemCode"].ToString()),
                    new JProperty("Quantity",          Convert.ToInt32(row["ShortageQty"])),
                    new JProperty("FromWarehouseCode", fromWhs),
                    new JProperty("WarehouseCode",     researchWhs)
                ));
            }
        }
        catch (Exception ex)
        {
            return "Error reading dispatch shortage data: " + ex.Message;
        }
        finally
        {
            db.Disconnect();
        }

        if (lines.Count == 0) return null;

        string companyDb = ConfigurationManager.AppSettings["SL_CompanyDB"] ?? sap_db;

        var payload = new JObject(
            new JProperty("FromWarehouse",      fromWhs),
            new JProperty("ToWarehouse",        researchWhs),
            new JProperty("U_BOL",              GloVarDocNum.ToString()),
            new JProperty("U_DESPATCH",         dispatchUser),
            new JProperty("U_ORITOWHS",         fromWhs),
            new JProperty("U_Type",             "Duty Paid"),
            new JProperty("StockTransferLines", lines)
        );

        var sl = new SapServiceLayer();
        try
        {
            sl.Login(companyDb);
            string response = sl.CreateInventoryTransfer(payload.ToString(Newtonsoft.Json.Formatting.None));

            try
            {
                var respObj  = JObject.Parse(response);
                int sapEntry = respObj["DocEntry"] != null ? Convert.ToInt32(respObj["DocEntry"]) : 0;
                sapDocNum    = respObj["DocNum"]   != null ? Convert.ToInt32(respObj["DocNum"])   : 0;

                if (sapEntry > 0)
                {
                    db.Connect();
                    using (var upd = new SqlCommand(
                        "UPDATE smm_Transdiscrep_odrf SET DocEntryTraDis = @Entry, DocNumTraDis = @Num " +
                        "WHERE CompanyId = @Company AND DocEntry = @LocalEntry", db.Conn))
                    {
                        upd.Parameters.AddWithValue("@Entry",      sapEntry);
                        upd.Parameters.AddWithValue("@Num",        sapDocNum);
                        upd.Parameters.AddWithValue("@Company",    sap_db);
                        upd.Parameters.AddWithValue("@LocalEntry", GloVarDocEntry);
                        upd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
            finally { db.Disconnect(); }

            return null;
        }
        catch (System.Net.WebException wex)
        {
            string err = SapServiceLayer.GetSlErrorMessage(wex);
            LogDispatchShortageLinesToTransferErrors(shortageRows, fromWhs, researchWhs, toOriWhs, dispatchUser, err);
            return err;
        }
        catch (Exception ex)
        {
            LogDispatchShortageLinesToTransferErrors(shortageRows, fromWhs, researchWhs, toOriWhs, dispatchUser, ex.Message);
            return ex.Message;
        }
        finally
        {
            sl.Logout();
        }
    }

    private void LogDispatchShortageLinesToTransferErrors(DataTable shortageRows,
        string fromWhs, string toWhs, string toOriWhs, string userApp, string errorMsg)
    {
        foreach (DataRow row in shortageRows.Rows)
        {
            InsertTransferError(
                Convert.ToInt32(row["LineNum"]),
                fromWhs,
                toWhs,
                toOriWhs,
                row["ItemCode"].ToString(),
                row["ItemName"].ToString(),
                Convert.ToInt32(row["ShortageQty"]),
                userApp,
                errorMsg);
        }
    }

    // Creates a SAP Inventory Transfer (OWTR) from the Duty Paid origin warehouse to the
    // research warehouse (queried from OWHS BPLId=3 Block='R') for each line where
    // DispatchQuantity > tmpQuantity (shortage on receipt).
    // Saves DocEntry/DocNum to DocEntryTraRec / DocNumTraRec.
    // On SAP failure, logs each shortage line to la_transfer_errors.
    // Returns null on success (including when there is no shortage), or error message.
    private string CreateSapDutyPaidShortageTransfer(string receiveUser, string researchWhs, out int sapDocNum)
    {
        sapDocNum = 0;
        string fromWhs  = "";
        string toOriWhs = "";
        int    shortageItrDocNum = 0;
        var    lines        = new JArray();
        var    shortageRows = new DataTable();

        try
        {
            db.Connect();

            string sql = @"
                SELECT h.FromWhsCode, h.ToWhsCode, ISNULL(h.DocNumITR, 0) AS DocNumITR,
                       d.LineNum, d.ItemCode, d.ItemName,
                       CAST(d.DispatchQuantity AS int) AS DispatchQty,
                       CAST(d.tmpQuantity      AS int) AS ReceivedQty,
                       CAST(d.DispatchQuantity - d.tmpQuantity AS int) AS ShortageQty
                FROM smm_Transdiscrep_odrf h WITH(NOLOCK)
                INNER JOIN smm_Transdiscrep_drf1 d WITH(NOLOCK)
                    ON h.DocEntry = d.DocEntry AND h.CompanyId = d.CompanyId
                WHERE h.CompanyId = @cid AND h.DocEntry = @de
                  AND d.DispatchQuantity > d.tmpQuantity
                ORDER BY d.LineNum";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.SelectCommand.Parameters.AddWithValue("@cid", sap_db);
            db.adapter.SelectCommand.Parameters.AddWithValue("@de",  GloVarDocEntry);
            db.adapter.Fill(shortageRows);

            if (shortageRows.Rows.Count == 0) return null;

            fromWhs  = shortageRows.Rows[0]["FromWhsCode"].ToString();
            toOriWhs = shortageRows.Rows[0]["ToWhsCode"].ToString();
            shortageItrDocNum = Convert.ToInt32(shortageRows.Rows[0]["DocNumITR"]);

            foreach (DataRow row in shortageRows.Rows)
            {
                lines.Add(new JObject(
                    new JProperty("ItemCode",          row["ItemCode"].ToString()),
                    new JProperty("Quantity",          Convert.ToInt32(row["ShortageQty"])),
                    new JProperty("FromWarehouseCode", fromWhs),
                    new JProperty("WarehouseCode",     researchWhs)
                ));
            }
        }
        catch (Exception ex)
        {
            return "Error reading shortage data: " + ex.Message;
        }
        finally
        {
            db.Disconnect();
        }

        if (lines.Count == 0) return null;

        string companyDb = ConfigurationManager.AppSettings["SL_CompanyDB"] ?? sap_db;

        string shortageComments = shortageItrDocNum > 0
            ? "Received short - ITR #" + shortageItrDocNum
            : "Received short";

        var payload = new JObject(
            new JProperty("FromWarehouse",      fromWhs),
            new JProperty("ToWarehouse",        researchWhs),
            new JProperty("U_BOL",              GloVarDocNum.ToString()),
            new JProperty("U_RECEIVE",          receiveUser),
            new JProperty("U_ORITOWHS",         fromWhs),
            new JProperty("U_Type",             "Duty Paid"),
            new JProperty("Comments",           shortageComments),
            new JProperty("StockTransferLines", lines)
        );

        var sl = new SapServiceLayer();
        try
        {
            sl.Login(companyDb);
            string response = sl.CreateInventoryTransfer(payload.ToString(Newtonsoft.Json.Formatting.None));

            try
            {
                var respObj  = JObject.Parse(response);
                int sapEntry = respObj["DocEntry"] != null ? Convert.ToInt32(respObj["DocEntry"]) : 0;
                sapDocNum    = respObj["DocNum"]   != null ? Convert.ToInt32(respObj["DocNum"])   : 0;

                if (sapEntry > 0)
                {
                    db.Connect();
                    using (var upd = new SqlCommand(
                        "UPDATE smm_Transdiscrep_odrf SET DocEntryTraRec = @Entry, DocNumTraRec = @Num " +
                        "WHERE CompanyId = @Company AND DocEntry = @LocalEntry", db.Conn))
                    {
                        upd.Parameters.AddWithValue("@Entry",      sapEntry);
                        upd.Parameters.AddWithValue("@Num",        sapDocNum);
                        upd.Parameters.AddWithValue("@Company",    sap_db);
                        upd.Parameters.AddWithValue("@LocalEntry", GloVarDocEntry);
                        upd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
            finally { db.Disconnect(); }

            return null;
        }
        catch (System.Net.WebException wex)
        {
            string err = SapServiceLayer.GetSlErrorMessage(wex);
            LogShortageLinesToTransferErrors(shortageRows, fromWhs, researchWhs, toOriWhs, receiveUser, err);
            return err;
        }
        catch (Exception ex)
        {
            LogShortageLinesToTransferErrors(shortageRows, fromWhs, researchWhs, toOriWhs, receiveUser, ex.Message);
            return ex.Message;
        }
        finally
        {
            sl.Logout();
        }
    }

    // Logs shortage lines (DispatchQty > ReceivedQty) to la_transfer_errors.
    private void LogShortageLinesToTransferErrors(DataTable shortageRows,
        string fromWhs, string toWhs, string toOriWhs, string userApp, string errorMsg)
    {
        foreach (DataRow row in shortageRows.Rows)
        {
            InsertTransferError(
                Convert.ToInt32(row["LineNum"]),
                fromWhs,
                toWhs,
                toOriWhs,
                row["ItemCode"].ToString(),
                row["ItemName"].ToString(),
                Convert.ToInt32(row["ShortageQty"]),
                userApp,
                errorMsg);
        }
    }

    // Creates OWTR #2 for surplus qty (tmpQty > DispatchQty, Duty Paid flow): FROM→same destination, no base doc.
    // On SAP failure, logs each surplus line to la_transfer_errors.
    // Returns null on success (including when there is no surplus), or error message.
    private string CreateSapDutyPaidSurplusTransfer(string receiveUser, out int sapDocNum)
    {
        sapDocNum = 0;
        string fromWhs  = "";
        string toWhs    = "";
        int    surplusItrDocNum = 0;
        var    lines       = new JArray();
        var    surplusRows = new DataTable();

        try
        {
            db.Connect();

            string sql = @"
                SELECT h.FromWhsCode, h.ToWhsCode, ISNULL(h.DocNumITR, 0) AS DocNumITR,
                       d.LineNum, d.ItemCode, d.ItemName,
                       CAST(d.tmpQuantity      AS int) AS ReceivedQty,
                       CAST(d.DispatchQuantity AS int) AS DispatchQty,
                       CAST(d.tmpQuantity - d.DispatchQuantity AS int) AS SurplusQty
                FROM smm_Transdiscrep_odrf h WITH(NOLOCK)
                INNER JOIN smm_Transdiscrep_drf1 d WITH(NOLOCK)
                    ON h.DocEntry = d.DocEntry AND h.CompanyId = d.CompanyId
                WHERE h.CompanyId = @cid AND h.DocEntry = @de
                  AND d.tmpQuantity > d.DispatchQuantity
                  AND d.DispatchQuantity > 0
                ORDER BY d.LineNum";

            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.SelectCommand.Parameters.AddWithValue("@cid", sap_db);
            db.adapter.SelectCommand.Parameters.AddWithValue("@de",  GloVarDocEntry);
            db.adapter.Fill(surplusRows);

            if (surplusRows.Rows.Count == 0) return null;

            fromWhs = surplusRows.Rows[0]["FromWhsCode"].ToString();
            toWhs   = surplusRows.Rows[0]["ToWhsCode"].ToString();
            surplusItrDocNum = Convert.ToInt32(surplusRows.Rows[0]["DocNumITR"]);

            foreach (DataRow row in surplusRows.Rows)
            {
                lines.Add(new JObject(
                    new JProperty("ItemCode",          row["ItemCode"].ToString()),
                    new JProperty("Quantity",          Convert.ToInt32(row["SurplusQty"])),
                    new JProperty("FromWarehouseCode", fromWhs),
                    new JProperty("WarehouseCode",     toWhs)
                ));
            }
        }
        catch (Exception ex)
        {
            return "Error reading surplus data: " + ex.Message;
        }
        finally
        {
            db.Disconnect();
        }

        if (lines.Count == 0) return null;

        string companyDb = ConfigurationManager.AppSettings["SL_CompanyDB"] ?? sap_db;

        var payload = new JObject(
            new JProperty("FromWarehouse",      fromWhs),
            new JProperty("ToWarehouse",        toWhs),
            new JProperty("U_BOL",              GloVarDocNum.ToString()),
            new JProperty("U_RECEIVE",          receiveUser),
            new JProperty("U_ORITOWHS",         toWhs),
            new JProperty("U_Type",             "Duty Paid"),
            new JProperty("Comments",           "Received over - Surplus qty received from origin warehouse - Transfer #" + GloVarDocNum + (surplusItrDocNum > 0 ? " - ITR #" + surplusItrDocNum : "")),
            new JProperty("StockTransferLines", lines)
        );

        var sl = new SapServiceLayer();
        try
        {
            sl.Login(companyDb);
            string response = sl.CreateInventoryTransfer(payload.ToString(Newtonsoft.Json.Formatting.None));

            try
            {
                var respObj = JObject.Parse(response);
                sapDocNum   = respObj["DocNum"] != null ? Convert.ToInt32(respObj["DocNum"]) : 0;
            }
            catch { }

            return null;
        }
        catch (System.Net.WebException wex)
        {
            string err = SapServiceLayer.GetSlErrorMessage(wex);
            LogSurplusLinesToTransferErrors(surplusRows, fromWhs, toWhs, receiveUser, err);
            return err;
        }
        catch (Exception ex)
        {
            LogSurplusLinesToTransferErrors(surplusRows, fromWhs, toWhs, receiveUser, ex.Message);
            return ex.Message;
        }
        finally
        {
            sl.Logout();
        }
    }

    // Logs surplus lines (tmpQty > DispatchQty) to la_transfer_errors.
    private void LogSurplusLinesToTransferErrors(DataTable surplusRows,
        string fromWhs, string toWhs, string userApp, string errorMsg)
    {
        foreach (DataRow row in surplusRows.Rows)
        {
            InsertTransferError(
                Convert.ToInt32(row["LineNum"]),
                fromWhs,
                toWhs,
                toWhs,
                row["ItemCode"].ToString(),
                row["ItemName"].ToString(),
                Convert.ToInt32(row["SurplusQty"]),
                userApp,
                errorMsg);
        }
    }

    // Logs main IT receive lines (tmpQuantity > 0) to la_transfer_errors when CreateSapInventoryTransfer fails.
    private void LogMainItLinesToTransferErrors(string userApp, string errorMsg)
    {
        try
        {
            db.Connect();

            string sql = @"
                SELECT h.FromWhsCode, d.ToWhsCode,
                       d.LineNum, d.ItemCode, d.ItemName,
                       CAST(d.tmpQuantity AS int) AS Qty
                FROM smm_Transdiscrep_odrf h WITH(NOLOCK)
                INNER JOIN smm_Transdiscrep_drf1 d WITH(NOLOCK)
                    ON h.DocEntry = d.DocEntry AND h.CompanyId = d.CompanyId
                WHERE h.CompanyId = @cid AND h.DocEntry = @de
                  AND d.tmpQuantity > 0
                ORDER BY d.LineNum";

            var dt = new DataTable();
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.SelectCommand.Parameters.AddWithValue("@cid", sap_db);
            db.adapter.SelectCommand.Parameters.AddWithValue("@de",  GloVarDocEntry);
            db.adapter.Fill(dt);

            foreach (DataRow row in dt.Rows)
            {
                InsertTransferError(
                    Convert.ToInt32(row["LineNum"]),
                    row["FromWhsCode"].ToString(),
                    row["ToWhsCode"].ToString(),
                    row["ToWhsCode"].ToString(),
                    row["ItemCode"].ToString(),
                    row["ItemName"].ToString(),
                    Convert.ToInt32(row["Qty"]),
                    userApp,
                    errorMsg);
            }
        }
        catch { }
        finally { db.Disconnect(); }
    }

    // Inserts a single row into la_transfer_errors.
    private void InsertTransferError(int lineNum, string fromWhs, string toWhs, string toOriWhs,
        string itemCode, string pluDesc, int qty, string userApp, string errorMsg)
    {
        try
        {
            db.Connect();
            using (var cmd = new SqlCommand(
                "INSERT INTO la_transfer_errors " +
                "(DocEntryOri, line, docdate, fromwhscode, towhscode, tooriwhscode, " +
                " itemcode, pludesc, quantity, userapp, error_message, fixed) " +
                "VALUES (@ori, @line, GETDATE(), @from, @to, @toori, " +
                "        @item, @plu, @qty, @user, @err, 'N')", db.Conn))
            {
                cmd.Parameters.AddWithValue("@ori",   GloVarDocEntry);
                cmd.Parameters.AddWithValue("@line",  lineNum);
                cmd.Parameters.AddWithValue("@from",  fromWhs);
                cmd.Parameters.AddWithValue("@to",    toWhs);
                cmd.Parameters.AddWithValue("@toori", toOriWhs);
                cmd.Parameters.AddWithValue("@item",  itemCode);
                cmd.Parameters.AddWithValue("@plu",   pluDesc);
                cmd.Parameters.AddWithValue("@qty",   qty);
                cmd.Parameters.AddWithValue("@user",  userApp);
                cmd.Parameters.AddWithValue("@err",   errorMsg);
                cmd.ExecuteNonQuery();
            }
        }
        catch { }
        finally { db.Disconnect(); }
    }

    protected void ReopenRecButton_Click(object sender, EventArgs e)
    {

    }

    protected void logTrace(string sObjectName, string sLogMessage)
    {
        db.Connect();

        try
        {

            db.cmd.Parameters.Clear();
            db.cmd.CommandText = "SMM_LOGTRACE_PRC";
            db.cmd.CommandType = CommandType.StoredProcedure;

            db.cmd.Parameters.Add(new SqlParameter("@ObjectName", SqlDbType.NVarChar));
            db.cmd.Parameters["@ObjectName"].Value = sObjectName;

            db.cmd.Parameters.Add(new SqlParameter("@LogMessage", SqlDbType.NVarChar));
            db.cmd.Parameters["@LogMessage"].Value = sLogMessage;


            db.cmd.Connection = db.Conn;


            db.cmd.ExecuteNonQuery();

        }
        catch (Exception ex)
        {
            db.Disconnect();
            throw new Exception("Caught exception in call procedure logTrace. ERROR MESSAGE : " + ex.Message);
        }

        db.Disconnect();

    }

    private string GetTransferType()
    {
        string transferType = "";
        db.Connect();
        try
        {
            using (var cmd = new SqlCommand(
                "SELECT ISNULL(TransferType,'') FROM smm_Transdiscrep_odrf " +
                "WHERE CompanyId = @Company AND DocEntry = @Entry", db.Conn))
            {
                cmd.Parameters.AddWithValue("@Company", sap_db);
                cmd.Parameters.AddWithValue("@Entry",   GloVarDocEntry);
                object val = cmd.ExecuteScalar();
                if (val != null && val != DBNull.Value) transferType = val.ToString();
            }
        }
        catch { }
        finally { db.Disconnect(); }
        return transferType;
    }

    private int GetOpchDocEntry()
    {
        db.Connect();
        try
        {
            using (var cmd = new SqlCommand(
                "SELECT ISNULL(DocEntryOPCH,0) FROM smm_Transdiscrep_odrf " +
                "WHERE CompanyId = @Company AND DocEntry = @Entry", db.Conn))
            {
                cmd.Parameters.AddWithValue("@Company", sap_db);
                cmd.Parameters.AddWithValue("@Entry",   GloVarDocEntry);
                object val = cmd.ExecuteScalar();
                return val != null && val != DBNull.Value ? Convert.ToInt32(val) : 0;
            }
        }
        catch { return 0; }
        finally { db.Disconnect(); }
    }

    // Reads OPCH line quantities from SAP and updates DispatchQuantity in
    // smm_Transdiscrep_drf1 so the receive view always reflects what was invoiced.
    // Only runs when: Dispatched='Y', Received='N', TransferType='SO', DocEntryOPCH > 0.
    private void SyncDispatchQtyFromOpch(string lvDispatched, string lvReceived)
    {
        if (lvDispatched != "Y" || lvReceived != "N") return;
        if (!string.Equals(GetTransferType(), "SO", StringComparison.OrdinalIgnoreCase)) return;

        int opchDocEntry = GetOpchDocEntry();
        if (opchDocEntry <= 0) return;

        var gr = new GoodsReceipt();
        DataTable dtLines = gr.GetApReserveInvoiceLines(sap_db, opchDocEntry);
        if (dtLines == null || dtLines.Rows.Count == 0) return;

        // Sum OPCH quantities by ItemCode (handles multiple lines per item)
        var opchQty = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (DataRow row in dtLines.Rows)
        {
            string itemCode = row["ItemCode"].ToString();
            decimal qty     = Convert.ToDecimal(row["Quantity"]);
            decimal existing;
            opchQty[itemCode] = opchQty.TryGetValue(itemCode, out existing) ? existing + qty : qty;
        }

        db.Connect();
        try
        {
            foreach (var kv in opchQty)
            {
                using (var cmd = new SqlCommand(
                    "UPDATE smm_Transdiscrep_drf1 SET DispatchQuantity = @qty, TmpQuantity = @qty " +
                    "WHERE CompanyId = @cid AND DocEntry = @de AND ItemCode = @item",
                    db.Conn))
                {
                    cmd.Parameters.AddWithValue("@qty",  kv.Value);
                    cmd.Parameters.AddWithValue("@cid",  sap_db);
                    cmd.Parameters.AddWithValue("@de",   GloVarDocEntry);
                    cmd.Parameters.AddWithValue("@item", kv.Key);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch { }
        finally { db.Disconnect(); }
    }

    // Creates an OPDN (Goods Receipt PO) in SAP B1 referencing the OPCH stored in DocEntryOPCH.
    // Reads received quantities from smm_Transdiscrep_drf1.TmpQuantity.
    // Saves OPDN DocEntry/DocNum to DocEntryTraRec2/DocNumTraRec2.
    // Returns null on success, or an error message string on failure.
    private string CreateApriOpdn(string receiveUser, out int sapDocNum, out Dictionary<string, decimal> surplusQtyByItem)
    {
        sapDocNum = 0;
        surplusQtyByItem = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        int    opchDocEntry = 0;
        string toWhsCode    = "";
        db.Connect();
        try
        {
            using (var cmd = new SqlCommand(
                "SELECT ISNULL(DocEntryOPCH,0), ISNULL(ToWhsCode,'') " +
                "FROM smm_Transdiscrep_odrf WHERE CompanyId=@c AND DocEntry=@e", db.Conn))
            {
                cmd.Parameters.AddWithValue("@c", sap_db);
                cmd.Parameters.AddWithValue("@e", GloVarDocEntry);
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        opchDocEntry = Convert.ToInt32(dr[0]);
                        toWhsCode    = dr[1].ToString();
                    }
                }
            }
        }
        catch (Exception ex) { db.Disconnect(); return "Error reading APRI record: " + ex.Message; }
        finally { db.Disconnect(); }

        if (opchDocEntry == 0)
            return "No OPCH DocEntry found for this APRI transfer (DocEntryOPCH is empty).";

        var gr = new GoodsReceipt();
        System.Data.DataRow opchHeader = gr.GetApReserveInvoiceHeader(sap_db, opchDocEntry);
        if (opchHeader == null)
            return "Could not load OPCH #" + opchDocEntry + ": " + (gr.LastError ?? "not found");

        System.Data.DataTable dtLines = gr.GetApReserveInvoiceLines(sap_db, opchDocEntry);
        if (dtLines == null || dtLines.Rows.Count == 0)
            return "No lines found in OPCH #" + opchDocEntry;

        // Build received quantities from TmpQuantity, keyed by ItemCode
        var sciQtyByItem = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        db.Connect();
        try
        {
            using (var cmd = new SqlCommand(
                "SELECT ItemCode, CAST(TmpQuantity AS DECIMAL(18,6)) " +
                "FROM smm_Transdiscrep_drf1 " +
                "WHERE CompanyId=@c AND DocEntry=@e AND TmpQuantity > 0", db.Conn))
            {
                cmd.Parameters.AddWithValue("@c", sap_db);
                cmd.Parameters.AddWithValue("@e", GloVarDocEntry);
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        sciQtyByItem[dr.GetString(0)] = dr.GetDecimal(1);
            }
        }
        catch (Exception ex) { db.Disconnect(); return "Error reading received quantities: " + ex.Message; }
        finally { db.Disconnect(); }

        if (sciQtyByItem.Count == 0) return null;

        var receivedQtys = new Dictionary<int, decimal>();
        foreach (System.Data.DataRow lr in dtLines.Rows)
        {
            string  itemCode    = lr["ItemCode"].ToString();
            int     lineNum     = Convert.ToInt32(lr["LineNum"]);
            decimal opchLineQty = Convert.ToDecimal(lr["Quantity"]);
            decimal sciQty;
            if (sciQtyByItem.TryGetValue(itemCode, out sciQty))
            {
                decimal toReceive = Math.Min(sciQty, opchLineQty);
                if (toReceive > 0)
                    receivedQtys[lineNum] = toReceive;
                decimal surplus = sciQty - opchLineQty;
                if (surplus > 0)
                    surplusQtyByItem[itemCode] = surplus;
            }
        }

        if (receivedQtys.Count == 0) return null;

        string cardCode   = opchHeader["CardCode"].ToString();
        int    bplId      = Convert.ToInt32(opchHeader["BplId"]);
        int    opchDocNum = Convert.ToInt32(opchHeader["DocNum"]);

        string payload = gr.BuildGrpoFromOpchWithQty(
            cardCode, bplId, opchDocEntry, opchDocNum, dtLines, receivedQtys,
            sciDocNum: GloVarDocNum, docNumITR: GloVarDocNumITR);

        string companyDb = ConfigurationManager.AppSettings["SL_CompanyDB"] ?? sap_db;
        var sl = new SapServiceLayer();
        try
        {
            sl.Login(companyDb);
            string response = sl.CreateGoodsReceiptPO(payload);

            try
            {
                var respObj  = JObject.Parse(response);
                int sapEntry = respObj["DocEntry"] != null ? Convert.ToInt32(respObj["DocEntry"]) : 0;
                sapDocNum    = respObj["DocNum"]   != null ? Convert.ToInt32(respObj["DocNum"])   : 0;

                if (sapEntry > 0)
                {
                    db.Connect();
                    using (var upd = new SqlCommand(
                        "UPDATE smm_Transdiscrep_odrf " +
                        "SET DocEntryTraRec2 = @entry, DocNumTraRec2 = @num " +
                        "WHERE CompanyId = @c AND DocEntry = @e", db.Conn))
                    {
                        upd.Parameters.AddWithValue("@entry", sapEntry);
                        upd.Parameters.AddWithValue("@num",   sapDocNum);
                        upd.Parameters.AddWithValue("@c",     sap_db);
                        upd.Parameters.AddWithValue("@e",     GloVarDocEntry);
                        upd.ExecuteNonQuery();
                    }
                    db.Disconnect();

                    gr.LogReceipt(sap_db, opchDocEntry, opchDocNum,
                        sapEntry, sapDocNum, cardCode, toWhsCode,
                        receiveUser, "SUCCESS", "");
                }
            }
            catch { }
            finally { db.Disconnect(); }

            return null;
        }
        catch (System.Net.WebException wex)
        {
            return SapServiceLayer.GetSlErrorMessage(wex);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            sl.Logout();
        }
    }

    // Processes surplus quantities (received > OPCH line qty) for a Duty Paid SO-type transfer.
    // Per item: if FROM warehouse has sufficient stock → creates OPDN #2 (dest branch) + OINV (origin branch).
    //           if insufficient stock → logs to la_transfer_errors, no document created for that item.
    // Returns null on success, or an error string if SAP document creation fails.
    private string CreateSurplusOpdn(string receiveUser,
        Dictionary<string, decimal> surplusQtyByItem, out int sapDocNum2)
    {
        sapDocNum2 = 0;
        if (surplusQtyByItem == null || surplusQtyByItem.Count == 0) return null;

        // ── 1. Read transfer header: FROM/TO warehouse and ORDR DocEntry ────────
        int    opchDocEntry = GetOpchDocEntry();
        int    docEntryITR  = 0;
        string fromWhsCode  = "";
        string toWhsCode    = "";
        db.Connect();
        try
        {
            using (var cmd = new SqlCommand(
                "SELECT ISNULL(DocEntryITR,0), ISNULL(FromWhsCode,''), ISNULL(ToWhsCode,'') " +
                "FROM smm_Transdiscrep_odrf WHERE CompanyId=@c AND DocEntry=@e", db.Conn))
            {
                cmd.Parameters.AddWithValue("@c", sap_db);
                cmd.Parameters.AddWithValue("@e", GloVarDocEntry);
                using (var dr = cmd.ExecuteReader())
                    if (dr.Read())
                    {
                        docEntryITR = Convert.ToInt32(dr[0]);
                        fromWhsCode = dr[1].ToString();
                        toWhsCode   = dr[2].ToString();
                    }
            }
        }
        catch { }
        finally { db.Disconnect(); }

        if (opchDocEntry == 0) return "No OPCH DocEntry for surplus OPDN.";

        // ── 2. Load OPCH header + lines (prices/warehouse for OPDN #2) ──────────
        var gr = new GoodsReceipt();
        System.Data.DataRow opchHeader = gr.GetApReserveInvoiceHeader(sap_db, opchDocEntry);
        if (opchHeader == null)
            return "Could not load OPCH #" + opchDocEntry + ": " + (gr.LastError ?? "not found");

        System.Data.DataTable dtOpchLines = gr.GetApReserveInvoiceLines(sap_db, opchDocEntry);
        if (dtOpchLines == null || dtOpchLines.Rows.Count == 0)
            return "No lines in OPCH #" + opchDocEntry;

        string opchCardCode = opchHeader["CardCode"].ToString();
        int    opchBplId    = Convert.ToInt32(opchHeader["BplId"]);

        var opchPriceByItem = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var opchWhsByItem   = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var opchUomByItem   = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string opchWhsType  = "";
        foreach (System.Data.DataRow lr in dtOpchLines.Rows)
        {
            string ic = lr["ItemCode"].ToString();
            opchPriceByItem[ic] = lr["Price"]   != System.DBNull.Value ? Convert.ToDecimal(lr["Price"]) : 0m;
            opchWhsByItem[ic]   = lr["WhsCode"].ToString();
            opchUomByItem[ic]   = lr["UoMCode"].ToString();
            if (string.IsNullOrEmpty(opchWhsType))
                opchWhsType = lr["WhsType"].ToString();
        }

        // ── 3. Load ORDR header + lines (prices/CardCode for OINV) ──────────────
        string ordrCardCode = "";
        int    ordrBplId    = 0;
        int    ordrDocNum   = 0;
        var    ordrPriceByItem = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (docEntryITR > 0)
        {
            db.Connect();
            try
            {
                using (var cmd = new SqlCommand(string.Format(
                    "SELECT CardCode, ISNULL(BPLId,1), ISNULL(DocNum,0) " +
                    "FROM {0}..ORDR WITH(NOLOCK) WHERE DocEntry=@e", sap_db), db.Conn))
                {
                    cmd.Parameters.AddWithValue("@e", docEntryITR);
                    using (var dr = cmd.ExecuteReader())
                        if (dr.Read())
                        {
                            ordrCardCode = dr[0].ToString();
                            ordrBplId    = Convert.ToInt32(dr[1]);
                            ordrDocNum   = Convert.ToInt32(dr[2]);
                        }
                }
                using (var cmd = new SqlCommand(string.Format(
                    "SELECT ItemCode, ISNULL(Price,0) " +
                    "FROM {0}..RDR1 WITH(NOLOCK) WHERE DocEntry=@e", sap_db), db.Conn))
                {
                    cmd.Parameters.AddWithValue("@e", docEntryITR);
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            ordrPriceByItem[dr.GetString(0)] = Convert.ToDecimal(dr[1]);
                }
            }
            catch { }
            finally { db.Disconnect(); }
        }

        // ── 4. Check inventory in FROM warehouse (OITW.OnHand) ──────────────────
        var stockByItem = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(fromWhsCode))
        {
            var itemListSb = new System.Text.StringBuilder();
            foreach (string ic in surplusQtyByItem.Keys)
            {
                if (itemListSb.Length > 0) itemListSb.Append(",");
                itemListSb.AppendFormat("'{0}'", ic.Replace("'", "''"));
            }
            db.Connect();
            try
            {
                using (var cmd = new SqlCommand(string.Format(
                    "SELECT ItemCode, ISNULL(OnHand,0) FROM {0}..OITW WITH(NOLOCK) " +
                    "WHERE WhsCode=@whs AND ItemCode IN ({1})", sap_db, itemListSb), db.Conn))
                {
                    cmd.Parameters.AddWithValue("@whs", fromWhsCode);
                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                            stockByItem[dr.GetString(0)] = Convert.ToDecimal(dr[1]);
                }
            }
            catch { }
            finally { db.Disconnect(); }
        }

        // ── 5. Load item descriptions for la_transfer_errors ────────────────────
        var descByItem = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        {
            var itemListSb2 = new System.Text.StringBuilder();
            foreach (string ic in surplusQtyByItem.Keys)
            {
                if (itemListSb2.Length > 0) itemListSb2.Append(",");
                itemListSb2.AppendFormat("'{0}'", ic.Replace("'", "''"));
            }
            db.Connect();
            try
            {
                using (var cmd = new SqlCommand(string.Format(
                    "SELECT ItemCode, ISNULL(ItemName,'') FROM {0}..OITM WITH(NOLOCK) " +
                    "WHERE ItemCode IN ({1})", sap_db, itemListSb2), db.Conn))
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                        descByItem[dr.GetString(0)] = dr.GetString(1);
            }
            catch { }
            finally { db.Disconnect(); }
        }

        // ── 6. Split surplus by stock availability (per-item) ───────────────────
        var hasStockItems = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        int lineIdx = 0;
        foreach (KeyValuePair<string, decimal> kv in surplusQtyByItem)
        {
            if (kv.Value <= 0) { lineIdx++; continue; }
            decimal onHand = 0m;
            stockByItem.TryGetValue(kv.Key, out onHand);

            if (onHand >= kv.Value)
            {
                hasStockItems[kv.Key] = kv.Value;
            }
            else
            {
                string desc = "";
                descByItem.TryGetValue(kv.Key, out desc);
                string destWhs = "";
                opchWhsByItem.TryGetValue(kv.Key, out destWhs);
                if (string.IsNullOrEmpty(destWhs)) destWhs = toWhsCode;
                InsertTransferError(lineIdx, fromWhsCode, destWhs, destWhs,
                    kv.Key, desc, (int)kv.Value, receiveUser, "Insufficient Inventory");
            }
            lineIdx++;
        }

        if (hasStockItems.Count == 0) return null; // all logged, nothing to create in SAP

        // ── 7. Build OPDN #2 payload (Goods Receipt PO at destination branch) ───
        string today   = DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        string comment = string.Format("Sobrante APRI - ORDR #{0} - Transfer DocEntry: {1}",
            ordrDocNum > 0 ? ordrDocNum.ToString() : docEntryITR.ToString(), GloVarDocEntry);

        var opdnLines = new JArray();
        foreach (KeyValuePair<string, decimal> kv in hasStockItems)
        {
            decimal price = 0m; opchPriceByItem.TryGetValue(kv.Key, out price);
            string  whs   = "";  opchWhsByItem.TryGetValue(kv.Key,   out whs);
            string  uom   = "";  opchUomByItem.TryGetValue(kv.Key,   out uom);

            var lineObj = new JObject(
                new JProperty("ItemCode",      kv.Key),
                new JProperty("Quantity",      kv.Value),
                new JProperty("UnitPrice",     price),
                new JProperty("WarehouseCode", whs)
            );
            if (!string.IsNullOrEmpty(uom)) lineObj["UoMCode"] = uom;
            opdnLines.Add(lineObj);
        }

        var opdnPayload = new JObject(
            new JProperty("CardCode",                opchCardCode),
            new JProperty("BPL_IDAssignedToInvoice", opchBplId),
            new JProperty("DocDate",                 today),
            new JProperty("TaxDate",                 today),
            new JProperty("DocDueDate",              today),
            new JProperty("Comments",                comment),
            new JProperty("U_bol",                   GloVarDocNum.ToString()),
            new JProperty("U_Type",                  !string.IsNullOrEmpty(opchWhsType) ? opchWhsType : "Duty Paid"),
            new JProperty("DocumentLines",           opdnLines)
        );
        if (GloVarDocNumITR > 0)
            opdnPayload["NumAtCard"] = GloVarDocNumITR.ToString();

        // ── 8. Build OINV payload (AR Invoice at origin/FROM branch via ORDR) ───
        JObject oinvPayload = null;
        if (docEntryITR > 0 && !string.IsNullOrEmpty(ordrCardCode))
        {
            var oinvLines = new JArray();
            foreach (KeyValuePair<string, decimal> kv in hasStockItems)
            {
                decimal price = 0m;
                ordrPriceByItem.TryGetValue(kv.Key, out price);

                oinvLines.Add(new JObject(
                    new JProperty("ItemCode",      kv.Key),
                    new JProperty("Quantity",      kv.Value),
                    new JProperty("UnitPrice",     price),
                    new JProperty("WarehouseCode", fromWhsCode)
                ));
            }
            if (oinvLines.Count > 0)
            {
                oinvPayload = new JObject(
                    new JProperty("CardCode",                ordrCardCode),
                    new JProperty("BPL_IDAssignedToInvoice", ordrBplId > 0 ? ordrBplId : 1),
                    new JProperty("DocDate",                 today),
                    new JProperty("TaxDate",                 today),
                    new JProperty("DocDueDate",              today),
                    new JProperty("Comments",                comment),
                    new JProperty("U_bol",                   GloVarDocNum.ToString()),
                    new JProperty("U_Type",                  !string.IsNullOrEmpty(opchWhsType) ? opchWhsType : "Duty Paid"),
                    new JProperty("DocumentLines",           oinvLines)
                );
                if (GloVarDocNumITR > 0)
                    oinvPayload["NumAtCard"] = GloVarDocNumITR.ToString();
            }
        }

        // ── 9. POST to SAP Service Layer ─────────────────────────────────────────
        string companyDb = ConfigurationManager.AppSettings["SL_CompanyDB"] ?? sap_db;
        var sl = new SapServiceLayer();
        try
        {
            sl.Login(companyDb);

            // OPDN #2
            string response = sl.CreateGoodsReceiptPO(opdnPayload.ToString(Newtonsoft.Json.Formatting.None));
            try
            {
                var respObj  = JObject.Parse(response);
                int sapEntry = respObj["DocEntry"] != null ? Convert.ToInt32(respObj["DocEntry"]) : 0;
                sapDocNum2   = respObj["DocNum"]   != null ? Convert.ToInt32(respObj["DocNum"])   : 0;
                if (sapEntry > 0)
                {
                    db.Connect();
                    try
                    {
                        using (var upd = new SqlCommand(
                            "UPDATE smm_Transdiscrep_odrf " +
                            "SET DocEntryOpdn2=@entry, DocNumOpdn2=@num " +
                            "WHERE CompanyId=@c AND DocEntry=@e", db.Conn))
                        {
                            upd.Parameters.AddWithValue("@entry", sapEntry);
                            upd.Parameters.AddWithValue("@num",   sapDocNum2);
                            upd.Parameters.AddWithValue("@c",     sap_db);
                            upd.Parameters.AddWithValue("@e",     GloVarDocEntry);
                            upd.ExecuteNonQuery();
                        }
                    }
                    finally { db.Disconnect(); }
                }
            }
            catch { }

            // OINV — only if OPDN succeeded and ORDR data is available
            if (oinvPayload != null)
            {
                try
                {
                    sl.CreateARInvoice(oinvPayload.ToString(Newtonsoft.Json.Formatting.None));
                }
                catch (System.Net.WebException wex)
                {
                    return "OPDN #2 OK (DocNum " + sapDocNum2 + "). Error creating AR Invoice: "
                           + SapServiceLayer.GetSlErrorMessage(wex);
                }
                catch (Exception ex)
                {
                    return "OPDN #2 OK (DocNum " + sapDocNum2 + "). Error creating AR Invoice: " + ex.Message;
                }
            }

            return null;
        }
        catch (System.Net.WebException wex)
        {
            return SapServiceLayer.GetSlErrorMessage(wex);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            sl.Logout();
        }
    }

    // Returns true when FROM=BODEGA (BPLId=1) and TO warehouse BPLId != 3 (ORDR/Sales Order path).
    // Returns false for Branch 3 destinations (OWTQ path) and for non-inter-branch transfers.
    private bool CheckIsBodegaToTienda(SqlConnection conn)
    {
        string sql = string.Format(
            @"SELECT TOP 1 ISNULL(tf.TYPEWHS,'') AS FromType,
                           ISNULL(wf.BPLId,0)   AS FromBPLId,
                           ISNULL(wt.BPLId,0)   AS ToBPLId
              FROM smm_Transdiscrep_odrf h WITH(NOLOCK)
              LEFT JOIN [{0}]..OWHS wf WITH(NOLOCK) ON wf.WhsCode = h.FromWhsCode
              LEFT JOIN dbo.SMM_WHSTYPE tf WITH(NOLOCK) ON tf.WHSCODE = h.FromWhsCode AND tf.COMPANYID = @cid
              LEFT JOIN smm_Transdiscrep_drf1 d WITH(NOLOCK) ON d.DocEntry = h.DocEntry AND d.CompanyId = h.CompanyId
              LEFT JOIN [{0}]..OWHS wt WITH(NOLOCK) ON wt.WhsCode = d.ToWhsCode
              WHERE h.CompanyId = @cid AND h.DocEntry = @de", sap_db);
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@cid", sap_db);
            cmd.Parameters.AddWithValue("@de",  GloVarDocEntry);
            using (var dr = cmd.ExecuteReader())
            {
                if (dr.Read())
                    return string.Equals(dr["FromType"].ToString(), "BODEGA", StringComparison.OrdinalIgnoreCase)
                           && Convert.ToInt32(dr["FromBPLId"]) == 1
                           && Convert.ToInt32(dr["ToBPLId"]) != 3;
            }
        }
        return false;
    }

    // Standalone overload — opens its own connection (for use outside of existing DB transactions).
    private bool CheckIsBodegaToTienda()
    {
        db.Connect();
        try   { return CheckIsBodegaToTienda(db.Conn); }
        catch { return false; }
        finally { db.Disconnect(); }
    }

    // Returns the inter-company customer CardCode for a destination warehouse via ApriCardCodeMapping.
    private string GetOrderCardCode(SqlConnection conn, string toWhs)
    {
        string sql = string.Format(
            "SELECT TOP 1 m.OinvCardCode FROM dbo.ApriCardCodeMapping m " +
            "JOIN [{0}]..OWHS w WITH(NOLOCK) ON w.BPLId = m.DestBPLId " +
            "WHERE w.WhsCode = @toWhs AND m.IsActive = 1", sap_db);
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@toWhs", toWhs);
            object val = cmd.ExecuteScalar();
            return val != null && val != DBNull.Value ? val.ToString() : null;
        }
    }
}
