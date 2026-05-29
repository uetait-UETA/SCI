using System;
using System.Collections.Generic;
using System.Web;
using System.IO;
using System.Xml;
//using SAPbobsCOM;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;


/// <summary>
/// Summary description for SqlObjs
/// </summary>
public class SqlObjs
{
    protected SqlDb db;
    protected string sap_db;
    protected string whs_code;

    public SqlObjs()
    {
        db = new SqlDb();
        db.Connect();

        System.Web.HttpContext ctx = System.Web.HttpContext.Current;
        sap_db = (ctx != null && ctx.Session != null) ? ctx.Session["CompanyId"] as string : null;
        if (string.IsNullOrEmpty(sap_db)) sap_db = ConfigurationSettings.AppSettings.Get("smm_db");
        whs_code = ConfigurationSettings.AppSettings.Get("whs_code");
    }

    public DataTable GetCompanies()
    {
        db.Connect();
        DataTable dt = new DataTable();

        // SmmNumId  = numeric PK of SMM_COMPANIES, used as dropdown value
        // Companycode = SAP B1 database name (may be shared by multiple branches)
        // Branch    = BPLId that auto-sets session branch on login
        string sql = @"
            SELECT 0 AS SmmNumId, 'Selecciona' AS Companycode, 'Select Company' AS CompanyName, 0 AS Branch
            UNION
            SELECT CompanyId AS SmmNumId, Companycode, CompanyName, ISNULL(Branch, 0) AS Branch
                FROM [dbo].[SMM_COMPANIES]
            ORDER BY 1";

        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in GetCompanies: " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public DataTable GetBranches(string companyId)
    {
        db.Connect();
        DataTable dt = new DataTable();
        string sql = @"
            SELECT 0 AS BranchId, '' AS BranchCode, 'Select Branch' AS BranchName, 0 AS IsSellerBranch
            UNION ALL
            SELECT BranchId, BranchCode, BranchName, IsSellerBranch
            FROM dbo.SMM_BRANCHES
            WHERE CompanyId = @CompanyId AND Active = 1
            ORDER BY BranchId";
        try
        {
            SqlCommand cmd = new SqlCommand(sql, db.Conn);
            cmd.Parameters.AddWithValue("@CompanyId", companyId);
            db.adapter = new SqlDataAdapter(cmd);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Error in GetBranches: " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
        return dt;
    }


}

