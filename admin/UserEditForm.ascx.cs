using System;
using System.Data;
using System.Collections;
using System.Web.UI;
using Telerik.Web.UI;

public partial class UserEditForm : System.Web.UI.UserControl
{

    private object _dataItem = null;

    protected void Page_Load(object sender, System.EventArgs e)
    {
        if (DataItem != null)
        {
            if (DataItem.GetType() == typeof(DataRowView))
            {
                chkActive.Checked = ((DataRowView)DataItem)["Active"].ToString() == "true" ? true : false;
            }
            else if (DataItem.GetType() == typeof(Telerik.Web.UI.GridInsertionObject))
            {
                chkActive.Checked = true;
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
