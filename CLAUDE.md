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

  **Detalle del ítem (portado de `C:\DGil\Aplicaciones\ADA\SCIv2`):** búsqueda adicional por Style / Color / Size (solo cuando Item/Barcode está vacío; `SearchByStyleColorSize` + `HandleMultiItemResult`) y bloque de detalle con RSKU Code, Retail Price, CSKU ID, Style, Color, Size, Class, Subclass, HS Code, Origin. Origen de datos en `GetData()`:

  | Campo | Fuente |
  |---|---|
  | CSKU ID | `OITM.U_CSKU_ID` |
  | Style / Color / Size | `OITM.U_VendorRefNum` / `U_VendorColorDesc` / `U_VendorSizeDesc` |
  | Class | `[@DCS_HIERARCHY].U_PRDCT_CLSS_NM` (join por `CODE = OITM.U_Hierarchy`) |
  | Subclass | `LEFT(OITM.U_Hierarchy, ...)` antes de `' - '` |
  | HS Code | `OITM.U_HSCODE` |
  | Origin | `[@ORIGIN].Name` (join por `Code = OITM.U_Origin`) |
  | Retail Price | `ITM1.Price` — lista según `Session["BranchId"]`: **4 (SFO) → lista 2** "SFO Retail Price"; cualquier otro (3 LAX, 1 bodega) → **lista 1** "LAX Retail Price" |

  Misma regla de precio que el reporte de transferencias (`Queries.With_SmmDraftDetail`, `TransferRepository`), pero allí el branch sale del `BPLId` del warehouse destino, no de la sesión. El grid de ProductLocator **no** se modificó (no tiene la columna APRI In Transit de ADA).

  **Columna "Committed" en el grid:** `OITW.IsCommited` (alias `committed`, header "Committed" 72px con tooltip "SAP OITW.IsCommited"), después de SOH, con total en el footer (`_ftCommitted`). Es solo informativa: no entra en `soh_avail` / `net_inventory`. La columna Reserved se muestra como **"Rsvd"** (tooltip "Reserved", 55px). Loc Name tiene 220px; el resto de anchos se redujo para que el total quepa (~1060px).

  **Units Per Case:** en el encabezado, después de Retail Price, `OITM.U_Bot` (UDF numérico "Units Per Case") → `lblUnitsPerCase`.

  **Bodegas excluidas del grid:** appSetting `ProductLocatorExcludeWhs` (códigos separados por coma, ej. `"3060"` = FRAGRANCE ESPECIAL WH). `GetSqlByCompany` agrega `AND loc NOT IN (...)`; clave ausente o vacía = no excluye nada. Se eligió lista explícita porque los criterios automáticos (sin POS Code / sin `SMM_WHSTYPE`) también ocultaban bodegas de daños y tiendas reales.

