using System;
using System.Linq;
using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Models;

namespace Come.CollectiveOAuth.Utils
{
    public class AuthChecker
    {
        /**
      * 是否支持第三方登录
      *
      * @param config config
      * @param source source
      * @return true or false
      * @since 1.6.1-beta
      */
        public static bool isSupportedAuth(ClientConfig config, IAuthSource source)
        {

            bool isSupported = !string.IsNullOrWhiteSpace(config.clientId) && !string.IsNullOrWhiteSpace(config.clientSecret) && !string.IsNullOrWhiteSpace(config.redirectUri);
            /*if (isSupported && DefaultAuthSource.ALIPAY == source)
            {
                isSupported = StringUtils.isNotEmpty(config.getAlipayPublicKey());
            }
            if (isSupported && AuthDefaultSource.STACK_OVERFLOW == source)
            {
                isSupported = StringUtils.isNotEmpty(config.getStackOverflowKey());
            }
            if (isSupported && AuthDefaultSource.WECHAT_ENTERPRISE == source)
            {
                isSupported = StringUtils.isNotEmpty(config.getAgentId());
            }*/

           

            return isSupported;
        }


        /**
         * 检查配置合法性。针对部分平台， 对redirect uri有特定要求。一般来说redirect uri都是http://，而对于facebook平台， redirect uri 必须是https的链接
         *
         * @param config config
         * @param source source
         * @since 1.6.1-beta
         */
        public static void checkConfig(ClientConfig config, IAuthSource source)
        {
            string redirectUri = config.redirectUri;
            if (!GlobalAuthUtil.isHttpProtocol(redirectUri) && !GlobalAuthUtil.isHttpsProtocol(redirectUri))
            {
                throw new Exception(AuthResponseStatus.ILLEGAL_REDIRECT_URI.GetDesc());
            }
            // facebook的回调地址必须为https的链接
            if ("FACEBOOK".Equals(source.getName().ToUpper()) && !GlobalAuthUtil.isHttpsProtocol(redirectUri))
            {
                // Facebook's redirect uri must use the HTTPS protocol
                throw new Exception(AuthResponseStatus.ILLEGAL_REDIRECT_URI.GetDesc());
            }
            // 支付宝在创建回调地址时，不允许使用localhost或者127.0.0.1
            if ("ALIPAY".Equals(source.getName().ToUpper()) && GlobalAuthUtil.isLocalHost(redirectUri))
            {
                // The redirect uri of alipay is forbidden to use localhost or 127.0.0.1
                throw new Exception(AuthResponseStatus.ILLEGAL_REDIRECT_URI.GetDesc());
            }
        }

        /**
         * 校验回调传回的code
         * <p>
         * {@code v1.10.0}版本中改为传入{@code source}和{@code callback}，对于不同平台使用不同参数接受code的情况统一做处理
         *
         * @param source   当前授权平台
         * @param callback 从第三方授权回调回来时传入的参数集合
         * @since 1.8.0
         */
        public static void checkCode(IAuthSource source, AuthCallback callback)
        {
            string code = callback.code;
            if (source.getName().ToUpper().Equals(DefaultAuthSourceEnum.ALIPAY_MP.ToString()))
            {
                code = callback.auth_code;
            }
            else if ("HUAWEI".Equals(source.getName().ToUpper()))
            {
                code = callback.authorization_code;
            }
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new Exception(AuthResponseStatus.ILLEGAL_CODE.GetDesc());
            }
        }

        /**
         * 校验回调传回的{@code state}，为空或者不存在
         * <p>
         * {@code state}不存在的情况只有两种：
         * 1. {@code state}已使用，被正常清除
         * 2. {@code state}为前端伪造，本身就不存在
         *
         * @param state          {@code state}一定不为空
         * @param authStateCache {@code authStateCache} state缓存实现
         */
        public static void checkState(string state, IAuthSource source, IAuthStateCache authStateCache)
        {
            if (string.IsNullOrWhiteSpace(state) || !authStateCache.containsKey(state))
            {
                throw new Exception(AuthResponseStatus.ILLEGAL_STATUS.GetDesc());
            }
        }
    }
}