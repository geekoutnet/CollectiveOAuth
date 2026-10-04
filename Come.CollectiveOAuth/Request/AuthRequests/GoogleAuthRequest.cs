using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Enums.Scope;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Come.CollectiveOAuth.Request
{
    /**
     * Google登录，对齐 JustAuth AuthGoogleRequest
     */
    public class GoogleAuthRequest : DefaultAuthRequest
    {
        public GoogleAuthRequest(ClientConfig config) : base(config, new GoogleAuthSource())
        {
        }

        public GoogleAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new GoogleAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string response = doPostAuthorizationCode(authCallback.code);
            var accessTokenObject = JsonExtensions.parseObject(response);
            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.expireIn = accessTokenObject.getIntValue("expires_in");
            authToken.scope = accessTokenObject.getString("scope");
            authToken.tokenType = accessTokenObject.getString("token_type");
            authToken.idToken = accessTokenObject.getString("id_token");
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            var httpHeader = new Dictionary<string, object> { { "Authorization", "Bearer " + authToken.accessToken } };
            string userInfo = HttpUtils.RequestPost(userInfoUrl(authToken), null, httpHeader, null, config.httpConfig);
            var obj = JsonExtensions.parseObject(userInfo);
            this.checkResponse(obj);

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.uuid = obj.getString("sub");
            authUser.username = obj.getString("email");
            authUser.avatar = obj.getString("picture");
            authUser.nickname = obj.getString("name");
            authUser.location = obj.getString("locale");
            authUser.email = obj.getString("email");
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = userInfo;
            return authUser;
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
            return UrlBuilder.fromBaseUrl(base.authorize(state))
                .queryParam("access_type", "offline")
                .queryParam("scope", this.getScopes(" ", false, AuthScopeUtils.getDefaultScopes(AuthGoogleScope.values())))
                .queryParam("prompt", "select_account")
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
            return UrlBuilder.fromBaseUrl(source.userInfo()).queryParam("access_token", authToken.accessToken).build();
        }

        /**
         * 检查响应内容是否正确
         *
         * @param object 请求响应内容
         */
        private void checkResponse(JObject obj)
        {
            if (obj.containsKey("error") || obj.containsKey("error_description"))
            {
                // 与 Java 字符串拼接一致：布尔值输出 true/false，null 输出 "null"
                throw new AuthException((obj.containsKey("error") ? "true" : "false") + ":" + (obj.getString("error_description") ?? "null"));
            }
        }
    }
}
