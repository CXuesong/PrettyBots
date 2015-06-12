namespace PrettyBots.Monitor.Baidu.Tieba
{

    /// <summary>
    /// �����˰ٶ����������б��еĻ����û���Ϣ��
    /// �Ժ����������Щ��Ϣ���Ҹ���ҳ�档
    /// </summary>
    public struct TiebaUserStub
    {
        public long? Id { get; set; }

        public string Name { get; set; }

        /// <summary>�û��ڴ����ɵĵȼ���</summary>
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