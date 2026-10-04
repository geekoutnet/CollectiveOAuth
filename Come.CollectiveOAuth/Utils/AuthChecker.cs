using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;

namespace Come.CollectiveOAuth.Utils
{
    public class AuthChecker
    {
        private static string nameOf(IAuthSource source)
        {
            return AuthSourceNames.toJustAuthName(source.getName().ToUpper());
        }

        /**
         * 是否支持第三方登录
         */
        public static bool isSupportedAuth(ClientConfig config, IAuthSource source)
        {
            string name = nameOf(source);
            bool isSupported = !string.IsNullOrEmpty(config.clientId) && !string.IsNullOrEmpty(config.clientSecret);
            if (isSupported && "STACK_OVERFLOW" == name)
            {
                isSupported = !string.IsNullOrEmpty(config.stackOverflowKey);
            }
            if (isSupported && "WECHAT_ENTERPRISE" == name)
            {
                isSupported = !string.IsNullOrEmpty(config.agentId);
            }
            if (isSupported && ("CODING" == name || "OKTA" == name))
            {
                isSupported = !string.IsNullOrEmpty(config.domainPrefix);
            }
            if (isSupported && "XMLY" == name)
            {
                isSupported = !string.IsNullOrEmpty(config.deviceId) && config.clientOsType != null;
                if (isSupported)
                {
                    isSupported = config.clientOsType == 3 || !string.IsNullOrEmpty(config.packId);
                }
            }
            return isSupported;
        }

        /**
         * 检查配置合法性
         */
        public static void checkConfig(ClientConfig config, IAuthSource source)
        {
            string redirectUri = config.redirectUri;
            if (config.ignoreCheckRedirectUri)
            {
                return;
            }
            if (string.IsNullOrEmpty(redirectUri))
            {
                throw new AuthException(AuthResponseStatus.ILLEGAL_REDIRECT_URI, source);
            }
            if (!GlobalAuthUtil.isHttpProtocol(redirectUri) && !GlobalAuthUtil.isHttpsProtocol(redirectUri))
            {
                throw new AuthException(AuthResponseStatus.ILLEGAL_REDIRECT_URI, source);
            }
        }

        /**
         * 校验回调传回的code
         */
        public static void checkCode(IAuthSource source, AuthCallback callback)
        {
            string name = nameOf(source);
            // 推特平台不支持回调 code 和 state
            if ("TWITTER" == name)
            {
                return;
            }
            string code = callback.code;
            if (string.IsNullOrEmpty(code) && "HUAWEI" == name)
            {
                code = callback.authorization_code;
            }
            if (string.IsNullOrEmpty(code))
            {
                throw new AuthException(AuthResponseStatus.ILLEGAL_CODE, source);
            }
        }

        /**
         * 校验回调传回的state，为空或者不存在
         */
        public static void checkState(string state, IAuthSource source, IAuthStateCache authStateCache)
        {
            // 推特平台不支持回调 code 和 state
            if ("TWITTER" == nameOf(source))
            {
                return;
            }
            if (string.IsNullOrEmpty(state) || !authStateCache.containsKey(state))
            {
                throw new AuthException(AuthResponseStatus.ILLEGAL_STATUS, source);
            }
        }
    }
}
