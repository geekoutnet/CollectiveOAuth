using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Enums.Scope;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 领英登录，对齐 JustAuth AuthLinkedinRequest
     */
    public class LinkedInAuthRequest : DefaultAuthRequest
    {
        public LinkedInAuthRequest(ClientConfig config) : base(config, new LinkedInAuthSource())
        {
        }

        public LinkedInAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new LinkedInAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            return this.getToken(accessTokenUrl(authCallback.code));
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string accessToken = authToken.accessToken;
            var httpHeader = new Dictionary<string, object>
            {
                { "Host", "api.linkedin.com" },
                { "Connection", "Keep-Alive" },
                { "Authorization", "Bearer " + accessToken }
            };

            string response = HttpUtils.RequestGet(userInfoUrl(authToken), httpHeader, config.httpConfig);
            var userInfoObject = JsonExtensions.parseObject(response);

            this.checkResponse(userInfoObject);

            string userName = getUserName(userInfoObject);

            // 获取用户头像
            string avatar = this.getAvatar(userInfoObject);

            // 获取用户邮箱地址
            string email = this.getUserEmail(accessToken);

            var authUser = new AuthUser();
            authUser.rawUserInfo = userInfoObject;
            authUser.uuid = userInfoObject.getString("id");
            authUser.username = userName;
            authUser.nickname = userName;
            authUser.avatar = avatar;
            authUser.email = email;
            authUser.token = authToken;
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.source = source.getName();
            authUser.originalUser = userInfoObject;
            authUser.originalUserStr = response;
            return authUser;
        }

        /**
         * 获取用户的真实名
         *
         * @param userInfoObject 用户json对象
         * @return 用户名
         */
        private string getUserName(JObject userInfoObject)
        {
            string firstName, lastName;
            // 获取firstName
            if (userInfoObject.containsKey("localizedFirstName"))
            {
                firstName = userInfoObject.getString("localizedFirstName");
            }
            else
            {
                firstName = getUserName(userInfoObject, "firstName");
            }
            // 获取lastName
            if (userInfoObject.containsKey("localizedLastName"))
            {
                lastName = userInfoObject.getString("localizedLastName");
            }
            else
            {
                lastName = getUserName(userInfoObject, "lastName");
            }
            return firstName + " " + lastName;
        }

        /**
         * 获取用户的头像
         *
         * @param userInfoObject 用户json对象
         * @return 用户的头像地址
         */
        private string getAvatar(JObject userInfoObject)
        {
            var profilePictureObject = userInfoObject.getJSONObject("profilePicture");
            if (null == profilePictureObject || !profilePictureObject.containsKey("displayImage~"))
            {
                return null;
            }
            var displayImageObject = profilePictureObject.getJSONObject("displayImage~");
            if (null == displayImageObject || !displayImageObject.containsKey("elements"))
            {
                return null;
            }
            var displayImageElements = displayImageObject.getJSONArray("elements");
            if (null == displayImageElements || displayImageElements.Count == 0)
            {
                return null;
            }
            var largestImageObj = displayImageElements[displayImageElements.Count - 1] as JObject;
            if (null == largestImageObj || !largestImageObj.containsKey("identifiers"))
            {
                return null;
            }
            var identifiers = largestImageObj.getJSONArray("identifiers");
            if (null == identifiers || identifiers.Count == 0)
            {
                return null;
            }
            return ((JObject)identifiers[0]).getString("identifier");
        }

        /**
         * 获取用户的email
         *
         * @param accessToken 用户授权后返回的token
         * @return 用户的邮箱地址
         */
        private string getUserEmail(string accessToken)
        {
            var httpHeader = new Dictionary<string, object>
            {
                { "Host", "api.linkedin.com" },
                { "Connection", "Keep-Alive" },
                { "Authorization", "Bearer " + accessToken }
            };

            string emailResponse = HttpUtils.RequestGet("https://api.linkedin.com/v2/emailAddress?q=members&projection=(elements*(handle~))", httpHeader, config.httpConfig);
            var emailObj = JsonExtensions.parseObject(emailResponse);

            this.checkResponse(emailObj);

            // 对应 JSONPath.eval(emailObj, "$['elements'][0]['handle~']['emailAddress']")
            var elements = emailObj.getJSONArray("elements");
            if (elements == null || elements.Count == 0)
            {
                return null;
            }
            var handle = (elements[0] as JObject).getJSONObject("handle~");
            return handle.getString("emailAddress");
        }

        private string getUserName(JObject userInfoObject, string nameKey)
        {
            string firstName;
            var firstNameObj = userInfoObject.getJSONObject(nameKey);
            var localizedObj = firstNameObj.getJSONObject("localized");
            var preferredLocaleObj = firstNameObj.getJSONObject("preferredLocale");
            firstName = localizedObj.getString(preferredLocaleObj.getString("language") + "_" + preferredLocaleObj.getString("country"));
            return firstName;
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
                throw new AuthException(obj.getString("error_description"), source);
            }
        }

        /**
         * 获取token，适用于获取access_token和刷新token
         *
         * @param accessTokenUrl 实际请求token的地址
         * @return token对象
         */
        private AuthToken getToken(string accessTokenUrl)
        {
            var httpHeader = new Dictionary<string, object>
            {
                { "Host", "www.linkedin.com" },
                { "Content-Type", "application/x-www-form-urlencoded" }
            };

            string response = HttpUtils.RequestPost(accessTokenUrl, null, httpHeader, null, config.httpConfig);
            var accessTokenObject = JsonExtensions.parseObject(response);

            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.expireIn = accessTokenObject.getIntValue("expires_in");
            authToken.refreshToken = accessTokenObject.getString("refresh_token");
            return authToken;
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
            return UrlBuilder.fromBaseUrl(base.authorize(state))
                .queryParam("scope", this.getScopes(" ", false, AuthScopeUtils.getDefaultScopes(AuthLinkedinScope.values())))
                .build();
        }

        /**
         * 返回获取userInfo的url
         *
         * @param authToken 用户授权后的token
         * @return 返回获取userInfo的url
         */
        protected override string userInfoUrl(AuthToken authToken)
        {
            return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("projection", "(id,firstName,lastName,profilePicture(displayImage~:playableStreams))")
                .build();
        }
    }
}
