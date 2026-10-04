using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Enums.Scope;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json.Linq;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 百度登录，对齐 JustAuth AuthBaiduRequest
     */
    public class BaiduAuthRequest : DefaultAuthRequest
    {
        public BaiduAuthRequest(ClientConfig config) : base(config, new BaiduAuthSource())
        {
        }

        public BaiduAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new BaiduAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string response = doPostAuthorizationCode(authCallback.code);
            return getAuthToken(response);
        }

        /**
         * 新旧应用返回的用户信息不一致：旧应用返回 userid，新应用返回 openid
         *
         * @param authToken token信息
         * @return AuthUser
         */
        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string userInfo = doGetUserInfo(authToken);
            var obj = JsonExtensions.parseObject(userInfo);
            this.checkResponse(obj);

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.uuid = obj.containsKey("userid") ? obj.getString("userid") : obj.getString("openid");
            authUser.username = obj.getString("username");
            authUser.nickname = obj.getString("username");
            authUser.avatar = getAvatar(obj);
            authUser.remark = obj.getString("userdetail");
            authUser.gender = GlobalAuthUtil.getRealGender(obj.getString("sex"));
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = userInfo;
            return authUser;
        }

        private string getAvatar(JObject obj)
        {
            string protrait = obj.getString("portrait");
            return string.IsNullOrEmpty(protrait) ? null : string.Format("http://himg.bdimg.com/sys/portrait/item/{0}.jpg", protrait);
        }

        public override AuthResponse revoke(AuthToken authToken)
        {
            string response = doGetRevoke(authToken);
            var obj = JsonExtensions.parseObject(response);
            this.checkResponse(obj);
            // 返回1表示取消授权成功，否则失败
            AuthResponseStatus status = obj.getIntValue("result") == 1 ? AuthResponseStatus.SUCCESS : AuthResponseStatus.FAILURE;
            return new AuthResponse(status.GetCode(), status.GetDesc());
        }

        public override AuthResponse refresh(AuthToken authToken)
        {
            string refreshUrl = UrlBuilder.fromBaseUrl(this.source.refresh())
                .queryParam("grant_type", "refresh_token")
                .queryParam("refresh_token", authToken.refreshToken)
                .queryParam("client_id", this.config.clientId)
                .queryParam("client_secret", this.config.clientSecret)
                .build();
            string response = HttpUtils.RequestGet(refreshUrl, null, config.httpConfig);
            return new AuthResponse(AuthResponseStatus.SUCCESS.GetCode(), null, this.getAuthToken(response));
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
                .queryParam("display", "popup")
                .queryParam("scope", this.getScopes(" ", false, AuthScopeUtils.getDefaultScopes(AuthBaiduScope.values())))
                .build();
        }

        /**
         * 检查响应内容是否正确
         *
         * @param object 请求响应内容
         */
        private void checkResponse(JObject obj)
        {
            if (obj.containsKey("error") || obj.containsKey("error_code"))
            {
                string msg = obj.containsKey("error_description") ? obj.getString("error_description") : obj.getString("error_msg");
                throw new AuthException(msg);
            }
        }

        private AuthToken getAuthToken(string response)
        {
            var accessTokenObject = JsonExtensions.parseObject(response);
            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.refreshToken = accessTokenObject.getString("refresh_token");
            authToken.scope = accessTokenObject.getString("scope");
            authToken.expireIn = accessTokenObject.getIntValue("expires_in");
            return authToken;
        }
    }
}
