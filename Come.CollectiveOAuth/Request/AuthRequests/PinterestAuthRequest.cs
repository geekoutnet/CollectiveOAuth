using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Request
{
    public class PinterestAuthRequest : DefaultAuthRequest
    {
        public PinterestAuthRequest(ClientConfig config) : base(config, new PinterestAuthSource())
        {
        }

        public PinterestAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new PinterestAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            var response = doPostAuthorizationCode(authCallback.code);
            var accessTokenObject = response.parseObject();
            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken
            {
                accessToken = accessTokenObject.GetParamString("access_token"),
                tokenType = accessTokenObject.GetParamString("token_type"),
                code = authCallback.code
            };

            return authToken;
        }

        
        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string userinfoUrl = userInfoUrl(authToken);
            var response = HttpUtils.RequestGet(userinfoUrl);
            var responseObj = response.parseObject();
            this.checkResponse(responseObj);

            var userObj = responseObj.GetParamString("data").parseObject();

            var authUser = new AuthUser();
            authUser.uuid = userObj.GetParamString("id");
            authUser.username = userObj.GetParamString("username");
            authUser.nickname = userObj.GetParamString("first_name") + userObj.GetParamString("last_name");
            authUser.avatar = getAvatarUrl(userObj);
            authUser.remark = userObj.GetParamString("bio");
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = responseObj;
            authUser.originalUserStr = response;
            return authUser;
        }


        private string getAvatarUrl(Dictionary<string, object> userObj)
        {
            // image is a map data structure
            var jsonObject = userObj.GetParamString("image").parseObject();
            if (jsonObject.Count == 0)
            {
                return null;
            }
            return jsonObject.GetParamString("60x60").parseObject().GetParamString("url");
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
                .queryParam("scope", config.scope.IsNullOrWhiteSpace() ? "read_public": config.scope)
                .queryParam("state", getRealState(state))
                .build();
        }

        /**
         * 返回获取userInfo的url
         *
         * @param authToken token
         * @return 返回获取userInfo的url
         */
        protected override string userInfoUrl(AuthToken authToken)
        {
            return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("access_token", authToken.accessToken)
                .queryParam("fields", "id,username,first_name,last_name,bio,image")
                .build();
        }


        /**
         * 检查响应内容是否正确
         *
         * @param object 请求响应内容
         */
        private void checkResponse(Dictionary<string, object> dic)
        {
            if (dic.ContainsKey("status") && "failure".Equals(dic.GetParamString("status")))
            {
                throw new Exception($"{dic.GetParamString("message")}");
            }
        }
    }
}