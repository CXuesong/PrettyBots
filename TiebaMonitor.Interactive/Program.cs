using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Threading;
using PrettyBots.Monitor;
using PrettyBots.Monitor.Baidu;
using PrettyBots.Monitor.Strategies;

namespace TiebaMonitor.Interactive
{
    class Program
    {
        private static BaiduVisitor visitor;

        static int Main(string[] args)
        {
            UI.Init();
            visitor = new BaiduVisitor();
            var isFirstLoop = true;
            if (args.Length == 2)
            {
                //Auto Mode
                // app.exe cookieDir forumName
                try
                {
                    AccountManagementRoutine(args[0]);
                    if (!visitor.AccountInfo.IsLoggedIn) return 1;
                    ZhidaoRedirectionCheckRoutine(args[1]);
                    return 0;
                }
                catch (Exception ex)
                {
                    UI.PrintError(ex);
                    return 255;
                }
            }
            while (true)
            {
                try
                {
                    if (isFirstLoop) AccountManagementRoutine();
                    isFirstLoop = false;
                    switch (UI.Input("操作", "C",
                            "L", "载入Cookies",
                            "P", "前缀检查",
                            "C", "知道-贴吧重定向问题检查",
                        "E", "退出"))
                    {
                        case "L":
                            AccountManagementRoutine();
                            break;
                        case "P":
                            PrefixCheckRoutine();
                            break;
                        case "C":
                            ZhidaoRedirectionCheckRoutine();
                            break;
                        case "E":
                            return 0;
                    }
                }
                catch (Exception ex)
                {
                    UI.PrintError(ex);
                }
            }
        }

        private static void AccountManagementRoutine(string cookiePath = null)
        {
            UI.Print();
            if (cookiePath == null) cookiePath = UI.InputFile("../../../BaiduInteractive/bin/Debug/BDICookies.bin");
            using (var fs = File.OpenRead(cookiePath))
                visitor.Session.LoadCookies(fs);
            visitor.AccountInfo.Update();
            if (visitor.AccountInfo.IsLoggedIn)
                UI.Print("您已经作为 {0} 登录至 baidu.com 。", visitor.AccountInfo.UserName);
            else
                UI.Print("您尚未登录。");
        }

        private static void PrefixCheckRoutine(string fn = null)
        {
            UI.Print();
            var isAuto = fn != null;
            if (fn == null)
                fn = UI.Input("键入贴吧名称", "mark5ds");
            if (string.IsNullOrWhiteSpace(fn)) return;
            var f = visitor.Tieba.Forum(fn);
            if (!f.IsExists) return;
            var suspectedTopics = f.Topics().Take(20).Where(t => !f.IsTopicPrefixMatch(t.Title)).ToList();
            if (suspectedTopics.Count > 0)
            {
                foreach (var t in suspectedTopics)
                    UI.Print(t);
            }
        }

        private static void ZhidaoRedirectionCheckRoutine(string fn = null)
        {
            UI.Print();
            var isAuto = fn != null;
            if (fn == null)
                fn = UI.Input("键入贴吧名称", "mark5ds");
            if (string.IsNullOrWhiteSpace(fn)) return;
            var f = visitor.Tieba.Forum(fn);
            if (!f.IsExists) return;
            var checker = new TiebaZhidaoRedirectionDetector(visitor);
            var rnd = new Random();
            /*
             容易误判的内容
             * RT~~
             * 跪求解答T_T
             * 
             */
            checker.StrongMatcher = new[]
            {
                "不解释 ！！！！答的上的乃神人！",
                "急急急 谢谢了",
                "哪位大大知道呀，小弟在此感激不尽",
                "谢谢吧里各位大爷，爱你们~",
                "麻烦知道的说下～我在此先谢过",
                "哪位高手如果知道是请告诉我一下，谢谢！",
                "红旗镇楼跪求解答",
                "求好心人解答~",
                "本吧好心人解答一下吧~~~",
                "求大神指导,好心人帮助"
            };
            checker.WeakMatcher = new[]
            {
                "是不是",
                "有没有",
                "怎么",
                "什么",
                "？"
            };
            var suspectedTopics = checker.CheckForum(fn, 15).Where(t =>
            {
                if (t.RepliesCount > 20) return false;
                //hasReplied
                if (t.Posts().Any(p =>
                {
                    if (p.AuthorName == visitor.AccountInfo.UserName)
                    {
                        if (string.IsNullOrWhiteSpace(p.Content)) return false;
                        if (p.Content.IndexOf("__checked__", StringComparison.Ordinal) >= 0)
                            return true;
                    }
                    return false;
                })) return false;
                return true;
            }).ToList();
            if (suspectedTopics.Count > 0)
            {
                foreach (var t in suspectedTopics) UI.Print(t);
                if (isAuto || UI.Confirm("是否回复？"))
                {
                    var isFirstReply = true;
                    var lastReplyTime = DateTime.MinValue;
                    foreach (var t in suspectedTopics)
                    {
                        try
                        {
                            UI.Print("回复：\n{0}", t);
                            var timeLapse = DateTime.Now - lastReplyTime;
                            if (timeLapse.TotalSeconds < 2)
                                Thread.Sleep((int)(2000 + rnd.Next(1000) - timeLapse.TotalMilliseconds));
                            string content = null;
                            if (f.TopicPrefix.Count > 0)
                            {
                                content = string.Format(Prompts.TiebaZhidaoRedirectionReply_HasPrefix,
                                    f.Name, f.GetTopicPrefix(rnd.Next(f.TopicPrefix.Count)));
                            }
                            else
                            {
                                content = string.Format(Prompts.TiebaZhidaoRedirectionReply, f.Name);
                            }
                            content += string.Format("\n\n__checked__ @ {0:F}", DateTime.Now);
                            t.Reply(BaiduUtility.TiebaEscape(content));
                        }
                        catch (Exception ex)
                        {
                            UI.PrintError(ex);
                        }
                        lastReplyTime = DateTime.Now;
                    }
                }
            }
        }
    }
}
