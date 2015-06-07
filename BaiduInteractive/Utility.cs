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
    }
}
