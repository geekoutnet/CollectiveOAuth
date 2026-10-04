using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Enums.Scope;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json.Linq;

namespace Come.CollectiveOAuth.Request
{
    /**
     * Facebook登录，对齐 JustAuth AuthFacebookRequest（C# 类名沿用原拼写）
     */
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
            string response = doPostAuthorizationCode(authCallback.code);
            var accessTokenObject = JsonExtensions.parseObject(response);
            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.expireIn = accessTokenObject.getIntValue("expires_in");
            authToken.tokenType = accessTokenObject.getString("token_type");
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string userInfo = doGetUserInfo(authToken);
            var obj = JsonExtensions.parseObject(userInfo);
            this.checkResponse(obj);

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.uuid = obj.getString("id");
            authUser.username = obj.getString("name");
            authUser.nickname = obj.getString("name");
            authUser.blog = obj.getString("link");
            authUser.avatar = getUserPicture(obj);
            authUser.location = obj.getString("locale");
            authUser.email = obj.getString("email");
            authUser.gender = GlobalAuthUtil.getRealGender(obj.getString("gender"));
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = userInfo;
            return authUser;
        }

        private string getUserPicture(JObject obj)
        {
            string picture = null;
            if (obj.containsKey("picture"))
            {
                var pictureObj = obj.getJSONObject("picture");
                pictureObj = pictureObj.getJSONObject("data");
                if (null != pictureObj)
                {
                    picture = pictureObj.getString("url");
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
                .queryParam("fields", "id,name,birthday,gender,hometown,email,devices,picture.width(400),link")
                .build();
        }

        protected override void checkConfig(ClientConfig config)
        {
            base.checkConfig(config);
            // facebook的回调地址必须为https的链接
            if (!GlobalAuthUtil.isHttpsProtocol(config.redirectUri))
            {
                // Facebook's redirect uri must use the HTTPS protocol
                throw new AuthException(AuthResponseStatus.ILLEGAL_REDIRECT_URI, source);
            }
        }

        /**
         * 检查响应内容是否正确
         *
         * @param object 请求响应内容
         */
        private void checkResponse(JObject obj)
        {
            if (obj.containsKey("error"))
            {
                throw new AuthException(obj.getJSONObject("error").getString("message"));
            }
        }

        /**
         * 返回带{@code state}参数的授权url，授权回调时会带上这个{@code state}
         *
         * @param state state 验证授权流程的参数，可以防止csrf
         * @return 返回授权地址
         */
        public override string authorize(string state)
        {
            return UrlBuilder.fromBaseUrl(base.authorize(state))
                .queryParam("scope", this.getScopes(",", false, AuthScopeUtils.getDefaultScopes(AuthFacebookScope.values())))
                .build();
        }
    }
}
