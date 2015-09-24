using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using PrettyBots.Visitors;
using PrettyBots.Visitors.Baidu;
using PrettyBots.Visitors.Baidu.Tieba;
using PrettyBots.Visitors.WeatherService;

namespace PrettyBots.Strategies.Baidu.Tieba
{
    public class NotificationChecker : Strategy
    {
        public const string JoinInPostContentKey = "NotificationChecker.JoinIn";
        public const string JoinInFailedPostContentKey = "NotificationChecker.JoinInFailed";
        public const string DefaultReplyPostContentKey = "NotificationChecker.DefaultReply";
        public const string GenericPostContentKey = "NotificationChecker.Generic";

        public NotificationChecker(Session session)
            : base(session)
        {
            RegisterSimpleAction("查?(一下)?我?(今天|最近|现在)?的?发帖量", p =>
            {
                const int maxPosts = 100;
                var s = p.Root.Tieba.Search(userName: p.Author.Name);
                var today = DateTime.Today;
                var counter = 0;
                var totalCounter = 0;
                var content = "我大概查了一下，你今天的发帖量";
                foreach (var r in s.Result.EnumerateToEnd())
                {
                    if (totalCounter >= maxPosts) break;
                    if (r.SubmissionTime.Date == today)
                        counter++;
                    else
                        break;
                    totalCounter++;
                }
                if (counter == 0)
                    content += "貌似是……零？";
                else if (totalCounter < maxPosts)
                    content += "达到了" + counter + "篇。";
                else
                    content += "超过了" + counter + "篇。";
                content += "还有，其实你可以试试 at 豌豆荚吧，问一下发帖量的。";
                GenericReply(p, string.Format(content, DateTime.Now));
            });
            RegisterSimpleAction("请?你?(报时|(查|报)?(一下)?时间)",
                p => GenericReply(p, string.Format("现在是：{0:G}。", DateTime.Now)));
            RegisterSimpleAction(@"(^|\s)(查一下)?(?<c1>[^\s]*?)(现在|今天|今儿|明天|这几天|近来)?\s*(?<c2>.*?)的?\s*天气(情况|状况)?\s*(如何|咋样|怎么?样)?",
                (p, m) =>
                {
                    var city = m.Groups["c1"].Value;
                    if (string.IsNullOrWhiteSpace(city)) city = m.Groups["c2"].Value;
                    if (string.IsNullOrWhiteSpace(city))
                    {
                        GenericReply(p, string.Format("你想知道哪里的天气？请把句子写完整，然后再试一次。"));
                        return;
                    }
                    var wr = new WeatherReportVisitor(WebSession);
                    try
                    {
                        var w = wr.GetWeather(city.Trim());
                        if (w == null)
                            GenericReply(p, string.Format("抱歉，目前还无法查询{0}的天气。", city));
                        else
                            GenericReply(p, BaiduUtility.TiebaEscape(w.ToString()));
                    }
                    catch (Exception ex) when (!(ex is NonhumanException))
                    {
                        GenericReply(p, BaiduUtility.TiebaEscape("在查询天气的时候遇到了一些问题。嘛不过你可以稍后再试一下的 ^^"));
                    }
                });
            RegisterSimpleAction("(问一下|请问)?你?(现在)?的?(状态|状况)(如何)?",
                p => GenericReply(p, string.Format("还好吧，目前关注了{0}个贴吧。",
                    p.Root.Tieba.FavoriteForums.Count)));
        }

        private bool JoinInForum(PostVisitorBase p)
        {
            if (!p.Forum.HasJoinedIn)
            {
                try
                {
                    p.Forum.JoinIn();
                    Logging.TraceInfo(this, "Joined {0}", p.Forum);
                    if (!p.Forum.HasSignedIn) p.Forum.SignIn();
                    var content = Session.TextComposer.ComposePost(JoinInPostContentKey, Prompts.ForumJoinedInContent, p);
                    p.Reply(content);
                    return true;
                }
                catch (NonhumanException ex)
                {
                    Logging.TraceWarning(this, ex.Message);
                }
                //catch (OperationFailedException ex)
                //{
                //    //未成功关注，那么也不需要发帖了。因为发不出去。
                //    var msg = string.Format("[{0}]{1}", ex.ErrorCode, ex.ErrorMessage);
                //    content = Session.TextComposer.ComposePost(JoinInFailedPostContentKey,
                //        string.Format(Prompts.ForumJoinedInContent, msg), p, msg);
                //}
            }
            return false;
        }

