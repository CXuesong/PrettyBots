using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using HtmlAgilityPack;

namespace TiebaMonitor.Kernel
{
    /// <summary>
    /// 表示当前登录账户的信息。
    /// </summary>
    public class BaiduAccountInfo : BaiduChildVisitor
    {
        public bool IsLoggedIn { get; private set; }

        public string UserName { get; private set; }

        public void Update()
        {
            string passportPage;
            using (var s = Parent.Session.CreateWebClient())
                passportPage = s.DownloadString("https://passport.baidu.com/");
            var doc = new HtmlDocument();
            doc.LoadHtml(passportPage);
            var node = doc.DocumentNode.SelectSingleNode("/html/head/title");
            if (node == null) throw new UnexpectedDataException();
            if (node.InnerText.Contains("登录"))
            {
                //尚未登录。
                IsLoggedIn = false;
                UserName = null;
                return;
            }
            IsLoggedIn = true;
            node = doc.GetElementbyId("displayUsername");
            if (node == null) throw new UnexpectedDataException();
            UserName = node.InnerText;
        }

        internal BaiduAccountInfo(BaiduVisitor parent)
            : base(parent)
        {
            IsLoggedIn = false;
            UserName = null;
        }
    }
}
