using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json.Linq;

namespace Come.CollectiveOAuth.Request
{
    /**
     * oschina登录，对齐 JustAuth AuthOschinaRequest
     */
    public class OschinaAuthRequest : DefaultAuthRequest
    {
        public OschinaAuthRequest(ClientConfig config) : base(config, new OschinaAuthSource())
        {
        }

        public OschinaAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new OschinaAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string response = doPostAuthorizationCode(authCallback.code);
            var accessTokenObject = JsonExtensions.parseObject(response);
            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.refreshToken = accessTokenObject.getString("refresh_token");
            authToken.uid = accessTokenObject.getString("uid");
            authToken.expireIn = accessTokenObject.getIntValue("expires_in");
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string response = doGetUserInfo(authToken);
            var obj = JsonExtensions.parseObject(response);
            this.checkResponse(obj);

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.uuid = obj.getString("id");
            authUser.username = obj.getString("name");
            authUser.nickname = obj.getString("name");
            authUser.avatar = obj.getString("avatar");
            authUser.blog = obj.getString("url");
            authUser.location = obj.getString("location");
            authUser.gender = GlobalAuthUtil.getRealGender(obj.getString("gender"));
            authUser.email = obj.getString("email");
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = response;
            return authUser;
        }

        /**
         * 返回获取accessToken的url
         *
         * @param code 授权回调时带回的授权码
         * @return 返回获取accessToken的url
         */
        protected override string accessTokenUrl(string code)
        {
            return UrlBuilder.fromBaseUrl(source.accessToken())
                .queryParam("code", code)
                .queryParam("client_id", config.clientId)
                .queryParam("client_secret", config.clientSecret)
                .queryParam("grant_type", "authorization_code")
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("dataType", "json")
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
                .queryParam("access_token", authToken.accessToken)
                .queryParam("dataType", "json")
                .build();
        }

        /**
         * 检查响应内容是否正确
         *
         * @param object 请求响应内容
         */
        private void checkResponse(JObject obj)
        {
            if (obj.containsKey("error"))
            {
                throw new AuthException(obj.getString("error_description"));
            }
        }
    }
}
