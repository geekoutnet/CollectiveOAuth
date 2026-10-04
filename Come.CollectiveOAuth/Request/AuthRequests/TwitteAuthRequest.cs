using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Come.CollectiveOAuth.Request
{
    /**
     * Twitter登录，对齐 JustAuth AuthTwitterRequest
     */
    public class TwitteAuthRequest : DefaultAuthRequest
    {
        private const string PREAMBLE = "OAuth";

        public TwitteAuthRequest(ClientConfig config) : base(config, new TwitterAuthSource())
        {
        }

        public TwitteAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
            : base(config, new TwitterAuthSource(), authStateCache)
        {
        }

        /**
         * 返回带{@code state}参数的授权url（Twitter 不使用 state，与 JustAuth 一致）
         *
         * @param state state 验证授权流程的参数，可以防止csrf
         * @return 返回授权地址
         */
        public override string authorize(string state)
        {
            AuthToken token = this.getRequestToken();
            return UrlBuilder.fromBaseUrl(source.authorize())
                .queryParam("oauth_token", token.oauthToken)
                .build();
        }

        /**
         * Obtaining a request token
         * https://developer.twitter.com/en/docs/twitter-for-websites/log-in-with-twitter/guides/implementing-sign-in-with-twitter
         *
         * @return request token
         */
        public AuthToken getRequestToken()
        {
            string baseUrl = "https://api.twitter.com/oauth/request_token";

            var oauthParams = buildOauthParams();
            oauthParams["oauth_callback"] = config.redirectUri;
            oauthParams["oauth_signature"] = GlobalAuthUtil.generateTwitterSignature(oauthParams, "POST", baseUrl, config.clientSecret, null);
            string header = buildHeader(oauthParams);

            var httpHeader = new Dictionary<string, object>
            {
                { "Authorization", header },
                { "User-Agent", "'JustAuth' HTTP Client Simple-Http" }
            };
            string requestToken = HttpUtils.RequestPost(baseUrl, null, httpHeader, null, config.httpConfig);

            var res = parseStringToMap(requestToken);

            var authToken = new AuthToken();
            authToken.oauthToken = getValue(res, "oauth_token");
            authToken.oauthTokenSecret = getValue(res, "oauth_token_secret");
            // Java Boolean.valueOf(String)：仅忽略大小写的 "true" 为 true
            authToken.oauthCallbackConfirmed = "true".Equals(getValue(res, "oauth_callback_confirmed"), StringComparison.OrdinalIgnoreCase);
            return authToken;
        }

        /**
         * Convert request token to access token
         * https://developer.twitter.com/en/docs/twitter-for-websites/log-in-with-twitter/guides/implementing-sign-in-with-twitter
         *
         * @return access token
         */
        protected override AuthToken getAccessToken(AuthCallback authCallback)
        {
            var oauthParams = buildOauthParams();
            oauthParams["oauth_token"] = authCallback.oauth_token;
            oauthParams["oauth_verifier"] = authCallback.oauth_verifier;
            oauthParams["oauth_signature"] = GlobalAuthUtil.generateTwitterSignature(oauthParams, "POST", source.accessToken(), config.clientSecret, authCallback.oauth_token);
            string header = buildHeader(oauthParams);

            var httpHeader = new Dictionary<string, object>
            {
                { "Authorization", header },
                { "Content-Type", "application/x-www-form-urlencoded" }
            };

            var form = new Dictionary<string, string> { { "oauth_verifier", authCallback.oauth_verifier } };
            string response = HttpUtils.RequestFormPost(source.accessToken(), GlobalAuthUtil.parseMapToString(form, true), httpHeader, null, config.httpConfig);

            var requestToken = parseStringToMap(response);

            var authToken = new AuthToken();
            authToken.oauthToken = getValue(requestToken, "oauth_token");
            authToken.oauthTokenSecret = getValue(requestToken, "oauth_token_secret");
            authToken.userId = getValue(requestToken, "user_id");
            authToken.screenName = getValue(requestToken, "screen_name");
            return authToken;
        }

        protected override AuthUser getUserInfo(AuthToken authToken)
        {
            var queryParams = new Dictionary<string, string>
            {
                { "include_entities", "true" },
                { "include_email", "true" }
            };

            var oauthParams = buildOauthParams();
            oauthParams["oauth_token"] = authToken.oauthToken;

            var signParams = new Dictionary<string, string>(oauthParams);
            foreach (var item in queryParams)
            {
                signParams[item.Key] = item.Value;
            }
            oauthParams["oauth_signature"] = GlobalAuthUtil.generateTwitterSignature(signParams, "GET", source.userInfo(), config.clientSecret, authToken.oauthTokenSecret);
            string header = buildHeader(oauthParams);

            var httpHeader = new Dictionary<string, object> { { "Authorization", header } };
            string response = HttpUtils.RequestGet(userInfoUrl(authToken), httpHeader, config.httpConfig);
            var userInfo = JsonExtensions.parseObject(response);

            var authUser = new AuthUser();
            authUser.rawUserInfo = userInfo;
            authUser.uuid = userInfo.getString("id_str");
            authUser.username = userInfo.getString("screen_name");
            authUser.nickname = userInfo.getString("name");
            authUser.remark = userInfo.getString("description");
            // Java 先后两次设置 avatar，以后一次（profile_image_url）为准
            authUser.avatar = userInfo.getString("profile_image_url");
            authUser.blog = userInfo.getString("url");
            authUser.location = userInfo.getString("location");
            authUser.email = userInfo.getString("email");
            authUser.source = source.getName();
            authUser.token = authToken;
            authUser.originalUser = userInfo;
            authUser.originalUserStr = response;
            return authUser;
        }

        protected override string userInfoUrl(AuthToken authToken)
        {
            // Java 传入 boolean true，toString 为 "true"；C# bool.ToString() 为 "True"，所以这里传字符串
            return UrlBuilder.fromBaseUrl(source.userInfo())
                .queryParam("include_entities", "true")
                .queryParam("include_email", "true")
                .build();
        }

        private Dictionary<string, string> buildOauthParams()
        {
            return new Dictionary<string, string>
            {
                { "oauth_consumer_key", config.clientId },
                { "oauth_nonce", GlobalAuthUtil.generateNonce(32) },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_timestamp", GlobalAuthUtil.getTimestamp() },
                { "oauth_version", "1.0" }
            };
        }

        /**
         * 与 Java 一致：OAuth k1="v1", k2="v2" （末尾保留一个空格）
         */
        private string buildHeader(Dictionary<string, string> oauthParams)
        {
            var sb = new StringBuilder(PREAMBLE + " ");
            foreach (var param in oauthParams)
            {
                sb.Append(param.Key).Append("=\"").Append(GlobalAuthUtil.urlEncode(param.Value)).Append('"').Append(", ");
            }
            return sb.Remove(sb.Length - 2, 1).ToString();
        }

        private static string getValue(Dictionary<string, string> map, string key)
        {
            return map.TryGetValue(key, out var value) ? value : null;
        }

        /**
         * 对齐 simple-http MapUtil.parseStringToMap(str, false)：不解码；不含 & 时整串作为 key；重复 key 以逗号拼接
         */
        private static Dictionary<string, string> parseStringToMap(string str)
        {
            var res = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(str))
            {
                return res;
            }
            int index = str.IndexOf('?');
            if (index > -1)
            {
                str = str.Substring(index + 1);
            }
            index = str.IndexOf('#');
            if (index > -1)
            {
                str = str.Substring(0, index);
            }
            if (string.IsNullOrEmpty(str))
            {
                return res;
            }
            if (!str.Contains("&"))
            {
                res[str] = "";
                return res;
            }

            int len = str.Length;
            string name = null;
            int pos = 0;
            int i;
            for (i = 0; i < len; i++)
            {
                char c = str[i];
                if (c == '=')
                {
                    if (name == null)
                    {
                        name = str.Substring(pos, i - pos);
                    }
                    pos = i + 1;
                }
                else if (c == '&')
                {
                    if (name == null && pos != i)
                    {
                        addParam(res, str.Substring(pos, i - pos), "");
                    }
                    else if (name != null)
                    {
                        addParam(res, name, str.Substring(pos, i - pos));
                        name = null;
                    }
                    pos = i + 1;
                }
            }

            if (pos != i)
            {
                if (name == null)
                {
                    addParam(res, str.Substring(pos, i - pos), "");
                }
                else
                {
                    addParam(res, name, str.Substring(pos, i - pos));
                }
            }
            else if (name != null)
            {
                addParam(res, name, "");
            }
            return res;
        }

        private static void addParam(Dictionary<string, string> map, string key, string value)
        {
            map[key] = map.TryGetValue(key, out var old) ? old + "," + value : value;
        }
    }
}
