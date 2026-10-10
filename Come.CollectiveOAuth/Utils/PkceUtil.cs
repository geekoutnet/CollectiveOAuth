using System;
using System.Security.Cryptography;
using System.Text;

namespace Come.CollectiveOAuth.Utils
{
    /// <summary>
    /// 对应 JustAuth PkceUtil
    /// </summary>
    public static class PkceUtil
    {
        public static string generateCodeVerifier()
        {
            return Base64Utils.encodeUrlSafe(RandomUtil.randomString(50));
        }

        public static string generateCodeChallenge(string codeChallengeMethod, string codeVerifier)
        {
            if ("S256".Equals(codeChallengeMethod, StringComparison.OrdinalIgnoreCase))
            {
                using (var sha256 = SHA256.Create())
                {
                    return Base64Utils.encodeUrlSafe(sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier)));
                }
            }
            return codeVerifier;
        }

        public static string newStringUsAscii(byte[] bytes)
        {
            return Encoding.ASCII.GetString(bytes);
        }
    }
}
