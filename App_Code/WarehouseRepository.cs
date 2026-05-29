using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Replaces SPs: Whs_DelBinsExcel_stg, Whs_UploadBinsExcel_stg,
/// SMM_UPLOAD_MINMAX, Item_Bin_Gen_Prc
/// </summary>
public class WarehouseRepository
{
    private readonly SqlDb _db;

    public WarehouseRepository(SqlDb db)
    {
        _db = db;
    }

    // ── Whs_DelBinsExcel_stg ─────────────────────────────────────────────
    public void DeleteBinsExcelStg(string whsCode)
    {
        using (SqlTransaction tx = _db.Conn.BeginTransaction())
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(
                    "DELETE dbo.Whs_itemsBinsExcel_stg WITH(ROWLOCK) WHERE WhsCode = @whs",
                    _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@whs", whsCode);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }

    // ── Whs_UploadBinsExcel_stg ──────────────────────────────────────────
    // sapDb: SAP B1 company DB used to look up item details (Session["CompanyId"]).
    // Original SP hardcoded NEWPCANA; pass the actual company DB instead.
    public void UploadBinExcelStg(string whsCode, int lineNum, string itemCode,
        string bin, string userSap, string sapDb)
    {
        using (SqlTransaction tx = _db.Conn.BeginTransaction())
        {
            try
            {
                string itemName      = "NO_EXISTE";
                string itemGrpName   = "NO_EXISTE";
                string itemBrandName = "NO_EXISTE";

                string lookupSql = string.Format(
                    @"SELECT ISNULL(MAX(tm.itemname),     'NO_EXISTE'),
                             ISNULL(MAX(tb.ItmsGrpNam),   'NO_EXISTE'),
                             ISNULL(MAX(tm.u_brand),       'NO_EXISTE')
                      FROM {0}..oitm  tm WITH(NOLOCK),
                           {0}..OITB  tb WITH(NOLOCK)
                      WHERE tm.ItmsGrpCod = tb.ItmsGrpCod
                        AND tm.itemcode   = @itemCode",
                    sapDb);

                using (SqlCommand cmd = new SqlCommand(lookupSql, _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@itemCode", itemCode);
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            itemName      = rdr[0].ToString();
                            itemGrpName   = rdr[1].ToString();
                            itemBrandName = rdr[2].ToString();
                        }
                    }
                }

                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO dbo.Whs_itemsBinsExcel_stg WITH(ROWLOCK)
                        (WhsCode, Linenum, ItemCode, ItemName, ItemGrpName,
                         ItemBrandName, Bin, Date_Created, Created_By)
                      VALUES
                        (@whs, @ln, @item, @iName, @grp, @brand, @bin, GETDATE(), @usr)",
                    _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@whs",   whsCode);
                    cmd.Parameters.AddWithValue("@ln",    lineNum);
                    cmd.Parameters.AddWithValue("@item",  itemCode);
                    cmd.Parameters.AddWithValue("@iName", itemName);
                    cmd.Parameters.AddWithValue("@grp",   itemGrpName);
                    cmd.Parameters.AddWithValue("@brand", itemBrandName);
                    cmd.Parameters.AddWithValue("@bin",   bin);
                    cmd.Parameters.AddWithValue("@usr",   userSap);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }

    // ── SMM_UPLOAD_MINMAX ────────────────────────────────────────────────
    // Returns "Item inserted" or "Item Updated" (mirrors SP SELECT result).
    public string UpsertMinMax(string companyId, string item, string loc,
        int min, int max, string user)
    {
        using (SqlTransaction tx = _db.Conn.BeginTransaction())
        {
            try
            {
                int cnt;
                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT COUNT(*) FROM dbo.rss_store_item_min_max WITH(NOLOCK)
                      WHERE item = @item AND loc = @loc AND CompanyId = @cid",
                    _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@item", item);
                    cmd.Parameters.AddWithValue("@loc",  loc);
                    cmd.Parameters.AddWithValue("@cid",  companyId);
                    cnt = (int)cmd.ExecuteScalar();
                }

                string msg;
                if (cnt == 0)
                {
                    using (SqlCommand cmd = new SqlCommand(
                        @"INSERT INTO dbo.rss_store_item_min_max WITH(ROWLOCK)
                            (CompanyId, ITEM, LOC, MIN_QTY, MAX_QTY,
                             CREATED_BY, DATE_CREATED, HOLD)
                          VALUES
                            (@cid, @item, @loc, @min, @max, @usr, GETDATE(), 0)",
                        _db.Conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@cid",  companyId);
                        cmd.Parameters.AddWithValue("@item", item);
                        cmd.Parameters.AddWithValue("@loc",  loc);
                        cmd.Parameters.AddWithValue("@min",  min);
                        cmd.Parameters.AddWithValue("@max",  max);
                        cmd.Parameters.AddWithValue("@usr",  user);
                        cmd.ExecuteNonQuery();
                    }
                    msg = "Item inserted";
                }
                else
                {
                    using (SqlCommand cmd = new SqlCommand(
                        @"UPDATE dbo.rss_store_item_min_max WITH(ROWLOCK)
                          SET MIN_QTY      = @min,
                              MAX_QTY      = @max,
                              UPDATED_BY   = @usr,
                              DATE_UPDATED = GETDATE()
                          WHERE item = @item AND loc = @loc AND CompanyId = @cid",
                        _db.Conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@min",  min);
                        cmd.Parameters.AddWithValue("@max",  max);
                        cmd.Parameters.AddWithValue("@usr",  user);
                        cmd.Parameters.AddWithValue("@item", item);
                        cmd.Parameters.AddWithValue("@loc",  loc);
                        cmd.Parameters.AddWithValue("@cid",  companyId);
                        cmd.ExecuteNonQuery();
                    }
                    msg = "Item Updated";
                }

                tx.Commit();
                return msg;
            }
            catch { tx.Rollback(); throw; }
        }
    }

    // ── Item_Bin_Gen_Prc ─────────────────────────────────────────────────
    // Reads all bins for the item/whs from WMS_Whs_Item_Bin, concatenates
    // them semicolon-separated, then upserts into WMS_Item_Bins_Cons.
    // Replaces the T-SQL CURSOR with a C# List<string> loop.
    public void GenerateItemBins(string whsCode, string itemCode, string user)
    {
        List<string> bins = new List<string>();

        using (SqlCommand cmd = new SqlCommand(
            @"SELECT DISTINCT Bin FROM dbo.WMS_Whs_Item_Bin WITH(NOLOCK)
              WHERE WhsCode  = @whs AND ItemCode = @item
              ORDER BY Bin",
            _db.Conn))
        {
            cmd.Parameters.AddWithValue("@whs",  whsCode);
            cmd.Parameters.AddWithValue("@item", itemCode);
            using (SqlDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                    bins.Add(rdr[0].ToString());
            }
        }

        if (bins.Count == 0) return;

        string binsStr = string.Join(";", bins);

        using (SqlTransaction tx = _db.Conn.BeginTransaction())
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(
                    @"DELETE dbo.WMS_Item_Bins_Cons
                      WHERE Whscode  = @whs AND ItemCode = @item",
                    _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@whs",  whsCode);
                    cmd.Parameters.AddWithValue("@item", itemCode);
                    cmd.ExecuteNonQuery();
                }

                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO dbo.WMS_Item_Bins_Cons
                        (Whscode, ItemCode, BINS,
                         created_by, date_created, updated_by, date_updated)
                      VALUES
                        (@whs, @item, @bins, @usr, GETDATE(), @usr, GETDATE())",
                    _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@whs",  whsCode);
                    cmd.Parameters.AddWithValue("@item", itemCode);
                    cmd.Parameters.AddWithValue("@bins", binsStr);
                    cmd.Parameters.AddWithValue("@usr",  user);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }
}
