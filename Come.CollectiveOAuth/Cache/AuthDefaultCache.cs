using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Come.CollectiveOAuth.Cache
{
    /// <summary>
    /// 对应 JustAuth AuthDefaultCache：进程内缓存，所有实例共享存储，按毫秒绝对过期
    /// </summary>
    public class AuthDefaultCache : IAuthCache
    {
        private static readonly ConcurrentDictionary<string, CacheState> stateCache = new ConcurrentDictionary<string, CacheState>();

        private Timer pruneTimer;

        public AuthDefaultCache()
        {
            if (AuthCacheConfig.schedulePrune)
            {
                this.schedulePrune(AuthCacheConfig.timeout);
            }
        }

        public void set(string key, string value)
        {
            set(key, value, AuthCacheConfig.timeout);
        }

        public void set(string key, string value, long timeout)
        {
            stateCache[key] = new CacheState(value, timeout);
        }

        public string get(string key)
        {
            return stateCache.TryGetValue(key, out var cacheState) && !cacheState.isExpired() ? cacheState.state : null;
        }

        public bool containsKey(string key)
        {
            return stateCache.TryGetValue(key, out var cacheState) && !cacheState.isExpired();
        }

        public void remove(string key)
        {
            stateCache.TryRemove(key, out _);
        }

        public void pruneCache()
        {
            foreach (var item in stateCache)
            {
                if (item.Value.isExpired())
                {
                    stateCache.TryRemove(item.Key, out _);
                }
            }
        }

        public void schedulePrune(long delay)
        {
            pruneTimer = new Timer(_ => pruneCache(), null, delay, delay);
        }

        private class CacheState
        {
            public string state { get; }

            private readonly long expire;

            public CacheState(string state, long expire)
            {
                this.state = state;
                // 实际过期时间等于当前时间加上有效期
                this.expire = currentTimeMillis() + expire;
            }

            public bool isExpired()
            {
                return currentTimeMillis() > this.expire;
            }

            private static long currentTimeMillis()
            {
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }
    }
}
