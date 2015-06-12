using System.Text;
using System.Web;

namespace PrettyBots.Monitor.Baidu
{
    public static class BaiduUtility
    {
        public static string TiebaEscape(string sourceText)
        {
            if (string.IsNullOrEmpty(sourceText)) return string.Empty;
            sourceText = HttpUtility.HtmlEncode(sourceText);
            var builder = new StringBuilder();
            //ç»§ç»­æµè¯[lbk]ceshi[rbk]ã123[emotion+pic_type=1+width=30+height=30]http://tb2.bdstatic.com/tb/editor/images/face/i_f01.png?t=20140803[/emotion]123
            foreach (var c in sourceText)
            {
                switch (c)
                {
                    case '[':
                        builder.Append("[lbk]");
                        break;
                    case ']':
                        builder.Append("[rbk]");
                        break;
                    case '\n':
                        builder.Append("[br]");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }
            return builder.ToString();
        }
    }
}
