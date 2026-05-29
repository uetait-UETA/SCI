// SCI v2 — Bilingual translation (EN ↔ ES)
// Applied on page load and after every ASP.NET AJAX postback (pageLoad hook).

var SCI_I18n = (function () {

    var dict_es = {
        // ── Login page ───────────────────────────────────────────────────
        "Login":            "Ingresar",
        "Log On":           "Ingresar",
        "Log Out":          "Salir",
        "Logout":           "Salir",
        "Log IN / OUT":     "Ingresar / Salir",
        "Select Company":   "Seleccionar Empresa",
        "User":             "Usuario",
        "Password":         "Contraseña",
        "Title":            "Título",
        "Notice":           "Aviso",
        "Start Date":       "Fecha Inicio",
        "End Date":         "Fecha Fin",

        // ── Navigation ───────────────────────────────────────────────────
        "Home":                         "Inicio",
        "Product Locator":              "Localizador de Productos",
        "Store Min-Max":                "Min-Max Tienda",
        "Min-Max":                      "Min-Max",
        "Min-Max Excel":                "Min-Max Excel",
        "Delivery Errors":              "Errores de Entrega",
        "Delivery Errors by Items":     "Errores de Entrega por Artículos",
        "Transfer Errors":              "Errores de Transferencia",
        "Transfers":                    "Transferencias",
        "Create Min-Max Transfer":      "Crear Transferencia Min-Max",
        "Create Manual Transfer":       "Crear Transferencia Manual",
        "Transfer by Excel":            "Transferencia por Excel",
        "Delete Transfer":              "Eliminar Transferencia",
        "View Transfers":               "Ver Transferencias",
        "Transfer Audit":               "Auditoría de Transferencias",
        "Transfer Audit by Item":       "Auditoría de Transferencias por Artículo",
        "Fill Priority":                "Prioridad de Llenado",
        "Warehouses":                   "Bodegas",
        "Bins":                         "Bins",
        "Bins Excel":                   "Bins Excel",
        "Reports":                      "Reportes",
        "Barcodes":                     "Códigos de Barras",
        "FAQs and Videos":              "FAQs y Videos",
        "Frequently Asked Questions":   "Preguntas Frecuentes",
        "Operational Videos":           "Videos Operativos",
        "Financial Videos":             "Videos Financieros",
        "Maintain FAQ":                 "Mantener FAQ",
        "Maintain Operativos":          "Mantener Operativos",
        "Maintain Financieros":         "Mantener Financieros",
        "Admin":                        "Administración",
        "Users":                        "Usuarios",
        "Credentials":                  "Credenciales",
        "SAP Inventory":                "Inventario SAP",
        "Store Inventory":              "Inventario de Tiendas",
        "Item Detail by Bins Stores":   "Detalle de Items por Bins Tiendas",
        "Bins Disp and Rec (Stores)":   "Bins Desp y Rec (Tiendas)",
        "Orders with Issues":           "Órdenes con Problemas",
        "Warehouse Receipt SAP APRI vs GRPOs (Accounting)": "Recibo Bodega SAP APRI vs GRPOs (Contabilidad)",
        "Warehouse Receipt vs SAP":     "Recibo de Bodega vs SAP",
        "Min-Max by Category and Brand":"Min-Max por Categoría y Marca",
        "Research":                     "Investigación",
        "Kardex":                       "Kardex",

        // ── Common UI ────────────────────────────────────────────────────
        "Search":           "Buscar",
        "Cancel":           "Cancelar",
        "Save":             "Guardar",
        "Delete":           "Eliminar",
        "Print":            "Imprimir",
        "Update":           "Actualizar",
        "Insert":           "Insertar",
        "Select All":       "Seleccionar Todo",
        "Date":             "Fecha",
        "Status":           "Estado",
        "Category":         "Categoría",
        "Brand":            "Marca",
        "Description":      "Descripción",
        "Item":             "Artículo",
        "Items":            "Artículos",
        "Location":         "Ubicación",
        "Quantity":         "Cantidad",
        "Details":          "Detalles",
        "Export to Excel":  "Exportar a Excel",
        "To Excel":         "A Excel",
        "View Report":      "Ver Reporte",
        "View Values":      "Ver Valores",
        "Save Changes":     "Guardar Cambios",
        "Company":          "Empresa",
        "Operation":        "Operación",
        "Origin":           "Origen",
        "Destination":      "Destino",
        "Line":             "Línea",
        "Group":            "Grupo",
        "Store":            "Tienda",
        "Warehouse":        "Bodega",
        "Balance":          "Balance",
        "Price":            "Precio",
        "AND":              "Y",
        "OR":               "O",
        "Copies:":          "Copias:",
        "Source":           "Origen",
        "Dispatches":       "Despachos",

        // ── Filters / Labels ─────────────────────────────────────────────
        "From Date":            "Fecha Desde",
        "To Date":              "Fecha Hasta",
        "Date From":            "Fecha Desde",
        "Date To":              "Fecha Hasta",
        "From Location":        "Ubicación Origen",
        "To Location":          "Ubicación Destino",
        "From Loc":             "Loc Origen",
        "To Loc":               "Loc Destino",
        "From Loc Name":        "Nombre Loc Origen",
        "To Loc Name":          "Nombre Loc Destino",
        "Open":                 "Abierto",
        "Closed":               "Cerrado",
        "All. . . .":           "Todos. . . .",
        "Select a Category":    "Seleccionar Categoría",
        "Select a category":    "Seleccionar Categoría",
        "Select Category(s)":   "Seleccionar Categoría(s)",
        "Select all Categories":"Seleccionar Todas las Categorías",
        "All Categories selected":"Todas las Categorías Seleccionadas",
        "All Categories":       "Todas las Categorías",
        "Select Brand(s)":      "Seleccionar Marca(s)",
        "All Brands":           "Todas las Marcas",
        "Select a Location":    "Seleccionar Ubicación",
        "Select all Locations": "Seleccionar Todas las Ubicaciones",
        "All Locations selected":"Todas las Ubicaciones Seleccionadas",
        "Select a Period":      "Seleccionar Período",
        "Select all Periods":   "Seleccionar Todos los Períodos",
        "All Periods selected": "Todos los Períodos Seleccionados",
        "Period (Optional)":    "Período (Opcional)",
        "Select Company":       "Seleccionar Empresa",
        "Select Group":         "Seleccionar Grupo",
        "Select Brand":         "Seleccionar Marca",
        "Open Orders Only":     "Solo Órdenes Abiertas",
        "Closed Orders Only":   "Solo Órdenes Cerradas",
        "All Orders":           "Todas las Órdenes",
        "All Locs Selected":    "Todas las Locs Seleccionadas",
        "Select To Loc":        "Seleccionar Loc Destino",
        "Item Code / Barcode":  "Código de Artículo / Código de Barras",
        "Item / Barcode":       "Artículo / Código de Barras",
        "Product Code / Barcode":"Código de Producto / Código de Barras",
        "Draft Number":         "Número de Borrador",
        "Group:":               "Grupo:",
        "Date:":                "Fecha:",

        // ── Grid Headers ─────────────────────────────────────────────────
        "DocNum":               "Núm. Doc.",
        "Doc #":                "Doc #",
        "Doc Date":             "Fecha Doc.",
        "Trans Num":            "Núm. Trans.",
        "Tsf. Date":            "Fecha Tsf.",
        "Draft Date":           "Fecha Borrador",
        "Dispatch Date":        "Fecha Despacho",
        "Receipt Date":         "Fecha Recibo",
        "Origin Date":          "Fecha Origen",
        "Total Lines":          "Total Líneas",
        "Qty":                  "Cant.",
        "Dispatched":           "Despachado",
        "Received":             "Recibido",
        "Dispatch Completed":   "Despacho Completado",
        "Rcv. Completed":       "Recibo Completado",
        "Input Type":           "Tipo Entrada",
        "Scan Status":          "Estado Escaneo",
        "Dispatch":             "Despacho",
        "Transfer / Order":     "Transferencia / Orden",
        "Draft":                "Borrador",
        "Originator":           "Originador",
        "Dispatcher":           "Despachador",
        "Receiver":             "Receptor",
        "Stat":                 "Est.",
        "Item Code":            "Código Artículo",
        "Bar Code":             "Código de Barras",
        "Item Name":            "Nombre Artículo",
        "Item Desc.":           "Desc. Artículo",
        "Scanned Bar Code":     "Código Barras Escaneado",
        "Barcode":              "Código de Barras",
        "Default Barcode":      "Código de Barras por Defecto",
        "Possible Barcodes":    "Posibles Códigos de Barras",
        "SAP Code":             "Código SAP",
        "Loc Name":             "Nombre Loc.",
        "SOH":                  "Existencia",
        "Reserved":             "Reservado",
        "Available SOH":        "Existencia Disponible",
        "Local In Transit":     "En Tránsito Local",
        "Colon In Transit":     "En Tránsito Colón",
        "Total In Transit":     "Total en Tránsito",
        "Net Inventory":        "Inventario Neto",
        "Min":                  "Mín",
        "Max":                  "Máx",
        "OnHand":               "Existencia",
        "Minimum":              "Mínimo",
        "Maximum":              "Máximo",
        "Hold":                 "Espera",
        "Replacement Item":     "Artículo de Reemplazo",
        "Comment":              "Comentario",
        "Items per Box":        "Unidades por Caja",
        "Inventory":            "Inventario",
        "Pri":                  "Pri.",
        "Store #":              "Tienda #",
        "Warehouse Receipt":    "Recibo de Bodega",
        "Sales Order":          "Orden de Venta",
        "Dif":                  "Dif.",
        "Error Posted":         "Error Publicado",
        "Ori Client":           "Cliente Origen",
        "Des Company ID":       "ID Empresa Destino",
        "QO":                   "CO",
        "QD":                   "CD",
        "QR":                   "CR",
        "Error Fixed":          "Error Corregido",
        "Fixed":                "Corregido",
        "No Fixed":             "No Corregido",
        "Trans #":              "Trans #",
        "Trans Date":           "Fecha Trans.",
        "Trans Qty":            "Cant. Trans.",
        "Whs Qty":              "Cant. Bodega",
        "Whs Code":             "Código Bodega",
        "Error Message":        "Mensaje de Error",
        "Old Bar Code Item":    "Art. Cód. Barras Anterior",
        "New Item":             "Artículo Nuevo",
        "New Bar Code Item":    "Art. Cód. Barras Nuevo",
        "Base Doc":             "Doc. Base",
        "Qty Trans":            "Cant. Trans.",
        "Receive":              "Recibir",
        "From":                 "Origen",
        "To":                   "Destino",
        "St":                   "Est.",

        // ── Transfers ────────────────────────────────────────────────────
        "Create Transfer":          "Crear Transferencia",
        "Create Draft Transfer":    "Crear Borrador",
        "Submit Draft":             "Enviar Borrador",
        "Refresh Quantities":       "Actualizar Cantidades",
        "Delete Draft":             "Eliminar Borrador",
        "Enter Quantity":           "Ingresar Cantidad",
        "Select All (current page)":"Seleccionar Todo (página actual)",
        "Transfer Deletion":        "Eliminación de Transferencia",
        "Transfer Audit by Product":"Auditoría de Transferencias por Artículo",
        "# Transfer":               "# Transferencia",
        "Item Groups":              "Grupos de Artículos",
        "View Transfers":           "Ver Transferencias",
        "Transfer Audit":           "Auditoría de Transferencias",

        // ── Delivery / Orders ────────────────────────────────────────────
        "Show uncorrected errors only": "Mostrar solo errores no corregidos",
        "Show all errors":              "Mostrar todos los errores",
        "INTERNAL TRANSFERS":           "TRANSFERENCIAS INTERNAS",
        "MERCHANDISE IN TRANSIT FROM COLON": "MERCANCÍA EN TRÁNSITO DESDE COLÓN",
        "Orders Being Processed in Colon":   "Órdenes en Proceso en Colón",
        "Search Transfers / Orders":         "Buscar Transferencias / Órdenes",

        // ── Min-Max ──────────────────────────────────────────────────────
        "Min-Max Values":               "Valores Min-Max",
        "Store / Warehouse Fill Priority":"Prioridad de Llenado Tienda/Bodega",
        "Upload Min-Max by Excel":      "Cargar Min-Max por Excel",
        "View All Items":               "Ver Todos los Artículos",
        "View Hold Items Only":         "Ver Solo Artículos en Espera",
        "Update Minimums to":           "Actualizar Mínimos a",
        "Upload Min-Max":               "Cargar Min-Max",
        "Unplanned products with available inventory in Warehouses": "Productos no planificados con inventario disponible en Bodegas",
        "Green represents items with less than 30 days in the Operation": "Verde: artículos con menos de 30 días en la Operación",
        "Items with less than 30 days in the Operation are shown in green": "Artículos con menos de 30 días en la Operación se muestran en verde",
        "Products not yet in SAP":      "Productos aún no en SAP",

        // ── SAP / Inventory ──────────────────────────────────────────────
        "SAP Stock":                        "Inventario SAP",
        "Warehouse Receipt SAP APRI VS GRPOs":"Recibo de Bodega SAP APRI VS GRPOs",
        "Update Barcode by Items.":         "Actualizar Código de Barras por Artículos.",
        "Inventory Movement by Items":      "Movimiento de Inventario por Artículos",
        "No barcodes exist for this item.": "No existen códigos de barras para este artículo.",
        "#APRI / #Order":                   "#APRI / #Orden",
        "#APRI / #Order:":                  "#APRI / #Orden:",
        "Invoice:":                         "Factura:",
        "Status Rec:":                      "Estado Rec:",
        "Status SAP:":                      "Estado SAP:",
        "#Order:":                          "#Orden:",
        "Cant SAP":                         "Cant SAP",

        // ── Bins ─────────────────────────────────────────────────────────
        "Bin by Item":          "Bin por Artículo",
        "Upload Bins by Excel": "Cargar Bins por Excel",
        "Create Bins":          "Crear Bins",
        "Insert Bin":           "Insertar Bin",
        "Excel File":           "Archivo Excel",

        // ── FAQ / Videos ─────────────────────────────────────────────────
        "Search FAQs":              "Buscar Preguntas Frecuentes",
        "Search Operational Videos":"Buscar Videos Operativos",
        "Search Financial Videos":  "Buscar Videos Financieros",
        "Cases":                    "Casos",
        "Solution":                 "Solución",
        "Image":                    "Imagen",
        "Solution Link":            "Enlace de Solución"
    };

    // Build ES → EN reverse dictionary automatically
    var dict_en = {};
    for (var k in dict_es) {
        if (dict_es.hasOwnProperty(k)) {
            var v = dict_es[k];
            if (v && v !== k) dict_en[v] = k;
        }
    }

    // ── Translate a single element's visible text ─────────────────────────
    function xlate(el, dict) {
        if (!el) return;
        var t = (el.textContent || el.innerText || '').trim();
        if (t && dict[t] !== undefined) {
            // Use textContent only if the element has no child ELEMENTS
            // (to avoid destroying nested icons/images)
            if (el.children && el.children.length === 0) {
                el.textContent = dict[t];
            } else {
                // Walk direct text nodes only
                for (var i = 0; i < el.childNodes.length; i++) {
                    var node = el.childNodes[i];
                    if (node.nodeType === 3) {  // TEXT_NODE
                        var nt = node.textContent.trim();
                        if (nt && dict[nt] !== undefined) {
                            node.textContent = node.textContent.replace(nt, dict[nt]);
                        }
                    }
                }
            }
        }
    }

    // ── Translate input[type=button/submit] ───────────────────────────────
    function xlateInput(el, dict) {
        if (!el) return;
        var v = (el.value || '').trim();
        if (v && dict[v] !== undefined) el.value = dict[v];
    }

    // ── forEach shim for NodeList (IE compatibility) ──────────────────────
    function each(nodeList, fn) {
        for (var i = 0; i < nodeList.length; i++) fn(nodeList[i]);
    }

    // ── Main apply function ───────────────────────────────────────────────
    function applyLanguage(lang) {
        if (typeof lang === 'undefined' || lang === null) return;
        var dict = (lang === 'es') ? dict_es : dict_en;
        if (!dict) return;

        // Labels (covers Login: "User", "Password", "Select Company", panel heading)
        each(document.querySelectorAll('label'), function (el) { xlate(el, dict); });

        // Table / grid headers
        each(document.querySelectorAll('th'), function (el) { xlate(el, dict); });

        // Telerik RadGrid header links (sort buttons inside <th>)
        each(document.querySelectorAll('th a, th button'), function (el) { xlate(el, dict); });

        // Telerik RadMenu item text spans
        each(document.querySelectorAll('.rmText'), function (el) { xlate(el, dict); });

        // Buttons (input)
        each(document.querySelectorAll('input[type="button"], input[type="submit"]'), function (el) { xlateInput(el, dict); });

        // Buttons (<button>)
        each(document.querySelectorAll('button'), function (el) { xlate(el, dict); });

        // myLabel class (some pages use div/span with this class)
        each(document.querySelectorAll('.myLabel, .PanelHeading, .myLabelMedium, .myLabelSmall'), function (el) { xlate(el, dict); });

        // Legend / fieldset captions
        each(document.querySelectorAll('legend, caption'), function (el) { xlate(el, dict); });

        // Telerik dropdowns default item text (first option)
        each(document.querySelectorAll('select option'), function (el) { xlate(el, dict); });

        // Checkboxes and radio label text (span next to input)
        each(document.querySelectorAll('.myLabel span, .mybtn'), function (el) { xlate(el, dict); });
    }

    return { apply: applyLanguage };
})();

// ── ASP.NET hook: called after initial load AND every partial AJAX update ────
function pageLoad() {
    if (typeof SCI_Lang !== 'undefined') {
        SCI_I18n.apply(SCI_Lang);
    }
}

// jQuery fallback for initial page render
$(document).ready(function () {
    if (typeof SCI_Lang !== 'undefined') {
        SCI_I18n.apply(SCI_Lang);
    }
});
