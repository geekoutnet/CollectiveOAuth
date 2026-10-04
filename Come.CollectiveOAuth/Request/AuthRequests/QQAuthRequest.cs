using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Enums.Scope;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;

namespace Come.CollectiveOAuth.Request
{
    /**
     * QQ登录，对齐 JustAuth AuthQqRequest
     */
    public class QQAuthRequest : DefaultAuthRequest
    {
        public QQAuthRequest(ClientConfig config) : base(config, new QQAuthSource())
        {
        }

        public QQAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new QQAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string response = doGetAuthorizationCode(authCallback.code);
            return getAuthToken(response);
        }

        public override AuthResponse refresh(AuthToken authToken)
        {
            string response = HttpUtils.RequestGet(refreshTokenUrl(authToken.refreshToken), null, config.httpConfig);
            return new AuthResponse(AuthResponseStatus.SUCCESS.GetCode(), null, getAuthToken(response));
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string openId = this.getOpenId(authToken);
            string response = doGetUserInfo(authToken);
            var obj = JsonExtensions.parseObject(response);
            if (obj.getIntValue("ret") != 0)
            {
                throw new AuthException(obj.getString("msg"));
            }
            string avatar = obj.getString("figureurl_qq_2");
            if (string.IsNullOrEmpty(avatar))
            {
                avatar = obj.getString("figureurl_qq_1");
            }

            // 与 Java String.format("%s-%s", ...) 一致：null 输出为 "null"
            string location = string.Format("{0}-{1}", GlobalAuthUtil.javaValueOf(obj.getString("province")), GlobalAuthUtil.javaValueOf(obj.getString("city")));

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.username = obj.getString("nickname");
            authUser.nickname = obj.getString("nickname");
            authUser.avatar = avatar;
            authUser.location = location;
            authUser.uuid = openId;
            authUser.gender = GlobalAuthUtil.getRealGender(obj.getString("gender"));
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = response;
            return authUser;
        }

        /**
         * 获取QQ用户的OpenId，支持自定义是否启用查询unionid的功能，如果启用查询unionid的功能，
         * 那就需要开发者先通过邮件申请unionid功能，参考链接 {@see http://wiki.connect.qq.com/unionid%E4%BB%8B%E7%BB%8D}
         *
         * @param authToken 通过{@link AuthQqRequest#getAccessToken(AuthCallback)}获取到的{@code authToken}
         * @return openId
         */
        private string getOpenId(AuthToken authToken)
        {
            string response = HttpUtils.RequestGet(UrlBuilder.fromBaseUrl("https://graph.qq.com/oauth2.0/me")
                .queryParam("access_token", authToken.accessToken)
                .queryParam("unionid", config.isUnionId() ? 1 : 0)
                .build(), null, config.httpConfig);
            string removePrefix = response.Replace("callback(", "");
            string removeSuffix = removePrefix.Replace(");", "");
            string openId = removeSuffix.Trim();
            var obj = JsonExtensions.parseObject(openId);
            if (obj.containsKey("error"))
            {
                throw new AuthException(GlobalAuthUtil.javaValueOf(obj.getString("error")) + ":" + GlobalAuthUtil.javaValueOf(obj.getString("error_description")));
            }
            authToken.openId = obj.getString("openid");
            if (obj.containsKey("unionid"))
            {
                authToken.unionId = obj.getString("unionid");
            }
            return string.IsNullOrEmpty(authToken.unionId) ? authToken.openId : authToken.unionId;
        }

        /**
         * 返回获取userInfo的url
         *
         * @param authToken 用户授权token
         * @return 返回获取userInfo的url
         */
        protected override string userInfoUrl(AuthToken authToken)
        {
            return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("access_token", authToken.accessToken)
                .queryParam("oauth_consumer_key", config.clientId)
                .queryParam("openid", authToken.openId)
                .build();
        }

        private AuthToken getAuthToken(string response)
        {
            var accessTokenObject = GlobalAuthUtil.parseStringToMap(response);
            if (!accessTokenObject.ContainsKey("access_token") || accessTokenObject.ContainsKey("code"))
            {
                throw new AuthException(accessTokenObject.TryGetValue("msg", out var msg) ? msg : null);
            }
            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject["access_token"];
            authToken.expireIn = int.Parse(accessTokenObject.TryGetValue("expires_in", out var expiresIn) ? expiresIn : "0");
            authToken.refreshToken = accessTokenObject.TryGetValue("refresh_token", out var refreshToken) ? refreshToken : null;
            return authToken;
        }

        public override string authorize(string state)
        {
            return UrlBuilder.fromBaseUrl(base.authorize(state))
                .queryParam("scope", this.getScopes(",", false, AuthScopeUtils.getDefaultScopes(AuthQqScope.values())))
                .build();
        }
    }
}
