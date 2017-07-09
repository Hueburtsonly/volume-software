using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WpfUsbApp1
{
    class ImageTemp
    {
        public static byte[] GenImageStream(byte disp, String s)
        {
            Bitmap bm = new Bitmap(128, 32, PixelFormat.Format32bppRgb);
            using (Graphics g = Graphics.FromImage(bm))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                g.Clear(Color.White);
                
                //g.DrawLine(Pens.Black, 0, 0, 128, 32);
                //g.DrawString()

                Font f = new Font("Arial Narrow", 24, FontStyle.Regular, GraphicsUnit.Pixel);

                //TextRenderer.DrawText(g, s, f, new Rectangle(0, 0, 128, 32), Color.Black, TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                g.DrawString(s, f, Brushes.Black, new RectangleF(0, 0, 128, 32));
            }
            byte[] ret = new byte[516];
            ret[0] = 4;
            ret[1] = 2;
            ret[2] = disp;
            ret[3] = 0;
            for (int ry = 0; ry < 4; ry++)
            {
                for (int x = 0; x < 128; x++)
                {
                    byte v = 0;
                    for (int sy = 0; sy < 8; sy++)
                    {
                        v = (byte)((v << 1) | ((bm.GetPixel(x, ry * 8 + sy).G < 128) ? 1 : 0));
                    }
                    ret[4 + (3 - ry) * 128 + x] = v;
                }
            }
            return ret;
        }
    }
}
