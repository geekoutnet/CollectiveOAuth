using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Request
{
    public class ToutiaoAuthRequest : DefaultAuthRequest
    {
        public ToutiaoAuthRequest(ClientConfig config) : base(config, new ToutiaoAuthSource())
        {
        }

        public ToutiaoAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new ToutiaoAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            var response = doGetAuthorizationCode(authCallback.code);
            var accessTokenObject = response.parseObject();

            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.expireIn = accessTokenObject.getInt32("expires_in");
            authToken.openId = accessTokenObject.getString("open_id");
            authToken.code = authCallback.code;
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string userResponse = doGetUserInfo(authToken);
            var userProfile = userResponse.parseObject();
            this.checkResponse(userProfile);

            var userObj = userProfile.getString("data").parseObject();

            bool isAnonymousUser = userObj.getInt32("uid_type") == 14;
            string anonymousUserName = "匿名用户";

            var authUser = new AuthUser();
            authUser.uuid = userObj.getString("uid");
            authUser.username = isAnonymousUser ? anonymousUserName : userObj.getString("screen_name");
            authUser.nickname = isAnonymousUser ? anonymousUserName : userObj.getString("screen_name");
            authUser.avatar = userObj.getString("avatar_url");
            authUser.remark = userObj.getString("description");
            authUser.gender = GlobalAuthUtil.getRealGender(userObj.getString("gender"));
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = userProfile;
            authUser.originalUserStr = userResponse;
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
                .queryParam("client_key", config.clientId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("auth_only", 1)
                .queryParam("display", 0)
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
                .queryParam("code", code)
                .queryParam("client_key", config.clientId)
                .queryParam("client_secret", config.clientSecret)
                .queryParam("grant_type", "authorization_code")
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
                .queryParam("client_key", config.clientId)
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
            if (dic.ContainsKey("error_code"))
            {
                throw new Exception(getToutiaoErrorCode(dic.getInt32("error_code")).GetDesc());
            }
        }

        private AuthToutiaoErrorCode getToutiaoErrorCode(int errorCode)
        {
            var enumObjects = typeof(AuthToutiaoErrorCode).ToList();
            var codeEnum = enumObjects.Where(a => a.ID == errorCode).ToList();
            if (codeEnum.Count > 0)
            {
                return GlobalAuthUtil.enumFromString<AuthToutiaoErrorCode>(codeEnum[0].Name);
            }
            else
            {
                return AuthToutiaoErrorCode.EC999;
            }
        }
    }
}