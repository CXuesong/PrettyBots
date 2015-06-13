using System;
using HtmlAgilityPack;

namespace PrettyBots.Visitors.Baidu
{
    /// <summary>
    /// 表示当前登录账户的信息。
    /// </summary>
    public class BaiduAccountInfo : ChildVisitor<BaiduVisitor>
    {
        public bool IsLoggedIn { get; private set; }

        public long UserId { get; set; }

        public string UserName { get; private set; }

        /// <summary>
        /// 应该是用于 Url 包含的用户名。
        /// </summary>
        public string UserNameUrl { get; private set; }

        /// <summary>
        /// 用于 消息提醒 的参数，全站保持一致。
        /// </summary>
        public string Portrait { get; private set; }

        internal void CheckPortrait()
        {
            if (string.IsNullOrEmpty(Portrait)) 
                throw new InvalidOperationException(Prompts.Exception_PortraitIsNull);
        }

        public void Update()
        {
            //var doc = new HtmlDocument();
            string pageHtml;
            using (var s = Parent.Session.CreateWebClient())
                pageHtml = s.DownloadString("http://tieba.baidu.com/");
            var userInfo = Utility.FindJsonAssignment(pageHtml, "PageData.user");
            /*
             {
    "id": "1733233632",
    "user_id": "1733233632",
    "name": "La_Mobile",
    "user_name": "La_Mobile",
    "name_url": "La_Mobile",
    "no_un": 0,
    "is_login": 1,
    "portrait": "e00b4c615f4d6f62696c654f67",
    "balv": {
        
    },
    /*Ban这个模块真够讨厌的-/
    "Parr_props": null,
    "Parr_scores": null,
    "mParr_props": null,
    "power": {
        
    }
}
             */
            IsLoggedIn = (int) userInfo["is_login"] != 0;
            if (IsLoggedIn)
            {
                UserId = (long)userInfo["user_id"];
                UserName = (string) userInfo["user_name"];
                UserNameUrl = (string) userInfo["name_url"];
                Portrait = (string) userInfo["portrait"];
            }
            else
            {
                UserName = UserNameUrl = Portrait = null;
            }
        }

        public override string ToString()
        {
            if (IsLoggedIn)
                return "已登录：[" + UserId + "]" + UserName + ", portrait:" + Portrait;
            else
                return "未登录";
        }

        internal BaiduAccountInfo(BaiduVisitor parent)
            : base(parent)
        {
            IsLoggedIn = false;
            UserName = null;
        }
    }
}
