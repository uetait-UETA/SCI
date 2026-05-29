# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**SCI (Sistema de Control de Inventario / Inventory Control System)** — ASP.NET Web Forms application (.NET 4.0, C#) that manages store inventory, warehouse transfers, and stock min/max levels. It integrates with SAP Business One (B1) via both direct SQL cross-database queries and the SAP Service Layer REST API.

## Deployment & Build

This is an IIS-hosted Web Forms application. There is no CLI build command — build via Visual Studio (`SCI.sln`) or publish to IIS. The current working config points to a **test environment** (server `10.15.10.90` / `10.15.10.140`). Production config is commented out in `Web.config`.

To switch environments, toggle the commented blocks in `Web.config` under `<connectionStrings>` and `<appSettings>`.

### Local dev (IIS Express)

Run via Visual Studio F5 — do **not** start IIS Express manually (causes port conflict). URL: `http://localhost:50063/`.

Both IIS Express config files must have `physicalPath="C:\DGil\Aplicaciones\DFS\SCIv2BrchInv"` for the SCI site:
- `.vs\config\applicationhost.config` — used by Visual Studio
- `.vs\SCI\config\applicationhost.config` — fallback

These were previously pointing to a stale network path (`\\192.168.168.70\WebData\SCI`) and have been corrected.

## Architecture

### Two separate SQL connections

| Class | Connection String | Database |
|---|---|---|
| `SqlDb` | `smm_latConnectionString` | SMM application DB (`smm_lat` / `SMM_DFC`) |
| `DFBUYINGdb` | `DFBUYING` | Purchasing DB, used only by `DataManager` |

All SAP B1 company data is accessed via cross-database queries: `SELECT ... FROM {CompanyId}..OITM` where `CompanyId` is the SAP B1 DB name stored in `Session["CompanyId"]`.

### Page base class

All pages inherit `BasePage` (`App_Code/BasePage.cs`), which:
- Redirects to `Login1.aspx` if `Session["UserId"]` is empty (overridable via `RequiresLogin`)
- Selects master page based on `Session["Language"]` (`en` → `SiteMaster.master`, `es` → `SiteMaster01142021.master`)
- Exposes `BranchId`, `BranchName`, `IsSellerBranch` session helpers

### Session variables (set at login in `Login1.aspx.cs`)

| Key | Content |
|---|---|
| `UserId` | Login username |
| `CompanyId` | SAP B1 company DB name (e.g. `DFC_HOLDINGS`) |
| `CompanyName` | Display name |
| `BranchId` | `SMM_COMPANIES.Branch` (maps to SAP `BPLId`) |
| `BranchName` | Display name from `SMM_BRANCHES` |
| `IsSellerBranch` | `"Y"` / `"N"` |
| `tienda_db` | Secondary SAP DB for store-level queries |
| `Language` | `"en"` or `"es"` |
| `Roles`, `Controles`, `Permissions` | `ArrayList` of role/control/permission strings |

### Role-based access control

Each page checks access in `Page_Load`:
```csharp
db.SISINV_GET_ACCESSTYPE_PRC(lCurUser, "PageName.aspx", ref strAccessType, ref strRole_Description);
```
`strAccessType` values: `"F"` = full, `"R"` = read-only, `"N"` = no access.  
Implemented in `AccessRepository.GetAccessType()` → queries `SISINV_GET_ACCESSTYPE_VIEW`.

### App_Code classes

- **`SqlDb`** — base DB wrapper; call `Connect()` / `Disconnect()` explicitly; exposes `Conn`, `cmd`, `adapter`
- **`Queries`** — shared SQL fragments: `WITH_NOLOCK`, `With_SmmDraftHeader()`, `With_ResearchWhsCodes()`
- **`SapServiceLayer`** — thin wrapper for SAP B1 Service Layer REST calls; one instance per operation: `Login(companyDb)` → action → `Logout()`; reads `SL_BaseUrl`, `SL_UserName`, `SL_Password` from `appSettings`
- **`TransferAutoDispatch`** — auto-dispatches transfers to SAP B1; `IsBodegaToTiendaTransfer()` gates the ITR creation (FROM must be `SMM_WHSTYPE.TYPEWHS='BODEGA'` + `BPLId=1`, TO must be `SMM_WHSTYPE.TYPEWHS='TIENDA'`); `RunAutoDispatch()` executes the full flow; on SAP failure it deletes the local draft
- **`TransferRepository`** — replaces legacy stored procedures for transfer CRUD
- **`WarehouseRepository`** — replaces legacy SPs for bin/min-max uploads
- **`AccessRepository`** — replaces `SISINV_GET_ACCESSTYPE_PRC` and login validation SPs
- **`DeliveryRepository`** — delivery/order error queries
- **`DataManager`** — uses `DFBUYINGdb`; purchasing/open-orders reports
- **`Reports`** — on-hand inventory report queries
- **`WMS`** — warehouse management system queries

