using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using Newtonsoft.Json.Linq;

// Direct port of CreaFacturasVentasSAP's InvoiceOrchestrator for FacturacionSAP.aspx's
// "Upload Selected" button — creates OINV/ORIN + cobro/reembolso synchronously when the
// user clicks, instead of waiting for the scheduled console app.
//
// DFS-specific adaptations vs the ADA version in SCIv2:
//   - Upload() takes a transType (DF/DP) parameter — each duty type produces a separate OINV.
//   - All SQL queries join ADR_TIENDA_SERIE with ts.DUTYTYPE = ss.TRANSTYPE (la_store_sales
//     has a TRANSTYPE column already populated by CreaDocSalesSAP) instead of the ADA
//     StoreMapCte that groups by NUMSERIE+NUMSTORE only.
//   - Payment and total queries filter by @transType so DF and DP payments don't mix.
//   - la_daily_returns has a ReconNum column (added by the SQL setup script) for CompensarMixtas.
//
// Deliberate accepted risk: business logic is duplicated vs CreaFacturasVentasSAP — both
// must be kept in sync for any future changes to payment matching or tender mapping.
public class InvoiceUploader
{
    // Max difference (SAP total vs payments) absorbed as a rounding adjustment on the largest
    // line — ODLNs created before 2026-08-27 stored prices with 2 decimals (3 x 17.33 = 51.99);
    // on big stores (C3001, 3,000+ lines a day) the cents add up past 0.10.
    // Web.config appSetting FacturacionSAP_MaxRoundingAdjustment (missing or invalid = 1.00).
    // Also used by InvoicePreview so "Ready" matches what the upload accepts.
    public static decimal MaxRoundingAdjustment
    {
        get
        {
            decimal value;
            string setting = System.Configuration.ConfigurationManager.AppSettings["FacturacionSAP_MaxRoundingAdjustment"];
            return decimal.TryParse(setting, System.Globalization.NumberStyles.Number,
                       System.Globalization.CultureInfo.InvariantCulture, out value) && value >= 0
                ? value : 1.00m;
        }
    }

    public class UploadResult
    {
        public bool Success;
        public string Summary;
    }

