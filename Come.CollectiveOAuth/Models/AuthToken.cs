namespace Come.CollectiveOAuth.Models
{
    /**
     * 授权所需的token
     * @author wei.fu
     * @since 1.8
     */
    public class AuthToken
    {
        public string accessToken { get; set; }
        public int expireIn { get; set; }
        public string refreshToken { get; set; }
        public string uid { get; set; }
        public string openId { get; set; }
        public string accessCode { get; set; }
        public string unionId { get; set; }

        /**
         * Google附带属性
         */
        public string scope { get; set; }
        public string tokenType { get; set; }
        public string idToken { get; set; }

        /**
         * 小米附带属性
         */
        public string macAlgorithm { get; set; }
        public string macKey { get; set; }

        /**
         * 企业微信附带属性
         *
         * @since 1.10.0
         */
        public string code { get; set; }

        /**
         * Twitter附带属性
         *
         * @since 1.13.0
         */
        public string oauthToken { get; set; }
        public string oauthTokenSecret { get; set; }
        public string userId { get; set; }
        public string screenName { get; set; }
        public bool oauthCallbackConfirmed { get; set; }

    }
}