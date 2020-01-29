using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Request
{
    public class DouyinAuthRequest : DefaultAuthRequest
    {
        public DouyinAuthRequest(ClientConfig config) : base(config, new DouyinAuthSource())
        {
        }

        public DouyinAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new DouyinAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            return this.getToken(accessTokenUrl(authCallback.code));
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string response = doGetUserInfo(authToken);
            var userInfoObject = response.parseObject();
            this.checkResponse(userInfoObject);
            var userObj = userInfoObject.getString("data").parseObject();

            var location = $"{userObj.getString("country")}-{userObj.getString("province")}-{userObj.getString("city")}";
            var authUser = new AuthUser();
            authUser.uuid = userObj.getString("union_id");
            authUser.username = userObj.getString("nickname");
            authUser.nickname = userObj.getString("nickname");
            authUser.avatar = userObj.getString("avatar");
            authUser.location = location;
            authUser.remark = userObj.getString("description");
            authUser.gender = GlobalAuthUtil.getRealGender(userObj.getString("gender"));

            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = userObj;
            authUser.originalUserStr = response;
            return authUser;
        }

        public override AuthResponse refresh(AuthToken oldToken)
        {
            var data = getToken(refreshTokenUrl(oldToken.refreshToken));
            return new AuthResponse(AuthResponseStatus.SUCCESS.GetCode(), AuthResponseStatus.SUCCESS.GetDesc(), data);
        }


        /**
         * 获取token，适用于获取access_token和刷新token
         *
         * @param accessTokenUrl 实际请求token的地址
         * @return token对象
         */
        private AuthToken getToken(string accessTokenUrl)
        {
            var response = HttpUtils.RequestPost(accessTokenUrl);
            string accessTokenStr = response;
            var tokenObj = accessTokenStr.parseObject();
            this.checkResponse(tokenObj);
            var accessTokenObject = tokenObj.getString("data").parseObject();

            var authToken = new AuthToken
            {
                accessToken = accessTokenObject.getString("access_token"),
                openId = accessTokenObject.getString("open_id"),
                expireIn = accessTokenObject.getInt32("token_type"),
                refreshToken = accessTokenObject.getString("refresh_token"),
                scope = accessTokenObject.getString("scope")
            };

            return authToken;
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
                .queryParam("client_key", config.clientId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("scope", config.scope.IsNullOrWhiteSpace() ? "user_info" : config.scope)
                .queryParam("state", getRealState(state))
                .build();
        }

        /**
         * 返回获取accessToken的url
         *
         * @param code oauth的授权码
         * @return 返回获取accessToken的url
         */
        protected override string accessTokenUrl(string code)
        {
            return UrlBuilder.fromBaseUrl(source.accessToken())
                .queryParam("code", code)
                .queryParam("client_key", config.clientId)
                .queryParam("client_secret", config.clientSecret)
                .queryParam("grant_type", "authorization_code")
                .build();
        }

        /**
         * 返回获取userInfo的url
         *
         * @param authToken oauth返回的token
         * @return 返回获取userInfo的url
         */
        protected override string userInfoUrl(AuthToken authToken)
        {
            return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("access_token", authToken.accessToken)
                .queryParam("open_id", authToken.openId)
                .build();
        }

        /**
         * 返回获取accessToken的url
         *
         * @param refreshToken oauth返回的refreshtoken
         * @return 返回获取accessToken的url
         */
        protected override string refreshTokenUrl(string refreshToken)
        {
            return UrlBuilder.fromBaseUrl(source.refresh())
                .queryParam("client_key", config.clientId)
                .queryParam("refresh_token", refreshToken)
                .queryParam("grant_type", "refresh_token")
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
            string message = dic.getString("message");
            var data = dic.getString("data").parseObject();
            int errorCode = data.getInt32("error_code");
            if ("error".Equals(message) || errorCode != 0)
            {
                throw new Exception(data.getString("description"));
            }
        }
    }
}