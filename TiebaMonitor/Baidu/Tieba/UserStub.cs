using System;

namespace PrettyBots.Visitors.Baidu.Tieba
{

    /// <summary>
    /// �����˰ٶ����������б��еĻ����û���Ϣ��
    /// �Ժ����������Щ��Ϣ���Ҹ���ҳ�档
    /// </summary>
    public struct UserStub : IEquatable<UserStub>
    {
        public bool Equals(UserStub other)
        {
            return string.CompareOrdinal(Name, other.Name) == 0;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is UserStub && Equals((UserStub) obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        public long? Id { get; set; }

        public string Name { get; set; }

        /// <summary>�û��ڴ����ɵĵȼ���</summary>
        public int? Level { get; set; }

        public override string ToString()
        {
            return string.Format("{0},{1},Lv{2}", Id, Name, Level);
        }

        public static bool operator==(UserStub x, UserStub y)
        {
            return string.CompareOrdinal(x.Name, y.Name) == 0;
        }

        public static bool operator !=(UserStub x, UserStub y)
        {
            return string.CompareOrdinal(x.Name, y.Name) != 0;
        }

        internal UserStub(long id, string name)
            : this(id, name, null)
        { }

        internal UserStub(long? id, string name, int? level) : this()
        {
            Id = id;
            Name = name;
            Level = level;
        }
    }
}