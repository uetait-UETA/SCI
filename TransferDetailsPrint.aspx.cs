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
                if (i % linesPerPage == 0)
                    str += hdr + LineItemHeader(ref colspan);

                str += LineItem(dt.Rows[i], i % linesPerPage);

                if ((i + 1) % linesPerPage == 0)
                {
                    str += "</table>"
                        + "<div class=\"pg-footer\">Page " + PageNum + " of " + PageCount + "</div>"
                        + "</div></div>";
                    if (i != dt.Rows.Count - 1)
                        str += "<H1 class=\"SaltoDePagina\"></H1>";
                    PageNum += 1;
                }
            }

            print_off();
        }
        else
        {
            str += "<tr><td colspan=\"" + colspan + "\" style=\"text-align:center; padding:14px; font-size:9pt;\"><b>No Records Found</b></td></tr>";
        }

        if (i % linesPerPage != 0)
        {
            str += "</table>"
                + "<div class=\"pg-footer\">Page " + PageNum + " of " + PageCount + "</div>"
                + "</div></div>";
        }
        //str += HtmlFooter();
        return str;
    }


    private string HeaderInfo()
    {
        UserApp = LvUser;
        string imgSrc = AnyPourpuse.GetBarCodeImage(DocEntry).ImageUrl;

        string str =
            "<div class=\"doc-page\">"

            // ── Title bar ──
            + "<div class=\"doc-title-bar\">"
            + "<span class=\"doc-title-l\">Dispatch / Receive</span>"
            + "<span class=\"doc-title-r\" style=\"color:#cc0000;\">" + OriCopy + "&nbsp;Document&nbsp;#&nbsp;" + DocEntry + "</span>"
            + "</div>"

            // ── Info row ──
            + "<div class=\"doc-info-row\">"
            + "<div class=\"doc-info-left\">"
            + "<div class=\"info-cols\">"
            // Left column
            + "<div class=\"info-col\">"
            + "<div class=\"field-group\"><div class=\"field-label\">From Location</div><div class=\"field-value\">" + FromLoc + "</div></div>"
            + "<div class=\"field-group\"><div class=\"field-label\">To Location</div><div class=\"field-value\">" + ToLoc + "</div></div>"
            + "<div class=\"field-group\"><div class=\"field-label\">Draft Date</div><div class=\"field-value\">" + DocDate + "</div></div>"
            + "<div class=\"field-group\"><div class=\"field-label\">Status</div><div class=\"field-value\">" + Status + "</div></div>"
            + "</div>"
            // Right column
            + "<div class=\"info-col\">"
            + "<div class=\"field-group\"><div class=\"field-label\">Draft User</div><div class=\"field-value\">" + DraftUser + "</div></div>"
            + "<div class=\"field-group\"><div class=\"field-label\">Dispatch User</div><div class=\"field-value\">" + DespUser + "</div></div>"
            + "<div class=\"field-group\"><div class=\"field-label\">Print User</div><div class=\"field-value\">" + UserApp + "</div></div>"
            + "<div class=\"field-group\"><div class=\"field-label\">Total Products</div><div class=\"field-value\">" + TotProds + "</div></div>"
            + "</div>"
            + "</div>"  // info-cols
            + "</div>"  // doc-info-left
            + "<div class=\"doc-info-right\">"
            + "<img src=\"" + imgSrc + "\" alt=\"Barcode " + DocEntry + "\" style=\"max-width:155px; max-height:75px;\" />"
            + "</div>"
            + "</div>"  // doc-info-row

            // ── Signatures ──
            + "<div class=\"sig-section\">"
            + "<div class=\"sig-title\">Signatures</div>"
            + "<div>"
            + "<span class=\"sig-entry\">Origin: <span class=\"sig-line\"></span></span>"
            + "<span class=\"sig-entry\">Destination: <span class=\"sig-line\"></span></span>"
            + "<span class=\"sig-entry\">Inv. Control: <span class=\"sig-line\"></span></span>"
            + "</div>"
            + "<div style=\"margin-top:7px;\" class=\"notes-line\">Notes: <span class=\"sig-line\"></span></div>"
            + "</div>"

            // ── Items section (table appended after) ──
            + "<div class=\"items-wrap\">";

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
            + "<th style=\"width:80px;\">Brand</th>"
            + "<th>Description</th>"
            + "<th style=\"width:72px;\">Price</th>"
            + "<th style=\"width:55px;\">Qty</th>";
        if (order_multiple == "C")
        {
            colspan = (int.Parse(colspan) + 1).ToString();
            str += "<th style=\"width:55px;\">Cases</th>";
        }
        str += "<th style=\"width:72px;\">Qty Control</th>"
            + "<th style=\"width:55px;\">Bins</th>"
            + "</tr>";
        return str;
    }

    // return a single line item table row
    protected string LineItem(DataRow row, int rowIndex = 0)
    {
        string brand = row["U_brand"].ToString();
        if (brand.Length > 14) brand = brand.Substring(0, 13) + "...";

        string str = "<tr>"
            + "<td style=\"text-align:center;\">" + row["LineNumber"] + "</td>"
            + "<td style=\"text-align:center;\">" + row["ItemCode"] + "</td>"
            + "<td style=\"text-align:center;\">" + row["BarCode"] + "</td>"
            + "<td style=\"text-align:left;\">" + brand + "</td>"
            + "<td style=\"text-align:left;\">" + row["Description"] + "</td>"
            + "<td style=\"text-align:right;\">" + String.Format("{0:N2}", row["Price"]) + "</td>"
            + "<td style=\"text-align:center;\">" + String.Format("{0:#,###}", row["Qty"]) + "</td>";
        if (order_multiple == "C")
            str += "<td style=\"text-align:center;\">" + String.Format("{0:#,###}", row["Cases"]) + "</td>";
        str += "<td style=\"text-align:center;\">&nbsp;</td>"
            + "<td style=\"text-align:center;\">" + String.Format("{0:#,###}", row["bins"]) + "</td>"
            + "</tr>";
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