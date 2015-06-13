using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using PrettyBots.Visitors;
using PrettyBots.Visitors.Baidu;
using PrettyBots.Visitors.Baidu.Tieba;

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
            var isFirstLoop = true;
            while (true)
            {
                try
                {
                    if (isFirstLoop) AccountManagementRoutine();
                    isFirstLoop = false;
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
            using (var client = new WebClient()) data = client.DownloadData(e.ImageUrl);
            using (var s = new MemoryStream(data, false))
            using (var bmp = new Bitmap(s))
                UI.Print(AsciiArtGenerator.ConvertToAscii(bmp, Console.WindowWidth - 2));
            var vc = UI.Input("键入验证码");
            if (!string.IsNullOrEmpty(vc)) e.VerificationCode = vc;
        }

        static void LoginRoutine()
        {
        BEGIN:
            UI.Print("=== 登录到：baidu.com ===");
            UI.Print("要取消，请直接按下回车键。");
            var userName = UI.Input("用户名");
            if (string.IsNullOrWhiteSpace(userName)) return;
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
                UI.Print(visitor.AccountInfo);
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
            var fn = UI.Input(Prompts.InputForumName, "mark5ds");
            if (string.IsNullOrWhiteSpace(fn)) return;
            var forum = UI.PromptWait(() => visitor.Tieba.Forum(fn));
            if (!forum.IsExists)
            {
                UI.Print(string.IsNullOrEmpty(forum.QueryResult) ? "贴吧不存在。" : forum.QueryResult);
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
                        var viewer = new EnumerableViewer<TopicVisitor>(
                            forum.Topics(), ForumViewer_OnViewItem, ForumViewer_OnItemSelected);
                        viewer.Show();
                        break;
                    case "B":
                        goto INPUT_FN;
                }
            }
        }

        private static void ForumViewer_OnViewItem(int index, TopicVisitor t)
        {
            var marks = "";
            if (t.IsTop) marks += "^";
            if (t.IsGood) marks += "*";
            UI.Write("{0}\t", index);
            UI.Print("{0,2} [{1,4}] {2}\n          by {3}\tRe by {4} @ {5}",
                marks, t.RepliesCount, t.Title,
                t.AuthorName, t.LastReplyer, t.LastReplyTime);
        }

        private static void ForumViewer_OnItemSelected(TopicVisitor t)
        {
            var marks = "";
            if (t.IsTop) marks += "^";
            if (t.IsGood) marks += "*";
            while (true)
            {
                UI.Print("[{0}] {1}\n[By {2}][Re {3}]", marks, t.Title, t.AuthorName, t.RepliesCount);
                UI.Print(t.PreviewText);
                try
                {
                    switch (UI.Input(Prompts.SelectAnOperation, "L",
                        "L", "查看帖子",
                        "R", "回复",
                        "B", Prompts.Back))
                    {
                        case "L":
                            var viewer = new EnumerableViewer<PostVisitor>(t.Posts(),
                                TopicViewer_OnViewItem, TopicViewer_OnItemSelected);
                            viewer.Show();
                            break;
                        case "R":
                            var content = UI.InputMultiline(Prompts.InputReplyContent);
                            if (!string.IsNullOrWhiteSpace(content))
                                t.Reply(BaiduUtility.TiebaEscape(content));
                            break;
                        case "B":
                            return;
                    }
                }
                catch (System.Exception ex)
                {
                    UI.PrintError(ex);
                }

            }
        }

        private static void TopicViewer_OnViewItem(int index, PostVisitor p)
        {
            UI.Print("{0}\t{1}楼\tBy [{2}]\t@{3}\tRe {4}", index, p.Floor, p.Author, p.SubmissionTime, p.CommentsCount);
            //UI.PrintToMargin("    " + Utility.PrettyParseHtml(p.Content, true));
            UI.Print(Utility.PrettyParseHtml(p.Content, PrettyParseHtmlOptions.DefaultCompact));
            UI.Print();
        }

        private static void TopicViewer_OnItemSelected(PostVisitor p)
        {
            UI.Print("{0}F\tBy [{1}]\t@{2}\tRe {3}", p.Floor, p.Author, p.SubmissionTime, p.CommentsCount);
            using (var client = new WebClient())
            {
                var pphOptions = new PrettyParseHtmlOptions(false, true, client, Console.WindowWidth - 2);
                UI.Print(Utility.PrettyParseHtml(p.Content, pphOptions));
                UI.Print();
                while (true)
                {
                    try
                    {
                        switch (UI.Input(Prompts.SelectAnOperation, "L",
                            "L", "查看楼中楼[" + p.CommentsCount + "]",
                            "R", "回复",
                            "B", Prompts.Back))
                        {
                            case "L":
                                var viewer = new EnumerableViewer<PostComment>(p.Comments(),
                                    (index, c) =>
                                    {
                                        UI.Print("{0}\tBy {1}\t@ {2}", index, c.AuthorName, c.SubmissionTime);
                                        UI.Print(Utility.PrettyParseHtml(c.Content, pphOptions));
                                    }, null);
                                viewer.Show();
                                break;
                            case "R":
                                var content = UI.InputMultiline(Prompts.InputReplyContent);
                                if (!string.IsNullOrWhiteSpace(content))
                                    p.Reply(BaiduUtility.TiebaEscape(content));
                                break;
                            case "B":
                                return;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        UI.PrintError(ex);
                    }
                }
            }
        }
    }
}
