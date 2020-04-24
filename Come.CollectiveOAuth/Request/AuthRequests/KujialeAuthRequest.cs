using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Request
{
    public class KujialeAuthRequest : DefaultAuthRequest
    {
        public KujialeAuthRequest(ClientConfig config) : base(config, new KujialeAuthSource())
        {
        }

        public KujialeAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new KujialeAuthSource(), authStateCache)
        {
        }

        /**
          * 返回带{@code state}参数的授权url，授权回调时会带上这个{@code state}
          * 默认只向用户请求用户信息授权
          *
          * @param state state 验证授权流程的参数，可以防止csrf
          * @return 返回授权地址
          * @since 1.11.0
          */
        public override string authorize(string state)
        {
             var urlBuilder = UrlBuilder.fromBaseUrl(source.authorize())
                .queryParam("response_type", "code")
                .queryParam("client_id", config.clientId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("state", getRealState(state))
                .queryParam("scope", config.scope.IsNullOrWhiteSpace() ? "get_user_info": config.scope)
                .build();
             return urlBuilder;
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            var response = doPostAuthorizationCode(authCallback.code);
            return getAuthToken(response);
        }

        private AuthToken getAuthToken(string response)
        {
            var accessTokenObject = response.parseObject();
            this.checkResponse(accessTokenObject);

            var resultObject = accessTokenObject.getJSONObject("d");

            var authToken = new AuthToken();
            authToken.accessToken = resultObject.getString("accessToken");
            authToken.refreshToken = resultObject.getString("refreshToken");
            authToken.expireIn = resultObject.getInt32("expiresIn");
            return authToken;
        }

        
        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string openId = this.getOpenId(authToken);

            var userInfoUrl = UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("access_token", authToken.accessToken)
                .queryParam("open_id", openId)
                .build();

            var response = HttpUtils.RequestGet(userInfoUrl);
            var resObj = response.parseObject();
            this.checkResponse(resObj);

            var userObj = resObj.getJSONObject("d");

            var authUser = new AuthUser();
            authUser.uuid = userObj.getString("openId");
            authUser.username = userObj.getString("userName");
            authUser.nickname = userObj.getString("userName");
            authUser.avatar = userObj.getString("avatar");
            authUser.gender = AuthUserGender.UNKNOWN;

            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = resObj;
            authUser.originalUserStr = response;
            return authUser;
        }

        /**
         * 获取酷家乐的openId，此id在当前client范围内可以唯一识别授权用户
         *
         * @param authToken 通过{@link AuthKujialeRequest#getAccessToken(AuthCallback)}获取到的{@code authToken}
         * @return openId
         */
        private string getOpenId(AuthToken authToken)
        {
            var openIdUrl = UrlBuilder.fromBaseUrl("https://oauth.kujiale.com/oauth2/auth/user")
                .queryParam("access_token", authToken.accessToken)
                .build();
            var response = HttpUtils.RequestGet(openIdUrl);
            var accessTokenObject = response.parseObject();
            this.checkResponse(accessTokenObject);
            return accessTokenObject.getString("d");
        }

        public override AuthResponse refresh(AuthToken authToken)
        {
            var refreshUrl = refreshTokenUrl(authToken.refreshToken);
            var response = HttpUtils.RequestPost(refreshUrl);
            return new AuthResponse(AuthResponseStatus.SUCCESS.GetCode(), AuthResponseStatus.SUCCESS.GetDesc(), getAuthToken(response));
        }

        /**
       * 校验请求结果
       *
       * @param response 请求结果
       * @return 如果请求结果正常，则返回Exception
       */
        private void checkResponse(Dictionary<string, object> dic)
        {
            if (dic.Count == 0)
            {
                throw new Exception("请求所返回的数据为空!");
            }

            if (!"0".Equals(dic.getString("c")))
            {
                throw new Exception($"{dic.getString("m")}");
            }
        }
    }
}