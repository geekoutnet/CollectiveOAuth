using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Come.CollectiveOAuth.Request
{
    /**
     * 饿了么登录，对齐 JustAuth AuthElemeRequest
     */
    public class ElemeAuthRequest : DefaultAuthRequest
    {
        private const string CONTENT_TYPE_FORM = "application/x-www-form-urlencoded;charset=UTF-8";
        private const string CONTENT_TYPE_JSON = "application/json; charset=utf-8";

        public ElemeAuthRequest(ClientConfig config) : base(config, new ElemeAuthSource())
        {
        }

        public ElemeAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new ElemeAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            var form = new Dictionary<string, string>
            {
                { "client_id", config.clientId },
                { "redirect_uri", config.redirectUri },
                { "code", authCallback.code },
                { "grant_type", "authorization_code" }
            };

            var httpHeader = this.buildHeader(CONTENT_TYPE_FORM, this.getRequestId(), true);
            string response = HttpUtils.RequestFormPost(source.accessToken(), GlobalAuthUtil.parseMapToString(form, true), httpHeader, null, config.httpConfig);
            var obj = JsonExtensions.parseObject(response);

            this.checkResponse(obj);

            var authToken = new AuthToken();
            authToken.accessToken = obj.getString("access_token");
            authToken.refreshToken = obj.getString("refresh_token");
            authToken.tokenType = obj.getString("token_type");
            authToken.expireIn = obj.getIntValue("expires_in");
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            var parameters = new Dictionary<string, object>();
            // 获取商户账号信息的API接口名称
            string action = "eleme.user.getUser";
            // 时间戳（与 Java 一致使用毫秒）
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // 公共参数
            var metasHashMap = new Dictionary<string, object>
            {
                { "app_key", config.clientId },
                { "timestamp", timestamp }
            };
            string signature = GlobalAuthUtil.generateElemeSignature(config.clientId, config.clientSecret, timestamp, action, authToken.accessToken, parameters);

            string requestId = this.getRequestId();

            var paramsMap = new Dictionary<string, object>
            {
                { "nop", "1.0.0" },
                { "id", requestId },
                { "action", action },
                { "token", authToken.accessToken },
                { "metas", metasHashMap },
                { "params", parameters },
                { "signature", signature }
            };

            var httpHeader = this.buildHeader(CONTENT_TYPE_JSON, requestId, false);
            string response = HttpUtils.RequestPost(source.userInfo(), JsonConvert.SerializeObject(paramsMap), httpHeader, null, config.httpConfig);

            var obj = JsonExtensions.parseObject(response);

            // 校验请求
            if (obj.containsKey("name"))
            {
                throw new AuthException(obj.getString("message"));
            }
            if (obj.containsKey("error") && obj["error"].Type != JTokenType.Null)
            {
                throw new AuthException(obj.getJSONObject("error").getString("message"));
            }

            var result = obj.getJSONObject("result");

            var authUser = new AuthUser();
            authUser.rawUserInfo = result;
            authUser.uuid = result.getString("userId");
            authUser.username = result.getString("userName");
            authUser.nickname = result.getString("userName");
            authUser.gender = AuthUserGender.UNKNOWN;
            authUser.token = authToken;
            authUser.source = source.getName();
            authUser.originalUser = result;
            authUser.originalUserStr = response;
            return authUser;
        }

        public override AuthResponse refresh(AuthToken oldToken)
        {
            var form = new Dictionary<string, string>
            {
                { "refresh_token", oldToken.refreshToken },
                { "grant_type", "refresh_token" }
            };

            var httpHeader = this.buildHeader(CONTENT_TYPE_FORM, this.getRequestId(), true);
            string response = HttpUtils.RequestFormPost(source.refresh(), GlobalAuthUtil.parseMapToString(form, true), httpHeader, null, config.httpConfig);

            var obj = JsonExtensions.parseObject(response);

            this.checkResponse(obj);

            var authToken = new AuthToken();
            authToken.accessToken = obj.getString("access_token");
            authToken.refreshToken = obj.getString("refresh_token");
            authToken.tokenType = obj.getString("token_type");
            authToken.expireIn = obj.getIntValue("expires_in");
            return new AuthResponse(Convert.ToInt32(AuthResponseStatus.SUCCESS), null, authToken);
        }

        public override string authorize(string state)
        {
            return UrlBuilder.fromBaseUrl(base.authorize(state)).queryParam("scope", "all").build();
        }

        private string getBasic(string appKey, string appSecret)
        {
            string encodeToString = Base64Utils.encode(Encoding.UTF8.GetBytes(appKey + ":" + appSecret));
            return "Basic" + " " + encodeToString;
        }

        private Dictionary<string, object> buildHeader(string contentType, string requestId, bool auth)
        {
            var httpHeader = new Dictionary<string, object>
            {
                { "Accept", "text/xml,text/javascript,text/html" },
                { "Content-Type", contentType },
                { "Accept-Encoding", "gzip" },
                { "User-Agent", "eleme-openapi-java-sdk" },
                { "x-eleme-requestid", requestId }
            };
            if (auth)
            {
                httpHeader.Add("Authorization", this.getBasic(config.clientId, config.clientSecret));
            }
            return httpHeader;
        }

        private string getRequestId()
        {
            return (UuidUtils.getUUID() + "|" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).ToUpper();
        }

        private void checkResponse(JObject obj)
        {
            if (obj.containsKey("error"))
            {
                throw new AuthException(obj.getString("error_description"));
            }
        }

        ///编码（保留的公开方法）
        public string encodeBase64(string contentStr, string encodeType = "utf-8")
        {
            string encode = "";
            byte[] bytes = Encoding.GetEncoding(encodeType).GetBytes(contentStr);
            try
            {
                encode = Convert.ToBase64String(bytes);
            }
            catch
            {
                encode = contentStr;
            }
            return encode;
        }

        ///解码（保留的公开方法）
        public string decodeBase64(string contentStr, string encodeType = "utf-8")
        {
            string decode = "";
            byte[] bytes = Convert.FromBase64String(contentStr);
            try
            {
                decode = Encoding.GetEncoding(encodeType).GetString(bytes);
            }
            catch
            {
                decode = contentStr;
            }
            return decode;
        }

        /**
         * 生成饿了么请求的Signature（保留的公开方法，改为与 JustAuth 相同的算法）
         */
        public string generateElemeSignature(long timestamp, string action, string token)
        {
            return GlobalAuthUtil.generateElemeSignature(config.clientId, config.clientSecret, timestamp, action, token, new Dictionary<string, object>());
        }

        /// <summary>
        /// 对字符串进行Md5加密，isUpper为True时返回大写，反之小写（保留的公开方法）
        /// </summary>
        public static string hashMd5String(string willMd5Str, bool isUpper = true)
        {
            StringBuilder sb = new StringBuilder();
            using (var md5 = MD5.Create())
            {
                byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(willMd5Str));
                for (int i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString("X2"));
                }
            }
            return isUpper ? sb.ToString().ToUpper() : sb.ToString().ToLower();
        }
    }
}
