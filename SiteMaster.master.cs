using System;
using System.Configuration;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telerik.Web.UI;
//using System.DirectoryServices;
//using System.DirectoryServices.AccountManagement;
//using Microsoft.Win32;
//using System.Diagnostics;


public partial class SiteMaster : System.Web.UI.MasterPage
{
    protected string v_User;
    public DataTable dt = new DataTable();
    //public DataManager dm = new DataManager();
    protected string v_MISMode = "N";
    protected void Page_Load(object sender, EventArgs e)
    {
        string lang = Session["Language"] as string ?? "en";
        lblHeading.Text = lang == "es" ? "Sistema de Control de Inventario" : "Inventory Control System";
        Page.Title = lblHeading.Text;

        lbEnglish.Style["opacity"] = lang == "en" ? "1" : "0.45";
        lbSpanish.Style["opacity"] = lang == "es" ? "1" : "0.45";

        // Pass current language to JavaScript
        Page.ClientScript.RegisterStartupScript(this.GetType(), "sci_lang",
            "var SCI_Lang = '" + lang + "';", true);

        // Update Login/Logout menu item on every request (language can change any time)
        bool isLoggedIn = !string.IsNullOrEmpty(Session["UserId"] as string);
        foreach (RadMenuItem menuItem in rmMainMenu.Items)
        {
            if (menuItem.Value == "LOGIN")
            {
                if (lang == "es")
                    menuItem.Text = isLoggedIn ? "Salir" : "Ingresar";
                else
                    menuItem.Text = isLoggedIn ? "Logout" : "Login";
            }
        }

        SetBreadCrumb();
        if (!IsPostBack)
        {
            if (v_MISMode == "Y")
            {
                //DFASecurity dfas = new DFASecurity();
                //v_User = dfas.GetIdentity().ToString();
                //if (v_User == "aespinoza" || v_User == "pveldi" || v_User == "jbaumgardner" || v_User == "jsimmons" || v_User == "rcortright" || v_User == "rsaavedra")
                ////if ("1" == "0")
                //{
                //    CheckBrowser();
                //    //GetUserPages();
                //    //SetThemeVisible();
                //}
                //else
                //{
                //    HttpContext.Current.Response.Redirect("MISMode.aspx", true);
                //}
            }
            else
            {
                CheckBrowser();
                //GetUserPages();
                //SetThemeVisible();
            }
            SetUser();
        }
    }
    private void SetThemeVisible()
    {
        //RadMenuItem currentItem = rmMainMenu.FindItemByUrl(Request.Url.PathAndQuery);
        //if (currentItem != null)
        //{
        //    if (currentItem.Value == "Default")
        //    {
        //        divSelectStyle.Visible = true;
        //    }
        //    else
        //    {
        //        divSelectStyle.Visible = false;
        //    }
        //}
    }
    private void SetUser()
    {
        string lang = Session["Language"] as string ?? "en";
        bool isEs = lang == "es";

        if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
        {
            lblUser.InnerText = "";
            lblCompany.InnerText = "";
        }
        else
        {
            string userName = Session["UserId"].ToString();
            lblUser.InnerHtml = "<span class=\"dropdown\" style=\"display:inline-block; position:relative;\">" +
                "<a href=\"#\" data-toggle=\"dropdown\" aria-expanded=\"false\" style=\"color:#004990; text-decoration:none;\">" +
                "<i class=\"fa fa-user\" style=\"font-size:22px; border:2px solid #004990; border-radius:50%; padding:5px 7px;\"></i>" +
                "</a>" +
                "<ul class=\"dropdown-menu dropdown-menu-right\" style=\"min-width:180px;\">" +
                "<li style=\"padding:8px 16px; white-space:nowrap; pointer-events:none;\">" +
                "<i class=\"fa fa-user\" style=\"margin-right:6px; color:#004990;\"></i><strong>" + userName + "</strong>" +
                "</li>" +
                "</ul>" +
                "</span>";

            string companyName = Session["CompanyName"] as string ?? "";
            string branchName  = Session["BranchName"]  as string ?? "";
            lblCompany.InnerText = companyName + (branchName != "" ? " | " + branchName : "");
        }
    }
    private void CheckBrowser()
    {
        try
        {
            System.Web.HttpBrowserCapabilities browser = Request.Browser;
            if (browser.Browser != "Chrome")
            {
                //Session["CheckBrowsers"] = "1";
                ShowDivMessage("Standard", "Need better Performance?", "For better performance with this application, please use Google Chrome!");
            }
        }
        catch (Exception ex)
        {
            ShowDivMessage("Error", "Failed to CheckBrowser", ex.Message.ToString());
        }
    }
    private void BuildMenu(DataTable dt)
    {
        try
        {
            foreach (DataRow item in dt.Rows)
            {
                if (item["HAS_ACCESS"].ToString() == "N")
                {
                    foreach (RadMenuItem menuItem in rmMainMenu.Items)
                    {
                        if (menuItem.Value == item["PAGE_NAME"].ToString())
                        {
                            rmMainMenu.Items[menuItem.Index - 1].Visible = false;  // Hide the separator
                            menuItem.Visible = false;
                        }
                        if (menuItem.Items.Count > 0)
                        {
                            int v_InvisibleChildItems = GetAllChildItems(menuItem, item["PAGE_NAME"].ToString());
                            if (v_InvisibleChildItems == menuItem.Items.Count)
                            {
                                rmMainMenu.Items[menuItem.Index - 1].Visible = false;
                                menuItem.Visible = false;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowDivMessage("Error", "Failed to GrantPageAccess", ex.Message.ToString());
        }
    }
    private int GetAllChildItems(RadMenuItem itemsList, string MenuId)
    {
        int i = 0;
        foreach (RadMenuItem menuItem2 in itemsList.Items)
        {
            if (menuItem2.Value == MenuId)
            {
                menuItem2.Visible = false;
                i = i + 1;
            }
            if (menuItem2.Items.Count > 0)
            {
                GetAllChildItems(menuItem2, MenuId);
            }
        }
        return i;
    }
    private void SetBreadCrumb()
    {
        RadMenuItem currentItem = rmMainMenu.FindItemByUrl(Request.Url.PathAndQuery);
        if (currentItem != null)
            currentItem.HighlightPath();
        else
            rmMainMenu.Items[0].HighlightPath();
    }
    public void ShowDivMessage(string v_Message_Type, string v_Message_Title, string v_Message)
    {
        rnMessage.Title = v_Message_Title.ToString();
        rnMessage.Text = v_Message.ToString();

        if (v_Message_Type == "Error" || v_Message_Type == "Delete")
        {
            rnMessage.TitleIcon = "delete";
        }
        else if (v_Message_Type == "Warning")
        {
            rnMessage.TitleIcon = "warning";
        }
        else if (v_Message_Type == "Standard")
        {
            rnMessage.TitleIcon = "info";
        }
        else if (v_Message_Type == "Ok")
        {
            rnMessage.TitleIcon = "ok";
        }
        else if (v_Message_Type == "Deny")
        {
            rnMessage.TitleIcon = "deny";
        }
        else if (v_Message_Type == "Edit")
        {
            rnMessage.TitleIcon = "edit";
        }

        rnMessage.Show();
    }
    protected void lbEnglish_Click(object sender, EventArgs e)
    {
        Session["Language"] = "en";
        Response.Redirect(Request.Url.PathAndQuery);
    }

    protected void lbSpanish_Click(object sender, EventArgs e)
    {
        Session["Language"] = "es";
        Response.Redirect(Request.Url.PathAndQuery);
    }

    protected void rcbSkins_SelectedIndexChanged(object sender, Telerik.Web.UI.RadComboBoxSelectedIndexChangedEventArgs e)
    {
        //RadHtmlChart pieStatus = (RadHtmlChart)cphMain.FindControl("pieStatus");
        //pieStatus.Skin = rcbSkins.SelectedValue;

        //RadHtmlChart rhcSalesLift = (RadHtmlChart)cphMain.FindControl("rhcSalesLift");
        //rhcSalesLift.Skin = rcbSkins.SelectedValue;

        //RadMap rmMap = (RadMap)cphMain.FindControl("rmMap");
        //rmMap.Skin = rcbSkins.SelectedValue;
    }

    #region commented Code
    //public void ShowAjaxMessage(string v_Message)
    //{
    //    //divAjaxMessage.InnerHtml = v_Message.ToString();
    //}

    //if (CheckUserAccess())
    //{
    //    GrantPageAccess();
    //}
    //else
    //{
    //    ShowDivMessage("Standard", "Access Denied", "Please contact MIS Helpdesk to get access to the application");
    //}

    //CheckUserGroupAccess(v_User);
    //SetUserAccess();

    //private bool CheckUserAccess()
    //{
    //    bool v_UserExists = false;
    //    try
    //    {
    //        v_UserExists = dm.CheckUserAccess();
    //    }
    //    catch (Exception ex)
    //    {
    //        ShowDivMessage("Error", "Failed to CheckUserAccess", ex.Message.ToString());
    //        return v_UserExists;
    //    }
    //    return v_UserExists;
    //}
    //private void GrantPageAccess()
    //{
    //    try
    //    {
    //        if (v_UserGroups != null)
    //        {
    //            for (int i = 0; i < v_UserGroups.Count(); i++)
    //            {
    //                if (v_UserGroups[i].Name.ToString().ToLower() == "sec-dfa-corp-mis-helpdesk")
    //                {

    //                }
    //            }
    //        }
    //        else
    //        {
    //            ShowDivMessage("Error", "Failed in SetUserAccess()","Failed to retrieve user group");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        ShowDivMessage("Error", "Failed to set user access", ex.Message.ToString());
    //    }
    //}

    //private void SetUserAccess()
    //{
    //    try
    //    {
    //        if (v_UserGroups != null)
    //        {
    //            for (int i = 0; i < v_UserGroups.Count(); i++)
    //            {
    //            v_UserGroups[i].Name
    //            }
    //        }
    //        else
    //        {
    //            ShowDivMessage("Error", "Failed in SetUserAccess()","Failed to retrieve user group");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        ShowDivMessage("Error", "Failed to set user access", ex.Message.ToString());
    //    }
    //}

    #endregion
    //protected void rmMainMenu_ItemClick(object sender, RadMenuEventArgs e)
    //{
    //    if (e.Item.Text=="Reports")
    //    {
    //        //string browser = string.Empty;
    //        //RegistryKey key = null;
    //        //try
    //        //{
    //        //    key = Registry.ClassesRoot.OpenSubKey(@"HTTP\shell\open\command", false);

    //        //    //trim off quotes
    //        //    browser = key.GetValue(null).ToString().ToLower().Replace("\"", "");
    //        //    if (!browser.EndsWith("exe"))
    //        //    {
    //        //        //get rid of everything after the ".exe"
    //        //        browser = browser.Substring(0, browser.LastIndexOf(".exe") + 4);
    //        //    }
    //        //}
    //        //finally
    //        //{
    //        //    if (key != null) key.Close();
    //        //}
    //        //return browser;

    //        //Process p = new Process();
    //        //p.StartInfo.FileName = "iexplore.exe";
    //        //p.StartInfo.Arguments = "http://www.ryanfarley.com/";
    //        //p.Start();
    //    }
    //}
}
