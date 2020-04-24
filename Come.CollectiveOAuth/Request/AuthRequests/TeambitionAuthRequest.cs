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
    public class TeambitionAuthRequest : DefaultAuthRequest
    {
        public TeambitionAuthRequest(ClientConfig config) : base(config, new GithubAuthSource())
        {
        }

        public TeambitionAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new GithubAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            var reqHeaders = new Dictionary<string, object>
            {
                { "Content-Type", "application/x-www-form-urlencoded" },
            };
            var reqParams = new Dictionary<string, object>
            {
                { "client_id", config.clientId },
                { "client_secret", config.clientSecret },
                { "code", authCallback.code },
                { "grant_type", "code" },
            };

            var response = HttpUtils.RequestPost(source.accessToken(), reqParams.spellParams(), reqHeaders);

            var accessTokenObject = response.parseObject();

            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.refreshToken = accessTokenObject.getString("refresh_token");

            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            var accessToken = authToken.accessToken;
            var reqHeaders = new Dictionary<string, object>
            {
                { "Authorization", "OAuth2 " + accessToken },
            };

            var response = HttpUtils.RequestGet(source.userInfo(), reqHeaders);
            var userObj = response.parseObject();

            this.checkResponse(userObj);
            authToken.uid = userObj.getString("_id");

            var authUser = new AuthUser();
            authUser.uuid = userObj.getString("_id");
            authUser.username = userObj.getString("name");
            authUser.nickname = userObj.getString("name");
            authUser.avatar = userObj.getString("avatarUrl");
            authUser.blog = userObj.getString("website");
            authUser.location = userObj.getString("location");
            authUser.email = userObj.getString("email");
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = userObj;
            authUser.originalUserStr = response;
            return authUser;
        }

        
        public override AuthResponse refresh(AuthToken oldToken)
        {
            string uid = oldToken.uid;
            string refreshToken = oldToken.refreshToken;
            var reqHeaders = new Dictionary<string, object>
            {
                { "Content-Type", "application/x-www-form-urlencoded" },
            };
            var reqParams = new Dictionary<string, object>
            {
                { "_userId", uid },
                { "refresh_token", refreshToken },
            };

            var response = HttpUtils.RequestPost(source.refresh(), reqParams.spellParams(), reqHeaders);

            var refreshTokenObject = response.parseObject();

            this.checkResponse(refreshTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = refreshTokenObject.getString("access_token");
            authToken.refreshToken = refreshTokenObject.getString("refresh_token");

            return new AuthResponse(AuthResponseStatus.SUCCESS.GetCode(), AuthResponseStatus.SUCCESS.GetDesc(), authToken);
        }


        /**
        * 校验请求结果
        *
        * @param response 请求结果
        * @return 如果请求结果正常，则返回Exception
        */
        private void checkResponse(Dictionary<string, object> dic)
        {
            if (dic.ContainsKey("message") && dic.ContainsKey("name"))
            {
                throw new Exception($"{dic.getString("getString")}, {dic.getString("name")}");
            }
        }
    }
}