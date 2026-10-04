using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Enums.Scope;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 微博登录，对齐 JustAuth AuthWeiboRequest
     */
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
            string response = doPostAuthorizationCode(authCallback.code);
            var accessTokenObject = JsonExtensions.parseObject(response);
            if (accessTokenObject.containsKey("error"))
            {
                throw new AuthException(accessTokenObject.getString("error_description"));
            }

            var authToken = new AuthToken();
            authToken.accessToken = accessTokenObject.getString("access_token");
            authToken.uid = accessTokenObject.getString("uid");
            authToken.openId = accessTokenObject.getString("uid");
            authToken.expireIn = accessTokenObject.getIntValue("expires_in");
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string accessToken = authToken.accessToken;
            string uid = authToken.uid;
            string oauthParam = string.Format("uid={0}&access_token={1}", GlobalAuthUtil.javaValueOf(uid), GlobalAuthUtil.javaValueOf(accessToken));

            var httpHeader = new Dictionary<string, object>
            {
                { "Authorization", "OAuth2 " + oauthParam },
                { "API-RemoteIP", getLocalIp() }
            };
            string userInfo = HttpUtils.RequestGet(userInfoUrl(authToken), httpHeader, config.httpConfig);
            var obj = JsonExtensions.parseObject(userInfo);
            if (obj.containsKey("error"))
            {
                throw new AuthException(obj.getString("error"));
            }

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.uuid = obj.getString("id");
            authUser.username = obj.getString("name");
            authUser.avatar = obj.getString("profile_image_url");
            authUser.blog = string.IsNullOrEmpty(obj.getString("url"))
                ? "https://weibo.com/" + GlobalAuthUtil.javaValueOf(obj.getString("profile_url"))
                : obj.getString("url");
            authUser.nickname = obj.getString("screen_name");
            authUser.location = obj.getString("location");
            authUser.remark = obj.getString("description");
            authUser.gender = GlobalAuthUtil.getRealGender(obj.getString("gender"));
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = obj;
            authUser.originalUserStr = userInfo;
            return authUser;
        }

        /**
         * 对应 JustAuth IpUtils.getLocalIp()：InetAddress.getLocalHost().getHostAddress()
         */
        private static string getLocalIp()
        {
            try
            {
                var addresses = Dns.GetHostAddresses(Dns.GetHostName());
                var address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork) ?? addresses.FirstOrDefault();
                return address?.ToString();
            }
            catch (Exception)
            {
                return null;
            }
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

        public override string authorize(string state)
        {
            return UrlBuilder.fromBaseUrl(base.authorize(state))
                .queryParam("scope", this.getScopes(",", false, AuthScopeUtils.getDefaultScopes(AuthWeiboScope.values())))
                .build();
        }

        public override AuthResponse revoke(AuthToken authToken)
        {
            string response = doGetRevoke(authToken);
            var obj = JsonExtensions.parseObject(response);
            if (obj.containsKey("error"))
            {
                return new AuthResponse(AuthResponseStatus.FAILURE.GetCode(), obj.getString("error"));
            }
            // 返回 result = true 表示取消授权成功，否则失败
            AuthResponseStatus status = obj.getBooleanValue("result") ? AuthResponseStatus.SUCCESS : AuthResponseStatus.FAILURE;
            return new AuthResponse(status.GetCode(), status.GetDesc());
        }
    }
}
