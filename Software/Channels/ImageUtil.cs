using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Software.Channels
{
    class ImageUtil
    {

        public static byte[] RenderPlainText(String s)
        {
            if (s.StartsWith("`"))
            {
                return RenderSevenSegment(s.Substring(1));
            }
            Bitmap bm = new Bitmap(128, 32, PixelFormat.Format32bppRgb);
            using (Graphics g = Graphics.FromImage(bm))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                g.Clear(Color.White);


                Font f = new Font("Arial", 24, FontStyle.Regular, GraphicsUnit.Pixel);

                //TextRenderer.DrawText(g, s, f, new Rectangle(0, 0, 128, 32), Color.Black, TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                g.DrawString(s, f, Brushes.Black, new RectangleF(-2, 0, 140, 32));
            }

            return ToBytes(bm);
        }
        public static byte[] RenderSevenSegment(String s)
        {
            Bitmap font = Software.Properties.Resources.Font7Seg;

            byte[] ret = new byte[512];
            int px = 0;

            foreach (char c in s)
            {
                int fx = 32 * (c % 16);
                int fy = 32 * (c / 16);
                for (int x = 0; x < 32; x++)
                {
                    if (font.GetPixel(fx + x, fy + 31).B > 128)
                    {
                        // End of Glyph
                        break;
                    }

                    for (int ry = 0; ry < 4; ry++)
                    {
                        byte v = 0;
                        for (int sy = 0; sy < 8; sy++)
                        {
                            v = (byte)((v << 1) | ((font.GetPixel(fx + x, fy + ry * 8 + sy).G < 128) ? 1 : 0));
                        }
                        ret[(3 - ry) * 128 + px] = v;
                    }

                    px++;
                    if (px >= 128)
                    {
                        return ret;
                    }
                }
            }
            return ret;
        }
        
        public static byte[] ToBytes(Bitmap bm)
        {
            byte[] ret = new byte[512];
            for (int ry = 0; ry < 4; ry++)
            {
                for (int x = 0; x < 128; x++)
                {
                    byte v = 0;
                    for (int sy = 0; sy < 8; sy++)
                    {
                        v = (byte)((v << 1) | ((bm.GetPixel(x, ry * 8 + sy).G < 128) ? 1 : 0));
                    }
                    ret[(3 - ry) * 128 + x] = v;
                }
            }
            return ret;
        }
    }
}
