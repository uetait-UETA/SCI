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

/// <summary>
/// Summary description for SqlDb
/// </summary>
public class SqlDb
{
    protected string connStr;
    public SqlConnection Conn;
    public SqlCommand cmd;
    public SqlDataReader rdr;
    public SqlDataAdapter adapter;
    public SqlDataAdapter adapter2; 
    public System.Data.DataSet dataSet;

    //2019-ABR-09: Añadido por Aldo Reina para poder verificar el estado de la base de datos:
    public ConnectionState DbConnectionState
    {
        get
        {
            if (Conn != null)
            {
                return Conn.State;
            }
            else
            {
                return ConnectionState.Closed;
            }
        }
    }

    public void SISINV_GET_ACCESSTYPE_PRC(string lCurUser, string lControlName,
        ref string strAccessType, ref string strRole_Description)
    {
        AccessRepository repo = new AccessRepository(this);
        string at, rd;
        repo.GetAccessType(lCurUser, lControlName, out at, out rd);
        strAccessType       = at;
        strRole_Description = rd;
    }

    public SqlDb()
    {
        //
        // TODO: Add constructor logic here
        //
    }

    public void Connect()
    {
        try
        {
            connStr = ConfigurationManager.ConnectionStrings["smm_latConnectionString"].ConnectionString;

            this.Conn = new SqlConnection();
            this.cmd = new SqlCommand();
            this.adapter = new SqlDataAdapter();
            this.dataSet = new DataSet();

            this.Conn.ConnectionString = connStr;
            this.cmd.Connection = this.Conn;

            if (DbConnectionState == ConnectionState.Closed)
            {
                Conn.Open();
            }
        }
        catch (Exception)
        {
            HttpContext.Current.Response.Redirect("AccessDenied.aspx", true);
        }
    }

    //2019-ABR-09: Añadido por Aldo Reina para cerrar la conexión a la base de datos:
    public void Disconnect()
    {
        try
        {
            if (Conn != null)
                Conn.Close();
        }
        catch (Exception)
        {

        }
    }



    //2019-ABR-09: Añadido por Aldo Reina, para la búsqueda por código de barras. Se colocó esta función acá 
    //porque será utilizada en varios lugares, y es mejor escribirla una vez que varias veces:
    public DataTable SearchItemByBarCodes(string companyId, string barCode)
    {
        DataTable dt = new DataTable();
        try
        {
            string queryString = "";
            queryString = queryString + "SELECT ";
            queryString = queryString + "ItemCode = b.ItemCode, ";
            queryString = queryString + "ItemName = CASE WHEN b.U_Type = 'Duty Free' THEN 'DF | ' WHEN b.U_Type = 'Duty Paid' THEN 'DP | ' ELSE '' END + LTRIM(RTRIM(b.ItemCode)) + ' | ' + LTRIM(RTRIM(b.itemname)) ";
            queryString = queryString + "FROM {0}..OBCD a " + Queries.WITH_NOLOCK + " ";
            queryString = queryString + "INNER JOIN {0}..OITM b " + Queries.WITH_NOLOCK + " on b.ItemCode = a.ItemCode ";
            queryString = queryString + "WHERE a.BcdCode = '{1}'";
            queryString = String.Format(queryString, companyId, barCode);

            adapter = new SqlDataAdapter(queryString, Conn);
            adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            Disconnect();
            throw new Exception("Error when SearchItemByBarCodes was called: " + ex.Message);
        }
        return dt;
    }

    public DataTable SearchBines(string companyId, string Bin)
    {
        DataTable dt = new DataTable();
        try
        {
            string queryString = "";
            queryString = queryString + "SELECT ";
            queryString = queryString + "ItemCode = b.ItemCode, ";
            queryString = queryString + "ItemName = LTRIM(RTRIM(b.ItemCode)) + ' | ' + LTRIM(RTRIM(b.itemname)) ";
            queryString = queryString + "FROM {0}..OBCD a " + Queries.WITH_NOLOCK + " ";
            queryString = queryString + "INNER JOIN {0}..OITM b " + Queries.WITH_NOLOCK + " on b.ItemCode = a.ItemCode ";
            queryString = queryString + "WHERE a.BcdCode = '{1}'";
            queryString = String.Format(queryString, companyId, Bin);

            adapter = new SqlDataAdapter(queryString, Conn);
            adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            Disconnect();
            throw new Exception("Error when SearchItemByBarCodes was called: " + ex.Message);
        }
        return dt;
    }

