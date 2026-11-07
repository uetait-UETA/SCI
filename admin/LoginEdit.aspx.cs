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
using System.Text;

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

        // Los desplegables de filtro se llenan una sola vez; en los postbacks
        // sus elementos y la seleccion se conservan por ViewState.
        if (!IsPostBack)
        {
            PoblarDesplegable(ddlFRole,    "Role");
            PoblarDesplegable(ddlFTypeWhs, "TypeWhs");
        }
    }

    protected void gridLogins_NeedDataSource(object sender, Telerik.Web.UI.GridNeedDataSourceEventArgs e)
    {
        DataTable dt = Admin.GetLogins();

        // IMPORTANTE: en la sesion se guarda la tabla COMPLETA, sin filtrar.
        // Los flujos de edicion e insercion la usan para localizar la fila
        // por LoginID; si aqui se guardara la tabla filtrada, editar con un
        // filtro puesto dejaria de encontrar el registro.
        this.Session["Logins"] = dt;
        this.Logins.PrimaryKey = new DataColumn[] { this.Logins.Columns["LoginID"] };

        // A la rejilla solo se le entregan las filas que pasan el filtro.
        DataTable filtrados = AplicarFiltro(dt);
        gridLogins.DataSource = filtrados;

        lblResultado.Text = (filtrados.Rows.Count == dt.Rows.Count)
            ? string.Format("{0} registros", dt.Rows.Count)
            : string.Format("{0} de {1} registros", filtrados.Rows.Count, dt.Rows.Count);

        gridLogins.MasterTableView.EditFormSettings.EditFormType = GridEditFormType.WebUserControl;
    }

    // ── Busqueda ────────────────────────────────────────────────────────────
    // No se usa el filtrado nativo de Telerik (AllowFilteringByColumn y
    // compania) porque altera el aspecto de la rejilla en esta aplicacion.
    // En su lugar se filtra el DataTable antes de asignarlo.

    private DataTable AplicarFiltro(DataTable dt)
    {
        List<string> condiciones = new List<string>();

        AgregarContiene(condiciones, "LoginID",      txtFLoginID.Text);
        AgregarContiene(condiciones, "UserFullName", txtFUserFullName.Text);

        // Role y WHS Type: valor exacto elegido en el desplegable
        if (!string.IsNullOrEmpty(ddlFRole.SelectedValue))
            condiciones.Add("ISNULL(Role,'') = '" + EscaparRowFilter(ddlFRole.SelectedValue) + "'");

        if (!string.IsNullOrEmpty(ddlFTypeWhs.SelectedValue))
            condiciones.Add("ISNULL(TypeWhs,'') = '" + EscaparRowFilter(ddlFTypeWhs.SelectedValue) + "'");

        // NumPrints es numerico: igualdad exacta, y solo si es un numero valido
        string numPrints = (txtFNumPrints.Text ?? "").Trim();
        if (numPrints != "")
        {
            int n;
            if (int.TryParse(numPrints, out n))
                condiciones.Add("NumPrints = " + n);
            else
                condiciones.Add("1 = 0");   // texto no numerico: no coincide con nada
        }

        // Active y Active_Pdt llegan como texto 'true'/'false' desde Admin.GetLogins()
        if (!string.IsNullOrEmpty(ddlFActive.SelectedValue))
            condiciones.Add("ISNULL(Active,'') = '" + ddlFActive.SelectedValue + "'");

        if (!string.IsNullOrEmpty(ddlFActivePdt.SelectedValue))
            condiciones.Add("ISNULL(Active_Pdt,'') = '" + ddlFActivePdt.SelectedValue + "'");

        if (condiciones.Count == 0) return dt;

        DataView vista = new DataView(dt);
        vista.RowFilter = string.Join(" AND ", condiciones.ToArray());
        return vista.ToTable();
    }

    private void AgregarContiene(List<string> condiciones, string columna, string valor)
    {
        valor = (valor ?? "").Trim();
        if (valor == "") return;
        condiciones.Add("ISNULL(" + columna + ",'') LIKE '%" + EscaparRowFilter(valor) + "%'");
    }

    /// <summary>
    /// Escapa los caracteres con significado especial en RowFilter de DataTable.
    /// Se recorre caracter a caracter para no reescapar lo ya escapado.
    /// </summary>
    private static string EscaparRowFilter(string valor)
    {
        StringBuilder sb = new StringBuilder(valor.Length + 8);
        foreach (char c in valor)
        {
            switch (c)
            {
                case '\'': sb.Append("''");  break;
                case '[':  sb.Append("[[]"); break;
                case ']':  sb.Append("[]]"); break;
                case '%':  sb.Append("[%]"); break;
                case '*':  sb.Append("[*]"); break;
                default:   sb.Append(c);     break;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Puebla un desplegable de filtro con los valores distintos que existen
    /// de verdad en una columna, para no cablear la lista. Solo se ofrecen
    /// opciones que devuelven al menos una fila.
    /// </summary>
    private void PoblarDesplegable(DropDownList ddl, string columna)
    {
        ddl.Items.Clear();
        ddl.Items.Add(new ListItem("(Todos)", ""));

        DataTable dt = this.Logins;
        if (dt == null || !dt.Columns.Contains(columna)) return;

        List<string> valores = new List<string>();
        foreach (DataRow row in dt.Rows)
        {
            string v = row[columna] == DBNull.Value ? "" : row[columna].ToString().Trim();
            if (v != "" && !valores.Contains(v)) valores.Add(v);
        }
        valores.Sort();
        foreach (string v in valores) ddl.Items.Add(new ListItem(v, v));
    }

    protected void btnBuscar_Click(object sender, EventArgs e)
    {
        gridLogins.CurrentPageIndex = 0;
        gridLogins.Rebind();
    }

    protected void btnLimpiar_Click(object sender, EventArgs e)
    {
        txtFLoginID.Text      = "";
        txtFUserFullName.Text = "";
        txtFNumPrints.Text    = "";
        ddlFRole.SelectedIndex      = 0;
        ddlFTypeWhs.SelectedIndex   = 0;
        ddlFActive.SelectedIndex    = 0;
        ddlFActivePdt.SelectedIndex = 0;

        gridLogins.CurrentPageIndex = 0;
        gridLogins.Rebind();
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

        // El campo de contrasena es TextMode="Password" y no recibe el valor
        // actual, asi que al editar llega vacio. Vacio significa "no cambiar":
        // no se anade a newValues, y la fila conserva la contrasena que ya
        // traia de la base de datos a traves de Session["Logins"].
        // SIN esta comprobacion, cualquier edicion de otro campo borraria la
        // contrasena del usuario y lo dejaria sin poder acceder.
        string nuevaPass = (userControl.FindControl("txtPassWd") as TextBox).Text;
        if (!string.IsNullOrEmpty(nuevaPass))
            newValues["PassWd"] = nuevaPass;

        newValues["NumPrints"] = (userControl.FindControl("txtNumPrints") as TextBox).Text;
        newValues["TypeWhs"] = (userControl.FindControl("drpTypeWhs") as DropDownList).SelectedItem.Value;
        newValues["RoleID"] = (userControl.FindControl("drpRoles") as DropDownList).SelectedItem.Value;
        newValues["Active"] = (userControl.FindControl("chkActive") as CheckBox).Checked.ToString().ToLower();
        newValues["Active_Pdt"] = (userControl.FindControl("chkActivePdt") as CheckBox).Checked.ToString().ToLower();
        newValues["AllowedCompanyId"] = (userControl.FindControl("drpAllowedCompany") as DropDownList).SelectedValue;

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

        // Al crear no hay contrasena previa que conservar, asi que es obligatoria.
        string nuevaPass = (userControl.FindControl("txtPassWd") as TextBox).Text;
        if (string.IsNullOrEmpty(nuevaPass))
        {
            AddLabel("ERROR!  La contrasena es obligatoria al crear una credencial.");
            e.Canceled = true;
            return;
        }

        newValues["LoginID"] = (userControl.FindControl("txtLoginID") as TextBox).Text;
        newValues["PassWd"] = nuevaPass;
        newValues["UserID"] = (userControl.FindControl("drpUsers") as DropDownList).SelectedItem.Value;
        newValues["RoleID"] = (userControl.FindControl("drpRoles") as DropDownList).SelectedItem.Value;
        newValues["TypeWhs"] = (userControl.FindControl("drpTypeWhs") as DropDownList).SelectedItem.Value;
        newValues["NumPrints"] = (userControl.FindControl("txtNumPrints") as TextBox).Text;
        newValues["Active"] = (userControl.FindControl("chkActive") as CheckBox).Checked.ToString().ToLower();
        newValues["Active_Pdt"] = (userControl.FindControl("chkActivePdt") as CheckBox).Checked.ToString().ToLower();
        newValues["AllowedCompanyId"] = (userControl.FindControl("drpAllowedCompany") as DropDownList).SelectedValue;

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