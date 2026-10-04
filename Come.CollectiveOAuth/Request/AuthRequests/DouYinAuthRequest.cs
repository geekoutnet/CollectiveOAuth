using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Enums.Scope;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json.Linq;
using System;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 抖音登录，对齐 JustAuth AuthDouyinRequest
     */
    public class DouyinAuthRequest : DefaultAuthRequest
    {
        public DouyinAuthRequest(ClientConfig config) : base(config, new DouyinAuthSource())
        {
        }

        public DouyinAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new DouyinAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            return this.getToken(accessTokenUrl(authCallback.code));
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string response = doGetUserInfo(authToken);
            var userInfoObject = JsonExtensions.parseObject(response);
            this.checkResponse(userInfoObject);
            var obj = userInfoObject.getJSONObject("data");
            authToken.unionId = obj.getString("union_id");

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.uuid = obj.getString("union_id");
            authUser.username = obj.getString("nickname");
            authUser.nickname = obj.getString("nickname");
            authUser.avatar = obj.getString("avatar");
            authUser.remark = obj.getString("description");
            authUser.gender = GlobalAuthUtil.getRealGender(obj.getString("gender"));
            // 与 Java String.format("%s %s %s") 一致，null 输出为 "null"
            authUser.location = string.Format("{0} {1} {2}",
                GlobalAuthUtil.javaValueOf(obj.getString("country")),
                GlobalAuthUtil.javaValueOf(obj.getString("province")),
                GlobalAuthUtil.javaValueOf(obj.getString("city")));
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = response;
            return authUser;
        }

        public override AuthResponse refresh(AuthToken oldToken)
        {
            return new AuthResponse(Convert.ToInt32(AuthResponseStatus.SUCCESS), null, getToken(refreshTokenUrl(oldToken.refreshToken)));
        }

        /**
         * 检查响应内容是否正确
         */
        private void checkResponse(JObject obj)
        {
            string message = obj.getString("message");
            var data = obj.getJSONObject("data");
            int errorCode = data.getIntValue("error_code");
            if ("error".Equals(message) || errorCode != 0)
            {
                throw new AuthException(errorCode, data.getString("description"));
            }
        }

        /**
         * 获取token，适用于获取access_token和刷新token
         */
        private AuthToken getToken(string accessTokenUrl)
        {
            string response = HttpUtils.RequestPost(accessTokenUrl, null, null, null, config.httpConfig);
            var obj = JsonExtensions.parseObject(response);
            this.checkResponse(obj);
            var dataObj = obj.getJSONObject("data");

            var authToken = new AuthToken();
            authToken.accessToken = dataObj.getString("access_token");
            authToken.openId = dataObj.getString("open_id");
            authToken.expireIn = dataObj.getIntValue("expires_in");
            authToken.refreshToken = dataObj.getString("refresh_token");
            authToken.refreshTokenExpireIn = dataObj.getIntValue("refresh_expires_in");
            authToken.scope = dataObj.getString("scope");
            return authToken;
        }

        public override string authorize(string state)
        {
            // Java 版传 encode=true 后交给不编码的 UrlBuilder；C# UrlBuilder 默认编码，这里传原值
            return UrlBuilder.fromBaseUrl(source.authorize())
                .queryParam("response_type", "code")
                .queryParam("client_key", config.clientId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("scope", this.getScopes(",", false, AuthScopeUtils.getDefaultScopes(AuthDouyinScope.values())))
                .queryParam("state", getRealState(state))
                .build();
        }

        protected override string accessTokenUrl(string code)
        {
            return UrlBuilder.fromBaseUrl(source.accessToken())
                .queryParam("code", code)
                .queryParam("client_key", config.clientId)
                .queryParam("client_secret", config.clientSecret)
                .queryParam("grant_type", "authorization_code")
                .build();
        }

        protected override string userInfoUrl(AuthToken authToken)
        {
            return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("access_token", authToken.accessToken)
                .queryParam("open_id", authToken.openId)
                .build();
        }

        protected override string refreshTokenUrl(string refreshToken)
        {
            return UrlBuilder.fromBaseUrl(source.refresh())
                .queryParam("client_key", config.clientId)
                .queryParam("refresh_token", refreshToken)
                .queryParam("grant_type", "refresh_token")
                .build();
        }
    }
}
