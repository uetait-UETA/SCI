using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telerik.Web.UI;
using System.Collections;
using System.Drawing;

public partial class BodegaTiendaEdit : BasePage
{
    protected SqlDb db = new SqlDb();
    protected string appUserName;
    protected string lCurUser;
    protected string sap_db;

    protected void Page_Load(object sender, EventArgs e)
    {
        if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
        {
            Response.Redirect("../Login1.aspx");
        }

        appUserName = (string)Session["UserId"];
        sap_db = (string)Session["CompanyId"];
        lCurUser = appUserName;

        ///////////////Begin  Control de acceso por Roles

        string lControlName = "BodegaTiendaEdit.aspx";
        string strAccessType = "";
        string strRole_Description = "";

        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, lControlName, ref strAccessType, ref strRole_Description);
        db.Disconnect();

        if (strAccessType != "F" && strAccessType != "R")
        {
            string message = "User " + lCurUser + ", with Role " + strRole_Description + " does not have permissions to access this screen.";
            string url = string.Format("../Default.aspx");
            string script = "{ alert('" + message + "'); window.location = '" + url + "'; }";
            ScriptManager.RegisterStartupScript(this.Page, Page.GetType(), "alert", script, true);
            return;
        }

        if (strAccessType == "R")
        {
            labelForm.InnerText = "Bodega-Tienda (Read-Only Access)";
            gridPairs.MasterTableView.CommandItemSettings.ShowAddNewRecordButton = false;
            gridSchedule.MasterTableView.CommandItemSettings.ShowAddNewRecordButton = false;
        }

        if (strAccessType == "F")
        {
            labelForm.InnerText = "Bodega-Tienda (Full Access)";
        }

