namespace Come.CollectiveOAuth.Cache
{
    /// <summary>
    /// 对应 JustAuth AuthCacheConfig
    /// </summary>
    public static class AuthCacheConfig
    {
        /// <summary>
        /// 默认缓存过期时间（毫秒）：3 分钟
        /// </summary>
        public static long timeout = 3 * 60 * 1000;

        /// <summary>
        /// 是否开启定时清理过期缓存
        /// </summary>
        public static bool schedulePrune = true;
    }
}