        private void GenericReply(PostVisitorBase p, string content)
        {
            p.Reply(Session.TextComposer.ComposePost(GenericPostContentKey, content, p));
        }

        private bool DefaultReply(PostVisitorBase p)
        {
            return p.Reply(Session.TextComposer.ComposePost(DefaultReplyPostContentKey, Prompts.TiebaDefaultReply, p));
        }

        // 处理简单的指令。
        private readonly List<Tuple<Regex, Action<PostVisitorBase, Match>>> SimpleActions =
            new List<Tuple<Regex, Action<PostVisitorBase, Match>>>();

        public void RegisterSimpleAction(string pattern, Action<PostVisitorBase, Match> action)
        {
            SimpleActions.Add(Tuple.Create(new Regex(pattern), action));
        }

        public void RegisterSimpleAction(string pattern, Action<PostVisitorBase> action)
        {
            SimpleActions.Add(Tuple.Create(new Regex(pattern),
                new Action<PostVisitorBase, Match>((p, a) => action(p))));
        }

        private bool HandleActions(PostVisitorBase p)
        {
            var pc = BaiduUtility.ParsePostContent(p.Content);
            var accountUser = p.Root.AccountInfo.UserName;
            //移除 @ 自己的节点。
            pc.Elements("at").Where(e => Utility.UserIdentity((string) e.Attribute("user"), accountUser)).Remove();
            //分析其余内容。
            var normalized = Utility.NormalizeString(pc.Value, true);
            //移除“回复”二字
            if (normalized.Length > 2 && normalized.Substring(0, 2) == "回复")
                normalized = normalized.Substring(2).TrimStart();
            foreach (var act in SimpleActions)
            {
                var result = act.Item1.Match(normalized);
                if (result.Success)
                {
                    act.Item2(p, result);
                    return true;
                }
            }
            //分析失败
            var ats = pc.Elements("at").ToArray();
            if (ats.Length > 0)
            {
                GenericReply(p, string.Format("你好像 at 了不止一个（{0}个）人……然后我就不知道该干什么了。", ats.Length + 1));
                return true;
            }
            return false;
        }

        // false : 无需继续处理当前回复。
        private bool HandleMagic(PostVisitorBase p)
        {
            var normalized = Utility.NormalizeString(p.Content, true);
            if (normalized.Contains("__SUPRESS_REPLY")) return false;
            return true;
        }

        protected override void EntryPointCore()
        {
            var visitor = new BaiduVisitor(WebSession);
            var replies = visitor.Tieba.Messages.PeekReplications(!Session.WebSession.IsDryRun);
            foreach (var rep in replies)
            {
                var p = rep.GetPost(visitor);
                //帖子可能被删掉了。
                if (p == null) continue;
                if (p is SubPostVisitor)
                {
                    var pc = BaiduUtility.ParsePostContent(p.Content);
                    var text = pc.FirstNode as XText;
                    if (text != null && text.Value != null && text.Value.Trim() == "回复")
                    {
                        var at = text.NextNode as XElement;
                        // 不要回复没有回复自己的 LZL。
                        if (at != null && !Utility.UserIdentity((string) at.Attribute("user"),
                            visitor.AccountInfo.UserName))
                            continue;
                    }
                }
                Logging.TraceInfo(this, "Reply:{0}", p);
                try
                {
                    if (!HandleMagic(p)) continue;
                    var justJoined = JoinInForum(p);
                    if (HandleActions(p)) continue;
                    if (!justJoined) DefaultReply(p);
                }
                catch (Exception ex)
                {
                    //如果已经掉线，则扔出异常。
                    if (ex is WebException && !WebSession.CheckConnectivity()) throw;
                    //如果没有掉线，则记录异常。
                    Logging.Exception(this, ex);
                }
            }
        }
    }
}
