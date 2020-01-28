using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;

namespace Come.CollectiveOAuth.Request
{
    public class GiteeAuthRequest : DefaultAuthRequest
    {
        public GiteeAuthRequest(ClientConfig config) : base(config, new GiteeAuthSource())
        {
        }

        public GiteeAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new GiteeAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string response = doPostAuthorizationCode(authCallback.code);
            var accessTokenObject = response.parseObject();
            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.GetParamString("access_token");
            authToken.refreshToken = accessTokenObject.GetParamString("refresh_token");
            authToken.tokenType = accessTokenObject.GetParamString("token_type");
            authToken.expireIn = accessTokenObject.GetParamInt32("expires_in").Value;
            authToken.scope = accessTokenObject.GetParamString("scope");

            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string response = doGetUserInfo(authToken);
            var userObj = response.parseObject();
            this.checkResponse(userObj);

            var authUser = new AuthUser();
            authUser.uuid = userObj.GetParamString("id");
            authUser.username = userObj.GetParamString("login");
            authUser.nickname = userObj.GetParamString("name");
            authUser.avatar = userObj.GetParamString("avatar_url");
            authUser.blog = userObj.GetParamString("blog");
            authUser.company = userObj.GetParamString("company");
            authUser.location = userObj.GetParamString("address");
            authUser.email = userObj.GetParamString("email");
            authUser.remark = userObj.GetParamString("bio");
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = userObj;
            authUser.originalUserStr = response;
            return authUser;
        }


        /**
        * 校验请求结果
        *
        * @param response 请求结果
        * @return 如果请求结果正常，则返回Exception
        */
        private void checkResponse(Dictionary<string, object> dic)
        {
            if (dic.ContainsKey("error"))
            {
                throw new Exception($"{dic.GetDicValue("error_description")}");
            }
        }
    }
}