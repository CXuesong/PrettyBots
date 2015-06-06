using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace TiebaMonitor.Kernel
{
    class ExtendedWebClient : WebClient
    {
        private Stack<CookieContainer> _CookieStack = new Stack<CookieContainer>();

        public CookieContainer CookieContainer { get; set; }

        public void PushNewCookieContainer()
        {
            PushCookieContainer(new CookieContainer());
        }

        public void PushCookieContainer(CookieContainer newContainer)
        {
            _CookieStack.Push(CookieContainer);
            CookieContainer = newContainer ?? new CookieContainer();
        }

        public void PopCookieContainer()
        {
            CookieContainer = _CookieStack.Pop();
        }

        public ExtendedWebClient()
            : this(new CookieContainer())
        { }

        public ExtendedWebClient(CookieContainer c)
        {
            this.CookieContainer = c;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            var webRequest = request as HttpWebRequest;
            if (webRequest != null)
                webRequest.CookieContainer = this.CookieContainer;
            return request;
        }
    }
}