    public DataTable SearchItems(string companyId, string Item)
    {
        DataTable dt = new DataTable();
        try
        {
            string queryString = "";
            queryString = queryString + "SELECT ";
            queryString = queryString + "ItemCode = b.ItemCode, ";
            queryString = queryString + "ItemName = LTRIM(RTRIM(b.ItemCode)) + ' | ' + LTRIM(RTRIM(b.itemname)) ";
            queryString = queryString + "FROM {0}..OBCD a " + Queries.WITH_NOLOCK + " ";
            queryString = queryString + "INNER JOIN {0}..OITM b " + Queries.WITH_NOLOCK + " on b.ItemCode = a.ItemCode ";
            queryString = queryString + "WHERE a.BcdCode = '{1}'";
            queryString = String.Format(queryString, companyId, Item);

            adapter = new SqlDataAdapter(queryString, Conn);
            adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            Disconnect();
            throw new Exception("Error when SearchItemByBarCodes was called: " + ex.Message);
        }
        return dt;
    }

    public DataTable GetWhsByCiaIdAndControl(string sap_db, string control, string whsTypes, int bplId = 0, string uStore = "")
    {
        DataTable dt = new DataTable();
        try
        {
            string queryString = "SELECT a.WhsCode AS WhsCode, LTRIM(RTRIM(a.WhsName)) AS WhsName ";
            queryString += " FROM {0}..OWHS a " + Queries.WITH_NOLOCK + " ";
            queryString += " INNER JOIN dbo.RSS_OWHS_CONTROL b " + Queries.WITH_NOLOCK + " ";
            queryString += " ON a.WhsCode = b.Whscode ";
            queryString += " INNER JOIN dbo.SMM_WHSTYPE c " + Queries.WITH_NOLOCK + " ";
            queryString += " ON a.WhsCode = c.WHSCODE ";
            queryString += " WHERE 1=1 ";
            queryString += " AND b.CompanyId = '{0}'";
            queryString += " AND c.COMPANYID = '{0}'";

            if (!string.IsNullOrEmpty(control))
            {
                queryString += " AND b.[Control] = '" + control + "'";
            }

            if (!string.IsNullOrEmpty(whsTypes))
            {
                string whsTypes2 = "";

                string[] whsTypesArray = whsTypes.Split(",".ToCharArray());

                foreach (string item in whsTypesArray)
                {
                    whsTypes2 += ",'" + item + "'";
                }

                if(!string.IsNullOrEmpty(whsTypes2))
                {
                    whsTypes2 = whsTypes2.Substring(1);
                }
                queryString += " AND c.TYPEWHS IN (" + whsTypes2 + ")";
            }

            if (bplId > 1)
                queryString += " AND a.BPLId IN (1," + bplId + ")";
            else if (bplId > 0)
                queryString += " AND a.BPLId = " + bplId;

            if (!string.IsNullOrEmpty(uStore))
            {
                queryString += " AND a.U_Store = '" + uStore + "'";
            }

            queryString += " ORDER BY a.WhsCode ";

            queryString = String.Format(queryString, sap_db);

            adapter = new SqlDataAdapter(queryString, Conn);
            adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            Disconnect();
            throw new Exception("Error when GetWhsByCiaIdAndControl was called: " + ex.Message);
        }
        return dt;
    }

    public DataTable GetWhsByUStoreAndBplId(string sap_db, string uStore, int bplId)
    {
        DataTable dt = new DataTable();
        try
        {
            string queryString = "SELECT WhsCode, LTRIM(RTRIM(WhsName)) AS WhsName, ISNULL(U_Type, '') AS U_Type";
            queryString += " FROM " + sap_db + "..OWHS " + Queries.WITH_NOLOCK;
            queryString += " WHERE 1=1";
            if (!string.IsNullOrEmpty(uStore))
                queryString += " AND U_Store = '" + uStore + "'";
            if (bplId > 0)
                queryString += " AND BPLId = " + bplId;
            queryString += " ORDER BY WhsCode";

            adapter = new SqlDataAdapter(queryString, Conn);
            adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            Disconnect();
            throw new Exception("Error when GetWhsByUStoreAndBplId was called: " + ex.Message);
        }
        return dt;
    }

