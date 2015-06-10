using System;
using System.Runtime.Serialization;

namespace PrettyBots.Monitor
{
    /// <summary>
    /// 表示因接受到的数据与期望不符而引发的异常。
    /// </summary>
    public class UnexpectedDataException : InvalidOperationException
    {
        public UnexpectedDataException()
            : this(Prompts.UnexpectedDataException)
        { }

        public UnexpectedDataException(string message)
            : base(message)
        { }

        public UnexpectedDataException(string message, Exception inner)
            : base(message, inner)
        { }

        public UnexpectedDataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    /// <summary>
    /// 表示因登录失败而引发的异常。
    /// </summary>
    public class LoginException : InvalidOperationException
    {
        public LoginException()
            : this(Prompts.UnexpectedDataException)
        { }

        public LoginException(string message)
            : base(message)
        { }

        public LoginException(string message, Exception inner)
            : base(message, inner)
        { }

        public LoginException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
