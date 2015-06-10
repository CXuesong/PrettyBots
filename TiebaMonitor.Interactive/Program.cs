using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiebaMonitor.Kernel;
using System.Net;
using System.IO;
using TiebaMonitor.Kernel.Strategies;
using System.Threading;

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
                            "C", "知道-贴吧重定向问题检查",
                        "E", "退出"))
                    {
                        case "L":
                            AccountManagementRoutine();
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

        private static void ZhidaoRedirectionCheckRoutine(string fn = null)
        {
            UI.Print();
            var isAuto = fn != null;
            if (fn == null)
                fn = UI.Input("键入贴吧名称", "mark5ds");
            if (string.IsNullOrWhiteSpace(fn)) return;
            var checker = new TiebaZhidaoRedirectionDetector(visitor);
            /*
             容易误判的内容
             * RT~~
             * 跪求解答T_T
             * 
             */
            checker.RedirectionMatcher = new[]
            {
                "不解释 ！！！！答的上的乃神人！",
                "急急急 谢谢了",
                "哪位大大知道呀，小弟在此感激不尽",
                "谢谢吧里各位大爷，爱你们~",
                "麻烦知道的说下～我在此先谢过",
                "哪位高手如果知道是请告诉我一下，谢谢！",
                "红旗镇楼跪求解答",
                "求好心人解答~",
                "本吧好心人解答一下吧~~~"
            };
            var suspectedTopics = checker.CheckForum(fn).ToList();
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
                            var hasReplied = t.Posts().Any(p =>
                            {
                                if (p.AuthorName == visitor.AccountInfo.UserName)
                                {
                                    if (string.IsNullOrWhiteSpace(p.Content)) return false;
                                    if (p.Content.IndexOf("Checked", StringComparison.Ordinal) >= 0)
                                        return true;
                                }
                                return false;
                            });
                            if (!hasReplied)
                            {
                                UI.Print("回复：\n{0}", t);
                                var timeLapse = DateTime.Now - lastReplyTime;
                                if (timeLapse.TotalSeconds < 2)
                                    Thread.Sleep((int)(3000 - timeLapse.TotalMilliseconds));
                                t.Reply(
                                    BaiduUtility.TiebaEscape(string.Format(
                                        "这是从百度知道上面转发过来的问题吗……\n我只是说有这种可能。\n\nChecked @ {0:F}",
                                        DateTime.Now)));
                            }
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
