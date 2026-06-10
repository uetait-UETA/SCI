using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Replaces SPs: delete_TransXsap, delete_TransXsap_drf1, update_TransXsap_drf1,
/// update_discrep_drf1, la_update_transfer_errors, smm_insTransferXexcel_stg,
/// SMM_DELETE_DRAFT_TRANSFER, smm_insert_Transdiscrep_audit_odrf,
/// Smm_Get_DispCompleted_Prc, Smm_ValDispatching_Order_Prc, Smm_ValReciving_Order_Prc,
/// SMM_GET_LOGIN_WHS_TYPE_PRC, reseq_TransXsap_drf1, smm_populate_discrep_odrf
/// </summary>
public class TransferRepository
{
    private readonly SqlDb _db;

    public TransferRepository(SqlDb db)
    {
        _db = db;
    }

    // ── delete_TransXsap ─────────────────────────────────────────────────
    public void DeleteTransfer(string companyId, int docEntry)
    {
        using (SqlTransaction tx = _db.Conn.BeginTransaction())
        {
            try
            {
                Exec(tx, "DELETE FROM dbo.smm_TransXsap_drf1 WHERE CompanyId=@cid AND DocEntry=@de", companyId, docEntry);
                Exec(tx, "DELETE FROM dbo.smm_TransXsap_odrf WHERE CompanyId=@cid AND DocEntry=@de", companyId, docEntry);
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }

    // ── delete_TransXsap_drf1 ────────────────────────────────────────────
    public void DeleteTransferZeroLines(string companyId, int docEntry)
    {
        using (SqlTransaction tx = _db.Conn.BeginTransaction())
        {
            try
            {
                Exec(tx,
                    "DELETE FROM dbo.smm_TransXsap_drf1 WHERE CompanyId=@cid AND DocEntry=@de AND DraftQuantity=0",
                    companyId, docEntry);
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }

    // ── update_TransXsap_drf1 ────────────────────────────────────────────
    public void UpdateTransferLineQty(string companyId, int docEntry, int lineNum, int qty)
    {
        using (SqlTransaction tx = _db.Conn.BeginTransaction())
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE dbo.smm_TransXsap_drf1
                      SET DraftQuantity=@qty, TmpQuantity=@qty
                      WHERE CompanyId=@cid AND DocEntry=@de AND LineNum=@ln",
                    _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@qty", qty);
                    cmd.Parameters.AddWithValue("@cid", companyId);
                    cmd.Parameters.AddWithValue("@de",  docEntry);
                    cmd.Parameters.AddWithValue("@ln",  lineNum);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }

    // ── update_discrep_drf1 ──────────────────────────────────────────────
    public void UpdateDiscrepLineQty(string companyId, int docEntry, int lineNum, int tmpQty)
    {
        using (SqlTransaction tx = _db.Conn.BeginTransaction())
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE smm_Transdiscrep_drf1 WITH(ROWLOCK)
                      SET TmpQuantity=@qty
                      WHERE CompanyId=@cid AND DocEntry=@de AND LineNum=@ln",
                    _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@qty", tmpQty);
                    cmd.Parameters.AddWithValue("@cid", companyId);
                    cmd.Parameters.AddWithValue("@de",  docEntry);
                    cmd.Parameters.AddWithValue("@ln",  lineNum);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }

    // ── la_update_transfer_errors ────────────────────────────────────────
    public void UpdateTransferError(int docEntry, int lineNum, string fixedFlag)
    {
        using (SqlTransaction tx = _db.Conn.BeginTransaction())
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE la_transfer_errors WITH(ROWLOCK)
                      SET Fixed=@fixed
                      WHERE DocEntryOri=@de AND Line=@ln",
                    _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@fixed", fixedFlag);
                    cmd.Parameters.AddWithValue("@de",    docEntry);
                    cmd.Parameters.AddWithValue("@ln",    lineNum);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }

    // ── smm_insTransferXexcel_stg ────────────────────────────────────────
    public void InsertTransferFromExcel(int docEntry, int lineNum, string fromWhs, string toWhs,
        string itemCode, string itemName, string user, int qty)
    {
        using (SqlTransaction tx = _db.Conn.BeginTransaction())
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO smm_TransferXexcel_stg WITH(ROWLOCK)
                      (DocEntry,LineNum,FromWhsCode,ToWhsCode,ItemCode,ItemNameXls,Quantity,Date_Created,Created_By)
                      VALUES (@de,@ln,@from,@to,@item,@name,@qty,GETDATE(),@user)",
                    _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@de",   docEntry);
                    cmd.Parameters.AddWithValue("@ln",   lineNum);
                    cmd.Parameters.AddWithValue("@from", fromWhs);
                    cmd.Parameters.AddWithValue("@to",   toWhs);
                    cmd.Parameters.AddWithValue("@item", itemCode);
                    cmd.Parameters.AddWithValue("@name", itemName);
                    cmd.Parameters.AddWithValue("@qty",  qty);
                    cmd.Parameters.AddWithValue("@user", user);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }

    // ── SMM_DELETE_DRAFT_TRANSFER ────────────────────────────────────────
    // Moves all transfer data to _bads archive tables, then deletes originals
    public void DeleteDraftTransfer(string companyId, int docEntry, string userDelete)
    {
        using (SqlTransaction tx = _db.Conn.BeginTransaction())
        {
            try
            {
                // smm_Transdiscrep_drf1
                Exec(tx, @"INSERT INTO smm_Transdiscrep_drf1_bads WITH(ROWLOCK)
                    (DocEntry,LineNum,DocNum,ToWhsCode,ToWhsName,ItemCode,ItemName,
                     DraftQuantity,DispatchQuantity,ReceivedQuantity,Price,TmpQuantity,Date_Created,Created_By)
                    SELECT DocEntry,LineNum,DocNum,ToWhsCode,ToWhsName,ItemCode,ItemName,
                     DraftQuantity,DispatchQuantity,ReceivedQuantity,Price,TmpQuantity,Date_Created,Created_By
                    FROM smm_Transdiscrep_drf1 WITH(NOLOCK) WHERE CompanyId=@cid AND DocEntry=@de",
                    companyId, docEntry);
                Exec(tx, "DELETE smm_Transdiscrep_drf1 WITH(ROWLOCK) WHERE CompanyId=@cid AND DocEntry=@de",
                    companyId, docEntry);

                // smm_Transdiscrep_odrf
                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO smm_Transdiscrep_odrf_bads WITH(ROWLOCK)
                      (DocEntry,DocNum,DocDate,FromWhsCode,FromWhsName,ToWhsCode,ToWhsName,DocStatus,
                       Dispatched,DispCompleted,Received,ReceCompleted,
                       DocEntryTraDis,DocNumTraDis,DocEntryTraRec,DocNumTraRec,
                       DocEntryTraRec2,DocNumTraRec2,UserDispatch,UserReceive,
                       Date_Created,Created_By,UserDelTransfer)
                      SELECT DocEntry,DocNum,DocDate,FromWhsCode,FromWhsName,ToWhsCode,ToWhsName,DocStatus,
                       Dispatched,DispCompleted,Received,ReceCompleted,
                       DocEntryTraDis,DocNumTraDis,DocEntryTraRec,DocNumTraRec,
                       DocEntryTraRec2,DocNumTraRec2,UserDispatch,UserReceive,
                       Date_Created,Created_By,@udel
                      FROM smm_Transdiscrep_odrf WITH(NOLOCK) WHERE CompanyId=@cid AND DocEntry=@de",
                    _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@udel", userDelete);
                    cmd.Parameters.AddWithValue("@cid",  companyId);
                    cmd.Parameters.AddWithValue("@de",   docEntry);
                    cmd.ExecuteNonQuery();
                }
                Exec(tx, "DELETE smm_Transdiscrep_odrf WITH(ROWLOCK) WHERE CompanyId=@cid AND DocEntry=@de",
                    companyId, docEntry);

                // smm_TransXsap_drf1
                Exec(tx, @"INSERT INTO smm_TransXsap_drf1_bads WITH(ROWLOCK)
                    (DocEntry,LineNum,DocNum,ToWhsCode,ToWhsName,ItemCode,ItemName,
                     DraftQuantity,DispatchQuantity,ReceivedQuantity,Price,TmpQuantity,Date_Created,Created_By)
                    SELECT DocEntry,LineNum,DocNum,ToWhsCode,ToWhsName,ItemCode,ItemName,
                     DraftQuantity,DispatchQuantity,ReceivedQuantity,Price,TmpQuantity,Date_Created,Created_By
                    FROM smm_TransXsap_drf1 WITH(NOLOCK) WHERE CompanyId=@cid AND DocEntry=@de",
                    companyId, docEntry);
                Exec(tx, "DELETE smm_TransXsap_drf1 WITH(ROWLOCK) WHERE CompanyId=@cid AND DocEntry=@de",
                    companyId, docEntry);

                // smm_TransXsap_odrf
                Exec(tx, @"INSERT INTO smm_TransXsap_odrf_bads WITH(ROWLOCK)
                    (DocEntry,DocNum,DocDate,FromWhsCode,FromWhsName,ToWhsCode,ToWhsName,DocStatus,
                     Dispatched,DispCompleted,Received,ReceCompleted,
                     DocEntryTraDis,DocNumTraDis,DocEntryTraRec,DocNumTraRec,
                     DocEntryTraRec2,DocNumTraRec2,UserDispatch,UserReceive,Date_Created,Created_By)
                    SELECT DocEntry,DocNum,DocDate,FromWhsCode,FromWhsName,ToWhsCode,ToWhsName,DocStatus,
                     Dispatched,DispCompleted,Received,ReceCompleted,
                     DocEntryTraDis,DocNumTraDis,DocEntryTraRec,DocNumTraRec,
                     DocEntryTraRec2,DocNumTraRec2,UserDispatch,UserReceive,Date_Created,Created_By
                    FROM smm_TransXsap_odrf WITH(NOLOCK) WHERE CompanyId=@cid AND DocEntry=@de",
                    companyId, docEntry);
                Exec(tx, "DELETE smm_TransXsap_odrf WITH(ROWLOCK) WHERE CompanyId=@cid AND DocEntry=@de",
                    companyId, docEntry);

                // SMM_drf1 (draft lines)
                Exec(tx, @"INSERT INTO drf1_bads WITH(ROWLOCK)
                    (DocEntry,LineNum,TargetType,ItemCode,Dscription,Quantity,Price,
                     Currency,Rate,LineTotal,TotalFrgn,SerialNum,WhsCode,DocDate,OcrCode,[U_TE])
                    SELECT DocEntry,LineNum,TargetType,ItemCode,Dscription,Quantity,Price,
                     Currency,Rate,LineTotal,TotalFrgn,SerialNum,WhsCode,DocDate,OcrCode,'XXX'
                    FROM Smm_drf1 WITH(NOLOCK) WHERE CompanyId=@cid AND DocEntry=@de",
                    companyId, docEntry);
                Exec(tx, "DELETE Smm_drf1 WITH(ROWLOCK) WHERE CompanyId=@cid AND DocEntry=@de",
                    companyId, docEntry);

                // SMM_odrf (draft header)
                Exec(tx, @"INSERT INTO odrf_bads WITH(ROWLOCK)
                    (docentry,DocNum,DocType,CANCELED,DocStatus,DocDate,DocDueDate,
                     CardCode,CardName,DocCur,DocRate,DocTotal,DocTotalFC,
                     Ref1,Ref2,Comments,CreateDate,Series,Filler,
                     FromDate,ToDate,U_RECEIVE,U_DESPATCH,U_ORITOWHS,DocTime,ObjType)
                    SELECT docentry,DocNum,DocType,CANCELED,DocStatus,DocDate,DocDueDate,
                     CardCode,CardName,DocCur,DocRate,DocTotal,DocTotalFC,
                     Ref1,Ref2,Comments,CreateDate,Series,Filler,
                     FromDate,ToDate,U_RECEIVE,U_DESPATCH,U_ORITOWHS,DocTime,ObjType
                    FROM SMM_odrf WITH(NOLOCK) WHERE CompanyId=@cid AND DocEntry=@de",
                    companyId, docEntry);
                Exec(tx, "DELETE SMM_odrf WITH(ROWLOCK) WHERE CompanyId=@cid AND DocEntry=@de",
                    companyId, docEntry);

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }

    // ── smm_insert_Transdiscrep_audit_odrf ──────────────────────────────
    public void UpsertTransferAudit(string companyId, int docEntry, string typeTrans, string sourceTrans)
    {
        using (SqlTransaction tx = _db.Conn.BeginTransaction())
        {
            try
            {
                int count;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM smm_Transdiscrep_audit_odrf WITH(NOLOCK) WHERE CompanyId=@cid AND DocEntry=@de",
                    _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@cid", companyId);
                    cmd.Parameters.AddWithValue("@de",  docEntry);
                    count = (int)cmd.ExecuteScalar();
                }

                string sqlAudit = count == 0
                    ? (typeTrans == "D"
                        ? @"INSERT INTO smm_Transdiscrep_audit_odrf WITH(ROWLOCK)
                            (CompanyId,DocEntry,DispatchDate,ReceiveDate,Date_Created,DispatchType)
                            VALUES (@cid,@de,GETDATE(),NULL,GETDATE(),@src)"
                        : @"INSERT INTO smm_Transdiscrep_audit_odrf WITH(ROWLOCK)
                            (CompanyId,DocEntry,DispatchDate,ReceiveDate,Date_Created,ReceiveType)
                            VALUES (@cid,@de,NULL,GETDATE(),GETDATE(),@src)")
                    : (typeTrans == "D"
                        ? "UPDATE smm_Transdiscrep_audit_odrf WITH(ROWLOCK) SET DispatchDate=GETDATE(),DispatchType=@src WHERE CompanyId=@cid AND DocEntry=@de"
                        : "UPDATE smm_Transdiscrep_audit_odrf WITH(ROWLOCK) SET ReceiveDate=GETDATE(),ReceiveType=@src WHERE CompanyId=@cid AND DocEntry=@de");

                using (SqlCommand cmd = new SqlCommand(sqlAudit, _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@src", sourceTrans);
                    cmd.Parameters.AddWithValue("@cid", companyId);
                    cmd.Parameters.AddWithValue("@de",  docEntry);
                    cmd.ExecuteNonQuery();
                }

                string sqlOdrf = typeTrans == "D"
                    ? "UPDATE smm_Transdiscrep_odrf WITH(ROWLOCK) SET DispatchType=@src WHERE CompanyId=@cid AND DocEntry=@de"
                    : "UPDATE smm_Transdiscrep_odrf WITH(ROWLOCK) SET ReceiveType=@src WHERE CompanyId=@cid AND DocEntry=@de";

                using (SqlCommand cmd = new SqlCommand(sqlOdrf, _db.Conn, tx))
                {
                    cmd.Parameters.AddWithValue("@src", sourceTrans);
                    cmd.Parameters.AddWithValue("@cid", companyId);
                    cmd.Parameters.AddWithValue("@de",  docEntry);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
        }
    }

    // ── Smm_Get_DispCompleted_Prc ────────────────────────────────────────
    public string GetDispatchCompleted(string companyId, int docEntry)
    {
        using (SqlCommand cmd = new SqlCommand(
            "SELECT ISNULL(DispCompleted,'N') FROM smm_transdiscrep_odrf WITH(NOLOCK) WHERE CompanyId=@cid AND DocEntry=@de",
            _db.Conn))
        {
            cmd.Parameters.AddWithValue("@cid", companyId);
            cmd.Parameters.AddWithValue("@de",  docEntry);
            object val = cmd.ExecuteScalar();
            return val != null ? val.ToString() : "N";
        }
    }

    // ── Smm_ValDispatching_Order_Prc / Smm_ValReciving_Order_Prc ────────
    // Both SPs had identical logic - one method covers both
    public string ValidateOrderQty(string companyId, int docEntry)
    {
        using (SqlCommand cmd = new SqlCommand(
            "SELECT COUNT(1) FROM smm_Transdiscrep_drf1 WITH(NOLOCK) WHERE CompanyId=@cid AND DocEntry=@de AND TmpQuantity=0",
            _db.Conn))
        {
            cmd.Parameters.AddWithValue("@cid", companyId);
            cmd.Parameters.AddWithValue("@de",  docEntry);
            int count = (int)cmd.ExecuteScalar();
            return count > 0
                ? "This order has zero quantities. Please review and/or check \"Accept Zero Quantities\"."
                : "Order is valid.";
        }
    }

    // ── SMM_GET_LOGIN_WHS_TYPE_PRC ───────────────────────────────────────
    public DataTable GetLoginWarehouseType(string loginId, int docEntry, string companyId)
    {
        string loginTypeWhs;
        using (SqlCommand cmd = new SqlCommand(
            "SELECT ISNULL(MAX(TypeWhs),'NOSETUP') FROM smm_login WITH(NOLOCK) WHERE LoginID=@lid",
            _db.Conn))
        {
            cmd.Parameters.AddWithValue("@lid", loginId);
            loginTypeWhs = cmd.ExecuteScalar().ToString();
        }

        string fromWhs, toWhs, dispatched, received;
        using (SqlCommand cmd = new SqlCommand(
            @"SELECT FromWhsCode, ToWhsCode, ISNULL(Dispatched,'N'), ISNULL(Received,'N')
              FROM smm_Transdiscrep_odrf WITH(NOLOCK)
              WHERE DocEntry=@de AND CompanyId=@cid", _db.Conn))
        {
            cmd.Parameters.AddWithValue("@de",  docEntry);
            cmd.Parameters.AddWithValue("@cid", companyId);
            using (SqlDataReader rdr = cmd.ExecuteReader())
            {
                rdr.Read();
                fromWhs    = rdr[0].ToString();
                toWhs      = rdr[1].ToString();
                dispatched = rdr[2].ToString();
                received   = rdr[3].ToString();
            }
        }

        string whsToCheck = (dispatched == "N" && received == "N") ? fromWhs : toWhs;

        string typeWhs;
        using (SqlCommand cmd = new SqlCommand(
            "SELECT ISNULL(MAX(TypeWhs),'NOSETUP') FROM SMM_WHSTYPE WITH(NOLOCK) WHERE WHSCODE=@whs AND CompanyId=@cid",
            _db.Conn))
        {
            cmd.Parameters.AddWithValue("@whs", whsToCheck);
            cmd.Parameters.AddWithValue("@cid", companyId);
            typeWhs = cmd.ExecuteScalar().ToString();
        }

        if (typeWhs == "BODTIE") typeWhs = loginTypeWhs;

        DataTable dt = new DataTable();
        dt.Columns.Add("loginTypeWhs");
        dt.Columns.Add("TypeWhs");
        DataRow row = dt.NewRow();
        row["loginTypeWhs"] = loginTypeWhs;
        row["TypeWhs"]      = typeWhs;
        dt.Rows.Add(row);
        return dt;
    }

    // ── reseq_TransXsap_drf1 ─────────────────────────────────────────────
    // Replaces cursor with C# loop - renumbers lines 0, 1, 2...
    public void ResequenceTransferLines(string companyId, int docEntry)
    {
        List<int> lines = new List<int>();
        using (SqlCommand cmd = new SqlCommand(
            "SELECT LineNum FROM smm_TransXsap_drf1 WITH(NOLOCK) WHERE CompanyId=@cid AND DocEntry=@de ORDER BY LineNum",
            _db.Conn))
        {
            cmd.Parameters.AddWithValue("@cid", companyId);
            cmd.Parameters.AddWithValue("@de",  docEntry);
            using (SqlDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read()) lines.Add(rdr.GetInt32(0));
            }
        }

        int newLine = 0;
        foreach (int origLine in lines)
        {
            using (SqlTransaction tx = _db.Conn.BeginTransaction())
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand(
                        @"UPDATE smm_TransXsap_drf1 WITH(ROWLOCK) SET LineNum=@nl
                          WHERE CompanyId=@cid AND DocEntry=@de AND LineNum=@ol",
                        _db.Conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@nl",  newLine);
                        cmd.Parameters.AddWithValue("@cid", companyId);
                        cmd.Parameters.AddWithValue("@de",  docEntry);
                        cmd.Parameters.AddWithValue("@ol",  origLine);
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                    newLine++;
                }
                catch { tx.Rollback(); throw; }
            }
        }
    }

    // ── smm_populate_discrep_odrf ────────────────────────────────────────
    // Copies draft from SMM_ODRF into discrepancy tables (header + lines)
    public void PopulateDiscrepancy(string companyId, int docEntry)
    {
        int headerCount;
        using (SqlCommand cmd = new SqlCommand(
            "SELECT COUNT(1) FROM smm_Transdiscrep_odrf WITH(NOLOCK) WHERE DocEntry=@de AND CompanyId=@cid",
            _db.Conn))
        {
            cmd.Parameters.AddWithValue("@de",  docEntry);
            cmd.Parameters.AddWithValue("@cid", companyId);
            headerCount = (int)cmd.ExecuteScalar();
        }

        if (headerCount == 0)
        {
            string sqlHeader = string.Format(@"
                INSERT INTO smm_Transdiscrep_odrf WITH(ROWLOCK)
                (CompanyId,DocEntry,DocNum,DocDate,FromWhsCode,FromWhsName,ToWhsCode,ToWhsName,
                 DocStatus,Dispatched,DispCompleted,Received,ReceCompleted,
                 DocEntryTraDis,DocNumTraDis,DocEntryTraRec,DocNumTraRec,Date_Created,Created_By)
                SELECT a.CompanyId, a.DocEntry, a.DocNum,
                    CONVERT(varchar,a.DocDate,101)+' '+
                    CASE WHEN DocTime<1000
                         THEN '0'+LEFT(CONVERT(varchar,a.DocTime),1)+':'+RIGHT(CONVERT(varchar,a.DocTime),2)
                         ELSE LEFT(CONVERT(varchar,a.DocTime),2)+':'+RIGHT(CONVERT(varchar,a.DocTime),2) END,
                    a.Filler, g.WhsName, ToWhs.ToWhsCode, ToWhs.ToWhsName,
                    A.DOCSTATUS,'N',NULL,'N',NULL,NULL,NULL,NULL,NULL,GETDATE(),NULL
                FROM SMM_ODRF AS a WITH(NOLOCK)
                INNER JOIN {0}.dbo.OWHS AS g WITH(NOLOCK) ON a.Filler=g.WhsCode,
                (SELECT MAX(b.WhsCode) ToWhsCode, MAX(f.WhsName) ToWhsName
                 FROM SMM_ODRF AS a WITH(NOLOCK)
                 INNER JOIN SMM_DRF1 AS b WITH(NOLOCK) ON a.DocEntry=b.DocEntry AND a.CompanyId=b.CompanyId
                 INNER JOIN {0}.dbo.OWHS AS f WITH(NOLOCK) ON b.WhsCode=f.WhsCode
                 WHERE a.DocEntry={1} AND a.CompanyId='{0}') ToWhs
                WHERE a.DocEntry={1} AND a.CompanyId='{0}'", companyId, docEntry);

            using (SqlCommand cmd = new SqlCommand(sqlHeader, _db.Conn))
                cmd.ExecuteNonQuery();
        }

        int linesCount;
        using (SqlCommand cmd = new SqlCommand(
            "SELECT COUNT(1) FROM smm_Transdiscrep_drf1 WITH(NOLOCK) WHERE DocEntry=@de AND CompanyId=@cid",
            _db.Conn))
        {
            cmd.Parameters.AddWithValue("@de",  docEntry);
            cmd.Parameters.AddWithValue("@cid", companyId);
            linesCount = (int)cmd.ExecuteScalar();
        }

        if (linesCount == 0)
        {
            string sqlLines = string.Format(@"
                INSERT INTO smm_Transdiscrep_drf1 WITH(ROWLOCK)
                (CompanyId,DocEntry,LineNum,DocNum,ToWhsCode,ToWhsName,ItemCode,ItemName,
                 DraftQuantity,DispatchQuantity,ReceivedQuantity,TmpQuantity,Price,Date_Created,Created_By)
                SELECT a.CompanyId,a.docentry,b.LineNum,a.DocNum,b.WhsCode,f.WhsName,
                    b.ItemCode,b.Dscription,b.Quantity,b.Quantity,b.Quantity,b.Quantity,
                    ISNULL(e.Price,0),GETDATE(),NULL
                FROM SMM_ODRF AS a WITH(NOLOCK)
                INNER JOIN SMM_DRF1 AS b WITH(NOLOCK) ON a.DocEntry=b.DocEntry AND a.CompanyId=b.CompanyId
                INNER JOIN {0}.dbo.OWHS AS f WITH(NOLOCK) ON b.WhsCode=f.WhsCode
                LEFT JOIN  {0}.dbo.ITM1 AS e WITH(NOLOCK) ON b.ItemCode=e.ItemCode AND e.PriceList=1
                WHERE a.DocEntry={1} AND a.CompanyId='{0}'", companyId, docEntry);

            using (SqlCommand cmd = new SqlCommand(sqlLines, _db.Conn))
                cmd.ExecuteNonQuery();
        }
    }

    // ── Private helper ───────────────────────────────────────────────────
    private void Exec(SqlTransaction tx, string sql, string companyId, int docEntry)
    {
        using (SqlCommand cmd = new SqlCommand(sql, _db.Conn, tx))
        {
            cmd.Parameters.AddWithValue("@cid", companyId);
            cmd.Parameters.AddWithValue("@de",  docEntry);
            cmd.ExecuteNonQuery();
        }
    }
}
