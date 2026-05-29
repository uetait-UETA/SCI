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

public partial class TransferDetails : System.Web.UI.Page
{
    protected int totalQty;
    protected string Status;
    protected string DocDate;
    protected string FromLoc;
    protected string ToLoc;
    protected string DocEntry;
    protected string colspan;
    protected string UserApp;
    protected string DraftUser;
    protected string DespUser;
    protected string RecUser;
    string order_multiple;
    protected string LvUser = "";
    protected string OriCopy = "SCREEN of ";
    protected string TotProds = "";
    protected string sap_db;
    protected SqlDb db = new SqlDb();

    protected void Page_Load(object sender, EventArgs e)
    {
        totalQty = 0;
        DocEntry = "";
        LvUser = (string)this.Session["UserId"];

        if (Request.QueryString["DocEntry"] == null)
        {
            Response.Write("ERROR: No document entry number specified in querystring.<br>");
            Response.End();
        }
        else
        {
            sap_db = (string)this.Session["CompanyId"];
            if (LvUser == "" || LvUser == null)
            {
                Response.Write("<script type=\"text/javascript\">alert('" + "Please log in to the system." + "');</script>");
                Response.End();
                //Response.Redirect("Login1.aspx");
            }
            DocEntry = Request.QueryString["DocEntry"].ToString();
            //DocEntry = "14";
            divContent.InnerHtml = GetTransferDetails(DocEntry, sap_db);
            this.Page.Title = "Dispatch Document No. " + DocEntry;


        }


    }

