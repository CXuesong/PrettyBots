using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TiebaMonitor.Interactive
{
    public static class ExtBitmap
    {
        public static string ASCIIFilter(this Bitmap sourceBitmap, int pixelBlockSize,
                                                                   int colorCount = 0)
        {
            var sourceData = sourceBitmap.LockBits(new Rectangle(0, 0,
                                    sourceBitmap.Width, sourceBitmap.Height),
                                                      ImageLockMode.ReadOnly,
                                                PixelFormat.Format32bppArgb);

            var pixelBuffer = new byte[sourceData.Stride * sourceData.Height];

            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);
            sourceBitmap.UnlockBits(sourceData);

            var asciiArt = new StringBuilder();

            var avgBlue = 0;
            var avgGreen = 0;
            var avgRed = 0;
            var offset = 0;

            var rows = sourceBitmap.Height / pixelBlockSize;
            var columns = sourceBitmap.Width / pixelBlockSize;

            if (colorCount > 0)
            {
                colorCharacters = GenerateRandomString(colorCount);
            }

            for (var y = 0; y < rows; y++)
            {
                for (var x = 0; x < columns; x++)
                {
                    avgBlue = 0;
                    avgGreen = 0;
                    avgRed = 0;

                    for (var pY = 0; pY < pixelBlockSize; pY++)
                    {
                        for (var pX = 0; pX < pixelBlockSize; pX++)
                        {
                            offset = y * pixelBlockSize * sourceData.Stride +
                                     x * pixelBlockSize * 4;

                            offset += pY * sourceData.Stride;
                            offset += pX * 4;

                            avgBlue += pixelBuffer[offset];
                            avgGreen += pixelBuffer[offset + 1];
                            avgRed += pixelBuffer[offset + 2];
                        }
                    }

                    avgBlue = avgBlue / (pixelBlockSize * pixelBlockSize);
                    avgGreen = avgGreen / (pixelBlockSize * pixelBlockSize);
                    avgRed = avgRed / (pixelBlockSize * pixelBlockSize);

                    asciiArt.Append(GetColorCharacter(avgBlue, avgGreen, avgRed));
                }

                asciiArt.Append("\r\n");
            }

            return asciiArt.ToString();
        }

        private static string GenerateRandomString(int maxSize)
        {
            var stringBuilder = new StringBuilder(maxSize);
            var randomChar = new Random();

            char charValue;

            for (var k = 0; k < maxSize; k++)
            {
                charValue = (char)(Math.Floor(255 * randomChar.NextDouble() * 4));

                if (stringBuilder.ToString().IndexOf(charValue) != -1)
                {
                    charValue = (char)(Math.Floor((byte)charValue *
                                            randomChar.NextDouble()));
                }

                if (Char.IsControl(charValue) == false &&
                    Char.IsPunctuation(charValue) == false &&
                    stringBuilder.ToString().IndexOf(charValue) == -1)
                {
                    stringBuilder.Append(charValue);

                    randomChar = new Random((int)((byte)charValue *
                                     (k + 1) + DateTime.Now.Ticks));
                }
                else
                {
                    randomChar = new Random((int)((byte)charValue *
                                     (k + 1) + DateTime.UtcNow.Ticks));
                    k -= 1;
                }
            }

            return stringBuilder.ToString().RandomStringSort();
        }

        public static string RandomStringSort(this string stringValue)
        {
            var charArray = stringValue.ToCharArray();

            var randomIndex = new Random((byte)charArray[0]);
            var iterator = charArray.Length;

            while (iterator > 1)
            {
                iterator -= 1;

                var nextIndex = randomIndex.Next(iterator + 1);

                var nextValue = charArray[nextIndex];
                charArray[nextIndex] = charArray[iterator];
                charArray[iterator] = nextValue;
            }

            return new string(charArray);
        }

        private static string colorCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private static string GetColorCharacter(int blue, int green, int red)
        {
            var colorChar = String.Empty;
            var intensity = (blue + green + red) / 3 *
                            (colorCharacters.Length - 1) / 255;

            colorChar = colorCharacters.Substring(intensity, 1).ToUpper();
            colorChar += colorChar.ToLower();

            return colorChar;
        }

        public static Bitmap TextToImage(this string text, Font font,
                                                        float factor)
        {
            var textBitmap = new Bitmap(1, 1);

            var graphics = Graphics.FromImage(textBitmap);

            var width = (int)Math.Ceiling(
                        graphics.MeasureString(text, font).Width *
                        factor);

            var height = (int)Math.Ceiling(
                         graphics.MeasureString(text, font).Height *
                         factor);

            graphics.Dispose();

            textBitmap = new Bitmap(width, height,
                                    PixelFormat.Format32bppArgb);

            graphics = Graphics.FromImage(textBitmap);
            graphics.Clear(Color.Black);

            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            graphics.ScaleTransform(factor, factor);
            graphics.DrawString(text, font, Brushes.White, new PointF(0, 0));

            graphics.Flush();
            graphics.Dispose();

            return textBitmap;
        }

        public static Bitmap ScaleBitmap(this Bitmap sourceBitmap, float factor)
        {
            var resultBitmap = new Bitmap((int)(sourceBitmap.Width * factor),
                                             (int)(sourceBitmap.Height * factor),
                                             PixelFormat.Format32bppArgb);

            var graphics = Graphics.FromImage(resultBitmap);

            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            graphics.DrawImage(sourceBitmap,
                new Rectangle(0, 0, resultBitmap.Width, resultBitmap.Height),
                new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height),
                                                         GraphicsUnit.Pixel);

            return resultBitmap;
        }
    }  
}