        ///////////////End  Control de acceso por Roles
    }

    // ── Grid 1: SMM_REA_BODEGA_TIENDAS ────────────────────────────────────────

    protected void gridPairs_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        try
        {
            DataTable dt = Admin.GetBodegaTiendaPairs(sap_db, sap_db);
            gridPairs.DataSource = dt;
            this.Session["BodegaTiendaPairs"] = dt;
            gridPairs.MasterTableView.EditFormSettings.EditFormType = GridEditFormType.WebUserControl;
        }
        catch (Exception ex)
        {
            AddLabel(gridPairs, "Error loading pairs: " + ex.Message);
        }
    }

    protected void gridPairs_UpdateCommand(object source, GridCommandEventArgs e)
    {
        GridEditableItem editedItem = e.Item as GridEditableItem;
        UserControl userControl = (UserControl)e.Item.FindControl(GridEditFormItem.EditFormUserControlID);

        string bodegaId = editedItem.OwnerTableView.DataKeyValues[editedItem.ItemIndex]["BodegaID"].ToString();
        string tiendaId = editedItem.OwnerTableView.DataKeyValues[editedItem.ItemIndex]["TiendaID"].ToString();

        DataRow[] changedRows = this.Pairs.Select("BodegaID = '" + bodegaId + "' AND TiendaID = '" + tiendaId + "'");
        if (changedRows.Length != 1)
        {
            AddLabel(gridPairs, "Unable to locate the pair for updating.");
            e.Canceled = true;
            return;
        }

        Hashtable newValues = new Hashtable();
        newValues["Priority"] = (userControl.FindControl("txtPriority") as TextBox).Text;
        newValues["isActive"] = (userControl.FindControl("chkActive") as CheckBox).Checked.ToString().ToLower();

        changedRows[0].BeginEdit();
        try
        {
            foreach (DictionaryEntry entry in newValues)
                changedRows[0][(string)entry.Key] = entry.Value;
            changedRows[0].EndEdit();

            if (Admin.UpdateBodegaTiendaPair(changedRows[0]))
            {
                this.Pairs.AcceptChanges();
                AddLabel(gridPairs, "Pair updated.");
            }
            else
            {
                changedRows[0].CancelEdit();
                AddLabel(gridPairs, "Unable to update pair.");
                e.Canceled = true;
            }
        }
        catch (Exception ex)
        {
            changedRows[0].CancelEdit();
            AddLabel(gridPairs, "Exception updating pair. MESSAGE: " + ex.Message);
            e.Canceled = true;
        }
    }

    protected void gridPairs_InsertCommand(object source, GridCommandEventArgs e)
    {
        UserControl userControl = (UserControl)e.Item.FindControl(GridEditFormItem.EditFormUserControlID);

        string bodegaId = (userControl.FindControl("drpBodega") as DropDownList).SelectedValue;
        string tiendaId = (userControl.FindControl("drpTienda") as DropDownList).SelectedValue;
        string priorityText = (userControl.FindControl("txtPriority") as TextBox).Text;
        bool active = (userControl.FindControl("chkActive") as CheckBox).Checked;

        if (string.IsNullOrEmpty(bodegaId) || string.IsNullOrEmpty(tiendaId))
        {
            AddLabel(gridPairs, "ERROR!  Please select both a Bodega and a Tienda.");
            e.Canceled = true;
            return;
        }

        int priority;
        if (!int.TryParse(priorityText, out priority) || priority < 1)
        {
            AddLabel(gridPairs, "ERROR!  Priority must be a positive number.");
            e.Canceled = true;
            return;
        }

        if (Admin.BodegaTiendaPairExists(sap_db, bodegaId, tiendaId))
        {
            AddLabel(gridPairs, "ERROR!  This Bodega/Tienda pair already exists. Edit the existing row instead.");
            e.Canceled = true;
            return;
        }

        DataRow newRow = this.Pairs.NewRow();
        newRow["CompanyID"] = sap_db;
        newRow["BodegaID"] = bodegaId;
        newRow["TiendaID"] = tiendaId;
        newRow["Priority"] = priority;
        newRow["isActive"] = active.ToString().ToLower();
        newRow["BodegaName"] = "";
        newRow["TiendaName"] = "";

        try
        {
            this.Pairs.Rows.Add(newRow);
            this.Pairs.AcceptChanges();

            if (Admin.InsertBodegaTiendaPair(newRow))
                AddLabel(gridPairs, "Pair created.");
        }
        catch (Exception ex)
        {
            AddLabel(gridPairs, "Unable to insert new pair. Reason: " + ex.Message);
            e.Canceled = true;
        }
    }

    protected void gridPairs_DeleteCommand(object source, GridCommandEventArgs e)
    {
        GridDataItem item = e.Item as GridDataItem;
        string bodegaId = item.OwnerTableView.DataKeyValues[item.ItemIndex]["BodegaID"].ToString();
        string tiendaId = item.OwnerTableView.DataKeyValues[item.ItemIndex]["TiendaID"].ToString();

        try
        {
            Admin.DeactivateBodegaTiendaPair(sap_db, bodegaId, tiendaId);
            AddLabel(gridPairs, "Pair deactivated (isActive=0) — not deleted.");
        }
        catch (Exception ex)
        {
            AddLabel(gridPairs, "Unable to deactivate pair. Reason: " + ex.Message);
            e.Canceled = true;
        }
    }

    private DataTable Pairs
    {
        get
        {
            object obj = this.Session["BodegaTiendaPairs"];
            if (obj != null) return (DataTable)obj;
            DataTable dt = Admin.GetBodegaTiendaPairs(sap_db, sap_db);
            this.Session["BodegaTiendaPairs"] = dt;
            return dt;
        }
    }

    // ── Grid 2: SMM_REA_BODEGA_TIENDAS_HRS ────────────────────────────────────

    protected void gridSchedule_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        try
        {
            DataTable dt = Admin.GetBodegaTiendaSchedule(sap_db, sap_db);
            gridSchedule.DataSource = dt;
            this.Session["BodegaTiendaSchedule"] = dt;
            gridSchedule.MasterTableView.EditFormSettings.EditFormType = GridEditFormType.WebUserControl;
        }
        catch (Exception ex)
        {
            AddLabel(gridSchedule, "Error loading schedule: " + ex.Message);
        }
    }

    protected void gridSchedule_UpdateCommand(object source, GridCommandEventArgs e)
    {
        GridEditableItem editedItem = e.Item as GridEditableItem;
        UserControl userControl = (UserControl)e.Item.FindControl(GridEditFormItem.EditFormUserControlID);

        string bodegaId = editedItem.OwnerTableView.DataKeyValues[editedItem.ItemIndex]["BodegaID"].ToString();
        string tiendaId = editedItem.OwnerTableView.DataKeyValues[editedItem.ItemIndex]["TiendaID"].ToString();

        DataRow[] changedRows = this.Schedule.Select("BodegaID = '" + bodegaId + "' AND TiendaID = '" + tiendaId + "'");
        if (changedRows.Length != 1)
        {
            AddLabel(gridSchedule, "Unable to locate the schedule row for updating.");
            e.Canceled = true;
            return;
        }

        Hashtable newValues = new Hashtable();
        newValues["FromHrs"] = (userControl.FindControl("timeFrom") as RadTimePicker).SelectedDate.HasValue
            ? (userControl.FindControl("timeFrom") as RadTimePicker).SelectedDate.Value.ToString("HH:mm") : "00:01";
        newValues["ToHrs"] = (userControl.FindControl("timeTo") as RadTimePicker).SelectedDate.HasValue
            ? (userControl.FindControl("timeTo") as RadTimePicker).SelectedDate.Value.ToString("HH:mm") : "23:59";
        newValues["isActive"] = (userControl.FindControl("chkActive") as CheckBox).Checked.ToString().ToLower();
        newValues["Days"] = BodegaTiendaHrsForm.BuildDaysString(userControl);

        changedRows[0].BeginEdit();
        try
        {
            foreach (DictionaryEntry entry in newValues)
                changedRows[0][(string)entry.Key] = entry.Value;
            changedRows[0].EndEdit();

            if (Admin.UpdateBodegaTiendaSchedule(changedRows[0]))
            {
                this.Schedule.AcceptChanges();
                AddLabel(gridSchedule, "Schedule updated.");
            }
            else
            {
                changedRows[0].CancelEdit();
                AddLabel(gridSchedule, "Unable to update schedule.");
                e.Canceled = true;
            }
        }
        catch (Exception ex)
        {
            changedRows[0].CancelEdit();
            AddLabel(gridSchedule, "Exception updating schedule. MESSAGE: " + ex.Message);
            e.Canceled = true;
        }
    }

    protected void gridSchedule_InsertCommand(object source, GridCommandEventArgs e)
    {
        UserControl userControl = (UserControl)e.Item.FindControl(GridEditFormItem.EditFormUserControlID);

        string pairKey = (userControl.FindControl("drpPair") as DropDownList).SelectedValue;
        if (string.IsNullOrEmpty(pairKey) || pairKey.IndexOf('|') < 0)
        {
            AddLabel(gridSchedule, "ERROR!  Please select a Bodega->Tienda pair.");
            e.Canceled = true;
            return;
        }

        string bodegaId = pairKey.Split('|')[0];
        string tiendaId = pairKey.Split('|')[1];

        if (Admin.BodegaTiendaScheduleExists(sap_db, bodegaId, tiendaId))
        {
            AddLabel(gridSchedule, "ERROR!  This pair already has a schedule row. Edit the existing row instead.");
            e.Canceled = true;
            return;
        }

        RadTimePicker timeFrom = userControl.FindControl("timeFrom") as RadTimePicker;
        RadTimePicker timeTo = userControl.FindControl("timeTo") as RadTimePicker;

        DataRow newRow = this.Schedule.NewRow();
        newRow["CompanyID"] = sap_db;
        newRow["BodegaID"] = bodegaId;
        newRow["TiendaID"] = tiendaId;
        newRow["FromHrs"] = timeFrom.SelectedDate.HasValue ? timeFrom.SelectedDate.Value.ToString("HH:mm") : "00:01";
        newRow["ToHrs"] = timeTo.SelectedDate.HasValue ? timeTo.SelectedDate.Value.ToString("HH:mm") : "23:59";
        newRow["isActive"] = (userControl.FindControl("chkActive") as CheckBox).Checked.ToString().ToLower();
        newRow["Days"] = BodegaTiendaHrsForm.BuildDaysString(userControl);
        newRow["BodegaName"] = "";
        newRow["TiendaName"] = "";

        try
        {
            this.Schedule.Rows.Add(newRow);
            this.Schedule.AcceptChanges();

            if (Admin.InsertBodegaTiendaSchedule(newRow))
                AddLabel(gridSchedule, "Schedule created.");
        }
        catch (Exception ex)
        {
            AddLabel(gridSchedule, "Unable to insert new schedule row. Reason: " + ex.Message);
            e.Canceled = true;
        }
    }

    protected void gridSchedule_DeleteCommand(object source, GridCommandEventArgs e)
    {
        GridDataItem item = e.Item as GridDataItem;
        string bodegaId = item.OwnerTableView.DataKeyValues[item.ItemIndex]["BodegaID"].ToString();
        string tiendaId = item.OwnerTableView.DataKeyValues[item.ItemIndex]["TiendaID"].ToString();

        try
        {
            Admin.DeactivateBodegaTiendaSchedule(sap_db, bodegaId, tiendaId);
            AddLabel(gridSchedule, "Schedule row deactivated (isActive=0) — not deleted.");
        }
        catch (Exception ex)
        {
            AddLabel(gridSchedule, "Unable to deactivate schedule row. Reason: " + ex.Message);
            e.Canceled = true;
        }
    }

    private DataTable Schedule
    {
        get
        {
            object obj = this.Session["BodegaTiendaSchedule"];
            if (obj != null) return (DataTable)obj;
            DataTable dt = Admin.GetBodegaTiendaSchedule(sap_db, sap_db);
            this.Session["BodegaTiendaSchedule"] = dt;
            return dt;
        }
    }

    // ── Shared ────────────────────────────────────────────────────────────────

    private void AddLabel(Telerik.Web.UI.RadGrid grid, string message)
    {
        Label lbl = new Label();
        lbl.Text = message;
        lbl.ForeColor = Color.Red;
        lbl.Font.Size = FontUnit.Large;
        grid.Controls.Add(lbl);
    }
}