### Key appSettings

| Key | Purpose |
|---|---|
| `TorColumnGroupName` | Column group label for TOR branch columns in StoreMinMax3 (default `"TOR"`) |
| `TorBPLId` | BPLId that identifies the TOR branch (default `1`) |
| `CreateTransferSerieDoc` | SAP document series for new transfer requests |
| `SL_BaseUrl` / `SL_UserName` / `SL_Password` | SAP Service Layer credentials |
| `whs_code` | Default warehouse code |

## Telerik RadGrid — Dynamic Columns

**Critical pattern**: dynamically-added columns (e.g. `GridBoundColumn`, `GridColumnGroup`) **must** be recreated on every postback in `NeedDataSource` or column/group creation events — never only in `!IsPostBack`. ViewState is bound to column position at render time; adding columns only on first load causes ViewState mismatches on postback.

See `feedback_telerik_dynamic_columns.md` in project memory for the full pattern.

## Known Customizations (tracked in memory)

- **StoreMinMax3.aspx** — TOR branch column group added; columns are dynamically generated per-warehouse grouped under a `GridColumnGroup`. See `project_storeminmax3_tor.md`.

  **Filtro por tipo de operación (Duty Free / Duty Paid):** `GetMinMaxQuantities` en `App_Code/Reports.cs` aplica en todos sus caminos de código:
  ```sql
  AND (ISNULL(c.U_Type, '') = '' OR ISNULL(b.U_Type, '') = c.U_Type)
  ```
  donde `c` = `OWHS` (warehouse seleccionado) y `b` = `OITM` (item). Solo muestra ítems cuyo `U_Type` coincide con el del warehouse. Si el warehouse no tiene `U_Type` configurado, no filtra. **Prerequisito:** la tabla `rss_loc_dept_multiple` debe tener entradas para cada combinación warehouse + categoría; si faltan, el grid queda vacío aunque los ítems existan en OITM. Script para insertar entradas faltantes:
  ```sql
  INSERT INTO SMM_DFC.dbo.rss_loc_dept_multiple (LOC, dept, companyId, ORDER_MULTIPLE)
  SELECT w.WhsCode, d.ItmsGrpCod, 'DFC_HOLDINGS', 'E'
  FROM DFC_HOLDINGS.dbo.OWHS w
  INNER JOIN SMM_DFC.dbo.RSS_OWHS_CONTROL c ON c.WhsCode=w.WhsCode AND c.Control='SETMINMAX' AND c.CompanyId='DFC_HOLDINGS'
  CROSS JOIN DFC_HOLDINGS.dbo.oitb d
  WHERE EXISTS (SELECT 1 FROM DFC_HOLDINGS.dbo.OITW iw INNER JOIN DFC_HOLDINGS.dbo.OITM im ON im.ItemCode=iw.ItemCode WHERE iw.WhsCode=w.WhsCode AND im.ItmsGrpCod=d.ItmsGrpCod)
  AND NOT EXISTS (SELECT 1 FROM SMM_DFC.dbo.rss_loc_dept_multiple m WHERE m.LOC=w.WhsCode AND m.dept=d.ItmsGrpCod AND m.companyId='DFC_HOLDINGS')
  ```

  **Sub-header centrado:** `gCol.HeaderStyle.HorizontalAlign = HorizontalAlign.Center` se aplica en los tres puntos donde se crean/actualizan columnas TOR: `Page_Init`, rama `existing` en `BuildTorColumns()`, y rama `else` (safety net) en `BuildTorColumns()`.

  **Grid vacío al cambiar dropdowns:**
  - *Tienda (`drpToWhsCode`)*: No usa `AutoPostBack` (causaba redirect a login vía `Global.asax`). En su lugar, atributo `onchange` JavaScript inline oculta el grid client-side (`g.style.display='none'`). `BindGridView1()` lo vuelve a mostrar con `GridView1.Visible = true`.
  - *Categoría (`DropDownItmGrp`)*: Sí usa `AutoPostBack`. En `DropDownItmGrp_SelectedIndexChanged` se limpian los parámetros del ODS (`depts=""`, `brands=""`) y se llama `GridView1.DataBind()`. Con parámetros vacíos, `GetMinMaxQuantities` hace early return (sin conectar a BD) → grid queda visible pero sin filas.
  - **⚠️ No usar el patrón `DataSourceID=""` / `DataBind(new DataTable())` / `DataSourceID="ObjectDataSource1"`** — asignar `DataSourceID` de vuelta dispara un auto-bind interno de Telerik que lanza excepción → `Application_Error` → redirect a Login1.aspx.

