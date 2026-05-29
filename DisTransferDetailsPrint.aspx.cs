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
                if ((i) % linesPerPage == 0)
                {
                    str += hdr + LineItemHeader(ref colspan);
                }

                // add a line item to the html string
                str += LineItem(dt.Rows[i]);

                if ((i + 1) % linesPerPage == 0)
                {
                    // add footer, pagebreak, and header for next page
                    str += "<tr><td colspan=\"" + colspan + "\" align=\"center\" style=\"font-size:8pt;\">Page " + PageNum + " of " + PageCount + "</td></tr></table><br><br>";
                    if (i != dt.Rows.Count - 1)
                    {
                        //str += "<br class=\"page\" />\r\n";
                        str += "<H1 class=\"SaltoDePagina\"></H1>";
                    }
                        
                    PageNum += 1;
                }
            }
        }
        else
        {
            str += "<tr><td colspan=\"" + colspan + "\" align=\"center\"><font face=\"arial\"><b>No Records Found</b></font></td></tr>";
        }

        if ((i) % linesPerPage != 0)
        {
            str += "<tr><td colspan=\"" + colspan + "\" align=\"center\" style=\"font-size:8pt;\">Page " + PageNum + " of " + PageCount + "</font></td></tr></table><br><br>";
        }
        //str += HtmlFooter();
        return str;
    }

    // return a single line item table row 
    protected string LineItem(DataRow row)
    {
        string str = ""
        + "<tr class=\"printRow\">";
        str += "<td class=\"printData\" style=\"text-align:center;\">" + row["LineNumber"].ToString() + "</td>";
        str += "<td class=\"printData\" style=\"text-align:center;\">" + row["ItemCode"].ToString() + "</td>";
        str += "<td class=\"printData\" style=\"text-align:center;\">" + row["BarCode"].ToString() + "</td>";
        str += "<td class=\"printData\" style=\"text-align:left;\">" + row["U_brand"].ToString() + "</td>";
        str += "<td class=\"printData\" style=\"text-align:left;\">" + row["Description"].ToString() + "</td>";
        str += "<td class=\"printData\" style=\"text-align:center;\">" + String.Format("{0:C}", row["Price"]) + "</td>";
        str += "<td class=\"printData\" style=\"text-align:center;\">" + String.Format("{0:#,###}", row["Qty"]) + "</td>";
        if (order_multiple == "C")
        {
            str += "<td class=\"printData\" style=\"text-align:center;\">" + String.Format("{0:#,###}", row["Cases"]) + "</td>";
        }
        str += "<td class=\"printData\" style=\"text-align:center; \"></td></tr>";
        str += "<tr><td class=\"printDataBottom\" colspan=\"" + colspan + "\"></td></tr>";
        return str;
    }

    // return a line item header row 
    protected string LineItemHeader(ref string colspan)
    {
        string str = ""
        + "<table style=\"width:100%; font-family: Arial, Helvetica, sans-serif; padding-top:9px;\">"
        + "<tr class=\"printHeader\">";
        str += "<td style=\"text-align:center;\">Line</td>";
        str += "<td style=\"text-align:center;\">Code</td>";
        str += "<td style=\"text-align:center;\">Barcode</td>";
        str += "<td style=\"text-align:left;\">Brand</td>";
        str += "<td style=\"text-align:left;\">Description</td>";
        str += "<td style=\"text-align:center;\">Price</td>";
        str += "<td style=\"text-align:center;\">Ordered</td>";
        if (order_multiple == "C")
        {
            colspan = (int.Parse(colspan) + 1).ToString();
            str += "<td style=\"text-align:center;\">Cases</td>";
        }
        str += "<td style=\"text-align:center;\">Received</td>";
        str += "</tr>";
        return str;
    }

    private string HeaderInfo()
    {
        UserApp = (string)this.Session["UserId"];
        string imgSrc = AnyPourpuse.GetBarCodeImage(DocEntry).ImageUrl;
        string str =
             "<fieldset>"
            + "<legend class=\"tblHeaderTitle\">Dispatch/Receive Document #" + DocEntry + "</legend>"
            + "<table class=\"tblHeader\">"
                + "<tr>"
                    + "<td style=\"padding-left:10px; text-align:left;\">"
                        + "<table>"
                            + "<tr>"
                            + "    <td style=\"font-weight:bold;\">From Location:</td>"
                            + "    <td style=\"padding-left:12px;\">" + FromLoc + "</td>"
                            + "    <td class=\"barCodeContainer\" rowspan=\"5\">"
                            + "        <img id=\"img_" + DocEntry + "\" alt=\"IMG_" + DocEntry + "\" class=\"barCodeImg\" src=\"" + imgSrc + "\">"
                            + "    </td>"
                            + "</tr>"
                            + "<tr>"
                            + "    <td style=\"font-weight:bold;\">To Location:</td>"
                            + "    <td style=\"padding-left:12px;\">" + ToLoc + "</td>"
                            + "</tr>"
                            + "<tr>"
                             + "   <td style=\"font-weight:bold;\">Date Created:</td>"
                            + "    <td style=\"padding-left:12px;\">" + DocDate + "</td>"
                            + "</tr>"
                            + "<tr>"
                            + "    <td style=\"font-weight:bold;\">Status:</td>"
                            + "    <td style=\"padding-left:12px;\">" + Status + "</td>"
                            + "</tr>"
                            + "    <td style=\"font-weight:bold;\">User:</td>"
                            + "    <td style=\"padding-left:12px;\">" + UserApp + "</td>"
                            + "</tr>"
                        + "</table>"
                    + "</td>"
                + "</tr>"
                + "<tr>"
                    + "<td style=\"font-weight:bold; padding-top:12px;\">Signatures:</td>"
                + "</tr>"
                + "<tr>"
                    + "<td style=\"font-weight:bold;\">"
                        + "Warehouse:______________________&nbsp;&nbsp;Store:_______________________&nbsp;&nbsp;Inv Control:___________________________"
                    + "</td>"
                + "</tr>"
                + "<tr><td>&nbsp;</td></tr>"
                + "<tr>"
                    + "<td style=\"font-weight:bold;\">"
                        + "Notes:______________________________________________________________________________________________"
                    + "</td>"
                + "</tr>"
            + "</table>"
        + "</fieldset>";
        return str;
    }
}