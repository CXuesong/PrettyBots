using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;

namespace TiebaMonitor.Kernel
{
    public static class BaiduUtility
    {
        public static string TiebaEscape(string sourceText)
        {
            var builder = new StringBuilder(HttpUtility.HtmlEncode(sourceText));
            //ç»§ç»­æµè¯[lbk]ceshi[rbk]ã123[emotion+pic_type=1+width=30+height=30]http://tb2.bdstatic.com/tb/editor/images/face/i_f01.png?t=20140803[/emotion]123
            builder.Replace("[", "[lbk]");
            builder.Replace("]", "[rbk]");
            builder.Replace("\n", "[br]");
            return builder.ToString();
        }
    }
}
