using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Web.UI;
//using System.Web.UI.WebControls;
using System.Xml;
using Telerik;
using Telerik.Web.UI;
using System.Data.SqlClient;
using System.IO;
using System.Drawing;
using System.Web.UI.WebControls;

public partial class PreOrderManagement : BasePage
{
    public DataTable dt = new DataTable();
    public DataManager dm = new DataManager();

    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            if (!IsPostBack)
            {
                if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
                {
                    Response.Redirect("Login1.aspx");
                }
                else
                {
                    string v_Message = dm.GetNewPreOrderHeaders((string)this.Session["UserId"]);

                    if (v_Message != "1")
                    {
                        ShowMasterPageMessage("Error", "Failed to Get New Orders", v_Message.ToString());
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed in Page_Load", ex.Message.ToString());
            return;
        }
    }
    private void ShowMasterPageMessage(string v_Message_Type, string v_Message_Title, string v_Message)
    {
        try
        {
            SiteMaster sm = (SiteMaster)this.Master;
            sm.ShowDivMessage(v_Message_Type, v_Message_Title, v_Message);
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed in ShowMasterPageMessage", ex.Message.ToString());
            return;
        }
    }

    protected void rgHead_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        try
        {
            DataTable dtFAQ = new DataTable();
            dtFAQ = dm.GetPreOrderHeaders(rcbPrinted.SelectedValue, rcbPicked.SelectedValue, rcbPacked.SelectedValue, rcbDelivered.SelectedValue);

            rgHead.DataSource = dtFAQ;
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
            return;
        }
    }

