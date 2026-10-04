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
     * 微信开放平台登录，对齐 JustAuth AuthWeChatOpenRequest
     */
    public partial class WeChatOpenAuthRequest : DefaultAuthRequest
    {
        public WeChatOpenAuthRequest(ClientConfig config) : base(config, new WechatOpenAuthSource())
        {
        }

        public WeChatOpenAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new WechatOpenAuthSource(), authStateCache)
        {
        }

        /**
         * 微信的特殊性，此时返回的信息同时包含 openid 和 access_token
         *
         * @param authCallback 回调返回的参数
         * @return 所有信息
         */
        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            return this.getToken(accessTokenUrl(authCallback.code));
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string openId = authToken.openId;

            string response = doGetUserInfo(authToken);
            JObject obj = JsonExtensions.parseObject(response);

            this.checkResponse(obj);

            // 与 Java String.format("%s-%s-%s") 一致：缺失字段输出 "null"
            string location = $"{obj.getString("country") ?? "null"}-{obj.getString("province") ?? "null"}-{obj.getString("city") ?? "null"}";

            if (obj.containsKey("unionid"))
            {
                authToken.unionId = obj.getString("unionid");
            }

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.username = obj.getString("nickname");
            authUser.nickname = obj.getString("nickname");
            authUser.avatar = obj.getString("headimgurl");
            authUser.location = location;
            authUser.uuid = openId;
            authUser.gender = GlobalAuthUtil.getWechatRealGender(obj.getString("sex"));
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = response;
            return authUser;
        }

        public override AuthResponse refresh(AuthToken oldToken)
        {
            return new AuthResponse(Convert.ToInt32(AuthResponseStatus.SUCCESS), null, this.getToken(refreshTokenUrl(oldToken.refreshToken)));
        }

        /**
         * 检查响应内容是否正确
         *
         * @param object 请求响应内容
         */
        private void checkResponse(JObject obj)
        {
            if (obj.containsKey("errcode"))
            {
                throw new AuthException(obj.getIntValue("errcode"), obj.getString("errmsg"));
            }
        }

        /**
         * 获取token，适用于获取access_token和刷新token
         *
         * @param accessTokenUrl 实际请求token的地址
         * @return token对象
         */
        private AuthToken getToken(string accessTokenUrl)
        {
            string response = HttpUtils.RequestGet(accessTokenUrl, null, config.httpConfig);
            JObject accessTokenObject = JsonExtensions.parseObject(response);

            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.refreshToken = accessTokenObject.getString("refresh_token");
            authToken.expireIn = accessTokenObject.getIntValue("expires_in");
            authToken.openId = accessTokenObject.getString("openid");
            return authToken;
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
                .queryParam("appid", config.clientId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("scope", "snsapi_login")
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
                .queryParam("appid", config.clientId)
                .queryParam("secret", config.clientSecret)
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
                .queryParam("access_token", authToken.accessToken)
                .queryParam("openid", authToken.openId)
                .queryParam("lang", "zh_CN")
                .build();
        }

        /**
         * 返回获取userInfo的url
         *
         * @param refreshToken getAccessToken方法返回的refreshToken
         * @return 返回获取userInfo的url
         */
        protected override string refreshTokenUrl(string refreshToken)
        {
            return UrlBuilder.fromBaseUrl(source.refresh())
                .queryParam("appid", config.clientId)
                .queryParam("refresh_token", refreshToken)
                .queryParam("grant_type", "refresh_token")
                .build();
        }
    }
}
