using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

/// <summary>
/// Thin wrapper around SAP B1 Services Layer HTTP calls.
/// One instance per operation: construct → Login → action → Logout.
/// </summary>
public class SapServiceLayer
{
    private readonly string _baseUrl;
    private readonly string _userName;
    private readonly string _password;
    private string          _sessionId; // B1SESSION captured from Login response

    public SapServiceLayer()
    {
        _baseUrl  = ConfigurationManager.AppSettings["SL_BaseUrl"];
        _userName = ConfigurationManager.AppSettings["SL_UserName"]
                 ?? ConfigurationManager.AppSettings["serverUserName"];
        _password = ConfigurationManager.AppSettings["SL_Password"]
                 ?? ConfigurationManager.AppSettings["serverPwd"];

        // Allow self-signed internal certificates
        ServicePointManager.ServerCertificateValidationCallback = (s, c, ch, e) => true;
        // Enable TLS 1.0 / 1.1 / 1.2 for .NET 4.0 compatibility. No SSL3 (48): SAP SL uses TLS 1.2
        // and .NET 8 (FacturacionSAPConsole, which compiles this file) throws on SSL3.
        ServicePointManager.SecurityProtocol =
            (SecurityProtocolType)(192 | 768 | 3072);
        // .NET agrega Expect:100-continue por defecto en POST; SAP SL no lo maneja bien
        ServicePointManager.Expect100Continue = false;
    }

    /// <summary>Throws on failure — caller must catch WebException or Exception.</summary>
    public void Login(string companyDb)
    {
        var payload = new JObject(
            new JProperty("CompanyDB", companyDb),
            new JProperty("UserName",  _userName),
            new JProperty("Password",  _password)
        );
        try
        {
            Post("/Login", payload.ToString(Newtonsoft.Json.Formatting.None));
        }
        catch (WebException wex)
        {
            string detail = GetSlErrorMessage(wex);
            throw new Exception(string.Format(
                "SL Login fallido [CompanyDB='{0}' UserName='{1}' URL='{2}']: {3}",
                companyDb, _userName, _baseUrl, detail));
        }
    }

    public void Logout()
    {
        try { Post("/Logout", "{}"); }
        catch { }
    }

    public string CreateGoodsReceiptPO(string jsonPayload)
    {
        string endpoint = System.Configuration.ConfigurationManager.AppSettings["SL_GrpoEndpoint"]
                          ?? "GoodsReceiptsPO";
        return Post("/" + endpoint, jsonPayload);
    }

    public string CancelGoodsReceiptPO(int docEntry)
    {
        string endpoint = System.Configuration.ConfigurationManager.AppSettings["SL_GrpoEndpoint"]
                          ?? "GoodsReceiptsPO";
        return Post("/" + endpoint + "(" + docEntry + ")/Cancel", "{}");
    }

    public string CreateInventoryTransferRequest(string jsonPayload)
    {
        return Post("/InventoryTransferRequests", jsonPayload);
    }

    public string CloseInventoryTransferRequest(int docEntry)
    {
        return Post("/InventoryTransferRequests(" + docEntry + ")/Close", "{}");
    }

    public string CreateInventoryTransfer(string jsonPayload)
    {
        return Post("/StockTransfers", jsonPayload);
    }

    public string CreateSalesOrder(string jsonPayload)
    {
        return Post("/Orders", jsonPayload);
    }

    public string CreateDeliveryNote(string jsonPayload)
    {
        return Post("/DeliveryNotes", jsonPayload);
    }

    public string CreateARInvoice(string jsonPayload)
    {
        return Post("/Invoices", jsonPayload);
    }

    public string CreateInventoryGenEntry(string jsonPayload)
    {
        return Post("/InventoryGenEntries", jsonPayload);
    }

    public string CreateInventoryGenExit(string jsonPayload)
    {
        return Post("/InventoryGenExits", jsonPayload);
    }

    public string GetInventoryTransferRequest(int docEntry)
    {
        return Get("/InventoryTransferRequests(" + docEntry + ")?$select=DocEntry,U_GTK_CONFIRMATION,StockTransferLines");
    }

    // ── Methods used by InvoiceUploader (FacturacionSAP.aspx) ────────────────────

    public string GetDocumentForInvoice(string entity, int docEntry)
    {
        return Get("/" + entity + "(" + docEntry + ")?$select=DocEntry,DocumentLines,DocumentAdditionalExpenses");
    }

    public string CreateInvoice(string jsonPayload)
    {
        return Post("/Invoices", jsonPayload);
    }

    public string CreateCreditNote(string jsonPayload)
    {
        return Post("/CreditNotes", jsonPayload);
    }

    public string CreateIncomingPayment(string jsonPayload)
    {
        return Post("/IncomingPayments", jsonPayload);
    }

    public string CreateOutgoingPayment(string jsonPayload)
    {
        return Post("/VendorPayments", jsonPayload);
    }

