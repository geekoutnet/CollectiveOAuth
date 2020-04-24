using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;
using Newtonsoft.Json;

namespace Come.CollectiveOAuth.Request
{
    public class MicrosoftAuthRequest : DefaultAuthRequest
    {
        public MicrosoftAuthRequest(ClientConfig config) : base(config, new MicrosoftAuthSource())
        {
        }

        public MicrosoftAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new MicrosoftAuthSource(), authStateCache)
        {
        }
        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            return getToken(accessTokenUrl(authCallback.code));
        }

        /**
         * 获取token，适用于获取access_token和刷新token
         *
         * @param accessTokenUrl 实际请求token的地址
         * @return token对象
         */
        private AuthToken getToken(string accessTokenUrl)
        {
            var reqParams = new Dictionary<string, object>
            {
                { "Host", "https://login.microsoftonline.com" },
                { "Content-Type", "application/x-www-form-urlencoded" },
            };

            var reqParamDic = GlobalAuthUtil.parseUrlObject(accessTokenUrl);
            var response = HttpUtils.RequestPost(accessTokenUrl, JsonConvert.SerializeObject(reqParamDic), reqParams);
            var accessTokenObject = response.parseObject();

            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.tokenType = accessTokenObject.getString("token_type");
            authToken.expireIn = accessTokenObject.getInt32("expires_in");
            authToken.refreshToken = accessTokenObject.getString("refresh_token");
            authToken.scope = accessTokenObject.getString("scope");

            return authToken;
        }


        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            var token = authToken.accessToken;
            var tokenType = authToken.tokenType;
            var jwt = tokenType + " " + token;
            var reqParams = new Dictionary<string, object>
            {
                { "Authorization", jwt },
            };

            var response = HttpUtils.RequestGet(userInfoUrl(authToken), reqParams);
            var userObj = response.parseObject();
            this.checkResponse(userObj);

            var authUser = new AuthUser();
            authUser.uuid = userObj.getString("id");
            authUser.username = userObj.getString("userPrincipalName");
            authUser.nickname = userObj.getString("displayName");
            authUser.location = userObj.getString("officeLocation");
            authUser.email = userObj.getString("mail");
            authUser.gender = AuthUserGender.UNKNOWN;

            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = userObj;
            authUser.originalUserStr = response;
            return authUser;
        }

        /**
         * 刷新access token （续期）
         *
         * @param authToken 登录成功后返回的Token信息
         * @return AuthResponse
         */
        public override AuthResponse refresh(AuthToken authToken)
        {
            var token = getToken(refreshTokenUrl(authToken.refreshToken));
            return new AuthResponse(AuthResponseStatus.SUCCESS.GetCode(), AuthResponseStatus.SUCCESS.GetDesc(), token);
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
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("response_mode", "query")
                .queryParam("scope", "offline_access%20" + (config.scope.IsNullOrWhiteSpace() ? "user.read%20mail.read" : config.scope))
                .queryParam("state", getRealState(state))
                .build();
        }

        /**
         * 返回获取accessToken的url
         *
         * @param code 授权code
         * @return 返回获取accessToken的url
         */
        protected override string accessTokenUrl(string code)
        {
            return UrlBuilder.fromBaseUrl(source.accessToken())
                .queryParam("code", code)
                .queryParam("client_id", config.clientId)
                .queryParam("client_secret", config.clientSecret)
                .queryParam("grant_type", "authorization_code")
                .queryParam("scope", config.scope.IsNullOrWhiteSpace() ? "user.read%20mail.read" : config.scope)
                .queryParam("redirect_uri", config.redirectUri)
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
            return UrlBuilder.fromBaseUrl(source.userInfo()).build();
        }

        /**
         * 返回获取accessToken的url
         *
         * @param refreshToken 用户授权后的token
         * @return 返回获取accessToken的url
         */
        protected override string refreshTokenUrl(string refreshToken)
        {
            return UrlBuilder.fromBaseUrl(source.refresh())
                .queryParam("client_id", config.clientId)
                .queryParam("client_secret", config.clientSecret)
                .queryParam("refresh_token", refreshToken)
                .queryParam("grant_type", "refresh_token")
                .queryParam("scope", config.scope.IsNullOrWhiteSpace() ? "user.read%20mail.read" : config.scope)
                .queryParam("redirect_uri", config.redirectUri)
                .build();
        }


        /**
         * 检查响应内容是否正确
         *
         * @param object 请求响应内容
         */
        private void checkResponse(Dictionary<string, object> dic)
        {
            if (dic.ContainsKey("error"))
            {
                throw new Exception(dic.getString("error_description"));
            }
        }
    }
}