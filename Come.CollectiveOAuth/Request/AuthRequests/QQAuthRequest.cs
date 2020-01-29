using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Request
{
    public class QQAuthRequest : DefaultAuthRequest
    {
        public QQAuthRequest(ClientConfig config) : base(config, new QQAuthSource())
        {
        }

        public QQAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new QQAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string response = doGetAuthorizationCode(authCallback.code);
            return getAuthToken(response);
        }

        public override AuthResponse refresh(AuthToken authToken)
        {
            string response = HttpUtils.RequestGet(refreshTokenUrl(authToken.refreshToken));
            return new AuthResponse(AuthResponseStatus.SUCCESS.GetCode(), AuthResponseStatus.SUCCESS.GetDesc(), getAuthToken(response));
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string openId = this.getOpenId(authToken);
            string response = doGetUserInfo(authToken);
            var userObj = response.parseObject();
            if (userObj.GetParamInt32("ret") != 0)
            {
                throw new Exception(userObj.GetParamString("msg"));
            }
            string avatar = userObj.GetParamString("figureurl_qq_2");
            if (avatar.IsNullOrWhiteSpace())
            {
                avatar = userObj.GetParamString("figureurl_qq_1");
            }

            string location = $"{userObj.GetParamString("province")}-{userObj.GetParamString("city")}";

            var authUser = new AuthUser();
            authUser.uuid = openId;
            authUser.username = userObj.GetParamString("nickname");
            authUser.nickname = userObj.GetParamString("nickname");
            authUser.avatar = avatar;
            authUser.location = location;
            authUser.email = userObj.GetParamString("email");
            authUser.remark = userObj.GetParamString("bio");
            authUser.gender = GlobalAuthUtil.getRealGender(userObj.GetParamString("gender"));
            authUser.token = authToken;
            authUser.source = source.getName();

            authUser.originalUser = userObj;
            authUser.originalUserStr = response;
            return authUser;
        }

        /**
         * 获取QQ用户的OpenId，支持自定义是否启用查询unionid的功能，如果启用查询unionid的功能，
         * 那就需要开发者先通过邮件申请unionid功能，参考链接 {@see http://wiki.connect.qq.com/unionid%E4%BB%8B%E7%BB%8D}
         *
         * @param authToken 通过{@link AuthQqRequest#getAccessToken(AuthCallback)}获取到的{@code authToken}
         * @return openId
         */
        private string getOpenId(AuthToken authToken)
        {
            var getOpenIdUrl = UrlBuilder.fromBaseUrl("https://graph.qq.com/oauth2.0/me")
                                .queryParam("access_token", authToken.accessToken)
                                .queryParam("unionid", config.unionId)
                                .build();
            string response = HttpUtils.RequestGet(getOpenIdUrl);
            if (!response.IsNullOrWhiteSpace())
            {
                string body = response;
                string removePrefix = body.Replace("callback(", "");
                string removeSuffix = removePrefix.Replace(");", "");
                string openId = removeSuffix.Trim();
                var openIdObj = openId.parseObject();
                if (openIdObj.ContainsKey("error"))
                {
                    throw new Exception(openIdObj.GetParamString("error") + ":" + openIdObj.GetParamString("error_description"));
                }
                authToken.openId = openIdObj.GetParamString("openid");
                if (openIdObj.ContainsKey("unionid"))
                {
                    authToken.unionId = openIdObj.GetParamString("unionid");
                }

                return authToken.unionId.IsNullOrWhiteSpace() ? authToken.openId : authToken.unionId;
            }

            throw new Exception("request error");
        }

        /**
         * 返回获取userInfo的url
         *
         * @param authToken 用户授权token
         * @return 返回获取userInfo的url
         */
        protected override string userInfoUrl(AuthToken authToken)
        {
            return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("access_token", authToken.accessToken)
                .queryParam("oauth_consumer_key", config.clientId)
                .queryParam("openid", authToken.openId)
                .build();
        }

        private AuthToken getAuthToken(string response)
        {
            var accessTokenObject = response.parseStringObject();
            if (!accessTokenObject.ContainsKey("access_token") || accessTokenObject.ContainsKey("code"))
            {
                throw new Exception(accessTokenObject.GetParamString("msg"));
            }
            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.GetParamString("access_token");
            authToken.expireIn = accessTokenObject.GetParamInt32("expires_in");
            authToken.refreshToken = accessTokenObject.GetParamString("refresh_token");

            return authToken;
        }
    }
}