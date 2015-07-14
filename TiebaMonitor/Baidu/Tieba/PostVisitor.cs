using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PrettyBots.Visitors.Baidu.Tieba
{
    public class PostVisitor : PostVisitorBase
    {
        // pn >= 2
        public const string CommentUrlFormat = "http://tieba.baidu.com/p/comment?t={0}&tid={1}&pid={2}&pn={3}";

        private SubPostListView _SubPosts;

        public override TopicVisitor Topic {
            get { return Parent.Parent; }
        }

        public new PostListView Parent
        {
            get { return (PostListView)base.Parent; }
        }

        /// <summary>
        /// ¥��¥��������
        /// </summary>
        public int SubPostsCount { get; private set; }

        public SubPostListView SubPosts
        {
            get
            {
                return _SubPosts;
                //TODO:����10���Ժ��¥��¥��
            }
        }

        public override Task<bool> ReplyAsync(string contentCode)
        {
            return Topic.ReplyAsync(contentCode, Id);
        }

        /// <summary>
        /// ������ӵ����ߡ�
        /// </summary>
        public override void BlockAuthor(string reason)
        {
            Topic.BlockUser(new BlockUserParams(Author.Name, Id), reason);
        }

        internal PostVisitor(long id, int floor, UserStub author, string content, DateTime submissionTime,
            int commentsCount, JObject subPostsCache, PostListView view)
            : base(id,floor,author,content,submissionTime, view)
        {
            SubPostsCount = commentsCount;
            _SubPosts = new SubPostListView(this, "about:blank");
            _SubPosts.SetOverallSubPostsCache(subPostsCache);
            //��֪��һҳ�϶���Ԥ�Ȼ���õġ�
            if (subPostsCache != null) _SubPosts.Refresh();
        }
    }

    public class SubPostListView : VisitorPageListView<PostVisitor, SubPostVisitor>
    {

        private JObject _OverallSubPostsCache;

        internal void SetOverallSubPostsCache(JObject cache)
        {
            _OverallSubPostsCache = cache;
        }

        internal SubPostListView(PostVisitor parent, string pageUrl)
            : base(parent, pageUrl)
        { }

        protected override Task OnRefreshPageAsync()
        {
            if (_OverallSubPostsCache == null) return null;
            //¥��¥
            /*
                 {
"errno": 0,
"errmsg": "success",
"data": {
    "comment_list": {
        "65718232016": {
            "comment_num": 1,
            "comment_list_num": 1,
            "comment_info": [
                {
                    "thread_id": "3636549985",
                    "post_id": "65718232016",
                    "comment_id": "65718247989",
                    "username": "˫������",
                    "user_id": "1038711401",
                    "now_time": 1426567930,
                    "content": "p.s. Lzע��ǰ׺<img class=\"BDE_Smiley\" pic_type=\"1\" width=\"30\" height=\"30\" src=\"http://tb2.bdstatic.com/tb/editor/images/face/i_f01.png?t=20140803\" >",
                    "ptype": 0,
                    "during_time": 0
                }
            ]
        }
    },
    "user_list": {
        "1038711401": {
            "user_id": 1038711401,
            "user_name": "˫������",
            "user_sex": 2,
            "user_status": 0,
            "bg_id": "1012",
            "card": "a:6:{s:8:\"post_num\";i:4175;s:8:\"good_num\";i:3;s:12:\"manager_info\";a:2:{s:7:\"manager\";a:2:{s:10:\"forum_list\";a:0:{}s:5:\"count\";i:0;}s:6:\"assist\";a:2:{s:10:\"forum_list\";a:3:{i:0;s:10:\"èͷӥ����\";i:1;s:8:\"ҹɷ����\";i:2;s:6:\"�����\";}s:5:\"count\";i:3;}}s:10:\"like_forum\";a:3:{i:10;a:2:{s:10:\"forum_list\";a:1:{i:0;s:10:\"èͷӥ����\";}s:5:\"count\";i:1;}i:8;a:2:{s:10:\"forum_list\";a:2:{i:0;s:10:\"�����ߴ���\";i:1;s:9:\"aea������\";}s:5:\"count\";i:2;}i:7;a:2:{s:10:\"forum_list\";a:2:{i:0;s:8:\"ҹɷ����\";i:1;s:8:\"��������\";}s:5:\"count\";i:4;}}s:9:\"is_novice\";i:0;s:7:\"op_time\";i:1433760933;}",
            "portrait_time": "1393118701",
            "mParr_props": [],
            "tbscore_repeate_finish_time": "1433562554",
            "use_sig": 0,
            "notice_mask": {
                "2": 0,
                "3": 0,
                "5": 0,
                "6": 0,
                "9": 0,
                "1000": 0
            },
            "user_type": 0,
            "meizhi_level": 0,
            "new_iconinfo": {
                "1": []
            },
            "portrait": "697ae58f8ce9b1bce6bba1e69c88e93d",
            "nickname": "˫������"
        }
    }
}
}
                 */
            var thisSp = _OverallSubPostsCache["data"]["comment_list"];
            //����û�лظ����á�
            thisSp = thisSp.SelectToken(Convert.ToString(Parent.Id), false);
            if (thisSp != null)
            {
                RegisterNewItem(
                    thisSp["comment_info"].Select(et =>
                        new SubPostVisitor((long) et["comment_id"],
                            new UserStub((long) et["user_id"], (string) et["username"]),
                            (string) et["content"],
                            Utility.FromUnixDateTime((long) et["now_time"]*1000),
                            this)));
            }
            _OverallSubPostsCache = null;
            return null;
        }

        protected override VisitorPageListView<SubPostVisitor> PageFactory(string url)
        {
            throw new NotImplementedException();
        }
    }
}