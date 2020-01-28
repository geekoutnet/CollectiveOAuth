namespace Come.CollectiveOAuth.Cache
{
    public interface IAuthStateCache
    {
        /**
         * 存入缓存
         *
         * @param key   缓存key
         * @param value 缓存内容
         */
        void cache(string key, string value);

        /**
         * 存入缓存
         *
         * @param key     缓存key
         * @param value   缓存内容
         * @param timeout 指定缓存过期时间（毫秒）
         */
        void cache(string key, string value, long timeout);

        /**
         * 获取缓存内容
         *
         * @param key 缓存key
         * @return 缓存内容
         */
        string get(string key);

        /**
         * 是否存在key，如果对应key的value值已过期，也返回false
         *
         * @param key 缓存key
         * @return true：存在key，并且value没过期；false：key不存在或者已过期
         */
        bool containsKey(string key);
    }
}