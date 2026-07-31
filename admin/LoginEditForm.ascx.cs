using System;
using System.Data;
using System.Collections;
using System.Web.UI;
using Telerik.Web.UI;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

public partial class LoginEditForm : System.Web.UI.UserControl
{

    private object _dataItem = null;

    protected void Page_Load(object sender, System.EventArgs e)
    {
        if (DataItem != null)
        {
            drpRoles.DataSource = Admin.GetRoleList();
            drpRoles.DataBind();

            drpUsers.DataSource = Admin.GetUserList();
            drpUsers.DataBind();

            // Load companies into allowed-company dropdown
            var companies = Admin.GetCompanyList();
            foreach (System.Data.DataRow cr in companies.Rows)
                drpAllowedCompany.Items.Add(new ListItem(cr["CompanyName"].ToString(), cr["Branch"].ToString()));

            if (DataItem.GetType() == typeof(DataRowView))
            {
                // This is an update
                chkActive.Checked = ((DataRowView)DataItem)["Active"].ToString() == "true" ? true : false;
                chkActivePdt.Checked = ((DataRowView)DataItem)["Active_Pdt"].ToString() == "true" ? true : false;
                txtLoginID.Enabled = false;

                foreach (ListItem li in drpTypeWhs.Items)
                {
                    if (li.Value == ((DataRowView)DataItem)["TypeWhs"].ToString())
                        li.Selected = true;
                }

                foreach (ListItem li in drpUsers.Items)
                {
                    li.Selected = false;
                    if (li.Value == ((DataRowView)DataItem)["UserID"].ToString())
                        li.Selected = true;
                }

                foreach (ListItem li in drpRoles.Items)
                {
                    li.Selected = false;
                    if (li.Value == ((DataRowView)DataItem)["RoleID"].ToString())
                        li.Selected = true;
                }

                string allowedVal = ((DataRowView)DataItem)["AllowedCompanyId"].ToString();
                foreach (ListItem li in drpAllowedCompany.Items)
                {
                    li.Selected = false;
                    if (li.Value == allowedVal)
                        li.Selected = true;
                }

                drpUsers.Enabled = false;
            }
            else if (DataItem.GetType() == typeof(Telerik.Web.UI.GridInsertionObject))
            {
                // This is an insert
                txtLoginID.Enabled = true;
                chkActive.Checked = true;
                chkActivePdt.Checked = true;
                drpUsers.Enabled = true;
                drpAllowedCompany.SelectedValue = "0";
            }
        }
    }

    public object DataItem
    {
        get
        {
            return this._dataItem;
        }
        set
        {
            this._dataItem = value;
        }
    }

}
