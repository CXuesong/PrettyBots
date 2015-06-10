namespace PrettyBots.Monitor.Baidu.Tieba
{
    class MessageNotifier : BaiduChildVisitor
    {
        //{0} -- UnixTime
        private const string NotifierUrlFormat = "http://tbmsg.baidu.com/gmessage/get?mtype=1&_={0}";

        public void Update()
        {
            
        }

        internal MessageNotifier(BaiduVisitor parent)
            : base(parent)
        {

        }
    }
}
