using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 钉钉登录抽象类，对齐 JustAuth AbstractAuthDingtalkRequest
     */
    public abstract class AbstractDingTalkAuthRequest : DefaultAuthRequest
    {
        protected AbstractDingTalkAuthRequest(ClientConfig config, IAuthSource source) : base(config, source)
        {
        }

        protected AbstractDingTalkAuthRequest(ClientConfig config, IAuthSource source, IAuthStateCache authStateCache)
            : base(config, source, authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            var authToken = new AuthToken();
            authToken.accessCode = authCallback.code;
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string code = authToken.accessCode;
            var param = new JObject();
            param["tmp_auth_code"] = code;
            string response = HttpUtils.RequestPost(userInfoUrl(authToken), param.ToString(Formatting.None), null, null, config.httpConfig);
            var obj = JsonExtensions.parseObject(response);
            if (obj.getIntValue("errcode") != 0)
            {
                throw new AuthException(obj.getString("errmsg"));
            }
            obj = obj.getJSONObject("user_info");

            var token = new AuthToken();
            token.openId = obj.getString("openid");
            token.unionId = obj.getString("unionid");

            var authUser = new AuthUser();
            authUser.rawUserInfo = obj;
            authUser.uuid = obj.getString("unionid");
            authUser.nickname = obj.getString("nick");
            authUser.username = obj.getString("nick");
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.source = source.getName();
            authUser.token = token;
            authUser.originalUser = obj;
            authUser.originalUserStr = response;
            return authUser;
        }

        /**
         * 返回带{@code state}参数的授权url，授权回调时会带上这个{@code state}
         */
        public override string authorize(string state)
        {
            return UrlBuilder.fromBaseUrl(source.authorize())
                .queryParam("response_type", "code")
                .queryParam("appid", config.clientId)
                .queryParam("scope", "snsapi_login")
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("state", getRealState(state))
                .build();
        }

        /**
         * 返回获取userInfo的url
         */
        protected override string userInfoUrl(AuthToken authToken)
        {
            // 根据timestamp, appSecret计算签名值
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            // Java 版先 urlEncode 再交给不编码的 UrlBuilder；C# UrlBuilder 默认编码，这里传原值
            string signature = GlobalAuthUtil.urlDecode(GlobalAuthUtil.generateDingTalkSignature(config.clientSecret, timestamp));

            return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("signature", signature)
                .queryParam("timestamp", timestamp)
                .queryParam("accessKey", config.clientId)
                .build();
        }
    }
}
