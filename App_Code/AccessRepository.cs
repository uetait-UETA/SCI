using System;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Replaces SPs: SISINV_GET_ACCESSTYPE_PRC, SMM_UACTIONS_CNT,
/// smm_login_validations, S_Cia_iPrinters_ByUserAndSapCia
/// </summary>
public class AccessRepository
{
    private readonly SqlDb _db;

    public AccessRepository(SqlDb db)
    {
        _db = db;
    }

    // ── SISINV_GET_ACCESSTYPE_PRC ─────────────────────────────────────────
    public void GetAccessType(string loginId, string controlName,
        out string accessType, out string roleDescription)
    {
        accessType       = "N";
        roleDescription  = "No Definido";

        try
        {
            using (SqlCommand cmd = new SqlCommand(
                "SELECT RoleId FROM dbo.SMM_LOGIN2 WITH(NOLOCK) WHERE loginId = @lid",
                _db.Conn))
            {
                cmd.Parameters.AddWithValue("@lid", loginId);
                object val = cmd.ExecuteScalar();
                if (val != null && val.ToString() == "0")
                {
                    accessType      = "F";
                    roleDescription = "TOTAL";
                    return;
                }
            }

            using (SqlCommand cmd = new SqlCommand(
                @"SELECT ISNULL(MAX(gt.AccessType), 'N')                     AS AccessType,
                         UPPER(ISNULL(MAX(rr.Role_Description), 'No Definido')) AS Role_Description
                  FROM dbo.SISINV_GET_ACCESSTYPE_VIEW gt,
                       dbo.SISINV_ROLES rr
                  WHERE gt.RoleID     = rr.RoleID
                    AND gt.loginID    = @lid
                    AND gt.controlName = @ctrl",
                _db.Conn))
            {
                cmd.Parameters.AddWithValue("@lid",  loginId);
                cmd.Parameters.AddWithValue("@ctrl", controlName);
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        accessType      = rdr[0].ToString();
                        roleDescription = rdr[1].ToString();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _db.Disconnect();
            throw new Exception("Error in GetAccessType: " + ex.Message);
        }
    }

    // ── SMM_UACTIONS_CNT ─────────────────────────────────────────────────
    public int GetUserActionsCount(string loginId, string action,
        string whsCode, string category)
    {
        try
        {
            int cnt;
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT COUNT(*) FROM dbo.SMM_USERS_ACTIONS WITH(NOLOCK)
                  WHERE Action = @act AND WhsCode = @whs",
                _db.Conn))
            {
                cmd.Parameters.AddWithValue("@act", action);
                cmd.Parameters.AddWithValue("@whs", whsCode);
                cnt = (int)cmd.ExecuteScalar();
            }

            if (cnt == 0) return 1;

            using (SqlCommand cmd = new SqlCommand(
                @"SELECT COUNT(*) FROM dbo.SMM_USERS_ACTIONS WITH(NOLOCK)
                  WHERE Action = @act AND WhsCode = @whs
                    AND (Category = @cat OR UPPER(Category) = 'ALL')",
                _db.Conn))
            {
                cmd.Parameters.AddWithValue("@act", action);
                cmd.Parameters.AddWithValue("@whs", whsCode);
                cmd.Parameters.AddWithValue("@cat", category);
                cnt = (int)cmd.ExecuteScalar();
            }

            if (cnt == 0) return 1;

            using (SqlCommand cmd = new SqlCommand(
                @"SELECT COUNT(*) FROM dbo.SMM_USERS_ACTIONS WITH(NOLOCK)
                  WHERE LoginID  = @lid AND Action = @act AND WhsCode = @whs
                    AND (Category = @cat OR UPPER(Category) = 'ALL')
                    AND Perm = 'Y'",
                _db.Conn))
            {
                cmd.Parameters.AddWithValue("@lid", loginId);
                cmd.Parameters.AddWithValue("@act", action);
                cmd.Parameters.AddWithValue("@whs", whsCode);
                cmd.Parameters.AddWithValue("@cat", category);
                cnt = (int)cmd.ExecuteScalar();
            }

            return cnt;
        }
        catch (Exception ex)
        {
            _db.Disconnect();
            throw new Exception("Error in GetUserActionsCount: " + ex.Message);
        }
    }

    // ── smm_login_validations ─────────────────────────────────────────────
    // Returns DataTable with ErrorId / ErrorMsg columns (same as SP result set).
    // textOut mirrors the SP @TextOut output parameter.
    public DataTable ValidateLogin(string loginId, string passwd,
        string companyId, out string textOut)
    {
        textOut = "";
        DataTable dt = new DataTable();
        dt.Columns.Add("ErrorId",  typeof(string));
        dt.Columns.Add("ErrorMsg", typeof(string));

        try
        {
            int cnt;

            // 1. Is the user active?
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT COUNT(*) FROM dbo.SMM_LOGIN WITH(NOLOCK)
                  WHERE LOGINID = @lid AND ACTIVE = 'Y'",
                _db.Conn))
            {
                cmd.Parameters.AddWithValue("@lid", loginId);
                cnt = (int)cmd.ExecuteScalar();
            }

            if (cnt == 0)
            {
                textOut = "Usuario Inactivo.";
                dt.Rows.Add("0", "Usuario Inactivo.");
                return dt;
            }

            // 2. Password correct?
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT COUNT(*) FROM dbo.SMM_LOGIN WITH(NOLOCK)
                  WHERE LOGINID = @lid AND PASSWD = @pwd",
                _db.Conn))
            {
                cmd.Parameters.AddWithValue("@lid", loginId);
                cmd.Parameters.AddWithValue("@pwd", passwd);
                cnt = (int)cmd.ExecuteScalar();
            }

            if (cnt == 0)
            {
                textOut = "Verifique Usuario Passwd.";
                dt.Rows.Add("0", "Verifique Usuario Passwd.");
                return dt;
            }

            // 3. User has access to this company?
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT COUNT(*) FROM dbo.SMM_LOGIN_COMPANY WITH(NOLOCK)
                  WHERE LOGINID = @lid AND CompanyId = @cid",
                _db.Conn))
            {
                cmd.Parameters.AddWithValue("@lid", loginId);
                cmd.Parameters.AddWithValue("@cid", companyId);
                cnt = (int)cmd.ExecuteScalar();
            }

            if (cnt == 0)
            {
                textOut = "Usuario No tiene permisos para esta compania.";
                dt.Rows.Add("0", "Usuario No tiene permisos para esta compania.");
                return dt;
            }

            textOut = "Login Okay.";

            // 4. Return controls / roles / permissions.
            //    Original SP always overwrites RoleId with 'ATOTAL' before branching,
            //    so the else branch is dead code — replicate the ATOTAL path only.
            const string sql =
                @"SELECT '1' AS ErrorId, 'No Errors.' AS ErrorMsg
                  FROM dbo.SMM_DUAL WITH(NOLOCK)
                  UNION
                  SELECT DISTINCT '2', cp.controlid
                  FROM dbo.smm_control_permissions cp WITH(NOLOCK)
                  UNION
                  SELECT DISTINCT '2', cp.controlid
                  FROM dbo.smm_control_permissions cp WITH(NOLOCK)
                  WHERE permissionid = 'ATOTAL'
                  UNION
                  SELECT DISTINCT '3', lt.roleid
                  FROM dbo.SMM_ROLES lt WITH(NOLOCK)
                  WHERE lt.roleid NOT IN ('READONLY')
                  UNION
                  SELECT DISTINCT '4', cp.PermissionId
                  FROM dbo.smm_control_permissions cp WITH(NOLOCK)";

            using (SqlCommand cmd = new SqlCommand(sql, _db.Conn))
            using (SqlDataAdapter ada = new SqlDataAdapter(cmd))
            {
                dt = new DataTable();
                ada.Fill(dt);
            }

            return dt;
        }
        catch (Exception ex)
        {
            _db.Disconnect();
            throw new Exception("Error in ValidateLogin: " + ex.Message);
        }
    }

    // ── S_Cia_iPrinters_ByUserAndSapCia ──────────────────────────────────
    // Returns DataTable with IpCode (IP:Port) and IpName columns.
    // Prefers user-specific printers; falls back to company-level printers.
    public DataTable GetPrinters(string userId, string sapCiaId)
    {
        DataTable dt = new DataTable();
        try
        {
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT [IP] + ':' + CONVERT(NVARCHAR(10), [Port]) AS IpCode,
                         Descr AS IpName
                  FROM dbo.Usr_iPrinters WITH(NOLOCK)
                  WHERE UserId = @uid AND [Active] = 1",
                _db.Conn))
            {
                cmd.Parameters.AddWithValue("@uid", userId);
                using (SqlDataAdapter ada = new SqlDataAdapter(cmd))
                    ada.Fill(dt);
            }

            if (dt.Rows.Count > 0) return dt;

            dt = new DataTable();
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT [IP] + ':' + CONVERT(NVARCHAR(10), [Port]) AS IpCode,
                         Descr AS IpName
                  FROM dbo.Cia_iPrinters WITH(NOLOCK)
                  WHERE [SapCiaId] = @cia AND [Active] = 1",
                _db.Conn))
            {
                cmd.Parameters.AddWithValue("@cia", sapCiaId);
                using (SqlDataAdapter ada = new SqlDataAdapter(cmd))
                    ada.Fill(dt);
            }

            return dt;
        }
        catch (Exception ex)
        {
            _db.Disconnect();
            throw new Exception("Error in GetPrinters: " + ex.Message);
        }
    }
}
