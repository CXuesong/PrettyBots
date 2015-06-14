using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrettyBots.Visitors
{
    /// <summary>
    /// 表示当前登录账户的信息。
    /// </summary>
    public interface IAccountInfo
    {
        /// <summary>
        /// 获取一个值，指示了用户当前是否已经登录。
        /// </summary>
        bool IsLoggedIn { get; }

        /// <summary>
        /// 用户名。
        /// </summary>
        string UserName { get; }

        /// <summary>
        /// 尝试使用指定的用户名和密码进行登录。
        /// </summary>
        /// <returns>如果用户成功登录，返回<c>true</c>。
        /// 如果用户取消登录，返回<c>false</c>。</returns>
        /// <exception cref="LoginException">登录过程中发生了错误。</exception>
        bool Login(string userName, string password);

        /// <summary>
        /// 注销当前用户。
        /// </summary>
        /// <exception cref="LoginException">注销过程中发生了错误。</exception>
        void Logout();
    }

    /// <summary>
    /// 为 <see cref="IAccountInfo"/> 提供实用函数。
    /// </summary>
    public static class AccountInfo
    {
        /// <summary>
        /// 获取账户所在的域名称。
        /// </summary>
        public static string[] GetSupportedDomains(IAccountInfo info)
        {
            if (info == null) throw new ArgumentNullException("info");
            return GetSupportedDomains(info.GetType());
        }

        public static string[] GetSupportedDomains(Type accountInfoType)
        {
            if (accountInfoType == null) throw new ArgumentNullException("accountInfoType");
            var attr =
                (AccountInfoAttribute) Attribute.GetCustomAttribute(accountInfoType, typeof (AccountInfoAttribute));
            return attr == null ? null : attr.SupportedDomains;
        }
    }

    public static class AccountDomains
    {
        public const string Baidu = "baidu";
        public const string NetEase = "netease";
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AccountInfoAttribute : Attribute
    {
        /// <summary>
        /// 此账户所起作用的域名称。此名称用于保存账户信息时区分不同的网站使用。区分大小写。
        /// </summary>
        public string[] SupportedDomains { get; private set; }

        public bool IsDomainSupported(string domain)
        {
            return SupportedDomains.Any(d => string.Equals(d, domain, StringComparison.Ordinal));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="supportedDomains">此账户所起作用的域名称。此名称用于保存账户信息时区分不同的网站使用。
        /// 区分大小写。</param>
        public AccountInfoAttribute(string[] supportedDomains)
        {
            SupportedDomains = supportedDomains;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="supportedDomain">此账户所起作用的域名称。此名称用于保存账户信息时区分不同的网站使用。
        /// 区分大小写。</param>
        public AccountInfoAttribute(string supportedDomain)
        {
            SupportedDomains = new[] {supportedDomain};
        }

        // This is a named argument
        public int NamedInt { get; set; }
    }
}
