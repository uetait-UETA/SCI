using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Collections; //Mod MINMAXUPDATE
using System.Data.SqlClient;
using System.Configuration;
using System.Xml;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

public partial class DisTransferDetails : System.Web.UI.Page
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
            //DocEntry = "14";  
            divContent.InnerHtml = disGetTransferDetails(DocEntry, sap_db);
            this.Page.Title = "Dispatch/Receive Document #" + DocEntry;
            
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
        int linesPerPage = 25;
        int PageNum = 1;
        int PageCount = 0;
        colspan = "7";
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
                    str += hdr + LineItemHeader();
                }

                // add a line item to the html string
                str += LineItem(dt.Rows[i]);

                if ((i + 1) % linesPerPage == 0)
                {
                    // add footer, pagebreak, and header for next page
                    str += @"<tr><td colspan=" + colspan + " align=center>Page " + PageNum + " of " + PageCount + "</td></tr></table><br><br>\n";
                    if (i != dt.Rows.Count - 1)
                        str += "<br class=\"page\" />\r\n";
                    PageNum += 1;
                }
            }
        }
        else
        {
            str += "<tr><td colspan=" + colspan + " align=center><font face=arial><b>No Records Found</b></font></td></tr>";
        }

        if ((i) % linesPerPage != 0)
        {
            str += @"<tr><td colspan=" + colspan + " align=center>Page " + PageNum + " of " + PageCount + "</font></td></tr></table><br><br>\n";
        }
        //str += HtmlFooter();
        return str;
    }

    // return a single line item table row 
    protected string LineItem(DataRow row)
    {
        string str = ""
        + "     <tr style=\"font-size:9pt; \">";
        str += "<td align=center style=\"width:40px; \">" + row["LineNumber"].ToString() + @"</td>";
        str += "<td align=center style=\"width:80px\">" + row["ItemCode"].ToString() + @"</td>";
        str += "<td align=center style=\"width:150px; font-size:11pt; text-align:left;\">" + row["BarCode"].ToString() + @"</td>";
        str += "<td align=center style=\"width:80px\">" + row["U_brand"].ToString() + @"</td>";
        str += "<td align=left style=\"width:320px\">" + row["Description"].ToString() + @"</td>";
        str += "<td align=center  style=\"width:40px; text-align:right;\">" + String.Format("{0:C}", row["Price"]) + @"</td>";
        str += "<td align=center style=\"width:40px; text-align:right;\">" + String.Format("{0:#,###}", row["Qty"]) + @"</td>";
        if (order_multiple == "C")
        { str += "<td align=center style=\"width:40px; text-align:right;\">" + String.Format("{0:#,###}", row["Cases"]) + @"</td>"; }
        str += "<td align=center style=\"width:40px\">&nbsp;</td>\r\n</tr>\r\n";
        str += "<tr><td colspan=" + colspan + " style=\"height:1px; border-bottom:solid 1px black; font-size:2pt;\">&nbsp;</td></tr>\r\n";
        return str;
    }

    // return a line item header row 
    protected string LineItemHeader()
    {
        string str = ""
        + "<table width=\"95%\">"
        + "     <tr style=\"font-size:9pt; font-weight:bold; color:white; text-decoration:underline; background-color:black\">";
        str += "<td align=center style=\"width:40px\">Line</td>";
        str += "<td align=center style=\"width:80px\">Code</td>";
        str += "<td align=center style=\"width:150px; font-size:11pt;\">Barcode</td>";
        str += "<td align=center style=\"width:80px\">Brand</td>";
        str += "<td align=left style=\"width:320px\">Description</td>";
        str += "<td align=right style=\"width:40px; text-align:right;\">Price</td>";
        str += "<td align=right style=\"width:80px; text-align:center;\">Ordered</td>";
        if (order_multiple == "C")
        { str += "<td align=right style=\"width:40px; text-align:center;\">Cases</td>"; }
        str += "<td align=right style=\"width:80px; text-align:center;\">Received</td>\r\n</tr>\r\n";
        return str;
    }

    private string HeaderInfo()
    {
        UserApp = (string)this.Session["UserId"];
        string str =
             "<fieldset style=\"text-align:center; width:590px;\">"
            + "<legend class=\"legendStandard\">Dispatch/Receive Document #" + DocEntry + "</legend>"
            + "<table style=\"width:95%\">"
                + "<tr><td style=\"padding-left:10px; text-align:left;\">"
                    + "<table width=\"300px\" style=\"width:300px; text-align:left;\">"
                        + "<tr>"
                        + "    <td width=\"120px\" style=\"font-weight:bold; text-align:left;\">From Location :&nbsp;</td>"
                        + "    <td width=\"180px;\" style=\"text-align:left;\">"
                        + FromLoc + "</td>"
                        + "</tr>"
                        + "<tr>"
                        + "    <td width=\"120px\" style=\"font-weight:bold; text-align:left;\">To Location :&nbsp;</td>"
                        + "    <td width=\"180px\" style=\"text-align:left;\">"
                        + ToLoc + "</td>"
                        + "</tr>"
                        + "<tr>"
                         + "   <td width=\"120px\" style=\"font-weight:bold; text-align:left;\">Date Created :&nbsp;</td>"
                        + "    <td width=\"180px\" style=\"text-align:left;\">"
                        + DocDate + "</td>"
                        + "</tr>"
                        + "<tr>"
                        + "    <td width=\"120px\" style=\"font-weight:bold; text-align:left;\">Status :&nbsp;</td>"
                        + "    <td width=\"180px\" style=\"text-align:left;\">"
                        + Status + "</td>"
                        + "</tr>"
                        + "    <td width=\"120px\" style=\"font-weight:bold; text-align:left;\">User :&nbsp;</td>"
                        + "    <td width=\"180px\" style=\"text-align:left;\">"
                        + UserApp + "</td>"
                        + "</tr>"
                    + "</table>"
                    + "</td></tr>"

                    + "<tr style=\"height:18px;\"><td>&nbsp;</td></tr> "
                    + "<tr>"
                    + "    <td style=\"font-weight:bold; text-align:left;\">Signatures :&nbsp;</td>"
                    + "</tr>"
                    + "<tr>"
                    + "    <td style=\"font-weight:bold; text-align:left;\" >"
                    + "    Warehouse:__________________&nbsp;&nbsp;Store:__________________&nbsp;&nbsp;Inv Control:__________________ "
                    + "        </td>"
                    + "</tr>"
                    + "<tr style=\"height:8px;\"><td>&nbsp;</td></tr> "
                    + "<tr>"
                    + "    <td style=\"font-weight:bold; text-align:left;\" >"
                    + "    Notes:___________________________________________________________________________ "
                    + "        </td>"
                    + "</tr>"
                    + "</table>"
        + "</fieldset>";
        return str;
    }


    protected void plBarCode_DataBinding(object sender, EventArgs e)
    {

    }
}