- **DeliveryErrors.aspx — Trans # dividido:** la columna `TicketNo` (`la_store_sales`, nvarchar de 7 a 11 dígitos) se muestra en tres columnas calculadas en `Delivery.GetDeliveryErrors` (`App_Code/Delivery.cs`): **Site** = `LEFT(TicketNo,3)` → `TicketSite`, **Register No.** = `SUBSTRING(TicketNo,4,3)` → `TicketRegNo`, **DFS Trans. No.** = `SUBSTRING(TicketNo,7,50)` → `TicketDfsNo`. `TicketNo` sigue en el SELECT.
- **StoreMinMax3Multiple.aspx — Units Per Case:** columna `UnitsPerCase` (`OITM.U_Bot`) antes de CSKU ID en el grid y en Export to Excel, con título **"Case Pack"** (la columna Case Pack anterior del export, `case_pack` fijo en 1, se quitó del export; la columna oculta "Items per Box" del grid sigue igual). Se agrega en las 4 variantes SQL de `Reports.GetMinMaxQuantities_Mult` que leen OITM (`b.U_Bot AS UnitsPerCase`); la variante "No en SAP" devuelve `NULL`. El export toma el valor del HiddenField `hdnUnitsPerCase` (mapa `s_map` en `WhsItemTemplate`). **Brand** (`marca`) muestra solo el nombre: `LEFT(u_brand, CHARINDEX(' - ', ...))` en las 4 variantes; el filtro por marca del WHERE sigue usando `u_brand` completo. **"Unplanned products with available inventory in Warehouses"** (NoPlanned): además de no tener Min/Max, el ítem debe tener `OnHand > 0` en alguna bodega del grid (`whs`, SETMINMAX/BODEGA) con el mismo `OWHS.U_Type` que la tienda (Duty Paid → 1136, Duty Free → 1161); filtro `unplannedStockFilter`. Se corrigió el `inner join inner join` duplicado y el join a `rss_store_item_min_max` por `e.companyId` en la variante NoPlanned + Item. Con "View Hold Items Only" esta opción no devuelve filas (no hay hold sin min/max).
- **Menú ADR (igual que ADA):** Delivery Errors, Delivery Errors by Items, Transfer Errors, PO Errors, SAP Invoicing, Inventory Adjustment by Excel, Inventory Adjustment by Items (`SiteMaster.master`; el master en español no se tocó).
  - **DeliveryErrorsItems.aspx** reactivada (antes redirigía a DeliveryErrors.aspx) con la versión de ADA: filtro "Inventory Problem Only / All", búsqueda por Item Code, validación de solo lectura al guardar. `Delivery.GetDeliveryErrorsItems(companyId, filterMode, itemFilter)` portado de ADA.
  - **POErrors.aspx** — solo la pantalla: en ADA depende de los "extra items" de Compras Directas (tablas `ComprasDirectasExtraItems*`, `GoodsReceipt.GetExtraItemPriceErrors` / `ReprocessPendingExtraItems` y unos 10 métodos más) que DFS no tiene. El code-behind devuelve un grid vacío con aviso; para que funcione hay que portar esa funcionalidad completa. El control `POErrors.aspx` se crea en `Scripts/Create_Admin_Role.sql`.
