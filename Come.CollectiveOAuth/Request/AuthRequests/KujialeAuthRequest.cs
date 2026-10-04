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
     * 酷家乐登录，对齐 JustAuth AuthKujialeRequest
     */
    public class KujialeAuthRequest : DefaultAuthRequest
    {
        public KujialeAuthRequest(ClientConfig config) : base(config, new KujialeAuthSource())
        {
        }

        public KujialeAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new KujialeAuthSource(), authStateCache)
        {
        }

        /**
         * 返回带{@code state}参数的授权url，默认只向用户请求用户信息授权
         */
        public override string authorize(string state)
        {
            return UrlBuilder.fromBaseUrl(base.authorize(state))
                .queryParam("scope", this.getScopes(",", false, AuthScopeUtils.getDefaultScopes(AuthKujialeScope.values())))
                .build();
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string response = doPostAuthorizationCode(authCallback.code);
            return getAuthToken(response);
        }

        private AuthToken getAuthToken(string response)
        {
            var accessTokenObject = checkResponse(response);
            var resultObject = accessTokenObject.getJSONObject("d");

            var authToken = new AuthToken();
            authToken.accessToken = resultObject.getString("accessToken");
            authToken.refreshToken = resultObject.getString("refreshToken");
            authToken.expireIn = resultObject.getIntValue("expiresIn");
            return authToken;
        }

        private JObject checkResponse(string response)
        {
            var accessTokenObject = JsonExtensions.parseObject(response);
            if (!"0".Equals(accessTokenObject.getString("c")))
            {
                throw new AuthException(accessTokenObject.getString("m"));
            }
            return accessTokenObject;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string openId = this.getOpenId(authToken);
            string response = HttpUtils.RequestGet(UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("access_token", authToken.accessToken)
                .queryParam("open_id", openId)
                .build(), null, config.httpConfig);
            var obj = JsonExtensions.parseObject(response);
            if (!"0".Equals(obj.getString("c")))
            {
                throw new AuthException(obj.getString("m"));
            }
            var resultObject = obj.getJSONObject("d");

            var authUser = new AuthUser();
            authUser.rawUserInfo = resultObject;
            authUser.username = resultObject.getString("userName");
            authUser.nickname = resultObject.getString("userName");
            authUser.avatar = resultObject.getString("avatar");
            authUser.uuid = resultObject.getString("openId");
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = resultObject;
            authUser.originalUserStr = response;
            return authUser;
        }

        /**
         * 获取酷家乐的openId，此id在当前client范围内可以唯一识别授权用户
         */
        private string getOpenId(AuthToken authToken)
        {
            string response = HttpUtils.RequestGet(UrlBuilder.fromBaseUrl("https://oauth.kujiale.com/oauth2/auth/user")
                .queryParam("access_token", authToken.accessToken)
                .build(), null, config.httpConfig);
            var accessTokenObject = checkResponse(response);
            return accessTokenObject.getString("d");
        }

        public override AuthResponse refresh(AuthToken authToken)
        {
            string response = HttpUtils.RequestPost(refreshTokenUrl(authToken.refreshToken), null, null, null, config.httpConfig);
            return new AuthResponse(Convert.ToInt32(AuthResponseStatus.SUCCESS), null, getAuthToken(response));
        }
    }
}
