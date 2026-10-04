using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Enums.Scope;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 华为登录，对齐 JustAuth AuthHuaweiRequest
     */
    public class HuaweiAuthRequest : DefaultAuthRequest
    {
        public HuaweiAuthRequest(ClientConfig config) : base(config, new HuaweiAuthSource())
        {
        }

        public HuaweiAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new HuaweiAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            var form = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", authCallback.authorization_code },
                { "client_id", config.clientId },
                { "client_secret", config.clientSecret },
                { "redirect_uri", config.redirectUri }
            };

            string response = HttpUtils.RequestFormPost(source.accessToken(), GlobalAuthUtil.parseMapToString(form, true), null, null, config.httpConfig);
            return getAuthToken(response);
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            var form = new Dictionary<string, string>
            {
                { "nsp_ts", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() },
                { "access_token", authToken.accessToken },
                { "nsp_fmt", "JS" },
                { "open_id", "OPENID" },
                { "nsp_svc", "huawei.oauth2.user.getTokenInfo" }
            };

            string response = HttpUtils.RequestFormPost(source.userInfo(), GlobalAuthUtil.parseMapToString(form, true), null, null, config.httpConfig);
            var obj = JsonExtensions.parseObject(response);

            this.checkResponse(obj);

            AuthUserGender gender = getRealGender(obj);

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.uuid = obj.getString("userID");
            authUser.username = obj.getString("userName");
            authUser.nickname = obj.getString("userName");
            authUser.gender = gender;
            authUser.avatar = obj.getString("headPictureURL");
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = response;
            return authUser;
        }

        public override AuthResponse refresh(AuthToken authToken)
        {
            var form = new Dictionary<string, string>
            {
                { "client_id", config.clientId },
                { "client_secret", config.clientSecret },
                { "refresh_token", authToken.refreshToken },
                { "grant_type", "refresh_token" }
            };

            string response = HttpUtils.RequestFormPost(source.refresh(), GlobalAuthUtil.parseMapToString(form, true), null, null, config.httpConfig);
            return new AuthResponse(Convert.ToInt32(AuthResponseStatus.SUCCESS), null, getAuthToken(response));
        }

        private AuthToken getAuthToken(string response)
        {
            var obj = JsonExtensions.parseObject(response);

            this.checkResponse(obj);

            var authToken = new AuthToken();
            authToken.accessToken = obj.getString("access_token");
            authToken.expireIn = obj.getIntValue("expires_in");
            authToken.refreshToken = obj.getString("refresh_token");
            return authToken;
        }

        public override string authorize(string state)
        {
            // Java 版传 encode=true 后交给不编码的 UrlBuilder；C# UrlBuilder 默认编码，这里传原值
            return UrlBuilder.fromBaseUrl(base.authorize(state))
                .queryParam("access_type", "offline")
                .queryParam("scope", this.getScopes(" ", false, AuthScopeUtils.getDefaultScopes(AuthHuaweiScope.values())))
                .build();
        }

        protected override string userInfoUrl(AuthToken authToken)
        {
            return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("nsp_ts", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                .queryParam("access_token", authToken.accessToken)
                .queryParam("nsp_fmt", "JS")
                .queryParam("nsp_svc", "OpenUP.User.getInfo")
                .build();
        }

        /**
         * 获取用户的实际性别。华为系统中，用户的性别：1表示女，0表示男
         */
        private AuthUserGender getRealGender(JObject obj)
        {
            int genderCodeInt = obj.getIntValue("gender");
            string genderCode = genderCodeInt == 1 ? "0" : (genderCodeInt == 0) ? "1" : genderCodeInt + "";
            return GlobalAuthUtil.getRealGender(genderCode);
        }

        /**
         * 校验响应结果
         */
        private void checkResponse(JObject obj)
        {
            if (obj.containsKey("NSP_STATUS"))
            {
                throw new AuthException(obj.getString("error"));
            }
            if (obj.containsKey("error"))
            {
                throw new AuthException(GlobalAuthUtil.javaValueOf(obj.getString("sub_error")) + ":" + GlobalAuthUtil.javaValueOf(obj.getString("error_description")));
            }
        }
    }
}
