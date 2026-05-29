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

        if (strAccessType != "F")
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
        //In order to get the current index code:

        GridViewRow gridViewRow = (GridViewRow)(sender as Control).Parent.Parent;
        int index = gridViewRow.RowIndex;

        //In order to get the control code:

        HiddenField lhdnDocEntry = (HiddenField)this.GridView1.Rows[index].FindControl("hdnDocEntry");   //row.FindControl("HiddenDocentry");
        string sDocEntry = lhdnDocEntry.Value;

        
        db.Connect();

        DataSet lDataSet = new DataSet();
        //SqlCommand sqlCommand = new SqlCommand();

        db.cmd.Parameters.Clear();
        db.cmd.CommandText = "SMM_DELETE_DRAFT_TRANSFER";
        db.cmd.CommandType = CommandType.StoredProcedure;

        db.cmd.Parameters.Add(new SqlParameter("@iDocEntry", SqlDbType.Int));
        db.cmd.Parameters["@iDocEntry"].Value = Convert.ToInt32(sDocEntry);

        db.cmd.Parameters.Add(new SqlParameter("@UserDelete", SqlDbType.NVarChar));
        db.cmd.Parameters["@UserDelete"].Value = appUserName;

        db.cmd.Parameters.Add(new SqlParameter("@CompanyId", SqlDbType.NVarChar));
        db.cmd.Parameters["@CompanyId"].Value = sap_db;

        db.cmd.Connection = db.Conn;

        try
        {
            db.cmd.ExecuteNonQuery();
            //Response.Write("<script>window.close()</script>");
	        string message = "Order " + sDocEntry + " has been deleted.";
	    	        string url = "/TransferDelete.aspx";
	    	        string script = "{ alert('";
	    	        script += message;
	    	        script += "');";
	    	        script += "window.location = '";
	    	        script += url;
	    	        script += "'; }";
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