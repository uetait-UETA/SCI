using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Web.UI;

public partial class DisTransferDetailsPrint : System.Web.UI.Page
{
    protected int totalQty;
    protected string Status;
    protected string DocDate;
    protected string FromLoc;
    protected string ToLoc;
    protected string DocEntry;
    protected string colspan;
    protected string UserApp;
    string order_multiple;
    protected string sap_db;
    protected bool isMassivePrinting;
    protected List<DocumentsPrint> docEntries;
    protected int copies;

    protected void Page_Load(object sender, EventArgs e)
    {
        totalQty = 0;
        DocEntry = "";
        string LvUser = (string)this.Session["UserId"];

        if (Request.QueryString["DocEntry"] == null)
        {
            Response.Write("ERROR: No document entry number specified in querystring.<br>");
            Response.End();
        }
        else
        {
            sap_db = (string)this.Session["CompanyId"];

            if (LvUser == "")
            {
                Response.Write("<script type=\"text/javascript\">alert('" + "Please log in to the system." + "');</script>");
                Response.End();
                //Response.Redirect("Login1.aspx");
            }

            DocEntry = Request.QueryString["DocEntry"].ToString();

            docEntries = (List<DocumentsPrint>)Session["docEntries"];
            int c1 = 0;

            if (DocEntry == "MASIVO")
            {
                isMassivePrinting = true;
                
                divContent.InnerHtml = "";

                c1 = 0;
                foreach (DocumentsPrint documentsPrint in docEntries)
                {
                    DocEntry = documentsPrint.DocNumber;
                    copies = documentsPrint.Copies;

                    for (int i = 0; i < copies; i++)
                    {
                        divContent.InnerHtml += (c1 == 0 ? "" : "<H1 class=\"SaltoDePagina\"></H1>") + disGetTransferDetails(DocEntry, sap_db);
                    }

                    c1++;
                }

                Page.Title = "";
            }
            else
            {
                DocEntry = docEntries[0].DocNumber;
                copies = docEntries[0].Copies;
                isMassivePrinting = false;
                c1 = 0;
                for (int i = 0; i < copies; i++)
                {
                    divContent.InnerHtml += (c1 == 0 ? "" : "<H1 class=\"SaltoDePagina\"></H1>") + disGetTransferDetails(DocEntry, sap_db);
                    c1++;
                }

                //divContent.InnerHtml = disGetTransferDetails(DocEntry, sap_db);
                //Page.Title = "Dispatch/Receive Document #" + DocEntry;
                Page.Title = "";
            }
        }
    }

    private string disGetTransferDetails(string DocEntry, string sap_db)
    {
        string str = "";
        Transfer tsf = new Transfer();

        DataTable dt = tsf.GetDisTransferDetails(DocEntry, sap_db);

        if (dt != null && dt.Rows.Count > 0)
        {
            DocEntry = dt.Rows[0]["DocEntry"].ToString().Trim();
            Status = dt.Rows[0]["Status"].ToString().Trim();
            DocDate = String.Format("{0:MM/dd/yyyy hh:mm}", dt.Rows[0]["DocDate"]);
            FromLoc = dt.Rows[0]["FromLocName"].ToString().Trim();
            ToLoc = dt.Rows[0]["ToLocName"].ToString().Trim();
            order_multiple = dt.Rows[0]["order_multiple"].ToString().Trim();

            // if no PO exists show the plan approval link
            if (DocEntry == "")
            {
                DocEntry = "ERROR: Document not found.";
                return DocEntry;
            }
        }
        else
        {
            return "ERROR: Could not find document #" + DocEntry;
        }

        str = PageContent(dt);
        return str;

    }

    private string PageContent(DataTable dt)
    {
        int linesPerPage = 0;

        if (!int.TryParse(ConfigurationManager.AppSettings["TransfersDispatchPrintLinesPerPage"].ToString(), out linesPerPage))
        {
            //If the conversion fails, it will use the default of 40 lines per page:
            linesPerPage = 40;
        }

        int PageNum = 1;
        int PageCount = 0;
        colspan = "8";
        string str = "";

        // get the formatted page header to display on each page of the document
        string hdr = HeaderInfo();

        // set page count for use in footer
        PageCount = dt.Rows.Count / linesPerPage;
        if (dt.Rows.Count % linesPerPage != 0)
            PageCount += 1;

        int i = 0;
        if (dt.Rows.Count > 0)
        {
            for (i = 0; i < dt.Rows.Count; i++)
            {
                if (i % linesPerPage == 0)
                {
                    str += hdr + LineItemHeader(ref colspan);
                }

                str += LineItem(dt.Rows[i], i % linesPerPage);

                if ((i + 1) % linesPerPage == 0)
                {
                    str += "</table>"
                        + "<div class=\"pg-footer\">Page " + PageNum + " of " + PageCount + "</div>"
                        + "</div></div>"; // close items-wrap + doc-page
                    if (i != dt.Rows.Count - 1)
                        str += "<H1 class=\"SaltoDePagina\"></H1>";
                    PageNum += 1;
                }
            }
        }
        else
        {
            str += "<tr><td colspan=\"" + colspan + "\" style=\"text-align:center; padding:14px; font-size:9pt;\"><b>No Records Found</b></td></tr>";
        }

        if (i % linesPerPage != 0)
        {
            str += "</table>"
                + "<div class=\"pg-footer\">Page " + PageNum + " of " + PageCount + "</div>"
                + "</div></div>"; // close items-wrap + doc-page
        }
        //str += HtmlFooter();
        return str;
    }

