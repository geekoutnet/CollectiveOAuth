using System;

namespace Come.CollectiveOAuth.Utils
{
    /// <summary>
    /// 对应 JustAuth UuidUtils：32 位小写十六进制
    /// </summary>
    public static class UuidUtils
    {
        public static string getUUID()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
