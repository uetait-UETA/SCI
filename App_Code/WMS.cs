using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Data.SqlClient;


public class WMS
{
    protected SqlDb db;
    protected string sap_db;

    public WMS()
    {
        db = new SqlDb();
        db.Connect();
        sap_db = HttpContext.Current.Session["CompanyId"] != null
            ? HttpContext.Current.Session["CompanyId"].ToString()
            : "";
    }

    public DataTable GetItemBin(string store, string depts, string brands, string lidArticulo, string lidBinTBox)
    {
        DataTable dt = new DataTable();

        //sap_db = CompanyId;

        string sql = null;
        string orderBy = " ORDER BY itemcode DESC";

        if (!string.IsNullOrEmpty(lidBinTBox))
        {
            sql = @"SELECT whscode, whsname, itemcode, itemname, itmsgrpcod,
                       itmsgrpnam, u_brand, bin
	                   FROM " + Queries.With_WmsWhsItemBin(sap_db) + @"
                       WHERE WhsCode = '" + store + @"' AND
                             bin = ltrim(rtrim('" + lidBinTBox + @"'))";
        }
        else
        {
            if (!string.IsNullOrEmpty(lidArticulo))
            {
                sql = @"SELECT whscode, whsname, itemcode, itemname, itmsgrpcod,
                       itmsgrpnam, u_brand, bin
	                   FROM " + Queries.With_WmsWhsItemBin(sap_db) + @"
                       WHERE WhsCode = '" + store + @"' AND
                             itemcode = ltrim(rtrim('" + lidArticulo + @"'))";
            }
            else
            {
                if (depts == null || store == null || brands == null)
                    return dt;

                sql = @"SELECT whscode, whsname, itemcode, itemname, itmsgrpcod,
                       itmsgrpnam, u_brand, bin
	                   FROM " + Queries.With_WmsWhsItemBinFull(sap_db) + @"
                       WHERE WhsCode = '" + store + @"' AND
                             itmsgrpcod IN (" + depts + @")";

                if (brands != "'All Brands'")
                    sql += " AND replace(u_brand, '''','_') IN (" + brands + ")";

                orderBy = " ORDER BY CASE WHEN bin IS NULL THEN 1 ELSE 0 END, itemcode ASC";
            }
        }

        sql += orderBy;




        try
        {
            db.adapter = new SqlDataAdapter(sql, db.Conn);
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in function GetItemBin. MESSAGE : " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
        return dt;
    }
}

