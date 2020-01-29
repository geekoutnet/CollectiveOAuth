using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;

namespace Come.CollectiveOAuth.Request
{
    public class FackbookAuthRequest : DefaultAuthRequest
    {
        public FackbookAuthRequest(ClientConfig config) : base(config, new FackbookAuthSource())
        {
        }

        public FackbookAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new FackbookAuthSource(), authStateCache)
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
                expireIn = accessTokenObject.GetParamInt32("expires_in"),
                tokenType = accessTokenObject.GetParamString("token_type"),
                code = authCallback.code
            };
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            var response = doGetUserInfo(authToken);
            var userObj = response.parseObject();
            this.checkResponse(userObj);

            var authUser = new AuthUser
            {
                uuid = userObj.GetParamString("id"),
                username = userObj.GetParamString("name"),
                nickname = userObj.GetParamString("name"),
                avatar = getUserPicture(userObj),
                location = userObj.GetParamString("locale"),
                email = userObj.GetParamString("email"),
                gender = GlobalAuthUtil.getRealGender(userObj.GetParamString("gender")),
                token = authToken,
                source = source.getName(),
                originalUser = userObj,
                originalUserStr = response
            };
            return authUser;
        }

        private string getUserPicture(Dictionary<string, object> userObj)
        {
            string picture = null;
            if (userObj.ContainsKey("picture"))
            {
                var pictureObj = userObj.GetParamString("picture").parseObject();
                pictureObj = pictureObj.GetParamString("data").parseObject();
                if (null != pictureObj)
                {
                    picture = pictureObj.GetParamString("url");
                }
            }
            return picture;
        }

        /**
         * 返回获取userInfo的url
         *
         * @param authToken 用户token
         * @return 返回获取userInfo的url
         */
        protected override string userInfoUrl(AuthToken authToken)
        {
            return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("access_token", authToken.accessToken)
                .queryParam("fields", "id,name,birthday,gender,hometown,email,devices,picture.width(400)")
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
                throw new Exception($"{dic.GetParamString("error").parseObject().GetParamString("message")}");
            }
        }
    }
}