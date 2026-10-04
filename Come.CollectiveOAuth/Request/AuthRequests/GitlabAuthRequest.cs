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
     * Gitlab登录，对齐 JustAuth AuthGitlabRequest
     */
    public class GitlabAuthRequest : DefaultAuthRequest
    {
        public GitlabAuthRequest(ClientConfig config) : base(config, new GitlabAuthSource())
        {
        }

        public GitlabAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new GitlabAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string response = doPostAuthorizationCode(authCallback.code);
            var obj = JsonExtensions.parseObject(response);

            this.checkResponse(obj);

            var authToken = new AuthToken();
            authToken.accessToken = obj.getString("access_token");
            authToken.refreshToken = obj.getString("refresh_token");
            authToken.idToken = obj.getString("id_token");
            authToken.tokenType = obj.getString("token_type");
            authToken.scope = obj.getString("scope");
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
            authUser.username = obj.getString("username");
            authUser.nickname = obj.getString("name");
            authUser.avatar = obj.getString("avatar_url");
            authUser.blog = obj.getString("web_url");
            authUser.company = obj.getString("organization");
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

        private void checkResponse(JObject obj)
        {
            // oauth/token 验证异常
            if (obj.containsKey("error"))
            {
                throw new AuthException(obj.getString("error_description"));
            }
            // user 验证异常
            if (obj.containsKey("message"))
            {
                throw new AuthException(obj.getString("message"));
            }
        }

        /**
         * 返回带{@code state}参数的授权url，授权回调时会带上这个{@code state}
         *
         * @param state state 验证授权流程的参数，可以防止csrf
         * @return 返回授权地址
         * @since 1.11.0
         */
        public override string authorize(string state)
        {
            // Java 以未编码的 "+" 连接（服务端按空格解析）；C# UrlBuilder 会编码参数值，"+" 会变成 %2B，
            // 这里改用空格连接，编码后服务端收到的同样是以空格分隔的 scope
            return UrlBuilder.fromBaseUrl(base.authorize(state))
                .queryParam("scope", this.getScopes(" ", false, AuthScopeUtils.getDefaultScopes(AuthGitlabScope.values())))
                .build();
        }
    }
}
