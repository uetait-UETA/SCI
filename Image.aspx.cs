using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public partial class Image : System.Web.UI.Page
{
    public DataManager dm = new DataManager();
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            //Comentado para poder compilar porque esta función no existe en la clase DataManager:
            //DataTable dt = dm.GetFAQImageByID(Request.QueryString["FaqId"]);
            
            
            //MemoryStream stream = new MemoryStream();
            //byte[] image = (byte[])dt.Rows[0]["SolutionImage"];
            //stream.Write(image, 0, image.Length);
            ////Bitmap bitmap = new Bitmap(stream);
            //var bitmap = new Image.FromStream(stream);
            //Response.ContentType = "image/Jpeg";
            //bitmap.Save(Response.OutputStream, ImageFormat.Jpeg);
            //stream.Close();

            //Comentado para poder compilar:
            //byte[] image = (byte[])dt.Rows[0]["SolutionImage"];
            //MemoryStream ms = new MemoryStream(image);
            //Image i = Image.FromStream(ms);
        }
        catch (Exception ex)
        {
            string hjk = ex.Message;
        }
    }
}