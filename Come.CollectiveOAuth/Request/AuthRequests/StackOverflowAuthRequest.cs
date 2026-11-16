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
     * Stack Overflow登录，对齐 JustAuth AuthStackOverflowRequest
     */
    public class StackOverflowAuthRequest : DefaultAuthRequest
    {
        public StackOverflowAuthRequest(ClientConfig config) : base(config, new StackOverflowAuthSource())
        {
        }

        public StackOverflowAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new StackOverflowAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string accessTokenUrl = this.accessTokenUrl(authCallback.code);
            // 对应 MapUtil.parseStringToMap(accessTokenUrl, false)
            var form = GlobalAuthUtil.parseQueryToMap(accessTokenUrl);
            var httpHeader = new Dictionary<string, object> { { "Content-Type", "application/x-www-form-urlencoded" } };
            string response = HttpUtils.RequestFormPost(accessTokenUrl, GlobalAuthUtil.parseMapToString(form, true), httpHeader, null, config.httpConfig);

            var accessTokenObject = JsonExtensions.parseObject(response);
            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.expireIn = accessTokenObject.getIntValue("expires");
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string userInfoUrl = UrlBuilder.fromBaseUrl(this.source.userInfo())
                .queryParam("access_token", authToken.accessToken)
                .queryParam("site", "stackoverflow")
                .queryParam("key", this.config.stackOverflowKey)
                .build();
            string response = HttpUtils.RequestGet(userInfoUrl, null, config.httpConfig);
            var obj = JsonExtensions.parseObject(response);
            this.checkResponse(obj);
            var items = obj.getJSONArray("items");
            if (items == null || items.Count == 0 || !(items[0] is JObject userObj))
            {
                throw new AuthException("Failed to get user info from Stack Overflow: items is empty");
            }

            var authUser = new AuthUser();
            authUser.rawUserInfo = userObj;
            authUser.uuid = userObj.getString("user_id");
            authUser.avatar = userObj.getString("profile_image");
            authUser.location = userObj.getString("location");
            authUser.nickname = userObj.getString("display_name");
            authUser.blog = userObj.getString("website_url");
            authUser.gender = AuthUserGender.UNKNOWN;
            fillAuthUser(authUser, authToken, userObj, response);
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
                .queryParam("scope", this.getScopes(",", false, AuthScopeUtils.getDefaultScopes(AuthStackoverflowScope.values())))
                .build();
        }

        /**
         * 检查响应内容是否正确
         *
         * @param object 请求响应内容
         */
        private void checkResponse(JObject obj)
        {
            checkErrorDescription(obj);
            // StackExchange API 错误结构：{"error_id":401,"error_message":"...","error_name":"..."}
            if (obj.containsKey("error_id"))
            {
                throw new AuthException(obj.getIntValue("error_id"), obj.getString("error_message") ?? obj.getString("error_name"));
            }
        }
    }
}
