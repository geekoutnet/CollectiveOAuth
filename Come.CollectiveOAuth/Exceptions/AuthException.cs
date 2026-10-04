using System;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Exceptions
{
    /// <summary>
    /// 对应 JustAuth AuthException
    /// </summary>
    public class AuthException : Exception
    {
        public int errorCode { get; }

        public string errorMsg { get; }

        private readonly string message;

        /// <summary>
        /// 与 Java getMessage() 一致：构造时传入 null 则为 null（.NET 默认会返回一段默认文案）
        /// </summary>
        public override string Message => message;

        public AuthException(string errorMsg) : this(Convert.ToInt32(AuthResponseStatus.FAILURE), errorMsg)
        {
        }

        public AuthException(string errorMsg, IAuthSource source) : this(Convert.ToInt32(AuthResponseStatus.FAILURE), errorMsg, source)
        {
        }

        public AuthException(int errorCode, string errorMsg) : base(errorMsg)
        {
            this.message = errorMsg;
            this.errorCode = errorCode;
            this.errorMsg = errorMsg;
        }

        public AuthException(AuthResponseStatus status) : this(Convert.ToInt32(status), status.GetDesc())
        {
        }

        public AuthException(int errorCode, string errorMsg, IAuthSource source) : this(errorCode, $"{errorMsg} [{source.getName()}]")
        {
        }

        public AuthException(AuthResponseStatus status, IAuthSource source) : this(Convert.ToInt32(status), status.GetDesc(), source)
        {
        }

        public AuthException(string message, Exception cause) : base(message, cause)
        {
            this.message = message;
        }

        /// <summary>
        /// 与 Java 的 RuntimeException(Throwable) 一致，消息为 "异常类型: 消息"
        /// </summary>
        public AuthException(Exception cause) : base($"{cause.GetType().FullName}: {cause.Message}", cause)
        {
            this.message = $"{cause.GetType().FullName}: {cause.Message}";
        }
    }
}
