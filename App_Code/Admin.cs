using System;
using System.Data;
using System.Web;

/// <summary>
/// Summary description for Admin
/// </summary>
public class Admin
{
    protected SqlDb db;

	public Admin()
	{
		//
		// TODO: Add constructor logic here
		//
	}

    public static DataTable GetUsers()
    {
        SqlDb db = new SqlDb();
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"
            select 
                 UserID
                ,FirstName
                ,OtherNames
                ,LastName1
                ,LastName2
                ,AdditionalInfo
                ,case when Active = 'Y' then 'true' else 'false' end Active
                ,Date_Created
                ,Created_By
            from smm_users
            order by FirstName";

            db.cmd.CommandText = sql;
            db.adapter.SelectCommand = db.cmd;
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.GetUsers.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public static bool UpdateUser(DataRow Row)
    {
        bool Result = false;
        SqlDb db = new SqlDb();
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"
            update smm_users 
            set
                 FirstName = @FirstName
                ,OtherNames = @OtherNames
                ,LastName1 = @LastName1
                ,LastName2 = @LastName2
                ,AdditionalInfo = @AdditionalInfo
                ,Active = @Active
            where UserId = @UserId";

            string Active = Row["Active"].ToString().ToLower() == "true" ? "Y" : "N";

            db.cmd.Parameters.AddWithValue("@FirstName", Row["FirstName"].ToString());
            db.cmd.Parameters.AddWithValue("@OtherNames", Row["OtherNames"].ToString());
            db.cmd.Parameters.AddWithValue("@LastName1", Row["LastName1"].ToString());
            db.cmd.Parameters.AddWithValue("@LastName2", Row["LastName2"].ToString());
            db.cmd.Parameters.AddWithValue("@AdditionalInfo", Row["AdditionalInfo"].ToString());
            db.cmd.Parameters.AddWithValue("@Active", Active);
            db.cmd.Parameters.AddWithValue("@UserId", Row["UserId"].ToString());

            db.cmd.CommandText = sql;
            db.cmd.ExecuteNonQuery();
            Result = true;
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.UpdateUser.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return Result;
    }

    public static int InsertUser(DataRow newRow)
    {
        int UserId = 0;
        SqlDb db = new SqlDb();
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            string user = HttpContext.Current.User.Identity.Name.ToLower();
            db.Connect();

            sql = @"
            insert into smm_users 
            (    FirstName 
                ,OtherNames 
                ,LastName1 
                ,LastName2 
                ,AdditionalInfo                 
                ,Active
                ,Date_Created
                ,Created_By
            )
            values
             (   @FirstName 
                ,@OtherNames 
                ,@LastName1 
                ,@LastName2 
                ,@AdditionalInfo                 
                ,@Active
                ,getdate()
                ,@Created_By
            );
            SELECT SCOPE_IDENTITY()";

            string Active = newRow["Active"].ToString().ToLower() == "true" ? "Y" : "N";

            db.cmd.Parameters.AddWithValue("@FirstName", newRow["FirstName"].ToString());
            db.cmd.Parameters.AddWithValue("@OtherNames", newRow["OtherNames"].ToString());
            db.cmd.Parameters.AddWithValue("@LastName1", newRow["LastName1"].ToString());
            db.cmd.Parameters.AddWithValue("@LastName2", newRow["LastName2"].ToString());
            db.cmd.Parameters.AddWithValue("@AdditionalInfo", newRow["AdditionalInfo"].ToString());
            db.cmd.Parameters.AddWithValue("@Active", Active);
            db.cmd.Parameters.AddWithValue("@Created_By", user);

            db.cmd.CommandText = sql;
            UserId = Convert.ToInt32(db.cmd.ExecuteScalar());
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.InsertUser.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
        return UserId;
    }

    public static DataTable GetLogins()
    {
        SqlDb db = new SqlDb();
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"
            select
	             a.LoginID
	            ,a.PassWd
	            ,a.UserID
	            ,case when a.Active = 'Y' then 'true' else 'false' end Active
	            ,a.RoleId
	            ,a.Date_Created
	            ,a.Created_By
	            ,a.NumPrints
	            ,a.TypeWhs
	            ,b.FirstName
                ,b.LastName1
                ,b.FirstName + ' ' + b.LastName1 as UserFullName
            	,c.role_description role
                ,case when a.Active_Pdt = 'Y' then 'true' else 'false' end Active_Pdt
                ,ISNULL(a.AllowedCompanyId, 0) AS AllowedCompanyId
			from smm_login a inner join smm_users b on a.UserID = b.UserID left outer join sisinv_roles c on c.RoleID = a.RoleID
            order by a.LoginID, c.role_description";

            db.cmd.CommandText = sql;
            db.adapter.SelectCommand = db.cmd;
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.GetLogins.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public static bool UpdateLogin(DataRow Row)
    {
        bool Result = false;
        SqlDb db = new SqlDb();
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"
            update smm_login
            set
                 PassWd = @PassWd
                ,UserID = @UserID
                ,RoleId = @RoleId
                ,NumPrints = @NumPrints
                ,TypeWhs = @TypeWhs
                ,Active = @Active
                ,Active_Pdt = @Active_Pdt
                ,AllowedCompanyId = @AllowedCompanyId
            where LoginID = @LoginID";

            string Active = Row["Active"].ToString().ToLower() == "true" ? "Y" : "N";
            string Active_Pdt = Row["Active_Pdt"].ToString().ToLower() == "true" ? "Y" : "N";

            db.cmd.Parameters.AddWithValue("@PassWd", Row["PassWd"].ToString());
            db.cmd.Parameters.AddWithValue("@UserID", Row["UserID"].ToString());
            db.cmd.Parameters.AddWithValue("@RoleId", Row["RoleId"].ToString());
            db.cmd.Parameters.AddWithValue("@NumPrints", Row["NumPrints"].ToString());
            db.cmd.Parameters.AddWithValue("@TypeWhs", Row["TypeWhs"].ToString());
            db.cmd.Parameters.AddWithValue("@Active", Active);
            db.cmd.Parameters.AddWithValue("@Active_Pdt", Active_Pdt);
            db.cmd.Parameters.AddWithValue("@AllowedCompanyId", Convert.ToInt32(Row["AllowedCompanyId"]));
            db.cmd.Parameters.AddWithValue("@LoginID", Row["LoginID"].ToString());

            db.cmd.CommandText = sql;
            db.cmd.ExecuteNonQuery();
            Result = true;
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.UpdateLogin.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return Result;
    }

    public static bool InsertLogin(DataRow newRow)
    {
        bool Result = false;
        SqlDb db = new SqlDb();
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            string user = HttpContext.Current.User.Identity.Name.ToLower();
            db.Connect();

            sql = @"
            insert into smm_login
            (    LoginID
                ,PassWd
                ,UserID
                ,RoleId
                ,NumPrints
                ,TypeWhs
                ,Active
                ,Date_Created
                ,Created_By
                ,Active_Pdt
                ,AllowedCompanyId
            )
            values
             (   @LoginID
                ,@PassWd
                ,@UserID
                ,@RoleId
                ,@NumPrints
                ,@TypeWhs
                ,@Active
                ,getdate()
                ,@Created_By
                ,@Active_Pdt
                ,@AllowedCompanyId
            )";

            string Active = newRow["Active"].ToString().ToLower() == "true" ? "Y" : "N";
            string Active_Pdt = newRow["Active_Pdt"].ToString().ToLower() == "true" ? "Y" : "N";

            db.cmd.Parameters.AddWithValue("@LoginID", newRow["LoginID"].ToString());
            db.cmd.Parameters.AddWithValue("@PassWd", newRow["PassWd"].ToString());
            db.cmd.Parameters.AddWithValue("@UserID", newRow["UserID"].ToString());
            db.cmd.Parameters.AddWithValue("@RoleId", newRow["RoleId"].ToString());
            db.cmd.Parameters.AddWithValue("@NumPrints", newRow["NumPrints"].ToString());
            db.cmd.Parameters.AddWithValue("@TypeWhs", newRow["TypeWhs"].ToString());
            db.cmd.Parameters.AddWithValue("@Active", Active);
            db.cmd.Parameters.AddWithValue("@Active_Pdt", Active_Pdt);
            db.cmd.Parameters.AddWithValue("@AllowedCompanyId", Convert.ToInt32(newRow["AllowedCompanyId"]));
            db.cmd.Parameters.AddWithValue("@Created_By", user);

            db.cmd.CommandText = sql;
            db.cmd.ExecuteScalar();
            Result = true;
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.InsertLogin.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
        return Result;
    }

    public static DataTable GetRoleList()
    {
        SqlDb db = new SqlDb();
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"
            select 
	             RoleID
                ,role_description
            from sisinv_roles
            order by role_description";

            db.cmd.CommandText = sql;
            db.adapter.SelectCommand = db.cmd;
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.GetRoleList.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public static DataTable GetCompanyList()
    {
        SqlDb db = new SqlDb();
        DataTable dt = new DataTable();
        try
        {
            db.Connect();
            db.cmd.CommandText = "SELECT Branch, CompanyName FROM SMM_COMPANIES ORDER BY CompanyName";
            db.adapter.SelectCommand = db.cmd;
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.GetCompanyList. MESSAGE: " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
        return dt;
    }

    public static object GetUserList()
    {
        SqlDb db = new SqlDb();
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"
            select 
	             UserID
                ,FirstName + ' ' + LastName1 as UserFullName
			from smm_users 
            order by UserFullName";

            db.cmd.CommandText = sql;
            db.adapter.SelectCommand = db.cmd;
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.GetUserList.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public static bool LoginExists(string LoginID)
    {
        bool Result = false;
        SqlDb db = new SqlDb();
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"
            select  LoginID
			from    smm_login 
            where   LoginID = @LoginID";

            db.cmd.CommandText = sql;
            db.cmd.Parameters.AddWithValue("@LoginID", LoginID);
            db.adapter.SelectCommand = db.cmd;
            db.adapter.Fill(dt);
            if (dt.Rows.Count > 0)
            {
                Result = true;
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.LoginExists.  MESSAGE : " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return Result;
    }

    // ── Bodega-Tienda pairs (SMM_REA_BODEGA_TIENDAS) ─────────────────────────

    public static DataTable GetWarehouseList(string sapDb)
    {
        SqlDb db = new SqlDb();
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"
            select WhsCode,
                   CONVERT(nvarchar(30), ISNULL(U_POSCode,'')) + ' - ' + WhsCode + ' - ' + WhsName as DisplayName
            from " + sapDb + @".dbo.OWHS with (nolock)
            order by U_POSCode, WhsCode";

            db.cmd.CommandText = sql;
            db.adapter.SelectCommand = db.cmd;
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.GetWarehouseList.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public static DataTable GetBodegaTiendaPairs(string companyId, string sapDb)
    {
        SqlDb db = new SqlDb();
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"
            select bt.CompanyID, bt.BodegaID, bt.TiendaID, bt.Priority,
                   case when bt.isActive = 1 then 'true' else 'false' end as isActive,
                   ISNULL(wb.WhsName,'') as BodegaName, ISNULL(wt.WhsName,'') as TiendaName
            from SMM_REA_BODEGA_TIENDAS bt with (nolock)
            left join " + sapDb + @".dbo.OWHS wb with (nolock) on wb.WhsCode = bt.BodegaID
            left join " + sapDb + @".dbo.OWHS wt with (nolock) on wt.WhsCode = bt.TiendaID
            where bt.CompanyID = @CompanyID
            order by bt.BodegaID, bt.TiendaID";

            db.cmd.CommandText = sql;
            db.cmd.Parameters.AddWithValue("@CompanyID", companyId);
            db.adapter.SelectCommand = db.cmd;
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.GetBodegaTiendaPairs.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public static bool BodegaTiendaPairExists(string companyId, string bodegaId, string tiendaId)
    {
        bool Result = false;
        SqlDb db = new SqlDb();
        DataTable dt = new DataTable();

        try
        {
            db.Connect();

            db.cmd.CommandText = @"
            select 1 from SMM_REA_BODEGA_TIENDAS
            where CompanyID = @CompanyID and BodegaID = @BodegaID and TiendaID = @TiendaID";
            db.cmd.Parameters.AddWithValue("@CompanyID", companyId);
            db.cmd.Parameters.AddWithValue("@BodegaID", bodegaId);
            db.cmd.Parameters.AddWithValue("@TiendaID", tiendaId);
            db.adapter.SelectCommand = db.cmd;
            db.adapter.Fill(dt);

            Result = dt.Rows.Count > 0;
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.BodegaTiendaPairExists.  MESSAGE : " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return Result;
    }

    public static bool InsertBodegaTiendaPair(DataRow newRow)
    {
        bool Result = false;
        SqlDb db = new SqlDb();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"
            insert into SMM_REA_BODEGA_TIENDAS (CompanyID, BodegaID, TiendaID, Priority, isActive)
            values (@CompanyID, @BodegaID, @TiendaID, @Priority, @isActive)";

            db.cmd.Parameters.AddWithValue("@CompanyID", newRow["CompanyID"].ToString());
            db.cmd.Parameters.AddWithValue("@BodegaID", newRow["BodegaID"].ToString());
            db.cmd.Parameters.AddWithValue("@TiendaID", newRow["TiendaID"].ToString());
            db.cmd.Parameters.AddWithValue("@Priority", Convert.ToInt32(newRow["Priority"]));
            db.cmd.Parameters.AddWithValue("@isActive", newRow["isActive"].ToString().ToLower() == "true" ? 1 : 0);

            db.cmd.CommandText = sql;
            db.cmd.ExecuteNonQuery();
            Result = true;
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.InsertBodegaTiendaPair.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return Result;
    }

    public static bool UpdateBodegaTiendaPair(DataRow row)
    {
        bool Result = false;
        SqlDb db = new SqlDb();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"
            update SMM_REA_BODEGA_TIENDAS
            set Priority = @Priority, isActive = @isActive
            where CompanyID = @CompanyID and BodegaID = @BodegaID and TiendaID = @TiendaID";

            db.cmd.Parameters.AddWithValue("@Priority", Convert.ToInt32(row["Priority"]));
            db.cmd.Parameters.AddWithValue("@isActive", row["isActive"].ToString().ToLower() == "true" ? 1 : 0);
            db.cmd.Parameters.AddWithValue("@CompanyID", row["CompanyID"].ToString());
            db.cmd.Parameters.AddWithValue("@BodegaID", row["BodegaID"].ToString());
            db.cmd.Parameters.AddWithValue("@TiendaID", row["TiendaID"].ToString());

            db.cmd.CommandText = sql;
            db.cmd.ExecuteNonQuery();
            Result = true;
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.UpdateBodegaTiendaPair.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return Result;
    }

    // ── Bodega-Tienda schedule (SMM_REA_BODEGA_TIENDAS_HRS) ──────────────────

    // Active pairs, for the schedule form's "which pair" picker — only lets a
    // schedule row reference a pair that actually exists in SMM_REA_BODEGA_TIENDAS.
    public static DataTable GetActiveBodegaTiendaPairsForDropdown(string companyId, string sapDb)
    {
        SqlDb db = new SqlDb();
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"
            select bt.BodegaID + '|' + bt.TiendaID as PairKey,
                   ISNULL(wb.WhsName, bt.BodegaID) + '  ->  ' + ISNULL(wt.WhsName, bt.TiendaID) as DisplayName
            from SMM_REA_BODEGA_TIENDAS bt with (nolock)
            left join " + sapDb + @".dbo.OWHS wb with (nolock) on wb.WhsCode = bt.BodegaID
            left join " + sapDb + @".dbo.OWHS wt with (nolock) on wt.WhsCode = bt.TiendaID
            where bt.CompanyID = @CompanyID and bt.isActive = 1
            order by bt.BodegaID, bt.TiendaID";

            db.cmd.CommandText = sql;
            db.cmd.Parameters.AddWithValue("@CompanyID", companyId);
            db.adapter.SelectCommand = db.cmd;
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.GetActiveBodegaTiendaPairsForDropdown.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public static DataTable GetBodegaTiendaSchedule(string companyId, string sapDb)
    {
        SqlDb db = new SqlDb();
        DataTable dt = new DataTable();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"
            select h.CompanyID, h.BodegaID, h.TiendaID, h.FromHrs, h.ToHrs,
                   case when h.isActive = 1 then 'true' else 'false' end as isActive,
                   ISNULL(h.Days, 'ALL') as Days,
                   ISNULL(wb.WhsName,'') as BodegaName, ISNULL(wt.WhsName,'') as TiendaName
            from SMM_REA_BODEGA_TIENDAS_HRS h with (nolock)
            left join " + sapDb + @".dbo.OWHS wb with (nolock) on wb.WhsCode = h.BodegaID
            left join " + sapDb + @".dbo.OWHS wt with (nolock) on wt.WhsCode = h.TiendaID
            where h.CompanyID = @CompanyID
            order by h.BodegaID, h.TiendaID";

            db.cmd.CommandText = sql;
            db.cmd.Parameters.AddWithValue("@CompanyID", companyId);
            db.adapter.SelectCommand = db.cmd;
            db.adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.GetBodegaTiendaSchedule.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return dt;
    }

    public static bool BodegaTiendaScheduleExists(string companyId, string bodegaId, string tiendaId)
    {
        bool Result = false;
        SqlDb db = new SqlDb();
        DataTable dt = new DataTable();

        try
        {
            db.Connect();

            db.cmd.CommandText = @"
            select 1 from SMM_REA_BODEGA_TIENDAS_HRS
            where CompanyID = @CompanyID and BodegaID = @BodegaID and TiendaID = @TiendaID";
            db.cmd.Parameters.AddWithValue("@CompanyID", companyId);
            db.cmd.Parameters.AddWithValue("@BodegaID", bodegaId);
            db.cmd.Parameters.AddWithValue("@TiendaID", tiendaId);
            db.adapter.SelectCommand = db.cmd;
            db.adapter.Fill(dt);

            Result = dt.Rows.Count > 0;
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.BodegaTiendaScheduleExists.  MESSAGE : " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return Result;
    }

    public static bool InsertBodegaTiendaSchedule(DataRow newRow)
    {
        bool Result = false;
        SqlDb db = new SqlDb();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"
            insert into SMM_REA_BODEGA_TIENDAS_HRS (CompanyID, BodegaID, TiendaID, FromHrs, ToHrs, isActive, Days)
            values (@CompanyID, @BodegaID, @TiendaID, @FromHrs, @ToHrs, @isActive, @Days)";

            db.cmd.Parameters.AddWithValue("@CompanyID", newRow["CompanyID"].ToString());
            db.cmd.Parameters.AddWithValue("@BodegaID", newRow["BodegaID"].ToString());
            db.cmd.Parameters.AddWithValue("@TiendaID", newRow["TiendaID"].ToString());
            db.cmd.Parameters.AddWithValue("@FromHrs", newRow["FromHrs"].ToString());
            db.cmd.Parameters.AddWithValue("@ToHrs", newRow["ToHrs"].ToString());
            db.cmd.Parameters.AddWithValue("@isActive", newRow["isActive"].ToString().ToLower() == "true" ? 1 : 0);
            db.cmd.Parameters.AddWithValue("@Days", newRow["Days"].ToString());

            db.cmd.CommandText = sql;
            db.cmd.ExecuteNonQuery();
            Result = true;
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.InsertBodegaTiendaSchedule.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return Result;
    }

    public static bool UpdateBodegaTiendaSchedule(DataRow row)
    {
        bool Result = false;
        SqlDb db = new SqlDb();
        string sql = "";

        try
        {
            db.Connect();

            sql = @"
            update SMM_REA_BODEGA_TIENDAS_HRS
            set FromHrs = @FromHrs, ToHrs = @ToHrs, isActive = @isActive, Days = @Days
            where CompanyID = @CompanyID and BodegaID = @BodegaID and TiendaID = @TiendaID";

            db.cmd.Parameters.AddWithValue("@FromHrs", row["FromHrs"].ToString());
            db.cmd.Parameters.AddWithValue("@ToHrs", row["ToHrs"].ToString());
            db.cmd.Parameters.AddWithValue("@isActive", row["isActive"].ToString().ToLower() == "true" ? 1 : 0);
            db.cmd.Parameters.AddWithValue("@Days", row["Days"].ToString());
            db.cmd.Parameters.AddWithValue("@CompanyID", row["CompanyID"].ToString());
            db.cmd.Parameters.AddWithValue("@BodegaID", row["BodegaID"].ToString());
            db.cmd.Parameters.AddWithValue("@TiendaID", row["TiendaID"].ToString());

            db.cmd.CommandText = sql;
            db.cmd.ExecuteNonQuery();
            Result = true;
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.UpdateBodegaTiendaSchedule.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return Result;
    }

    // "Delete" in both grids is a soft-deactivate (isActive=0) — never a real DELETE,
    // consistent with how every other maintenance script in this system handles
    // SMM_REA_BODEGA_TIENDAS / SMM_REA_BODEGA_TIENDAS_HRS.
    public static bool DeactivateBodegaTiendaPair(string companyId, string bodegaId, string tiendaId)
    {
        bool Result = false;
        SqlDb db = new SqlDb();

        try
        {
            db.Connect();
            db.cmd.CommandText = @"
            update SMM_REA_BODEGA_TIENDAS set isActive = 0
            where CompanyID = @CompanyID and BodegaID = @BodegaID and TiendaID = @TiendaID";
            db.cmd.Parameters.AddWithValue("@CompanyID", companyId);
            db.cmd.Parameters.AddWithValue("@BodegaID", bodegaId);
            db.cmd.Parameters.AddWithValue("@TiendaID", tiendaId);
            db.cmd.ExecuteNonQuery();
            Result = true;
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.DeactivateBodegaTiendaPair.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return Result;
    }

    public static bool DeactivateBodegaTiendaSchedule(string companyId, string bodegaId, string tiendaId)
    {
        bool Result = false;
        SqlDb db = new SqlDb();

        try
        {
            db.Connect();
            db.cmd.CommandText = @"
            update SMM_REA_BODEGA_TIENDAS_HRS set isActive = 0
            where CompanyID = @CompanyID and BodegaID = @BodegaID and TiendaID = @TiendaID";
            db.cmd.Parameters.AddWithValue("@CompanyID", companyId);
            db.cmd.Parameters.AddWithValue("@BodegaID", bodegaId);
            db.cmd.Parameters.AddWithValue("@TiendaID", tiendaId);
            db.cmd.ExecuteNonQuery();
            Result = true;
        }
        catch (Exception ex)
        {
            throw new Exception("Exception in function Admin.DeactivateBodegaTiendaSchedule.  MESSAGE :" + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }

        return Result;
    }
}