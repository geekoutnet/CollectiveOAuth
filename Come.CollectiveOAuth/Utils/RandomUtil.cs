using System;
using System.Security.Cryptography;
using System.Text;

namespace Come.CollectiveOAuth.Utils
{
    /// <summary>
    /// 对应 JustAuth RandomUtil
    /// </summary>
    public static class RandomUtil
    {
        public const string BASE_CHAR_NUMBER = "abcdefghijklmnopqrstuvwxyz0123456789";

        private static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();

        /// <summary>
        /// 返回 [0, bound) 的随机整数
        /// </summary>
        public static int nextInt(int bound)
        {
            var bytes = new byte[4];
            lock (rng)
            {
                rng.GetBytes(bytes);
            }
            return (int)(BitConverter.ToUInt32(bytes, 0) % (uint)bound);
        }

        public static string randomString(int length)
        {
            if (length < 1)
            {
                length = 1;
            }
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(BASE_CHAR_NUMBER[nextInt(BASE_CHAR_NUMBER.Length)]);
            }
            return sb.ToString();
        }
    }
}
