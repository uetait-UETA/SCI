using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;
using Telerik;
using Telerik.Web.UI;
using Telerik.Web.UI.GridExcelBuilder;
using System.IO;
using System.Configuration;

public partial class MaintainVideosOperativos : BasePage
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
                    LoadCasos();
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

    private void LoadCasos()
    {
        try
        {
            rcbCasos.Items.Clear();

            DataTable dtCasos = dm.GetVideosOperativosCasos();

            rcbCasos.DataSource = dtCasos;
            rcbCasos.DataValueField = "FaqId";
            rcbCasos.DataTextField = "CASOS";
            rcbCasos.DataBind();
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to Get Casos data", ex.Message.ToString());
        }
    }

    protected void rbtnAddFaq_Click(object sender, EventArgs e)
    {
        try
        {
            if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
            {
                Response.Redirect("Login1.aspx");
            }
            else
            {
                string v_FileExt = "";

                if (fuDocs.HasFile)
                {
                    v_FileExt = Path.GetExtension(fuDocs.PostedFile.FileName);
                }

                string v_Message = dm.UpdateVideosOperativos("I", "", rtbCasos.Text, rtbSoluciones.Text, v_FileExt, rtbSolucionLink.Text, Session["UserId"].ToString());
                int dummy = 0;
                if (int.TryParse(v_Message, out dummy))
                {
                    if (fuDocs.HasFile)
                    {
                        string filename = "IMG" + v_Message + Path.GetExtension(fuDocs.PostedFile.FileName); //Path.GetFileName(fuDocs.FileName);
                        fuDocs.SaveAs(Server.MapPath("~/ImgPreguntasFrecuentes/") + filename);
                    }

                    v_Message = "1";
                    if (v_Message == "1")
                    {
                        rtbCasos.Text = "";
                        rtbSoluciones.Text = "";
                        rtbSolucionLink.Text = "";
                        LoadCasos();
                        ShowMasterPageMessage("Ok", "Success", "Case added succesfully!");
                    }
                    else
                    {
                        ShowMasterPageMessage("Error", "Failed to add case", v_Message.ToString());
                    }
                }
                else
                {
                    ShowMasterPageMessage("Error", "Failed to add case", v_Message.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to add case", ex.Message.ToString());
        }
    }

    protected void rcbCasos_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
    {
        try
        {
            dt = dm.GetVideosOperativosByID(rcbCasos.SelectedValue);

            rtbUCasos.Text = dt.Rows[0]["Casos"].ToString();
            rtbUSoluciones.Text = dt.Rows[0]["Solucion"].ToString();
            rtbUSolucionLink.Text = dt.Rows[0]["SolucionLink"].ToString();
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to get case", ex.Message.ToString());
        }
    }

    protected void rtbUpdate_Click(object sender, EventArgs e)
    {
        try
        {
            if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
            {
                Response.Redirect("Login1.aspx");
            }
            else
            {
                string v_FileExt = "";

                if (fuUDocs.HasFile)
                {
                    //v_DocName = Path.GetFileName(fuUDocs.PostedFile.FileName);
                    //Stream fs = fuUDocs.PostedFile.InputStream;
                    //BinaryReader br = new BinaryReader(fs);
                    //v_DocData = br.ReadBytes((Int32)fs.Length);
                    v_FileExt = Path.GetExtension(fuDocs.PostedFile.FileName);
                }

                string v_Message = dm.UpdateVideosOperativos("U", rcbCasos.SelectedValue, rtbUCasos.Text, rtbUSoluciones.Text, v_FileExt, rtbUSolucionLink.Text, Session["UserId"].ToString());

                if (fuDocs.HasFile)
                {
                    string filename = "IMG" + rcbCasos.SelectedValue + Path.GetExtension(fuDocs.PostedFile.FileName); //Path.GetFileName(fuDocs.FileName);
                    fuDocs.SaveAs(Server.MapPath("~/ImgPreguntasFrecuentes/") + filename);
                }

                if (v_Message == "1")
                {
                    rtbUCasos.Text = "";
                    rtbUSoluciones.Text = "";
                    rtbUSolucionLink.Text = "";
                    rcbCasos.ClearSelection();
                    LoadCasos();
                    ShowMasterPageMessage("Ok", "Success", "Case updated succesfully!");
                }
                else
                {
                    ShowMasterPageMessage("Error", "Failed to update case", v_Message.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to update case", ex.Message.ToString());
        }
    }

    protected void rtbDelete_Click(object sender, EventArgs e)
    {
        try
        {
            if ((string)this.Session["UserId"] == "" || (string)this.Session["UserId"] == null)
            {
                Response.Redirect("Login1.aspx");
            }
            else
            {
                string v_Message = dm.UpdateVideosOperativos("D", rcbCasos.SelectedValue, "", "", "", "", "");

                if (v_Message == "1")
                {
                    rtbUCasos.Text = "";
                    rtbUSoluciones.Text = "";
                    rtbUSolucionLink.Text = "";
                    rcbCasos.ClearSelection();
                    LoadCasos();
                    ShowMasterPageMessage("Ok", "Success", "Case deleted succesfully!");
                }
                else
                {
                    ShowMasterPageMessage("Error", "Failed to delete case", v_Message.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            ShowMasterPageMessage("Error", "Failed to delete case", ex.Message.ToString());
        }
    }
}