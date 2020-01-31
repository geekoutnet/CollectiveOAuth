using System.Collections.Generic;
using System.Linq;
using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Request;
using Come.CollectiveOAuth.Utils;

namespace Come.Web.Sample
{
    public class AuthRequestFactory
    {

        #region 从Webconfig中获取默认配置（可以改造成从数据库中读取）
        public Dictionary<string, ClientConfig> _clientConfigs;

        public Dictionary<string, ClientConfig> ClientConfigs
        {
            get
            {
                if (_clientConfigs == null)
                {
                    var _defaultPrefix = "CollectiveOAuth_";
                    _clientConfigs = new Dictionary<string, ClientConfig>();

                    #region 或者默认授权列表数据
                    var defaultAuthList = typeof(DefaultAuthSourceEnum).ToList().Select(a => a.Name.ToUpper()).ToList();
                    foreach (var authSource in defaultAuthList)
                    {
                        var clientConfig = new ClientConfig();
                        clientConfig.clientId = AppSettingUtils.GetStrValue($"{_defaultPrefix}{authSource}_ClientId");
                        clientConfig.clientSecret = AppSettingUtils.GetStrValue($"{_defaultPrefix}{authSource}_ClientSecret");
                        clientConfig.redirectUri = AppSettingUtils.GetStrValue($"{_defaultPrefix}{authSource}_RedirectUri");
                        clientConfig.alipayPublicKey = AppSettingUtils.GetStrValue($"{_defaultPrefix}{authSource}_AlipayPublicKey");
                        clientConfig.unionId = AppSettingUtils.GetStrValue($"{_defaultPrefix}{authSource}_UnionId");
                        clientConfig.stackOverflowKey = AppSettingUtils.GetStrValue($"{_defaultPrefix}{authSource}_StackOverflowKey");
                        clientConfig.agentId = AppSettingUtils.GetStrValue($"{_defaultPrefix}{authSource}_AgentId");
                        clientConfig.scope = AppSettingUtils.GetStrValue($"{_defaultPrefix}{authSource}_Scope");
                        _clientConfigs.Add(authSource, clientConfig);
                    }
                    #endregion
                }
                return _clientConfigs;
            }
        }


        public ClientConfig GetClientConfig(string authSource)
        {
            if (authSource.IsNullOrWhiteSpace())
            {
                return null;
            }

            if (!ClientConfigs.ContainsKey(authSource))
            {
                return null;
            }
            else
            {
                return ClientConfigs[authSource];
            }
        } 
        #endregion

        /**
        * 返回AuthRequest对象
        *
        * @return {@link AuthRequest}
        */
        public IAuthRequest getRequest(string authSource)
        {
            // 获取 CollectiveOAuth 中已存在的
            IAuthRequest authRequest = getDefaultRequest(authSource);
            return authRequest;
        }


        /// <summary>
        /// 获取默认的 Request
        /// </summary>
        /// <param name="authSource"></param>
        /// <returns>{@link AuthRequest}</returns>
        private IAuthRequest getDefaultRequest(string authSource)
        {
            ClientConfig clientConfig = GetClientConfig(authSource);
            IAuthStateCache authStateCache = new DefaultAuthStateCache();

            DefaultAuthSourceEnum authSourceEnum = GlobalAuthUtil.enumFromString<DefaultAuthSourceEnum>(authSource);

            switch (authSourceEnum)
            {

                case DefaultAuthSourceEnum.WECHAT_MP:
                    return new WeChatMpAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.WECHAT_OPEN:
                    return new WeChatOpenAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.WECHAT_ENTERPRISE:
                    return new WeChatEnterpriseAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.WECHAT_ENTERPRISE_SCAN:
                    return new WeChatEnterpriseScanAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.ALIPAY_MP:
                    return new AlipayMpAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.GITEE:
                    return new GiteeAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.GITHUB:
                    return new GithubAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.BAIDU:
                    return new BaiduAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.XIAOMI:
                    return new XiaoMiAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.DINGTALK_SCAN:
                    return new DingTalkScanAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.OSCHINA:
                    return new OschinaAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.CODING:
                    return new CodingAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.LINKEDIN:
                    return new LinkedInAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.WEIBO:
                    return new WeiboAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.QQ:
                    return new QQAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.DOUYIN:
                    return new DouyinAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.GOOGLE:
                    return new GoogleAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.FACEBOOK:
                    return new FackbookAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.MICROSOFT:
                    return new MicrosoftAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.TOUTIAO:
                    return new ToutiaoAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.TEAMBITION:
                    return new TeambitionAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.RENREN:
                    return new RenrenAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.PINTEREST:
                    return new PinterestAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.STACK_OVERFLOW:
                    return new StackOverflowAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.HUAWEI:
                    return new HuaweiAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.KUJIALE:
                    return new KujialeAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.GITLAB:
                    return new GitlabAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.MEITUAN:
                    return new MeituanAuthRequest(clientConfig, authStateCache);

                case DefaultAuthSourceEnum.ELEME:
                    return new ElemeAuthRequest(clientConfig, authStateCache);

                default:
                    return null;
            }
        }
    }
}