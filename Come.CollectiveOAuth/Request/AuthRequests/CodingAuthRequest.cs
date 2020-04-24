using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Request
{
    public class CodingAuthRequest : DefaultAuthRequest
    {
        public CodingAuthRequest(ClientConfig config) : base(config, new CodingAuthSource())
        {
        }

        public CodingAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new CodingAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string response = doGetAuthorizationCode(authCallback.code);
            var accessTokenObject = response.parseObject();
            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.expireIn = accessTokenObject.getInt32("expires_in");
            authToken.refreshToken = accessTokenObject.getString("refresh_token");
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string response = doGetUserInfo(authToken);
            var resData = response.parseObject();
            this.checkResponse(resData);

            var userObj = resData.getString("data").parseObject();

            var authUser = new AuthUser();
            authUser.uuid = userObj.getString("id");
            authUser.username = userObj.getString("name");
            authUser.nickname = userObj.getString("name");
            authUser.avatar = $"{"https://coding.net/"}{userObj.getString("avatar")}";
            authUser.blog = $"{"https://coding.net/"}{userObj.getString("path")}";
            authUser.company = userObj.getString("company");
            authUser.location = userObj.getString("location");
            authUser.email = userObj.getString("email");
            authUser.remark = userObj.getString("slogan");
            authUser.gender = GlobalAuthUtil.getRealGender(userObj.getString("sex"));

            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = resData;
            authUser.originalUserStr = response;
            return authUser;
        }

        protected override string doGetUserInfo(AuthToken authToken)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            return HttpUtils.RequestJsonGet(userInfoUrl(authToken));
        }

        protected override string doGetAuthorizationCode(String code)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            return HttpUtils.RequestJsonGet(accessTokenUrl(code));
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
                .queryParam("client_id", config.clientId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("scope", config.scope.IsNullOrWhiteSpace() ? "user" : config.scope)
                .queryParam("state", getRealState(state))
                .build();
        }
        /**
        * 校验请求结果
        *
        * @param response 请求结果
        * @return 如果请求结果正常，则返回Exception
        */
        private void checkResponse(Dictionary<string, object> dic)
        {
            if (dic.ContainsKey("code") && dic.getInt32("code") != 0)
            {
                throw new Exception($"{dic.getString("msg")}");
            }
        }
    }
}