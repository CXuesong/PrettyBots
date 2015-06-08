using System;
using System.Reflection;

namespace BaiduInterop.Interactive
{
    class Utility
    {
        private static string _ApplicationTitle;
        private static string _ProductName;
        private static Version _ProductVersion;

        public static string ApplicationTitle
        {
            get
            {
                if (_ApplicationTitle == null)
                {
                    var titleAttribute = typeof(Utility).Assembly.GetCustomAttribute<AssemblyTitleAttribute>();
                    _ApplicationTitle = titleAttribute != null ? titleAttribute.Title : "";
                }
                return _ApplicationTitle;
            }
        }

        public static string ProductName
        {
            get
            {
                if (_ProductName == null)
                {
                    var productAttribute = typeof(Utility).Assembly.GetCustomAttribute<AssemblyProductAttribute>();
                    _ProductName = productAttribute != null ? productAttribute.Product : "";
                }
                return _ProductName;
            }
        }

        public static Version ProductVersion
        {
            get
            {
                if (_ProductVersion == null) _ProductVersion = typeof(Utility).Assembly.GetName().Version;
                return _ProductVersion;
            }
        }

        public static string StringEllipsis(string source, int length)
        {
            if (length < 3) throw new ArgumentOutOfRangeException("length");
            if (string.IsNullOrEmpty(source)) return string.Empty;
            if (source.Length <= length) return source;
            return source.Substring(0, length - 3) + "...";
        }

        public static string PadString(string s, int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 回退指定的 Url。例如，将 http://abc.def/abc/def 回退为 http://abc.def/abc 。
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string FallbackUrl(string url)
        {
            throw new NotImplementedException();
        }
    }
}
