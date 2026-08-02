using System;
using System.Data;
using System.Web.UI;
using Telerik.Web.UI;
using System.Web.UI.WebControls;

public partial class BodegaTiendaPairForm : System.Web.UI.UserControl
{
    private object _dataItem = null;

    protected void Page_Load(object sender, System.EventArgs e)
    {
        if (DataItem != null)
        {
            string sap_db = (string)Session["CompanyId"];

            DataTable warehouses = Admin.GetWarehouseList(sap_db);

            drpBodega.DataSource = warehouses;
            drpBodega.DataBind();

            drpTienda.DataSource = warehouses;
            drpTienda.DataBind();

            if (DataItem.GetType() == typeof(DataRowView))
            {
                // Update: BodegaID/TiendaID are the natural key — not editable.
                DataRowView row = (DataRowView)DataItem;

                if (!IsPostBack)
                    chkActive.Checked = row["isActive"].ToString().ToLower() == "true";

                foreach (ListItem li in drpBodega.Items)
                    li.Selected = (li.Value == row["BodegaID"].ToString());

                foreach (ListItem li in drpTienda.Items)
                    li.Selected = (li.Value == row["TiendaID"].ToString());

                drpBodega.Enabled = false;
                drpTienda.Enabled = false;
            }
            else if (DataItem.GetType() == typeof(Telerik.Web.UI.GridInsertionObject))
            {
                // Insert: user must pick both.
                chkActive.Checked = true;
                drpBodega.Enabled = true;
                drpTienda.Enabled = true;
                if (string.IsNullOrEmpty(txtPriority.Text)) txtPriority.Text = "1";
            }
        }
    }

    public object DataItem
    {
        get { return this._dataItem; }
        set { this._dataItem = value; }
    }
}