## Global.asax — Manejo de errores

`Global.asax` tiene `Application_Error` que llama `Server.ClearError()` y `Response.Redirect("~/Login1.aspx")`. **Cualquier excepción no manejada en la aplicación redirige al login.** Al depurar redirects inesperados al login, buscar primero excepciones en el flujo, no problemas de sesión.
- **ProductLocator.aspx** — BPLId branch filter applied (default branch first in sort), column renamed Colon → "Tor In Transit". See `project_productlocator_branch_filter.md`.
- **CreateTransfer / CreateTransferXsap / GoodsReceiptPO — auto-dispatch** — `TransferAutoDispatch.RunAutoDispatch()` se llama al crear el draft en las 3 pantallas. El auto-dispatch (creación del ITR en SAP B1) **solo ocurre cuando el origen es BODEGA (`SMM_WHSTYPE.TYPEWHS='BODEGA'`, `BPLId=1`) Y el destino es TIENDA (`SMM_WHSTYPE.TYPEWHS='TIENDA'`)**. Cualquier otra combinación (TIENDA→TIENDA, BODEGA→BODEGA, etc.) no genera auto-dispatch. La lógica está en `TransferAutoDispatch.IsBodegaToTiendaTransfer()` (`App_Code/TransferAutoDispatch.cs`). `GoodsReceiptPO.aspx` es exclusivamente de recepción — no hace dispatch.

  **Notas importantes sobre `IsBodegaToTiendaTransfer`:**
  - Usa `SMM_WHSTYPE.TYPEWHS` (clasificación de la aplicación) — **NO** `OWHS.U_Type` (campo SAP), ya que solo `SMM_WHSTYPE` tiene los valores 'BODEGA'/'TIENDA' confiables.
  - El warehouse destino (TO) se lee desde `smm_Transdiscrep_drf1` (líneas) — **NO** desde `smm_Transdiscrep_odrf.ToWhsCode` (encabezado), porque el campo del encabezado puede ser NULL.
- **TransferDiscreOrdf.aspx — campos ITR en smm_Transdiscrep_odrf** — se agregaron dos columnas nuevas a la tabla `smm_Transdiscrep_odrf`:

  | Columna | Tipo | Propósito |
  |---|---|---|
  | `DocEntryITR` | INT NULL | DocEntry del SAP **Inventory Transfer Request** (OWTQ), creado en el paso de Dispatch |
  | `DocNumITR` | INT NULL | DocNum del SAP Inventory Transfer Request (OWTQ) |

  Los campos existentes `DocEntryTraRec2` / `DocNumTraRec2` quedan **exclusivamente** para el **Inventory Transfer** (OWTR) creado en el paso de Receive.

  **Script SQL requerido (ya debe estar aplicado en BD):**
  ```sql
  ALTER TABLE dbo.smm_Transdiscrep_odrf
      ADD DocEntryITR INT NULL,
          DocNumITR   INT NULL;
  ```

  **Archivos modificados:**
  - `TransferDiscreOrdf.aspx.cs` — `CreateSapTransferRequest`: guarda OWTQ en `DocEntryITR`/`DocNumITR`
  - `TransferDiscreOrdf.aspx.cs` — `CreateSapInventoryTransfer`: lee `DocEntryITR` como base doc para crear el IT
  - `TransferDiscreOrdf.aspx.cs` — `SyncFromSapItr`: lee/escribe `DocEntryITR`/`DocNumITR` al sincronizar desde SAP
  - `App_Code/TransferAutoDispatch.cs` — `CreateSapTransferRequest`: igual que arriba, para el flujo auto-dispatch

