using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Request
{
    public class StackOverflowAuthRequest : DefaultAuthRequest
    {
        public StackOverflowAuthRequest(ClientConfig config) : base(config, new StackOverflowAuthSource())
        {
        }

        public StackOverflowAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new StackOverflowAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string accessTokenUrl = this.accessTokenUrl(authCallback.code);

            var reqHeaders = new Dictionary<string, object>
            {
                { "Content-Type", "application/x-www-form-urlencoded" },
            };
            var reqParams = accessTokenUrl.parseUrlObject();

            var response = HttpUtils.RequestPost(source.accessToken(), reqParams.spellParams(), reqHeaders);

            var accessTokenObject = response.parseObject();
            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.expireIn = accessTokenObject.getInt32("expires");
            return authToken;
        }


        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string userInfoUrl = UrlBuilder.fromBaseUrl(this.source.userInfo())
                .queryParam("access_token", authToken.accessToken)
                .queryParam("site", "stackoverflow")
                .queryParam("key", this.config.stackOverflowKey)
                .build();

            var response = HttpUtils.RequestGet(userInfoUrl);
            var responseObj = response.parseObject();
            this.checkResponse(responseObj);
            var userObj = responseObj.getString("items").parseListObject()[0];

            var authUser = new AuthUser();
            authUser.uuid = userObj.getString("user_id");
            authUser.username = userObj.getString("username");
            authUser.nickname = userObj.getString("display_name");
            authUser.avatar = userObj.getString("profile_image");
            authUser.location = userObj.getString("location");
           
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = responseObj;
            authUser.originalUserStr = response;
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
                .queryParam("client_id", config.clientId)
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("scope", config.scope.IsNullOrWhiteSpace() ? "read_inbox" : config.scope)
                .queryParam("state", getRealState(state))
                .build();
        }


        /**
         * 检查响应内容是否正确
         *
         * @param object 请求响应内容
         */
        private void checkResponse(Dictionary<string, object> dic)
        {
            if (dic.ContainsKey("error"))
            {
                throw new Exception($"{dic.getString("error_description")}");
            }
        }
    }
}