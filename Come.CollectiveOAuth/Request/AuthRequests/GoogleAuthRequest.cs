using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Request
{
    public class GoogleAuthRequest : DefaultAuthRequest
    {
        public GoogleAuthRequest(ClientConfig config) : base(config, new GoogleAuthSource())
        {
        }

        public GoogleAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new GoogleAuthSource(), authStateCache)
        {
        }
        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            var response = doPostAuthorizationCode(authCallback.code);
            var accessTokenObject = response.parseObject();
            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.expireIn = accessTokenObject.getInt32("expires_in");
            authToken.idToken = accessTokenObject.getString("id_token");
            authToken.tokenType = accessTokenObject.getString("token_type");
            authToken.scope = accessTokenObject.getString("scope");

            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            var reqParams = new Dictionary<string, object>
            {
                { "Authorization", "Bearer " + authToken.accessToken }
            };
            var response = HttpUtils.RequestPost(userInfoUrl(authToken), null, reqParams);
            var userInfo = response;
            var userObj = userInfo.parseObject();
            this.checkResponse(userObj);

            var authUser = new AuthUser();
            authUser.uuid = userObj.getString("sub");
            authUser.username = userObj.getString("email");
            authUser.nickname = userObj.getString("name");
            authUser.avatar = userObj.getString("picture");
            authUser.location = userObj.getString("locale");
            authUser.email = userObj.getString("email");
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
            return UrlBuilder.fromBaseUrl(source.authorize())
                .queryParam("response_type", "code")
                .queryParam("client_id", config.clientId)
                .queryParam("scope", config.scope.IsNullOrWhiteSpace() ? "openid%20email%20profile" : config.scope)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("state", getRealState(state))
                .build();
        }

        /**
         * 返回获取userInfo的url
         *
         * @param authToken 用户授权后的token
         * @return 返回获取userInfo的url
         */
        protected override string userInfoUrl(AuthToken authToken)
        {
            return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("access_token", authToken.accessToken)
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
            if (dic.ContainsKey("error") || dic.ContainsKey("error_description"))
            {
                throw new Exception($"{dic.getString("error")}: {dic.getString("error_description")}");
            }
        }
    }
}