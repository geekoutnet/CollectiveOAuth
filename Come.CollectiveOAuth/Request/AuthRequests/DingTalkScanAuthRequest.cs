using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;
using Newtonsoft.Json;
using DingTalk.Api;
using DingTalk.Api.Response;
using DingTalk.Api.Request;

namespace Come.CollectiveOAuth.Request
{
    public class DingTalkScanAuthRequest : DefaultAuthRequest
    {
        public DingTalkScanAuthRequest(ClientConfig config) : base(config, new DingTalkScanAuthSource())
        {
        }

        public DingTalkScanAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new DingTalkScanAuthSource(), authStateCache)
        {
        }


        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            var authToken = new AuthToken();
            authToken.accessCode = authCallback.code;
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            var client = new DefaultDingTalkClient(source.userInfo());
            OapiSnsGetuserinfoBycodeRequest req = new OapiSnsGetuserinfoBycodeRequest();
            req.TmpAuthCode = authToken.accessCode;
            OapiSnsGetuserinfoBycodeResponse response = client.Execute(req, config.clientId, config.clientSecret);

            if (response.IsError)
            {
                throw new Exception(response.Errmsg);
            }
            var userObj = response.UserInfo;

            authToken.openId = userObj.Openid;
            authToken.unionId = userObj.Unionid;

            var authUser = new AuthUser();
            authUser.uuid = userObj.Unionid;
            authUser.username = userObj.Nick;
            authUser.nickname = userObj.Nick;
            authUser.gender = AuthUserGender.UNKNOWN;

            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = response;
            authUser.originalUserStr = JsonConvert.SerializeObject(response);
            return authUser;

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
                .queryParam("scope", config.scope.IsNullOrWhiteSpace() ? "snsapi_login" : config.scope)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("state", getRealState(state))
                .build();
        }

    }
}