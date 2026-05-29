using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telerik.Web.UI;
using System.Collections;


public partial class UserEdit : BasePage
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

        appUserName = (string)this.Session["UserId"];

        ///////////////Begin New  Control de acceso por Roles

        lCurUser = appUserName;

        //flagokay = 'Y';

        string lControlName = "UserEdit.aspx";

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
    		labelForm.InnerText = "Users (Read-Only Access)";
    	}
    
    	if (strAccessType == "F")
    	{
    	    labelForm.InnerText = "Users (Full Access)";
    	}	

        ///////////////End  New Control de acceso por Roles

    }

    protected void gridUsers_NeedDataSource(object sender, Telerik.Web.UI.GridNeedDataSourceEventArgs e)
    {
        DataTable dt = Admin.GetUsers();
        gridUsers.DataSource = dt;

        this.Session["Users"] = dt;
        this.Users.PrimaryKey = new DataColumn[] { this.Users.Columns["UserID"] };
        gridUsers.MasterTableView.EditFormSettings.EditFormType = GridEditFormType.WebUserControl;
    }

    protected void gridUsers_UpdateCommand(object source, GridCommandEventArgs e)
    {
        GridEditableItem editedItem = e.Item as GridEditableItem;
        UserControl userControl = (UserControl)e.Item.FindControl(GridEditFormItem.EditFormUserControlID);

        //Prepare new row to add it in the DataSource
        DataRow[] changedRows = this.Users.Select("UserId = " + editedItem.OwnerTableView.DataKeyValues[editedItem.ItemIndex]["UserId"]);

        if (changedRows.Length != 1)
        {
            gridUsers.Controls.Add(new LiteralControl("Unable to locate the User for updating."));
            e.Canceled = true;
            return;
        }

        //Update new values
        Hashtable newValues = new Hashtable();

        newValues["FirstName"] = (userControl.FindControl("txtFirstName") as TextBox).Text;
        newValues["OtherNames"] = (userControl.FindControl("txtOtherNames") as TextBox).Text;
        newValues["LastName1"] = (userControl.FindControl("txtLastName1") as TextBox).Text;
        newValues["LastName2"] = (userControl.FindControl("txtLastName2") as TextBox).Text;
        newValues["AdditionalInfo"] = (userControl.FindControl("txtAdditionalInfo") as TextBox).Text;
        newValues["Active"] = (userControl.FindControl("chkActive") as CheckBox).Checked.ToString().ToLower();

        changedRows[0].BeginEdit();
        try
        {
            foreach (DictionaryEntry entry in newValues)
            {
                changedRows[0][(string)entry.Key] = entry.Value;
            }
            changedRows[0].EndEdit();
            this.Users.AcceptChanges();

            Admin.UpdateUser(changedRows[0]);
        }
        catch (Exception ex)
        {
            changedRows[0].CancelEdit();

            Label lblError = new Label
            {
                Text = "Unable to update User. Reason: " + ex.Message,
                ForeColor = System.Drawing.Color.Red
            };
            gridUsers.Controls.Add(lblError);

            e.Canceled = true;
        }
    }

    protected void gridUsers_InsertCommand(object source, GridCommandEventArgs e)
    {
        UserControl userControl = (UserControl)e.Item.FindControl(GridEditFormItem.EditFormUserControlID);

        //Create new row in the DataSource
        DataRow newRow = this.Users.NewRow();

        //Update new values
        Hashtable newValues = new Hashtable();

        newValues["FirstName"] = (userControl.FindControl("txtFirstName") as TextBox).Text;
        newValues["OtherNames"] = (userControl.FindControl("txtOtherNames") as TextBox).Text;
        newValues["LastName1"] = (userControl.FindControl("txtLastName1") as TextBox).Text;
        newValues["LastName2"] = (userControl.FindControl("txtLastName2") as TextBox).Text;
        newValues["AdditionalInfo"] = (userControl.FindControl("txtAdditionalInfo") as TextBox).Text;
        newValues["Active"] = (userControl.FindControl("chkActive") as CheckBox).Checked.ToString().ToLower();

        try
        {
            foreach (DictionaryEntry entry in newValues)
            {
                newRow[(string)entry.Key] = entry.Value;
            }

            // Insert the user into the DB
            int UserId = Admin.InsertUser(newRow);

            if (UserId > 0)
            {
                // Add the new User record to the Users DataTable
                newRow["UserId"] = UserId;
                this.Users.Rows.Add(newRow);
                this.Users.AcceptChanges();
            }
            else
            {
                Label lblError = new Label();
                lblError.Text = "ERROR!  Unable to insert new user record.";
                lblError.ForeColor = System.Drawing.Color.Red;
                gridUsers.Controls.Add(lblError);

                e.Canceled = true;
            }
        }
        catch (Exception ex)
        {
            Label lblError = new Label();
            lblError.Text = "Unable to insert new user record. Reason: " + ex.Message;
            lblError.ForeColor = System.Drawing.Color.Red;
            gridUsers.Controls.Add(lblError);

            e.Canceled = true;
        }
    }

    private DataTable Users
    {
        get
        {
            object obj = this.Session["Users"];
            if ((!(obj == null)))
            {
                return ((DataTable)(obj));
            }
            DataTable myDataTable = new DataTable();
            myDataTable = Admin.GetUsers();
            this.Session["Users"] = myDataTable;
            return myDataTable;
        }
    }

}