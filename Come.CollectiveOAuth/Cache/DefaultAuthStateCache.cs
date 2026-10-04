using System;

namespace Come.CollectiveOAuth.Cache
{
    public class DefaultAuthStateCache : IAuthStateCache
    {
        /// <summary>
        /// 默认缓存前缀
        /// </summary>
        private static string Default_Cache_Prefix = "CollectiveOAuth_Status_";

        public void cache(string key, string value)
        {
            HttpRuntimeCache.Set($"{Default_Cache_Prefix}{key}", value);
        }

        public void cache(string key, string value, long timeout)
        {
            // 接口约定 timeout 为毫秒，HttpRuntimeCache 以秒为单位
            HttpRuntimeCache.Set($"{Default_Cache_Prefix}{key}", value, timeout / 1000);
        }

        public void remove(string key)
        {
            HttpRuntimeCache.Remove($"{Default_Cache_Prefix}{key}");
        }

        public bool containsKey(string key)
        {
            var cacheObj = HttpRuntimeCache.Get($"{Default_Cache_Prefix}{key}");
            if (cacheObj != null)
            {
                return true;
            }

            return false;
        }

        public string get(string key)
        {
            var cacheObj = HttpRuntimeCache.Get($"{Default_Cache_Prefix}{key}");
            if (cacheObj != null)
            {
                return Convert.ToString(cacheObj);
            }
            else
            {
                return null;
            }
        }
    }
}