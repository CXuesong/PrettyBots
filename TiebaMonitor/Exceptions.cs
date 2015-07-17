using System;
using System.Runtime.Serialization;

namespace PrettyBots.Visitors
{
    /// <summary>
    /// 表示因接受到的数据与期望不符而引发的异常。
    /// </summary>
    public class UnexpectedDataException : InvalidOperationException
    {
        public UnexpectedDataException()
            : this(Prompts.OperationUnauthorizedException)
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
}
