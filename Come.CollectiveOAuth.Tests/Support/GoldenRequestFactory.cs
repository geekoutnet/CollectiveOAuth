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


                // ===== 批次 B =====
                { "GITEE", (c, s) => new GiteeAuthRequest(c, s) },
                { "BAIDU", (c, s) => new BaiduAuthRequest(c, s) },
                { "MI", (c, s) => new XiaoMiAuthRequest(c, s) },
                { "OSCHINA", (c, s) => new OschinaAuthRequest(c, s) },
                { "CODING", (c, s) => new CodingAuthRequest(c, s) },
                { "WEIBO", (c, s) => new WeiboAuthRequest(c, s) },
                { "QQ", (c, s) => new QQAuthRequest(c, s) },


                // ===== 批次 C =====


                // ===== 批次 D =====


                // ===== 批次 E =====

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
