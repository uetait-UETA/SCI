using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telerik.Web.UI;
using System.Collections;
using System.Configuration;
using System.Drawing;
using System.Data.SqlClient;

public partial class LoginEdit : BasePage
{
    protected SqlDb db = new SqlDb();
    protected string appUserName;
    protected string lCurUser;
    protected void Page_Load(object sender, EventArgs e)
    {
	    if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
    	{
    	    Response.Redirect("../Login1.aspx");
    	}
        
        appUserName = (string)Session["UserId"];
	        
        ///////////////Begin New  Control de acceso por Roles

		lCurUser = appUserName;
		//flagokay = 'Y';
		string lControlName = "LoginEdit.aspx";

        string strAccessType = "";
        string strRole_Description = "";

        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, lControlName, ref strAccessType, ref strRole_Description);
        db.Disconnect();

        if (strAccessType != "F") //if(strAccessType == "N")
		{
		    //flagokay = 'N';
		    string message = "User " + lCurUser + ", with Role " + strRole_Description + " does not have permissions to access this screen.";
		    string url = string.Format("../Default.aspx");
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
			labelForm.InnerText = "Credentials (Read-Only Access)";
		}

		if (strAccessType == "F")
		{
			labelForm.InnerText = "Credentials (Full Access)";
		}	

        ///////////////End  New Control de acceso por Roles
    }

    protected void gridLogins_NeedDataSource(object sender, Telerik.Web.UI.GridNeedDataSourceEventArgs e)
    {
        DataTable dt = Admin.GetLogins();
        gridLogins.DataSource = dt;

        this.Session["Logins"] = dt;
        this.Logins.PrimaryKey = new DataColumn[] { this.Logins.Columns["LoginID"] };
        gridLogins.MasterTableView.EditFormSettings.EditFormType = GridEditFormType.WebUserControl;
    }

    protected void gridLogins_UpdateCommand(object source, GridCommandEventArgs e)
    {
        GridEditableItem editedItem = e.Item as GridEditableItem;
        UserControl userControl = (UserControl)e.Item.FindControl(GridEditFormItem.EditFormUserControlID);

        //Prepare new row to add it in the DataSource
        DataRow[] changedRows = this.Logins.Select("LoginID = '" + editedItem.OwnerTableView.DataKeyValues[editedItem.ItemIndex]["LoginID"] + "'");

        if (changedRows.Length != 1)
        {
            gridLogins.Controls.Add(new LiteralControl("Unable to locate the User for updating."));
            e.Canceled = true;
            return;
        }

        //Update new values
        Hashtable newValues = new Hashtable();

        newValues["PassWd"] = (userControl.FindControl("txtPassWd") as TextBox).Text;
        newValues["NumPrints"] = (userControl.FindControl("txtNumPrints") as TextBox).Text;
        newValues["TypeWhs"] = (userControl.FindControl("drpTypeWhs") as DropDownList).SelectedItem.Value;
        newValues["RoleID"] = (userControl.FindControl("drpRoles") as DropDownList).SelectedItem.Value;
        newValues["Active"] = (userControl.FindControl("chkActive") as CheckBox).Checked.ToString().ToLower();
        newValues["Active_Pdt"] = (userControl.FindControl("chkActivePdt") as CheckBox).Checked.ToString().ToLower();

        changedRows[0].BeginEdit();
        try
        {
            foreach (DictionaryEntry entry in newValues)
            {
                changedRows[0][(string)entry.Key] = entry.Value;
            }
            changedRows[0].EndEdit();

            if (Admin.UpdateLogin(changedRows[0]))
            {
                this.Logins.AcceptChanges();
                AddLabel("Login updated.");
            }
            else
            {
                changedRows[0].CancelEdit();
                AddLabel("Unable to update Login");
                e.Canceled = true;  
            }
        }
        catch (Exception ex)
        {
            changedRows[0].CancelEdit();
            AddLabel("Exception in function gridLogins_UpdateCommand. MESSSAGE: " + ex.Message);
            e.Canceled = true;
        }
    }

    protected void gridLogins_InsertCommand(object source, GridCommandEventArgs e)
    {
        UserControl userControl = (UserControl)e.Item.FindControl(GridEditFormItem.EditFormUserControlID);

        //Create new row in the DataSource
        DataRow newRow = this.Logins.NewRow();

        //Update new values
        Hashtable newValues = new Hashtable();

        newValues["LoginID"] = (userControl.FindControl("txtLoginID") as TextBox).Text;
        newValues["PassWd"] = (userControl.FindControl("txtPassWd") as TextBox).Text;
        newValues["UserID"] = (userControl.FindControl("drpUsers") as DropDownList).SelectedItem.Value;
        newValues["RoleID"] = (userControl.FindControl("drpRoles") as DropDownList).SelectedItem.Value;
        newValues["TypeWhs"] = (userControl.FindControl("drpTypeWhs") as DropDownList).SelectedItem.Value;
        newValues["NumPrints"] = (userControl.FindControl("txtNumPrints") as TextBox).Text;
        newValues["Active"] = (userControl.FindControl("chkActive") as CheckBox).Checked.ToString().ToLower();
        newValues["Active_Pdt"] = (userControl.FindControl("chkActivePdt") as CheckBox).Checked.ToString().ToLower();

        string LoginID = (userControl.FindControl("txtLoginID") as TextBox).Text;
        try
        {
            foreach (DictionaryEntry entry in newValues)
            {
                newRow[(string)entry.Key] = entry.Value;
            }

            if (LoginID != "")
            {
                // Add the new User record to the Users DataTable
                newRow["LoginID"] = LoginID;
                if (Admin.LoginExists(LoginID))
                {
                    AddLabel("ERROR!  Login ID '" + LoginID + "' already exists.  Please enter a different Login ID");
                    e.Canceled = true;
                    return;
                }
                else
                {
                    this.Logins.Rows.Add(newRow);
                    this.Logins.AcceptChanges();
                }
            }
            else
            {
                AddLabel("ERROR!  Unable to insert new login record.");
                e.Canceled = true;
                return;
            }

            // Insert the user into the DB
            if (Admin.InsertLogin(newRow))
            {
                AddLabel("Login created");
            }
        }
        catch (Exception ex)
        {
            AddLabel("Unable to insert new Login record. Reason: " + ex.Message);
            e.Canceled = true;
        }
    }

    private void AddLabel(string Message)
    {
        Label lblError = new Label();
        lblError.Text = Message;
        lblError.ForeColor = System.Drawing.Color.Red;
        lblError.Font.Size = System.Web.UI.WebControls.FontUnit.Large;
        gridLogins.Controls.Add(lblError);
    }

    private DataTable Logins
    {
        get
        {
            object obj = this.Session["Logins"];
            if ((!(obj == null)))
            {
                return ((DataTable)(obj));
            }
            DataTable myDataTable = new DataTable();
            myDataTable = Admin.GetLogins();
            this.Session["Logins"] = myDataTable;
            return myDataTable;
        }
    }

}