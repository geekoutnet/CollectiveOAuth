using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Request
{
    public class MeituanAuthRequest : DefaultAuthRequest
    {
        public MeituanAuthRequest(ClientConfig config) : base(config, new MeituanAuthSource())
        {
        }

        public MeituanAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new MeituanAuthSource(), authStateCache)
        {
        }
        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            var reqParams = new Dictionary<string, object>
            {
                { "app_id", config.clientId },
                { "secret", config.clientSecret },
                { "code", authCallback.code },
                { "grant_type", "authorization_code" },
            };

            var response = HttpUtils.RequestFormPost(source.accessToken(), reqParams.spellParams());
            var accessTokenObject = response.parseObject();

            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken
            {
                accessToken = accessTokenObject.getString("access_token"),
                expireIn = accessTokenObject.getInt32("expires_in"),
                refreshToken = accessTokenObject.getString("refresh_token"),
                code = authCallback.code
            };

            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            var reqParams = new Dictionary<string, object>
            {
                { "app_id", config.clientId },
                { "secret", config.clientSecret },
                { "access_token", authToken.accessToken },
            };

            var response = HttpUtils.RequestFormPost(source.userInfo(), reqParams.spellParams());
            var userObj = response.parseObject();

            this.checkResponse(userObj);

            var authUser = new AuthUser
            {
                uuid = userObj.getString("openid"),
                username = userObj.getString("nickname"),
                nickname = userObj.getString("nickname"),
                avatar = userObj.getString("avatar"),
                gender = AuthUserGender.UNKNOWN,
                token = authToken,
                source = source.getName(),
                originalUser = userObj,
                originalUserStr = response
            };
            return authUser;
        }

        public override AuthResponse refresh(AuthToken oldToken)
        {
            var reqParams = new Dictionary<string, object>
            {
                { "app_id", config.clientId },
                { "secret", config.clientSecret },
                { "refresh_token", oldToken.refreshToken },
                { "grant_type", "refresh_token" },
            };

            var response = HttpUtils.RequestFormPost(source.refresh(), reqParams.spellParams());
            var accessTokenObject = response.parseObject();

            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken
            {
                accessToken = accessTokenObject.getString("access_token"),
                refreshToken = accessTokenObject.getString("refresh_token"),
                expireIn = accessTokenObject.getInt32("expires_in")
            };

            return new AuthResponse(AuthResponseStatus.SUCCESS.GetCode(), AuthResponseStatus.SUCCESS.GetDesc(), authToken);
        }


        public override string authorize(string state)
        {
            return UrlBuilder.fromBaseUrl(source.authorize())
                .queryParam("response_type", "code")
                .queryParam("app_id", config.clientId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("state", getRealState(state))
                .queryParam("scope", config.scope)
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
            if (dic.ContainsKey("error_code"))
            {
                throw new Exception($"{dic.getString("error_msg")}");
            }
        }
    }
}