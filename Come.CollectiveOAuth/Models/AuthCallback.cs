namespace Come.CollectiveOAuth.Models
{
    public class AuthCallback
    {
        private string _code;

        /**
         * 访问AuthorizeUrl后回调时带的参数code，为空时取 auth_code（与 JustAuth 一致）
         */
        public string code
        {
            get { return string.IsNullOrEmpty(_code) ? auth_code : _code; }
            set { _code = value; }
        }

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
         */
        public string authorization_code { get; set; }

        /**
         * Twitter回调后返回的oauth_token
         */
        public string oauth_token { get; set; }

        /**
         * Twitter回调后返回的oauth_verifier
         */
        public string oauth_verifier { get; set; }

        /**
         * 苹果仅在用户首次授权应用程序时返回此值
         */
        public string user { get; set; }

        /**
         * 苹果错误信息，仅在用户取消授权时返回此值
         */
        public string error { get; set; }

        /**
         * 兼容旧版本，等同于 oauth_token
         */
        [Newtonsoft.Json.JsonIgnore]
        public string oauthToken
        {
            get { return oauth_token; }
            set { oauth_token = value; }
        }

        /**
         * 兼容旧版本，等同于 oauth_verifier
         */
        [Newtonsoft.Json.JsonIgnore]
        public string oauthVerifier
        {
            get { return oauth_verifier; }
            set { oauth_verifier = value; }
        }
    }
}