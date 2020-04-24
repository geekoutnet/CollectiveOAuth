using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Request
{
    public class OschinaAuthRequest : DefaultAuthRequest
    {
        public OschinaAuthRequest(ClientConfig config) : base(config, new OschinaAuthSource())
        {
        }

        public OschinaAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new OschinaAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string response = doPostAuthorizationCode(authCallback.code);
            var accessTokenObject = response.parseObject();
            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.refreshToken = accessTokenObject.getString("refresh_token");
            authToken.uid = accessTokenObject.getString("uid");
            authToken.expireIn = accessTokenObject.getInt32("expires_in");
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
            authUser.username = userObj.getString("name");
            authUser.nickname = userObj.getString("name");
            authUser.avatar = userObj.getString("avatar");
            authUser.blog = userObj.getString("url");
            authUser.location = userObj.getString("location");
            authUser.email = userObj.getString("email");
            authUser.gender = GlobalAuthUtil.getRealGender(userObj.getString("gender"));

            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = userObj;
            authUser.originalUserStr = response;
            return authUser;
        }

        /**
         * 返回获取accessToken的url
         *
         * @param code 授权回调时带回的授权码
         * @return 返回获取accessToken的url
         */
        protected override string accessTokenUrl(string code)
        {
            return UrlBuilder.fromBaseUrl(source.accessToken())
                .queryParam("code", code)
                .queryParam("client_id", config.clientId)
                .queryParam("client_secret", config.clientSecret)
                .queryParam("grant_type", "authorization_code")
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("dataType", "json")
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
                .queryParam("dataType", "json")
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