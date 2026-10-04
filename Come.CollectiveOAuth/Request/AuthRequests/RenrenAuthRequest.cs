using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Enums.Scope;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 人人登录，对齐 JustAuth AuthRenrenRequest
     */
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
            string response = doGetUserInfo(authToken);
            var userObj = JsonExtensions.parseObject(response).getJSONObject("response");

            var authUser = new AuthUser();
            authUser.rawUserInfo = userObj;
            authUser.uuid = userObj.getString("id");
            authUser.avatar = getAvatarUrl(userObj);
            authUser.nickname = userObj.getString("name");
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
            // token 以 urlEncode 后的形式保存（与 Java 一致），Java 原样拼入不编码的 url；
            // C# UrlBuilder 会再编码一次，这里先解码，保证服务端收到的值与 Java 相同
            string refreshToken = authToken.refreshToken == null ? null : GlobalAuthUtil.urlDecode(authToken.refreshToken);
            return new AuthResponse(Convert.ToInt32(AuthResponseStatus.SUCCESS), null, getToken(this.refreshTokenUrl(refreshToken)));
        }

        private AuthToken getToken(string url)
        {
            string response = HttpUtils.RequestPost(url, null, null, null, config.httpConfig);
            var jsonObject = JsonExtensions.parseObject(response);
            if (jsonObject.containsKey("error"))
            {
                // 对应 Java JSONObject.toString()（紧凑格式；fastjson 的键顺序可能与原文不同）
                throw new AuthException("Failed to get token from Renren: " + jsonObject.ToString(Formatting.None));
            }

            var authToken = new AuthToken();
            authToken.tokenType = jsonObject.getString("token_type");
            authToken.expireIn = jsonObject.getIntValue("expires_in");
            authToken.accessToken = GlobalAuthUtil.urlEncode(jsonObject.getString("access_token"));
            authToken.refreshToken = GlobalAuthUtil.urlEncode(jsonObject.getString("refresh_token"));
            authToken.openId = jsonObject.getJSONObject("user").getString("id");
            return authToken;
        }

        private string getAvatarUrl(JObject userObj)
        {
            var jsonArray = userObj.getJSONArray("avatar");
            if (jsonArray == null || jsonArray.Count == 0)
            {
                return null;
            }
            return ((JObject)jsonArray[0]).getString("url");
        }

        private AuthUserGender getGender(JObject userObj)
        {
            var basicInformation = userObj.getJSONObject("basicInformation");
            if (basicInformation == null)
            {
                return AuthUserGender.UNKNOWN;
            }
            return GlobalAuthUtil.getRealGender(basicInformation.getString("sex"));
        }

        private string getCompany(JObject userObj)
        {
            var jsonArray = userObj.getJSONArray("work");
            if (jsonArray == null || jsonArray.Count == 0)
            {
                return null;
            }
            return ((JObject)jsonArray[0]).getString("name");
        }

        /**
         * 返回获取userInfo的url
         *
         * @param authToken 用户授权后的token
         * @return 返回获取userInfo的url
         */
        protected override string userInfoUrl(AuthToken authToken)
        {
            // accessToken 已是 urlEncode 后的值（与 Java 一致），Java 原样拼入不编码的 url；
            // C# UrlBuilder 会再编码一次，这里先解码，保证服务端收到的值与 Java 相同
            return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("access_token", authToken.accessToken == null ? null : GlobalAuthUtil.urlDecode(authToken.accessToken))
                .queryParam("userId", authToken.openId)
                .build();
        }

        public override string authorize(string state)
        {
            return UrlBuilder.fromBaseUrl(base.authorize(state))
                .queryParam("scope", this.getScopes(",", false, AuthScopeUtils.getDefaultScopes(AuthRenrenScope.values())))
                .build();
        }
    }
}
