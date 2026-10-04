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

        /**
         * 是否需要申请 unionid（JustAuth 中为 boolean，这里兼容字符串配置："true" 忽略大小写视为开启）
         */
        public bool isUnionId()
        {
            return "true".Equals(unionId?.Trim(), System.StringComparison.OrdinalIgnoreCase);
        }

        /**
         * 企业微信，授权方的网页应用ID
         */
        public string usertype { get; set; }

        /**
         * 域名前缀。使用 Coding 登录和 Okta 登录时，需要传该值
         */
        public string domainPrefix { get; set; }

        /**
         * 针对国外服务可以单独设置代理
         */
        public HttpConfig httpConfig { get; set; }

        /**
         * 忽略校验 state 参数，默认不开启
         */
        public bool ignoreCheckState { get; set; }

        /**
         * 支持自定义授权平台的 scope 内容
         */
        public System.Collections.Generic.List<string> scopes { get; set; }

        /**
         * 设备ID, 设备唯一标识ID（喜马拉雅）
         */
        public string deviceId { get; set; }

        /**
         * 喜马拉雅：客户端操作系统类型，1-iOS系统，2-Android系统，3-Web
         */
        public int? clientOsType { get; set; }

        /**
         * 喜马拉雅：客户端包名
         */
        public string packId { get; set; }

        /**
         * 是否开启 PKCE 模式
         */
        public bool pkce { get; set; }

        private string _authServerId;

        /**
         * Okta 授权服务器的 ID，默认为 default
         */
        public string authServerId
        {
            get { return string.IsNullOrEmpty(_authServerId) ? "default" : _authServerId; }
            set { _authServerId = value; }
        }

        /**
         * 忽略校验 redirectUri 参数，默认不开启
         */
        public bool ignoreCheckRedirectUri { get; set; }

        /**
         * 适配 builder 模式创建 AuthRequest 对象时，Microsoft Entra ID 的 tenantId
         */
        public string tenantId { get; set; }

        /**
         * 苹果开发者账号中的密钥标识符
         */
        public string kid { get; set; }

        /**
         * 苹果开发者账号中的团队ID
         */
        public string teamId { get; set; }

        /**
         * 企业微信扫码登录类型：ServiceApp 服务商登录、CorpApp 企业自建/代开发应用登录
         */
        public string loginType { get; set; } = "CorpApp";

        /**
         * 企业微信扫码登录语言：zh 中文、en 英文
         */
        public string lang { get; set; } = "zh";

        /**
         * 钉钉：控制输出特定类型的组织列表，org_type=management 表示只输出有管理权限的组织
         */
        public string dingTalkOrgType { get; set; }

        /**
         * 钉钉：用于指定用户需要选择的组织
         */
        public string dingTalkCorpId { get; set; }

        /**
         * 钉钉：true 表示专属帐号登录，展示组织代码输入页
         */
        public bool dingTalkExclusiveLogin { get; set; }

        /**
         * 钉钉：开启了专属帐号功能的组织 corpId
         */
        public string dingTalkExclusiveCorpId { get; set; }
    }
}