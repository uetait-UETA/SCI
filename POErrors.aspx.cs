using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telerik.Web.UI;

// Ported from ADA (SCIv2) — screen only. The data behind it (Compras Directas "extra items"
// price errors: ComprasDirectasExtraItems / ComprasDirectasExtraItemsPriceErrors and the
// GoodsReceipt.GetExtraItemPriceErrors / ReprocessPendingExtraItems methods) does not exist
// in this project yet, so the grid stays empty and Reprocess is disabled until it is ported.
public partial class POErrors : BasePage
{
    protected SqlDb db = new SqlDb();
    protected string lCurUser;

    protected void Page_Load(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty((string)Session["UserId"]))
        {
            Response.Redirect("Login1.aspx");
            return;
        }

        lCurUser = (string)Session["UserId"];
        string strAccessType = "", strRoleDescription = "";

        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, "POErrors.aspx", ref strAccessType, ref strRoleDescription);
        db.Disconnect();

        if (strAccessType == "N")
        {
            string message = "User " + lCurUser + ", with Role " + strRoleDescription + " does not have permissions to access this screen.";
            string script = "{ alert('" + message + "'); window.location = 'Default.aspx'; }";
            ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "alert", script, true);
            return;
        }

        labelForm.InnerText = strAccessType == "F" ? "PO Errors" : "PO Errors (Read-Only Access)";

        lblResult.Text = "PO Errors is not available yet in this company (Direct Purchase extra items are not enabled).";
        lblResult.ForeColor = System.Drawing.Color.DarkOrange;
    }

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        rgHead.Rebind();
    }

    protected void rgHead_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        rgHead.DataSource = new DataTable();
    }

    protected void rgHead_ItemDataBound(object sender, GridItemEventArgs e)
    {
    }

    protected void rgHead_ItemCommand(object sender, GridCommandEventArgs e)
    {
    }
}
