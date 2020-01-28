using System;
using System.Web;

namespace Come.CollectiveOAuth.Cache
{
    public class HttpRuntimeCache
    {
        /// <summary>
        /// 获取数据缓存
        /// </summary>
        /// <param name="CacheKey">键</param>
        public static object Get(string CacheKey)
        {
            System.Web.Caching.Cache objCache = HttpRuntime.Cache;
            return objCache[CacheKey];
        }

        /// <summary>
        /// 设置数据缓存
        /// 变化时间过期（平滑过期）。表示缓存连续2个小时没有访问就过期（TimeSpan.FromSeconds(7200)）。
        /// </summary>
        /// <param name="CacheKey">键</param>
        /// <param name="objObject">值</param>
        /// <param name="Second">过期时间，默认7200秒 </param>
        /// <param name="Sliding">是否相对过期，默认是；否，则固定时间过期</param>
        public static void Set(string CacheKey, object objObject, long Second = 7200, bool Sliding = true)
        {
            System.Web.Caching.Cache objCache = HttpRuntime.Cache;
            if (Sliding)
            {
                objCache.Insert(CacheKey, objObject, null, DateTime.MaxValue, TimeSpan.FromSeconds(Second));
            }
            else
            {
                objCache.Insert(CacheKey, objObject, null, DateTime.Now.AddSeconds(Second), TimeSpan.Zero);
            }
        }

        /// <summary>
        /// 移除指定数据缓存
        /// </summary>
        /// <param name="CacheKey">键</param>
        public static void Remove(string CacheKey)
        {
            System.Web.Caching.Cache objCache = HttpRuntime.Cache;
            objCache.Remove(CacheKey);
        }

        /// <summary>
        /// 移除全部缓存
        /// </summary>
        public static void RemoveAll()
        {
            var objCache = HttpRuntime.Cache.GetEnumerator();
            while (objCache.MoveNext())
            {
                HttpRuntime.Cache.Remove(objCache.Key.ToString());
            }
        }
    }
}