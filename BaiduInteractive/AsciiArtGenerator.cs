using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaiduInterop.Interactive
{
    static class AsciiArtGenerator
    {
        private static string[] _AsciiChars = { "#", "#", "@", "%", "=", "+", "*", ":", "-", ".", " " };

        private static Bitmap ResizeImage(Image inputBitmap, int asciiWidth)
        {
            int asciiHeight = 0;
            //Calculate the new Height of the image from its width
            asciiHeight = (int)Math.Ceiling((double)inputBitmap.Height * asciiWidth / inputBitmap.Width);
            //Create a new Bitmap and define its resolution
            var result = new Bitmap(asciiWidth, asciiHeight);
            using (var g = Graphics.FromImage(result))
            {
                //The interpolation mode produces high quality images 
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(inputBitmap, 0, 0, asciiWidth, asciiHeight);
            }
            return result;
        }

        public static string ConvertToAscii(Bitmap image)
        {
            var toggle = false;
            var sb = new StringBuilder();
            for (int h = 0; h < image.Height; h++)
            {
                for (int w = 0; w < image.Width; w++)
                {
                    var pixelColor = image.GetPixel(w, h);
                    //Average out the RGB components to find the Gray Color
                    var red = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
                    var green = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
                    var blue = (pixelColor.R + pixelColor.G + pixelColor.B) / 3;
                    var grayColor = Color.FromArgb(red, green, blue);
                    //Use the toggle flag to minimize height-wise stretch
                    if (!toggle)
                    {
                        int index = (grayColor.R * 10) / 255;
                        sb.Append(_AsciiChars[index]);
                    }
                }
                if (!toggle)
                {
                    sb.Append("\n");
                    toggle = true;
                }
                else
                {
                    toggle = false;
                }
            }
            return sb.ToString();
        }

        public static string ConvertToAscii(Image image, int asciiWidth, bool noStretch = false)
        {
            if (noStretch && asciiWidth > image.Width) asciiWidth = image.Width;
            using (var resized = ResizeImage(image, asciiWidth))
                return ConvertToAscii(resized);
        }
    }
}
