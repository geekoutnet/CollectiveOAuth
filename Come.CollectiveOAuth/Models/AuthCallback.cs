namespace Come.CollectiveOAuth.Models
{
    public class AuthCallback
    {
        /**
     * 访问AuthorizeUrl后回调时带的参数code
     */
        public string code { get; set; }

        /**
         * 访问AuthorizeUrl后回调时带的参数auth_code，该参数目前只使用于支付宝登录
         */
        public string auth_code { get; set; }

        /**
         * 访问AuthorizeUrl后回调时带的参数state，用于和请求AuthorizeUrl前的state比较，防止CSRF攻击
         */
        public string state { get; set; }

        /**
         * 华为授权登录接受code的参数名
         *
         * @since 1.10.0
         */
        public string authorization_code { get; set; }

        /**
         * Twitter回调后返回的oauth_token
         *
         * @since 1.13.0
         */
        public string oauthToken { get; set; }

        /**
         * Twitter回调后返回的oauth_verifier
         *
         * @since 1.13.0
         */
        public string oauthVerifier { get; set; }
    }
}