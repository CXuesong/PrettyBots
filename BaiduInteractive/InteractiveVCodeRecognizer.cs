using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PrettyBots.Visitors;

namespace BaiduInterop.Interactive
{
    class InteractiveVCodeRecognizer : VerificationCodeRecognizer
    {
        protected override string RecognizeFromUrl(string imageUrl, WebSession session)
        {
            byte[] data;
            using (var client = new WebClient()) data = client.DownloadData(imageUrl);
            using (var s = new MemoryStream(data, false))
            using (var bmp = new Bitmap(s))
                UI.Print(AsciiArtGenerator.ConvertToAscii(bmp, Console.WindowWidth - 2));
            var vc = UI.Input("键入验证码");
            if (string.IsNullOrEmpty(vc)) return null;
            return vc;
        }
    }
}
