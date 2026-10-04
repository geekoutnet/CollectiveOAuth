using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json.Linq;
using System;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 今日头条登录，对齐 JustAuth AuthToutiaoRequest
     */
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
            string response = doGetAuthorizationCode(authCallback.code);
            var accessTokenObject = JsonExtensions.parseObject(response);

            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.expireIn = accessTokenObject.getIntValue("expires_in");
            authToken.openId = accessTokenObject.getString("open_id");
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string userResponse = doGetUserInfo(authToken);
            var userProfile = JsonExtensions.parseObject(userResponse);

            this.checkResponse(userProfile);

            var user = userProfile.getJSONObject("data");

            bool isAnonymousUser = user.getIntValue("uid_type") == 14;
            string anonymousUserName = "匿名用户";

            var authUser = new AuthUser();
            authUser.rawUserInfo = user;
            authUser.uuid = user.getString("uid");
            authUser.username = isAnonymousUser ? anonymousUserName : user.getString("screen_name");
            authUser.nickname = isAnonymousUser ? anonymousUserName : user.getString("screen_name");
            authUser.avatar = user.getString("avatar_url");
            authUser.remark = user.getString("description");
            authUser.gender = GlobalAuthUtil.getRealGender(user.getString("gender"));
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = user;
            authUser.originalUserStr = userResponse;
            return authUser;
        }

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

        protected override string accessTokenUrl(string code)
        {
            return UrlBuilder.fromBaseUrl(source.accessToken())
                .queryParam("code", code)
                .queryParam("client_key", config.clientId)
                .queryParam("client_secret", config.clientSecret)
                .queryParam("grant_type", "authorization_code")
                .build();
        }

        protected override string userInfoUrl(AuthToken authToken)
        {
            return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("client_key", config.clientId)
                .queryParam("access_token", authToken.accessToken)
                .build();
        }

        /**
         * 检查响应内容是否正确
         */
        private void checkResponse(JObject obj)
        {
            if (obj.containsKey("error_code"))
            {
                throw new AuthException(getErrorCode(obj.getIntValue("error_code")).GetDesc());
            }
        }

        /**
         * 对应 Java AuthToutiaoErrorCode.getErrorCode，未知错误码返回 EC999
         */
        private static AuthToutiaoErrorCode getErrorCode(int errorCode)
        {
            return Enum.IsDefined(typeof(AuthToutiaoErrorCode), errorCode) ? (AuthToutiaoErrorCode)errorCode : AuthToutiaoErrorCode.EC999;
        }
    }
}
