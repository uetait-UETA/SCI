using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Data.SqlClient;

public partial class TransferDelete : BasePage
{
    protected string sap_db;
    protected string appUserName;
    protected SqlDb db = new SqlDb();
    protected string lCurUser;


    protected void Page_Load(object sender, EventArgs e)
    {
        if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

        if (string.IsNullOrEmpty((string)this.Session["CompanyId"]))
        {
            Response.Redirect("Login1.aspx");
        }

        appUserName = (string)this.Session["UserId"];

        sap_db = (string)this.Session["CompanyId"];
        CompanyIdLabel.Text = sap_db;

        ///////////////Begin New  Control de acceso por Roles
	    lCurUser = (string)this.Session["UserId"];
	    char flagokay = 'Y';
		string lControlName = "TransferDelete.aspx"; //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

        string strRole_Description = "";
        string strAccessType = "";

        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, lControlName, ref strAccessType, ref strRole_Description);
        db.Disconnect();

        // Allow: GERENTE DE OPERACION or superuser (TOTAL)
        bool isAllowedRole = string.Equals(strRole_Description, "GERENTE DE OPERACION", StringComparison.OrdinalIgnoreCase)
                          || string.Equals(strRole_Description, "TOTAL", StringComparison.OrdinalIgnoreCase);

        if (strAccessType != "F" || !isAllowedRole)
		{
		    flagokay = 'N';
		    string message = !isAllowedRole
                ? "Access restricted. Only users with role 'GERENTE DE OPERACION' can access this screen. Your role: " + strRole_Description
                : "User " + lCurUser + " does not have permissions to access this screen.";
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
		    labelForm.InnerText = "Delete Transfer (Read-Only Access)";
	    }
			
		if (strAccessType == "F")
		{
		    labelForm.InnerText = "Delete Transfer (Full Access)"; //////////<<<<<<<<
		}	
///////////////End  New Control de acceso por Roles

    }
    
    protected void btnSearch_Click(object sender, EventArgs e)
    {

    }

    protected void DeleteBtn_Click(object sender, EventArgs e)
    {
        GridViewRow gridViewRow = (GridViewRow)(sender as Control).Parent.Parent;
        int index = gridViewRow.RowIndex;

        HiddenField lhdnDocEntry = (HiddenField)this.GridView1.Rows[index].FindControl("hdnDocEntry");
        string sDocEntry = lhdnDocEntry.Value;
        int docEntryInt = Convert.ToInt32(sDocEntry);

        db.Connect();
        try
        {
            // Check if draft is TIENDA→TIENDA and get its OWTQ DocEntryITR
            int owtqDocEntry = 0;
            bool isTiendaToTienda = false;
            string sqlCheck = @"
                SELECT
                    ISNULL(o.DocEntryITR, 0) AS DocEntryITR,
                    ISNULL(fw.TYPEWHS, '') AS FromWhsType,
                    ISNULL(tw.TYPEWHS, '') AS ToWhsType
                FROM smm_Transdiscrep_odrf o WITH(NOLOCK)
                LEFT JOIN SMM_WHSTYPE fw WITH(NOLOCK)
                    ON fw.WHSCODE = o.FromWhsCode AND fw.COMPANYID = @CompanyId
                LEFT JOIN (
                    SELECT TOP 1 d.DocEntry, d.CompanyId, d.ToWhsCode
                    FROM smm_Transdiscrep_drf1 d WITH(NOLOCK)
                    WHERE d.DocEntry = @DocEntry AND d.CompanyId = @CompanyId
                ) AS firstLine ON firstLine.DocEntry = o.DocEntry AND firstLine.CompanyId = o.CompanyId
                LEFT JOIN SMM_WHSTYPE tw WITH(NOLOCK)
                    ON tw.WHSCODE = firstLine.ToWhsCode AND tw.COMPANYID = @CompanyId
                WHERE o.DocEntry = @DocEntry AND o.CompanyId = @CompanyId";

            using (var cmd = new SqlCommand(sqlCheck, db.Conn))
            {
                cmd.Parameters.AddWithValue("@DocEntry", docEntryInt);
                cmd.Parameters.AddWithValue("@CompanyId", sap_db);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        owtqDocEntry = Convert.ToInt32(reader["DocEntryITR"]);
                        string fromType = reader["FromWhsType"].ToString();
                        string toType   = reader["ToWhsType"].ToString();
                        isTiendaToTienda = string.Equals(fromType, "TIENDA", StringComparison.OrdinalIgnoreCase)
                                        && string.Equals(toType,   "TIENDA", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            // Cancel OWTQ in SAP B1 when TIENDA→TIENDA and an ITR exists
            if (isTiendaToTienda && owtqDocEntry > 0)
            {
                var sl = new SapServiceLayer();
                try
                {
                    sl.Login(sap_db);
                    sl.CloseInventoryTransferRequest(owtqDocEntry);
                }
                catch (Exception slEx)
                {
                    db.Disconnect();
                    Response.Write("Error canceling SAP Transfer Request (DocEntry=" + owtqDocEntry + "): " + slEx.Message);
                    return;
                }
                finally
                {
                    sl.Logout();
                }
            }

            db.cmd.Parameters.Clear();
            db.cmd.CommandText = "SMM_DELETE_DRAFT_TRANSFER";
            db.cmd.CommandType = CommandType.StoredProcedure;
            db.cmd.Parameters.Add(new SqlParameter("@iDocEntry",  SqlDbType.Int))    .Value = docEntryInt;
            db.cmd.Parameters.Add(new SqlParameter("@UserDelete", SqlDbType.NVarChar)).Value = appUserName;
            db.cmd.Parameters.Add(new SqlParameter("@CompanyId",  SqlDbType.NVarChar)).Value = sap_db;
            db.cmd.Connection = db.Conn;

            db.cmd.ExecuteNonQuery();

            string message = "Order " + sDocEntry + " has been deleted.";
            string url     = "/TransferDelete.aspx";
            string script  = "{ alert('" + message + "'); window.location = '" + url + "'; }";
            ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "alert", script, true);
        }
        catch (Exception ex)
        {
            Response.Write("Error when SMM_DELETE_DRAFT_TRANSFER was called.");
            Response.Write(ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        GridView1.DataBind();
    }

    protected void lnkbDocEntry_Click(object sender, EventArgs e)
    {
        GridViewRow gridViewRow = (GridViewRow)(sender as Control).Parent.Parent;
        HiddenField lhdnDocEntry = (HiddenField)this.GridView1.Rows[gridViewRow.RowIndex].FindControl("hdnDocEntry");   //row.FindControl("HiddenDocentry");

        rwTransfers.NavigateUrl = "TransferDetailsPrint.aspx?DocEntry=" + lhdnDocEntry.Value;
        rwTransfers.Width = Unit.Pixel(Convert.ToInt32(Math.Round(Convert.ToDouble(hfScreenWidth.Value) * 0.98)));
        rwTransfers.Height = Unit.Pixel(Convert.ToInt32(Math.Round(Convert.ToDouble(hfScreenHeight.Value) * 0.90)));

        string script = "function f(){$find(\"" + rwTransfers.ClientID + "\").show(); Sys.Application.remove_load(f);}Sys.Application.add_load(f);";

        ScriptManager.RegisterStartupScript(Page, Page.GetType(), "key", script, true);
    }
}