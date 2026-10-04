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
     * Gitee登录，对齐 JustAuth AuthGiteeRequest
     */
    public class GiteeAuthRequest : DefaultAuthRequest
    {
        public GiteeAuthRequest(ClientConfig config) : base(config, new GiteeAuthSource())
        {
        }

        public GiteeAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new GiteeAuthSource(), authStateCache)
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
            authToken.scope = accessTokenObject.getString("scope");
            authToken.tokenType = accessTokenObject.getString("token_type");
            authToken.expireIn = accessTokenObject.getIntValue("expires_in");
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string userInfo = doGetUserInfo(authToken);
            var obj = JsonExtensions.parseObject(userInfo);
            this.checkResponse(obj);

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.uuid = obj.getString("id");
            authUser.username = obj.getString("login");
            authUser.avatar = obj.getString("avatar_url");
            authUser.blog = obj.getString("blog");
            authUser.nickname = obj.getString("name");
            authUser.company = obj.getString("company");
            authUser.location = obj.getString("address");
            authUser.email = obj.getString("email");
            authUser.remark = obj.getString("bio");
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = userInfo;
            return authUser;
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
         * 返回带{@code state}参数的授权url，授权回调时会带上这个{@code state}
         *
         * @param state state 验证授权流程的参数，可以防止csrf
         * @return 返回授权地址
         */
        public override string authorize(string state)
        {
            // Java 版传 encode=true 后交给不编码的 UrlBuilder；C# UrlBuilder 默认编码，这里传原值
            return UrlBuilder.fromBaseUrl(base.authorize(state))
                .queryParam("scope", this.getScopes(" ", false, AuthScopeUtils.getDefaultScopes(AuthGiteeScope.values())))
                .build();
        }
    }
}
