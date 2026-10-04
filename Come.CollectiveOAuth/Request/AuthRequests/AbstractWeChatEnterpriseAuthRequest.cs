using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 企业微信登录父类，对齐 JustAuth AbstractAuthWeChatEnterpriseRequest
     */
    public abstract class AbstractWeChatEnterpriseAuthRequest : DefaultAuthRequest
    {
        protected AbstractWeChatEnterpriseAuthRequest(ClientConfig config, IAuthSource source) : base(config, source)
        {
        }

        protected AbstractWeChatEnterpriseAuthRequest(ClientConfig config, IAuthSource source, IAuthStateCache authStateCache)
            : base(config, source, authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            string response = doGetAuthorizationCode(accessTokenUrl(null));

            JObject obj = this.checkResponse(response);

            var authToken = new AuthToken();
            authToken.accessToken = obj.getString("access_token");
            authToken.expireIn = obj.getIntValue("expires_in");
            authToken.code = authCallback.code;
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string response = doGetUserInfo(authToken);
            JObject obj = this.checkResponse(response);

            // 返回 OpenId 或其他，均代表非当前企业用户，不支持
            if (!obj.containsKey("UserId"))
            {
                throw new AuthException(AuthResponseStatus.UNIDENTIFIED_PLATFORM, source);
            }
            string userId = obj.getString("UserId");
            string userTicket = obj.getString("user_ticket");
            JObject userDetail = getUserDetail(authToken.accessToken, userId, userTicket);

            var authUser = new AuthUser();
            authUser.rawUserInfo = userDetail;
            authUser.username = userDetail.getString("name");
            authUser.nickname = userDetail.getString("alias");
            authUser.avatar = userDetail.getString("avatar");
            authUser.location = userDetail.getString("address");
            authUser.email = userDetail.getString("email");
            authUser.uuid = userId;
            authUser.gender = GlobalAuthUtil.getWechatRealGender(userDetail.getString("gender"));
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = userDetail;
            authUser.originalUserStr = userDetail.ToString(Formatting.None);
            return authUser;
        }

        /**
         * 校验请求结果
         *
         * @param response 请求结果
         * @return 如果请求结果正常，则返回JSONObject
         */
        private JObject checkResponse(string response)
        {
            JObject obj = JsonExtensions.parseObject(response);

            if (obj.containsKey("errcode") && obj.getIntValue("errcode") != 0)
            {
                throw new AuthException(obj.getString("errmsg"), source);
            }

            return obj;
        }

        /**
         * 返回获取accessToken的url
         *
         * @param code 授权码
         * @return 返回获取accessToken的url
         */
        protected override string accessTokenUrl(string code)
        {
            return UrlBuilder.fromBaseUrl(source.accessToken())
                .queryParam("corpid", config.clientId)
                .queryParam("corpsecret", config.clientSecret)
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
                .queryParam("access_token", authToken.accessToken)
                .queryParam("code", authToken.code)
                .build();
        }

        /**
         * 用户详情
         *
         * @param accessToken accessToken
         * @param userId      企业内用户id
         * @param userTicket  成员票据，用于获取用户信息或敏感信息
         * @return 用户详情
         */
        private JObject getUserDetail(string accessToken, string userId, string userTicket)
        {
            // 用户基础信息
            string userInfoUrl = UrlBuilder.fromBaseUrl("https://qyapi.weixin.qq.com/cgi-bin/user/get")
                .queryParam("access_token", accessToken)
                .queryParam("userid", userId)
                .build();
            string userInfoResponse = HttpUtils.RequestGet(userInfoUrl, null, config.httpConfig);
            JObject userInfo = checkResponse(userInfoResponse);

            // 用户敏感信息
            if (!string.IsNullOrEmpty(userTicket))
            {
                string userDetailUrl = UrlBuilder.fromBaseUrl("https://qyapi.weixin.qq.com/cgi-bin/auth/getuserdetail")
                    .queryParam("access_token", accessToken)
                    .build();
                var param = new JObject { { "user_ticket", userTicket } };
                string userDetailResponse = HttpUtils.RequestPost(userDetailUrl, param.ToString(Formatting.None), null, null, config.httpConfig);
                JObject userDetail = checkResponse(userDetailResponse);

                // 与 Java putAll 一致：同名字段覆盖
                foreach (var property in userDetail.Properties())
                {
                    userInfo[property.Name] = property.Value;
                }
            }
            return userInfo;
        }
    }
}
