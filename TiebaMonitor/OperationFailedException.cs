using System;
using System.Runtime.Serialization;

namespace PrettyBots.Visitors
{
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

    /// <summary>
    /// 表示因操作过于密集而引发的异常。
    /// </summary>
    public class OperationTooFrequentException : OperationFailedException
    {
        
        public OperationTooFrequentException(int errorCode)
            : base(errorCode, Prompts.OperationsTooFrequentException)
        {
        }

        public OperationTooFrequentException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// 表示因无法通过验证码验证而引发的异常。
    /// </summary>
    public class NonhumanException : OperationFailedException
    {

        public NonhumanException(int errorCode)
            : base(errorCode, Prompts.NeedVCode)
        {
        }

        public NonhumanException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// 表示因权限不足而引发的异常。
    /// </summary>
    public class OperationUnauthorizedException : OperationFailedException
    {

        public OperationUnauthorizedException(int errorCode)
            : base(errorCode, Prompts.NeedVCode)
        {
        }

        public OperationUnauthorizedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// 表示因用户被封禁而引发的异常。
    /// </summary>
    public class AccountBlockedException : OperationFailedException
    {

        public AccountBlockedException(int errorCode)
            : base(errorCode, Prompts.AccountBlockedException)
        {
        }

        public AccountBlockedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}