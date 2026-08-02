-- Registers the new admin/BodegaTiendaEdit.aspx page in the access-control
-- tables, granting it to the exact same roles as UserEdit.aspx and
-- LoginEdit.aspx (confirmed identical: RoleID 1/2 = Full, everything else = No
-- access), per the "Migrate_SMM_PROD.sql" section 3 pattern used to register
-- FillBrandPriority.aspx / ComprasDirectas.aspx / ComprasDirectasDetail.aspx.
--
-- Run manually against TEST first, then PRODUCTION -- not executed automatically.

IF NOT EXISTS (SELECT 1 FROM dbo.SISINV_CONTROLS WHERE ControlName = 'BodegaTiendaEdit.aspx')
BEGIN
    INSERT INTO dbo.SISINV_CONTROLS (ControlName, ControlType, ControlDesc, Date_Created, Created_By)
    VALUES ('BodegaTiendaEdit.aspx', 'FORM', 'Bodega-Tienda Pairs and Schedule Admin', GETDATE(), 'SYSTEM');
    PRINT 'Inserted: SISINV_CONTROLS BodegaTiendaEdit.aspx';
END
ELSE PRINT 'Exists:  SISINV_CONTROLS BodegaTiendaEdit.aspx';
GO

INSERT INTO dbo.SISINV_ROLE_CONTROL (RoleID, ControlName, AccessType, Date_Created, Created_By)
SELECT rc.RoleID, 'BodegaTiendaEdit.aspx', rc.AccessType, GETDATE(), 'SYSTEM'
FROM dbo.SISINV_ROLE_CONTROL rc
WHERE rc.ControlName = 'UserEdit.aspx'
  AND NOT EXISTS (
      SELECT 1 FROM dbo.SISINV_ROLE_CONTROL x
      WHERE x.ControlName = 'BodegaTiendaEdit.aspx' AND x.RoleID = rc.RoleID
  );
PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' SISINV_ROLE_CONTROL rows inserted for BodegaTiendaEdit.aspx';
GO

-- Verify -- should match UserEdit.aspx / LoginEdit.aspx exactly (RoleID 1/2 = 'F', rest = 'N'):
-- SELECT ControlName, RoleID, AccessType FROM SISINV_ROLE_CONTROL
-- WHERE ControlName IN ('UserEdit.aspx','LoginEdit.aspx','BodegaTiendaEdit.aspx')
-- ORDER BY RoleID, ControlName;
