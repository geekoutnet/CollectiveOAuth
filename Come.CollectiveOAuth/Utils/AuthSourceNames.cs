using System.Collections.Generic;
using System.Linq;

namespace Come.CollectiveOAuth.Utils
{
    /// <summary>
    /// C# 平台名与 JustAuth 平台名的映射（仅列出不同的部分）
    /// </summary>
    public static class AuthSourceNames
    {
        private static readonly Dictionary<string, string> csharpToJustAuth = new Dictionary<string, string>
        {
            { "XIAOMI", "MI" },
            { "ALIPAY_MP", "ALIPAY" },
            { "DINGTALK_SCAN", "DINGTALK" },
            { "WECHAT_ENTERPRISE", "WECHAT_ENTERPRISE_WEB" },
            { "WECHAT_ENTERPRISE_SCAN", "WECHAT_ENTERPRISE" }
        };

        private static readonly Dictionary<string, string> justAuthToCsharp = csharpToJustAuth.ToDictionary(kv => kv.Value, kv => kv.Key);

        public static string toJustAuthName(string name)
        {
            return name != null && csharpToJustAuth.TryGetValue(name, out var value) ? value : name;
        }

        public static string fromJustAuthName(string name)
        {
            return name != null && justAuthToCsharp.TryGetValue(name, out var value) ? value : name;
        }
    }
}