    public string GetWhsUType(string sap_db, string whsCode)
    {
        try
        {
            cmd.CommandText = "SELECT ISNULL(U_Type, '') FROM " + sap_db + "..OWHS " + Queries.WITH_NOLOCK + " WHERE WhsCode = '" + whsCode + "'";
            cmd.CommandType = System.Data.CommandType.Text;
            object result = cmd.ExecuteScalar();
            return result != null ? result.ToString() : "";
        }
        catch
        {
            return "";
        }
    }

    public DataTable GetInventarioDeTiendasTocumen(string v_Marca, string v_Grupo, string CompanyId, string whsType = "TODAS", int branchId = 0)
    {
        DataTable dt = new DataTable();
        try
        {
            Connect();

            // Step 1: companies configured for this login
            cmd.CommandText = @"SELECT Id, LTRIM(RTRIM(CompanyId)) AS CompanyId, SortNumber,
                                        LTRIM(RTRIM(ISNULL(LinkServer,''))) AS LinkServer
                                 FROM SMM_COMPANIES_FOR_INVENTORY WITH(NOLOCK)
                                 WHERE CompanyIdLogin = @Login";
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Clear();
            cmd.Parameters.Add(new SqlParameter("@Login", SqlDbType.NVarChar, 30)).Value = CompanyId;

            var companies = new System.Collections.Generic.List<object[]>();
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                    companies.Add(new object[] { r.GetInt32(0), r.GetString(1), r.GetInt32(2), r.GetString(3) });
            }
            if (companies.Count == 0) return dt;

            // BPLId filter: always include branch 1 (default) + the selected branch
            string bplClause = branchId > 1
                ? "AND w.BPLId IN (1," + branchId + ")"
                : "AND w.BPLId = 1";

            string whsTypeClause = "";
            if (whsType != "TODAS")
            {
                var types = whsType.Split(',');
                var quoted = System.Array.ConvertAll(types, t => "'" + t.Trim().Replace("'", "''") + "'");
                whsTypeClause = "AND w.WhsType IN (" + string.Join(",", quoted) + ")";
            }

            // Step 2: per-company inventory SELECTs
            var innerSelects = new System.Collections.Generic.List<string>();
            var ciaCodesIN   = new System.Collections.Generic.List<string>();

            foreach (object[] co in companies)
            {
                int    ciaId   = (int)co[0];
                string ciaCode = (string)co[1];
                int    sortNum = (int)co[2];
                string lnkSrv  = (string)co[3];

                string db1 = (lnkSrv == "" || lnkSrv == "-")
                    ? "[" + ciaCode + "]"
                    : "[" + lnkSrv + "].[" + ciaCode + "]";

                cmd.CommandText = string.Format(
                    "SELECT w.WhsCode FROM SMM_COMPANIES_FOR_INVENTORY_WHS w WITH(NOLOCK)" +
                    " WHERE w.SmmCompaniesForInventory_Id=@CiaId {0} {1}", bplClause, whsTypeClause);
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Clear();
                cmd.Parameters.Add(new SqlParameter("@CiaId", SqlDbType.Int)).Value = ciaId;

                var whs = new System.Collections.Generic.List<string>();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) whs.Add("'" + r.GetString(0).Replace("'", "''") + "'");
                }
                if (whs.Count == 0) continue;

                string whsIN    = string.Join(",", whs);
                string grpSafe  = v_Grupo.Replace("'", "''");
                string mrcFilter = v_Marca != "TODAS"
                    ? "AND b.U_BRAND IN (SELECT splitdata FROM dbo.fn_SplitString('" + v_Marca.Replace("'", "''") + "',','))"
                    : "";

