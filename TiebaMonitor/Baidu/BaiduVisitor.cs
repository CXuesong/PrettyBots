using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using PrettyBots.Visitors.Baidu.Tieba;

namespace PrettyBots.Visitors.Baidu
{
    /// <summary>
    /// 用于登录百度平台。
    /// </summary>
    /// <remarks>可以使用同一个<see cref="WebSession"/>建立多个Visitor。</remarks>
    public class BaiduVisitor : Visitor
    {
        /// <summary>
        /// 管理当前用户的账户信息。
        /// </summary>
        public new BaiduAccountInfo AccountInfo { get { return (BaiduAccountInfo) base.AccountInfo; } }

        /// <summary>
        /// 管理当前用户的贴吧消息。
        /// </summary>
        public MessagesVisitor Messages { get; private set; }

        private TiebaVisitor _Tieba;

        /// <summary>
        /// 访问百度贴吧。
        /// </summary>
        public TiebaVisitor Tieba
        {
            get
            {
                if (_Tieba == null) _Tieba = new TiebaVisitor(this);
                return _Tieba;
            }
        }

        public BaiduVisitor(WebSession session)
            : base(session)
        {
            base.AccountInfo = new BaiduAccountInfo(this);
            Messages = new MessagesVisitor(this);
        }

        public BaiduVisitor()
            : this(null)
        { }
    }
}
