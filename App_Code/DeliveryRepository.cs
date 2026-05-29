using System;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Replaces SPs: SMM_UpdateDeliveryItemCode_PRC, SMM_UpdateDeliveryItemNumber_PRC,
/// sp_ordenesProblem
/// </summary>
public class DeliveryRepository
{
    private readonly SqlDb _db;

    public DeliveryRepository(SqlDb db)
    {
        _db = db;
    }

    // ── SMM_UpdateDeliveryItemCode_PRC ────────────────────────────────────
    // Updates skunum in la_store_sales for all records matching sku+storeNum
    // that are linked through la_delivery_errors and have no delivery doc yet.
    public void UpdateDeliveryItemCode(string sku, string newSku,
        string whsCode, string sapDb, out int result, out string message)
    {
        result  = 0;
        message = "";

        using (SqlTransaction tx = _db.Conn.BeginTransaction())
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE dbo.la_store_sales
                      SET skunum = @newSku
                      WHERE DeliveryDocNum IS NULL
                        AND id IN (
                            SELECT ID FROM dbo.la_delivery_errors WITH(NOLOCK)
                            WHERE skunum        = @sku
                              AND DeliveryDocNum IS NULL
                              AND CompanyId      = @db
                              AND storenum       = @store
                        )",
                    _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@newSku", newSku);
                    cmd.Parameters.AddWithValue("@sku",    sku);
                    cmd.Parameters.AddWithValue("@db",     sapDb);
                    cmd.Parameters.AddWithValue("@store",  whsCode);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                result  = 1;
                message = "COMMIT";
            }
            catch (Exception ex)
            {
                tx.Rollback();
                message = ex.Message;
            }
        }
    }

    // ── SMM_UpdateDeliveryItemNumber_PRC ──────────────────────────────────
    // Updates skunum on a specific la_store_sales record (by ID) and also
    // updates any linked return record matched by albaran / serie / itemnum.
    public void UpdateDeliveryItemNumber(int id, string newSku,
        string sapDb, out int result, out string message)
    {
        result  = 0;
        message = "";

        string currentNumSerie   = "";
        int    currentNumAlbaran = 0;
        int    itemNum           = 0;

        try
        {
            using (SqlCommand cmd = new SqlCommand(
                @"SELECT itemnum, NUMALBARAN, NUMSERIE
                  FROM dbo.la_store_sales WITH(NOLOCK)
                  WHERE ID = @id
                    AND CompanyId = CASE WHEN @db = '' THEN CompanyId ELSE @db END",
                _db.Conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@db", sapDb);
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        itemNum           = Convert.ToInt32(rdr[0]);
                        currentNumAlbaran = Convert.ToInt32(rdr[1]);
                        currentNumSerie   = rdr[2].ToString();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return;
        }

        using (SqlTransaction tx = _db.Conn.BeginTransaction())
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE dbo.la_store_sales WITH(ROWLOCK)
                      SET skunum = @newSku
                      WHERE ID = @id
                        AND CompanyId = CASE WHEN @db = '' THEN CompanyId ELSE @db END",
                    _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@newSku", newSku);
                    cmd.Parameters.AddWithValue("@id",     id);
                    cmd.Parameters.AddWithValue("@db",     sapDb);
                    cmd.ExecuteNonQuery();
                }

                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE dbo.la_store_sales WITH(ROWLOCK)
                      SET skunum = @newSku
                      WHERE CompanyId          = CASE WHEN @db = '' THEN CompanyId ELSE @db END
                        AND ABONODE_NUMALBARAN = @albaran
                        AND ABONODE_NUMSERIE   = @serie
                        AND itemnum            = @itemNum",
                    _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@newSku",  newSku);
                    cmd.Parameters.AddWithValue("@db",      sapDb);
                    cmd.Parameters.AddWithValue("@albaran", currentNumAlbaran);
                    cmd.Parameters.AddWithValue("@serie",   currentNumSerie);
                    cmd.Parameters.AddWithValue("@itemNum", itemNum);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                result  = 1;
                message = "COMMIT";
            }
            catch (Exception ex)
            {
                tx.Rollback();
                message = ex.Message;
            }
        }
    }

    // ── sp_ordenesProblem ────────────────────────────────────────────────
    public DataTable GetOrdersWithProblems(string companyId, DateTime? period)
    {
        DataTable dt = new DataTable();
        try
        {
            string sql;
            if (period.HasValue)
            {
                sql = @"SELECT a.ReceivingId, a.OriClient, a.DesCompanyId,
                               a.OSDocNum, a.ErrorPosted, a.Created_Date
                        FROM dbo.TOCLEC_RECEIVING_ERRORS_POSTED a WITH(NOLOCK)
                        WHERE a.DesCompanyId = @cid
                          AND a.created_date >= @period
                          AND a.created_date  < DATEADD(DD, 1, @period)";
            }
            else
            {
                sql = @"SELECT a.ReceivingId, a.OriClient, a.DesCompanyId,
                               a.OSDocNum, a.ErrorPosted, a.Created_Date
                        FROM dbo.TOCLEC_RECEIVING_ERRORS_POSTED a WITH(NOLOCK)
                        WHERE a.DesCompanyId = @cid";
            }

            using (SqlCommand cmd = new SqlCommand(sql, _db.Conn))
            {
                cmd.Parameters.AddWithValue("@cid", companyId);
                if (period.HasValue)
                    cmd.Parameters.AddWithValue("@period", period.Value);
                dt.Load(cmd.ExecuteReader());
            }
        }
        catch (Exception ex)
        {
            _db.Disconnect();
            throw new Exception("Error in GetOrdersWithProblems: " + ex.Message);
        }
        return dt;
    }
}
