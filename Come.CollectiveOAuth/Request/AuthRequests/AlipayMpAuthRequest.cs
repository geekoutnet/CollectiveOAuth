using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using Aop.Api;
using Aop.Api.Request;
using Aop.Api.Response;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 支付宝公钥模式登录，对齐 JustAuth AuthAlipayRequest
     * <p>
     * 与 Java 的差异：C# 支付宝 SDK（DefaultAopClient）不支持代理，config.httpConfig.proxy 对本平台无效
     */
    public partial class AlipayMpAuthRequest : DefaultAuthRequest
    {
        private const string GATEWAY = "https://openapi.alipay.com/gateway.do";

        /**
         * 支付宝公钥：当选择支付宝登录时，该值可用
         * 对应“RSA2(SHA256)密钥”中的“支付宝公钥”
         */
        private readonly string alipayPublicKey;

        private readonly IAopClient aopClient;

        /**
         * 未传 alipayPublicKey 时使用 config.alipayPublicKey（Java 中已标记为 Deprecated）
         */
        public AlipayMpAuthRequest(ClientConfig config) : this(config, (string)null)
        {
        }

        /**
         * 未传 alipayPublicKey 时使用 config.alipayPublicKey（Java 中已标记为 Deprecated）
         */
        public AlipayMpAuthRequest(ClientConfig config, IAuthStateCache authStateCache) : this(config, null, authStateCache)
        {
        }

        /**
         * @param config          公共的OAuth配置
         * @param alipayPublicKey 支付宝公钥
         */
        public AlipayMpAuthRequest(ClientConfig config, string alipayPublicKey) : base(config, new AlipayMPAuthSource())
        {
            this.alipayPublicKey = determineAlipayPublicKey(alipayPublicKey, config);
            check(config);
            this.aopClient = createClient(config);
        }

        /**
         * @param config          公共的OAuth配置
         * @param alipayPublicKey 支付宝公钥
         * @param authStateCache  state 缓存
         */
        public AlipayMpAuthRequest(ClientConfig config, string alipayPublicKey, IAuthStateCache authStateCache)
            : base(config, new AlipayMPAuthSource(), authStateCache)
        {
            this.alipayPublicKey = determineAlipayPublicKey(alipayPublicKey, config);
            check(config);
            this.aopClient = createClient(config);
        }

        private IAopClient createClient(ClientConfig config)
        {
            // Java: new DefaultAlipayClient(GATEWAY, clientId, clientSecret, "json", "UTF-8", alipayPublicKey, "RSA2")
            return new DefaultAopClient(GATEWAY, config.clientId, config.clientSecret, "json", "1.0", "RSA2", this.alipayPublicKey, "UTF-8", false);
        }

        private static string determineAlipayPublicKey(string alipayPublicKey, ClientConfig config)
        {
            return alipayPublicKey != null ? alipayPublicKey : config.alipayPublicKey;
        }

        protected virtual void check(ClientConfig config)
        {
            AuthChecker.checkConfig(config, source);

            if (string.IsNullOrEmpty(alipayPublicKey))
            {
                throw new AuthException(AuthResponseStatus.PARAMETER_INCOMPLETE, source);
            }

            // 支付宝在创建回调地址时，不允许使用localhost或者127.0.0.1
            if (GlobalAuthUtil.isLocalHost(config.redirectUri))
            {
                throw new AuthException(AuthResponseStatus.ILLEGAL_REDIRECT_URI, source);
            }
        }

        protected override void checkCode(AuthCallback authCallback)
        {
            if (string.IsNullOrEmpty(authCallback.auth_code))
            {
                throw new AuthException(AuthResponseStatus.ILLEGAL_CODE, source);
            }
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            AlipaySystemOauthTokenRequest request = new AlipaySystemOauthTokenRequest();
            request.GrantType = "authorization_code";
            request.Code = authCallback.auth_code;
            AlipaySystemOauthTokenResponse response;
            try
            {
                response = this.aopClient.Execute(request);
            }
            catch (Exception e)
            {
                throw new AuthException(e);
            }
            // Java response.isSuccess() 即 subCode 为空，与 C# !IsError 等价
            if (response.IsError)
            {
                throw new AuthException(response.SubMsg);
            }

            var authToken = new AuthToken();
            authToken.accessToken = response.AccessToken;
            authToken.uid = response.UserId;
            authToken.expireIn = int.Parse(response.ExpiresIn);
            authToken.refreshToken = response.RefreshToken;
            return authToken;
        }

        /**
         * 刷新access token （续期）
         *
         * @param authToken 登录成功后返回的Token信息
         * @return AuthResponse
         */
        public override AuthResponse refresh(AuthToken authToken)
        {
            AlipaySystemOauthTokenRequest request = new AlipaySystemOauthTokenRequest();
            request.GrantType = "refresh_token";
            request.RefreshToken = authToken.refreshToken;
            AlipaySystemOauthTokenResponse response;
            try
            {
                response = this.aopClient.Execute(request);
            }
            catch (Exception e)
            {
                throw new AuthException(e);
            }
            if (response.IsError)
            {
                throw new AuthException(response.SubMsg);
            }

            var token = new AuthToken();
            token.accessToken = response.AccessToken;
            token.uid = response.UserId;
            token.expireIn = int.Parse(response.ExpiresIn);
            token.refreshToken = response.RefreshToken;
            return new AuthResponse(Convert.ToInt32(AuthResponseStatus.SUCCESS), null, token);
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            string accessToken = authToken.accessToken;
            AlipayUserInfoShareRequest request = new AlipayUserInfoShareRequest();
            AlipayUserInfoShareResponse response;
            try
            {
                response = this.aopClient.Execute(request, accessToken);
            }
            catch (AopException e)
            {
                throw new AuthException(e.ErrorMsg, e);
            }
            if (response.IsError)
            {
                throw new AuthException(response.SubMsg);
            }

            string province = response.Province, city = response.City;
            string location = string.Format("{0} {1}", string.IsNullOrEmpty(province) ? "" : province, string.IsNullOrEmpty(city) ? "" : city);

            var authUser = new AuthUser();
            authUser.rawUserInfo = toRawUserInfo(response);
            authUser.uuid = response.UserId;
            authUser.username = string.IsNullOrEmpty(response.UserName) ? response.NickName : response.UserName;
            authUser.nickname = response.NickName;
            authUser.avatar = response.Avatar;
            authUser.location = location;
            authUser.gender = GlobalAuthUtil.getRealGender(response.Gender);
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = response;
            authUser.originalUserStr = JsonConvert.SerializeObject(response);
            return authUser;
        }

        /**
         * 对应 Java JSONObject.parseObject(JSONObject.toJSONString(response))：
         * fastjson 按 getter 输出驼峰属性名、省略 null，isSuccess() 输出为 success
         */
        private static JObject toRawUserInfo(AopResponse response)
        {
            var serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            });
            var obj = JObject.FromObject(response, serializer);
            obj.Remove("isError");
            obj["success"] = !response.IsError;
            return obj;
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
                .queryParam("app_id", config.clientId)
                .queryParam("scope", "auth_user")
                .queryParam("redirect_uri", config.redirectUri)
                .queryParam("state", getRealState(state))
                .build();
        }
    }
}
