using System;

public partial class RoleEditForm : System.Web.UI.UserControl
{
    private object _dataItem;

    public object DataItem
    {
        get { return _dataItem; }
        set { _dataItem = value; }
    }
}
