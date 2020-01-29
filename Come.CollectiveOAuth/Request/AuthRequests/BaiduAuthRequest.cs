using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;

namespace Come.CollectiveOAuth.Request
{
    public class BaiduAuthRequest : DefaultAuthRequest
    {
        public BaiduAuthRequest(ClientConfig config) : base(config, new BaiduAuthSource())
        {
        }

        public BaiduAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new BaiduAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string response = doPostAuthorizationCode(authCallback.code);
            var accessTokenObject = response.parseObject();
            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.refreshToken = accessTokenObject.getString("refresh_token");
            authToken.expireIn = accessTokenObject.getInt32("expires_in");
            authToken.scope = accessTokenObject.getString("scope");

            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string response = doGetUserInfo(authToken);
            var userObj = response.parseObject();
            this.checkResponse(userObj);

            var authUser = new AuthUser();
            authUser.uuid = userObj.getString("userid");
            authUser.username = userObj.getString("username");
            authUser.nickname = userObj.getString("username");

            string protrait = userObj.getString("portrait");
            authUser.avatar = protrait.IsNullOrWhiteSpace() ? null : string.Format("http://himg.bdimg.com/sys/portrait/item/{0}.jpg", protrait);

            authUser.remark = userObj.getString("userdetail");
            authUser.gender = GlobalAuthUtil.getRealGender(userObj.getString("sex"));

            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = userObj;
            authUser.originalUserStr = response;
            return authUser;
        }

        public override AuthResponse revoke(AuthToken authToken)
        {
            string response = doGetRevoke(authToken);
            var revokeObj = response.parseObject();
            this.checkResponse(revokeObj);
            // 返回1表示取消授权成功，否则失败
            AuthResponseStatus status = revokeObj.getInt32("result") == 1 ? AuthResponseStatus.SUCCESS : AuthResponseStatus.FAILURE;
            return new AuthResponse(status.GetCode(), status.GetDesc());
        }

        public override AuthResponse refresh(AuthToken authToken)
        {
            string refreshUrl = UrlBuilder.fromBaseUrl(this.source.refresh())
                .queryParam("grant_type", "refresh_token")
                .queryParam("refresh_token", authToken.refreshToken)
                .queryParam("client_id", this.config.clientId)
                .queryParam("client_secret", this.config.clientSecret)
                .build();
            string response = HttpUtils.RequestGet(refreshUrl);
            var accessTokenObject = response.parseObject();
            this.checkResponse(accessTokenObject);

            var newAuthToken = new AuthToken();
            newAuthToken.accessToken = accessTokenObject.getString("access_token");
            newAuthToken.refreshToken = accessTokenObject.getString("refresh_token");
            newAuthToken.expireIn = accessTokenObject.getInt32("expires_in");
            newAuthToken.scope = accessTokenObject.getString("scope");

            return new AuthResponse(AuthResponseStatus.SUCCESS.GetCode(), AuthResponseStatus.SUCCESS.GetDesc(), newAuthToken);
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
                .queryParam("display", "page")
                .queryParam("scope", "basic")
                .queryParam("state", getRealState(state))
                .build();
        }

        /**
         * 校验请求结果
         *
         * @param response 请求结果
         * @return 如果请求结果正常，则返回JSONObject
         */
        private void checkResponse(Dictionary<string, object> dic)
        {
            if (dic.ContainsKey("error") || dic.ContainsKey("error_code"))
            {
                throw new Exception($@"error_code: {dic.getString("error_code")}," +
                    $" error_description: {dic.getString("error_description")}," +
                    $" error_msg: {dic.getString("error_msg")}");
            }
        }
    }
}