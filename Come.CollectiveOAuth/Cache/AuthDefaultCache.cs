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

        /// <summary>
        /// 存储是进程级共享的，定时清理器也只需一个：静态单例，避免每 new 一次泄漏一个 Timer
        /// </summary>
        private static Timer pruneTimer;

        private static long pruneDelay;

        private static readonly object pruneTimerLock = new object();

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

        /// <summary>
        /// 原子删除：仅当本次调用删除了一个未过期的 key 时返回 true
        /// </summary>
        public bool tryRemove(string key)
        {
            return stateCache.TryRemove(key, out var cacheState) && !cacheState.isExpired();
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

        /// <summary>
        /// 启动（或调整周期）进程内唯一的定时清理器；delay 非正数时忽略
        /// </summary>
        public void schedulePrune(long delay)
        {
            if (delay <= 0)
            {
                return;
            }
            // Timer 允许的最大周期为 0xFFFFFFFE 毫秒
            delay = Math.Min(delay, 0xFFFFFFFEL);
            lock (pruneTimerLock)
            {
                if (pruneTimer == null)
                {
                    pruneTimer = new Timer(_ => pruneCache(), null, delay, delay);
                }
                else if (pruneDelay != delay)
                {
                    // 只在周期变化时调整，避免频繁 new 实例不断推迟清理
                    pruneTimer.Change(delay, delay);
                }
                pruneDelay = delay;
            }
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
