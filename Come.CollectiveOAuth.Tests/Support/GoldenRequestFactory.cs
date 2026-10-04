using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Request;

namespace Come.CollectiveOAuth.Tests.Support
{
    /// <summary>
    /// JustAuth 平台名 → C# 请求类。阶段 2/3 每对齐或新增一个平台就在这里登记
    /// </summary>
    internal static class GoldenRequestFactory
    {
        private static readonly Dictionary<string, Func<ClientConfig, IAuthStateCache, IAuthRequest>> creators =
            new Dictionary<string, Func<ClientConfig, IAuthStateCache, IAuthRequest>>
            {
                { "GITHUB", (c, s) => new GithubAuthRequest(c, s) },

                // ===== 批次 A =====
                { "WECHAT_ENTERPRISE", (c, s) => new WeChatEnterpriseScanAuthRequest(c, s) },
                { "WECHAT_ENTERPRISE_WEB", (c, s) => new WeChatEnterpriseAuthRequest(c, s) },
                { "WECHAT_OPEN", (c, s) => new WeChatOpenAuthRequest(c, s) },
                { "WECHAT_MP", (c, s) => new WeChatMpAuthRequest(c, s) },


                // ===== 批次 B =====
                { "GITEE", (c, s) => new GiteeAuthRequest(c, s) },
                { "BAIDU", (c, s) => new BaiduAuthRequest(c, s) },
                { "MI", (c, s) => new XiaoMiAuthRequest(c, s) },
                { "OSCHINA", (c, s) => new OschinaAuthRequest(c, s) },
                { "CODING", (c, s) => new CodingAuthRequest(c, s) },
                { "WEIBO", (c, s) => new WeiboAuthRequest(c, s) },
                { "QQ", (c, s) => new QQAuthRequest(c, s) },


                // ===== 批次 C =====
                { "DINGTALK", (c, s) => new DingTalkScanAuthRequest(c, s) },
                { "DOUYIN", (c, s) => new DouyinAuthRequest(c, s) },
                { "TOUTIAO", (c, s) => new ToutiaoAuthRequest(c, s) },
                { "HUAWEI", (c, s) => new HuaweiAuthRequest(c, s) },
                { "KUJIALE", (c, s) => new KujialeAuthRequest(c, s) },
                { "MEITUAN", (c, s) => new MeituanAuthRequest(c, s) },
                { "ELEME", (c, s) => new ElemeAuthRequest(c, s) },
                { "TAOBAO", (c, s) => new TaobaoAuthRequest(c, s) },

                // ===== 批次 D =====
                { "LINKEDIN", (c, s) => new LinkedInAuthRequest(c, s) },
                { "GOOGLE", (c, s) => new GoogleAuthRequest(c, s) },
                { "FACEBOOK", (c, s) => new FackbookAuthRequest(c, s) },
                { "MICROSOFT", (c, s) => new MicrosoftAuthRequest(c, s) },
                { "PINTEREST", (c, s) => new PinterestAuthRequest(c, s) },
                { "STACK_OVERFLOW", (c, s) => new StackOverflowAuthRequest(c, s) },
                { "GITLAB", (c, s) => new GitlabAuthRequest(c, s) },
                { "TEAMBITION", (c, s) => new TeambitionAuthRequest(c, s) },
                { "RENREN", (c, s) => new RenrenAuthRequest(c, s) },


                // ===== 批次 E =====
                { "TWITTER", (c, s) => new TwitteAuthRequest(c, s) },
                { "ALIPAY", (c, s) => new AlipayMpAuthRequest(c, s) },

            };

        public static bool IsRegistered(string justAuthName)
        {
            return creators.ContainsKey(justAuthName);
        }

        public static IAuthRequest Create(string justAuthName, ClientConfig config, IAuthStateCache cache)
        {
            return creators[justAuthName](config, cache);
        }
    }
}
