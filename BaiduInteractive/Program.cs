using System;
using System.Drawing;
using System.Net;
using TiebaMonitor.Kernel;

namespace BaiduInterop.Interactive
{
    class Program
    {
        private static BaiduVisitor visitor;

        static int Main(string[] args)
        {
            UI.Init();
            visitor = new BaiduVisitor();
            visitor.Session.RequestingVerificationCode += session_RequestVerificationCode;
            AccountManagementRoutine();
            while (true)
            {
                try
                {
                    while (true)
                    {
                        switch (UI.Input("操作", "S",
                                "A", "账户管理",
                                "S", "百度搜索",
                                "P", "百度贴吧",
                            "E", "退出"))
                        {
                            case "A":
                                AccountManagementRoutine();
                                break;
                            case "S":
                                break;
                            case "P":
                                BaiduTiebaRoutine();
                                break;
                            case "E":
                                return 0;
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UI.PrintError(ex);
                    continue;
                }
            }
            return 0;
        }

        static void session_RequestVerificationCode(object sender, RequestingVerificationCodeEventArgs e)
        {
            using (var client = new WebClient())
            {
                using (var s = client.OpenRead(e.ImageUrl))
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    using (var bmp = new Bitmap(s))
                    {
                        UI.Print(bmp.ASCIIFilter(2, 5));
                    }
                }
            }
            var vc = UI.Input("键入验证码");
            if (!string.IsNullOrEmpty(vc)) e.VerificationCode = vc;
        }

        static void LoginRoutine()
        {
        BEGIN:
            UI.Print("=== 登录到：baidu.com ===");
            var userName = UI.Input("用户名");
            var password = UI.InputPassword("密码");
            try
            {
                if (!visitor.Login(userName, password)) goto BEGIN;
            }
            catch (Exception ex)
            {
                UI.PrintError(ex);
                goto BEGIN;
            }
            UI.Print("已作为 {0} 登录 baidu.com 。", visitor.AccountInfo.UserName);
        }

        static void AccountManagementRoutine()
        {
            if (visitor.AccountInfo.IsLoggedIn)
                UI.Print("您已经登录。\n用户名：{0}", visitor.AccountInfo.UserName);
            else
                UI.Print("您尚未登录。");
            while (true)
            {
                switch (UI.Input(Prompts.SelectAnOperation, "B",
                    "L", visitor.AccountInfo.IsLoggedIn ? "注销" : "登录",
                    "B", "返回"))
                {
                    case "L":
                        if (visitor.AccountInfo.IsLoggedIn)
                        {
                            visitor.Logout();
                            if (UI.Confirm("确实要注销吗？"))
                            {
                                visitor.Logout();
                                UI.Print("已注销。");
                            }
                        }
                        else
                        {
                            LoginRoutine();
                        }
                        break;
                    case "B":
                        return;
                }
            }
        }

        static void BaiduTiebaRoutine()
        {
        INPUT_FN:
            UI.Print();
            var fn = UI.Input(Prompts.InputForumName, "化学");
            if (string.IsNullOrEmpty(fn)) return;
            var forum = visitor.TiebaVisitor.Forum(fn);
            if (!forum.IsExists)
            {
                UI.Print("贴吧不存在。");
                goto INPUT_FN;
            }
            if (forum.IsRedirected) UI.Print("重定向至：{0}。", forum.Name);
            UI.Print("Id：{0}\n“{1}”数：{2}\n主题数：{3}\n帖子数：{4}",
                forum.Id, forum.MemberName, forum.MembersCount,
                forum.TopicsCount, forum.PostsCount);
            while (true)
            {
                switch (UI.Input(Prompts.SelectAnOperation, "L",
                    "L", Prompts.ListTopics,
                    "B", Prompts.Back))
                {
                    case "L":
                        var topics = forum.Topics();
                        foreach (var t in topics)
                        {
                            var marks = "";
                            if (t.IsTop) marks += "^";
                            if (t.IsGood) marks += "*";
                            UI.Print("[{0,2}][{1,4}] {2}\n          by {3}\tRe by {4} @ {5}",
                                marks, t.RepliesCount, t.Title,
                                t.AuthorName, t.LastReplyer, t.LastReplyTime);
                        }
                        break;
                    case "B":
                        goto INPUT_FN;
                }
            }
        }
    }
}
