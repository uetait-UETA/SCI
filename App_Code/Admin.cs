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
}