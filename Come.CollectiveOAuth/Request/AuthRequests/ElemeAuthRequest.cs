using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Enums;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Come.CollectiveOAuth.Request
{
    public class ElemeAuthRequest : DefaultAuthRequest
    {
        public ElemeAuthRequest(ClientConfig config) : base(config, new ElemeAuthSource())
        {
        }

        public ElemeAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new ElemeAuthSource(), authStateCache)
        {
        }

        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            var reqParams = new Dictionary<string, object>
            {
                { "client_id", config.clientId },
                { "redirect_uri", config.clientSecret },
                { "code", authCallback.code },
                { "grant_type", "authorization_code" },
            };

            var reqHeaders = this.getSpecialHeader(this.getRequestId());

            var response = HttpUtils.RequestFormPost(source.accessToken(), reqParams.spellParams(), reqHeaders);
            var accessTokenObject = response.parseObject();

            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken
            {
                accessToken = accessTokenObject.getString("access_token"),
                expireIn = accessTokenObject.getInt32("expires_in"),
                refreshToken = accessTokenObject.getString("refresh_token"),
                tokenType = accessTokenObject.getString("token_type"),
                code = authCallback.code
            };

            return authToken;
        }


        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            // 获取商户账号信息的API接口名称
            String action = "eleme.user.getUser";
            // 时间戳，单位秒。API服务端允许客户端请求最大时间误差为正负5分钟。
            long timestamp = DateTime.Now.Ticks;
            // 公共参数
            var metasHashMap = new Dictionary<string, object>();
            metasHashMap.Add("app_key", config.clientId);
            metasHashMap.Add("timestamp", timestamp);
            string signature = this.generateElemeSignature(timestamp, action, authToken.accessToken);
            string requestId = this.getRequestId();

            var paramsMap = new Dictionary<string, object>
            {
                { "nop", "1.0.0" },
                { "id", requestId },
                { "action", action },
                { "token", authToken.accessToken },
                { "metas", JsonConvert.SerializeObject(metasHashMap) },
                { "params", "{}" },
                { "signature", signature }
            };

            var reqHeaders = new Dictionary<string, object>
            {
                { "Content-Type", "application/json; charset=utf-8" },
                { "Accept", "text/xml,text/javascript,text/html" },
                { "Accept-Encoding", "gzip" },
                { "User-Agent", "eleme-openapi-java-sdk"},
                { "x-eleme-requestid", requestId},
                { "Authorization", this.spliceBasicAuthStr()}
            };
            var response = HttpUtils.RequestPost(source.userInfo(), JsonConvert.SerializeObject(paramsMap), reqHeaders);

            var resObj = response.parseObject();

            // 校验请求
            if (resObj.ContainsKey("name"))
            {
                throw new Exception(resObj.getString("message"));
            }
            if (resObj.ContainsKey("error") && !resObj.getString("error").IsNullOrWhiteSpace())
            {
                throw new Exception(resObj.getJSONObject("error").getString("message"));
            }

            var userObj = resObj.getJSONObject("result");

            var authUser = new AuthUser
            {
                uuid = userObj.getString("userId"),
                username = userObj.getString("userName"),
                nickname = userObj.getString("userName"),
                gender = AuthUserGender.UNKNOWN,
                token = authToken,
                source = source.getName(),
                originalUser = resObj,
                originalUserStr = response
            };
            return authUser;
        }

        public override AuthResponse refresh(AuthToken oldToken)
        {
            var reqParams = new Dictionary<string, object>
            {
                { "refresh_token", oldToken.refreshToken },
                { "grant_type", "refresh_token" },
            };

            var reqHeaders = this.getSpecialHeader(this.getRequestId());

            var response = HttpUtils.RequestFormPost(source.accessToken(), reqParams.spellParams(), reqHeaders);
            var accessTokenObject = response.parseObject();

            this.checkResponse(accessTokenObject);

            var authToken = new AuthToken
            {
                accessToken = accessTokenObject.getString("access_token"),
                refreshToken = accessTokenObject.getString("refresh_token"),
                expireIn = accessTokenObject.getInt32("expires_in"),
                tokenType = accessTokenObject.getString("token_type")
            };

            return new AuthResponse(AuthResponseStatus.SUCCESS.GetCode(), AuthResponseStatus.SUCCESS.GetDesc(), authToken);
        }

        public override string authorize(string state)
        {
            return UrlBuilder.fromBaseUrl(base.authorize(state))
                .queryParam("scope", config.scope.IsNullOrWhiteSpace() ? "all" : config.scope)
                .build();
        }

        private string spliceBasicAuthStr()
        {
            string encodeToString = encodeBase64($"{config.clientId}:{config.clientSecret}");
            return $"Basic {encodeToString}";
        }

        private Dictionary<string, object> getSpecialHeader(string requestId)
        {
            var headers = new Dictionary<string, object>
            {
                { "Content-Type", "application/x-www-form-urlencoded;charset=UTF-8" },
                { "Accept", "text/xml,text/javascript,text/html" },
                { "Accept-Encoding", "gzip" },
                { "User-Agent", "eleme-openapi-java-sdk"},
                { "x-eleme-requestid", requestId},
                { "Authorization", this.spliceBasicAuthStr()}
            };
            return headers;
        }


        private string getRequestId()
        {
            return (Guid.NewGuid().ToString() + "|" + DateTime.Now.Ticks.ToString()).ToUpper();
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
                throw new Exception($"{dic.getString("error_description")}");
            }
        }

        ///编码
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
        ///解码
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
         * 生成饿了么请求的Signature
         * <p>
         * 代码copy并修改自：https://coding.net/u/napos_openapi/p/eleme-openapi-java-sdk/git/blob/master/src/main/java/eleme/openapi/sdk/utils/SignatureUtil.java
         *
         * @param appKey     平台应用的授权key
         * @param secret     平台应用的授权密钥
         * @param timestamp  时间戳，单位秒。API服务端允许客户端请求最大时间误差为正负5分钟。
         * @param action     饿了么请求的api方法
         * @param token      用户授权的token
         * @param parameters 加密参数
         * @return Signature
         */
        public string generateElemeSignature(long timestamp, string action, string token)
        {
            Dictionary<string, object> dicList = new Dictionary<string, object>();
            dicList.Add("app_key", config.clientId);
            dicList.Add("timestamp", timestamp);

            var signStr = dicList.Sort().spellParams();
            string splice = $"{action}{token}{signStr}{config.clientSecret}";
            string calculatedSignature = hashMd5String(splice);
            return calculatedSignature;
        }

        /// <summary>
        /// 对字符串进行Md5加密，isUpper为True时返回大写，反之小写
        /// </summary>
        /// <param name="willMd5Str"></param>
        /// <param name="isUpper"></param>
        public static string hashMd5String(string willMd5Str, bool isUpper = true)
        {
            //就是比string往后一直加要好的优化容器
            StringBuilder sb = new StringBuilder();
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                //将输入字符串转换为字节数组并计算哈希。
                byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(willMd5Str));

                //X为     十六进制 X都是大写 x都为小写
                //2为 每次都是两位数
                //假设有两个数10和26，正常情况十六进制显示0xA、0x1A，这样看起来不整齐，为了好看，可以指定"X2"，这样显示出来就是：0x0A、0x1A。 
                //遍历哈希数据的每个字节
                //并将每个字符串格式化为十六进制字符串。
                int length = data.Length;
                for (int i = 0; i < length; i++)
                    sb.Append(data[i].ToString("X2"));

            }
            if (isUpper)
            {
                return sb.ToString().ToUpper();
            }
            else
            {
                return sb.ToString().ToLower();
            }
        }
    }
}