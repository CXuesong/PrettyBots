﻿//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.34014
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace PrettyBots.Monitor {
    using System;
    
    
    /// <summary>
    ///   一个强类型的资源类，用于查找本地化的字符串等。
    /// </summary>
    // 此类是由 StronglyTypedResourceBuilder
    // 类通过类似于 ResGen 或 Visual Studio 的工具自动生成的。
    // 若要添加或移除成员，请编辑 .ResX 文件，然后重新运行 ResGen
    // (以 /str 作为命令选项)，或重新生成 VS 项目。
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Prompts {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Prompts() {
        }
        
        /// <summary>
        ///   返回此类使用的缓存的 ResourceManager 实例。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("PrettyBots.Monitor.Prompts", typeof(Prompts).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   使用此强类型资源类，为所有资源查找
        ///   重写当前线程的 CurrentUICulture 属性。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   查找类似 登录失败。 的本地化字符串。
        /// </summary>
        internal static string LoginException {
            get {
                return ResourceManager.GetString("LoginException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 登录失败。错误代码：{0}。 的本地化字符串。
        /// </summary>
        internal static string LoginException_ErrorCode {
            get {
                return ResourceManager.GetString("LoginException_ErrorCode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 密码错误。 的本地化字符串。
        /// </summary>
        internal static string LoginException_Password {
            get {
                return ResourceManager.GetString("LoginException_Password", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 无效的用户名。 的本地化字符串。
        /// </summary>
        internal static string LoginException_UserName {
            get {
                return ResourceManager.GetString("LoginException_UserName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 需要登录以进行操作。 的本地化字符串。
        /// </summary>
        internal static string NeedLogin {
            get {
                return ResourceManager.GetString("NeedLogin", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 操作失败。错误代码：{0}。 的本地化字符串。
        /// </summary>
        internal static string OperationFailedException_ErrorCode {
            get {
                return ResourceManager.GetString("OperationFailedException_ErrorCode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 接收到了预期之外格式的数据。 的本地化字符串。
        /// </summary>
        internal static string UnexpectedDataException {
            get {
                return ResourceManager.GetString("UnexpectedDataException", resourceCulture);
            }
        }
    }
}
