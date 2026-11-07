/* =====================================================================
   Recrea drafts de transferencia borradas (TransferDelete) cuyo ITR
   (OWTQ) sigue abierto en SAP y ya fue despachado por WMS.

   Origen de datos: smm_Transdiscrep_odrf_bads / smm_Transdiscrep_drf1_bads
   (copia que deja TransferDelete al borrar).

   La draft se deja en el mismo estado en que la deja el auto-dispatch
   (Dispatched='N', Received='N', DocEntryITR/DocNumITR del OWTQ,
   TransferType='OWTQ'). Al abrirla en TransferDiscreOrdf.aspx,
   SyncFromSapItr() lee el GTK del OWTQ, completa el Dispatch en SCI
   (Smm_populate_whs_transfers_Batch 'D') y habilita el botón Receive.

   USO:
     1. Revisar/ajustar @Company y la lista @Map (draft SCI <-> DocNum ITR).
        Si la BD SAP no es PRO_DFSC, reemplazar "PRO_DFSC." en todo el script.
     2. Ejecutar con @Commit = 0 (simulación: hace ROLLBACK) y revisar resultados.
     3. Si todo está bien, ejecutar con @Commit = 1.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit  bit          = 0;            -- 0 = simulación (ROLLBACK), 1 = confirmar
DECLARE @Company nvarchar(30) = N'PRO_DFSC';  -- smm_Transdiscrep_odrf.CompanyId

DECLARE @Map TABLE (DocEntryDrf int PRIMARY KEY, ItrDocNum int NOT NULL, DocEntryITR int NULL, Problema nvarchar(200) NULL);
INSERT INTO @Map (DocEntryDrf, ItrDocNum) VALUES
    (2000947, 1508),
    (2000948, 1509),
    (2000949, 1510),
    (2000950, 1511),
    (2000953, 1517);

/* ---------- 1. Resolver el ITR y validar ---------- */
UPDATE m SET m.DocEntryITR = q.DocEntry
FROM @Map m
JOIN PRO_DFSC.dbo.OWTQ q WITH(NOLOCK)
  ON q.DocNum = m.ItrDocNum
 AND CAST(q.U_BOL AS nvarchar(20)) = CAST(m.DocEntryDrf AS nvarchar(20));

UPDATE m SET Problema =
    CASE
        WHEN m.DocEntryITR IS NULL
            THEN N'ITR no encontrado con ese DocNum y U_BOL = draft'
        WHEN EXISTS (SELECT 1 FROM dbo.smm_Transdiscrep_odrf o WHERE o.CompanyId = @Company AND o.DocEntry = m.DocEntryDrf)
            THEN N'La draft ya existe en smm_Transdiscrep_odrf'
        WHEN NOT EXISTS (SELECT 1 FROM dbo.smm_Transdiscrep_odrf_bads b WHERE b.DocEntry = m.DocEntryDrf)
            THEN N'No está en smm_Transdiscrep_odrf_bads'
        WHEN NOT EXISTS (SELECT 1 FROM dbo.smm_Transdiscrep_drf1_bads b WHERE b.DocEntry = m.DocEntryDrf)
            THEN N'No tiene líneas en smm_Transdiscrep_drf1_bads'
        WHEN EXISTS (SELECT 1 FROM PRO_DFSC.dbo.OWTQ q WHERE q.DocEntry = m.DocEntryITR AND (q.DocStatus <> 'O' OR q.CANCELED <> 'N'))
            THEN N'El ITR no está abierto en SAP'
        WHEN EXISTS (SELECT 1 FROM PRO_DFSC.dbo.OWTQ q WHERE q.DocEntry = m.DocEntryITR AND ISNULL(q.U_GTK_CONFIRMATION,'') = '')
            THEN N'El ITR no tiene GTK (WMS no lo ha despachado)'
    END
FROM @Map m;

-- Resumen por draft
SELECT m.DocEntryDrf, m.ItrDocNum, m.DocEntryITR,
       q.DocStatus AS ItrStatus, q.U_GTK_CONFIRMATION AS ItrGTK,
       q.Filler AS ItrFrom, q.ToWhsCode AS ItrTo,
       ISNULL(m.Problema, N'OK') AS Resultado
FROM @Map m
LEFT JOIN PRO_DFSC.dbo.OWTQ q WITH(NOLOCK) ON q.DocEntry = m.DocEntryITR
ORDER BY m.DocEntryDrf;

