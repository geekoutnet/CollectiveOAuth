using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Request
{
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
            var accessTokenObject = response.parseStringObject();
            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.tokenType = accessTokenObject.getString("token_type");
            authToken.scope = accessTokenObject.getString("scope");
            authToken.code = authCallback.code;

            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string response = doGetUserInfo(authToken);
            var userObj = response.parseObject();
            this.checkResponse(userObj);

            var authUser = new AuthUser();
            authUser.uuid = userObj.getString("id");
            authUser.username = userObj.getString("login");
            authUser.nickname = userObj.getString("name");
            authUser.avatar = userObj.getString("avatar_url");
            authUser.blog = userObj.getString("blog");
            authUser.company = userObj.getString("company");
            authUser.location = userObj.getString("location");
            authUser.email = userObj.getString("email");
            authUser.remark = userObj.getString("bio");
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = userObj;
            authUser.originalUserStr = response;
            return authUser;
        }

        /// <summary>
        /// 重写获取用户信息方法
        /// </summary>
        /// <param name="authToken"></param>
        /// <returns></returns>
        protected override string doGetUserInfo(AuthToken authToken)
        {
            return HttpUtils.RequestJsonGet(userInfoUrl(authToken));
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
            return UrlBuilder.fromBaseUrl(source.authorize())
                .queryParam("client_id", config.clientId)
                .queryParam("response_type", "code")
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("scope", config.scope.IsNullOrWhiteSpace() ? "user" : config.scope)
                .queryParam("state", getRealState(state) + "#wechat_redirect")
                .build();
        }

        /**
       * 校验请求结果
       *
       * @param response 请求结果
       * @return 如果请求结果正常，则返回Exception
       */
        private void checkResponse(Dictionary<string, object> dic)
        {
            if (dic.ContainsKey("error"))
            {
                throw new Exception($"{dic.getString("error_description")}");
            }
        }
    }
}