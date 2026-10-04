using System;
using System.Text;

namespace Come.CollectiveOAuth.Utils
{
    /// <summary>
    /// 对应 JustAuth Base64Utils，URL 安全模式不带填充
    /// </summary>
    public static class Base64Utils
    {
        public static string encode(byte[] source)
        {
            return source == null ? null : Convert.ToBase64String(source);
        }

        public static string encode(string source)
        {
            return source == null ? null : encode(Encoding.UTF8.GetBytes(source));
        }

        public static string encodeUrlSafe(byte[] source)
        {
            return source == null ? null : Convert.ToBase64String(source).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static string encodeUrlSafe(string source)
        {
            return source == null ? null : encodeUrlSafe(Encoding.UTF8.GetBytes(source));
        }
    }
}
