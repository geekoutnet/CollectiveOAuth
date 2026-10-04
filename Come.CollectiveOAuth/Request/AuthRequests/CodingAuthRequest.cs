using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums.Scope;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json.Linq;

namespace Come.CollectiveOAuth.Request
{
    /**
     * Coding登录，对齐 JustAuth AuthCodingRequest
     */
    public class CodingAuthRequest : DefaultAuthRequest
    {
        public CodingAuthRequest(ClientConfig config) : base(config, new CodingAuthSource())
        {
        }

        public CodingAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new CodingAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string response = doGetAuthorizationCode(authCallback.code);
            var accessTokenObject = JsonExtensions.parseObject(response);
            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.expireIn = accessTokenObject.getIntValue("expires_in");
            authToken.refreshToken = accessTokenObject.getString("refresh_token");
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string response = doGetUserInfo(authToken);
            var obj = JsonExtensions.parseObject(response);
            this.checkResponse(obj);

            obj = obj.getJSONObject("data");
            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.uuid = obj.getString("id");
            authUser.username = obj.getString("name");
            // 与 Java 字符串拼接一致：null 拼成 "null"
            authUser.avatar = "https://coding.net" + GlobalAuthUtil.javaValueOf(obj.getString("avatar"));
            authUser.blog = "https://coding.net" + GlobalAuthUtil.javaValueOf(obj.getString("path"));
            authUser.nickname = obj.getString("name");
            authUser.company = obj.getString("company");
            authUser.location = obj.getString("location");
            authUser.gender = GlobalAuthUtil.getRealGender(obj.getString("sex"));
            authUser.email = obj.getString("email");
            authUser.remark = obj.getString("slogan");
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = response;
            return authUser;
        }

        /**
         * 检查响应内容是否正确
         *
         * @param object 请求响应内容
         */
        private void checkResponse(JObject obj)
        {
            if (obj.getIntValue("code") != 0)
            {
                throw new AuthException(obj.getString("msg"));
            }
        }

        /**
         * 对应 Java String.format(url, config.getDomainPrefix())
         */
        private string withDomainPrefix(string url)
        {
            return url.Replace("%s", GlobalAuthUtil.javaValueOf(config.domainPrefix));
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
            return UrlBuilder.fromBaseUrl(withDomainPrefix(source.authorize()))
                .queryParam("response_type", "code")
                .queryParam("client_id", config.clientId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("scope", this.getScopes(" ", false, AuthScopeUtils.getDefaultScopes(AuthCodingScope.values())))
                .queryParam("state", getRealState(state))
                .build();
        }

        /**
         * 返回获取accessToken的url
         *
         * @param code 授权码
         * @return 返回获取accessToken的url
         */
        protected override string accessTokenUrl(string code)
        {
            return UrlBuilder.fromBaseUrl(withDomainPrefix(source.accessToken()))
                .queryParam("code", code)
                .queryParam("client_id", config.clientId)
                .queryParam("client_secret", config.clientSecret)
                .queryParam("grant_type", "authorization_code")
                .queryParam("redirect_uri", config.redirectUri)
                .build();
        }

        /**
         * 返回获取userInfo的url
         *
         * @param authToken token
         * @return 返回获取userInfo的url
         */
        protected override string userInfoUrl(AuthToken authToken)
        {
            return UrlBuilder.fromBaseUrl(withDomainPrefix(source.userInfo()))
                .queryParam("access_token", authToken.accessToken)
                .build();
        }
    }
}
