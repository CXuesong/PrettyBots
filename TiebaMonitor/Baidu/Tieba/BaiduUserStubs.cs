namespace PrettyBots.Monitor.Baidu.Tieba
{

    /// <summary>
    /// 保存了百度贴吧帖子列表中的基本用户信息。
    /// 稍后可以利用这些信息查找个人页面。
    /// </summary>
    public struct TiebaUserStub
    {
        public long? Id { get; set; }

        public string Name { get; set; }

        /// <summary>用户在此贴吧的等级。</summary>
        public int? Level { get; set; }

        public override string ToString()
        {
            return string.Format("{0},{1},Lv{2}", Id, Name, Level);
        }

        internal TiebaUserStub(string name) : this(null, name, null)
        { }

        internal TiebaUserStub(long? id, string name, int? level) : this()
        {
            Id = id;
            Name = name;
            Level = level;
        }
    }
}