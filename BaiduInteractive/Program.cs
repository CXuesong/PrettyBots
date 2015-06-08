using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using TiebaMonitor.Kernel;
using TiebaMonitor.Kernel.Tieba;

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
            byte[] data;
            using (var client = new WebClient())
                data = client.DownloadData(e.ImageUrl);
            using (var s = new MemoryStream(data, false))
            using (var bmp = new Bitmap(s))
            {
                UI.Print(AsciiArtGenerator.ConvertToAscii(bmp, Console.WindowWidth - 2));
            }
            var vc = UI.Input("键入验证码");
            if (!string.IsNullOrEmpty(vc)) e.VerificationCode = vc;
        }

        static void LoginRoutine()
        {
        BEGIN:
            UI.Print("=== 登录到：baidu.com ===");
            UI.Print("要取消，请直接按下回车键。");
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
        }

        static void AccountManagementRoutine()
        {
            SHOW_ACCOUNT:
            UI.Print();
            if (visitor.AccountInfo.IsLoggedIn)
                UI.Print("您已经作为 {0} 登录至 baidu.com 。", visitor.AccountInfo.UserName);
            else
                UI.Print("您尚未登录。");
            while (true)
            {
                switch (UI.Input(Prompts.SelectAnOperation, "B",
                    "L", visitor.AccountInfo.IsLoggedIn ? "注销" : "登录",
                    "SS", "保存会话",
                    "LS", "载入会话",
                    "B", "返回"))
                {
                    case "L":
                        if (visitor.AccountInfo.IsLoggedIn)
                        {
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
                        goto SHOW_ACCOUNT;
                    case "SS":
                        using (var fs = File.OpenWrite(UI.InputFile("BDICookies.bin")))
                        {
                            visitor.Session.SaveCookies(fs);
                        }
                        break;
                    case "LS":
                        using (var fs = File.OpenRead(UI.InputFile("BDICookies.bin")))
                        {
                            visitor.Session.LoadCookies(fs);
                        }
                        visitor.AccountInfo.Update();
                        goto SHOW_ACCOUNT;
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
            if (string.IsNullOrWhiteSpace(fn)) return;
            var forum = UI.PromptWait(() => visitor.TiebaVisitor.Forum(fn));
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
                        var topics = forum.Topics().ToList();
                        int i = 0;
                        foreach (var t in topics)
                        {
                            PrintTopic(i, t);
                            i++;
                        }
                        while (true)
                        {
                            var sel = UI.Input<int>("键入编号以查看帖子", "取消");
                            if (sel == null) break;
                            if (sel < 0 || sel >= topics.Count)
                            {
                                UI.Print(Prompts.NumberOverflow);
                                continue;
                            }
                            TiebaTopicRoutine(topics[sel.Value]);
                        }
                        break;
                    case "B":
                        goto INPUT_FN;
                }
            }
        }

        private static void PrintTopic(int index, TopicVisitor t)
        {
            var marks = "";
            if (t.IsTop) marks += "^";
            if (t.IsGood) marks += "*";
            UI.Write("{0,2} ", index);
            UI.Print("[{0,2}][{1,4}] {2}\n          by {3}\tRe by {4} @ {5}",
                marks, t.RepliesCount, t.Title,
                t.AuthorName, t.LastReplyer, t.LastReplyTime);
        }

        private static void TiebaTopicRoutine(TopicVisitor t)
        {
            var marks = "";
            if (t.IsTop) marks += "^";
            if (t.IsGood) marks += "*";
            UI.Print(t.Title);
            UI.Print("[{0}] {1} [By {2}][Re {3}]", marks, t.Title, t.AuthorName, t.RepliesCount);
            foreach (var p in t.Posts())
            {
                UI.Print("#{0, 4} [By {1}]", p.Floor, p.AuthorName);
                UI.Print("    " + Utility.StringEllipsis(p.Content, 20));
            }
        }
    }
}
