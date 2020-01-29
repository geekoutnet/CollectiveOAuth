using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Request
{
    public class HuaweiAuthRequest : DefaultAuthRequest
    {
        public HuaweiAuthRequest(ClientConfig config) : base(config, new HuaweiAuthSource())
        {
        }

        public HuaweiAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new HuaweiAuthSource(), authStateCache)
        {
        }

        /**
        * 获取access token
        *
        * @param authCallback 授权成功后的回调参数
        * @return token
        * @see AuthDefaultRequest#authorize()
        * @see AuthDefaultRequest#authorize(String)
        */
        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            var reqParams = new Dictionary<string, object>
            {
                { "grant_type", "authorization_code" },
                { "code", authCallback.authorization_code },
                { "client_id", config.clientId },
                { "client_secret", config.clientSecret },
                { "redirect_uri", config.redirectUri },
            };

            var response = HttpUtils.RequestFormPost(source.accessToken(), reqParams.spellParams());

            return getAuthToken(response);
        }

        /**
         * 使用token换取用户信息
         *
         * @param authToken token信息
         * @return 用户信息
         * @see AuthDefaultRequest#getAccessToken(AuthCallback)
         */
        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            var reqParams = new Dictionary<string, object>
            {
                { "nsp_ts", DateTime.Now.Ticks },
                { "access_token", authToken.accessToken },
                { "nsp_fmt", "JS" },
                { "nsp_svc", "OpenUP.User.getInfo" },
            };

            var response = HttpUtils.RequestFormPost(source.userInfo(), reqParams.spellParams());
            var userObj = response.parseObject();

            this.checkResponse(userObj);

            AuthUserGender gender = getRealGender(userObj);

            var authUser = new AuthUser();
            authUser.uuid = userObj.getString("userID");
            authUser.username = userObj.getString("userName");
            authUser.nickname = userObj.getString("userName");
            authUser.gender = gender;
            authUser.avatar = userObj.getString("headPictureURL");
          
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
            var reqParams = new Dictionary<string, object>
            {
                { "client_id", config.clientId },
                { "client_secret", config.clientSecret },
                { "refresh_token", authToken.refreshToken },
                { "grant_type", "refresh_token" },
            };
            var response = HttpUtils.RequestFormPost(source.refresh(), reqParams.spellParams());

            return new AuthResponse(AuthResponseStatus.SUCCESS.GetCode(), AuthResponseStatus.SUCCESS.GetDesc(), getAuthToken(response));
        }

        private AuthToken getAuthToken(string response)
        {
            var authTokenObj = response.parseObject();

            this.checkResponse(authTokenObj);

            var authToken = new AuthToken();
            authToken.accessToken = authTokenObj.getString("access_token");
            authToken.refreshToken = authTokenObj.getString("refresh_token");
            authToken.expireIn = authTokenObj.getInt32("expires_in");
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
                .queryParam("client_id", config.clientId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("access_type", "offline")
                .queryParam("scope", config.scope.IsNullOrWhiteSpace() ? "https%3A%2F%2Fwww.huawei.com%2Fauth%2Faccount%2Fbase.profile" : config.scope)
                .queryParam("state", getRealState(state))
                .build();
        }

        /**
         * 返回获取accessToken的url
         *
         * @param code 授权码
         * @return 返回获取accessToken的url
         */
        protected override string accessTokenUrl(string code)
        {
            return UrlBuilder.fromBaseUrl(source.accessToken())
                .queryParam("grant_type", "authorization_code")
                .queryParam("code", code)
                .queryParam("client_id", config.clientId)
                .queryParam("client_secret", config.clientSecret)
                .queryParam("redirect_uri", config.redirectUri)
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
                .queryParam("nsp_ts", DateTime.Now.Ticks)
                .queryParam("access_token", authToken.accessToken)
                .queryParam("nsp_fmt", "JS")
                .queryParam("nsp_svc", "OpenUP.User.getInfo")
                .build();
        }

        /**
         * 获取用户的实际性别。华为系统中，用户的性别：1表示女，0表示男
         *
         * @param object obj
         * @return AuthUserGender
         */
        private AuthUserGender getRealGender(Dictionary<string, object> userObj)
        {
            int genderCodeInt = userObj.getInt32("gender");
            string genderCode = genderCodeInt == 1 ? "0" : (genderCodeInt == 0) ? "1" : genderCodeInt + "";
            return GlobalAuthUtil.getRealGender(genderCode);
        }

        /**
         * 校验响应结果
         *
         * @param object 接口返回的结果
         */
        private void checkResponse(Dictionary<string, object> dic)
        {
            if (dic.ContainsKey("NSP_STATUS"))
            {
                throw new Exception(dic.getString("error"));
            }
            if (dic.ContainsKey("error"))
            {
                throw new Exception(dic.getString("sub_error") + ":" + dic.getString("error_description"));
            }
        }
    }
}