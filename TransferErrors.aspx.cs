using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Data.SqlClient;
using System.Drawing;


public partial class TransferErrors : BasePage
{
    protected SqlDb db = new SqlDb();
    protected string sap_db;
    protected string lCurUser;


    protected void Page_Load(object sender, EventArgs e)
    {

        if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
        {
            Response.Redirect("Login1.aspx");
        }

        sap_db = sap_db = (string)this.Session["CompanyId"];
        CompanyIdLabel.Text = sap_db;
                    
        ///////////////Begin New  Control de acceso por Roles
	    if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
	    {
            Response.Redirect("Login1.aspx");
	    }

        lCurUser = (string)this.Session["UserId"];
        char flagokay = 'Y';
		string lControlName = "TransferErrors.aspx";
        string strAccessType = "";
        string strRole_Description = "";

        db.Connect();
        db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, lControlName, ref strAccessType, ref strRole_Description);
        db.Disconnect();

        if (strAccessType == "N")
		{
		    flagokay = 'N';
		    string message = "User " + lCurUser + ", with Role " + strRole_Description + " does not have permissions to access this screen.";
		    string url = string.Format("Default.aspx");
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
			Button2.Enabled = false;
			Button2.ForeColor = Color.Silver;
            labelForm.InnerText = "Transfer Errors (Read-Only Access)";      
		}
			
		if (strAccessType == "F")
		{
			Button2.Enabled = true; 
			labelForm.InnerText = "Transfer Errors (Full Access)";           
		}	
			
		if (flagokay == 'Y')
        {
            ObjectDataSource1.SelectParameters["ShowAll"].DefaultValue = radioShowAll.Checked.ToString();
        }
        ///////////////End  New Control de acceso por Roles
    }

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        GridView1.DataBind();
    }
   
    protected void Button2_Click(object sender, EventArgs e)
    {
        int rg2count = GridView1.Rows.Count;
        string lblvalue = "";
        string lfixed = "";
        int  LvLinNum;
        int  DocEntryOri;

        db.Connect();

        //TextBox1.Text = "";

        for (int i = 0; i < rg2count; i++)
        {

            Label lblDe = (Label)GridView1.Rows[i].FindControl("DE");
            Label lblLn = (Label)GridView1.Rows[i].FindControl("Ln");

            DocEntryOri = Convert.ToInt32(lblDe.Text);
            LvLinNum = Convert.ToInt32(lblLn.Text);           

            RadioButtonList LMyList = (RadioButtonList)GridView1.Rows[i].FindControl("rblFixed");
            ListItem Litem = LMyList.Items.FindByText("Fixed");

            Label lblS = (Label)GridView1.Rows[i].FindControl("lblSelected");
            lblvalue = lblS.Text.ToString ();
            
            if (lblvalue == "0")
            {
                lblvalue = "False";
            }
            else
            {
                lblvalue = "True";
            }
            

            if (Litem != null)
            {
                
                if (Litem.Selected.ToString() == "True")
                {
                    lfixed = "Y";
                }
                else
                {
                    lfixed = "N";
                }

                //TextBox1.Text = TextBox1.Text + '/' + DocEntryOri.ToString()  + '-' + LvLinNum.ToString () + '-' + lfixed + '-' + lblvalue;//    .Value.ToString();

                if (Litem.Selected.ToString() != lblvalue)
                {                    
                    SqlCommand sqlCommand = new SqlCommand();
                    sqlCommand.Parameters.Clear();
                    sqlCommand.CommandText = "la_update_transfer_errors";
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Connection = db.Conn;

                    sqlCommand.Parameters.Add(new SqlParameter("@DocEntry", SqlDbType.Int));
                    sqlCommand.Parameters["@DocEntry"].Value = DocEntryOri;

                    sqlCommand.Parameters.Add(new SqlParameter("@Linenum", SqlDbType.SmallInt));
                    sqlCommand.Parameters["@Linenum"].Value = LvLinNum;

                    sqlCommand.Parameters.Add(new SqlParameter("@Fixed", SqlDbType.NChar));
                    sqlCommand.Parameters["@Fixed"].Value = lfixed;

                    try
                    {
                        sqlCommand.ExecuteNonQuery();
                    }

                    catch (Exception ex)
                    {
                        Response.Write("la_update_transfer_errors was called.");
                        Response.Write(ex.Message);
                    }
                }

            }
           

            lblvalue = "";

        }

              

        

        
        
    }
    protected void GridView1_DataBinding(object sender, EventArgs e)
    {
        /*if (e.Row.RowType == DataControlRowType.DataRow)
        {
            RadioButtonList rdoRowList = e.Row.FindControl("rblFixed") as RadioButtonList;
            //rdoRowList.SelectedIndex = ((ObjectDataSource)((GridView) sender).DataSource).d
        }*/

    }
    protected void GridView1_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            //find contro using
            RadioButtonList MyList = (RadioButtonList)e.Row.FindControl("rblFixed"); //control id which is in gridview
            Label lblSelected = (Label)e.Row.FindControl("lblSelected"); //control id which is in gridview
            if (lblSelected != null && MyList != null)
            {
                ListItem item = MyList.Items.FindByValue(lblSelected.Text);
                if (item != null)
                {
                    MyList.Items.FindByValue(lblSelected.Text).Selected = true;
                }
            }
        }

    }
}
