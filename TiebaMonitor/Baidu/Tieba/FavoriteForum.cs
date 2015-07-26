using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Visitors.Baidu.Tieba
{
    /// <summary>
    /// 用于保存喜欢的吧的信息。
    /// </summary>
    public class FavoriteForum
    {
        /// <summary>贴吧的 Id。</summary>
        public long Id { get; private set; }

        /// <summary>贴吧名称。</summary>
        public string Name { get; private set; }

        public DateTime JoinTime { get; private set; }

        public int Level { get; private set; }

        public bool HasSignedIn { get; private set; }

        public override string ToString()
        {
            return (HasSignedIn ? "[SI]" : "") + string.Format("[{0}]{1},J:{2},L{3}", Id, Name, JoinTime, Level);
        }

        internal void LoadData(long id, string name, DateTime joinTime, int level, bool hasSignedIn)
        {
            Id = id;
            Name = name;
            JoinTime = joinTime;
            Level = level;
            HasSignedIn = hasSignedIn;
        }

        internal void LoadData(int level, bool hasSignedIn)
        {
            Level = level;
            HasSignedIn = hasSignedIn;
        }

        internal FavoriteForum()
        { }
    }
}
