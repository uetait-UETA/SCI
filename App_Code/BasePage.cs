using System;
using System.Web.UI;

public class BasePage : System.Web.UI.Page
{
    protected virtual bool RequiresLogin { get { return true; } }

    protected override void OnPreInit(EventArgs e)
    {
        string lang = Session["Language"] as string ?? "en";
        MasterPageFile = lang == "es" ? "~/SiteMaster01142021.master" : "~/SiteMaster.master";
        base.OnPreInit(e);
    }

    protected override void OnLoad(EventArgs e)
    {
        if (RequiresLogin && string.IsNullOrEmpty(Session["UserId"] as string))
        {
            Response.Redirect("~/Login1.aspx", true);
            return;
        }
        base.OnLoad(e);
    }

    protected int BranchId
    {
        get
        {
            object val = Session["BranchId"];
            return (val != null && val.ToString() != "") ? Convert.ToInt32(val) : 0;
        }
    }

    protected string BranchName
    {
        get
        {
            string val = Session["BranchName"] as string;
            return val != null ? val : "";
        }
    }

    protected bool IsSellerBranch
    {
        get
        {
            return (Session["IsSellerBranch"] as string) == "Y";
        }
    }
}
