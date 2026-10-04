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
            var form = parseQueryToMap(accessTokenUrl);
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
            var userObj = (JObject)obj.getJSONArray("items")[0];

            var authUser = new AuthUser();
            authUser.rawUserInfo = userObj;
            authUser.uuid = userObj.getString("user_id");
            authUser.avatar = userObj.getString("profile_image");
            authUser.location = userObj.getString("location");
            authUser.nickname = userObj.getString("display_name");
            authUser.blog = userObj.getString("website_url");
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = userObj;
            authUser.originalUserStr = response;
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
            if (obj.containsKey("error"))
            {
                throw new AuthException(obj.getString("error_description"));
            }
        }

        /**
         * 对应 simple-http MapUtil.parseStringToMap(url, false)：取 ? 之后、# 之前的参数，同名参数以逗号拼接；
         * C# 的 UrlBuilder 已对参数值编码，这里解码还原为原值
         */
        private static Dictionary<string, string> parseQueryToMap(string url)
        {
            var map = new Dictionary<string, string>();
            int index = url.IndexOf('?');
            string query = index > -1 ? url.Substring(index + 1) : url;
            int hash = query.IndexOf('#');
            if (hash > -1)
            {
                query = query.Substring(0, hash);
            }
            foreach (var part in query.Split('&'))
            {
                if (part.Length == 0)
                {
                    continue;
                }
                int eq = part.IndexOf('=');
                string key = GlobalAuthUtil.urlDecode(eq < 0 ? part : part.Substring(0, eq));
                string value = eq < 0 ? "" : GlobalAuthUtil.urlDecode(part.Substring(eq + 1));
                map[key] = map.TryGetValue(key, out var old) ? old + "," + value : value;
            }
            return map;
        }
    }
}