    private string GetTransferDetails(string DocEntry, string sap_db)
    {
        string str = "";
        Transfer tsf = new Transfer();

        DataTable dt = tsf.GetTransferDetails(DocEntry, sap_db);

        if (dt != null && dt.Rows.Count > 0)
        {
            DocEntry = dt.Rows[0]["DocEntry"].ToString().Trim();
            Status = dt.Rows[0]["Status"].ToString().Trim();
            DocDate = dt.Rows[0]["DocDate"].ToString().Trim();//String.Format("{0:MM/dd/yyyy hh:mm}", dt.Rows[0]["DocDate"]);
            FromLoc = dt.Rows[0]["FromLocName"].ToString().Trim();
            ToLoc = dt.Rows[0]["ToLocName"].ToString().Trim();
            order_multiple = dt.Rows[0]["order_multiple"].ToString().Trim();
            DraftUser = dt.Rows[0]["DraftUser"].ToString().Trim();
            DespUser = dt.Rows[0]["DespUser"].ToString().Trim();
            RecUser = dt.Rows[0]["RecUser"].ToString().Trim();
            OriCopy = dt.Rows[0]["oricopy"].ToString();
            TotProds = dt.Rows[0]["TotalProds"].ToString();


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
        int linesPerPage = 47;
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

            print_off(); //1
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

    private string HeaderInfo()
    {
        UserApp = (string)this.Session["UserId"];
        string str =
             "<fieldset style=\"text-align:center; width:600px;\">"
            //+ "<legend class=\"legendStandard\"></legend>"
            + "<table style=\"width:95%\">"
                + "<tr><td style=\"padding-left:10px; text-align:left;\">"                
                    + "<table width=\"1000px\" style=\"width:1000px; text-align:left;\">"
                        + "<tr>"
                        + "    <td colspan=\"4\" width=\"400px\" style=\"font-weight:bold; font-size:medium; text-align:left; color:red\">" + OriCopy + " Document No. " + DocEntry + "</td>"
                        + "</tr>"
                        + "<tr>"
                        + "    <td width=\"200px\" style=\"font-weight:bold; text-align:left;\">Origin :&nbsp;</td>"
                        + "    <td width=\"400px;\" style=\"text-align:left; font-size:11pt;\">"
                        + FromLoc + "</td>"
                        + "</tr>"
                        + "<tr>"
                        + "    <td width=\"200px\" style=\"font-weight:bold; text-align:left;\">Destination :&nbsp;</td>"
                        + "    <td width=\"400px\" style=\"text-align:left; font-size:11pt;\">"
                        + ToLoc + "</td>"
                        + "</tr>"
                        + "<tr>"
                         + "   <td width=\"200px\" style=\"font-weight:bold; text-align:left;\">Draft Date :&nbsp;</td>"
                        + "    <td width=\"400px\" style=\"text-align:left; font-size:11pt;\">"
                        + DocDate + "</td>"
                        + "</tr>"
                        + "<tr>"
                        + "    <td width=\"200px\" style=\"font-weight:bold; text-align:left;\">Status :&nbsp;</td>"
                        + "    <td width=\"400px\" style=\"text-align:left; font-size:11pt;\">"
                        + Status + "</td>"
                        + "</tr>"
                         + "<tr>"
                        + "    <td width=\"200px\" style=\"font-weight:bold; text-align:left;\">Draft User :&nbsp;</td>"
                        + "    <td width=\"400px\" style=\"text-align:left; font-size:11pt;\">"
                        + DraftUser + "</td>"
                        + "</tr>"
                         + "<tr>"
                        + "    <td width=\"200px\" style=\"font-weight:bold; text-align:left;\">Dispatch User :&nbsp;</td>"
                        + "    <td width=\"400px\" style=\"text-align:left; font-size:11pt;\">"
                        + DespUser + "</td>"
                        + "</tr>"
                        + "    <td width=\"200px\" style=\"font-weight:bold; text-align:left;\">Print User :&nbsp;</td>"
                        + "    <td width=\"400px\" style=\"text-align:left; font-size:11pt;\">"
                        + UserApp + "</td>"
                        //+ "</tr>"
                        //+ "</tr>"
                        + "    <td width=\"200px\" style=\"font-weight:bold; text-align:left;\">Total Products :&nbsp;</td>"
                        + "    <td width=\"400px\" style=\"text-align:left; font-size:11pt;\">"
                        + TotProds + "</td>"
                        + "</tr>"
                    + "</table>"
                    + "</td></tr>"

                    + "<tr style=\"height:18px;\"><td>&nbsp;</td></tr> "
                    + "<tr>"
                    + "    <td style=\"font-weight:bold; text-align:left;\">Signatures:&nbsp;</td>"
                    + "</tr>"
                    + "<tr>"
                    + "    <td style=\"font-weight:bold; text-align:left;\" >"
                    + "    Origin:__________________&nbsp;&nbsp;Destination:__________________&nbsp;&nbsp;Inventory Control:_______________________"
                    + "        </td>"
                    + "</tr>"
                    + "<tr style=\"height:8px;\"><td>&nbsp;</td></tr> "
                    + "<tr>"
                    + "    <td style=\"font-weight:bold; text-align:left;\" >"
                    + "    Notes:______________________________________________________________________________________"
                    + "        </td>"
                    + "</tr>"
                    + "</table>"
        + "</fieldset>";

        return str;
    }


    // return a single line item table row 
    protected string LineItem(DataRow row)
    {
        string str = ""
        + "     <tr style=\"font-size:11pt; \">";
        str += "<td align=center style=\"width:40px; font-size:11pt; \">" + row["LineNumber"].ToString() + @"</td>";
        str += "<td align=center style=\"width:150px; font-size:11pt;\">" + row["ItemCode"].ToString() + @"</td>";
        str += "<td align=center style=\"width:150px; font-size:11pt; text-align:left;\">" + row["BarCode"].ToString() + @"</td>";
        str += "<td align=left style=\"width:80px; font-size:11pt;\">" + row["U_brand"].ToString() + @"</td>";
        str += "<td align=left style=\"width:420px; font-size:10pt;\">" + row["Description"].ToString() + @"</td>";
        str += "<td align=right  style=\"width:40px; font-size:11pt; text-align:right;\">" + String.Format("{0:C}", row["Price"]) + @"</td>";
        str += "<td align=center style=\"width:40px; font-size:11pt; text-align:center;\">" + String.Format("{0:#,###}", row["Qty"]) + @"</td>";
        if (order_multiple == "C")
        { str += "<td align=center style=\"width:40px; font-size:11pt; text-align:center;\">" + String.Format("{0:#,###}", row["Cases"]) + @"</td>"; }
        str += "<td align=center style=\"width:40px; font-size:11pt;\">&nbsp;</td>";
        str += "<td align=center style=\"width:400px; font-size:11pt; text-align:center;\">" + String.Format("{0:#,###}", row["bins"]) + @"</td></tr>";
        str += "<tr><td colspan=" + colspan + " style=\"height:1px; border-bottom:solid 1px black; font-size:11pt;\">&nbsp;</td></tr>\r\n";
        return str;
    }

    // return a line item header row 
    protected string LineItemHeader()
    {
        string str = ""
        + "<table width=\"950px\">"
        + "     <tr style=\"font-size:11pt; font-weight:bold; color:white; text-decoration:underline; background-color:black\">";
        str += "<td align=center style=\"width:40px; font-size:11pt;\">Line</td>";
        str += "<td align=center style=\"width:150px; font-size:11pt;\">Code</td>";
        str += "<td align=center style=\"width:150px; font-size:11pt;\">Barcode</td>";
        str += "<td align=left style=\"width:80px; font-size:11pt;\">Brand</td>";
        str += "<td align=left style=\"width:420px; font-size:10pt;\">Description</td>";
        str += "<td align=right style=\"width:40px; font-size:11pt; text-align:right;\">Price</td>";
        str += "<td align=center style=\"width:80px; font-size:11pt; text-align:center;\">Qty</td>";
        if (order_multiple == "C")
        { str += "<td align=center style=\"width:40px; font-size:11pt; text-align:center;\">Cases</td>"; }
        str += "<td align=center style=\"width:80px; font-size:11pt;  text-align:center;\">Qty Control</td>";
        str += "<td align=center style=\"width:400px; font-size:11pt; text-align:center;\">Bins</td></tr>\r\n";
        return str;
    }


    protected void Button1_Click(object sender, EventArgs e)
    {


        ClientScript.RegisterStartupScript(GetType(), "hwa", "window.print()", true);

        Button1.Visible = false;




        string sql = @"insert into smm_Print_Control
                        (DocEntry, DocNum, PrintType, PrintUser, Date_Created, Created_By) values
                        (" + DocEntry + @" , NULL, 'DESPATCH', '" + LvUser + @"', getdate(), '" + LvUser + @"')";

        try
        {
            db.Connect();
            db.cmd.Parameters.Clear();
            db.cmd.CommandType = CommandType.Text;
            db.cmd.CommandText = sql;
            db.cmd.Connection = db.Conn;
            db.cmd.ExecuteNonQuery();
            db.Conn.Close();

        }

        catch (Exception ex)
        {
            throw new Exception("Caught exception in  Button1_Click. ERROR MESSAGE : " + ex.Message);
        }
    }

    protected void print_off()
    {
        //DataTable dt = new DataTable();
        int lNumPrintsDone = 0;
        int lPrintsPermitted = 0;

        if (LvUser == "" || LvUser == null)
        {
            Response.Write("<script type=\"text/javascript\">alert('" + "Please log in to the system." + "');</script>");
            Response.End();
            //Response.Redirect("Login1.aspx");
        }


        string sql = @"SELECT  cast(A.NumPrintsDone as char) NumPrintsDone, cast(B.PrintsPermitted as char) PrintsPermitted
                            from
                            (select count(a.DocEntry) NumPrintsDone
                            from smm_Print_Control a 
                            where  a.docentry =  " + DocEntry + @"
                              and a.PrintType = 'DESPATCH'                                                 
                            ) A,
                            (
                            select  isnull(NumPrints,0) as PrintsPermitted
                            from SMM_LOGIN 
                            where  UPPER(LoginId) = UPPER('" + LvUser + @"')) B ";

        try
        {
            db.Connect();
            db.cmd.Parameters.Clear();
            db.cmd.CommandType = CommandType.Text;
            db.cmd.CommandText = sql;
            db.cmd.Connection = db.Conn;
            //db.cmd.ExecuteNonQuery();


            SqlDataReader lReader = db.cmd.ExecuteReader();




            while (lReader.Read())
            {


                lNumPrintsDone = Convert.ToInt32(lReader.GetString(0));
                lPrintsPermitted = Convert.ToInt32(lReader.GetString(1));
            }

            lReader.Close();

            db.Conn.Close();
        }

        catch (Exception ex)
        {
            throw new Exception("Caught exception in  print_off. ERROR MESSAGE : " + ex.Message);
        }

        if (lNumPrintsDone == 0)
        {
            Button1.Visible = true;
            OriCopy = "SCREEN of ";
        }
        else
        {
            if (lNumPrintsDone > lPrintsPermitted)
            {
                Button1.Visible = false;
            }
            else
            {
                Button1.Visible = true;
            }
        }
    }




}

