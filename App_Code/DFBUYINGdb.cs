using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Data.SqlClient;

/// <summary>
/// Summary description for SqlDb
/// </summary>
public class DFBUYINGdb
{
    protected string connStr;
    public SqlConnection Conn;
    public SqlCommand cmd;
    public SqlDataReader rdr;
    public SqlDataAdapter adapter;
    public SqlDataAdapter adapter2; 
    public System.Data.DataSet dataSet;

    public DFBUYINGdb()
    {
        //
        // TODO: Add constructor logic here
        //
    }

    public void Connect()
    {
        try
        {
            connStr = ConfigurationManager.ConnectionStrings["DFBUYING"].ConnectionString;

            this.Conn = new SqlConnection();
            this.cmd = new SqlCommand();
            this.adapter = new SqlDataAdapter();
            this.dataSet = new DataSet();

            this.Conn.ConnectionString = connStr;
            this.cmd.Connection = this.Conn;
            try
            {
                Conn.Open();
            }
            catch (SqlException ex)
            {

            }
        }
        catch (Exception ex)
        {
            HttpContext.Current.Response.Redirect("AccessDenied.aspx", true);
        }
    }

    public void Disconnect()
    {
        try
        {
            Conn.Close();
        }
        catch (Exception)
        {

        }
    }

}
