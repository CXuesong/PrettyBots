using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using PrettyBots.Visitors;
using PrettyBots.Visitors.Baidu;
using PrettyBots.Visitors.Baidu.Tieba;

namespace PrettyBots.Strategies.Baidu.Tieba
{
    /// <summary>
    /// 用于科学地执行贴吧签到任务。
    /// </summary>
    public class BatchSignIn : Strategy
    {
        private static readonly XName XNForum = "forum";
        private static readonly XName XNId = "id";
        private static readonly XName XNTime = "time";

        public BatchSignIn(Session session)
            : base(session)
        {
            ForumCountLimit = 10;
        }

        /// <summary>
        /// 用于设置一次最多的签到贴吧数量。
        /// </summary>
        public int ForumCountLimit { get; set; }

        /// <summary>
        /// 获取上次签到（或检查签到）的时间。
        /// </summary>
        public DateTime? GetLastSignedInTime(long forumId)
        {
            var xf = Status.Elements(XNForum).FirstOrDefault(e => (long) e.Attribute(XNId) == forumId);
            if (xf == null) return null;
            return (DateTime?) xf.Attribute(XNTime);
        }

        /// <summary>
        /// 获取上次签到（或检查签到）的时间。
        /// </summary>
        public void SetLastSignedInTime(long forumId, DateTime time)
        {
            var xf = Status.Elements(XNForum).FirstOrDefault(e => (long)e.Attribute(XNId) == forumId);
            if (xf == null)
            {
                xf = new XElement(XNForum, new XAttribute(XNId, forumId));
                Status.Add(xf);
            }
            xf.SetAttributeValue(XNTime, time);
            //SubmitStatus();
        }

        protected override void EntryPointCore()
        {
            var visitor = new BaiduVisitor(WebSession);
            var rand = new Random();
            try
            {
                if (!visitor.Tieba.HasOneKeySignedIn && visitor.Tieba.OneKeySignedInSignificant)
                {
                    //先来一把一键签到。
                    try
                    {
                        visitor.Tieba.OneKeySignIn();
                        foreach (var f in visitor.Tieba.FavoriteForums)
                            if (f.HasSignedIn) SetLastSignedInTime(f.Id, DateTime.Now);
                    }
                    catch (OperationFailedException ex)
                    {
                        switch (ex.ErrorCode)
                        {
                            case 340009:    //time error
                                //现在的时间不宜签到。
                                Logging.TraceInfo(this, "现在不能一键签到。");
                                break;
                            default:
                                throw;
                        }
                    }
                }
                //现在从首页应该是看不到每个贴吧的签到状态了。杯具。
                Func<FavoriteForum, bool> needSignInCriterion = f =>
                {
                    var lt = GetLastSignedInTime(f.Id);
                    if (lt == null) return true;
                    return (DateTime.Today - lt.Value.Date).Days >= 1;
                };
                var destForums = visitor.Tieba.FavoriteForums
                    .Where(f => !f.HasSignedIn)
                    .Where(needSignInCriterion)
                    .ToArray();
                Utility.Shuffle(destForums);
                if (destForums.Length > ForumCountLimit)
                {
                    //限制一次签到贴吧的数量。
                    destForums = destForums.Take(ForumCountLimit*2)
                        .Where(f => rand.NextDouble() > 0.5)
                        .Take(ForumCountLimit)
                        .ToArray();
                }
                foreach (var ff in destForums)
                {
                    var forum = visitor.Tieba.Forum(ff.Name);
                    if (forum.HasSignedIn) continue;
                    try
                    {
                        if (forum.IsExists == false) continue;
                        if (forum.HasSignedIn)
                        {
                            //签到了，但是没记录。
                            SetLastSignedInTime(forum.Id, DateTime.Now);
                            continue;
                        }
                        forum.SignIn();
                        SetLastSignedInTime(forum.Id, DateTime.Now);
                    }
                    catch (OperationTooFrequentException ex)
                    {
                        Thread.Sleep(10000);
                        //本吧还是先跳过吧
                    }
                    catch (NonhumanException ex)
                    {
                        //需要验证码，那就躲开算了。
                        Logging.TraceInfo(this, "已触发验证码验证。");
                        return;
                    }
                }
            }
            finally
            {
                //将状态变化提交至数据库。
                SubmitStatus();
            }
        }
    }
}