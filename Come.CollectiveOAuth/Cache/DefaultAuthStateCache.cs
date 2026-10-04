namespace Come.CollectiveOAuth.Cache
{
    /// <summary>
    /// 默认的 state 缓存，对应 JustAuth AuthDefaultStateCache（默认 3 分钟过期）
    /// </summary>
    public class DefaultAuthStateCache : IAuthStateCache
    {
        private static readonly IAuthCache authCache = new AuthDefaultCache();

        public void cache(string key, string value)
        {
            authCache.set(key, value);
        }

        public void cache(string key, string value, long timeout)
        {
            authCache.set(key, value, timeout);
        }

        public string get(string key)
        {
            return authCache.get(key);
        }

        public bool containsKey(string key)
        {
            return authCache.containsKey(key);
        }

        public void remove(string key)
        {
            authCache.remove(key);
        }
    }
}
