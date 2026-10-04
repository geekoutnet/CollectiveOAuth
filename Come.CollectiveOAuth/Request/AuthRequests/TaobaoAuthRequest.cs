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
     * 淘宝登录，对齐 JustAuth AuthTaobaoRequest；淘宝没有单独的用户信息接口，用户信息随 access_token 一起返回
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

        private AuthToken getAuthToken(JObject obj)
        {
            this.checkResponse(obj);

            var authToken = new AuthToken();
            authToken.accessToken = obj.getString("access_token");
            authToken.expireIn = obj.getIntValue("expires_in");
            authToken.tokenType = obj.getString("token_type");
            authToken.idToken = obj.getString("id_token");
            authToken.refreshToken = obj.getString("refresh_token");
            authToken.uid = obj.getString("taobao_user_id");
            authToken.openId = obj.getString("taobao_open_uid");
            return authToken;
        }

        private void checkResponse(JObject obj)
        {
            if (obj.containsKey("error"))
            {
                throw new AuthException(obj.getString("error_description"));
            }
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string response = doPostAuthorizationCode(authToken.accessCode);
            var accessTokenObject = JsonExtensions.parseObject(response);
            if (accessTokenObject.containsKey("error"))
            {
                throw new AuthException(accessTokenObject.getString("error_description"));
            }
            authToken = this.getAuthToken(accessTokenObject);

            string nick = GlobalAuthUtil.urlDecode(accessTokenObject.getString("taobao_user_nick"));

            var authUser = new AuthUser();
            authUser.rawUserInfo = accessTokenObject;
            authUser.uuid = string.IsNullOrEmpty(authToken.uid) ? authToken.openId : authToken.uid;
            authUser.username = nick;
            authUser.nickname = nick;
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = accessTokenObject;
            authUser.originalUserStr = response;
            return authUser;
        }

        public override AuthResponse refresh(AuthToken oldToken)
        {
            string tokenUrl = refreshTokenUrl(oldToken.refreshToken);
            string response = HttpUtils.RequestPost(tokenUrl, null, null, null, config.httpConfig);
            var accessTokenObject = JsonExtensions.parseObject(response);
            return new AuthResponse(Convert.ToInt32(AuthResponseStatus.SUCCESS), null, this.getAuthToken(accessTokenObject));
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
    }
}
