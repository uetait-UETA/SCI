using System;
using System.Data;
using System.Web.UI;
using Telerik.Web.UI;
using System.Web.UI.WebControls;
using System.Collections.Generic;

public partial class BodegaTiendaHrsForm : System.Web.UI.UserControl
{
    private object _dataItem = null;

    private static readonly string[] DayCheckboxIds = { "chkMon", "chkTue", "chkWed", "chkThu", "chkFri", "chkSat", "chkSun" };
    private static readonly string[] DayNames = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

    protected void Page_Load(object sender, System.EventArgs e)
    {
        if (DataItem != null)
        {
            string sap_db = (string)Session["CompanyId"];

            drpPair.DataSource = Admin.GetActiveBodegaTiendaPairsForDropdown(sap_db, sap_db);
            drpPair.DataBind();

            if (DataItem.GetType() == typeof(DataRowView))
            {
                // Update: the Bodega->Tienda pair is the natural key — not editable.
                DataRowView row = (DataRowView)DataItem;

                string pairKey = row["BodegaID"].ToString() + "|" + row["TiendaID"].ToString();
                drpPair.Items.Insert(1, new ListItem(
                    row["BodegaName"].ToString() + "  ->  " + row["TiendaName"].ToString(), pairKey));
                foreach (ListItem li in drpPair.Items)
                    li.Selected = (li.Value == pairKey);
                drpPair.Enabled = false;

                if (!IsPostBack)
                {
                    chkActive.Checked = row["isActive"].ToString().ToLower() == "true";
                    SetDayCheckboxes(row["Days"].ToString());
                    SetTime(timeFrom, row["FromHrs"].ToString());
                    SetTime(timeTo, row["ToHrs"].ToString());
                }
            }
            else if (DataItem.GetType() == typeof(Telerik.Web.UI.GridInsertionObject))
            {
                // Insert
                chkActive.Checked = true;
                drpPair.Enabled = true;
                chkAllDays.Checked = true;
                SetTime(timeFrom, "00:01");
                SetTime(timeTo, "23:59");
            }
        }
    }

    private void SetDayCheckboxes(string days)
    {
        if (string.IsNullOrEmpty(days) || days.Trim().ToUpper() == "ALL")
        {
            chkAllDays.Checked = true;
            return;
        }

        chkAllDays.Checked = false;
        for (int i = 0; i < DayCheckboxIds.Length; i++)
        {
            CheckBox chk = FindControl(DayCheckboxIds[i]) as CheckBox;
            if (chk != null)
                chk.Checked = days.IndexOf(DayNames[i], StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    private void SetTime(RadTimePicker picker, string hhmm)
    {
        DateTime parsed;
        if (DateTime.TryParseExact(hhmm, "HH:mm", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out parsed))
        {
            picker.SelectedDate = parsed;
        }
    }

    // Reads the day checkboxes off a parent grid's edit-form UserControl and builds
    // the comma-separated Days string ("Monday,Wednesday" or "ALL"). Shared with
    // BodegaTiendaEdit.aspx.cs's Insert/Update handlers.
    public static string BuildDaysString(UserControl control)
    {
        CheckBox chkAll = control.FindControl("chkAllDays") as CheckBox;
        if (chkAll != null && chkAll.Checked)
            return "ALL";

        var selected = new List<string>();
        for (int i = 0; i < DayCheckboxIds.Length; i++)
        {
            CheckBox chk = control.FindControl(DayCheckboxIds[i]) as CheckBox;
            if (chk != null && chk.Checked)
                selected.Add(DayNames[i]);
        }

        return selected.Count > 0 ? string.Join(",", selected.ToArray()) : "ALL";
    }

    public object DataItem
    {
        get { return this._dataItem; }
        set { this._dataItem = value; }
    }
}
