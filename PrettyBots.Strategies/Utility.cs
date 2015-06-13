using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Strategies
{
    internal static class Utility
    {

        /// <summary>
        /// 移除字符串中的符号和空白等内容，并使得大小写一致，便于比对。
        /// </summary>
        public static string NormalizeString(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return string.Empty;
            var builder = new StringBuilder();
            foreach (var c in str)
            {
                if (char.IsWhiteSpace(c) || char.IsPunctuation(c) ||
                    char.IsSymbol(c) || char.IsControl(c) ||
                    char.IsSeparator(c))
                    continue;
                builder.Append(char.ToUpper(c));
            }
            return builder.ToString();
        }
    }
}