    public string GetCreditCards()
    {
        return Get("/CreditCards?$select=CreditCardCode,GLAccount");
    }

    public string GetDocBalance(string entity, int docEntry)
    {
        return Get("/" + entity + "(" + docEntry + ")?$select=DocTotal,PaidToDate,TransNum");
    }

    public string GetJournalEntry(int transId)
    {
        return Get("/JournalEntries(" + transId + ")?$select=JournalEntryLines");
    }

    // Closes a delivery (DeliveryNotes) or return (Returns): status only, no journal entry and no
    // inventory movement (tested in TEST_PRO_DFSC 2026-11-07, ODLN 978).
    public string CloseDocument(string entity, int docEntry)
    {
        return Post("/" + entity + "(" + docEntry + ")/Close", "{}");
    }

    // Journal entry of a payment (the Payments entity does not expose it). outgoing = VendorPayments.
    // The first one is the payment itself (a cancellation adds a second one).
    public string GetPaymentJournalEntries(bool outgoing, int docEntry)
    {
        return Get("/JournalEntries?$filter=OriginalJournal%20eq%20'" + (outgoing ? "ttVendorPayment" : "ttReceipt")
            + "'%20and%20Original%20eq%20" + docEntry + "&$select=JdtNum&$orderby=JdtNum");
    }

    public string CreateInternalReconciliation(string jsonPayload)
    {
        return Post("/InternalReconciliations", jsonPayload);
    }

    /// <summary>
    /// Extracts the human-readable error from a SAP SL WebException response body.
    /// SAP SL error format: {"error":{"code":"...","message":{"value":"..."}}}
    /// </summary>
    public static string GetSlErrorMessage(WebException ex)
    {
        try
        {
            if (ex.Response == null) return ex.Message;
            using (var reader = new StreamReader(ex.Response.GetResponseStream()))
            {
                string body = reader.ReadToEnd();
                try
                {
                    var obj = JObject.Parse(body);
                    JToken errorToken = obj["error"];
                    if (errorToken != null)
                    {
                        JToken msgToken = errorToken["message"];
                        if (msgToken != null)
                        {
                            JToken valToken = msgToken["value"];
                            if (valToken != null)
                            {
                                string val = valToken.ToString();
                                if (!string.IsNullOrEmpty(val)) return val;
                            }
                        }
                    }
                }
                catch { }
                return body.Length > 400 ? body.Substring(0, 400) : body;
            }
        }
        catch
        {
            return ex.Message;
        }
    }

    // SAP takes ~2.7 min to create a C3001 daily invoice (~4,000 lines, tested 2026-11-01 in
    // TEST_PRO_DFSC); a shorter timeout gives up while SAP still creates the document.
    private const int RequestTimeoutMs = 15 * 60 * 1000;

    private string Get(string endpoint)
    {
        var req = (HttpWebRequest)WebRequest.Create(_baseUrl + endpoint);
        req.Method  = "GET";
        req.Accept  = "application/json";
        req.Timeout = RequestTimeoutMs;
        req.ReadWriteTimeout = RequestTimeoutMs;

        if (!string.IsNullOrEmpty(_sessionId))
            req.Headers["Cookie"] = "B1SESSION=" + _sessionId;

        using (var resp = (HttpWebResponse)req.GetResponse())
        using (var rdr  = new StreamReader(resp.GetResponseStream()))
            return rdr.ReadToEnd();
    }

    private string Post(string endpoint, string jsonPayload)
    {
        var req = (HttpWebRequest)WebRequest.Create(_baseUrl + endpoint);
        req.Method      = "POST";
        req.ContentType = "application/json";
        req.Accept      = "application/json";
        req.Timeout     = RequestTimeoutMs;
        req.ReadWriteTimeout = RequestTimeoutMs;

        // CookieContainer has a known .NET bug with IP-based URLs (domain matching fails).
        // Instead, capture B1SESSION from the Login Set-Cookie header and send it explicitly.
        if (!string.IsNullOrEmpty(_sessionId))
            req.Headers["Cookie"] = "B1SESSION=" + _sessionId;

        byte[] body = Encoding.UTF8.GetBytes(jsonPayload);
        req.ContentLength = body.Length;

        using (Stream s = req.GetRequestStream())
            s.Write(body, 0, body.Length);

        using (var resp = (HttpWebResponse)req.GetResponse())
        {
            // Capture B1SESSION from Set-Cookie (present only in Login response)
            string setCookie = resp.Headers["Set-Cookie"];
            if (!string.IsNullOrEmpty(setCookie))
            {
                var m = System.Text.RegularExpressions.Regex.Match(
                    setCookie, @"B1SESSION=([^;,\s]+)");
                if (m.Success)
                    _sessionId = m.Groups[1].Value;
            }

            using (var rdr = new StreamReader(resp.GetResponseStream()))
                return rdr.ReadToEnd();
        }
    }
}