                innerSelects.Add(string.Format(@"
SELECT '[{0}] {1}' cia,c.ItmsGrpNam,a.ItemCode,
    STUFF((SELECT ' - '+RIGHT(BcdCode,5) FROM {2}.dbo.OBCD a1 WITH(NOLOCK)
           WHERE a1.ItemCode=a.ItemCode FOR XML PATH('')),1,3,'') BarCode,
    b.ItemName,b.u_class,b.U_BRAND,
    SUM(a.OnHand) OnHand,SUM(a.OnOrder) OnOrder,SUM(a.IsCommited) IsCommited,
    a.WhsCode,{0} AS CompanySortNumber
FROM {2}.[dbo].[OITW] a WITH(NOLOCK)
INNER JOIN {2}.[dbo].[OITM] b WITH(NOLOCK) ON b.itemcode=a.itemcode
INNER JOIN {2}.[dbo].[OITB] c WITH(NOLOCK) ON c.ItmsGrpCod=b.ItmsGrpCod
WHERE a.WhsCode IN ({3})
  AND (a.OnHand+a.OnOrder+a.IsCommited)>0
  AND c.ItmsGrpCod IN (SELECT [Param] FROM dbo.fn_MVParam('{4}',','))
  {5}
GROUP BY c.ItmsGrpNam,a.ItemCode,b.ItemName,b.u_class,b.U_BRAND,a.WhsCode",
                    sortNum, ciaCode, db1, whsIN, grpSafe, mrcFilter));

                ciaCodesIN.Add("'" + ciaCode.Replace("'", "''") + "'");
            }

            if (innerSelects.Count == 0) return dt;

            // Step 3: wrap with outer SELECT to get WhsCodeSortNumber
            string fullSql = string.Format(@"
SELECT T.cia,T.ItmsGrpNam,T.ItemCode,T.BarCode,T.ItemName,T.u_class,T.U_BRAND,
       T.OnHand,T.OnOrder,T.IsCommited,
       ('['+CONVERT(VARCHAR,d.WhsCodeSortNumber)+'] '+T.WhsCode) AS WhsCode,
       T.CompanySortNumber,d.WhsCodeSortNumber
FROM ({0}) T
INNER JOIN (
    SELECT d1.CompanyId,d2.WhsCode,d2.SortNumber AS WhsCodeSortNumber
    FROM SMM_COMPANIES_FOR_INVENTORY d1 WITH(NOLOCK)
    INNER JOIN SMM_COMPANIES_FOR_INVENTORY_WHS d2 WITH(NOLOCK)
        ON d2.SmmCompaniesForInventory_Id=d1.Id
) d ON d.WhsCode=T.WhsCode
WHERE d.CompanyId IN ({1})
ORDER BY T.CompanySortNumber,T.cia,d.WhsCodeSortNumber,T.WhsCode,T.ItmsGrpNam,T.U_BRAND,T.ItemCode",
                string.Join("\nUNION ALL\n", innerSelects),
                string.Join(",", ciaCodesIN));

            cmd.CommandText = fullSql;
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.Clear();
            dt.Load(cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            Disconnect();
        }
        return dt;
    }

    public DataTable GetOrdenesConProblemas(DateTime? v_Period, string sap_db)
    {
        DataTable dt = new DataTable();
        try
        {
            Connect();
            DeliveryRepository repo = new DeliveryRepository(this);
            dt = repo.GetOrdersWithProblems(sap_db, v_Period);
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
        }
        finally
        {
            Disconnect();
        }
        return dt;
    }

    public DataTable GetGrupo(string CompanyId)
    {
        string sap_db = CompanyId;
        DataTable dt = new DataTable();

        try
        {
            Connect();

            cmd.CommandText = "select ItmsGrpCod,ItmsGrpNam from " + sap_db + ".dbo.OITB";
            cmd.CommandType = CommandType.Text;
            dt.Load(cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            Disconnect();
        }
        return dt;
    }
    public DataTable GetMarcas(string v_Grupo, string CompanyId)
    {
        DataTable dt = new DataTable();

        try
        {
            Connect();

            cmd.CommandText = "select 'TODAS' u_Brand union all select distinct u_Brand from " + CompanyId + ".dbo.oitm " + Queries.WITH_NOLOCK + " where ItmsGrpCod in (" + v_Grupo + ") and u_Brand <> '' order by u_Brand";
            cmd.CommandType = CommandType.Text;
            dt.Load(cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            Disconnect();
        }
        return dt;
    }
}
