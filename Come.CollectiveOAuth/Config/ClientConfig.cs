namespace Come.CollectiveOAuth.Config
{
    /**
     * CollectiveOAuth配置类
     *
     * @author wei.fu (wei.fu@rthinkingsoft.cn)
     * @since 1.8
     */
    public class ClientConfig
    {
        /**
         * 客户端id：对应各平台的appKey
         */
        public string clientId { get; set; }

        /**
         * 客户端Secret：对应各平台的appSecret
         */
        public string clientSecret { get; set; }

        /**
         * 登录成功后的回调地址
         */
        public string redirectUri { get; set; }

        /**
         * 支付宝公钥：当选择支付宝登录时，该值可用
         * 对应“RSA2(SHA256)密钥”中的“支付宝公钥”
         */
        public string alipayPublicKey { get; set; }

        /**
         * 是否需要申请unionid，目前只针对qq登录
         * 注：qq授权登录时，获取unionid需要单独发送邮件申请权限。如果个人开发者账号中申请了该权限，可以将该值置为true，在获取openId时就会同步获取unionId
         * 参考链接：http://wiki.connect.qq.com/unionid%E4%BB%8B%E7%BB%8D
         * <p>
         * 1.7.1版本新增参数
         */
        public string unionId { get; set; }

        /**
         * Stack Overflow Key
         * <p>
         *
         * @since 1.9.0
         */
        public string stackOverflowKey { get; set; }

        /**
         * 企业微信，授权方的网页应用ID
         *
         * @since 1.10.0
         */
        public string agentId { get; set; }

        /**
         * 企业微信，授权方的网页应用ID
         *
         * @since 1.10.0
         */
        public string scope { get; set; }
    }
}