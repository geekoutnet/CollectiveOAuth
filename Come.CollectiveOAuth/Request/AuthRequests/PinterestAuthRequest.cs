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
     * Pinterest登录，对齐 JustAuth AuthPinterestRequest
     */
    public class PinterestAuthRequest : DefaultAuthRequest
    {
        private const string FAILURE = "failure";

        public PinterestAuthRequest(ClientConfig config) : base(config, new PinterestAuthSource())
        {
        }

        public PinterestAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new PinterestAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string response = doPostAuthorizationCode(authCallback.code);
            var accessTokenObject = JsonExtensions.parseObject(response);
            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.tokenType = accessTokenObject.getString("token_type");
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string userinfoUrl = userInfoUrl(authToken);
            string response = HttpUtils.RequestGet(userinfoUrl, null, config.httpConfig);
            var obj = JsonExtensions.parseObject(response);
            this.checkResponse(obj);
            var userObj = obj.getJSONObject("data");

            var authUser = new AuthUser();
            authUser.rawUserInfo = userObj;
            authUser.uuid = userObj.getString("id");
            authUser.avatar = getAvatarUrl(userObj);
            authUser.username = userObj.getString("username");
            // 与 Java 字符串拼接一致：null 输出为 "null"
            authUser.nickname = (userObj.getString("first_name") ?? "null") + " " + (userObj.getString("last_name") ?? "null");
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.remark = userObj.getString("bio");
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = userObj;
            authUser.originalUserStr = response;
            return authUser;
        }

        private string getAvatarUrl(JObject userObj)
        {
            // image is a map data structure
            var jsonObject = userObj.getJSONObject("image");
            if (jsonObject == null)
            {
                return null;
            }
            return jsonObject.getJSONObject("60x60").getString("url");
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
                .queryParam("scope", this.getScopes(",", false, AuthScopeUtils.getDefaultScopes(AuthPinterestScope.values())))
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
            return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("access_token", authToken.accessToken)
                .queryParam("fields", "id,username,first_name,last_name,bio,image")
                .build();
        }

        /**
         * 检查响应内容是否正确
         * <p>
         * 注意：与 JustAuth 1.16.7 原样一致，该条件恒为 false（不含 status 时 status 不可能等于 failure），即实际不会抛出异常
         *
         * @param object 请求响应内容
         */
        private void checkResponse(JObject obj)
        {
            if (!obj.containsKey("status") && FAILURE.Equals(obj.getString("status")))
            {
                throw new AuthException(obj.getString("message"));
            }
        }
    }
}
