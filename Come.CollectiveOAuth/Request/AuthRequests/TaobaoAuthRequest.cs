using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 淘宝登录，淘宝没有单独的用户信息接口，用户信息随 access_token 一起返回
     */
    public class TaobaoAuthRequest : DefaultAuthRequest
    {
        public TaobaoAuthRequest(ClientConfig config) : base(config, new TaobaoAuthSource())
        {
        }

        public TaobaoAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new TaobaoAuthSource(), authStateCache)
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
            string response = doPostAuthorizationCode(authToken.accessCode);
            var accessTokenObject = response.parseObject();
            this.checkResponse(accessTokenObject);

            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.expireIn = accessTokenObject.getInt32("expires_in");
            authToken.tokenType = accessTokenObject.getString("token_type");
            authToken.idToken = accessTokenObject.getString("id_token");
            authToken.refreshToken = accessTokenObject.getString("refresh_token");
            authToken.uid = accessTokenObject.getString("taobao_user_id");
            authToken.openId = accessTokenObject.getString("taobao_open_uid");

            string nick = GlobalAuthUtil.urlDecode(accessTokenObject.getString("taobao_user_nick"));

            var authUser = new AuthUser();
            authUser.uuid = authToken.uid.IsNullOrWhiteSpace() ? authToken.openId : authToken.uid;
            authUser.username = nick;
            authUser.nickname = nick;
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = accessTokenObject;
            authUser.originalUserStr = response;
            return authUser;
        }

        public override string authorize(string state)
        {
            return UrlBuilder.fromBaseUrl(source.authorize())
                .queryParam("response_type", "code")
                .queryParam("client_id", config.clientId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("view", "web")
                .queryParam("state", getRealState(state))
                .build();
        }

        private void checkResponse(Dictionary<string, object> dic)
        {
            if (dic.ContainsKey("error"))
            {
                throw new Exception($"{dic.getString("error_description")}");
            }
        }
    }
}
