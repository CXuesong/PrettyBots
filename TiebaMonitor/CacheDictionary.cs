//using System;
//using System.Collections;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace PrettyBots.Visitors
//{
//    /// <summary>
//    /// 用于保存页面缓存的字典。
//    /// </summary>
//    class CacheDictionary
//    {
//        private class CacheInfoEntry
//        {
//            public int Time;
//            public string Url;
//            public string Content;

//            public CacheInfoEntry(int time, string url, string content)
//            {
//                Time = time;
//                Url = url;
//                Content = content;
//            }
//        }
//        private Dictionary<string, CacheInfoEntry> myDict;
//        private LinkedList<CacheInfoEntry> cacheInfo;
//        private long _CacheDurationMilliseconds;
//        private int _Capacity;

//        /// <summary>
//        /// 获取缓存的有效时间（毫秒）。
//        /// </summary>
//        public long CacheDuration
//        {
//            get { return _CacheDurationMilliseconds; }
//            set
//            {
//                if (value < 0) throw new ArgumentOutOfRangeException("value");
//                _CacheDurationMilliseconds = value;
//            }
//        }

//        public int Capacity
//        {
//            get { return _Capacity; }
//            set
//            {
//                if (value <= 0) throw new ArgumentOutOfRangeException("value");
//                _Capacity = value;
//            }
//        }

//        /// <summary>
//        /// 增加一个缓存项目。
//        /// </summary>
//        public void AddCache(string url, string content)
//        {
//            if (myDict.Count >= Capacity) Prune();
//            CacheInfoEntry entry;
//            if (myDict.TryGetValue())
//            if (myDict.ContainsKey(url))
//            {
//                myDict.
//            }
//        }

//        //移除过多或过期的缓存。
//        private void Prune()
//        {
//            var now = Environment.TickCount;
//            while (cacheInfo.Count > 0)
//            {
//                var entry = cacheInfo.Last.Value;
//                //修剪时移除一半的缓存。
//                if (cacheInfo.Count > Capacity / 2) goto REMOVE;
//                var delta = now - entry.Time;
//                if (delta > _CacheDurationMilliseconds || delta < 0)
//                    goto REMOVE;
//                continue;
//            REMOVE:
//                cacheInfo.RemoveLast();
//                myDict.Remove(entry.Url);
//            }
//        }

//        public CacheDictionary()
//        {
//            Capacity = 100;
//            CacheDuration = 30 * 1000;
//            myDict = new Dictionary<string, string>(100, StringComparer.OrdinalIgnoreCase);
//        }
//    }
//}