- **TransferDiscreOrdf.aspx — grid encabezado (GridView1)** — layout de columnas actual:

  | Cells[] | DataField | Header | Visible |
  |---|---|---|---|
  | 0 | DocNumITR | ITR # (60px, Wrap=False) | Sí |
  | 1-16 | DocNum…ReceiveType | sin cambios | Sí |
  | 12 | DocDisRec2 | TrasNum (= DocNumTraRec2, OWTR DocNum) | Sí |
  | 17 | U_GTK_CONFIRMATION | GTKConf | No |
  | 18 | DocEntryITR | DocEntryITR | No |
  | 19 | FromWhsType | FromWhsType | No |
  | 20 | DocEntryTraRec2 | DocEntryTraRec2 | No |

  **⚠️ Importante — hidden BoundField cells:** los `BoundField` con `Visible="False"` existen en la colección `Cells` pero su propiedad `Text` queda vacía en ASP.NET WebForms (el data binding se omite para columnas ocultas). **Nunca leer datos de validación desde `Cells[17..20]`** — usar siempre una consulta a BD o un `HiddenField` separado.

  **Validación GTK:** `GridView1_PreRender` y `DisRec()` usan `GetLocalGtkConfirmation()` (consulta directa a BD) — **no** desde `Cells[]`. Intentar leer `Cells[17].Text` devuelve siempre vacío.

  **Edición de cantidad Duty Paid:** `GridView2_DataBound` llama a `GetFromWhsType()` (consulta directa a BD — `OWHS.U_Type` del warehouse FROM) para habilitar `TextBox1` (ReadOnly=false, fondo blanco) cuando el valor es `"Duty Paid"`. **No** usa `Cells[19].Text` — ese campo es un BoundField oculto y siempre devuelve vacío.

- **TransferDiscreOrdf.aspx — `DocNumTraRec2` / `DocEntryTraRec2` (OWTR):**
  - Deben contener **solo** el DocNum/DocEntry del OWTR (Inventory Transfer, paso Receive)
  - El SP `Smm_populate_whs_transfers_Batch` puede sobreescribirlos con el valor del OWTQ durante Dispatch → ambos flujos (`TransferDiscreOrdf.aspx.cs` `DisRec()` y `TransferAutoDispatch.RunAutoDispatch()`) los limpian a NULL después de llamar al SP con TypeTran='D'
  - `TrasNum` (columna 12) muestra vacío cuando `DocNumTraRec2` es 0/NULL; solo muestra valor después del Receive

  **`App_Code/Transfer.cs` — `GetTransdiscrepOrder`:** incluye `DocEntryITR`, `DocNumITR`, `DocEntryTraRec2`, `FromWhsType` (LEFT JOIN `OWHS` por `FromWhsCode`). `DocDisRec2` usa `CASE WHEN > 0` para mostrar vacío en lugar de "0".

- **TransferDiscreOrdf.aspx — valores del campo `Received`:**

  | Valor | Significado | Quién lo escribe |
  |---|---|---|
  | `'N'` | No recibido | SP `smm_populate_discrep_odrf` (INSERT inicial) |
  | `'L'` | Recibido y cerrado | SP `Smm_populate_whs_transfers_Batch` (TypeTran='R') |

  El SP usa `'L'` (no `'Y'`) para marcar recepción completada. El código C# compara siempre contra `"N"` — cualquier valor distinto de `'N'` significa procesado. **Nunca asumir que el valor "recibido" es `'Y'`.**

  `SyncFromSapItr` compara `received != "N"` para saltar la sincronización en órdenes ya cerradas — no `received == "Y"`, que nunca sería verdadero.

- **MinMaxByExcel.aspx — validación de tipo de operación al subir Excel:** antes de hacer el upload a BD, se valida que todos los ítems del Excel pertenezcan al mismo `U_Type` que el warehouse seleccionado en "Operation".

  **Métodos agregados en `MinMaxByExcel.aspx.cs`:**
  - `GetWhsUType()` — consulta `OWHS.U_Type` para el warehouse seleccionado (`Loc`). Retorna cadena vacía si el warehouse no tiene tipo configurado (en ese caso la validación se omite).
  - `CheckItemTypes(string whsUType)` — consulta `OITM.U_Type` para todos los ítems del grid en una sola query; marca en **rojo** (`Color.LightCoral`) las filas con tipo incorrecto y activa `wrongTypeQty = 1`.

  **Flujo de validación en `importData()`:**
  ```
  CheckRows() → CheckDuplicated() → CheckItemTypes() → UploadDataToDataBase()
  ```
  Si `wrongTypeQty == 1` se bloquea el upload con mensaje: *"Some items do not belong to the '{tipo}' operation type. Review lines highlighted in red."*

  **⚠️ Usar `using` para los comandos SQL locales** — no reasignar `db.cmd` ni `db.adapter` dentro de estos métodos (son objetos compartidos). Los métodos usan `SqlCommand` y `SqlDataAdapter` locales con `using`.
