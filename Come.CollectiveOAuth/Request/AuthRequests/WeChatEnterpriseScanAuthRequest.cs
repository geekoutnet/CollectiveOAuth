using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;

namespace Come.CollectiveOAuth.Request
{
    public class WeChatEnterpriseScanAuthRequest : DefaultAuthRequest
    {
        public WeChatEnterpriseScanAuthRequest(ClientConfig config) : base(config, new WechatEnterpriseScanAuthSource())
        {
        }

        public WeChatEnterpriseScanAuthRequest(ClientConfig config, IAuthStateCache authStateCache) 
            : base(config, new WechatEnterpriseScanAuthSource(), authStateCache)
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
            string response = doGetAuthorizationCode(accessTokenUrl(authCallback.code));
            var jsonObj = response.parseObject();

            this.checkResponse(jsonObj);

            var authToken = new AuthToken();
            authToken.accessToken = jsonObj.getString("access_token");
            authToken.expireIn = jsonObj.getInt32("expires_in");
            authToken.code = authCallback.code;

            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string response = doGetUserInfo(authToken);
            var jsonObj = response.parseObject();
            this.checkResponse(jsonObj);

            // 返回 OpenId 或其他，均代表非当前企业用户，不支持
            if (!jsonObj.ContainsKey("UserId"))
            {
                throw new Exception(AuthResponseStatus.UNIDENTIFIED_PLATFORM.GetDesc());
            }
            string userId = jsonObj.getString("UserId");
            string userDetailResponse = getUserDetail(authToken.accessToken, userId);
            var userDetailObj = userDetailResponse.parseObject();
            this.checkResponse(userDetailObj);

            var authUser = new AuthUser();
            authUser.username = userDetailObj.getString("name");
            authUser.nickname = userDetailObj.getString("alias");
            authUser.avatar = userDetailObj.getString("avatar");
            authUser.location = userDetailObj.getString("address");
            authUser.email = userDetailObj.getString("email");
            authUser.uuid = userDetailObj.getString("userId");
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.gender = GlobalAuthUtil.getWechatRealGender(userDetailObj.getString("gender"));

            authUser.originalUser = userDetailObj;
            authUser.originalUserStr = response;
            return authUser;
        }

        /**
         * 校验请求结果
         *
         * @param response 请求结果
         * @return 如果请求结果正常，则返回JSONObject
         */
        private void checkResponse(Dictionary<string, object> dic)
        {
            if (dic.ContainsKey("errcode") && dic.getInt32("errcode") != 0)
            {
                throw new Exception($"errcode: {dic.getString("errcode")}, errmsg: {dic.getString("errmsg")}");
            }
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
                .queryParam("agentid", config.agentId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("state", getRealState(state))
                .build();
        }

        /**
         * 返回获取accessToken的url
         *
         * @param code 授权码
         * @return 返回获取accessToken的url
         */
        protected override string accessTokenUrl(String code)
        {
            return UrlBuilder.fromBaseUrl(source.accessToken())
                .queryParam("corpid", config.clientId)
                .queryParam("corpsecret", config.clientSecret)
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
                .queryParam("code", authToken.code)
                .build();
        }

        /**
         * 用户详情
         *
         * @param accessToken accessToken
         * @param userId      企业内用户id
         * @return 用户详情
         */
        private string getUserDetail(string accessToken, string userId)
        {
            string userDetailUrl = UrlBuilder.fromBaseUrl("https://qyapi.weixin.qq.com/cgi-bin/user/get")
                .queryParam("access_token", accessToken)
                .queryParam("userid", userId)
                .build();
            return HttpUtils.RequestGet(userDetailUrl);
        }
    }
}