- **Rol Administrator (RoleID 19, ADMIN):** `Scripts/Create_Admin_Role.sql` lo crea, le da F a todos los controles y deja `RoleEdit.aspx` solo para ese rol (Users / Credentials / Scheduler Min-Max siguen también para GEOPE y LOGTA, por decisión del usuario). Cada login tiene un solo rol (`SMM_LOGIN.RoleId`); RoleId `0` = acceso total sin consultar permisos.
- **FacturacionSAP.aspx (SAP Invoicing)** — mismo diseño que ADA (columnas Items / Total (SAP) / Total (POS) / Status / Detail, check "Hide items needing manual resolution", grid Upload Requests), pero el preview se calcula **en vivo** en `InvoicePreview.GetPreview(sapDb, bplId, desde, hasta)` (DFS no tiene `la_invoice_preview` ni el dry-run programado) y por branch de sesión (Upload Requests también, desde 2026-11-06: `GetUploadRequests(desde, hasta, bplId)` filtra por `ADR_TIENDA_SERIE.CompanyId` del CardCode, porque `la_invoice_upload_requests` no guarda branch; antes mostraba LAX y SFO juntos). Items / Total (SAP) = líneas abiertas DLN1/RDN1 + DLN3/RDN3 − devoluciones mixtas (-3), igual que `InvoiceUploader.ProcessSide`. Grupos sin líneas abiertas → bloqueo "Manual resolution required (bost_Close)", salvo que ya estén facturados. **Facturados** (check "Show invoiced", `includeInvoiced`): `la_daily_invoices`/`la_daily_returns`, o directo en SAP — OINV/ORIN con líneas `BaseType` 15/16 sobre las ODLN/ORDN del grupo (CardCode C30xx LAX / C31xx SFO); se muestran como "Invoiced: OINV #n" y no se pueden seleccionar. Sin el check se ocultan. DF/DP (`TRANSTYPE`) va como DataKey y, desde 2026-11-09, también como columna visible "DF/DP" en el grid principal y en Upload Requests (antes estaba oculta a pedido del usuario; se mostró por los grupos DF Export de tiendas DP). **"Ready" = se puede subir:** la consulta replica las validaciones del uploader — pagos (`PayTxn`/`PayRows`/`MixedTotals` = `GetPagosParaFactura`/`GetPagosParaDevolucion`/`GetCompensacionMixtas`) deben cuadrar con Total (SAP) ±0.01 y todas las formas de pago deben estar en `ADR_TENDER_SAP`; si no, "Blocked" con el motivo. **DF y DP son independientes** (decisión del usuario): `InvoiceUploader.HayErroresPendientes` filtra por `TRANSTYPE`, y el detalle de partidas pendientes también. **Ventas↔pagos se cruzan por `TransactionID` + `transnum`** (preview y `InvoiceUploader.MakeTxnTotalsCte` / `GetPagosPara*` / `GetCompensacionMixtas`): `la_store_sales.transnum` se repite en distintos días (~18% de los tickets), `TransactionID` es único — cruzar solo por `transnum` mezclaba pagos de otros días. Lo mismo con **`la_delivery_errors`**: hoy todas sus filas tienen `TransactionID` NULL, así que el cruce por `transnum` + `itemnum` exige además el mismo día (`CAST(itemdatetime AS DATE)`) en `InvoicePreview` (ErrorItems y detalle de pendientes) y en `InvoiceUploader.HayErroresPendientes`; sin eso, un error de otro día con el mismo transnum bloqueaba el grupo (ej. C3002 DF 08/06 por un error del 10/16). **Rendimiento:** `la_store_sales` (600k+ filas) no tiene índice por `transnum`; `GetPreview` copia primero las ventas del rango (±1 día) a `#ss` y los pagos a `#sp` con índice clustered por `transnum`, y termina con `OPTION (RECOMPILE)` (el plan cacheado para un día sin pagos tardaba minutos en otro). `CommandTimeout = 300`. Los pagos (ORCT / OVPM) se crean en el mismo "Upload Selected", justo después de la OINV/ORIN. **Ajuste por redondeo:** las ODLN creadas antes del 2026-08-27 guardaron precios con 2 decimales (3 × 17.33 = 51.99), así que Total (SAP) puede diferir de los pagos por centavos. Si la diferencia es ≤ `InvoiceUploader.MaxRoundingAdjustment` (appSetting `FacturacionSAP_MaxRoundingAdjustment`, punto decimal; ausente o inválido = 1.00. Era 0.10 fijo hasta 2026-11-01 — en C3001, con 3,000+ líneas por día, los centavos pasaban de 0.10), el uploader manda `LineTotal` ajustado en la línea de mayor monto (SAP conserva el precio base y lo registra como descuento de línea; probado en TEST_PRO_DFSC en ambos sentidos), verifica que el `DocTotal` creado cuadre antes de crear el pago, y el preview muestra "Ready (rounding adj. ±x.xx)" (columna `RoundingAdj`). Diferencias mayores siguen Blocked. **Retry Payments:** botón visible solo con appSetting `FacturacionSAP_RetryPayments = "true"` (y acceso F). Las filas con OINV/ORIN creada pero sin pago (`PaymentDocNum`/`RefundDocNum` NULL en `la_daily_*` y documento abierto con `PaidToDate = 0` en SAP; columna `PaymentPending`) se muestran siempre, en naranja "Invoiced: OINV #n - payment pending", y se pueden marcar. `InvoiceUploader.RetryPayments` lee el saldo en SAP, exige `DocTotal` = pagos POS (+ compensación) y crea el cobro/reembolso con el mismo `CrearPago` del upload; si el documento ya tiene algo aplicado en SAP no lo toca. Las filas facturadas muestran el `DocTotal` real de la OINV/ORIN (los gastos DLN3 no tienen estado y seguían sumando). **No** usar `TotalDiscount` negativo en el encabezado: SAP lo ignora sin error (`DiscountPercent` negativo sí funciona). Requiere `TransactionID` y `la_invoice_upload_requests`, que existen en SMM_PROD pero **no** en SMM_DFC. **ODLN/ORDN que mezclan ventas de varios días:** hasta el 2026-11-01 ExtraeCreaDocSalesSAP agrupaba las ventas pendientes por `transnum` (`GetPendingAsync` no traía `TransactionID`) y metía ventas de días posteriores en una ODLN con la fecha de la más vieja (141 casos, 06/08–07/10). `InvoicePreview.MixedDeliveriesSql` (compartido por el preview y `InvoiceUploader.GetMixedDeliveryLines`) empareja en orden las líneas DLN1/RDN1 con las ventas de `la_store_sales` del mismo cliente (orden: itemdatetime, TransactionID, itemnum) y exige mismo artículo, cantidad y monto; cada día factura solo sus líneas y el resto queda abierto para su día (probado en TEST_PRO_DFSC: la factura de un día posterior toma la línea de la ODLN vieja). Los gastos adicionales (impuesto DLN3/RDN3) de esas entregas se reparten por día según el `SalesTaxAmount` de las transacciones de cada día (`#mixexp`): solo si hay un único código de gasto y su total coincide (±0.02) con la suma; en los casos reales todo el impuesto era del día original y los días posteriores tenían 0. Si las líneas no cuadran o el impuesto no se puede repartir → Blocked "mixes POS sales of several days". Excluye las transacciones mixtas viejas (venta+devolución metidas completas en una ORDN cuyo DocEntry coincide con una ODLN de la misma tienda). `SapInvoices` ("ya facturado") también cuenta solo las líneas del día. **Tiendas grandes (C3001: ~2,000 ODLN y ~4,000 líneas por día):** el uploader lee las líneas abiertas con una sola consulta a DLN1/RDN1 (`GetOpenDeliveryLines`; antes una lectura por ODLN en el Service Layer = 3.6 min) y solo lee por Service Layer las entregas con gastos DLN3/RDN3; las listas de DocEntry se insertan en bloques de 1,000 (`InsertDocEntriesSql`, límite de `VALUES`). Crear esa factura tarda ~2.7 min en SAP, así que `SapServiceLayer` espera hasta 15 min (antes 60 s: SCI se rendía y SAP la creaba igual, sin pago). `Upload` captura cualquier excepción y la reporta en Upload Requests (antes iba a `Application_Error` → login). **Validación contra el POS (`InvoicePreview.PosGapsSql`, 2026-11-01):** el preview y el uploader (`GetPosGapMessage`, justo antes de crear la OINV/ORIN) comparan el POS (servidor vinculado, appSetting `FacturacionSAP_PosDb`, por defecto `DFSPOS.POS`) con lo cargado: líneas de `vw_POS_VentaItems` (mismos filtros que ExtraeCreaDocSalesSAP: vista RetailTransactionStockView, SELL/RETURN, artículos DF excluidos de transacciones DP salvo `DeeeItemCode`, cajas sin ADR_TIENDA_SERIE no cuentan) que no están en `la_store_sales` por TransactionID+itemnum, y transacciones con más líneas de pago en `POS_TransactionTender` que en `la_store_payments`. Si falta algo → Blocked "N POS transaction(s) ... not loaded" / "POS payments of N transaction(s) ... not loaded". Si el POS no responde (TRY/CATCH sobre `INSERT ... EXEC sp_executesql`) → Blocked "POS check unavailable". Motivo: sin esto, una venta que nunca llegó a SCI junto con su pago "cuadraba" (C3111 DP 15/10 y DF 19/10 salían Ready sin 4 ventas). Agrega ~0.5 s por consulta. **Formas de pago tipo Transfer (PayPal, Venmo, DFS Circle) en reembolsos:** en cobros (ORCT) van como `PaymentChecks`/`CheckAccount`; en reembolsos (OVPM) eso falla con "No matching records found (ODBC -2028)" (SAP lo trata como cheque emitido y busca banco propio), así que van como `TransferAccount`/`TransferSum` (una sola cuenta por reembolso; si hay varias → bloqueado). Probado en TEST_PRO_DFSC con la ORIN 26 de C3117 (2026-11-01). **Líneas excluidas (`dbo.la_invoice_excluded_lines`, BaseType 15/16 + DocEntry + LineNum):** el preview y el uploader nunca las copian. Se usa para las 14 transacciones mixtas viejas (ago/sep-2026) cuya venta quedó dentro de una ORDN: la ORDN no se puede cancelar (el artículo devuelto ya no tiene stock) y SAP no permite cerrar líneas de devoluciones ("You cannot close rows in this type of document"). `Scripts/Reset_Old_Mixed_ORDN.sql` deja pendientes las filas de venta (la integración crea una ODLN que saca las unidades) y excluye la línea de la ORDN; después de subir la nota de crédito del día se cierra la ORDN (`Returns(n)/Close`, sin movimiento de inventario; luego esa ORIN ya no se puede cancelar). Aplicado a TxnID 72942 / ORDN 372 el 2026-11-01. **`LineNum = -1` = gastos adicionales (DLN3/RDN3) del documento**: se usa para transacciones mixtas DP cuyo impuesto (de toda la transacción) quedó completo en la ODLN y en la ORDN porque la integración vieja no guardaba `MontoSinImpuesto`; el impuesto va solo en el lado neto (venta ≥ devolución → se excluye el de la ORDN; si no, el de la ODLN). `Scripts/Exclude_Duplicated_Mixed_Tax.sql` (8 transacciones, 16/09–24/10).
- **FacturacionSAPConsole (2026-11-02)** — "Upload Selected" de SAP Invoicing desde la consola del servidor, sin marcar filas: `FacturacionSAPConsole.exe --desde=AAAA-MM-DD [--hasta=] [--site=LAX|SFO|3|4] [--cardcode=] [--transtype=DF|DP] [--dry-run]` (`--check-sap [--company=X]` prueba SQL + login). Sin parámetros (doble clic) es interactivo: pregunta fechas/site/tienda, muestra el dry-run y sube solo si se escribe `SI`. Proyecto .NET 8 en `C:\DGil\Aplicaciones\DFS\ExtraeCreaDocSalesSAP\ExtraeCreaDocSalesSAP\FacturacionSAPConsole\` (en `ExtraeCreaDocSalesSAP.sln`) que **enlaza** `App_Code/InvoicePreview.cs`, `InvoiceUploader.cs` y `SapServiceLayer.cs` de SCI (lógica única; solo `SqlDb` es propio porque el de SCI usa System.Web). **Si se cambian esos 3 archivos, recompilar/publicar también la consola.** Toma las filas no Blocked y no facturadas de `GetPreview` (mismo criterio que el botón), llama a `Upload` (que revalida todo) y registra en Upload Requests con el usuario `FacturacionSAP_ConsoleUser` (default CONSOLE). Mutex global: una ejecución a la vez. Config en `FacturacionSAPConsole.dll.config` (mismas claves que Web.config + `FacturacionSAP_CompanyDb`). `SapServiceLayer` ya no pide SSL3 (48): .NET 8 lo rechaza ("requested security protocol is not supported").
- **SAP Invoicing — subida nocturna (2026-11-06, pedido de contabilidad):** appSetting `FacturacionSAP_UploadMode` = `Queue` → "Upload Selected" no llama a SAP: inserta en `la_invoice_upload_requests` sin `ProcessedAt` (`InvoicePreview.QueueUploadRequest`, no duplica si ya hay uno pendiente) y el grupo se muestra **"In Progress (usuario MM/dd HH:mm)"** sin check; en Upload Requests "In Progress". Ausente o cualquier otro valor = subida inmediata como antes. La interfaz nocturna es `FacturacionSAPConsole.exe --process-queue [--dry-run]` (tarea programada): toma los pendientes de ambos branches, el más viejo primero, llama a `Upload` (revalida todo; comentario "by <usuario> (night process)") y escribe el resultado en la misma fila (`MarkUploadRequestProcessed`). `RecordUploadResult` (subida inmediata, consola por fecha, Retry Payments) completa la solicitud pendiente del grupo si existe en vez de insertar otra. No hay botón para cancelar una solicitud In Progress.
- **SAP Invoicing — efectivo en sentido contrario (2026-11-06):** si el efectivo neto de un reembolso (o de un cobro) queda al revés — ej. TxnID 24671 C3001 DP 08/08: devolución de 77.92 que devolvió 123.00 a DFS Circle Exchange y el cliente regresó 45.08 de cambio en efectivo — SAP no acepta `CashSum` negativo. Antes: Blocked "negative cash sum". Ahora `BuildPaymentRequest` saca el efectivo (out `efectivoContrario`/`cuentaEfectivo`), el pago principal lleva las demás formas de pago aplicando solo el total del documento (el exceso queda a cuenta) y `CrearEfectivoContrario` crea el pago opuesto en efectivo a cuenta (ORCT para un reembolso, OVPM rCustomer para un cobro) y reconcilia las líneas del cliente (`SapServiceLayer.GetPaymentJournalEntries`: el Service Layer no expone el asiento del pago; se busca por `OriginalJournal` ttReceipt/ttVendorPayment + `Original`). Si no hay otra forma de pago → Blocked. Probado en TEST_PRO_DFSC (ORIN 56, OVPM 200, ORCT 709, recon 1042; la ORIN 56 quedó sin cancelar por el límite de 90 días).
- **ODLN/ORDN abiertas solo por el impuesto (2026-11-07):** la OINV/ORIN copia las líneas con referencia base (SAP las cierra) pero el impuesto DLN3/RDN3 sumado por código **sin** referencia (`BaseType -1`), así que SAP deja abierta cada entrega facturada (igual con la integración vieja). No tiene efecto contable. Se cierran con `FacturacionSAPConsole.exe --close-bases [--dry-run] [--company=X] [--max=N]` (`SapServiceLayer.CloseDocument`, `Close` = solo estado, sin asiento ni inventario; probado en TEST_PRO_DFSC): abiertas, sin líneas abiertas y con **todas** las líneas copiadas a OINV (13) / ORIN (14); las cerradas por devolución u otro documento quedan fuera. ~0.5 s por documento, por eso **no** se hace en el uploader (C3001 sumaría ~17 min por subida); corre de noche con `ProcesoNocturno.bat` (`--process-queue` + `--close-bases`).
- **Ventas Export en tiendas DP (2026-11-09):** el POS marca como **DF "Export"** (sin impuesto, pasajero con vuelo internacional) ventas de mercadería Duty Paid en tiendas DP (C3012 cajas 202/203/205/207, C3114 214, C3117 501, C3104 113, C3108 304/305). Esas cajas solo tenían fila DP en `ADR_TIENDA_SERIE`: la integración las carga por su fallback (usa la fila del tipo contrario) y crea la ODLN Duty Free, pero SAP Invoicing cruza por NUMSERIE + NUMSTORE + **DUTYTYPE** y no las veía (~300,000 sin facturar desde 06/08). `Scripts/Add_ADR_TIENDA_SERIE_Missing_DutyType.sql` agregó la fila DF copiando la DP (mismo CARCODE/WHSCODE/CompanyId; respaldo `ADR_TIENDA_SERIE_bak_20261109`): la integración sigue igual y aparecen grupos "Cxxxx DF" aparte. **No** se agregó DP a tiendas DF (C3105 caja 307, C3109 caja 306): con fila DP la integración aplicaría "txn DP no puede llevar ítems DF" y excluiría ítems. Contabilidad: DP siempre lleva impuesto, por eso Export se factura como Duty Free.
- **Entregas devueltas y re-entregadas a mano en SAP (2026-11-09):** si un usuario SAP (ej. "cacre") devuelve una ODLN de la integración con una ORDN y crea otra ODLN igual ("Based On Return n" / "Replace Delivery n"), `la_store_sales` sigue apuntando a la ODLN original (cerrada) y el grupo no sale Ready ("bost_Close") o no aparece. Se corrige apuntando las ventas a la ODLN nueva: `Scripts/Fix_C3117_0807_Redirect_ODLN6253.sql` y `Scripts/Fix_Cacre_Redeliveries_0806_0807.sql` (C3117 DP 06/08 → 6252, C3008 DF 07/08 → 6254; subidos OINV #438 / #437). Las ventas con `DeliveryDocNum` 99999999 (prueba) o 0 (excluidas) no tienen ODLN y la pantalla no las muestra (ej. C3107 DP 06/08).
- **Release C3012 DF 11–13/08:** las OINV manuales #216/#218/#220 (división C3012/C3017) se marcaron `TransType = 'DFM'` en `la_daily_invoices` (`Scripts/Release_C3012_DF_Manual_Invoices.sql`) para poder subir las ventas Export de esos días.
- **Ambientes (Web.config):** SMM_PROD / PRO_DFSC = **PRODUCCIÓN**; SMM_DFC / DFC_HOLDINGS = TEST (ambos en 10.15.10.90).
- **sapInventory.aspx (SAP Stock)** — grid y Export to Excel con las columnas de ADA: CSKU ID, Style, Color, Size, Subclass, HS Code, Origin (mismas fuentes que ProductLocator). En `Reports.getOnhandItems` (5 variantes de SQL): **Brand** = `LEFT(U_BRAND, ...)` antes de `' - '` (solo nombre); **Class** = `[@DCS_HIERARCHY].U_PRDCT_CLSS_NM` (`OITM.U_CLASS` está vacío en DFS). Como las consultas usan joins con coma, Origin y Class se obtienen con **subconsultas correlacionadas** — no agregar `LEFT JOIN` directo ahí (fallaría por scope de `oitm`). Si se agrega/quita una columna del grid, actualizar los índices `row.Cells[n]` en `btnExport_Click`.
- **CreateTransfer.aspx — filtro por Class** — ListBox `lstClasses` debajo de Brand con `[@DCS_HIERARCHY].U_PRDCT_CLSS_NM` (join `CODE = OITM.U_Hierarchy`), limitado a los Item Groups seleccionados. `LoadClasses()` se llama al cambiar Item Group y al cambiar Brand (`lstItemGroups` tiene `AutoPostBack`): con una marca específica solo muestra las clases de esa marca; con "All Brands" (o sin marca) muestra todas. Primera opción **"All Class"**, seleccionada por defecto = sin filtro (comportamiento original). El filtro (`classFilter`, `EXISTS` sobre `@DCS_HIERARCHY`) se aplica **solo** en el INSERT a `rss_results`; el DELETE previo y `CreateSAPDraft` siguen filtrando solo por brand. **No** agregar el filtro al SQL de `CreateSAPDraft`: ese SQL se parte en 10 trozos de 120 chars para `smm_populate_Smm_Draft` y más de ~1200 chars se trunca.
- **SP `submit_DraftXsap` — doble descuento de ITR (fix 2026-10-13)** — usado solo por `CreateTransferXsap` y `CreateTransferByExcel` (Min-Max usa `smm_populate_Smm_Draft` + `Queries.With_NetInventory`, que no resta `IsCommited` y no tiene el problema). El SP calcula `Inventory = OnHand - IsCommited - en tránsito SCI`; un ITR (OWTQ) abierto ya está en `OITW.IsCommited`, así que las transferencias con ITR se restaban dos veces → ninguna línea pasaba (log `SMM_LOGTRACE` = **TP7**), el SP borraba la draft **sin error** y la pantalla mostraba "Draft XXXX completed." sin ITR. Fix: en `InTransitOutSubmitCheck` se agregó `AND ISNULL(sh.DocEntryITR,0) = 0`. Script: `Scripts/Fix_submit_DraftXsap_ITR_DoubleCount.sql`. Aplicado en test (`10.15.10.90`) en **SMM_PROD** y **SMM_DFC**; SMM_LAX/SMM_SFO tienen versión 2022 distinta (no aplicar sin comparar). Para depurar submits: `SELECT * FROM SMM_LOGTRACE WHERE ObjectName='submit_DraftXsap'` (TP6 = OK, TP7 = sin líneas → draft borrada).
- **CreateTransferXsap — draft temporal expira a los 10 min** — `smm_populate_TransXsap_odrf` borra los encabezados de `smm_TransXsap_odrf` con más de 10 min sin enviarse cada vez que **cualquier usuario** crea una draft en la compañía.
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
