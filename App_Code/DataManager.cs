using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Summary description for DataManager
/// </summary>
public class DataManager
{
    protected SqlDb sqlDB;
    protected DFBUYINGdb dfbuyingDB;
    protected string tienda_db;
    protected string sapDb;   // SAP B1 company DB (Session["CompanyId"], e.g. DFC_HOLDINGS)

    public DataManager()
    {
        System.Web.HttpContext ctx = System.Web.HttpContext.Current;
        tienda_db = (ctx != null && ctx.Session != null) ? ctx.Session["tienda_db"] as string : null;
        if (string.IsNullOrEmpty(tienda_db)) tienda_db = ConfigurationManager.AppSettings["tienda_db"];

        sapDb = (ctx != null && ctx.Session != null) ? ctx.Session["CompanyId"] as string : null;
        if (string.IsNullOrEmpty(sapDb)) sapDb = ConfigurationManager.AppSettings["sap_db"];
    }
    public DataTable GetOrdenesAbiertas(string v_Corte, string v_Orden)
    {
        DataTable dt = new DataTable();
        try
        {
            dfbuyingDB = new DFBUYINGdb();
            dfbuyingDB.Connect();

            string sql = BuildCortePedidosSql(v_Corte, v_Orden);
            dfbuyingDB.cmd.CommandText = sql;
            dfbuyingDB.cmd.CommandType = CommandType.Text;
            if (v_Orden != "")
                dfbuyingDB.cmd.Parameters.Add(
                    new SqlParameter("@orden", SqlDbType.NVarChar, 20)).Value = v_Orden;
            dt.Load(dfbuyingDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            dfbuyingDB.Disconnect();
        }
        return dt;
    }

    public DataTable GetOrdenesAbiertasDetail(string v_Corte, string v_Orden)
    {
        DataTable dt = new DataTable();
        try
        {
            dfbuyingDB = new DFBUYINGdb();
            dfbuyingDB.Connect();

            string sql = BuildCortePedidosSql(v_Corte, v_Orden);
            dfbuyingDB.cmd.CommandText = sql;
            dfbuyingDB.cmd.CommandType = CommandType.Text;
            if (v_Orden != "")
                dfbuyingDB.cmd.Parameters.Add(
                    new SqlParameter("@orden", SqlDbType.NVarChar, 20)).Value = v_Orden;
            dt.Load(dfbuyingDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            dfbuyingDB.Disconnect();
        }
        return dt;
    }

    // Replaces sp_corteped_todosETI logic
    private string BuildCortePedidosSql(string v_Corte, string v_Orden)
    {
        if (v_Orden != "")
            return "SELECT * FROM dbo.V_CortePedidos WITH(NOLOCK) WHERE [Orden de Venta] = @orden";

        int corte;
        if (!int.TryParse(v_Corte, out corte)) corte = 0;

        if (corte == 1)
            return "SELECT * FROM dbo.V_CortePedidos WITH(NOLOCK) WHERE Delivery IS NULL AND DocStatus = 'O'";
        if (corte == 2)
            return "SELECT * FROM dbo.V_CortePedidos WITH(NOLOCK) WHERE Delivery IS NOT NULL AND DocStatus = 'C' AND Fecha > DATEADD(DD, -11, GETDATE())";
        return "SELECT * FROM dbo.V_CortePedidos WITH(NOLOCK) WHERE Fecha > DATEADD(DD, -11, GETDATE())";
    }

    public DataTable GetBinesDesp(string v_Orden, string v_DocS)
    {
        DataTable dt = new DataTable();
        try
        {
            dfbuyingDB = new DFBUYINGdb();
            dfbuyingDB.Connect();

            // Replaces sp_BinesDEspachados — queries linked server tocumen.tiendas.dbo.V_RecepTocumen
            string sql;
            if (v_Orden != "")
            {
                sql = "SELECT * FROM tocumen.tiendas.dbo.V_RecepTocumen WHERE osdocnum = @orden";
            }
            else if (v_DocS == "C")
            {
                sql = "SELECT * FROM tocumen.tiendas.dbo.V_RecepTocumen WHERE DocSts = 'R'";
            }
            else if (v_DocS == "O")
            {
                sql = "SELECT * FROM tocumen.tiendas.dbo.V_RecepTocumen WHERE DocSts = 'A'";
            }
            else
            {
                sql = "SELECT * FROM tocumen.tiendas.dbo.V_RecepTocumen";
            }

            dfbuyingDB.cmd.CommandText = sql;
            dfbuyingDB.cmd.CommandType = CommandType.Text;
            if (v_Orden != "")
                dfbuyingDB.cmd.Parameters.Add(
                    new SqlParameter("@orden", SqlDbType.NVarChar, 20)).Value = v_Orden;
            dt.Load(dfbuyingDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            dfbuyingDB.Disconnect();
        }
        return dt;
    }

    public DataTable GetDetalledeItemsporBins(string v_Orden, string v_Bin, string CompanyId)
    {
        DataTable dt = new DataTable();

        try
        {
            dfbuyingDB = new DFBUYINGdb();
            dfbuyingDB.Connect();

            string sql = @"
    select f.PRODUCT, DESCRIPT, CAST(ROUND(QTY, 0) AS INT) QTY 
    from wms.dbo.V_UETA_ship f " + Queries.WITH_NOLOCK + @"  
    inner join wms.dbo.PRODMSTR p " + Queries.WITH_NOLOCK + @"  on p.PRODUCT = f.PRODUCT
    where TRACKTRACE = '{1}' and P.CLIENTNAME = 'DFBUYING' and f.packslip = '{2}'
    ";

            sql = string.Format(sql, CompanyId, v_Bin, v_Orden);

            dfbuyingDB.cmd.CommandText = sql;

            //dfbuyingDB.cmd.CommandText = "select f.PRODUCT, [dbo].[FIVEBCODEPRODS] (f.PRODUCT) BarCode, DESCRIPT, QTY from wms.dbo.V_UETA_ship f (nolock) inner join  wms.dbo.PRODMSTR p(nolock) on p.PRODUCT = f.PRODUCT where TRACKTRACE = '" + v_Bin + "' and P.CLIENTNAME = 'DFBUYING'";
            dfbuyingDB.cmd.CommandType = CommandType.Text;
            dt.Load(dfbuyingDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            dfbuyingDB.Disconnect();
        }
        return dt;
    }
    public string GetBarcodeByProduct(string v_Item, string sap_db = "")
    {
        string v_Barcode, sql;

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sql = "SELECT STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM {0}.dbo.OBCD a1 " + Queries.WITH_NOLOCK + " WHERE a1.ItemCode='{1}' FOR XML PATH ('')), 1, 3, '')";

            string db = (sap_db != null && sap_db != "") ? sap_db : sapDb;
            sql = string.Format(sql, db, v_Item);

            //sqlDB.cmd.CommandText = "select [dbo].[FIVEBCODEPRODS] ('" + v_Item + "')";
            sqlDB.cmd.CommandText = sql;
            sqlDB.cmd.CommandType = CommandType.Text;
            v_Barcode = sqlDB.cmd.ExecuteScalar().ToString();
        }
        catch (Exception ex)
        {
            v_Barcode = ex.Message.ToString();
            return v_Barcode;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return v_Barcode;
    }
    public DataTable GetOrdenesConProblemas(string v_Period)
    {
        DataTable dt = new DataTable();

        try
        {
            dfbuyingDB = new DFBUYINGdb();
            dfbuyingDB.Connect();

            dfbuyingDB.cmd.CommandText = "sp_ordenesProblem";
            dfbuyingDB.cmd.CommandType = CommandType.StoredProcedure;
            dfbuyingDB.cmd.Parameters.Add(new SqlParameter("@fperiodo", SqlDbType.NVarChar)).Value = v_Period;
            dt.Load(dfbuyingDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            dfbuyingDB.Disconnect();
        }
        return dt;
    }
    //public DataTable GetInventarioDeTiendasTocumen(string v_Marca, string v_Grupo, string CompanyId, string whsType = "TODAS")
    //{
    //    DataTable dt = new DataTable();
    //    string sap_db = CompanyId;

    //    try
    //    {
    //        dfbuyingDB = new DFBUYINGdb();
    //        dfbuyingDB.Connect();

    //        dfbuyingDB.cmd.CommandText = "SP_INVENTORY_SAP_Location_dfa";
    //        dfbuyingDB.cmd.CommandType = CommandType.StoredProcedure;
    //        dfbuyingDB.cmd.Parameters.Add(new SqlParameter("@MARCA", SqlDbType.VarChar)).Value = v_Marca;
    //        dfbuyingDB.cmd.Parameters.Add(new SqlParameter("@GRUPO", SqlDbType.VarChar)).Value = v_Grupo;
    //        dfbuyingDB.cmd.Parameters.Add(new SqlParameter("@CompanyId", SqlDbType.NVarChar)).Value = sap_db;

    //        if (whsType != "TODAS")
    //        {
    //            dfbuyingDB.cmd.Parameters.Add(new SqlParameter("@WhsType", SqlDbType.NVarChar)).Value = sap_db;
    //        }
            
    //        dt.Load(dfbuyingDB.cmd.ExecuteReader());
    //    }
    //    catch (Exception ex)
    //    {
    //        dt.Columns.Add("ErrMsg", typeof(string));
    //        dt.Rows.Add(ex.Message);
    //        return dt;
    //    }
    //    finally
    //    {
    //        dfbuyingDB.Disconnect();
    //    }
    //    return dt;
    //}
    //public DataTable GetGrupo(string CompanyId)
    //{
    //    string sap_db = CompanyId;
    //    DataTable dt = new DataTable();

    //    try
    //    {
    //        dfbuyingDB = new DFBUYINGdb();
    //        dfbuyingDB.Connect();

    //        dfbuyingDB.cmd.CommandText = "select ItmsGrpCod,ItmsGrpNam from " + sap_db + ".dbo.OITB";
    //        dfbuyingDB.cmd.CommandType = CommandType.Text;
    //        dt.Load(dfbuyingDB.cmd.ExecuteReader());
    //    }
    //    catch (Exception ex)
    //    {
    //        dt.Columns.Add("ErrMsg", typeof(string));
    //        dt.Rows.Add(ex.Message);
    //        return dt;
    //    }
    //    finally
    //    {
    //        dfbuyingDB.Disconnect();
    //    }
    //    return dt;
    //}
    //public DataTable GetMarcas(string v_Grupo, string CompanyId)
    //{
    //    string sap_db = CompanyId;
    //    DataTable dt = new DataTable();

    //    try
    //    {
    //        dfbuyingDB = new DFBUYINGdb();
    //        dfbuyingDB.Connect();

    //        dfbuyingDB.cmd.CommandText = "select 'TODAS' u_brand union all select distinct u_brand from " + sap_db + ".dbo.oitm where ItmsGrpCod in (" + v_Grupo + ") and u_brand<>'' order by u_brand";
    //        dfbuyingDB.cmd.CommandType = CommandType.Text;
    //        dt.Load(dfbuyingDB.cmd.ExecuteReader());
    //    }
    //    catch (Exception ex)
    //    {
    //        dt.Columns.Add("ErrMsg", typeof(string));
    //        dt.Rows.Add(ex.Message);
    //        return dt;
    //    }
    //    finally
    //    {
    //        dfbuyingDB.Disconnect();
    //    }
    //    return dt;
    //}
    public DataTable GetBinesDespTiendas(string v_Doc, string v_ST, string CompanyId)
    {
        string sap_db = CompanyId;

        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();
            sqlDB.cmd.CommandText = "SP_VER_TRANSFER";
            sqlDB.cmd.CommandType = CommandType.StoredProcedure;
            //sqlDB.cmd.Parameters.Add(new SqlParameter("@Doc", SqlDbType.VarChar)).Value = v_Doc;
            //sqlDB.cmd.Parameters.Add(new SqlParameter("@st", SqlDbType.VarChar)).Value = v_ST;

            sqlDB.cmd.Parameters.Add(new SqlParameter("@Doc", SqlDbType.NVarChar)).Value = v_Doc;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@st", SqlDbType.NVarChar)).Value = v_ST;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@CompanyId", SqlDbType.NVarChar)).Value = sap_db;

            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable GetDetalledeItemsporBinsTiendas(string v_Orden, string v_Bin, string CompanyId)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            string sql = @"
select a.ItemCode PRODUCT, b.itemname DESCRIPT, CAST(ROUND(a.Quantity, 0) AS INT) QTY, BARCODE=ISNULL(STUFF((SELECT ' - ' + RIGHT(BcdCode, 5) FROM {0}.dbo.OBCD a1 " + Queries.WITH_NOLOCK + @" WHERE a1.ItemCode=a.ItemCode FOR XML PATH ('')), 1, 3, ''), '')
from TOCLEC_BODINT_PACKING_BIN_ORDENES_ITEMS a " + Queries.WITH_NOLOCK + @"  
inner join TOCLEC_BODINT_PACKING c " + Queries.WITH_NOLOCK + @"  on c.PackingId = a.PackingId
inner join {0}..OITM b " + Queries.WITH_NOLOCK + @"  on b.itemcode = a.ItemCode
where c.CompanyId = '{0}' and a.OrderId = '{1}' and a.BinId = '{2}'
";

            sql = string.Format(sql, CompanyId, v_Orden, v_Bin);

            //sqlDB.cmd.CommandText = "select a.ItemCode PRODUCT,b.itemname DESCRIPT, a.Quantity QTY from TOCLEC_BODINT_PACKING_BIN_ORDENES_ITEMS a (nolock) inner join v_ms_OITM(nolock) b on b.itemcode = a.ItemCode where OrderId = '" + v_Orden + "' and BinId = '" + v_Bin + "'";
            //sqlDB.cmd.CommandText = "select a.ItemCode PRODUCT,b.itemname DESCRIPT, CAST(ROUND(a.Quantity, 0) AS INT) QTY from TOCLEC_BODINT_PACKING_BIN_ORDENES_ITEMS a (nolock) inner join v_ms_OITM b (nolock) on b.itemcode = a.ItemCode  where OrderId = '" + v_Orden + "' and BinId = '" + v_Bin + "'";
            sqlDB.cmd.CommandText = sql;
            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable GetReciboBodegaSAPAPRIvsGPRO(string v_NumFact, string v_CIA)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "[dbo].[RS_BODEGA_PEDIDO_2B]";
            sqlDB.cmd.CommandType = CommandType.StoredProcedure;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@numfact", SqlDbType.NVarChar)).Value = v_NumFact;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@cia", SqlDbType.NVarChar)).Value = v_CIA;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable ReciboBodegaVSSAP(string v_NumFact, string v_CIA)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            //sqlDB.cmd.CommandText = "[" + tienda_db + "].[dbo].[RS_BODEGA_PEDIDO]";
            sqlDB.cmd.CommandText = "[dbo].[RS_BODEGA_PEDIDO]";
            sqlDB.cmd.CommandType = CommandType.StoredProcedure;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@numfact", SqlDbType.NVarChar)).Value = v_NumFact;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@cia", SqlDbType.NVarChar)).Value = v_CIA;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }

    public DataTable GetCIAs()
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            //sqlDB.cmd.CommandText = "SELECT [CiaId], [CiaName] FROM Tiendas.[dbo].[cias_po] " + Queries.WITH_NOLOCK + @" WHERE [CiaId] <> '-1'";
            sqlDB.cmd.CommandText = "SELECT [Companycode] AS CiaId, [CompanyName] AS CiaName FROM [dbo].[SMM_COMPANIES] " + Queries.WITH_NOLOCK + @" ";
            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }

    public DataTable GetCIAsKardex()
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "select codcia from TIENDAS.dbo.cias order by codcia";
            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable GetGruposKardex(string v_Company)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "SP_INVENTORY_GROUP";
            sqlDB.cmd.CommandType = CommandType.StoredProcedure;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@Company", SqlDbType.NVarChar)).Value = v_Company;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable GetInventoryKardex(string v_Company, string v_Groupo, string v_Item, string v_FromDate, string v_ToDate)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "SP_INVENTORY_KARDEX";
            sqlDB.cmd.CommandType = CommandType.StoredProcedure;
            sqlDB.cmd.CommandTimeout = 240;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@Company", SqlDbType.NVarChar)).Value = v_Company;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@Grupo", SqlDbType.NVarChar)).Value = v_Groupo;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@Item", SqlDbType.NVarChar)).Value = v_Item;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@FromDate", SqlDbType.NVarChar)).Value = v_FromDate;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@ToDate", SqlDbType.NVarChar)).Value = v_ToDate;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }

    public DataTable GetFAQImageByID(string v_FaqID)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "SELECT SolutionImageName, SolutionImage FROM dbo.TocPreguntasFrecuentes WHERE FAQID = " + v_FaqID;
            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }

    public DataTable GetItemAutoComplete(string v_Item, string v_Cmpy)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();
            //sqlDB.cmd.Parameters.Clear();

            //sqlDB.cmd.CommandText = "SELECT TOP 1000 ItemCode FROM " + v_Cmpy + ".dbo.OITM " + Queries.WITH_NOLOCK + @"  WHERE ItemCode LIKE '%" + v_Item + "%'";
            //sqlDB.cmd.CommandText = "SELECT TOP 1000 CONVERT(NVARCHAR(254), ItemCode) as BCDCODE FROM " + v_Cmpy + ".dbo.OBCD " + Queries.WITH_NOLOCK + @"  WHERE BCDCODE LIKE '%" + v_Item + "%'";

            sqlDB.cmd.CommandText = "dbo.GetItemsAutoComplete";
            sqlDB.cmd.CommandType = CommandType.StoredProcedure;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_Item", SqlDbType.NVarChar)).Value = v_Item;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_Cmpy", SqlDbType.NVarChar)).Value = v_Cmpy;

            //sqlDB.cmd.CommandText = "SELECT TOP 1000 ItemCode FROM " + v_Cmpy + ".dbo.OITM " + Queries.WITH_NOLOCK + @"  WHERE ItemCode LIKE '%" + v_Item + "%' UNION SELECT TOP 1000 ItemCode FROM DFATOCUMEN.dbo.OBCD " + Queries.WITH_NOLOCK + @"  WHERE BCDCODE LIKE '%" + v_Item + "%'";
            //sqlDB.cmd.CommandType = CommandType.Text;

            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    //public byte[] GetFAQImageByIDs(string v_FaqID)
    //{
    //    byte[] barrImg = null;

    //    try
    //    {
    //        sqlDB = new SqlDb();
    //        sqlDB.Connect();

    //        sqlDB.cmd.CommandText = "SELECT SolutionImage FROM dbo.TocPreguntasFrecuentes WHERE FAQID = " + v_FaqID;
    //        sqlDB.cmd.CommandType = CommandType.Text;
    //        barrImg = (byte[])sqlDB.cmd.ExecuteScalar();
    //    }
    //    catch (Exception ex)
    //    {
    //        barrImg = null;
    //    }
    //    finally
    //    {
    //        sqlDB.Disconnect();
    //    }
    //    return barrImg;
    //}
    public DataTable GetFAQs()
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "SELECT * FROM dbo.TocPreguntasFrecuentes order by Casos";
            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable GetFAQsByID(string v_FaqId)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "SELECT * FROM dbo.TocPreguntasFrecuentes WHERE FAQID = " + v_FaqId + "  order by Casos";
            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable GetFAQsByCase(string v_Case)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            if (v_Case == "")
            {
                sqlDB.cmd.CommandText = "SELECT * FROM dbo.TocPreguntasFrecuentes order by Casos";
            }
            else
            {
                sqlDB.cmd.CommandText = "SELECT * FROM dbo.TocPreguntasFrecuentes WHERE [Casos] LIKE '%" + v_Case + "%' order by Casos";
            }

            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable GetCasos()
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "SELECT FaqId, Casos FROM dbo.TocPreguntasFrecuentes order by Casos";
            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public string UpdateFAQs(string v_UpdateType, string v_FaqId, string v_Casos, string v_Solucion, string v_SolucionImageExt, string v_SolucionLink, string v_UpdatedBy)
    {
        string v_Message = "1";

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "dbo.spTocPreguntasFrecuentes";
            sqlDB.cmd.CommandType = CommandType.StoredProcedure;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_UpdateType", SqlDbType.NVarChar)).Value = v_UpdateType;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_FaqId", SqlDbType.NVarChar)).Value = v_FaqId;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_Casos", SqlDbType.NVarChar)).Value = v_Casos;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_Solucion", SqlDbType.NVarChar)).Value = v_Solucion;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_SolucionImageExt", SqlDbType.NVarChar)).Value = v_SolucionImageExt; ;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_SolucionLink", SqlDbType.NVarChar)).Value = v_SolucionLink;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_UpdatedBy", SqlDbType.NVarChar)).Value = v_UpdatedBy;

            v_Message = sqlDB.cmd.ExecuteScalar().ToString();
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return v_Message;
    }






    public DataTable GetVideosOperativos()
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "SELECT * FROM dbo.TocVideosOperativos order by Casos";
            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable GetVideosOperativosByID(string v_FaqId)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "SELECT * FROM dbo.TocVideosOperativos WHERE FAQID = " + v_FaqId + "  order by Casos";
            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable GetVideosOperativosByCase(string v_Case)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            if (v_Case == "")
            {
                sqlDB.cmd.CommandText = "SELECT * FROM dbo.TocVideosOperativos order by Casos";
            }
            else
            {
                sqlDB.cmd.CommandText = "SELECT * FROM dbo.TocVideosOperativos WHERE [Casos] LIKE '%" + v_Case + "%' order by Casos";
            }

            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable GetVideosOperativosCasos()
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "SELECT FaqId, Casos FROM dbo.TocVideosOperativos order by Casos";
            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public string UpdateVideosOperativos(string v_UpdateType, string v_FaqId, string v_Casos, string v_Solucion, string v_SolucionImageExt, string v_SolucionLink, string v_UpdatedBy)
    {
        string v_Message = "1";

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "dbo.spTocVideosOperativos";
            sqlDB.cmd.CommandType = CommandType.StoredProcedure;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_UpdateType", SqlDbType.NVarChar)).Value = v_UpdateType;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_FaqId", SqlDbType.NVarChar)).Value = v_FaqId;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_Casos", SqlDbType.NVarChar)).Value = v_Casos;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_Solucion", SqlDbType.NVarChar)).Value = v_Solucion;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_SolucionImageExt", SqlDbType.NVarChar)).Value = v_SolucionImageExt; ;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_SolucionLink", SqlDbType.NVarChar)).Value = v_SolucionLink;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_UpdatedBy", SqlDbType.NVarChar)).Value = v_UpdatedBy;

            v_Message = sqlDB.cmd.ExecuteScalar().ToString();
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return v_Message;
    }





    public DataTable GetVideosFinancieros()
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "SELECT * FROM dbo.TocVideosFinancieros order by Casos";
            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable GetVideosFinancierosByID(string v_FaqId)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "SELECT * FROM dbo.TocVideosFinancieros WHERE FAQID = " + v_FaqId + "  order by Casos";
            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable GetVideosFinancierosByCase(string v_Case)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            if (v_Case == "")
            {
                sqlDB.cmd.CommandText = "SELECT * FROM dbo.TocVideosFinancieros order by Casos";
            }
            else
            {
                sqlDB.cmd.CommandText = "SELECT * FROM dbo.TocVideosFinancieros WHERE [Casos] LIKE '%" + v_Case + "%' order by Casos";
            }

            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable GetVideosFinancierosCasos()
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "SELECT FaqId, Casos FROM dbo.TocVideosFinancieros order by Casos";
            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public string UpdateVideosFinancieros(string v_UpdateType, string v_FaqId, string v_Casos, string v_Solucion, string v_SolucionImageExt, string v_SolucionLink, string v_UpdatedBy)
    {
        string v_Message = "1";

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "dbo.spTocVideosFinancieros";
            sqlDB.cmd.CommandType = CommandType.StoredProcedure;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_UpdateType", SqlDbType.NVarChar)).Value = v_UpdateType;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_FaqId", SqlDbType.NVarChar)).Value = v_FaqId;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_Casos", SqlDbType.NVarChar)).Value = v_Casos;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_Solucion", SqlDbType.NVarChar)).Value = v_Solucion;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_SolucionImageExt", SqlDbType.NVarChar)).Value = v_SolucionImageExt; ;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_SolucionLink", SqlDbType.NVarChar)).Value = v_SolucionLink;
            sqlDB.cmd.Parameters.Add(new SqlParameter("@v_UpdatedBy", SqlDbType.NVarChar)).Value = v_UpdatedBy;

            v_Message = sqlDB.cmd.ExecuteScalar().ToString();
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return v_Message;
    }
	
	//2019-ABR-09: Añadido por Aldo Reina, para la búsqueda por código de barras. Se colocó esta función acá 
    //porque será utilizada en varios lugares, y es mejor escribirla una vez que varias veces:
    public DataTable SearchItemByBarCodes(string companyId, string grpId, string barCode)
    {
        DataTable dt = new DataTable();
        try
        {
            sqlDB = new SqlDb();

            string queryString = "";
            queryString = queryString + "SELECT ";
            queryString = queryString + "ItemCode = b.ItemCode, ";
            queryString = queryString + "ItemName = CASE WHEN b.U_Type = 'Duty Free' THEN 'DF | ' WHEN b.U_Type = 'Duty Paid' THEN 'DP | ' ELSE '' END + LTRIM(RTRIM(b.ItemCode)) + ' | ' + LTRIM(RTRIM(b.itemname)) ";

            queryString = queryString + "FROM {0}..OBCD a " + Queries.WITH_NOLOCK + @" ";
            queryString = queryString + "INNER JOIN {0}..OITM b " + Queries.WITH_NOLOCK + @" on b.ItemCode = a.ItemCode ";

            queryString = queryString + "WHERE a.BcdCode = '{1}'";

            if (grpId != "")
            {
                queryString = queryString + " AND b.ItmsGrpCod IN (" + grpId + ")";
            }

            queryString = String.Format(queryString, companyId, barCode);

            if (sqlDB.DbConnectionState == ConnectionState.Closed)
            {
                sqlDB.Connect();
            }

            sqlDB.adapter = new SqlDataAdapter(queryString, sqlDB.Conn);
            sqlDB.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
        finally
        {
            if (sqlDB.DbConnectionState == ConnectionState.Open)
            {
                sqlDB.Disconnect();
            }
        }
        return dt;
    }
    public DataTable GetResearchData(string v_Company, string v_Item)
    {
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            if (v_Item == "")
            {
                sql = Queries.With_ResearchWhsCodes() + @"
SELECT T0.[DocDate], T0.[DocNum], T0.[U_BOL] 'TRANSFER', T0.[Filler] 'FROM', T0.[ToWhsCode] 'TO', T1.[ItemCode], T1.[Dscription], cast(T1.[Quantity] as integer) 'Quantity', U_Receive, U_Despatch
FROM {0}.dbo.OWTR T0 " + Queries.WITH_NOLOCK + @"  
INNER JOIN {0}.dbo.WTR1 T1 " + Queries.WITH_NOLOCK + @"  ON T0.[DocEntry] = T1.[DocEntry]
WHERE( T0.[Filler] IN (SELECT WHSCODE FROM ResearchWhs " + Queries.WITH_NOLOCK + @" ) )

UNION ALL

SELECT T0.[DocDate], T0.[DocNum], T0.[U_BOL] 'TRANSFER', T0.[Filler] 'FROM', T0.[ToWhsCode] 'TO', T1.[ItemCode], T1.[Dscription], cast(T1.[Quantity] as integer) 'Quantity', U_Receive, U_Despatch
FROM {0}.dbo.OWTR T0 " + Queries.WITH_NOLOCK + @"  
INNER JOIN {0}.dbo.WTR1 T1 " + Queries.WITH_NOLOCK + @"  ON T0.[DocEntry] = T1.[DocEntry]
WHERE( T0.[ToWhsCode] IN (SELECT WHSCODE FROM ResearchWhs " + Queries.WITH_NOLOCK + @" ) )

ORDER BY 1 DESC, 3";
                sql = string.Format(sql, v_Company);
            }
            else
            {
                sql = Queries.With_ResearchWhsCodes() + @"
SELECT T0.[DocDate], T0.[DocNum], T0.[U_BOL] 'TRANSFER', T0.[Filler] 'FROM', T0.[ToWhsCode] 'TO', T1.[ItemCode], T1.[Dscription], cast(T1.[Quantity] as integer) 'Quantity', U_Receive, U_Despatch
FROM {0}.dbo.OWTR T0 " + Queries.WITH_NOLOCK + @"  
INNER JOIN {0}.dbo.WTR1 T1 " + Queries.WITH_NOLOCK + @"  ON T0.[DocEntry] = T1.[DocEntry]
WHERE( T0.[Filler] IN (SELECT WHSCODE FROM ResearchWhs " + Queries.WITH_NOLOCK + @" ) )
AND T1.[ItemCode] = '{1}'

UNION ALL

SELECT T0.[DocDate], T0.[DocNum], T0.[U_BOL] 'TRANSFER', T0.[Filler] 'FROM', T0.[ToWhsCode] 'TO', T1.[ItemCode], T1.[Dscription], cast(T1.[Quantity] as integer) 'Quantity', U_Receive, U_Despatch
FROM {0}.dbo.OWTR T0 " + Queries.WITH_NOLOCK + @"  
INNER JOIN {0}.dbo.WTR1 T1 " + Queries.WITH_NOLOCK + @"  ON T0.[DocEntry] = T1.[DocEntry]
WHERE( T0.[ToWhsCode] IN (SELECT WHSCODE FROM ResearchWhs " + Queries.WITH_NOLOCK + @" ) )
AND T1.[ItemCode] = '{1}'

ORDER BY 1 DESC, 3";
                sql = string.Format(sql, v_Company, v_Item);
            }

            sqlDB.cmd.CommandType = CommandType.Text;
            sqlDB.cmd.CommandText = sql;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable GetPreOrderHeaders(string v_Printed, string v_Picked, string v_Packed, string v_Delivered)
    {
        DataTable dt = new DataTable();
        string v_Query = "";

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            v_Query = @"SELECT a.*, b.isPrinted, b.isPicked, b.isPacked, b.isDelivered, b.LastUpdatedBy, b.LastUpdateDateTime
                        FROM [dbo].[ICG_HEADER_SALES_VW] a INNER JOIN dbo.DFAPreOrderManagement b ON a.MagentoOrderNumber = b.MagentoOrderNumber AND a.InvoiceNumber = b.InvoiceNumber
                        WHERE a.isPending != 'N' ";

            if (v_Printed != "0")
            {
                v_Query += " AND b.isPrinted = '" + v_Printed + "'";
            }

            if (v_Picked != "0")
            {
                v_Query += " AND b.isPicked = '" + v_Picked + "'";
            }

            if (v_Packed != "0")
            {
                v_Query += " AND b.isPacked = '" + v_Packed + "'";
            }

            if (v_Delivered != "0")
            {
                v_Query += " AND b.isDelivered = '" + v_Delivered + "'";
            }

            sqlDB.cmd.CommandText = v_Query;

            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable GetPreOrderHeaderByInvoiceID(string v_InvoiceNumber)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = @"SELECT	a.*, b.qty 
                                        FROM	[dbo].[ICG_HEADER_SALES_VW] a INNER JOIN 
		                                        (SELECT SUM(Quantity) QTY FROM [dbo].[ICG_DETAIL_SALES_VW] WHERE InvoiceNumber = " + v_InvoiceNumber + @") b ON 1=1
                                        WHERE	InvoiceNumber = " + v_InvoiceNumber;


            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public DataTable GetPreOrderDetails(string v_InvoiceNumber, string v_MagentoOrderNumber)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            string v_Query = "";

            if (v_MagentoOrderNumber != "0")
            {
                v_Query = @"SELECT a.InvoiceNumber, a.MagentoOrderNumber, a.SapProductId, a.ProductDescription, a.Quantity, b.EStatus, BarCode, LineNumber, 
                            ProductCategory, CAST(ProductCategory as VARCHAR(20)) + ' - ' +  CAST(ProductCategoryDesc AS VARCHAR(100)) ProductCategoryDesc, 
                            Brand, CAST(Brand as VARCHAR(20)) + ' - ' +  CAST(BrandDesc AS VARCHAR(100)) BrandDesc
                            from [dbo].[ICG_DETAIL_SALES_VW] a INNER JOIN (SELECT	CASE WHEN isPrinted = 'N' THEN 'Not Printed' ELSE CASE WHEN isPicked = 'N' THEN 'Printed' ELSE CASE WHEN isPacked = 'N' THEN 'Picked' ELSE CASE WHEN isDelivered = 'N' THEN 'Packed' ELSE 'Delivered' END END END END EStatus 
                            FROM dbo.DFAPreOrderManagement WHERE InvoiceNumber = " + v_InvoiceNumber + " AND MagentoOrderNumber = '" + v_MagentoOrderNumber + "') b ON 1 = 1 WHERE InvoiceNumber = " + v_InvoiceNumber + " AND MagentoOrderNumber = '" + v_MagentoOrderNumber + "'"; 
            }
            else
            {
                v_Query = @"SELECT a.InvoiceNumber, a.MagentoOrderNumber, a.SapProductId, a.ProductDescription, a.Quantity, b.EStatus, BarCode, LineNumber, 
                            ProductCategory, CAST(ProductCategory as VARCHAR(20)) + ' - ' +  CAST(ProductCategoryDesc AS VARCHAR(100)) ProductCategoryDesc, 
                            Brand, CAST(Brand as VARCHAR(20)) + ' - ' +  CAST(BrandDesc AS VARCHAR(100)) BrandDesc
                            from [dbo].[ICG_DETAIL_SALES_VW] a INNER JOIN (SELECT	CASE WHEN isPrinted = 'N' THEN 'Not Printed' ELSE CASE WHEN isPicked = 'N' THEN 'Printed' ELSE CASE WHEN isPacked = 'N' THEN 'Picked' ELSE CASE WHEN isDelivered = 'N' THEN 'Packed' ELSE 'Delivered' END END END END EStatus 
                            FROM dbo.DFAPreOrderManagement WHERE InvoiceNumber = " + v_InvoiceNumber  + ") b ON 1 = 1 WHERE InvoiceNumber = " + v_InvoiceNumber;
            }

            sqlDB.cmd.CommandText = v_Query;
            sqlDB.cmd.CommandType = CommandType.Text;
            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
    public string GetNewPreOrderHeaders(string v_UpdatedBy)
    {
        string v_Message = "1";

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            string v_Query = @"
                            INSERT INTO dbo.DFAPreOrderManagement(InvoiceNumber, MagentoOrderNumber, isPrinted, isPicked, isPacked, isDelivered, LastUpdatedBy, LastUpdateDateTime)
                            SELECT InvoiceNumber, MagentoOrderNumber, 'N', 'N', 'N', 'N', '" + v_UpdatedBy + "', GETDATE() FROM[dbo].[ICG_HEADER_SALES_VW] a" +
                            " WHERE   isPending != 'N' " +
                            " AND NOT EXISTS(SELECT * FROM dbo.DFAPreOrderManagement b WHERE CONVERT(NVARCHAR(50), a.MagentoOrderNumber) = CONVERT(NVARCHAR(50), b.MagentoOrderNumber))";

            sqlDB.cmd.CommandText = v_Query;
            sqlDB.cmd.CommandType = CommandType.Text;

            sqlDB.cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return v_Message;
    }
    public string PreOrderPrinted(string v_InvoiceNumber, string v_MagentoOrderNumber, string v_Status, string v_UpdatedBy)
    {
        string v_Message = "1";

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();
            string v_Query = "";

            if (v_MagentoOrderNumber == "0")
            {
                v_Query = @"
                            UPDATE dbo.DFAPreOrderManagement SET isPrinted = '" + v_Status + "', LastUpdatedBy = '" + v_UpdatedBy + "', LastUpdateDateTime = GETDATE() " +
                            " WHERE InvoiceNumber = " + v_InvoiceNumber;
            }
            else
            {
                v_Query = @"
                            UPDATE dbo.DFAPreOrderManagement SET isPrinted = '" + v_Status + "', LastUpdatedBy = '" + v_UpdatedBy + "', LastUpdateDateTime = GETDATE() " +
                            " WHERE InvoiceNumber = " + v_InvoiceNumber + " AND MagentoOrderNumber = '" + v_MagentoOrderNumber + "'";
            }

            sqlDB.cmd.CommandText = v_Query;
            sqlDB.cmd.CommandType = CommandType.Text;

            sqlDB.cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return v_Message;
    }
    public string PreOrderPicked(string v_InvoiceNumber, string v_MagentoOrderNumber, string v_Status, string v_UpdatedBy)
    {
        string v_Message = "1";

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();
            string v_Query = "";

            if (v_MagentoOrderNumber == "0")
            {
                v_Query = @"
                            UPDATE dbo.DFAPreOrderManagement SET isPicked = '" + v_Status + "', LastUpdatedBy = '" + v_UpdatedBy + "', LastUpdateDateTime = GETDATE() " +
                            " WHERE InvoiceNumber = " + v_InvoiceNumber;
            }
            else
            {
                v_Query = @"
                            UPDATE dbo.DFAPreOrderManagement SET isPicked = '" + v_Status + "', LastUpdatedBy = '" + v_UpdatedBy + "', LastUpdateDateTime = GETDATE() " +
                            " WHERE InvoiceNumber = " + v_InvoiceNumber + " AND MagentoOrderNumber = '" + v_MagentoOrderNumber + "'";
            }



            sqlDB.cmd.CommandText = v_Query;
            sqlDB.cmd.CommandType = CommandType.Text;

            sqlDB.cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return v_Message;
    }
    public string PreOrderPacked(string v_InvoiceNumber, string v_MagentoOrderNumber, string v_Status, string v_UpdatedBy)
    {
        string v_Message = "1";

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();
            string v_Query = "";

            if (v_MagentoOrderNumber == "0")
            {
                v_Query = @"
                            UPDATE dbo.DFAPreOrderManagement SET isPacked = '" + v_Status + "', LastUpdatedBy = '" + v_UpdatedBy + "', LastUpdateDateTime = GETDATE() " +
                            " WHERE InvoiceNumber = " + v_InvoiceNumber;
            }
            else
            {
                v_Query = @"
                            UPDATE dbo.DFAPreOrderManagement SET isPacked = '" + v_Status + "', LastUpdatedBy = '" + v_UpdatedBy + "', LastUpdateDateTime = GETDATE() " +
                            " WHERE InvoiceNumber = " + v_InvoiceNumber + " AND MagentoOrderNumber = '" + v_MagentoOrderNumber + "'";
            }



            sqlDB.cmd.CommandText = v_Query;
            sqlDB.cmd.CommandType = CommandType.Text;

            sqlDB.cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return v_Message;
    }
    public string PreOrderDelivered(string v_InvoiceNumber, string v_MagentoOrderNumber, string v_Status, string v_UpdatedBy)
    {
        string v_Message = "1";

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();
            string v_Query = "";

            if (v_MagentoOrderNumber == "0")
            {
                v_Query = @"
                            UPDATE dbo.DFAPreOrderManagement SET isDelivered = '" + v_Status + "', LastUpdatedBy = '" + v_UpdatedBy + "', LastUpdateDateTime = GETDATE() " +
                            " WHERE InvoiceNumber = " + v_InvoiceNumber;
            }
            else
            {
                v_Query = @"
                            UPDATE dbo.DFAPreOrderManagement SET isDelivered = '" + v_Status + "', LastUpdatedBy = '" + v_UpdatedBy + "', LastUpdateDateTime = GETDATE() " +
                            " WHERE InvoiceNumber = " + v_InvoiceNumber + " AND MagentoOrderNumber = '" + v_MagentoOrderNumber + "'";
            }



            sqlDB.cmd.CommandText = v_Query;
            sqlDB.cmd.CommandType = CommandType.Text;

            sqlDB.cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return v_Message;
    }
	
	/*2021-ABR-27: Agregado por Aldo Reina, para reporte de preOrders por estado:*/
	public DataTable GetPreOrderHeaders(string v_Printed, string v_Picked, string v_Packed, string v_Delivered, string v_OTypeId)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "RPT_PreOrderManagement_Det";

            sqlDB.cmd.CommandType = CommandType.StoredProcedure;

            sqlDB.cmd.Parameters.Add(new SqlParameter("@InvoiceNumber", "-1"));
            sqlDB.cmd.Parameters.Add(new SqlParameter("@OTypeId", v_OTypeId));
            sqlDB.cmd.Parameters.Add(new SqlParameter("@IsPrinted", v_Printed));
            sqlDB.cmd.Parameters.Add(new SqlParameter("@IsPicked", v_Picked));
            sqlDB.cmd.Parameters.Add(new SqlParameter("@IsPacked", v_Packed));
            sqlDB.cmd.Parameters.Add(new SqlParameter("@IsDelivered", v_Delivered));

            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
	public DataTable GetPreOrderHeaderByInvoiceID2(string v_InvoiceNumber)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "RPT_PreOrderManagement_Det";

            sqlDB.cmd.CommandType = CommandType.StoredProcedure;

            sqlDB.cmd.Parameters.Add(new SqlParameter("@InvoiceNumber", v_InvoiceNumber));
            sqlDB.cmd.Parameters.Add(new SqlParameter("@OTypeId", "0"));
            sqlDB.cmd.Parameters.Add(new SqlParameter("@IsPrinted", "0"));
            sqlDB.cmd.Parameters.Add(new SqlParameter("@IsPicked", "0"));
            sqlDB.cmd.Parameters.Add(new SqlParameter("@IsPacked", "0"));
            sqlDB.cmd.Parameters.Add(new SqlParameter("@IsDelivered", "0"));

            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
	public DataTable GetPreOrderDetails2(string v_InvoiceNumber, string v_MagentoOrderNumber)
    {
        DataTable dt = new DataTable();

        try
        {
            sqlDB = new SqlDb();
            sqlDB.Connect();

            sqlDB.cmd.CommandText = "RPT_PreOrderManagement_Det_Detail";
            sqlDB.cmd.CommandType = CommandType.StoredProcedure;

            sqlDB.cmd.Parameters.Add(new SqlParameter("@InvoiceNumber", v_InvoiceNumber));
            sqlDB.cmd.Parameters.Add(new SqlParameter("@MagentoOrderNumber", v_MagentoOrderNumber));

            dt.Load(sqlDB.cmd.ExecuteReader());
        }
        catch (Exception ex)
        {
            dt.Columns.Add("ErrMsg", typeof(string));
            dt.Rows.Add(ex.Message);
            return dt;
        }
        finally
        {
            sqlDB.Disconnect();
        }
        return dt;
    }
	/*;*/
}