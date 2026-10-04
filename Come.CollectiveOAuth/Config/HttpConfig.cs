using System.Net;

namespace Come.CollectiveOAuth.Config
{
    /// <summary>
    /// HTTP 配置，对应 simple-http HttpConfig
    /// </summary>
    public class HttpConfig
    {
        /// <summary>
        /// 超时时间（毫秒），小于等于 0 表示不限制
        /// </summary>
        public int timeout { get; set; }

        /// <summary>
        /// 代理，为空时不使用代理
        /// </summary>
        public IWebProxy proxy { get; set; }
    }
}
