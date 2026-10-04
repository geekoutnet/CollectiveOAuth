using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Come.CollectiveOAuth.Request
{
    /**
     * Teambition授权登录，对齐 JustAuth AuthTeambitionRequest
     */
    public class TeambitionAuthRequest : DefaultAuthRequest
    {
        public TeambitionAuthRequest(ClientConfig config) : base(config, new TeambitionAuthSource())
        {
        }

        public TeambitionAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new TeambitionAuthSource(), authStateCache)
        {
        }

        /**
         * @param authCallback 回调返回的参数
         * @return 所有信息
         */
        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            var form = new Dictionary<string, string>
            {
                { "client_id", config.clientId },
                { "client_secret", config.clientSecret },
                { "code", authCallback.code },
                { "grant_type", "code" }
            };

            string response = HttpUtils.RequestFormPost(source.accessToken(), GlobalAuthUtil.parseMapToString(form, true), null, null, config.httpConfig);
            var accessTokenObject = JsonExtensions.parseObject(response);

            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.refreshToken = accessTokenObject.getString("refresh_token");
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string accessToken = authToken.accessToken;

            var httpHeader = new Dictionary<string, object> { { "Authorization", "OAuth2 " + accessToken } };

            string response = HttpUtils.RequestGet(source.userInfo(), httpHeader, config.httpConfig);
            var obj = JsonExtensions.parseObject(response);

            this.checkResponse(obj);

            authToken.uid = obj.getString("_id");

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.uuid = obj.getString("_id");
            authUser.username = obj.getString("name");
            authUser.nickname = obj.getString("name");
            authUser.avatar = obj.getString("avatarUrl");
            authUser.blog = obj.getString("website");
            authUser.location = obj.getString("location");
            authUser.email = obj.getString("email");
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = response;
            return authUser;
        }

        public override AuthResponse refresh(AuthToken oldToken)
        {
            string uid = oldToken.uid;
            string refreshToken = oldToken.refreshToken;

            var form = new Dictionary<string, string>
            {
                { "_userId", uid },
                { "refresh_token", refreshToken }
            };
            string response = HttpUtils.RequestFormPost(source.refresh(), GlobalAuthUtil.parseMapToString(form, true), null, null, config.httpConfig);
            var refreshTokenObject = JsonExtensions.parseObject(response);

            this.checkResponse(refreshTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = refreshTokenObject.getString("access_token");
            authToken.refreshToken = refreshTokenObject.getString("refresh_token");
            return new AuthResponse(Convert.ToInt32(AuthResponseStatus.SUCCESS), null, authToken);
        }

        /**
         * 检查响应内容是否正确
         *
         * @param object 请求响应内容
         */
        private void checkResponse(JObject obj)
        {
            if (obj.containsKey("message") && obj.containsKey("name"))
            {
                throw new AuthException(obj.getString("name") + ", " + obj.getString("message"));
            }
        }
    }
}
