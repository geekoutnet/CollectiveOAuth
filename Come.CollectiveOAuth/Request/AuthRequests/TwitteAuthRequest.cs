//using Come.CollectiveOAuth.Cache;
//using Come.CollectiveOAuth.Config;
//using Come.CollectiveOAuth.Models;
//using Come.CollectiveOAuth.Utils;
//using System;
//using System.Collections.Generic;
//using Come.CollectiveOAuth.Enums;
//
//namespace Come.CollectiveOAuth.Request
//{
//    public class TwitteAuthRequest : DefaultAuthRequest
//    {
//        public TwitteAuthRequest(ClientConfig config) : base(config, new TwitterAuthSource())
//        {
//        }
//
//        public TwitteAuthRequest(ClientConfig config, IAuthStateCache authStateCache)
//            : base(config, new TwitterAuthSource(), authStateCache)
//        {
//        }
//
//        /**
//         * Obtaining a request token
//         * https://developer.twitter.com/en/docs/twitter-for-websites/log-in-with-twitter/guides/implementing-sign-in-with-twitter
//         *
//         * @return request token
//         */
//        public AuthToken getRequestToken()
//        {
//            String baseUrl = "https://api.twitter.com/oauth/request_token";
//
//            Map<String, Object> oauthParams = buildOauthParams();
//            oauthParams.put("oauth_callback", config.getRedirectUri());
//            oauthParams.put("oauth_signature", generateTwitterSignature(oauthParams, "POST", baseUrl, config.getClientSecret(), null));
//            String header = buildHeader(oauthParams);
//            HttpResponse requestToken = HttpRequest.post(baseUrl).header("Authorization", header).execute();
//            checkResponse(requestToken);
//
//            Map<String, Object> res = GlobalAuthUtil.parseQueryToMap(requestToken.body());
//
//            return AuthToken.builder()
//                .oauthToken(res.get("oauth_token").toString())
//                .oauthTokenSecret(res.get("oauth_token_secret").toString())
//                .oauthCallbackConfirmed(Boolean.valueOf(res.get("oauth_callback_confirmed").toString()))
//                .build();
//        }
//
//        /**
//         * Convert request token to access token
//         * https://developer.twitter.com/en/docs/twitter-for-websites/log-in-with-twitter/guides/implementing-sign-in-with-twitter
//         *
//         * @return access token
//         */
//        protected override AuthToken getAccessToken(AuthCallback authCallback)
//        {
//            //Sign the request
//            /*using (HMACSHA1 hashAlgorithm = new HMACSHA1(new ASCIIEncoding().GetBytes(signingKey)))
//            {
//
//                return Convert.ToBase64String(
//                    hashAlgorithm.ComputeHash(
//                        new ASCIIEncoding().GetBytes(baseSignatureString)
//                    )
//                );
//            }*/
//
//            Map<String, Object> oauthParams = buildOauthParams();
//            oauthParams.put("oauth_token", authCallback.getOauthToken());
//            oauthParams.put("oauth_verifier", authCallback.getOauthVerifier());
//            oauthParams.put("oauth_signature", generateTwitterSignature(oauthParams, "POST", source.accessToken(), config.getClientSecret(), authCallback.getOauthToken()));
//            String header = buildHeader(oauthParams);
//            HttpResponse response = HttpRequest.post(source.accessToken())
//                .header("Authorization", header)
//                .header("Content-Type", "application/x-www-form-urlencoded")
//                .form("oauth_verifier", authCallback.getOauthVerifier())
//                .execute();
//            checkResponse(response);
//
//            Map<String, Object> requestToken = GlobalAuthUtil.parseQueryToMap(response.body());
//
//            return AuthToken.builder()
//                .oauthToken(requestToken.get("oauth_token").toString())
//                .oauthTokenSecret(requestToken.get("oauth_token_secret").toString())
//                .userId(requestToken.get("user_id").toString())
//                .screenName(requestToken.get("screen_name").toString())
//                .build();
//        }
//
//        protected override AuthUser getUserInfo(AuthToken authToken)
//        {
//            Map<String, Object> queryParams = new HashMap<>();
//            queryParams.put("user_id", authToken.getUserId());
//            queryParams.put("screen_name", authToken.getScreenName());
//            queryParams.put("include_entities", true);
//
//            Map<String, Object> oauthParams = buildOauthParams();
//            oauthParams.put("oauth_token", authToken.getOauthToken());
//
//            Map < String, Object > params = new HashMap<>(oauthParams);
//            params.putAll(queryParams);
//            oauthParams.put("oauth_signature", generateTwitterSignature(params, "GET", source.userInfo(), config.getClientSecret(), authToken.getOauthTokenSecret()));
//            String header = buildHeader(oauthParams);
//            HttpResponse response = HttpRequest.get(userInfoUrl(authToken)).header("Authorization", header).execute();
//            checkResponse(response);
//            JSONObject userInfo = JSONObject.parseObject(response.body());
//
//            return AuthUser.builder()
//                .uuid(userInfo.getString("id_str"))
//                .username(userInfo.getString("screen_name"))
//                .nickname(userInfo.getString("name"))
//                .remark(userInfo.getString("description"))
//                .avatar(userInfo.getString("profile_image_url_https"))
//                .blog(userInfo.getString("url"))
//                .location(userInfo.getString("location"))
//                .source(source.toString())
//                .token(authToken)
//                .build();
//        }
//
//        protected override string userInfoUrl(AuthToken authToken)
//        {
//            return UrlBuilder.fromBaseUrl(source.userInfo())
//                .queryParam("user_id", authToken.userId)
//                .queryParam("screen_name", authToken.screenName)
//                .queryParam("include_entities", true)
//                .build();
//        }
//
//        private Map<String, Object> buildOauthParams()
//        {
//            Map < String, Object > params = new HashMap<>();
//        params.put("oauth_consumer_key", config.getClientId());
//        params.put("oauth_nonce", GlobalAuthUtil.generateNonce(32));
//        params.put("oauth_signature_method", "HMAC-SHA1");
//        params.put("oauth_timestamp", GlobalAuthUtil.getTimestamp());
//        params.put("oauth_version", "1.0");
//            return params;
//        }
//
//        private string buildHeader(Map<String, Object> oauthParams)
//        {
//            final StringBuilder sb = new StringBuilder(PREAMBLE);
//
//            for (Map.Entry<String, Object> param : oauthParams.entrySet())
//            {
//                if (sb.length() > PREAMBLE.length())
//                {
//                    sb.append(", ");
//                }
//                sb.append(param.getKey())
//                    .append("=\"")
//                    .append(urlEncode(param.getValue().toString()))
//                    .append('"');
//            }
//
//            return sb.toString();
//        }
//
//        /**
//       * 校验请求结果
//       *
//       * @param response 请求结果
//       * @return 如果请求结果正常，则返回Exception
//       */
//        private void checkResponse(Dictionary<string, object> dic)
//        {
//            if (dic.Count == 0)
//            {
//                throw new Exception("请求返回数据为空");
//            }
//        }
//    }
//}