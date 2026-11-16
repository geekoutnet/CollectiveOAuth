#if !NETFRAMEWORK
using System;
using Microsoft.Extensions.Caching.Distributed;

namespace Come.CollectiveOAuth.Cache
{
    /// <summary>
    /// 基于 IDistributedCache 的 state 缓存，多实例部署时配合 Redis 等分布式缓存使用
    /// </summary>
    public class DistributedAuthStateCache : IAtomicAuthStateCache
    {
        /// <summary>
        /// 默认缓存前缀
        /// </summary>
        private static string Default_Cache_Prefix = "CollectiveOAuth_Status_";

        /// <summary>
        /// 默认过期时间（毫秒），与 JustAuth 保持一致为 3 分钟
        /// </summary>
        private static long Default_Timeout = 3 * 60 * 1000;

        private readonly IDistributedCache distributedCache;

        public DistributedAuthStateCache(IDistributedCache distributedCache)
        {
            this.distributedCache = distributedCache;
        }

        public void cache(string key, string value)
        {
            cache(key, value, Default_Timeout);
        }

        public void cache(string key, string value, long timeout)
        {
            distributedCache.SetString($"{Default_Cache_Prefix}{key}", value, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(timeout)
            });
        }

        public bool containsKey(string key)
        {
            return get(key) != null;
        }

        public string get(string key)
        {
            return distributedCache.GetString($"{Default_Cache_Prefix}{key}");
        }

        public void remove(string key)
        {
            distributedCache.Remove($"{Default_Cache_Prefix}{key}");
        }

        private static readonly object tryRemoveLock = new object();

        /// <summary>
        /// 尽力而为的“校验并删除”：IDistributedCache 没有原子删除操作，这里用进程内锁包住 get + remove，
        /// 只能保证单进程内同一 state 仅被消费一次。多实例部署若需要跨进程的严格一次性，
        /// 请自行实现 <see cref="IAtomicAuthStateCache"/>，使用 Redis GETDEL（或 Lua 脚本）等原子操作。
        /// </summary>
        public virtual bool tryRemove(string key)
        {
            lock (tryRemoveLock)
            {
                if (get(key) == null)
                {
                    return false;
                }
                remove(key);
                return true;
            }
        }
    }
}
#endif
