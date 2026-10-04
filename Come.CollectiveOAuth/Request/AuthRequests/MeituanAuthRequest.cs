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
     * 美团登录，对齐 JustAuth AuthMeituanRequest
     */
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
            var form = new Dictionary<string, string>
            {
                { "app_id", config.clientId },
                { "secret", config.clientSecret },
                { "code", authCallback.code },
                { "grant_type", "authorization_code" }
            };

            string response = HttpUtils.RequestFormPost(source.accessToken(), GlobalAuthUtil.parseMapToString(form, true), null, null, config.httpConfig);
            var obj = JsonExtensions.parseObject(response);

            this.checkResponse(obj);

            var authToken = new AuthToken();
            authToken.accessToken = obj.getString("access_token");
            authToken.refreshToken = obj.getString("refresh_token");
            authToken.expireIn = obj.getIntValue("expires_in");
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            var form = new Dictionary<string, string>
            {
                { "app_id", config.clientId },
                { "secret", config.clientSecret },
                { "access_token", authToken.accessToken }
            };

            string response = HttpUtils.RequestFormPost(source.userInfo(), GlobalAuthUtil.parseMapToString(form, true), null, null, config.httpConfig);
            var obj = JsonExtensions.parseObject(response);

            this.checkResponse(obj);

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.uuid = obj.getString("openid");
            authUser.username = obj.getString("nickname");
            authUser.nickname = obj.getString("nickname");
            authUser.avatar = obj.getString("avatar");
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = response;
            return authUser;
        }

        public override AuthResponse refresh(AuthToken oldToken)
        {
            var form = new Dictionary<string, string>
            {
                { "app_id", config.clientId },
                { "secret", config.clientSecret },
                { "refresh_token", oldToken.refreshToken },
                { "grant_type", "refresh_token" }
            };

            string response = HttpUtils.RequestFormPost(source.refresh(), GlobalAuthUtil.parseMapToString(form, true), null, null, config.httpConfig);
            var obj = JsonExtensions.parseObject(response);

            this.checkResponse(obj);

            var authToken = new AuthToken();
            authToken.accessToken = obj.getString("access_token");
            authToken.refreshToken = obj.getString("refresh_token");
            authToken.expireIn = obj.getIntValue("expires_in");
            return new AuthResponse(Convert.ToInt32(AuthResponseStatus.SUCCESS), null, authToken);
        }

        private void checkResponse(JObject obj)
        {
            if (obj.containsKey("error_code"))
            {
                // 与 Java 保持一致：读取的键为 erroe_msg（JustAuth 原样拼写）
                throw new AuthException(obj.getString("erroe_msg"));
            }
        }

        public override string authorize(string state)
        {
            return UrlBuilder.fromBaseUrl(base.authorize(state))
                .queryParam("scope", "")
                .build();
        }
    }
}
