using System;
using System.Data;
using System.Web.UI.WebControls;
using System.Drawing;
using Telerik.Web.UI;

public partial class R_InventarioDeTiendasTocumen : BasePage
{
    public DataTable dt = new DataTable();
    public DataManager dm = new DataManager();
    protected string sap_db;
    SqlDb db = new SqlDb();

    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            ValidaSesionNullOrEmpty(new string[] { "UserId", "CompanyId" });

            sap_db = (string)Session["CompanyId"];

            if (!IsPostBack)
            {
                LoadGrups();
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed in Page_Load", ex.Message.ToString());
        }
    }

    private void GoToLogin()
    {
        Response.Redirect("Login1.aspx");
    }

    private void ValidaSesionNullOrEmpty(string[] keyNames)
    {
        bool r = false;
        foreach (string keyName in keyNames)
        {
            if(Session[keyName] == null || (string)Session[keyName] == "")
            {
                r = true;
                break;
            }
        }
        
        if (r)
        {
            GoToLogin();
        }
    }

    private bool ValidaSesionRptDataNull(string keyName, bool goToLogin)
    {
        bool r = false;
        if (Session[keyName] == null || (DataTable)Session[keyName] == null)
        {
            r = true;

            if (goToLogin)
            {
                GoToLogin();
            }
        }

        return r;
    }

    private void ShowMasterPageMessage(string v_Message_Type, string v_Message_Title, string v_Message)
    {
        try
        {
            SiteMaster sm = (SiteMaster)this.Master;
            sm.ShowDivMessage(v_Message_Type, v_Message_Title, v_Message);
        }
        catch (Exception ex)
        {
            throw new Exception("Error. Failed in ShowMasterPageMessage - ERROR: " + ex.Message);
        }
    }
    
    private void LoadGrups()
    {
        try
        {
            DataTable dtGroupo = db.GetGrupo(sap_db);

            rcbGrupo.DataSource = dtGroupo;
            rcbGrupo.DataValueField = "ItmsGrpCod";
            rcbGrupo.DataTextField = "ItmsGrpNam";
            rcbGrupo.DataBind();
        }
        catch (Exception)
        {
            throw;
        }
    }

    private void LoadBrands()
    {
        try
        {
            DataTable dtMarca = db.GetMarcas(AnyPourpuse.GetSelectedItems(rcbGrupo), sap_db);

            rcbMarca.DataSource = dtMarca;
            rcbMarca.DataValueField = "u_Brand";
            rcbMarca.DataTextField = "u_Brand";
            rcbMarca.DataBind();
        }
        catch (Exception)
        {
            throw;
        }
    }

    private DataTable GetInventarioDeTiendasTocumen(string marca, string grupos, string sap_db, string whsType, int branchId)
    {
        try
        {
            dt = db.GetInventarioDeTiendasTocumen(marca, grupos, sap_db, whsType, branchId);
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
        }
        return dt;
    }

    protected void rbtnView_Click(object sender, EventArgs e)
    {
        if (rcbGrupo.CheckedItems.Count > 0 && rcbMarca.SelectedValue != "")
        {
            Session["dtIDTT"] = null;
            rpgHead.Rebind();
        }
        else
        {
            ShowMasterPageMessage("Error", "Failed to Get data", "Select Grupo & Marca");
        }
    }

    protected void rcbGrupo_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        try
        {
            Session["dtIDTT"] = null;
            rpgHead.Visible = false;
            divHeading.Visible = false;
            rbtnExport.Visible = false;

            LoadBrands();
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed in rcbGrupo_SelectedIndexChanged", ex.Message);
        }
    }

    protected void rcbMarca_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        Session["dtIDTT"] = null;
        rpgHead.Visible = false;
        divHeading.Visible = false;
        rbtnExport.Visible = false;
    }

    protected void rpgHead_NeedDataSource(object sender, PivotGridNeedDataSourceEventArgs e)
    {
        ValidaSesionNullOrEmpty(new string[] { "UserId", "CompanyId" });
        try
        {
            DataTable dtSource = new DataTable();
  
            if (rcbGrupo.CheckedItems.Count > 0 && rcbMarca.SelectedValue != "")
            {
                if (ValidaSesionRptDataNull("dtIDTT", false))
                {
                    string grupos = AnyPourpuse.GetSelectedItems(rcbGrupo);
                    string whsTypes = "TIENDA,BODEGA";
                    int branchId = 0;
                    int.TryParse(Session["BranchId"] as string, out branchId);
                    Session["dtIDTT"] = GetInventarioDeTiendasTocumen(rcbMarca.SelectedValue, grupos, sap_db, whsTypes, branchId);
                }

                dtSource = (DataTable)Session["dtIDTT"];

                if (dtSource.Columns[0].ColumnName.ToString() == "ErrMsg")
                {
                    ShowMasterPageMessage("Error", "Failed to load data", dt.Rows[0]["ErrMsg"].ToString());
                }
                else
                {
                    rpgHead.DataSource = dtSource;
                    divHeading.Visible = true;
                    rpgHead.Visible = true;
                    rbtnExport.Visible = true;
                }
            }
            else
            {
                rpgHead.DataSource = null;
                divHeading.Visible = false;
                rpgHead.Visible = false;
                rbtnExport.Visible = false;
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message);
        }
    }

    protected void rbtnExport_Click(object sender, EventArgs e)
    {
        rpgHead.ExportToExcel();
    }
    protected void rpgHead_PivotGridCellExporting(object sender, PivotGridCellExportingArgs e)
    {
        PivotGridBaseModelCell modelDataCell = e.PivotGridModelCell as PivotGridBaseModelCell;
        if (modelDataCell != null)
        {
            AddStylesToDataCells(modelDataCell, e);
        }

        if (modelDataCell.TableCellType == PivotGridTableCellType.RowHeaderCell)
        {
            AddStylesToRowHeaderCells(modelDataCell, e);
        }

        if (modelDataCell.TableCellType == PivotGridTableCellType.ColumnHeaderCell)
        {
            AddStylesToColumnHeaderCells(modelDataCell, e);
        }

        if (modelDataCell.IsGrandTotalCell)
        {
            e.ExportedCell.Style.BackColor = Color.FromArgb(128, 128, 128);
            e.ExportedCell.Style.Font.Bold = true;
        }

        if (IsTotalDataCell(modelDataCell))
        {
            e.ExportedCell.Style.BackColor = Color.FromArgb(150, 150, 150);
            e.ExportedCell.Style.Font.Bold = true;
            AddBorders(e);
        }

        if (IsGrandTotalDataCell(modelDataCell))
        {
            e.ExportedCell.Style.BackColor = Color.FromArgb(128, 128, 128);
            e.ExportedCell.Style.Font.Bold = true;
            AddBorders(e);
        }
    }

    private void AddStylesToDataCells(PivotGridBaseModelCell modelDataCell, PivotGridCellExportingArgs e)
    {
        if (modelDataCell.Data != null && modelDataCell.Data.GetType() == typeof(decimal))
        {
            decimal value = Convert.ToDecimal(modelDataCell.Data);
            if (value > 100000)
            {
                e.ExportedCell.Style.BackColor = Color.FromArgb(51, 204, 204);
                AddBorders(e);
            }
        }
    }

    private void AddStylesToColumnHeaderCells(PivotGridBaseModelCell modelDataCell, PivotGridCellExportingArgs e)
    {
        if (e.ExportedCell.Table.Columns[e.ExportedCell.ColIndex].Width == 0)
        {
            e.ExportedCell.Table.Columns[e.ExportedCell.ColIndex].Width = 200D;
        }

        if (modelDataCell.IsTotalCell)
        {
            e.ExportedCell.Style.BackColor = Color.FromArgb(150, 150, 150);
            e.ExportedCell.Style.Font.Bold = true;
        }
        else
        {
            e.ExportedCell.Style.BackColor = Color.FromArgb(192, 192, 192);
        }
        AddBorders(e);
    }

    private void AddStylesToRowHeaderCells(PivotGridBaseModelCell modelDataCell, PivotGridCellExportingArgs e)
    {
        if (e.ExportedCell.Table.Columns[e.ExportedCell.ColIndex].Width == 0)
        {
            e.ExportedCell.Table.Columns[e.ExportedCell.ColIndex].Width = 80D;
        }
        if (modelDataCell.IsTotalCell)
        {
            e.ExportedCell.Style.BackColor = Color.FromArgb(150, 150, 150);
            e.ExportedCell.Style.Font.Bold = true;
        }
        else
        {
            e.ExportedCell.Style.BackColor = Color.FromArgb(192, 192, 192);
            e.ExportedCell.Table.Columns[5].Width = 450D;
            if (e.ExportedCell.ColIndex == 4)
            {
                e.ExportedCell.Format = "mso-number-format:\\@";
            }
        }

        AddBorders(e);
    }

    private static void AddBorders(PivotGridCellExportingArgs e)
    {
        e.ExportedCell.Style.BorderBottomColor = Color.FromArgb(128, 128, 128);
        e.ExportedCell.Style.BorderBottomWidth = new Unit(1);
        e.ExportedCell.Style.BorderBottomStyle = BorderStyle.Solid;

        e.ExportedCell.Style.BorderRightColor = Color.FromArgb(128, 128, 128);
        e.ExportedCell.Style.BorderRightWidth = new Unit(1);
        e.ExportedCell.Style.BorderRightStyle = BorderStyle.Solid;

        e.ExportedCell.Style.BorderLeftColor = Color.FromArgb(128, 128, 128);
        e.ExportedCell.Style.BorderLeftWidth = new Unit(1);
        e.ExportedCell.Style.BorderLeftStyle = BorderStyle.Solid;

        e.ExportedCell.Style.BorderTopColor = Color.FromArgb(128, 128, 128);
        e.ExportedCell.Style.BorderTopWidth = new Unit(1);
        e.ExportedCell.Style.BorderTopStyle = BorderStyle.Solid;
    }

    private bool IsTotalDataCell(PivotGridBaseModelCell modelDataCell)
    {
        return modelDataCell.TableCellType == PivotGridTableCellType.DataCell &&
        (modelDataCell.CellType == PivotGridDataCellType.ColumnTotalDataCell ||
        modelDataCell.CellType == PivotGridDataCellType.RowTotalDataCell ||
        modelDataCell.CellType == PivotGridDataCellType.RowAndColumnTotal);
    }

    private bool IsGrandTotalDataCell(PivotGridBaseModelCell modelDataCell)
    {
        return modelDataCell.TableCellType == PivotGridTableCellType.DataCell &&
        (modelDataCell.CellType == PivotGridDataCellType.ColumnGrandTotalDataCell ||
        modelDataCell.CellType == PivotGridDataCellType.ColumnGrandTotalRowTotal ||
        modelDataCell.CellType == PivotGridDataCellType.RowGrandTotalColumnTotal ||
        modelDataCell.CellType == PivotGridDataCellType.RowGrandTotalDataCell ||
        modelDataCell.CellType == PivotGridDataCellType.RowAndColumnGrandTotal);
    }

    protected void rpgHead_CellDataBound(object sender, PivotGridCellDataBoundEventArgs e)
    {
        if (e.Cell is PivotGridColumnHeaderCell)
        {
            int index = e.Cell.Text.IndexOf("Sum of");
            if (index >= 0)
            {
                e.Cell.Text = e.Cell.Text.Replace("Sum of", "");
            }
        }
    }

    protected void rpgHead_PivotGridExporting(object sender, PivotGridExportingArgs e)
    {
        
    }
}