-- Líneas: respaldo vs ITR (Diferencia <> 'OK' debe revisarse)
;WITH b AS (
    SELECT d.DocEntry, d.ItemCode, d.ToWhsCode, CAST(d.DraftQuantity AS int) AS Qty
    FROM dbo.smm_Transdiscrep_drf1_bads d
    WHERE d.DocEntry IN (SELECT DocEntryDrf FROM @Map)
), l AS (
    SELECT m.DocEntryDrf AS DocEntry, w.ItemCode, w.WhsCode, CAST(w.Quantity AS int) AS Qty, CAST(w.OpenQty AS int) AS OpenQty
    FROM @Map m
    JOIN PRO_DFSC.dbo.WTQ1 w WITH(NOLOCK) ON w.DocEntry = m.DocEntryITR
)
SELECT ISNULL(b.DocEntry, l.DocEntry) AS DocEntryDrf,
       ISNULL(b.ItemCode, l.ItemCode) AS ItemCode,
       b.Qty AS QtyDraft, l.Qty AS QtyITR, l.OpenQty AS OpenQtyITR,
       CASE WHEN b.ItemCode IS NULL THEN N'Solo en ITR'
            WHEN l.ItemCode IS NULL THEN N'Solo en draft'
            WHEN b.Qty <> l.Qty     THEN N'Cantidad distinta'
            ELSE N'OK' END AS Diferencia
FROM b
FULL JOIN l ON l.DocEntry = b.DocEntry AND l.ItemCode = b.ItemCode AND l.WhsCode = b.ToWhsCode
ORDER BY 1, 2;

IF EXISTS (SELECT 1 FROM @Map WHERE Problema IS NOT NULL)
BEGIN
    RAISERROR(N'Hay drafts con problemas (ver columna Resultado). No se insertó nada.', 16, 1);
    RETURN;
END

/* ---------- 2. Recrear encabezado y líneas ---------- */
BEGIN TRAN;

INSERT INTO dbo.smm_Transdiscrep_odrf
    (CompanyId, DocEntry, DocNum, DocDate, FromWhsCode, FromWhsName, ToWhsCode, ToWhsName,
     DocStatus, Dispatched, DispCompleted, Received, ReceCompleted,
     Date_Created, Created_By,
     DocEntryITR, DocNumITR, TransferType)
SELECT @Company, b.DocEntry, b.DocNum, b.DocDate, b.FromWhsCode, b.FromWhsName, b.ToWhsCode, b.ToWhsName,
       'O', 'N', NULL, 'N', NULL,
       b.Date_Created, b.Created_By,
       q.DocEntry, q.DocNum, 'OWTQ'
FROM dbo.smm_Transdiscrep_odrf_bads b
JOIN @Map m ON m.DocEntryDrf = b.DocEntry
JOIN PRO_DFSC.dbo.OWTQ q WITH(NOLOCK) ON q.DocEntry = m.DocEntryITR;

INSERT INTO dbo.smm_Transdiscrep_drf1
    (CompanyId, DocEntry, LineNum, DocNum, ToWhsCode, ToWhsName, ItemCode, ItemName,
     DraftQuantity, DispatchQuantity, ReceivedQuantity, Price, TmpQuantity, Date_Created, Created_By)
SELECT @Company, d.DocEntry, d.LineNum, d.DocNum, d.ToWhsCode, d.ToWhsName, d.ItemCode, d.ItemName,
       d.DraftQuantity, d.DispatchQuantity, d.ReceivedQuantity, d.Price, d.TmpQuantity, d.Date_Created, d.Created_By
FROM dbo.smm_Transdiscrep_drf1_bads d
JOIN @Map m ON m.DocEntryDrf = d.DocEntry;

-- Resultado
SELECT o.DocEntry, o.FromWhsCode, o.ToWhsCode, o.Dispatched, o.Received, o.DocEntryITR, o.DocNumITR, o.TransferType,
       (SELECT COUNT(*) FROM dbo.smm_Transdiscrep_drf1 d WHERE d.CompanyId = o.CompanyId AND d.DocEntry = o.DocEntry) AS Lineas
FROM dbo.smm_Transdiscrep_odrf o
WHERE o.CompanyId = @Company AND o.DocEntry IN (SELECT DocEntryDrf FROM @Map)
ORDER BY o.DocEntry;

IF @Commit = 1
BEGIN
    COMMIT;
    PRINT N'COMMIT: drafts recreadas. Abrir cada una en TransferDiscreOrdf.aspx para completar Dispatch y luego Receive.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT N'SIMULACIÓN: se hizo ROLLBACK. Cambiar @Commit = 1 para confirmar.';
END
