using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Request
{
    public class WeiboAuthRequest : DefaultAuthRequest
    {
        public WeiboAuthRequest(ClientConfig config) : base(config, new WeiboAuthSource())
        {
        }

        public WeiboAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new WeiboAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            var response = doPostAuthorizationCode(authCallback.code);
            var accessTokenObject = response.parseObject();
            if (accessTokenObject.ContainsKey("error"))
            {
                throw new Exception(accessTokenObject.GetParamString("error_description"));
            }

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.GetParamString("access_token");
            authToken.uid = accessTokenObject.GetParamString("uid");
            authToken.openId = accessTokenObject.GetParamString("uid");
            authToken.expireIn = accessTokenObject.GetParamInt32("expires_in");
            authToken.code = authCallback.code;

            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            var accessToken = authToken.accessToken;
            var uid = authToken.uid;
            var oauthParam = $"uid={uid}&access_token={accessToken}";
            var reqParams = new Dictionary<string, object>();
            reqParams.Add("Authorization", "OAuth2 " + oauthParam);
            reqParams.Add("API-RemoteIP", "application/x-www-form-urlencoded");

            string response = HttpUtils.RequestGet(userInfoUrl(authToken), reqParams);
          
            var userObj = response.parseObject();
            if (userObj.ContainsKey("error"))
            {
                throw new Exception(userObj.GetParamString("error"));
            }

            var authUser = new AuthUser();
            authUser.uuid = userObj.GetParamString("id");
            authUser.username = userObj.GetParamString("name");
            authUser.nickname = userObj.GetParamString("screen_name");
            authUser.avatar = userObj.GetParamString("profile_image_url");
            authUser.blog = userObj.GetParamString("url").IsNullOrWhiteSpace() ? $"{"https://weibo.com/"}{userObj.GetParamString("profile_url")}" : userObj.GetParamString("url");
            authUser.location = userObj.GetParamString("location");
            authUser.remark = userObj.GetParamString("description");
            authUser.gender = GlobalAuthUtil.getRealGender(userObj.GetParamString("gender"));

            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = userObj;
            authUser.originalUserStr = response;
            return authUser;
        }

        /**
         * 返回获取userInfo的url
         *
         * @param authToken authToken
         * @return 返回获取userInfo的url
         */
        protected override string userInfoUrl(AuthToken authToken)
        {
            return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("access_token", authToken.accessToken)
                .queryParam("uid", authToken.uid)
                .build();
        }

        public override AuthResponse revoke(AuthToken authToken)
        {
            var response = doGetRevoke(authToken);
            var retObj = response.parseObject();
            if (retObj.ContainsKey("error"))
            {
                return new AuthResponse(AuthResponseStatus.FAILURE.GetCode(), retObj.GetParamString("error"));
            }
            // 返回 result = true 表示取消授权成功，否则失败
            AuthResponseStatus status = retObj.GetParamBool("result") ? AuthResponseStatus.SUCCESS : AuthResponseStatus.FAILURE;
            return new AuthResponse(status.GetCode(), status.GetDesc());
        }
    }
}