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
    /// 表示因操作失败而引发的异常。一般此异常是由服务器返回数据中的 err_no 指示的。
    /// </summary>
    public class OperationFailedException : InvalidOperationException
    {
        private int _ErrorCode;
        private string _ErrorMessage;

        /// <summary>
        /// 获取异常代码。
        /// </summary>
        public int ErrorCode
        {
            get { return _ErrorCode; }
        }

        /// <summary>
        /// 获取异常消息。
        /// </summary>
        public string ErrorMessage
        {
            get { return _ErrorMessage; }
        }

        public OperationFailedException()
            : this(Prompts.OperationFailedException)
        { }

        public OperationFailedException(int errorCode, string errorMessage)
            : base(
                string.Format(
                    string.IsNullOrEmpty(errorMessage)
                        ? Prompts.OperationFailedException_ErrorCode
                        : Prompts.OperationFailedException_ErrorCodeMessage,
                    errorCode, errorMessage))
        {
            _ErrorCode = errorCode;
            _ErrorMessage = errorMessage;
        }

        public OperationFailedException(int errorCode)
            : this(errorCode, null)
        {
            
        }

        public OperationFailedException(string message)
            : base(message)
        { }

        public OperationFailedException(string message, Exception inner)
            : base(message, inner)
        { }

        public OperationFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _ErrorCode = info.GetInt32("ErrorCode");
            _ErrorMessage = info.GetString("ErrorMessage");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("ErrorCode", _ErrorCode);
            info.AddValue("ErrorMessage", _ErrorMessage);
        }
    }
}
