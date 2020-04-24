using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Cache;

namespace Come.CollectiveOAuth.Request
{
    public partial class WeChatMpAuthRequest : DefaultAuthRequest
    {
        public WeChatMpAuthRequest(ClientConfig config) : base(config, new WechatMPAuthSource())
        {
        }

        public WeChatMpAuthRequest(ClientConfig config, IAuthStateCache authStateCache) : base(config, new WechatMPAuthSource(), authStateCache)
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
            var jsonObj = response.parseObject();

            this.checkResponse(jsonObj);

            //string location = String.format("%s-%s-%s", object.getString("country"), object.getString("province"), object.getString("city"));
            string location = $"{jsonObj.getString("country")}-{jsonObj.getString("province")}-{jsonObj.getString("city")}";
            if (jsonObj.ContainsKey("unionid"))
            {
                authToken.unionId = jsonObj.getString("unionid");
            }

            var authUser = new AuthUser();

            authUser.username = jsonObj.getString("nickname");
            authUser.nickname = jsonObj.getString("nickname");
            authUser.avatar = jsonObj.getString("headimgurl");
            authUser.location = location;
            authUser.uuid = openId;
            authUser.gender = GlobalAuthUtil.getWechatRealGender(jsonObj.getString("sex"));
            authUser.token = authToken;
            authUser.source = source.getName();

            authUser.originalUser = jsonObj;
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
        private void checkResponse(Dictionary<string, object> dic)
        {
            if (dic.ContainsKey("errcode"))
            {
                throw new Exception($"errcode: {dic.getString("errcode")}, errmsg: {dic.getString("errmsg")}");
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
            string response = HttpUtils.RequestGet(accessTokenUrl);
            var accessTokenObject = response.parseObject();

            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();

            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.refreshToken = accessTokenObject.getString("refresh_token");
            authToken.expireIn = accessTokenObject.getInt32("expires_in");
            authToken.openId = accessTokenObject.getString("openid");
            authToken.scope = accessTokenObject.getString("scope");

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
                .queryParam("appid", config.clientId)
                .queryParam("redirect_uri", GlobalAuthUtil.urlEncode(config.redirectUri))
                .queryParam("response_type", "code")
                .queryParam("scope", config.scope.IsNullOrWhiteSpace() ? "snsapi_userinfo" : config.scope)
                .queryParam("state", getRealState(state) + "#wechat_redirect")
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
                .queryParam("appid", config.clientId)
                .queryParam("secret", config.clientSecret)
                .queryParam("code", code)
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
        protected override string refreshTokenUrl(String refreshToken)
        {
            return UrlBuilder.fromBaseUrl(source.refresh())
                .queryParam("appid", config.clientId)
                .queryParam("grant_type", "refresh_token")
                .queryParam("refresh_token", refreshToken)
                .build();
        }
    }
}