using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Enums.Scope;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System.Collections.Generic;

namespace Come.CollectiveOAuth.Request
{
    /**
     * Github登录，对齐 JustAuth AuthGithubRequest
     */
    public class GithubAuthRequest : DefaultAuthRequest
    {
        public GithubAuthRequest(ClientConfig config) : base(config, new GithubAuthSource())
        {
        }

        public GithubAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new GithubAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string response = doPostAuthorizationCode(authCallback.code);
            var res = GlobalAuthUtil.parseStringToMap(response);
            this.checkResponse(res.ContainsKey("error"), res.TryGetValue("error_description", out var description) ? description : null);

            var authToken = new AuthToken();
            authToken.accessToken = res.TryGetValue("access_token", out var accessToken) ? accessToken : null;
            authToken.scope = res.TryGetValue("scope", out var scope) ? scope : null;
            authToken.tokenType = res.TryGetValue("token_type", out var tokenType) ? tokenType : null;
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            var header = new Dictionary<string, object> { { "Authorization", "token " + authToken.accessToken } };
            string response = HttpUtils.RequestGet(UrlBuilder.fromBaseUrl(source.userInfo()).build(), header, config.httpConfig);
            var obj = JsonExtensions.parseObject(response);
            this.checkResponse(obj.containsKey("error"), obj.getString("error_description"));

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.uuid = obj.getString("id");
            authUser.username = obj.getString("login");
            authUser.avatar = obj.getString("avatar_url");
            authUser.blog = obj.getString("blog");
            authUser.nickname = obj.getString("name");
            authUser.company = obj.getString("company");
            authUser.location = obj.getString("location");
            authUser.email = obj.getString("email");
            authUser.remark = obj.getString("bio");
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = response;
            return authUser;
        }

        private void checkResponse(bool error, string errorDescription)
        {
            if (error)
            {
                throw new AuthException(errorDescription);
            }
        }

        public override string authorize(string state)
        {
            // Java 版传 encode=true 后交给不编码的 UrlBuilder；C# UrlBuilder 默认编码，这里传原值
            return UrlBuilder.fromBaseUrl(base.authorize(state))
                .queryParam("scope", this.getScopes(" ", false, AuthScopeUtils.getDefaultScopes(AuthGithubScope.values())))
                .build();
        }
    }
}
