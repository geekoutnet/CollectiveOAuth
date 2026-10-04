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


                // ===== 批次 C =====


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
