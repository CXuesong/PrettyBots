using System.Text;
using System.Web;

namespace PrettyBots.Monitor.Baidu
{
    public static class BaiduUtility
    {
        public static string TiebaEscape(string sourceText)
        {
            var builder = new StringBuilder(HttpUtility.HtmlEncode(sourceText));
            //ç»§ç»­æµè¯[lbk]ceshi[rbk]ã123[emotion+pic_type=1+width=30+height=30]http://tb2.bdstatic.com/tb/editor/images/face/i_f01.png?t=20140803[/emotion]123
            builder.Replace("[", "[lbk\n");
            builder.Replace("]", "[rbk]");
            //小心不要出 bug ……
            builder.Replace("[lbk\n", "[lbk]");
            builder.Replace("\n", "[br]");
            //TODO 使用 StringBuilder 重写。
            return builder.ToString();
        }
    }
}
