using System;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json.Linq;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 各平台共用的辅助方法
     */
    public partial class DefaultAuthRequest
    {
        /**
         * 填充用户信息中与平台字段无关的公共部分：token、source、原始用户信息
         */
        protected void fillAuthUser(AuthUser authUser, AuthToken authToken, object originalUser, string originalUserStr)
        {
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = originalUser;
            authUser.originalUserStr = originalUserStr;
        }

        /**
         * 标准 OAuth 错误响应：包含 error 字段时以 error_description 抛出异常
         */
        protected static void checkErrorDescription(JObject obj)
        {
            if (obj.containsKey("error"))
            {
                throw new AuthException(obj.getString("error_description"));
            }
        }

        /**
         * 执行操作，非 AuthException 的异常统一包装为 AuthException（AuthException 原样抛出）
         */
        protected static T wrapAuthException<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch (Exception e) when (!(e is AuthException))
            {
                throw new AuthException(e);
            }
        }
    }
}
