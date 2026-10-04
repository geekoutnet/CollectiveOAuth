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
     * 微软登录抽象类，负责处理使用微软国际和微软中国账号登录，对齐 JustAuth AbstractAuthMicrosoftRequest
     */
    public abstract class AbstractMicrosoftAuthRequest : DefaultAuthRequest
    {
        protected AbstractMicrosoftAuthRequest(ClientConfig config, IAuthSource source) : base(config, source)
        {
        }

        protected AbstractMicrosoftAuthRequest(ClientConfig config, IAuthSource source, IAuthStateCache authStateCache)
            : base(config, source, authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            return getToken(accessTokenUrl(authCallback.code));
        }

        /**
         * 获取token，适用于获取access_token和刷新token
         *
         * @param accessTokenUrl 实际请求token的地址
         * @return token对象
         */
        private AuthToken getToken(string accessTokenUrl)
        {
            var httpHeader = new Dictionary<string, object>();

            // 对应 MapUtil.parseStringToMap(accessTokenUrl, false)：把 url 上的参数同时作为表单提交
            var form = parseQueryToMap(accessTokenUrl);

            string response = HttpUtils.RequestFormPost(accessTokenUrl, GlobalAuthUtil.parseMapToString(form, true), httpHeader, null, config.httpConfig);
            var accessTokenObject = JsonExtensions.parseObject(response);

            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.expireIn = accessTokenObject.getIntValue("expires_in");
            authToken.scope = accessTokenObject.getString("scope");
            authToken.tokenType = accessTokenObject.getString("token_type");
            authToken.refreshToken = accessTokenObject.getString("refresh_token");
            return authToken;
        }

        /**
         * 检查响应内容是否正确
         *
         * @param object 请求响应内容
         */
        private void checkResponse(JObject obj)
        {
            if (obj.containsKey("error"))
            {
                throw new AuthException(obj.getString("error_description"));
            }
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string token = authToken.accessToken;
            string tokenType = authToken.tokenType;
            // 与 Java 字符串拼接一致：null 输出为 "null"
            string jwt = (tokenType ?? "null") + " " + (token ?? "null");

            var httpHeader = new Dictionary<string, object> { { "Authorization", jwt } };

            string userInfo = HttpUtils.RequestGet(userInfoUrl(authToken), httpHeader, config.httpConfig);
            var obj = JsonExtensions.parseObject(userInfo);
            this.checkResponse(obj);

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.uuid = obj.getString("id");
            authUser.username = obj.getString("userPrincipalName");
            authUser.nickname = obj.getString("displayName");
            authUser.location = obj.getString("officeLocation");
            authUser.email = obj.getString("mail");
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = userInfo;
            return authUser;
        }

        /**
         * 刷新access token （续期）
         *
         * @param authToken 登录成功后返回的Token信息
         * @return AuthResponse
         */
        public override AuthResponse refresh(AuthToken authToken)
        {
            return new AuthResponse(Convert.ToInt32(AuthResponseStatus.SUCCESS), null, getToken(refreshTokenUrl(authToken.refreshToken)));
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
            // 兼容 Microsoft Entra ID 登录（原微软 AAD）
            return UrlBuilder.fromBaseUrl(formatUrl(source.authorize(), getTenantId()))
                .queryParam("response_type", "code")
                .queryParam("client_id", config.clientId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("state", getRealState(state))
                .queryParam("response_mode", "query")
                .queryParam("scope", this.getScopes(" ", false, AuthScopeUtils.getDefaultScopes(AuthMicrosoftScope.values())))
                .build();
        }

        /**
         * 返回获取accessToken的url
         *
         * @param code 授权code
         * @return 返回获取accessToken的url
         */
        protected override string accessTokenUrl(string code)
        {
            return UrlBuilder.fromBaseUrl(formatUrl(source.accessToken(), getTenantId()))
                .queryParam("code", code)
                .queryParam("client_id", config.clientId)
                .queryParam("client_secret", config.clientSecret)
                .queryParam("grant_type", "authorization_code")
                .queryParam("scope", this.getScopes(" ", false, AuthScopeUtils.getDefaultScopes(AuthMicrosoftScope.values())))
                .queryParam("redirect_uri", config.redirectUri)
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
            return UrlBuilder.fromBaseUrl(source.userInfo()).build();
        }

        /**
         * 返回获取accessToken的url
         *
         * @param refreshToken 用户授权后的token
         * @return 返回获取accessToken的url
         */
        protected override string refreshTokenUrl(string refreshToken)
        {
            return UrlBuilder.fromBaseUrl(formatUrl(source.refresh(), getTenantId()))
                .queryParam("client_id", config.clientId)
                .queryParam("client_secret", config.clientSecret)
                .queryParam("refresh_token", refreshToken)
                .queryParam("grant_type", "refresh_token")
                .queryParam("scope", this.getScopes(" ", false, AuthScopeUtils.getDefaultScopes(AuthMicrosoftScope.values())))
                .queryParam("redirect_uri", config.redirectUri)
                .build();
        }

        /**
         * tenantId 未配置时默认为 common
         */
        private string getTenantId()
        {
            return string.IsNullOrEmpty(config.tenantId) ? "common" : config.tenantId;
        }

        /**
         * 对应 Java String.format(url, tenantId)，地址模板中的占位符为 %s
         */
        private static string formatUrl(string url, string tenantId)
        {
            return url.Replace("%s", tenantId);
        }

        /**
         * 对应 simple-http MapUtil.parseStringToMap(url, false)：取 ? 之后、# 之前的参数，同名参数以逗号拼接；
         * C# 的 UrlBuilder 已对参数值编码，这里解码还原为原值
         */
        private static Dictionary<string, string> parseQueryToMap(string url)
        {
            var map = new Dictionary<string, string>();
            int index = url.IndexOf('?');
            string query = index > -1 ? url.Substring(index + 1) : url;
            int hash = query.IndexOf('#');
            if (hash > -1)
            {
                query = query.Substring(0, hash);
            }
            foreach (var part in query.Split('&'))
            {
                if (part.Length == 0)
                {
                    continue;
                }
                int eq = part.IndexOf('=');
                string key = GlobalAuthUtil.urlDecode(eq < 0 ? part : part.Substring(0, eq));
                string value = eq < 0 ? "" : GlobalAuthUtil.urlDecode(part.Substring(eq + 1));
                map[key] = map.TryGetValue(key, out var old) ? old + "," + value : value;
            }
            return map;
        }
    }
}
