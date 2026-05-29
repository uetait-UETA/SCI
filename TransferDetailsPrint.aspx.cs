using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

public partial class TransferDetailsPrint : System.Web.UI.Page
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
    protected bool isMassivePrinting;
    protected List<DocumentsPrint> docEntries;
    protected int copies;
    

    protected void Page_Load(object sender, EventArgs e)
    {
        totalQty = 0;
        DocEntry = "";
        LvUser = (string)Session["UserId"];

        if (Request.QueryString["DocEntry"] == null)
        {
            Response.Write("ERROR: No document entry number specified in querystring.<br>");
            Response.End();
        }
        else
        {
            sap_db = (string)Session["CompanyId"];

            if (string.IsNullOrEmpty(LvUser))
            {
                Response.Write("<script type=\"text/javascript\">alert('" + "Please log in to the system." + "');</script>");
                Response.End();
            }

            DocEntry = Request.QueryString["DocEntry"].ToString();

            if (DocEntry == "MASIVO")
            {
                isMassivePrinting = true;
                docEntries = (List<DocumentsPrint>)Session["docEntries"];
                divContent.InnerHtml = "";
                int c1 = 0;

                foreach (DocumentsPrint documentsPrint in docEntries)
                {
                    DocEntry = documentsPrint.DocNumber;
                    copies = documentsPrint.Copies;

                    for (int i = 0; i < copies; i++)
                    {
                        divContent.InnerHtml += (c1 == 0 ? "" : "<H1 class=\"SaltoDePagina\"></H1>") + GetTransferDetails(DocEntry, sap_db);
                    }
                    
                    c1++;
                }

                Page.Title = "";
            }
            else
            {
                isMassivePrinting = false;
                divContent.InnerHtml = GetTransferDetails(DocEntry, sap_db);
                //Page.Title = "Dispatch Document No. " + DocEntry;
                Page.Title = "";
            }
        }
    }

    private string GetTransferDetails(string DocEntry, string sap_db)
    {
        string str = "";
        Transfer tsf = new Transfer();

        DataTable dt = tsf.GetTransferDetails(DocEntry, sap_db);

        if (dt != null && dt.Rows.Count > 0)
        {
            DocEntry = dt.Rows[0]["DocEntry"].ToString().Trim().ToUpper();
            Status = dt.Rows[0]["Status"].ToString().Trim().ToUpper();
            DocDate = dt.Rows[0]["DocDate"].ToString().Trim().ToUpper();
            FromLoc = dt.Rows[0]["FromLocName"].ToString().Trim().ToUpper();
            ToLoc = dt.Rows[0]["ToLocName"].ToString().Trim().ToUpper();
            order_multiple = dt.Rows[0]["order_multiple"].ToString().Trim();
            DraftUser = dt.Rows[0]["DraftUser"].ToString().Trim().ToUpper();
            DespUser = dt.Rows[0]["DespUser"].ToString().Trim().ToUpper();
            RecUser = dt.Rows[0]["RecUser"].ToString().Trim().ToUpper();
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
        int linesPerPage = 0;

        if(!int.TryParse(ConfigurationManager.AppSettings["TransfersDetailsPrintLinesPerPage"].ToString(), out linesPerPage))
        {
            //If the conversion fails, it will use the default of 40 lines per page:
            linesPerPage = 40;
        }

        int PageNum = 1;
        int PageCount = 0;
        colspan = "10";
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
                        //str += "<br class=\"page\" />";
                        str += "<H1 class=\"SaltoDePagina\"></H1>";
                    }
                    PageNum += 1;
                }
            }

            print_off(); //1
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


    private string HeaderInfo()
    {
        UserApp = LvUser;
        string imgSrc = AnyPourpuse.GetBarCodeImage(DocEntry).ImageUrl;
        string str = "<table class=\"tblHeader\">"
                        + "<caption><span class=\"captionText\">Dispatch Document No. " + DocEntry + "<span></caption>"
                        + "<tr>"
                            + "<td style=\"padding-left:10px; text-align:left;\">"
                                + "<table>"
                                    + "<tr>"
                                    + "    <td class=\"tblHeaderTitle\" colspan=\"4\">" + OriCopy + " Document No. <span style=\"font-size:11pt;font-weight:normal;\">" + DocEntry + "</span></td>"
                                    + "    <td class=\"barCodeContainer\" rowspan=\"7\">"
                                    + "        <img id=\"img_" + DocEntry + "\" alt=\"IMG_" + DocEntry + "\" class=\"barCodeImg\" src=\"" + imgSrc + "\">"
                                    + "    </td>"
                                    + "</tr>"
                                    + "<tr>"
                                    + "    <td style=\"font-weight:bold;\">Origin:</td>"
                                    + "    <td style=\"padding-left:22px; font-size:8.50pt;\">" + FromLoc + "</td>"
                                    + "</tr>"
                                    + "<tr>"
                                    + "    <td style=\"font-weight:bold;\">Destination:</td>"
                                    + "    <td style=\"padding-left:22px; font-size:8.50pt;\">" + ToLoc + "</td>"
                                    + "</tr>"
                                    + "<tr>"
                                    + "    <td style=\"font-weight:bold;\">Draft Date:</td>"
                                    + "    <td style=\"padding-left:22px;\">" + DocDate + "</td>"
                                    + "</tr>"
                                    + "<tr>"
                                    + "    <td style=\"font-weight:bold;\">Status:</td>"
                                    + "    <td style=\"padding-left:22px;\">" + Status + "</td>"
                                    + "</tr>"
                                    + "<tr>"
                                    + "    <td style=\"font-weight:bold;\">Draft User:</td>"
                                    + "    <td style=\"padding-left:22px;\">" + DraftUser + "</td>"
                                    + "</tr>"
                                    + "<tr>"
                                    + "    <td style=\"font-weight:bold;\">Dispatch User:</td>"
                                    + "    <td style=\"padding-left:22px;\">" + DespUser + "</td>"
                                    + "</tr>"
                                    + "<tr>"
                                    + "    <td style=\"font-weight:bold;\">Print User:</td>"
                                    + "    <td style=\"padding-left:22px;\">" + UserApp + "</td>"
                                    + "    <td style=\"font-weight:bold;padding-left:30px\">Total Products:</td>"
                                    + "    <td style=\"padding-left:20px;\">" + TotProds + "</td>"
                                    + "</tr>"
                                + "</table>"
                            + "</td>"
                        + "</tr>"

                        + "<tr>"
                        + "    <td style=\"font-weight:bold; padding-top:2px;\">Signatures:</td>"
                        + "</tr>"
                        + "<tr>"
                        + "    <td style=\"font-weight:bold;\">"
                        + "        Origin:&nbsp;&nbsp;&nbsp;____________________________&nbsp;&nbsp;Destination:&nbsp;&nbsp;____________________________&nbsp;&nbsp;&nbsp;&nbsp;Inventory Control:&nbsp;&nbsp;&nbsp;&nbsp;________________________________"
                        + "    </td>"
                        + "</tr>"
                        + "<tr>"
                        + "    <td style=\"font-weight:bold; padding-top:9px;\">"
                        + "        Notes:________________________________________________________________________________________________________________________"
                        + "    </td>"
                        + "</tr>"
                    + "</table>";

        return str;
    }

    // return a line item header row 
    protected string LineItemHeader(ref string colspan)
    {
        string str = ""
        + "<table style=\"width:100%; font-family: Arial, Helvetica, sans-serif;\">"
        + "<tr class=\"printHeader\">";
        str += "<td style=\"text-align:center;\">Line</td>";
        str += "<td style=\"text-align:center;\">Code</td>";
        str += "<td style=\"text-align:center;\">Barcode</td>";
        str += "<td style=\"text-align:left;\">Brand</td>";
        str += "<td style=\"text-align:left;\">Description</td>";
        str += "<td style=\"text-align:center;\">Price</td>";
        str += "<td style=\"text-align:center;\">Qty</td>";
        if (order_multiple == "C")
        {
            colspan = (int.Parse(colspan) + 1).ToString();
            str += "<td style=\"text-align:center;\">Cases</td>";
        }
        str += "<td style=\"text-align:center;\">Qty Control</td>";
        str += "<td style=\"text-align:center;\">Bins</td>";
        str += "</tr>";
        return str;
    }

    // return a single line item table row 

    protected string LineItem(DataRow row)
    {
        string str = ""
        + "<tr class=\"printRow\" >";
        str += "<td class=\"printData\" style=\"text-align:center; \">" + row["LineNumber"].ToString() + "</td>";
        str += "<td class=\"printData\" style=\"text-align:center; font-size:8.50pt; \">" + row["ItemCode"].ToString() + "</td>";
        str += "<td class=\"printData\" style=\"text-align:center; \">" + row["BarCode"].ToString() + "</td>";
        str += "<td class=\"printData\" style=\"text-align:left; font-size:6pt;\"> " + ((row["U_brand"].ToString().Length <= 14) ? row["U_brand"].ToString() : row["U_brand"].ToString().Substring(0, 13) + "...").ToString() + "</td>";
        str += "<td class=\"printData\" style=\"text-align:left; \">" + row["Description"].ToString() + "</td>";
        str += "<td class=\"printData\" style=\"text-align:center; \">" + String.Format("{0:C}", row["Price"]) + "</td>";
        str += "<td class=\"printData\" style=\"text-align:center; font-size:8.50pt;\">" + String.Format("{0:#,###}", row["Qty"]) + "</td>";
        if (order_multiple == "C")
        {
            str += "<td class=\"printData\" style=\"text-align:center; \">" + String.Format("{0:#,###}", row["Cases"]) + "</td>";
        }
        str += "<td class=\"printData\" style=\"text-align:center; \"></td>";
        str += "<td class=\"printData\" style=\"text-align:center; \">" + String.Format("{0:#,###}", row["bins"]) + "</td></tr>";
        str += "<tr><td class=\"printDataBottom\" colspan=\"" + colspan + "\"></td></tr>";
        return str;
    }

    protected void Button1_Click(object sender, EventArgs e)
    {
        ClientScript.RegisterStartupScript(GetType(), "hwa", "window.print()", true);
        Button1.Visible = false;

        string sql = "";

        try
        {
            if (!isMassivePrinting)
            {
                sql = @"insert into smm_Print_Control
                        (DocEntry, DocNum, PrintType, PrintUser, Date_Created, Created_By) values
                        (" + DocEntry + @" , NULL, 'DESPATCH', '" + LvUser + @"', getdate(), '" + LvUser + @"')";

                db.Connect();
                db.cmd.Parameters.Clear();
                db.cmd.CommandType = CommandType.Text;
                db.cmd.CommandText = sql;
                db.cmd.Connection = db.Conn;
                db.cmd.ExecuteNonQuery();
            }
            else
            {
                if(Session["docEntries"] != null)
                {
                    List<DocumentsPrint> docs = (List<DocumentsPrint>)Session["docEntries"];

                    db.Connect();
                    db.cmd.Parameters.Clear();

                    foreach (DocumentsPrint item in docs)
                    {
                        DocEntry = item.DocNumber;

                        sql = @"insert into smm_Print_Control
                        (DocEntry, DocNum, PrintType, PrintUser, Date_Created, Created_By) values
                        (" + DocEntry + @" , NULL, 'DESPATCH', '" + LvUser + @"', getdate(), '" + LvUser + @"')";

                        if(db.DbConnectionState != System.Data.ConnectionState.Open)
                        {
                            db.Connect();
                        }
                        
                        db.cmd.CommandType = CommandType.Text;
                        db.cmd.CommandText = sql;
                        db.cmd.Connection = db.Conn;
                        db.cmd.ExecuteNonQuery();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in  Button1_Click. ERROR MESSAGE : " + ex.Message);
        }
        finally
        {
            db.Disconnect();
        }
    }


    protected void print_off()
    {
        int lNumPrintsDone = 0;
        int lPrintsPermitted = 0;

        if (LvUser == "" || LvUser == null)
        {
            Response.Write("<script type=\"text/javascript\">alert('" + "Please log in to the system." + "');</script>");
            Response.End();
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

            SqlDataReader lReader = db.cmd.ExecuteReader();

            while (lReader.Read())
            {
                lNumPrintsDone = Convert.ToInt32(lReader.GetString(0));
                lPrintsPermitted = Convert.ToInt32(lReader.GetString(1));
            }

            lReader.Close();
        }
        catch (Exception ex)
        {
            throw new Exception("Caught exception in  print_off. ERROR MESSAGE : " + ex.Message);
        }
        finally
        {
            db.Disconnect();
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