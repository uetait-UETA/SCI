using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telerik.Web.UI;

public partial class RoleEdit : BasePage
{
    protected SqlDb db = new SqlDb();
    protected string lCurUser;

    protected void Page_Load(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty((string)Session["UserId"]))
        {
            Response.Redirect("../Login1.aspx");
            return;
        }

        lCurUser = (string)Session["UserId"];

        string strAccessType = "", strRoleDescription = "";
        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, "RoleEdit.aspx", ref strAccessType, ref strRoleDescription);
        db.Disconnect();

        if (strAccessType != "F")
        {
            string message = "User " + lCurUser + ", with Role " + strRoleDescription + " does not have permissions to access this screen.";
            string script = "{ alert('" + message + "'); window.location = '../Default.aspx'; }";
            ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "alert", script, true);
            return;
        }

        if (!IsPostBack)
        {
            BindRolesDropDown();
        }
    }

    private void BindRolesDropDown()
    {
        string previouslySelected = ddlRoles.SelectedValue;
        ddlRoles.DataSource = Admin.GetRoles();
        ddlRoles.DataBind();
        if (!string.IsNullOrEmpty(previouslySelected) && ddlRoles.Items.FindByValue(previouslySelected) != null)
            ddlRoles.SelectedValue = previouslySelected;
    }

    protected void btnAddScreen_Click(object sender, EventArgs e)
    {
        string controlName = (txtNewControlName.Text ?? "").Trim();
        string controlDesc = (txtNewControlDesc.Text ?? "").Trim();

        if (string.IsNullOrEmpty(controlName) || string.IsNullOrEmpty(controlDesc))
        {
            lblAddScreenMsg.ForeColor = System.Drawing.Color.DarkRed;
            lblAddScreenMsg.Text = "Both the screen name and the description are required.";
            return;
        }

        string error = Admin.CreateControl(controlName, controlDesc, lCurUser);
        if (error != null)
        {
            lblAddScreenMsg.ForeColor = System.Drawing.Color.DarkRed;
            lblAddScreenMsg.Text = error;
            return;
        }

        lblAddScreenMsg.ForeColor = System.Drawing.Color.Green;
        lblAddScreenMsg.Text = "\"" + controlName + "\" was registered. It shows as No Access for every role until you grant it below.";
        txtNewControlName.Text = "";
        txtNewControlDesc.Text = "";
        gridPermissions.Rebind();
    }

    // ── Roles grid ───────────────────────────────────────────────────────────────

    protected void gridRoles_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        gridRoles.DataSource = Admin.GetRoles();
    }

    protected void gridRoles_UpdateCommand(object source, GridCommandEventArgs e)
    {
        UserControl userControl = (UserControl)e.Item.FindControl(GridEditFormItem.EditFormUserControlID);
        GridEditableItem editedItem = e.Item as GridEditableItem;
        string roleId = editedItem.OwnerTableView.DataKeyValues[editedItem.ItemIndex]["ROLEID"].ToString();

        string name = (userControl.FindControl("txtRoleName") as TextBox).Text.Trim();
        string description = (userControl.FindControl("txtRoleDescription") as TextBox).Text.Trim();

        try
        {
            Admin.UpdateRole(roleId, name, description);
        }
        catch (Exception ex)
        {
            lblRolesMsg.ForeColor = System.Drawing.Color.DarkRed;
            lblRolesMsg.Text = "Unable to update role. Reason: " + ex.Message;
            e.Canceled = true;
            return;
        }

        BindRolesDropDown();
        gridPermissions.Rebind();
    }

    protected void gridRoles_InsertCommand(object source, GridCommandEventArgs e)
    {
        UserControl userControl = (UserControl)e.Item.FindControl(GridEditFormItem.EditFormUserControlID);

        string name = (userControl.FindControl("txtRoleName") as TextBox).Text.Trim();
        string description = (userControl.FindControl("txtRoleDescription") as TextBox).Text.Trim();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description))
        {
            gridRoles.Controls.Add(new Label { Text = "Role Name and Description are both required.", ForeColor = System.Drawing.Color.Red });
            e.Canceled = true;
            return;
        }

        try
        {
            Admin.CreateRole(name, description);
        }
        catch (Exception ex)
        {
            lblRolesMsg.ForeColor = System.Drawing.Color.DarkRed;
            lblRolesMsg.Text = "Unable to create role. Reason: " + ex.Message;
            e.Canceled = true;
            return;
        }

        BindRolesDropDown();
    }

    protected void gridRoles_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (gridRoles.SelectedItems.Count == 0) return;
        GridDataItem item = gridRoles.SelectedItems[0] as GridDataItem;
        string roleId = item.GetDataKeyValue("ROLEID").ToString();

        if (ddlRoles.Items.FindByValue(roleId) != null)
        {
            ddlRoles.SelectedValue = roleId;
            gridPermissions.Rebind();
        }
    }

    // ── Screen Access grid ───────────────────────────────────────────────────────

    protected void ddlRoles_SelectedIndexChanged(object sender, EventArgs e)
    {
        gridPermissions.MasterTableView.ClearEditItems();
        gridPermissions.Rebind();
    }

    protected void gridPermissions_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        if (string.IsNullOrEmpty(ddlRoles.SelectedValue)) return;
        gridPermissions.DataSource = Admin.GetControlsForRole(ddlRoles.SelectedValue);
    }

    private static readonly Dictionary<string, string> _menuGroups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "ProductLocator.aspx",                     "Product Locator" },
        { "StoreMinMax3.aspx",                       "Store Min-Max" },
        { "MinMaxByExcel.aspx",                      "Store Min-Max" },
        { "StoreMinMax3Multiple.aspx",               "Store Min-Max" },
        { "DeliveryErrors.aspx",                     "ADR" },
        { "DeliveryErrorsItems.aspx",                "ADR" },
        { "TransferErrors.aspx",                     "ADR" },
        { "FacturacionSAP.aspx",                     "ADR" },
        { "InventoryAdjustByExcel.aspx",             "ADR" },
        { "InventoryAdjustByCategory.aspx",          "ADR" },
        { "InventoryAdjustByItems.aspx",             "ADR" },
        { "GoodsReceiptPO.aspx",                     "Receiving" },
        { "ComprasDirectas.aspx",                    "Receiving" },
        { "ComprasDirectasDetail.aspx",              "Receiving" },
        { "PreOrderManagement.aspx",                 "Receiving" },
        { "CreateTransfer.aspx",                     "Transfers" },
        { "CreateTransferXsap.aspx",                 "Transfers" },
        { "createTransferByExcel.aspx",              "Transfers" },
        { "TransferDelete.aspx",                     "Transfers" },
        { "Transfers.aspx",                          "Transfers" },
        { "Transfers_Audit.aspx",                    "Transfers" },
        { "Transfers_Audit_Item.aspx",               "Transfers" },
        { "FillPriority.aspx",                       "Fill Priority" },
        { "FillBrandPriority.aspx",                  "Fill Priority" },
        { "WhsItemBin.aspx",                         "Warehouses" },
        { "BinByExcel.aspx",                         "Warehouses" },
        { "StoreBines.aspx",                         "Warehouses" },
        { "PrintPriceLabel.aspx",                    "Warehouses" },
        { "sapInventory.aspx",                       "Reports" },
        { "R_Kardex.aspx",                           "Reports" },
        { "R_OrdenesAbiertas.aspx",                  "Reports" },
        { "R_OrdenesConProblemas.aspx",              "Reports" },
        { "R_BinesDesp.aspx",                        "Reports" },
        { "R_BinesDespTiendas.aspx",                 "Reports" },
        { "R_Research.aspx",                         "Reports" },
        { "UETAUpdateBarcodeSap.aspx",               "Barcodes" },
        { "UserEdit.aspx",                           "Admin" },
        { "LoginEdit.aspx",                          "Admin" },
        { "RoleEdit.aspx",                           "Admin" },
        { "BodegaTiendaEdit.aspx",                   "Admin" },
    };

    protected string MenuGroupFor(string controlName)
    {
        string group;
        if (_menuGroups.TryGetValue(controlName ?? "", out group))
            return group;
        return "Other";
    }

    protected void gridPermissions_ItemDataBound(object sender, GridItemEventArgs e)
    {
        GridEditableItem editedItem = e.Item as GridEditableItem;
        if (editedItem == null || !editedItem.IsInEditMode) return;

        RadioButton rbFull = editedItem.FindControl("rbFull") as RadioButton;
        RadioButton rbReadOnly = editedItem.FindControl("rbReadOnly") as RadioButton;
        RadioButton rbNoAccess = editedItem.FindControl("rbNoAccess") as RadioButton;
        if (rbFull == null || rbReadOnly == null || rbNoAccess == null) return;

        string currentAccessType = DataBinder.Eval(editedItem.DataItem, "AccessType").ToString();
        rbFull.Checked = (currentAccessType == "F");
        rbReadOnly.Checked = (currentAccessType == "R");
        rbNoAccess.Checked = (currentAccessType == "N");
    }

    protected void gridPermissions_UpdateCommand(object source, GridCommandEventArgs e)
    {
        GridEditableItem editedItem = e.Item as GridEditableItem;
        string controlName = editedItem.OwnerTableView.DataKeyValues[editedItem.ItemIndex]["ControlName"].ToString();

        RadioButton rbFull = editedItem.FindControl("rbFull") as RadioButton;
        RadioButton rbReadOnly = editedItem.FindControl("rbReadOnly") as RadioButton;
        RadioButton rbNoAccess = editedItem.FindControl("rbNoAccess") as RadioButton;
        if (rbFull == null || rbReadOnly == null || rbNoAccess == null)
        {
            lblPermissionsMsg.ForeColor = System.Drawing.Color.DarkRed;
            lblPermissionsMsg.Text = "Unable to update access. Reason: could not find the Access controls.";
            e.Canceled = true;
            return;
        }
        string newAccessType = rbFull.Checked ? "F" : (rbReadOnly.Checked ? "R" : "N");

        try
        {
            Admin.SetRoleControlAccess(ddlRoles.SelectedValue, controlName, newAccessType, lCurUser);
            lblPermissionsMsg.ForeColor = System.Drawing.Color.Green;
            lblPermissionsMsg.Text = "Saved.";
        }
        catch (Exception ex)
        {
            lblPermissionsMsg.ForeColor = System.Drawing.Color.DarkRed;
            lblPermissionsMsg.Text = "Unable to update access. Reason: " + ex.Message;
            e.Canceled = true;
        }
    }
}
