using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrettyBots.Visitors.Baidu.Tieba;
using PrettyBots.Visitors.Baidu.Tieba;

namespace PrettyBots.Strategies
{
    public class TextComposingEventArgs : EventArgs
    {
        private string _ContentKey;
        private object _Context;

        public string ContentKey
        {
            get { return _ContentKey; }
        }

        public object Context
        {
            get { return _Context; }
        }

        public string Content { get; set; }

        public TextComposingEventArgs(string contentKey, string content, object context)
        {
            _ContentKey = contentKey;
            Content = content;
        }
    }

    public class PostComposingEventArgs : TextComposingEventArgs
    {
        private PostVisitorBase _Referer;

        public PostVisitorBase Referer
        {
            get { return _Referer; }
        }

        public PostComposingEventArgs(string contentKey, string content, object context, PostVisitorBase referer)
            : base(contentKey, content, context)
        {
            _Referer = referer;
        }
    }

    /// <summary>
    /// 为生成帖子内容提供统一的方法。
    /// </summary>
    public class TextComposer
    {
        public static readonly TextComposer Default = new TextComposer();

        public event EventHandler<TextComposingEventArgs> TextComposing;

        protected virtual void OnTextComposing(TextComposingEventArgs e)
        {
            if (TextComposing != null) TextComposing(this, e);
        }

        public string ComposeText(string key, string defaultContent)
        {
            return ComposeText(key, defaultContent, null);
        }

        public string ComposeText(string key, string defaultContent, object context)
        {
            var e = new TextComposingEventArgs(key, defaultContent, context);
            OnTextComposing(e);
            return e.Content;
        }

        public string ComposePost(string key, string defaultContent, PostVisitorBase referer)
        {
            return ComposePost(key, defaultContent, referer, null);
        }

        public string ComposePost(string key, string defaultContent, PostVisitorBase referer, object context)
        {
            var e = new PostComposingEventArgs(key, defaultContent, context, referer);
            OnTextComposing(e);
            return e.Content;
        }
    }
}
