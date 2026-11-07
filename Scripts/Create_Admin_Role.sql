/* =====================================================================
   Rol Administrator (RoleID 19 - ADMIN)

   - Crea el rol 19 ADMIN si no existe (en SMM_PROD ya existe).
   - Le da acceso F a TODAS las pantallas (SISINV_CONTROLS), para que
     los usuarios que pasen a este rol no pierdan ningún acceso.
   - Roles (RoleEdit.aspx) queda exclusiva del Administrator: 'N' para
     los demás roles. Users / Credentials / Scheduler Min-Max se dejan
     como están (GEOPE y LOGTA mantienen su acceso).
   - Asigna el rol a los logins de @Logins (cada login tiene un solo rol).

   USO: ejecutar en cada base SMM (SMM_PROD, SMM_DFC, ...).
        Primero con @Commit = 0 (simulación), luego con @Commit = 1.
        Los usuarios deben cerrar sesión y volver a entrar.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Commit  bit          = 0;          -- 0 = simulación (ROLLBACK), 1 = confirmar
DECLARE @RoleId  varchar(10)  = '19';
DECLARE @User    varchar(20)  = 'DGILTIE';  -- queda en Created_By

DECLARE @Logins TABLE (LoginID varchar(30) PRIMARY KEY);
INSERT INTO @Logins VALUES ('DGILTIE');     -- agregar aquí los demás logins: ,('XXXX')

BEGIN TRAN;

-- 1. Rol
IF NOT EXISTS (SELECT 1 FROM dbo.SISINV_ROLES WHERE RoleID = @RoleId)
    INSERT INTO dbo.SISINV_ROLES (RoleID, Role_Name, Role_Description)
    VALUES (@RoleId, 'ADMIN', 'Administrator');

-- 2. Control RoleEdit.aspx (no existe en algunas bases)
IF NOT EXISTS (SELECT 1 FROM dbo.SISINV_CONTROLS WHERE ControlName = 'RoleEdit.aspx')
    INSERT INTO dbo.SISINV_CONTROLS (ControlName, ControlType, ControlDesc, Date_Created, Created_By)
    VALUES ('RoleEdit.aspx', 'FORM', 'Role Maintenance (Admin)', GETDATE(), @User);

-- Control POErrors.aspx (menú ADR > PO Errors, portado de ADA)
IF NOT EXISTS (SELECT 1 FROM dbo.SISINV_CONTROLS WHERE ControlName = 'POErrors.aspx')
    INSERT INTO dbo.SISINV_CONTROLS (ControlName, ControlType, ControlDesc, Date_Created, Created_By)
    VALUES ('POErrors.aspx', 'FORM', 'PO Errors (ADR)', GETDATE(), @User);

-- 3. Acceso F del Administrator a todas las pantallas
UPDATE rc SET AccessType = 'F'
FROM dbo.SISINV_ROLE_CONTROL rc
WHERE rc.RoleID = @RoleId AND ISNULL(rc.AccessType, '') <> 'F';

INSERT INTO dbo.SISINV_ROLE_CONTROL (RoleID, ControlName, AccessType, Date_Created, Created_By)
SELECT @RoleId, c.ControlName, 'F', GETDATE(), @User
FROM dbo.SISINV_CONTROLS c
WHERE NOT EXISTS (SELECT 1 FROM dbo.SISINV_ROLE_CONTROL rc WHERE rc.RoleID = @RoleId AND rc.ControlName = c.ControlName);

-- 4. Roles (RoleEdit.aspx) solo para el Administrator
UPDATE dbo.SISINV_ROLE_CONTROL SET AccessType = 'N'
WHERE ControlName = 'RoleEdit.aspx' AND RoleID <> @RoleId AND ISNULL(AccessType, '') <> 'N';

-- 5. Asignar el rol a los logins
UPDATE l SET l.RoleId = @RoleId
FROM dbo.SMM_LOGIN l
JOIN @Logins x ON x.LoginID = l.LoginID;

-- Resultado
SELECT DB_NAME() AS Base, l.LoginID, l.RoleId, CASE WHEN l.LoginID IS NULL THEN 'NO EXISTE' ELSE 'OK' END AS Estado
FROM @Logins x LEFT JOIN dbo.SMM_LOGIN l ON l.LoginID = x.LoginID;

SELECT DB_NAME() AS Base, gt.controlName, gt.RoleID, COUNT(*) AS Usuarios
FROM dbo.SISINV_GET_ACCESSTYPE_VIEW gt
WHERE gt.AccessType = 'F'
  AND gt.controlName IN ('UserEdit.aspx', 'LoginEdit.aspx', 'RoleEdit.aspx', 'BodegaTiendaEdit.aspx')
GROUP BY gt.controlName, gt.RoleID
ORDER BY gt.controlName, gt.RoleID;

SELECT DB_NAME() AS Base, COUNT(*) AS PantallasConF_Admin,
       (SELECT COUNT(*) FROM dbo.SISINV_CONTROLS) AS PantallasTotales
FROM dbo.SISINV_ROLE_CONTROL WHERE RoleID = @RoleId AND AccessType = 'F';

IF @Commit = 1
BEGIN
    COMMIT;
    PRINT 'COMMIT: rol Administrator listo. Los usuarios deben volver a iniciar sesion.';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT 'SIMULACION: se hizo ROLLBACK. Cambiar @Commit = 1 para confirmar.';
END