    protected void rgHead_ItemDataBound(object sender, GridItemEventArgs e)
    {
        try
        {
            if (e.Item is GridDataItem)
            {
                GridDataItem item = (GridDataItem)e.Item;
                LinkButton lbPrinted = item["Printed"].Controls[0] as LinkButton;
                LinkButton lbPicked = item["Picked"].Controls[0] as LinkButton;
                LinkButton lbPacked = item["Packed"].Controls[0] as LinkButton;
                LinkButton lbDelivered = item["Delivered"].Controls[0] as LinkButton;

                lbPrinted.Attributes["onclick"] = "return confirm('Are you sure you want to change the status? This will change the status of Picked, Packed and Delivered if you changing the status from Yes to No')";
                lbPicked.Attributes["onclick"] = "return confirm('Are you sure you want to change the status? This will change the status of Packed and Delivered if you changing the status from Yes to No')";
                lbPacked.Attributes["onclick"] = "return confirm('Are you sure you want to change the status? This will change the status of Delivered if you changing the status from Yes to No')";
                lbDelivered.Attributes["onclick"] = "return confirm('Are you sure you want to change the delivery status?')";

                if (item["isPrinted"].Text == "Y")
                {
                    lbPrinted.Text = "Yes";
                }
                else
                {
                    lbPrinted.Text = "No";
                }

                if (item["isPicked"].Text == "Y")
                {
                    lbPicked.Text = "Yes";
                }
                else
                {
                    lbPicked.Text = "No";
                }

                if (item["isPacked"].Text == "Y")
                {
                    lbPacked.Text = "Yes";
                }
                else
                {
                    lbPacked.Text = "No";
                }

                if (item["isDelivered"].Text == "Y")
                {
                    lbDelivered.Text = "Yes";
                }
                else
                {
                    lbDelivered.Text = "No";
                }
                //RadBinaryImage rb = item.FindControl("Image1") as RadBinaryImage;

                ////string hjk = rb.AlternateText;
                //if (rb.AlternateText == "")
                //{
                //    rb.Visible = false;
                //}
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get data", ex.Message.ToString());
            return;
        }
    }

    protected void rgHead_ItemCommand(object sender, GridCommandEventArgs e)
    {
        try
        {
            string v_Status = "";

            if (e.CommandName == "View")
            {
                if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
                {
                    Response.Redirect("Login1.aspx");
                }
                else
                {
                    GridDataItem item = (GridDataItem)e.Item;

                    hfInvoiceNumber.Value = (e.Item as GridDataItem).GetDataKeyValue("InvoiceNumber").ToString();
                    if ((e.Item as GridDataItem).GetDataKeyValue("MagentoOrderNumber") == null || (e.Item as GridDataItem).GetDataKeyValue("MagentoOrderNumber") == "")
                    {
                        hfMagentoOrderNumber.Value = "0";
                    }
                    else
                    {
                        hfMagentoOrderNumber.Value = (e.Item as GridDataItem).GetDataKeyValue("MagentoOrderNumber").ToString();
                    }

                    //pnlDetails.Visible = false;
                    pnlDetails.Style["display"] = "block";

                    DataTable dtHead = dm.GetPreOrderHeaderByInvoiceID(hfInvoiceNumber.Value.ToString());

                    lblWebOrderNumber.Text = dtHead.Rows[0]["MagentoOrderNumber"].ToString();
                    lblCustomerName.Text = dtHead.Rows[0]["CustomerName"].ToString();
                    lblPassportNumber.Text = dtHead.Rows[0]["CustomerPassportNumber"].ToString();
                    lblFlight.Text = dtHead.Rows[0]["Flight"].ToString();
                    lblPickupDate.Text = DateTime.Now.ToString();
                    lblPrintDate.Text = DateTime.Now.ToString();
                    lblUser.Text = (string)this.Session["UserId"];
                    lblInvoiceNumber.Text = dtHead.Rows[0]["InvoiceNumber"].ToString();
                    lblFlightDate.Text = Convert.ToDateTime(dtHead.Rows[0]["FlightDate"]).ToShortDateString();
                    lblFlightTime.Text = dtHead.Rows[0]["FlightTime"].ToString().Substring(0, 5);
                    lblQty.Text = dtHead.Rows[0]["Qty"].ToString();

                    DataTable dt1 = dm.GetPreOrderDetails(hfInvoiceNumber.Value.ToString(), hfMagentoOrderNumber.Value.ToString());

                    lblEstatus.Text = dt1.Rows[0]["EStatus"].ToString();

                    rgDetail.DataSource = dt1;
                    rgDetail.Rebind();
                }
            }
            if (e.CommandName == "Print")
            {
                if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
                {
                    Response.Redirect("Login1.aspx");
                }
                else
                {
                    GridDataItem item = (GridDataItem)e.Item;
                    LinkButton lbPrinted = item["Printed"].Controls[0] as LinkButton;

                    string v_MagentoOrderNumber = "";
                    string v_InvoiceNumber = (e.Item as GridDataItem).GetDataKeyValue("InvoiceNumber").ToString();
                    if ((e.Item as GridDataItem).GetDataKeyValue("MagentoOrderNumber") == null || (e.Item as GridDataItem).GetDataKeyValue("MagentoOrderNumber") == "")
                    {
                        v_MagentoOrderNumber = "0";
                    }
                    else
                    {
                        v_MagentoOrderNumber = (e.Item as GridDataItem).GetDataKeyValue("MagentoOrderNumber").ToString();
                    }

                    string v_UserID = (string)this.Session["UserId"];

                    if (lbPrinted.Text == "Yes")
                    {
                        v_Status = "N";
                    }
                    else
                    {
                        v_Status = "Y";
                    }

                    string v_Message = dm.PreOrderPrinted(v_InvoiceNumber.ToString(), v_MagentoOrderNumber.ToString(), v_Status, v_UserID.ToString());

                    if (v_Message != "1")
                    {
                        ShowMasterPageMessage("Error", "Failed to update as delivered", v_Message.ToString());
                        return;
                    }
                    else
                    {
                        rgHead.Rebind();
                        pnlDetails.Style["display"] = "none";
                    }
                }
            }
            if (e.CommandName == "Picked")
            {
                if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
                {
                    Response.Redirect("Login1.aspx");
                }
                else
                {
                    GridDataItem item = (GridDataItem)e.Item;
                    LinkButton lbPicked = item["Picked"].Controls[0] as LinkButton;
                    if (item["isPrinted"].Text == "N")
                    {
                        ShowMasterPageMessage("Error", "Update Failed", "You cannot change the pick status before printing.");
                        return;
                    }

                    string v_MagentoOrderNumber = "";
                    string v_InvoiceNumber = (e.Item as GridDataItem).GetDataKeyValue("InvoiceNumber").ToString();
                    if ((e.Item as GridDataItem).GetDataKeyValue("MagentoOrderNumber") == null || (e.Item as GridDataItem).GetDataKeyValue("MagentoOrderNumber") == "")
                    {
                        v_MagentoOrderNumber = "0";
                    }
                    else
                    {
                        v_MagentoOrderNumber = (e.Item as GridDataItem).GetDataKeyValue("MagentoOrderNumber").ToString();
                    }

                    string v_UserID = (string)this.Session["UserId"];

                    if (lbPicked.Text == "Yes")
                    {
                        v_Status = "N";
                    }
                    else
                    {
                        v_Status = "Y";
                    }

                    string v_Message = dm.PreOrderPicked(v_InvoiceNumber.ToString(), v_MagentoOrderNumber.ToString(), v_Status, v_UserID.ToString());

                    if (v_Message != "1")
                    {
                        ShowMasterPageMessage("Error", "Failed to update as picked", v_Message.ToString());
                        return;
                    }
                    else
                    {
                        rgHead.Rebind();
                        pnlDetails.Style["display"] = "none";
                    }
                }
            }
            if (e.CommandName == "Packed")
            {
                if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
                {
                    Response.Redirect("Login1.aspx");
                }
                else
                {
                    GridDataItem item = (GridDataItem)e.Item;
                    LinkButton lbPacked = item["Packed"].Controls[0] as LinkButton;
                    if (item["isPicked"].Text == "N")
                    {
                        ShowMasterPageMessage("Error", "Update Failed", "You cannot change the pack status before picking.");
                        return;
                    }

                    string v_MagentoOrderNumber = "";
                    string v_InvoiceNumber = (e.Item as GridDataItem).GetDataKeyValue("InvoiceNumber").ToString();
                    if ((e.Item as GridDataItem).GetDataKeyValue("MagentoOrderNumber") == null || (e.Item as GridDataItem).GetDataKeyValue("MagentoOrderNumber") == "")
                    {
                        v_MagentoOrderNumber = "0";
                    }
                    else
                    {
                        v_MagentoOrderNumber = (e.Item as GridDataItem).GetDataKeyValue("MagentoOrderNumber").ToString();
                    }

                    string v_UserID = (string)this.Session["UserId"];

                    if (lbPacked.Text == "Yes")
                    {
                        v_Status = "N";
                    }
                    else
                    {
                        v_Status = "Y";
                    }

                    string v_Message = dm.PreOrderPacked(v_InvoiceNumber.ToString(), v_MagentoOrderNumber.ToString(), v_Status, v_UserID.ToString());

                    if (v_Message != "1")
                    {
                        ShowMasterPageMessage("Error", "Failed to update as picked", v_Message.ToString());
                        return;
                    }
                    else
                    {
                        rgHead.Rebind();
                        pnlDetails.Style["display"] = "none";
                    }
                }
            }
            if (e.CommandName == "Delivered")
            {
                if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
                {
                    Response.Redirect("Login1.aspx");
                }
                else
                {
                    GridDataItem item = (GridDataItem)e.Item;
                    LinkButton lbDelivered = item["Delivered"].Controls[0] as LinkButton;
                    if (item["isPacked"].Text == "N")
                    {
                        ShowMasterPageMessage("Error", "Update Failed", "You cannot change the delivery status before packing.");
                        return;
                    }

                    string v_MagentoOrderNumber = "";
                    string v_InvoiceNumber = (e.Item as GridDataItem).GetDataKeyValue("InvoiceNumber").ToString();
                    if ((e.Item as GridDataItem).GetDataKeyValue("MagentoOrderNumber") == null || (e.Item as GridDataItem).GetDataKeyValue("MagentoOrderNumber") == "")
                    {
                        v_MagentoOrderNumber = "0";
                    }
                    else
                    {
                        v_MagentoOrderNumber = (e.Item as GridDataItem).GetDataKeyValue("MagentoOrderNumber").ToString();
                    }

                    string v_UserID = (string)this.Session["UserId"];

                    if (lbDelivered.Text == "Yes")
                    {
                        v_Status = "N";
                    }
                    else
                    {
                        v_Status = "Y";
                    }

                    string v_Message = dm.PreOrderDelivered(v_InvoiceNumber.ToString(), v_MagentoOrderNumber.ToString(), v_Status, v_UserID.ToString());

                    if (v_Message != "1")
                    {
                        ShowMasterPageMessage("Error", "Failed to update as delivered", v_Message.ToString());
                        return;
                    }
                    else
                    {
                        rgHead.Rebind();
                        pnlDetails.Style["display"] = "none";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to update as picked", ex.Message.ToString());
            return;
        }
    }

    protected void rgDetail_NeedDataSource(object sender, GridNeedDataSourceEventArgs e)
    {
        if (hfInvoiceNumber.Value != "-1" && hfMagentoOrderNumber.Value != "-1")
        {
            DataTable dt1 = dm.GetPreOrderDetails(hfInvoiceNumber.Value.ToString(), hfMagentoOrderNumber.Value.ToString());
            rgDetail.DataSource = dt1;
        }
    }

    protected void rcbPrinted_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        //pnlDetails.Visible = false;
        pnlDetails.Style["display"] = "none";
        rgHead.Rebind();
    }

    protected void rcbPicked_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        //pnlDetails.Visible = false;
        pnlDetails.Style["display"] = "none";
        rgHead.Rebind();
    }

    protected void rcbPacked_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        //pnlDetails.Visible = false;
        pnlDetails.Style["display"] = "none";
        rgHead.Rebind();
    }

    protected void rcbDelivered_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        //pnlDetails.Visible = false;
        pnlDetails.Style["display"] = "none";
        rgHead.Rebind();
    }
}