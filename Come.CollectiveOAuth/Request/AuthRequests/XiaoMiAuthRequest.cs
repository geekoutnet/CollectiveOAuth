using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Enums.Scope;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 小米登录，对齐 JustAuth AuthMiRequest
     */
    public class XiaoMiAuthRequest : DefaultAuthRequest
    {
        private static readonly string PREFIX = "&&&START&&&";

        public XiaoMiAuthRequest(ClientConfig config) : base(config, new XiaoMiAuthSource())
        {
        }

        public XiaoMiAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new XiaoMiAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            return getToken(accessTokenUrl(authCallback.code));
        }

        private AuthToken getToken(string accessTokenUrl)
        {
            string response = HttpUtils.RequestGet(accessTokenUrl, null, config.httpConfig);
            string jsonStr = response.Replace(PREFIX, "");
            var accessTokenObject = JsonExtensions.parseObject(jsonStr);

            if (accessTokenObject.containsKey("error"))
            {
                throw new AuthException(accessTokenObject.getString("error_description"));
            }

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.expireIn = accessTokenObject.getIntValue("expires_in");
            authToken.scope = accessTokenObject.getString("scope");
            authToken.tokenType = accessTokenObject.getString("token_type");
            authToken.refreshToken = accessTokenObject.getString("refresh_token");
            authToken.openId = accessTokenObject.getString("openId");
            authToken.macAlgorithm = accessTokenObject.getString("mac_algorithm");
            authToken.macKey = accessTokenObject.getString("mac_key");
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            // 获取用户信息
            string userResponse = doGetUserInfo(authToken);

            var userProfile = JsonExtensions.parseObject(userResponse);
            if ("error".Equals(userProfile.getString("result"), StringComparison.OrdinalIgnoreCase))
            {
                throw new AuthException(userProfile.getString("description"));
            }

            var obj = userProfile.getJSONObject("data");

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.uuid = authToken.openId;
            authUser.username = obj.getString("miliaoNick");
            authUser.nickname = obj.getString("miliaoNick");
            authUser.avatar = obj.getString("miliaoIcon");
            authUser.email = obj.getString("mail");
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = userResponse;

            // 获取用户邮箱手机号等信息
            string emailPhoneUrl = string.Format("{0}?clientId={1}&token={2}", "https://open.account.xiaomi.com/user/phoneAndEmail", config.clientId, authToken.accessToken);

            string emailResponse = HttpUtils.RequestGet(emailPhoneUrl, null, config.httpConfig);
            var userEmailPhone = JsonExtensions.parseObject(emailResponse);
            if (!"error".Equals(userEmailPhone.getString("result"), StringComparison.OrdinalIgnoreCase))
            {
                var emailPhone = userEmailPhone.getJSONObject("data");
                authUser.email = emailPhone.getString("email");
            }
            // 否则：小米开发平台暂时不对外开放用户手机及邮箱信息的获取（Java 版仅打印 warn 日志）

            return authUser;
        }

        /**
         * 刷新access token （续期）
         *
         * @param authToken 登录成功后返回的Token信息
         * @return AuthResponse
         */
        public override AuthResponse refresh(AuthToken authToken)
        {
            return new AuthResponse(AuthResponseStatus.SUCCESS.GetCode(), null, getToken(refreshTokenUrl(authToken.refreshToken)));
        }

        /**
         * 返回带{@code state}参数的授权url，授权回调时会带上这个{@code state}
         *
         * @param state state 验证授权流程的参数，可以防止csrf
         * @return 返回授权地址
         * @since 1.9.3
         */
        public override string authorize(string state)
        {
            // Java 版传 encode=true 后交给不编码的 UrlBuilder；C# UrlBuilder 默认编码，这里传原值
            return UrlBuilder.fromBaseUrl(base.authorize(state))
                .queryParam("skip_confirm", "false")
                .queryParam("scope", this.getScopes(" ", false, AuthScopeUtils.getDefaultScopes(AuthMiScope.values())))
                .build();
        }

        /**
         * 返回获取userInfo的url
         *
         * @param authToken 用户授权后的token
         * @return 返回获取userInfo的url
         */
        protected override string userInfoUrl(AuthToken authToken)
        {
            return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("clientId", config.clientId)
                .queryParam("token", authToken.accessToken)
                .build();
        }
    }
}
