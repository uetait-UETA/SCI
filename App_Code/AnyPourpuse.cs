using System;
using System.Drawing;
using System.IO;

/// <summary>
/// Summary description for AnyPourpuse
/// </summary>
public static class AnyPourpuse
{
    public static System.Web.UI.WebControls.Image GetBarCodeImage(string text)
    {
        string barCode = text;
        System.Web.UI.WebControls.Image imgBarCode = null;

        try
        {
            imgBarCode = new System.Web.UI.WebControls.Image();
            using (Bitmap bitMap = new Bitmap(barCode.Length * 35, 80))
            {
                using (Graphics graphics = Graphics.FromImage(bitMap))
                {
                    Font oFont = new Font("IDAutomationHC39M", 16);
                    PointF point = new PointF(2f, 2f);
                    SolidBrush blackBrush = new SolidBrush(Color.Black);
                    SolidBrush whiteBrush = new SolidBrush(Color.White);
                    graphics.FillRectangle(whiteBrush, 0, 0, bitMap.Width, bitMap.Height);
                    graphics.DrawString("*" + barCode + "*", oFont, blackBrush, point);
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    bitMap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    byte[] byteImage = ms.ToArray();

                    Convert.ToBase64String(byteImage);
                    imgBarCode.ImageUrl = "data:image/png;base64," + Convert.ToBase64String(byteImage);
                }
            }
        }
        catch (Exception)
        {

        }

        return imgBarCode;
    }

    public static string GetSelectedItems(Telerik.Web.UI.RadComboBox comboBox)
    {
        string v_Items = string.Empty;
        int i = 0;

        try
        {
            if (comboBox.CheckedItems.Count != 0)
            {
                foreach (var item in comboBox.CheckedItems)
                {
                    i = i + 1;
                    v_Items = v_Items + "," + item.Value;
                }
            }

            v_Items = v_Items.Substring(1);

            return v_Items;
        }
        catch (Exception)
        {
            return v_Items;
        }
    }
}
