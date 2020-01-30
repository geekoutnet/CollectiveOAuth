using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Request
{
    public class RenrenAuthRequest : DefaultAuthRequest
    {
        public RenrenAuthRequest(ClientConfig config) : base(config, new RenrenAuthSource())
        {
        }

        public RenrenAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new RenrenAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            return this.getToken(accessTokenUrl(authCallback.code));
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            var response = doGetUserInfo(authToken);
            var userObj = response.parseObject().getJSONObject("response");

            var authUser = new AuthUser();
            authUser.uuid = userObj.getString("id");
            authUser.username = userObj.getString("name");
            authUser.nickname = userObj.getString("name");
            authUser.avatar = getAvatarUrl(userObj);
            authUser.company = getCompany(userObj);
            authUser.gender = getGender(userObj);

            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = userObj;
            authUser.originalUserStr = response;
            return authUser;
        }

        public override AuthResponse refresh(AuthToken authToken)
        {
            var token = getToken(this.refreshTokenUrl(authToken.refreshToken));
            return new AuthResponse(AuthResponseStatus.SUCCESS.GetCode(), AuthResponseStatus.SUCCESS.GetDesc(), token);
        }

        private AuthToken getToken(string url)
        {
            var response = HttpUtils.RequestPost(url);
            var jsonObject = response.parseObject();
            if (jsonObject.ContainsKey("error"))
            {
                throw new Exception("Failed to get token from Renren: " + jsonObject);
            }

            var authToken = new AuthToken();
            authToken.accessToken = jsonObject.getString("access_token");
            authToken.tokenType = jsonObject.getString("token_type");
            authToken.expireIn = jsonObject.getInt32("expires_in");
            authToken.refreshToken = jsonObject.getString("refresh_token");
            authToken.openId = jsonObject.getJSONObject("user").getString("id");

            return authToken;
        }

        private string getAvatarUrl(Dictionary<string, object> userObj)
        {
            var jsonArray = userObj.getJSONArray("avatar");
            if (jsonArray.Count == 0)
            {
                return null;
            }
            return jsonArray[0].getString("url");
        }

        private AuthUserGender getGender(Dictionary<string, object> userObj)
        {
            var basicInformation = userObj.getJSONObject("basicInformation");
            if (basicInformation.Count == 0)
            {
                return AuthUserGender.UNKNOWN;
            }
            return GlobalAuthUtil.getRealGender(basicInformation.getString("sex"));
        }

        private string getCompany(Dictionary<string, object> userObj)
        {
            var jsonArray = userObj.getJSONArray("work");
            if (jsonArray.Count == 0)
            {
                return null;
            }
            return jsonArray[0].getString("name");
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
                .queryParam("userId", authToken.openId)
                .build();
        }

        public override string authorize(string state)
        {
            return UrlBuilder.fromBaseUrl(source.authorize())
                .queryParam("response_type", "code")
                .queryParam("client_id", config.clientId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("state", getRealState(state))
                .queryParam("display", "page")
                .build();
        }
    }
}