    // return a single line item table row
    protected string LineItem(DataRow row, int rowIndex = 0)
    {
        string str = "<tr>"
            + "<td style=\"text-align:center;\">" + row["LineNumber"] + "</td>"
            + "<td style=\"text-align:center;\">" + row["ItemCode"] + "</td>"
            + "<td style=\"text-align:center;\">" + row["BarCode"] + "</td>"
            + "<td style=\"text-align:left;\">" + row["U_brand"] + "</td>"
            + "<td style=\"text-align:left;\">" + row["Description"] + "</td>"
            + "<td style=\"text-align:right;\">" + String.Format("{0:C}", row["Price"]) + "</td>"
            + "<td style=\"text-align:center;\">" + String.Format("{0:#,###}", row["Qty"]) + "</td>";
        if (order_multiple == "C")
            str += "<td style=\"text-align:center;\">" + String.Format("{0:#,###}", row["Cases"]) + "</td>";
        str += "<td style=\"text-align:center;\">&nbsp;</td></tr>";
        return str;
    }

    // return a line item header row
    protected string LineItemHeader(ref string colspan)
    {
        string str = "<table class=\"doc-table\">"
            + "<tr>"
            + "<th style=\"width:42px;\">Line</th>"
            + "<th style=\"width:105px;\">Item Code</th>"
            + "<th style=\"width:105px;\">Barcode</th>"
            + "<th style=\"width:90px;\">Brand</th>"
            + "<th>Description</th>"
            + "<th style=\"width:72px;\">Price</th>"
            + "<th style=\"width:65px;\">Ordered</th>";
        if (order_multiple == "C")
        {
            colspan = (int.Parse(colspan) + 1).ToString();
            str += "<th style=\"width:55px;\">Cases</th>";
        }
        str += "<th style=\"width:68px;\">Received</th>"
            + "</tr>";
        return str;
    }

    private string HeaderInfo()
    {
        UserApp = (string)this.Session["UserId"];
        string imgSrc = AnyPourpuse.GetBarCodeImage(DocEntry).ImageUrl;

        string str =
            "<div class=\"doc-page\">"

            // ── Title bar ──
            + "<div class=\"doc-title-bar\">"
            + "<span class=\"doc-title-l\">Dispatch / Receive</span>"
            + "<span class=\"doc-title-r\">Document&nbsp;#&nbsp;" + DocEntry + "</span>"
            + "</div>"

            // ── Info row ──
            + "<div class=\"doc-info-row\">"
            + "<div class=\"doc-info-left\">"
            + "<div class=\"field-group\"><div class=\"field-label\">From Location</div><div class=\"field-value\">" + FromLoc + "</div></div>"
            + "<div class=\"field-group\"><div class=\"field-label\">To Location</div><div class=\"field-value\">" + ToLoc + "</div></div>"
            + "<div class=\"field-group\"><div class=\"field-label\">Date Created</div><div class=\"field-value\">" + DocDate + "</div></div>"
            + "<div class=\"field-group\"><div class=\"field-label\">Status</div><div class=\"field-value\">" + Status + "</div></div>"
            + "<div class=\"field-group\"><div class=\"field-label\">User</div><div class=\"field-value\">" + UserApp + "</div></div>"
            + "</div>"
            + "<div class=\"doc-info-right\">"
            + "<img src=\"" + imgSrc + "\" alt=\"Barcode " + DocEntry + "\" style=\"max-width:155px; max-height:75px;\" />"
            + "</div>"
            + "</div>"

            // ── Signatures ──
            + "<div class=\"sig-section\">"
            + "<div class=\"sig-title\">Signatures</div>"
            + "<div>"
            + "<span class=\"sig-entry\">Warehouse: <span class=\"sig-line\"></span></span>"
            + "<span class=\"sig-entry\">Store: <span class=\"sig-line\"></span></span>"
            + "<span class=\"sig-entry\">Inv. Control: <span class=\"sig-line\"></span></span>"
            + "</div>"
            + "<div style=\"margin-top:7px;\" class=\"notes-line\">Notes: <span class=\"sig-line\"></span></div>"
            + "</div>"

            // ── Items section (table appended after) ──
            + "<div class=\"items-wrap\">";

        return str;
    }
}