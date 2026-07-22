using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Data.SqlClient;
using System.Drawing;
using Newtonsoft.Json.Linq;


public partial class TransferErrors : BasePage
{
    protected SqlDb db = new SqlDb();
    protected string sap_db;
    protected string lCurUser;


    protected void Page_Load(object sender, EventArgs e)
    {

        if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

        sap_db = sap_db = (string)this.Session["CompanyId"];
        CompanyIdLabel.Text = sap_db;
                    
        ///////////////Begin New  Control de acceso por Roles
	    if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
	    {
            Response.Redirect("Login1.aspx");
	    }

        lCurUser = (string)this.Session["UserId"];
        char flagokay = 'Y';
		string lControlName = "TransferErrors.aspx";
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
			Button2.Enabled = false;
			Button2.ForeColor = Color.Silver;
            labelForm.InnerText = "Transfer Errors (Read-Only Access)";      
		}
			
		if (strAccessType == "F")
		{
			Button2.Enabled = true; 
			labelForm.InnerText = "Transfer Errors (Full Access)";           
		}	
			
		if (flagokay == 'Y')
        {
            ObjectDataSource1.SelectParameters["ShowAll"].DefaultValue = radioShowAll.Checked.ToString();
            int branchId = 0;
            int.TryParse(Session["BranchId"] as string, out branchId);
            ObjectDataSource1.SelectParameters["BranchId"].DefaultValue = branchId.ToString();
        }
        ///////////////End  New Control de acceso por Roles
    }

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        GridView1.DataBind();
    }
   
    protected void Button2_Click(object sender, EventArgs e)
    {
        int rg2count = GridView1.Rows.Count;
        string lblvalue = "";
        string lfixed = "";
        int  LvLinNum;
        int  DocEntryOri;

        db.Connect();

        //TextBox1.Text = "";

        for (int i = 0; i < rg2count; i++)
        {

            Label lblDe = (Label)GridView1.Rows[i].FindControl("DE");
            Label lblLn = (Label)GridView1.Rows[i].FindControl("Ln");

            DocEntryOri = Convert.ToInt32(lblDe.Text);
            LvLinNum = Convert.ToInt32(lblLn.Text);           

            RadioButtonList LMyList = (RadioButtonList)GridView1.Rows[i].FindControl("rblFixed");
            ListItem Litem = LMyList.Items.FindByText("Fixed");

            Label lblS = (Label)GridView1.Rows[i].FindControl("lblSelected");
            lblvalue = lblS.Text.ToString ();
            
            if (lblvalue == "0")
            {
                lblvalue = "False";
            }
            else
            {
                lblvalue = "True";
            }
            

            if (Litem != null)
            {
                
                if (Litem.Selected.ToString() == "True")
                {
                    lfixed = "Y";
                }
                else
                {
                    lfixed = "N";
                }

                //TextBox1.Text = TextBox1.Text + '/' + DocEntryOri.ToString()  + '-' + LvLinNum.ToString () + '-' + lfixed + '-' + lblvalue;//    .Value.ToString();

                if (Litem.Selected.ToString() != lblvalue)
                {                    
                    SqlCommand sqlCommand = new SqlCommand();
                    sqlCommand.Parameters.Clear();
                    sqlCommand.CommandText = "la_update_transfer_errors";
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Connection = db.Conn;

                    sqlCommand.Parameters.Add(new SqlParameter("@DocEntry", SqlDbType.Int));
                    sqlCommand.Parameters["@DocEntry"].Value = DocEntryOri;

                    sqlCommand.Parameters.Add(new SqlParameter("@Linenum", SqlDbType.SmallInt));
                    sqlCommand.Parameters["@Linenum"].Value = LvLinNum;

                    sqlCommand.Parameters.Add(new SqlParameter("@Fixed", SqlDbType.NChar));
                    sqlCommand.Parameters["@Fixed"].Value = lfixed;

                    try
                    {
                        sqlCommand.ExecuteNonQuery();
                    }

                    catch (Exception ex)
                    {
                        Response.Write("la_update_transfer_errors was called.");
                        Response.Write(ex.Message);
                    }
                }

            }
           

            lblvalue = "";

        }

              

        

        
        
    }
    protected void GridView1_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName != "ReprocessShortage") return;
        int docEntryOri = Convert.ToInt32(e.CommandArgument);
        string result = ReprocessShortage(docEntryOri);
        if (result == null)
        {
            divMessage.InnerText = "IT de faltante reprocesado correctamente en SAP.";
            divMessage.Attributes["class"] = "alert alert-success";
        }
        else
        {
            divMessage.InnerText = "Error al reprocesar: " + result;
            divMessage.Attributes["class"] = "alert alert-danger";
        }
        GridView1.DataBind();
    }

    private string ReprocessShortage(int docEntryOri)
    {
        string fromWhs      = "";
        string researchWhs  = "";
        string fromWhsUType = "";
        int    docNum       = 0;
        int    docNumItr    = 0;
        var    lines        = new JArray();
        var    shortageRows = new DataTable();

        db.Connect();
        try
        {
            // 1. Try to get research whs from existing error records
            using (var cmd = new SqlCommand(
                "SELECT TOP 1 towhscode FROM la_transfer_errors " +
                "WHERE DocEntryOri=@de AND fixed='N' AND ISNULL(towhscode,'')<>''",
                db.Conn))
            {
                cmd.Parameters.AddWithValue("@de", docEntryOri);
                object val = cmd.ExecuteScalar();
                if (val != null && val != DBNull.Value)
                    researchWhs = val.ToString();
            }

            // 2. Get draft header + FROM warehouse U_Type
            using (var cmd = new SqlCommand(
                "SELECT h.FromWhsCode, h.DocNum, ISNULL(h.DocNumITR,0) AS DocNumITR, " +
                "ISNULL(w.U_Type,'') AS FromUType " +
                "FROM smm_Transdiscrep_odrf h WITH(NOLOCK) " +
                "LEFT JOIN [" + sap_db + "]..OWHS w WITH(NOLOCK) ON w.WhsCode=h.FromWhsCode " +
                "WHERE h.CompanyId=@cid AND h.DocEntry=@de", db.Conn))
            {
                cmd.Parameters.AddWithValue("@cid", sap_db);
                cmd.Parameters.AddWithValue("@de",  docEntryOri);
                using (var rdr = cmd.ExecuteReader())
                {
                    if (!rdr.Read())
                        return "Draft no encontrado en smm_Transdiscrep_odrf.";
                    fromWhs      = rdr["FromWhsCode"].ToString();
                    docNum       = Convert.ToInt32(rdr["DocNum"]);
                    docNumItr    = Convert.ToInt32(rdr["DocNumITR"]);
                    fromWhsUType = rdr["FromUType"].ToString();
                }
            }

            // 3. If research whs not in la_transfer_errors, look it up in OWHS
            if (string.IsNullOrEmpty(researchWhs))
            {
                string uTypeClause = string.IsNullOrEmpty(fromWhsUType) ? "" : " AND ISNULL(U_Type,'')=@utype";

                int fromBplId = 0, toBplId = 0;
                using (var cmd = new SqlCommand(
                    "SELECT ISNULL(BPLId,0) FROM [" + sap_db + "]..OWHS WITH(NOLOCK) WHERE WhsCode=@whs",
                    db.Conn))
                {
                    cmd.Parameters.AddWithValue("@whs", fromWhs);
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) fromBplId = Convert.ToInt32(val);
                }
                using (var cmd = new SqlCommand(
                    "SELECT ISNULL(t.BPLId,0) FROM smm_Transdiscrep_odrf h WITH(NOLOCK) " +
                    "JOIN [" + sap_db + "]..OWHS t WITH(NOLOCK) ON t.WhsCode=h.ToWhsCode " +
                    "WHERE h.CompanyId=@cid AND h.DocEntry=@de", db.Conn))
                {
                    cmd.Parameters.AddWithValue("@cid", sap_db);
                    cmd.Parameters.AddWithValue("@de",  docEntryOri);
                    object val = cmd.ExecuteScalar();
                    if (val != null && val != DBNull.Value) toBplId = Convert.ToInt32(val);
                }

                int[] tryBplIds = (fromBplId == toBplId || toBplId == 0)
                    ? new[] { fromBplId }
                    : new[] { fromBplId, toBplId };
                foreach (int bplId in tryBplIds)
                {
                    if (bplId == 0) continue;
                    using (var cmd = new SqlCommand(
                        "SELECT TOP 1 WhsCode FROM [" + sap_db + "]..OWHS WITH(NOLOCK) " +
                        "WHERE BPLId=@bpl AND Block='R'" + uTypeClause, db.Conn))
                    {
                        cmd.Parameters.AddWithValue("@bpl", bplId);
                        if (!string.IsNullOrEmpty(fromWhsUType))
                            cmd.Parameters.AddWithValue("@utype", fromWhsUType);
                        object val = cmd.ExecuteScalar();
                        if (val != null && val != DBNull.Value) { researchWhs = val.ToString(); break; }
                    }
                }
            }

            if (string.IsNullOrEmpty(researchWhs))
                return "No se encontró warehouse de investigación (Block='R') en OWHS para este transfer.";

            // 4. Get shortage lines (DispatchQuantity > tmpQuantity)
            string lineSql = @"
                SELECT d.LineNum, d.ItemCode, d.ItemName,
                       CAST(d.DispatchQuantity - d.tmpQuantity AS int) AS ShortageQty
                FROM smm_Transdiscrep_drf1 d WITH(NOLOCK)
                WHERE d.CompanyId=@cid AND d.DocEntry=@de
                  AND d.DispatchQuantity > d.tmpQuantity
                ORDER BY d.LineNum";
            using (var adapter = new SqlDataAdapter(lineSql, db.Conn))
            {
                adapter.SelectCommand.Parameters.AddWithValue("@cid", sap_db);
                adapter.SelectCommand.Parameters.AddWithValue("@de",  docEntryOri);
                adapter.Fill(shortageRows);
            }

            if (shortageRows.Rows.Count == 0)
                return "No hay líneas con faltante (DispatchQuantity > tmpQuantity) en el draft.";

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
            return "Error leyendo datos: " + ex.Message;
        }
        finally
        {
            db.Disconnect();
        }

        string companyDb = System.Configuration.ConfigurationManager.AppSettings["SL_CompanyDB"] ?? sap_db;
        string comments  = docNumItr > 0 ? "Received short - ITR #" + docNumItr : "Received short";

        var payload = new JObject(
            new JProperty("FromWarehouse",      fromWhs),
            new JProperty("ToWarehouse",        researchWhs),
            new JProperty("U_BOL",              docNum.ToString()),
            new JProperty("U_RECEIVE",          lCurUser),
            new JProperty("U_ORITOWHS",         fromWhs),
            new JProperty("U_Type",             "Duty Paid"),
            new JProperty("Comments",           comments),
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
                int sapNum   = respObj["DocNum"]   != null ? Convert.ToInt32(respObj["DocNum"])   : 0;
                if (sapEntry > 0)
                {
                    db.Connect();
                    using (var upd = new SqlCommand(
                        "UPDATE smm_Transdiscrep_odrf " +
                        "SET DocEntryTraRec=@entry, DocNumTraRec=@num " +
                        "WHERE CompanyId=@cid AND DocEntry=@de", db.Conn))
                    {
                        upd.Parameters.AddWithValue("@entry", sapEntry);
                        upd.Parameters.AddWithValue("@num",   sapNum);
                        upd.Parameters.AddWithValue("@cid",   sap_db);
                        upd.Parameters.AddWithValue("@de",    docEntryOri);
                        upd.ExecuteNonQuery();
                    }
                    using (var fix = new SqlCommand(
                        "UPDATE la_transfer_errors SET fixed='Y' WHERE DocEntryOri=@de AND fixed='N'",
                        db.Conn))
                    {
                        fix.Parameters.AddWithValue("@de", docEntryOri);
                        fix.ExecuteNonQuery();
                    }
                    db.Disconnect();
                    return null;
                }
                return "SAP no devolvió DocEntry en la respuesta.";
            }
            catch (Exception ex) { db.Disconnect(); return "Error procesando respuesta SAP: " + ex.Message; }
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

    protected void GridView1_DataBinding(object sender, EventArgs e)
    {
        /*if (e.Row.RowType == DataControlRowType.DataRow)
        {
            RadioButtonList rdoRowList = e.Row.FindControl("rblFixed") as RadioButtonList;
            //rdoRowList.SelectedIndex = ((ObjectDataSource)((GridView) sender).DataSource).d
        }*/

    }
    protected void GridView1_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            RadioButtonList MyList = (RadioButtonList)e.Row.FindControl("rblFixed");
            Label lblSelected = (Label)e.Row.FindControl("lblSelected");
            if (lblSelected != null && MyList != null)
            {
                ListItem item = MyList.Items.FindByValue(lblSelected.Text);
                if (item != null)
                {
                    MyList.Items.FindByValue(lblSelected.Text).Selected = true;
                }
            }

            // Show Reprocess button only for unfixed rows when user has full access
            LinkButton btnReprocess = (LinkButton)e.Row.FindControl("btnReprocess");
            if (btnReprocess != null)
            {
                bool isFixed = lblSelected != null && lblSelected.Text == "1";
                btnReprocess.Visible = !isFixed && Button2.Enabled;
            }
        }

    }
}