    // (CardCode, TransType, FechaDoc) → one OINV + cobro + ORIN + reembolso + reconciliation.
    public UploadResult Upload(string sapDb, string cardCode, DateTime fecha, string transType, string userId)
    {
        SapServiceLayer sl = new SapServiceLayer();
        try
        {
            sl.Login(sapDb);
        }
        catch (Exception ex)
        {
            return new UploadResult { Success = false, Summary = "SAP login failed: " + ex.Message };
        }

        List<string> parts = new List<string>();
        try
        {
            string saleMsg = ProcessSide(sl, sapDb, cardCode, fecha, false, transType, userId);
            if (saleMsg != null) parts.Add(saleMsg);

            string returnMsg = ProcessSide(sl, sapDb, cardCode, fecha, true, transType, userId);
            if (returnMsg != null) parts.Add(returnMsg);

            string reconMsg = CompensarMixtas(sl, cardCode, fecha, transType);
            if (reconMsg != null) parts.Add(reconMsg);
        }
        catch (Exception ex)
        {
            // Reported in Upload Requests instead of bubbling up (Global.asax would redirect to Login1.aspx).
            parts.Add("Unexpected error: " + ex.Message);
        }
        finally
        {
            sl.Logout();
        }

        string summary = parts.Count > 0 ? string.Join("; ", parts) : "Nothing to create (blocked or already invoiced).";

        bool anyCreated = false;
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i].IndexOf("OINV #", StringComparison.OrdinalIgnoreCase) >= 0 ||
                parts[i].IndexOf("ORIN #", StringComparison.OrdinalIgnoreCase) >= 0)
                anyCreated = true;
        }

        return new UploadResult { Success = anyCreated, Summary = summary };
    }

    // "Retry Payments" (FacturacionSAP_RetryPayments): creates the cobro / reembolso of an OINV / ORIN
    // already created by Upload whose payment failed (e.g. an inactive G/L account). Only when the
    // document is still open and unpaid in SAP and its total matches the POS payments.
    public UploadResult RetryPayments(string sapDb, string cardCode, DateTime fecha, string transType)
    {
        SapServiceLayer sl = new SapServiceLayer();
        try
        {
            sl.Login(sapDb);
        }
        catch (Exception ex)
        {
            return new UploadResult { Success = false, Summary = "SAP login failed: " + ex.Message };
        }

        List<string> parts = new List<string>();
        bool anyCreated = false;
        try
        {
            foreach (bool esDevolucion in new[] { false, true })
            {
                string msg = RetryPaymentSide(sl, cardCode, fecha, esDevolucion, transType);
                if (msg == null) continue;
                parts.Add(msg);
                if (msg.IndexOf("payment #", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.IndexOf("refund #", StringComparison.OrdinalIgnoreCase) >= 0)
                    anyCreated = true;
            }

            string reconMsg = CompensarMixtas(sl, cardCode, fecha, transType);
            if (reconMsg != null) parts.Add(reconMsg);
        }
        finally
        {
            sl.Logout();
        }

        string summary = "Retry payments: " + (parts.Count > 0 ? string.Join("; ", parts) : "nothing pending.");
        return new UploadResult { Success = anyCreated, Summary = summary };
    }

    private string RetryPaymentSide(SapServiceLayer sl, string cardCode, DateTime fecha, bool esDevolucion, string transType)
    {
        string tipoDoc = esDevolucion ? "ORIN" : "OINV";
        string sql = esDevolucion
            ? "SELECT ReturnDocNum AS DocNum, ReturnDocEntry AS DocEntry, BPLId FROM dbo.la_daily_returns " +
              "WHERE CardCode = @cardCode AND FechaDoc = @fecha AND TransType = @transType " +
              "  AND ReturnDocNum > 0 AND ReturnDocEntry IS NOT NULL AND RefundDocNum IS NULL"
            : "SELECT InvoiceDocNum AS DocNum, InvoiceDocEntry AS DocEntry, BPLId FROM dbo.la_daily_invoices " +
              "WHERE CardCode = @cardCode AND FechaDoc = @fecha AND TransType = @transType " +
              "  AND InvoiceDocNum > 0 AND InvoiceDocEntry IS NOT NULL AND PaymentDocNum IS NULL";

        DataTable dt = RunPagosQuery(sql, cardCode, fecha, transType);
        if (dt.Rows.Count == 0) return null;

        int docNum = Convert.ToInt32(dt.Rows[0]["DocNum"]);
        int docEntry = Convert.ToInt32(dt.Rows[0]["DocEntry"]);
        int bplId = Convert.ToInt32(dt.Rows[0]["BPLId"]);
        string docTxt = tipoDoc + " #" + docNum;

        decimal docTotal, paidToDate;
        try
        {
            JObject bal = JObject.Parse(sl.GetDocBalance(esDevolucion ? "CreditNotes" : "Invoices", docEntry));
            docTotal = Convert.ToDecimal(bal["DocTotal"]);
            paidToDate = Convert.ToDecimal(bal["PaidToDate"]);
        }
        catch (WebException wex)
        {
            return docTxt + " error reading balance: " + SapServiceLayer.GetSlErrorMessage(wex);
        }

        if (paidToDate != 0)
            return docTxt + " skipped: already has " + paidToDate.ToString("N2") + " applied in SAP B1 (review manually).";

        DataTable pagos = esDevolucion
            ? GetPagosParaDevolucion(cardCode, fecha, transType)
            : GetPagosParaFactura(cardCode, fecha, transType);

        decimal totalPagado = 0m;
        foreach (DataRow r in pagos.Rows) totalPagado += Convert.ToDecimal(r["Amount"]);
        decimal compensacion = GetCompensacionMixtas(cardCode, fecha, transType);

        if (Math.Abs(docTotal - totalPagado - compensacion) > 0.005m)
            return docTxt + " blocked: SAP total (" + docTotal.ToString("N2") + ") does not match POS payments ("
                + totalPagado.ToString("N2") + ")" + (compensacion != 0 ? " + mixed-transaction offset " + compensacion.ToString("N2") : "") + ".";

        if (compensacion > 0 && Math.Abs(totalPagado) <= 0.01m)
        {
            if (esDevolucion) SetRefund(cardCode, fecha, transType, 0);
            else SetPayment(cardCode, fecha, transType, 0);
            return docTxt + " (fully offset " + compensacion.ToString("N2") + ", no " + (esDevolucion ? "refund" : "payment") + ")";
        }

        string mappingError, cuentaEfectivo;
        decimal efectivoContrario;
        JObject paymentRequest = BuildPaymentRequest(sl, cardCode, fecha, bplId, pagos, GetTenderMappings(), esDevolucion, out mappingError,
            out efectivoContrario, out cuentaEfectivo);
        if (paymentRequest == null)
            return docTxt + " blocked: " + mappingError;

        return CrearPago(sl, paymentRequest, docEntry, totalPagado, esDevolucion, cardCode, fecha, transType, docTxt,
            efectivoContrario, cuentaEfectivo);
    }

    // ── One side (Sale or Return) of a CardCode+Fecha+TransType group ───────────────
    private string ProcessSide(SapServiceLayer sl, string sapDb, string cardCode, DateTime fecha, bool esDevolucion, string transType, string userId)
    {
        int txnmodifier = esDevolucion ? 1 : 0;
        string tipoDoc = esDevolucion ? "ORIN" : "OINV";
        string ladoTxt = esDevolucion ? "Return" : "Sale";

        int bplId;
        List<int> deliveryDocEntries;
        bool hasItems = GetPendingSideInfo(cardCode, fecha, txnmodifier, esDevolucion, transType,
            out bplId, out deliveryDocEntries);
        if (!hasItems) return null;

        if (HayEntregasPendientes(cardCode, fecha, transType))
            return ladoTxt + " blocked: pending deliveries (ODLN/ORDN not yet complete).";
        if (HayErroresPendientes(cardCode, fecha, transType))
            return ladoTxt + " blocked: pending delivery errors (la_delivery_errors).";

        // The day must match the POS: sales or payments the POS has and SCI doesn't would leave the
        // invoice incomplete even though SAP total and loaded payments agree.
        string posGap = InvoicePreview.GetPosGapMessage(cardCode, transType, fecha, bplId);
        if (posGap != null)
            return ladoTxt + " blocked: " + posGap;

        // Deliveries holding sales of several days: only the lines of this day are invoiced.
        int mixBlockedDoc;
        Dictionary<int, decimal> mixExpenses;
        Dictionary<int, HashSet<int>> mixLines = GetMixedDeliveryLines(sapDb, fecha, txnmodifier, deliveryDocEntries, out mixBlockedDoc, out mixExpenses);
        if (mixBlockedDoc != 0)
            return ladoTxt + " blocked: " + (esDevolucion ? "ORDN" : "ODLN") + " #" + mixBlockedDoc
                + " mixes POS sales of several days and its lines can't be split by day. Manual resolution required in SAP B1.";

        string entity = esDevolucion ? "Returns" : "DeliveryNotes";
        int baseType = esDevolucion ? 16 : 15;
        JArray lines = new JArray();
        Dictionary<int, JObject> expensesByCode = new Dictionary<int, JObject>();
        decimal invoiceTotal = 0m;
        JObject maxLine = null;          // largest base-document line, receives the rounding adjustment
        decimal maxLineTotal = 0m;

        // Open lines of every delivery in one query: reading them one by one through the Service
        // Layer took 3.6 min for C3001's ~2,000 daily deliveries.
        HashSet<int> docsWithExpenses;
        Dictionary<int, List<KeyValuePair<int, decimal>>> openLines =
            GetOpenDeliveryLines(sapDb, esDevolucion, deliveryDocEntries, out docsWithExpenses);

        for (int i = 0; i < deliveryDocEntries.Count; i++)
        {
            int docEntry = deliveryDocEntries[i];

            HashSet<int> dayLines;
            mixLines.TryGetValue(docEntry, out dayLines);

            List<KeyValuePair<int, decimal>> docLines;
            if (openLines.TryGetValue(docEntry, out docLines))
            {
                foreach (KeyValuePair<int, decimal> line in docLines)
                {
                    int lineNum = line.Key;
                    if (dayLines != null && !dayLines.Contains(lineNum)) continue;   // belongs to another day
                    decimal lineTotal = line.Value;

                    JObject invLine = new JObject();
                    invLine["BaseType"] = baseType;
                    invLine["BaseEntry"] = docEntry;
                    invLine["BaseLine"] = lineNum;
                    lines.Add(invLine);
                    invoiceTotal += lineTotal;
                    if (lineTotal > maxLineTotal)
                    {
                        maxLine = invLine;
                        maxLineTotal = lineTotal;
                    }
                }
            }

            if (!docsWithExpenses.Contains(docEntry)) continue;

            // Additional expenses (few documents): read through the Service Layer as before.
            JObject doc;
            try
            {
                string json = sl.GetDocumentForInvoice(entity, docEntry);
                doc = JObject.Parse(json);
            }
            catch (WebException wex)
            {
                return ladoTxt + " error reading " + entity + " #" + docEntry + ": " + SapServiceLayer.GetSlErrorMessage(wex);
            }

            JArray docExpenses = doc["DocumentAdditionalExpenses"] as JArray;
            if (docExpenses != null)
            {
                foreach (JToken expToken in docExpenses)
                {
                    JObject exp = (JObject)expToken;
                    int expnsCode = exp["ExpenseCode"] != null ? Convert.ToInt32(exp["ExpenseCode"]) : 0;
                    decimal expTotal = exp["LineTotal"] != null ? Convert.ToDecimal(exp["LineTotal"]) : 0m;

                    // Delivery mixing several days: only this day's share of its tax (one expense code,
                    // checked by InvoicePreview.MixedDeliveriesSql).
                    // The share is used once per delivery even if its expense comes in several rows.
                    decimal share;
                    if (dayLines != null && expTotal != 0)
                    {
                        expTotal = mixExpenses.TryGetValue(docEntry, out share) ? share : 0m;
                        mixExpenses.Remove(docEntry);
                    }
                    if (expTotal == 0) continue;

                    if (expensesByCode.ContainsKey(expnsCode))
                    {
                        decimal existing = Convert.ToDecimal(expensesByCode[expnsCode]["LineTotal"]);
                        expensesByCode[expnsCode]["LineTotal"] = existing + expTotal;
                    }
                    else
                    {
                        JObject newExp = new JObject();
                        newExp["ExpenseCode"] = expnsCode;
                        newExp["LineTotal"] = expTotal;
                        newExp["TaxCode"] = exp["TaxCode"];
                        newExp["DistributionMethod"] = exp["DistMethod"];
                        newExp["Billable"] = exp["Billable"];
                        newExp["VatGroup"] = exp["VatGroup"];
                        expensesByCode[expnsCode] = newExp;
                    }
                    invoiceTotal += expTotal;
                }
            }
        }

        if (lines.Count == 0)
        {
            string tipoDocBloqueado = esDevolucion ? "ORDN" : "ODLN";
            string motivo = "All " + tipoDocBloqueado + " lines are already invoiced (bost_Close). Manual resolution required in SAP B1.";
            if (!esDevolucion) MarkInvoiceError(cardCode, fecha, transType, motivo);
            return ladoTxt + " blocked: " + motivo;
        }

        // Mixed-transaction returns (DeliveryDocNum = -3): negative lines in the daily invoice.
        if (!esDevolucion)
        {
            DataTable devMixtas = GetDevolucionesMixtas(cardCode, fecha, transType);
            foreach (DataRow d in devMixtas.Rows)
            {
                decimal qty = Convert.ToDecimal(d["Qty"]);
                decimal monto = Convert.ToDecimal(d["Monto"]);
                JObject negLine = new JObject();
                negLine["ItemCode"] = d["ItemCode"].ToString();
                negLine["Quantity"] = -qty;
                negLine["UnitPrice"] = qty > 0 ? Math.Round(monto / qty, 6) : monto;
                if (d["WhsCode"] != DBNull.Value) negLine["WarehouseCode"] = d["WhsCode"].ToString();
                lines.Add(negLine);
                invoiceTotal -= monto;
            }
        }

        // ── Validate payments match invoice total ─────────────────────────────────
        DataTable pagos = esDevolucion
            ? GetPagosParaDevolucion(cardCode, fecha, transType)
            : GetPagosParaFactura(cardCode, fecha, transType);

        decimal totalPagado = 0m;
        foreach (DataRow r in pagos.Rows) totalPagado += Convert.ToDecimal(r["Amount"]);

        decimal compensacion = GetCompensacionMixtas(cardCode, fecha, transType);
        string compTxt = compensacion != 0 ? " + mixed-transaction offset " + compensacion.ToString("N2") : "";

        // Cent differences (<= MaxRoundingAdjustment) are absorbed on the largest line: sending an
        // explicit LineTotal makes SAP keep the base price and post the difference as a line
        // discount (tested in TEST_PRO_DFSC). A negative header TotalDiscount is silently ignored.
        decimal diferencia = Math.Round(invoiceTotal - totalPagado - compensacion, 2);
        decimal ajuste = 0m;
        if (diferencia != 0 && Math.Abs(diferencia) <= MaxRoundingAdjustment
            && maxLine != null && maxLineTotal > Math.Abs(diferencia))
        {
            ajuste = -diferencia;
            maxLine["LineTotal"] = Math.Round(maxLineTotal + ajuste, 2);
            invoiceTotal += ajuste;
        }
        else if (Math.Abs(diferencia) > 0.01m)
        {
            string motivo = esDevolucion
                ? "Credit memo total (" + invoiceTotal.ToString("N2") + ") does not match total refunded in payments (" + totalPagado.ToString("N2") + ")" + compTxt + "."
                : "Invoice total (" + invoiceTotal.ToString("N2") + ") does not match total collected in payments (" + totalPagado.ToString("N2") + ")" + compTxt + ".";
            return ladoTxt + " blocked: " + motivo;
        }

        bool sinDineroReal = compensacion > 0 && Math.Abs(totalPagado) <= 0.01m;

        // ── Tender -> SAP account mapping ─────────────────────────────────────────
        JObject paymentRequest = null;
        decimal efectivoContrario = 0m;
        string cuentaEfectivo = null;
        if (!sinDineroReal)
        {
            DataTable tenderMappings = GetTenderMappings();
            string mappingError;
            paymentRequest = BuildPaymentRequest(sl, cardCode, fecha, bplId, pagos, tenderMappings, esDevolucion, out mappingError,
                out efectivoContrario, out cuentaEfectivo);
            if (paymentRequest == null)
                return ladoTxt + " blocked: " + mappingError;
        }

        // ── Create OINV / ORIN ────────────────────────────────────────────────────
        string uType = transType != null && transType.Equals("DP", StringComparison.OrdinalIgnoreCase)
            ? "Duty Paid" : "Duty Free";
        string fechaStr = fecha.ToString("yyyy-MM-dd");

        JObject invoiceRequest = new JObject();
        invoiceRequest["CardCode"] = cardCode;
        invoiceRequest["BPL_IDAssignedToInvoice"] = bplId;
        invoiceRequest["DocDate"] = fechaStr;
        invoiceRequest["TaxDate"] = fechaStr;
        invoiceRequest["DocDueDate"] = fechaStr;
        invoiceRequest["NumAtCard"] = fechaStr;
        invoiceRequest["U_Type"] = uType;
        invoiceRequest["Comments"] = "Manually uploaded from SCI (FacturacionSAP.aspx) by " + userId + " on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "."
            + (ajuste != 0 ? " Rounding adjustment " + ajuste.ToString("+0.00;-0.00") + " on largest line to match POS payments." : "");
        invoiceRequest["DocumentLines"] = lines;
        if (expensesByCode.Count > 0)
        {
            JArray expArray = new JArray();
            foreach (JObject v in expensesByCode.Values) expArray.Add(v);
            invoiceRequest["DocumentAdditionalExpenses"] = expArray;
        }

        int newDocNum = 0, newDocEntry = 0;
        decimal? newDocTotal = null;
        try
        {
            string resp = esDevolucion
                ? sl.CreateCreditNote(invoiceRequest.ToString(Newtonsoft.Json.Formatting.None))
                : sl.CreateInvoice(invoiceRequest.ToString(Newtonsoft.Json.Formatting.None));
            JObject respObj = JObject.Parse(resp);
            newDocEntry = respObj["DocEntry"] != null ? Convert.ToInt32(respObj["DocEntry"]) : 0;
            newDocNum = respObj["DocNum"] != null ? Convert.ToInt32(respObj["DocNum"]) : 0;
            if (respObj["DocTotal"] != null) newDocTotal = Convert.ToDecimal(respObj["DocTotal"]);
        }
        catch (WebException wex)
        {
            string err = SapServiceLayer.GetSlErrorMessage(wex);
            if (!esDevolucion) MarkInvoiceError(cardCode, fecha, transType, err);
            return ladoTxt + " error: SAP rejected " + tipoDoc + " creation - " + err;
        }

        if (esDevolucion) UpsertReturn(cardCode, bplId, fecha, transType, newDocNum, newDocEntry);
        else UpsertInvoice(cardCode, bplId, fecha, transType, newDocNum, newDocEntry);

        string docTxt = tipoDoc + " #" + newDocNum + (ajuste != 0 ? " (rounding adj. " + ajuste.ToString("+0.00;-0.00") + ")" : "");

        // The adjusted document must match what was collected before the payment is created.
        if (ajuste != 0 && newDocTotal.HasValue && Math.Abs(newDocTotal.Value - invoiceTotal) > 0.005m)
            return docTxt + " (" + (esDevolucion ? "refund" : "payment") + " NOT created) - SAP total "
                + newDocTotal.Value.ToString("N2") + " does not match expected " + invoiceTotal.ToString("N2") + "; review in SAP B1.";

        if (sinDineroReal)
        {
            if (esDevolucion) SetRefund(cardCode, fecha, transType, 0);
            else SetPayment(cardCode, fecha, transType, 0);
            return docTxt + " (fully offset " + compensacion.ToString("N2") + ", no " + (esDevolucion ? "refund" : "payment") + ")";
        }

        return CrearPago(sl, paymentRequest, newDocEntry, totalPagado, esDevolucion, cardCode, fecha, transType, docTxt,
            efectivoContrario, cuentaEfectivo);
    }

    // ── Create cobro / reembolso applied to an OINV / ORIN ────────────────────────
    private string CrearPago(SapServiceLayer sl, JObject paymentRequest, int docEntry, decimal totalPagado,
        bool esDevolucion, string cardCode, DateTime fecha, string transType, string docTxt,
        decimal efectivoContrario, string cuentaEfectivo)
    {
        JArray paymentInvoices = new JArray();
        JObject paymentInvoice = new JObject();
        paymentInvoice["LineNum"] = 0;
        paymentInvoice["DocEntry"] = docEntry;
        paymentInvoice["InvoiceType"] = esDevolucion ? "it_CredItnote" : "it_Invoice";
        paymentInvoice["SumApplied"] = totalPagado;
        paymentInvoices.Add(paymentInvoice);
        paymentRequest["PaymentInvoices"] = paymentInvoices;

        try
        {
            string payResp = esDevolucion
                ? sl.CreateOutgoingPayment(paymentRequest.ToString(Newtonsoft.Json.Formatting.None))
                : sl.CreateIncomingPayment(paymentRequest.ToString(Newtonsoft.Json.Formatting.None));
            JObject payObj = JObject.Parse(payResp);
            int payDocNum = payObj["DocNum"] != null ? Convert.ToInt32(payObj["DocNum"]) : 0;

            if (esDevolucion) SetRefund(cardCode, fecha, transType, payDocNum);
            else SetPayment(cardCode, fecha, transType, payDocNum);

            string res = docTxt + " + " + (esDevolucion ? "refund #" : "payment #") + payDocNum;
            if (efectivoContrario > 0)
            {
                int payDocEntry = payObj["DocEntry"] != null ? Convert.ToInt32(payObj["DocEntry"]) : 0;
                res += CrearEfectivoContrario(sl, paymentRequest, payDocEntry, esDevolucion, cardCode, fecha, efectivoContrario, cuentaEfectivo);
            }
            return res;
        }
        catch (WebException wex)
        {
            string err = SapServiceLayer.GetSlErrorMessage(wex);
            return docTxt + " (" + (esDevolucion ? "refund" : "payment") + " pending) - " + err;
        }
    }

    // Cash that went the other way (see BuildPaymentRequest): the main payment left that amount on
    // account, so an opposite cash payment (cobro for a refund, reembolso for a sale) is created on
    // account and both are reconciled. Errors are reported in the summary for manual follow-up in SAP.
    private string CrearEfectivoContrario(SapServiceLayer sl, JObject mainRequest, int mainDocEntry, bool esDevolucion,
        string cardCode, DateTime fecha, decimal monto, string cuentaEfectivo)
    {
        string txt = esDevolucion ? "cash received " : "cash paid out ";
        JObject req = new JObject();
        req["CardCode"] = cardCode;
        req["DocDate"] = fecha.ToString("yyyy-MM-dd");
        if (mainRequest["BPLID"] != null) req["BPLID"] = mainRequest["BPLID"];
        if (!esDevolucion) req["DocType"] = "rCustomer";
        req["CashAccount"] = cuentaEfectivo;
        req["CashSum"] = monto;
        req["Remarks"] = "POS cash in the opposite direction of the " + (esDevolucion ? "refund" : "payment") + " (on account, reconciled).";

        int oppDocEntry, oppDocNum;
        try
        {
            string resp = esDevolucion
                ? sl.CreateIncomingPayment(req.ToString(Newtonsoft.Json.Formatting.None))
                : sl.CreateOutgoingPayment(req.ToString(Newtonsoft.Json.Formatting.None));
            JObject o = JObject.Parse(resp);
            oppDocEntry = Convert.ToInt32(o["DocEntry"]);
            oppDocNum = Convert.ToInt32(o["DocNum"]);
        }
        catch (WebException wex)
        {
            return " (" + txt + monto.ToString("N2") + " NOT created - " + SapServiceLayer.GetSlErrorMessage(wex)
                + "; create it on account in SAP B1 and reconcile)";
        }

        string oppTxt = " + " + txt + (esDevolucion ? "payment #" : "refund #") + oppDocNum + " (" + monto.ToString("N2") + ")";

        try
        {
            // main payment: refund = VendorPayments, sale = IncomingPayments; the opposite one is the other.
            int mainTrans = FirstJournalEntry(sl, esDevolucion, mainDocEntry);
            int oppTrans = FirstJournalEntry(sl, !esDevolucion, oppDocEntry);

            JArray filas = new JArray();
            foreach (int transId in new[] { mainTrans, oppTrans })
            {
                JObject je = JObject.Parse(sl.GetJournalEntry(transId));
                JObject linea = null;
                foreach (JToken l in (JArray)je["JournalEntryLines"])
                    if (string.Equals(l["ShortName"].ToString(), cardCode, StringComparison.OrdinalIgnoreCase)) { linea = (JObject)l; break; }
                if (linea == null)
                    return oppTxt + " (not reconciled: journal entry " + transId + " has no line for " + cardCode + ")";

                JObject fila = new JObject();
                fila["Selected"] = "tYES";
                fila["ShortName"] = cardCode;
                fila["TransId"] = transId;
                fila["TransRowId"] = Convert.ToInt32(linea["Line_ID"]);
                fila["CreditOrDebit"] = Convert.ToDecimal(linea["Debit"]) != 0 ? "codDebit" : "codCredit";
                fila["ReconcileAmount"] = monto;
                filas.Add(fila);
            }

            JObject recon = new JObject();
            recon["ReconDate"] = fecha.ToString("yyyy-MM-dd");
            recon["CardOrAccount"] = "coaCard";
            recon["InternalReconciliationOpenTransRows"] = filas;
            JObject r = JObject.Parse(sl.CreateInternalReconciliation(recon.ToString(Newtonsoft.Json.Formatting.None)));
            return oppTxt + " reconciled #" + r["ReconNum"];
        }
        catch (Exception ex)
        {
            string err = ex is WebException ? SapServiceLayer.GetSlErrorMessage((WebException)ex) : ex.Message;
            return oppTxt + " (not reconciled: " + err + ")";
        }
    }

    private static int FirstJournalEntry(SapServiceLayer sl, bool outgoing, int docEntry)
    {
        JArray v = (JArray)JObject.Parse(sl.GetPaymentJournalEntries(outgoing, docEntry))["value"];
        if (v == null || v.Count == 0) throw new Exception("journal entry of " + (outgoing ? "refund" : "payment") + " " + docEntry + " not found");
        return Convert.ToInt32(v[0]["JdtNum"]);
    }

    // ── Delivery doc entries for one side + transType, not yet invoiced ───────────
    private bool GetPendingSideInfo(string cardCode, DateTime fecha, int txnmodifier, bool esDevolucion,
        string transType, out int bplId, out List<int> deliveryDocEntries)
    {
        bplId = 0;
        deliveryDocEntries = new List<int>();

        string notExistsClause = esDevolucion
            ? "AND NOT EXISTS (SELECT 1 FROM dbo.la_daily_returns dr WHERE dr.CardCode = ts.CARCODE AND dr.FechaDoc = CAST(ss.itemdatetime AS DATE) AND dr.TransType = @transType AND dr.ReturnDocNum > 0)"
            : "AND NOT EXISTS (SELECT 1 FROM dbo.la_daily_invoices di WHERE di.CardCode = ts.CARCODE AND di.FechaDoc = CAST(ss.itemdatetime AS DATE) AND di.TransType = @transType AND di.InvoiceDocNum > 0)";

        string sql =
            "SELECT DISTINCT ss.DeliveryDocEntry, CAST(ts.CompanyId AS INT) AS BPLId " +
            "FROM dbo.la_store_sales ss " +
            "INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE " +
            "WHERE ts.CARCODE = @cardCode AND CAST(ss.itemdatetime AS DATE) = @fecha " +
            "  AND ss.txnmodifier = @txnmodifier AND ss.TRANSTYPE = @transType " +
            "  AND ss.DeliveryDocNum > 0 AND ss.DeliveryDocEntry IS NOT NULL AND ss.DeliveryDocEntry <> 99999999 " +
            "  " + notExistsClause;

        DataTable dt = new DataTable();
        SqlDb db = new SqlDb();
        db.Connect();
        try
        {
            using (SqlCommand cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@cardCode", cardCode);
                cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                cmd.Parameters.AddWithValue("@txnmodifier", txnmodifier);
                cmd.Parameters.AddWithValue("@transType", transType ?? "");
                db.adapter.SelectCommand = cmd;
                db.adapter.Fill(dt);
            }
        }
        finally { db.Disconnect(); }

        if (dt.Rows.Count == 0) return false;

        bplId = Convert.ToInt32(dt.Rows[0]["BPLId"]);
        foreach (DataRow r in dt.Rows)
            deliveryDocEntries.Add(Convert.ToInt32(r["DeliveryDocEntry"]));

        return true;
    }

    // ── Lines of this day in deliveries that mix POS sales of several days ────────
    // Same pairing as the preview (InvoicePreview.MixedDeliveriesSql). Returns docEntry → line
    // numbers belonging to @fecha, only for mixed deliveries; deliveries not in the dictionary are
    // copied whole. blockedDoc <> 0 when a mixed delivery can't be split safely. expenseShares:
    // this day's share of each mixed delivery's additional expenses (tax).
    private Dictionary<int, HashSet<int>> GetMixedDeliveryLines(string sapDb, DateTime fecha, int txnmodifier,
        List<int> deliveryDocEntries, out int blockedDoc, out Dictionary<int, decimal> expenseShares)
    {
        blockedDoc = 0;
        expenseShares = new Dictionary<int, decimal>();
        Dictionary<int, HashSet<int>> result = new Dictionary<int, HashSet<int>>();
        if (deliveryDocEntries.Count == 0) return result;

        string sql =
            "SET NOCOUNT ON;\n" +
            "CREATE TABLE #mixcand (DeliveryDocEntry BIGINT, txnmodifier INT);\n" +
            InsertDocEntriesSql("#mixcand", deliveryDocEntries, ", " + txnmodifier) +
            string.Format(InvoicePreview.MixedDeliveriesSql, sapDb) +
            "SELECT md.DocEntry, md.Splittable, ml.LineNum FROM #mixdocs md " +
            "LEFT JOIN #mixlines ml ON ml.DocEntry = md.DocEntry AND ml.txnmodifier = md.txnmodifier AND ml.FechaDoc = @fecha;\n" +
            "SELECT me.DocEntry, me.Monto FROM #mixexp me INNER JOIN #mixdocs md ON md.DocEntry = me.DocEntry AND md.txnmodifier = me.txnmodifier " +
            "WHERE me.FechaDoc = @fecha;";

        DataSet ds = new DataSet();
        SqlDb db = new SqlDb();
        db.Connect();
        try
        {
            using (SqlCommand cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                cmd.CommandTimeout = 300;
                db.adapter.SelectCommand = cmd;
                db.adapter.Fill(ds);
            }
        }
        finally { db.Disconnect(); }

        foreach (DataRow r in ds.Tables[0].Rows)
        {
            int docEntry = Convert.ToInt32(r["DocEntry"]);
            if (Convert.ToInt32(r["Splittable"]) == 0) { blockedDoc = docEntry; return result; }

            HashSet<int> lines;
            if (!result.TryGetValue(docEntry, out lines)) { lines = new HashSet<int>(); result[docEntry] = lines; }
            if (r["LineNum"] != DBNull.Value) lines.Add(Convert.ToInt32(r["LineNum"]));
        }
        foreach (DataRow r in ds.Tables[1].Rows)
            expenseShares[Convert.ToInt32(r["DocEntry"])] = Convert.ToDecimal(r["Monto"]);
        return result;
    }

    // ── Open lines (LineNum, LineTotal) of the deliveries, read from the SAP DB ────
    // docsWithExpenses: deliveries with DLN3/RDN3 rows, whose expenses are still read through the
    // Service Layer (ProcessSide).
    private Dictionary<int, List<KeyValuePair<int, decimal>>> GetOpenDeliveryLines(string sapDb, bool esDevolucion,
        List<int> deliveryDocEntries, out HashSet<int> docsWithExpenses)
    {
        Dictionary<int, List<KeyValuePair<int, decimal>>> result = new Dictionary<int, List<KeyValuePair<int, decimal>>>();
        docsWithExpenses = new HashSet<int>();
        if (deliveryDocEntries.Count == 0) return result;

        string lineTable = esDevolucion ? "RDN1" : "DLN1";
        string expTable = esDevolucion ? "RDN3" : "DLN3";
        string sql =
            "SET NOCOUNT ON;\n" +
            "CREATE TABLE #docs (DocEntry INT PRIMARY KEY);\n" +
            InsertDocEntriesSql("#docs", deliveryDocEntries, "") +
            "SELECT l.DocEntry, l.LineNum, l.LineTotal FROM " + sapDb + ".." + lineTable + " l\n" +
            "INNER JOIN #docs d ON d.DocEntry = l.DocEntry\n" +
            "WHERE l.LineStatus = 'O'\n" +
            // Lines never to be invoiced (see InvoicePreview SapLines / Scripts/Reset_Old_Mixed_ORDN.sql).
            "  AND NOT EXISTS (SELECT 1 FROM dbo.la_invoice_excluded_lines xl WHERE xl.BaseType = " + (esDevolucion ? 16 : 15) +
            " AND xl.DocEntry = l.DocEntry AND xl.LineNum = l.LineNum)\n" +
            "ORDER BY l.DocEntry, l.VisOrder, l.LineNum;\n" +
            "SELECT DISTINCT e.DocEntry FROM " + sapDb + ".." + expTable + " e INNER JOIN #docs d ON d.DocEntry = e.DocEntry\n" +
            // LineNum -1 = the document's additional expenses are excluded (see InvoicePreview SapLines).
            "WHERE NOT EXISTS (SELECT 1 FROM dbo.la_invoice_excluded_lines xl WHERE xl.BaseType = " + (esDevolucion ? 16 : 15) +
            " AND xl.DocEntry = e.DocEntry AND xl.LineNum = -1);";

        DataSet ds = new DataSet();
        SqlDb db = new SqlDb();
        db.Connect();
        try
        {
            using (SqlCommand cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.CommandTimeout = 300;
                db.adapter.SelectCommand = cmd;
                db.adapter.Fill(ds);
            }
        }
        finally { db.Disconnect(); }

        foreach (DataRow r in ds.Tables[0].Rows)
        {
            int docEntry = Convert.ToInt32(r["DocEntry"]);
            List<KeyValuePair<int, decimal>> lines;
            if (!result.TryGetValue(docEntry, out lines)) { lines = new List<KeyValuePair<int, decimal>>(); result[docEntry] = lines; }
            lines.Add(new KeyValuePair<int, decimal>(Convert.ToInt32(r["LineNum"]), Convert.ToDecimal(r["LineTotal"])));
        }
        foreach (DataRow r in ds.Tables[1].Rows)
            docsWithExpenses.Add(Convert.ToInt32(r["DocEntry"]));
        return result;
    }

    // INSERT statements for a temp table of DocEntries; a table value constructor takes at most
    // 1000 rows (C3001 has ~2,000 deliveries a day). extraValues is appended to each row, e.g. ", 0".
    private static string InsertDocEntriesSql(string table, List<int> docEntries, string extraValues)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < docEntries.Count; i += 1000)
        {
            List<string> values = new List<string>();
            for (int j = i; j < docEntries.Count && j < i + 1000; j++)
                values.Add("(" + docEntries[j] + extraValues + ")");
            sb.Append("INSERT INTO " + table + " VALUES " + string.Join(", ", values) + ";\n");
        }
        return sb.ToString();
    }

    private bool HayEntregasPendientes(string cardCode, DateTime fecha, string transType)
    {
        string sql =
            "SELECT CASE WHEN EXISTS (" +
            "    SELECT 1 FROM dbo.la_store_sales ss " +
            "    INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE " +
            "    WHERE ts.CARCODE = @cardCode AND CAST(ss.itemdatetime AS DATE) = @fecha AND ss.TRANSTYPE = @transType " +
            "      AND (ss.DeliveryDocNum IS NULL OR ss.DeliveryDocNum = -1)" +
            ") THEN 1 ELSE 0 END";

        return RunScalar(sql, cardCode, fecha, transType) == 1;
    }

    // DF and DP are independent documents: only errors of the same TRANSTYPE block this group.
    private bool HayErroresPendientes(string cardCode, DateTime fecha, string transType)
    {
        string sql =
            "SELECT CASE WHEN EXISTS (" +
            "    SELECT 1 FROM dbo.la_delivery_errors de " +
            "    INNER JOIN dbo.la_store_sales ss ON (" +
            "        (de.TransactionID IS NOT NULL AND ss.TransactionID = de.TransactionID AND ss.itemnum = de.itemnum)" +
            "        OR (de.TransactionID IS NULL AND ss.transnum = de.transnum AND ss.itemnum = de.itemnum" +
            "            AND CAST(ss.itemdatetime AS DATE) = CAST(de.itemdatetime AS DATE))" +
            "    )" +
            "    INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE " +
            "    WHERE ts.CARCODE = @cardCode AND CAST(ss.itemdatetime AS DATE) = @fecha AND ss.TRANSTYPE = @transType" +
            ") THEN 1 ELSE 0 END";

        SqlDb db = new SqlDb();
        db.Connect();
        try
        {
            using (SqlCommand cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@cardCode", cardCode);
                cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                cmd.Parameters.AddWithValue("@transType", transType ?? "");
                return Convert.ToInt32(cmd.ExecuteScalar()) == 1;
            }
        }
        finally { db.Disconnect(); }
    }

    private int RunScalar(string sql, string cardCode, DateTime fecha, string transType)
    {
        SqlDb db = new SqlDb();
        db.Connect();
        try
        {
            using (SqlCommand cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@cardCode", cardCode);
                cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                cmd.Parameters.AddWithValue("@transType", transType ?? "");
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
        finally { db.Disconnect(); }
    }

    // Mixed-transaction returns that go as negative lines in the OINV (DeliveryDocNum = -3).
    private DataTable GetDevolucionesMixtas(string cardCode, DateTime fecha, string transType)
    {
        string sql =
            "SELECT ss.skunum AS ItemCode, ABS(ss.qty) AS Qty, ABS(ss.extsellprice) AS Monto, ss.WhsCode " +
            "FROM dbo.la_store_sales ss " +
            "INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE " +
            "WHERE ts.CARCODE = @cardCode AND CAST(ss.itemdatetime AS DATE) = @fecha " +
            "  AND ss.txnmodifier = 1 AND ss.DeliveryDocNum = -3 AND ss.TRANSTYPE = @transType " +
            "ORDER BY ss.TransactionID, ss.itemnum";
        return RunPagosQuery(sql, cardCode, fecha, transType);
    }

    // TxnTotals CTE filtered by transType so DF and DP payments don't mix. Keyed by TransactionID:
    // la_store_sales.transnum repeats across days (~18% of tickets), TransactionID is unique.
    private string MakeTxnTotalsCte()
    {
        return
            "(SELECT TransactionID, transnum, " +
            "        SUM(CASE WHEN txnmodifier = 0 THEN ABS(extsellprice) + ABS(extsellprice) - ABS(ISNULL(MontoSinImpuesto, extsellprice)) ELSE 0 END) AS VentaTotal, " +
            "        SUM(CASE WHEN txnmodifier = 1 THEN ABS(extsellprice) + ABS(extsellprice) - ABS(ISNULL(MontoSinImpuesto, extsellprice)) ELSE 0 END) AS DevTotal, " +
            "        MAX(CASE WHEN DeliveryDocNum = -3 THEN 1 ELSE 0 END) AS EnFactura " +
            " FROM dbo.la_store_sales WHERE ISNULL(DeliveryDocNum, -1) <> 0 AND TRANSTYPE = @transType GROUP BY TransactionID, transnum)";
    }

    private decimal GetCompensacionMixtas(string cardCode, DateTime fecha, string transType)
    {
        string sql =
            "WITH TxnTotals AS " + MakeTxnTotalsCte() + " " +
            "SELECT ISNULL(SUM(CASE WHEN tt.VentaTotal < tt.DevTotal THEN tt.VentaTotal ELSE tt.DevTotal END), 0) " +
            "FROM TxnTotals tt " +
            "WHERE tt.VentaTotal > 0 AND tt.DevTotal > 0 AND tt.EnFactura = 0 " +
            "  AND EXISTS (SELECT 1 FROM dbo.la_store_sales ss " +
            "              INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE " +
            "              WHERE ss.TransactionID = tt.TransactionID AND ss.transnum = tt.transnum AND ts.CARCODE = @cardCode AND CAST(ss.itemdatetime AS DATE) = @fecha)";

        SqlDb db = new SqlDb();
        db.Connect();
        try
        {
            using (SqlCommand cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@cardCode", cardCode);
                cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                cmd.Parameters.AddWithValue("@transType", transType ?? "");
                return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }
        finally { db.Disconnect(); }
    }

    // DFS la_store_payments has no AmountFC column — omit it from all SELECT lists.
    private DataTable GetPagosParaFactura(string cardCode, DateTime fecha, string transType)
    {
        string sql =
            "WITH TxnTotals AS " + MakeTxnTotalsCte() + " " +
            "SELECT ID, TenderID, Amount, CurrencyID FROM (" +
            "  SELECT DISTINCT sp.ID, sp.TenderID, " +
            "         CASE WHEN sp.IsChange = 1 THEN -sp.Amount ELSE sp.Amount END AS Amount, " +
            "         sp.CurrencyID " +
            "  FROM dbo.la_store_payments sp " +
            "  INNER JOIN dbo.la_store_sales ss ON ss.TransactionID = sp.TransactionID AND ss.transnum = sp.transnum AND ss.TRANSTYPE = @transType " +
            "  INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE " +
            "  INNER JOIN TxnTotals tt ON tt.TransactionID = sp.TransactionID AND tt.transnum = sp.transnum " +
            "  WHERE ts.CARCODE = @cardCode AND sp.BusinessDayDate = @fecha " +
            "    AND sp.LineType IN ('TenderMerch', 'TenderChange') AND ss.txnmodifier = 0 " +
            "    AND tt.VentaTotal > tt.DevTotal AND tt.EnFactura = 0 " +
            "  UNION ALL " +
            "  SELECT DISTINCT sp.ID, sp.TenderID, " +
            "         (CASE WHEN tt.DevTotal > tt.VentaTotal THEN -1 ELSE 1 END) * (CASE WHEN sp.IsChange = 1 THEN -1 ELSE 1 END) * ABS(sp.Amount) AS Amount, " +
            "         sp.CurrencyID " +
            "  FROM dbo.la_store_payments sp " +
            "  INNER JOIN dbo.la_store_sales ss ON ss.TransactionID = sp.TransactionID AND ss.transnum = sp.transnum AND ss.TRANSTYPE = @transType " +
            "  INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE " +
            "  INNER JOIN TxnTotals tt ON tt.TransactionID = sp.TransactionID AND tt.transnum = sp.transnum " +
            "  WHERE ts.CARCODE = @cardCode AND sp.BusinessDayDate = @fecha " +
            "    AND sp.LineType IN ('TenderMerch', 'TenderChange') AND tt.EnFactura = 1 " +
            ") x";
        return RunPagosQuery(sql, cardCode, fecha, transType);
    }

    private DataTable GetPagosParaDevolucion(string cardCode, DateTime fecha, string transType)
    {
        string sql =
            "WITH TxnTotals AS " + MakeTxnTotalsCte() + " " +
            "SELECT ID, TenderID, Amount, CurrencyID FROM (" +
            "  SELECT DISTINCT sp.ID, sp.TenderID, " +
            "         CASE WHEN sp.IsChange = 1 THEN -ABS(sp.Amount) ELSE ABS(sp.Amount) END AS Amount, " +
            "         sp.CurrencyID " +
            "  FROM dbo.la_store_payments sp " +
            "  INNER JOIN dbo.la_store_sales ss ON ss.TransactionID = sp.TransactionID AND ss.transnum = sp.transnum AND ss.TRANSTYPE = @transType " +
            "  INNER JOIN dbo.ADR_TIENDA_SERIE ts ON ts.NUMSERIE = ss.NUMSERIE AND ts.NUMSTORE = CAST(ss.storenum AS INT) AND ts.DUTYTYPE = ss.TRANSTYPE " +
            "  INNER JOIN TxnTotals tt ON tt.TransactionID = sp.TransactionID AND tt.transnum = sp.transnum " +
            "  WHERE ts.CARCODE = @cardCode AND sp.BusinessDayDate = @fecha " +
            "    AND sp.LineType IN ('TenderMerch', 'TenderChange') AND ss.txnmodifier = 1 " +
            "    AND tt.DevTotal >= tt.VentaTotal AND tt.EnFactura = 0 " +
            ") x";
        return RunPagosQuery(sql, cardCode, fecha, transType);
    }

    private DataTable RunPagosQuery(string sql, string cardCode, DateTime fecha, string transType)
    {
        DataTable dt = new DataTable();
        SqlDb db = new SqlDb();
        db.Connect();
        try
        {
            using (SqlCommand cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@cardCode", cardCode);
                cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                cmd.Parameters.AddWithValue("@transType", transType ?? "");
                db.adapter.SelectCommand = cmd;
                db.adapter.Fill(dt);
            }
        }
        finally { db.Disconnect(); }
        return dt;
    }

    private DataTable GetTenderMappings()
    {
        DataTable dt = new DataTable();
        SqlDb db = new SqlDb();
        db.Connect();
        try
        {
            using (SqlCommand cmd = new SqlCommand(
                "SELECT TenderID, BPLId, SapAccount, PaymentType, Description FROM dbo.ADR_TENDER_SAP", db.Conn))
            {
                db.adapter.SelectCommand = cmd;
                db.adapter.Fill(dt);
            }
        }
        finally { db.Disconnect(); }
        return dt;
    }

    private Dictionary<int, string> _creditCardAccountsCache;

    private Dictionary<int, string> GetCreditCardAccounts(SapServiceLayer sl)
    {
        if (_creditCardAccountsCache != null) return _creditCardAccountsCache;

        Dictionary<int, string> result = new Dictionary<int, string>();
        try
        {
            string json = sl.GetCreditCards();
            JObject obj = JObject.Parse(json);
            JArray value = obj["value"] as JArray;
            if (value != null)
            {
                foreach (JToken t in value)
                {
                    JObject cc = (JObject)t;
                    if (cc["GLAccount"] == null) continue;
                    string glAccount = cc["GLAccount"].ToString();
                    if (string.IsNullOrWhiteSpace(glAccount)) continue;
                    int code = Convert.ToInt32(cc["CreditCardCode"]);
                    result[code] = glAccount;
                }
            }
        }
        catch { }

        _creditCardAccountsCache = result;
        return result;
    }

    // ── Tender -> SAP account mapping (port of AplicarFormasDePago) ─────────────
    private JObject BuildPaymentRequest(SapServiceLayer sl, string cardCode, DateTime fecha, int bplId,
        DataTable pagos, DataTable tenderMappings, bool esDevolucion, out string error,
        out decimal efectivoContrario, out string cuentaEfectivo)
    {
        error = null;
        efectivoContrario = 0m;
        cuentaEfectivo = null;

        if (pagos.Rows.Count == 0) { error = "no payments found for this date."; return null; }
        if (tenderMappings.Rows.Count == 0) { error = "ADR_TENDER_SAP is empty."; return null; }

        decimal totalPagado = 0m;
        foreach (DataRow r in pagos.Rows) totalPagado += Convert.ToDecimal(r["Amount"]);
        if (totalPagado <= 0) { error = "total payments amount to zero or less."; return null; }

        JObject request = new JObject();
        request["CardCode"] = cardCode;
        request["DocDate"] = fecha.ToString("yyyy-MM-dd");
        if (bplId > 0) request["BPLID"] = bplId;
        if (esDevolucion) request["DocType"] = "rCustomer";

        Dictionary<string, decimal> byTender = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (DataRow r in pagos.Rows)
        {
            string tid = r["TenderID"].ToString();
            decimal amt = Convert.ToDecimal(r["Amount"]);
            if (byTender.ContainsKey(tid)) byTender[tid] += amt;
            else byTender[tid] = amt;
        }

        decimal? cashSum = null;
        string cashAccount = null;
        Dictionary<int, JObject> creditCardsByCode = new Dictionary<int, JObject>();
        List<JObject> checks = new List<JObject>();
        List<string> problemas = new List<string>();
        Dictionary<int, string> creditCardAccounts = null;

        foreach (KeyValuePair<string, decimal> kv in byTender)
        {
            string tenderId = kv.Key;
            decimal suma = kv.Value;

            DataRow[] matches = tenderMappings.Select("TenderID = '" + tenderId.Replace("'", "''") + "'");
            if (matches.Length == 0) { problemas.Add("'" + tenderId + "' not in ADR_TENDER_SAP"); continue; }

            DataRow map = null;
            foreach (DataRow m in matches)
                if (m["BPLId"] != DBNull.Value && Convert.ToInt32(m["BPLId"]) == bplId) { map = m; break; }
            if (map == null)
                foreach (DataRow m in matches)
                    if (m["BPLId"] == DBNull.Value) { map = m; break; }
            if (map == null) { problemas.Add("'" + tenderId + "' has no mapping for BPL=" + bplId); continue; }

            string paymentType = map["PaymentType"].ToString().ToUpperInvariant();
            string sapAccount = map["SapAccount"].ToString();

            if (paymentType == "CASH")
            {
                if (string.IsNullOrWhiteSpace(sapAccount)) { problemas.Add("'" + tenderId + "' (CASH) has no SapAccount"); continue; }
                cashAccount = sapAccount;
                cashSum = (cashSum ?? 0) + suma;
            }
            else if (paymentType == "CREDITCARD")
            {
                int ccCode;
                if (!int.TryParse(sapAccount, out ccCode)) { problemas.Add("'" + tenderId + "' (CREDITCARD) SapAccount not numeric"); continue; }

                string creditAcct = null;
                if (esDevolucion)
                {
                    if (creditCardAccounts == null) creditCardAccounts = GetCreditCardAccounts(sl);
                    if (!creditCardAccounts.TryGetValue(ccCode, out creditAcct))
                    { problemas.Add("'" + tenderId + "' (CREDITCARD " + ccCode + ") no GL account for refund"); continue; }
                }

                if (creditCardsByCode.ContainsKey(ccCode))
                {
                    creditCardsByCode[ccCode]["CreditSum"] = Convert.ToDecimal(creditCardsByCode[ccCode]["CreditSum"]) + suma;
                }
                else
                {
                    JObject cc = new JObject();
                    cc["CreditCard"] = ccCode;
                    cc["CreditSum"] = suma;
                    cc["VoucherNum"] = "POS";
                    if (esDevolucion) { cc["CreditAcct"] = creditAcct; cc["CreditCardNumber"] = "0000"; cc["FirstPaymentDue"] = fecha.ToString("yyyy-MM-dd"); }
                    else cc["CardValidUntil"] = "2049-12-31";
                    creditCardsByCode[ccCode] = cc;
                }
            }
            else if (paymentType == "CHECK" || paymentType == "TRANSFER")
            {
                if (string.IsNullOrWhiteSpace(sapAccount)) { problemas.Add("'" + tenderId + "' (" + paymentType + ") has no SapAccount"); continue; }
                JObject chk = new JObject();
                chk["CheckAccount"] = sapAccount;
                chk["CheckSum"] = suma;
                checks.Add(chk);
            }
            else
            {
                problemas.Add("'" + tenderId + "' PaymentType '" + paymentType + "' unknown");
            }
        }

        if (problemas.Count > 0) { error = "payment mapping errors: " + string.Join("; ", problemas); return null; }

        // Net cash going the other way (e.g. TxnID 24671: return refunded 123.00 to DFS Circle Exchange
        // and the customer gave back the 45.08 cash change of the original sale). SAP does not take a
        // negative CashSum, so the payment carries only the other tenders (the excess stays on account)
        // and CrearPago adds an opposite cash payment for that amount and reconciles both.
        // Tested in TEST_PRO_DFSC 2026-11-06 (ORIN 56, OVPM 200, ORCT 709, recon 1042).
        if (cashSum.HasValue && cashSum.Value < 0)
        {
            efectivoContrario = -cashSum.Value;
            cuentaEfectivo = cashAccount;
            cashSum = null;
        }

        List<string> negativos = new List<string>();
        List<int> ceros = new List<int>();
        foreach (KeyValuePair<int, JObject> kv in creditCardsByCode)
        {
            decimal s = Convert.ToDecimal(kv.Value["CreditSum"]);
            if (s < 0) negativos.Add("card " + kv.Key + " (" + s.ToString("N2") + ")");
            else if (s == 0) ceros.Add(kv.Key);
        }
        foreach (int k in ceros) creditCardsByCode.Remove(k);
        checks.RemoveAll(ch => Convert.ToDecimal(ch["CheckSum"]) == 0);
        foreach (JObject ch in checks)
            if (Convert.ToDecimal(ch["CheckSum"]) < 0) negativos.Add("account " + ch["CheckAccount"]);
        if (negativos.Count > 0) { error = "negative payment amounts that SAP will reject: " + string.Join(", ", negativos); return null; }
        if (efectivoContrario > 0 && creditCardsByCode.Count == 0 && checks.Count == 0)
        {
            error = "cash goes the opposite way (" + efectivoContrario.ToString("N2") + ") and there is no other tender to carry the "
                + (esDevolucion ? "refund" : "payment") + ".";
            return null;
        }

        if (cashSum.HasValue) { request["CashAccount"] = cashAccount; request["CashSum"] = cashSum.Value; }
        if (creditCardsByCode.Count > 0)
        {
            JArray arr = new JArray();
            foreach (JObject v in creditCardsByCode.Values) arr.Add(v);
            request["PaymentCreditCards"] = arr;
        }
        if (checks.Count > 0 && esDevolucion)
        {
            // In a refund (VendorPayments) a check is one issued by the company and needs a house
            // bank: SAP answers "No matching records found (ODBC -2028)". Refunds go as a bank
            // transfer instead, which takes a single account (tested in TEST_PRO_DFSC 2026-11-01).
            Dictionary<string, decimal> byAccount = new Dictionary<string, decimal>();
            foreach (JObject ch in checks)
            {
                string acct = ch["CheckAccount"].ToString();
                decimal s = Convert.ToDecimal(ch["CheckSum"]);
                byAccount[acct] = byAccount.ContainsKey(acct) ? byAccount[acct] + s : s;
            }
            if (byAccount.Count > 1)
            {
                error = "refund with transfer tenders to more than one account (" + string.Join(", ", new List<string>(byAccount.Keys).ToArray())
                    + ") - SAP takes a single transfer account per refund.";
                return null;
            }
            foreach (KeyValuePair<string, decimal> kv in byAccount)
            {
                request["TransferAccount"] = kv.Key;
                request["TransferSum"] = kv.Value;
                request["TransferDate"] = fecha.ToString("yyyy-MM-dd");
                request["TransferReference"] = "POS";
            }
        }
        else if (checks.Count > 0)
        {
            JArray arr = new JArray();
            foreach (JObject ch in checks) arr.Add(ch);
            request["PaymentChecks"] = arr;
        }

        return request;
    }

    // ── la_daily_invoices / la_daily_returns tracking ────────────────────────────
    private void MarkInvoiceError(string cardCode, DateTime fecha, string transType, string errMsg)
    {
        string sql =
            "MERGE dbo.la_daily_invoices AS target " +
            "USING (SELECT @cardCode AS CardCode, @fecha AS FechaDoc, @transType AS TransType) AS source " +
            "  ON target.CardCode = source.CardCode AND target.FechaDoc = source.FechaDoc AND target.TransType = source.TransType " +
            "WHEN MATCHED THEN UPDATE SET InvoiceDocNum = -1, error_message = @error, date_updated = GETDATE() " +
            "WHEN NOT MATCHED THEN INSERT (CompanyId, CardCode, BPLId, FechaDoc, TransType, InvoiceDocNum, date_created, error_message) " +
            "  VALUES (0, @cardCode, 0, @fecha, @transType, -1, GETDATE(), @error);";
        ExecuteNonQuery(sql, cardCode, fecha, transType, "@error", errMsg);
    }

    private void UpsertInvoice(string cardCode, int bplId, DateTime fecha, string transType, int docNum, int docEntry)
    {
        string sql =
            "MERGE dbo.la_daily_invoices AS target " +
            "USING (SELECT @cardCode AS CardCode, @fecha AS FechaDoc, @transType AS TransType) AS source " +
            "  ON target.CardCode = source.CardCode AND target.FechaDoc = source.FechaDoc AND target.TransType = source.TransType " +
            "WHEN MATCHED THEN UPDATE SET InvoiceDocNum = @docNum, InvoiceDocEntry = @docEntry, error_message = NULL, date_updated = GETDATE() " +
            "WHEN NOT MATCHED THEN INSERT (CompanyId, CardCode, BPLId, FechaDoc, TransType, InvoiceDocNum, InvoiceDocEntry, date_created, error_message) " +
            "  VALUES (0, @cardCode, @bplId, @fecha, @transType, @docNum, @docEntry, GETDATE(), NULL);";

        SqlDb db = new SqlDb();
        db.Connect();
        try
        {
            using (SqlCommand cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@cardCode", cardCode);
                cmd.Parameters.AddWithValue("@bplId", bplId);
                cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                cmd.Parameters.AddWithValue("@transType", transType ?? "");
                cmd.Parameters.AddWithValue("@docNum", docNum);
                cmd.Parameters.AddWithValue("@docEntry", docEntry);
                cmd.ExecuteNonQuery();
            }
        }
        finally { db.Disconnect(); }
    }

    private void UpsertReturn(string cardCode, int bplId, DateTime fecha, string transType, int docNum, int docEntry)
    {
        string sql =
            "MERGE dbo.la_daily_returns AS target " +
            "USING (SELECT @cardCode AS CardCode, @fecha AS FechaDoc, @transType AS TransType) AS source " +
            "  ON target.CardCode = source.CardCode AND target.FechaDoc = source.FechaDoc AND target.TransType = source.TransType " +
            "WHEN MATCHED THEN UPDATE SET ReturnDocNum = @docNum, ReturnDocEntry = @docEntry, date_updated = GETDATE() " +
            "WHEN NOT MATCHED THEN INSERT (CompanyId, CardCode, BPLId, FechaDoc, TransType, ReturnDocNum, ReturnDocEntry, date_created) " +
            "  VALUES (0, @cardCode, @bplId, @fecha, @transType, @docNum, @docEntry, GETDATE());";

        SqlDb db = new SqlDb();
        db.Connect();
        try
        {
            using (SqlCommand cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@cardCode", cardCode);
                cmd.Parameters.AddWithValue("@bplId", bplId);
                cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                cmd.Parameters.AddWithValue("@transType", transType ?? "");
                cmd.Parameters.AddWithValue("@docNum", docNum);
                cmd.Parameters.AddWithValue("@docEntry", docEntry);
                cmd.ExecuteNonQuery();
            }
        }
        finally { db.Disconnect(); }
    }

    private void SetPayment(string cardCode, DateTime fecha, string transType, int paymentDocNum)
    {
        ExecuteNonQuery(
            "UPDATE dbo.la_daily_invoices SET PaymentDocNum = @val, date_updated = GETDATE() WHERE CardCode = @cardCode AND FechaDoc = @fecha AND TransType = @transType",
            cardCode, fecha, transType, "@val", paymentDocNum.ToString());
    }

    private void SetRefund(string cardCode, DateTime fecha, string transType, int refundDocNum)
    {
        ExecuteNonQuery(
            "UPDATE dbo.la_daily_returns SET RefundDocNum = @val, date_updated = GETDATE() WHERE CardCode = @cardCode AND FechaDoc = @fecha AND TransType = @transType",
            cardCode, fecha, transType, "@val", refundDocNum.ToString());
    }

    private void SetRecon(string cardCode, DateTime fecha, string transType, int reconNum)
    {
        ExecuteNonQuery(
            "UPDATE dbo.la_daily_returns SET ReconNum = @val, date_updated = GETDATE() WHERE CardCode = @cardCode AND FechaDoc = @fecha AND TransType = @transType",
            cardCode, fecha, transType, "@val", reconNum.ToString());
    }

    // ── Mixed-transaction reconciliation (Fase G equivalent) ────────────────────
    private string CompensarMixtas(SapServiceLayer sl, string cardCode, DateTime fecha, string transType)
    {
        string sql =
            "SELECT r.TransType, i.InvoiceDocEntry, r.ReturnDocEntry " +
            "FROM dbo.la_daily_returns r " +
            "INNER JOIN dbo.la_daily_invoices i ON i.CardCode = r.CardCode AND i.FechaDoc = r.FechaDoc AND i.TransType = r.TransType " +
            "WHERE r.CardCode = @cardCode AND r.FechaDoc = @fecha AND r.TransType = @transType " +
            "  AND r.ReturnDocNum > 0 AND r.ReturnDocEntry IS NOT NULL AND r.RefundDocNum IS NOT NULL AND r.ReconNum IS NULL " +
            "  AND i.InvoiceDocNum > 0 AND i.InvoiceDocEntry IS NOT NULL AND i.PaymentDocNum IS NOT NULL";

        DataTable dt = RunPagosQuery(sql, cardCode, fecha, transType);
        if (dt.Rows.Count == 0) return null;

        DataRow row = dt.Rows[0];
        int invEntry = Convert.ToInt32(row["InvoiceDocEntry"]);
        int rinEntry = Convert.ToInt32(row["ReturnDocEntry"]);

        decimal compensacion = GetCompensacionMixtas(cardCode, fecha, transType);
        if (compensacion <= 0)
        {
            SetRecon(cardCode, fecha, transType, 0);
            return null;
        }

        try
        {
            JObject inv = JObject.Parse(sl.GetDocBalance("Invoices", invEntry));
            JObject rin = JObject.Parse(sl.GetDocBalance("CreditNotes", rinEntry));
            decimal invAbierto = Convert.ToDecimal(inv["DocTotal"]) - Convert.ToDecimal(inv["PaidToDate"]);
            decimal rinAbierto = Convert.ToDecimal(rin["DocTotal"]) - Convert.ToDecimal(rin["PaidToDate"]);

            if (invAbierto <= 0.01m && rinAbierto <= 0.01m) { SetRecon(cardCode, fecha, transType, 0); return null; }

            if (Math.Abs(invAbierto - compensacion) > 0.01m || Math.Abs(rinAbierto - compensacion) > 0.01m)
                return "Offset not reconciled: offset " + compensacion.ToString("N2") + " vs SAP balance (inv " + invAbierto.ToString("N2") + ", rin " + rinAbierto.ToString("N2") + ")";

            JArray filas = new JArray();
            foreach (int transId in new[] { Convert.ToInt32(inv["TransNum"]), Convert.ToInt32(rin["TransNum"]) })
            {
                JObject je = JObject.Parse(sl.GetJournalEntry(transId));
                JObject linea = null;
                foreach (JToken l in (JArray)je["JournalEntryLines"])
                    if (string.Equals(l["ShortName"].ToString(), cardCode, StringComparison.OrdinalIgnoreCase)) { linea = (JObject)l; break; }
                if (linea == null)
                    return "Offset not reconciled: journal entry " + transId + " has no line for " + cardCode;

                decimal debe = Convert.ToDecimal(linea["Debit"]);
                decimal haber = Convert.ToDecimal(linea["Credit"]);
                bool esDebe = debe != 0;
                decimal signo = (esDebe ? debe : haber) < 0 ? -1 : 1;

                JObject fila = new JObject();
                fila["Selected"] = "tYES";
                fila["ShortName"] = cardCode;
                fila["TransId"] = transId;
                fila["TransRowId"] = Convert.ToInt32(linea["Line_ID"]);
                fila["CreditOrDebit"] = esDebe ? "codDebit" : "codCredit";
                fila["ReconcileAmount"] = signo * compensacion;
                filas.Add(fila);
            }

            JObject recon = new JObject();
            recon["ReconDate"] = fecha.ToString("yyyy-MM-dd");
            recon["CardOrAccount"] = "coaCard";
            recon["InternalReconciliationOpenTransRows"] = filas;

            JObject resp = JObject.Parse(sl.CreateInternalReconciliation(recon.ToString(Newtonsoft.Json.Formatting.None)));
            int reconNum = Convert.ToInt32(resp["ReconNum"]);
            SetRecon(cardCode, fecha, transType, reconNum);
            return "offset " + compensacion.ToString("N2") + " reconciled (#" + reconNum + ")";
        }
        catch (WebException wex)
        {
            return "Offset not reconciled: " + SapServiceLayer.GetSlErrorMessage(wex);
        }
    }

    private void ExecuteNonQuery(string sql, string cardCode, DateTime fecha, string transType, string extraParamName, string extraParamValue)
    {
        SqlDb db = new SqlDb();
        db.Connect();
        try
        {
            using (SqlCommand cmd = new SqlCommand(sql, db.Conn))
            {
                cmd.Parameters.AddWithValue("@cardCode", cardCode);
                cmd.Parameters.AddWithValue("@fecha", fecha.Date);
                cmd.Parameters.AddWithValue("@transType", transType ?? "");
                // Detect the extra param type: if it looks like an integer, use int.
                int intVal;
                if (int.TryParse(extraParamValue, out intVal))
                    cmd.Parameters.AddWithValue(extraParamName, intVal);
                else
                    cmd.Parameters.AddWithValue(extraParamName, extraParamValue ?? "");
                cmd.ExecuteNonQuery();
            }
        }
        finally { db.Disconnect(); }
    }
}
