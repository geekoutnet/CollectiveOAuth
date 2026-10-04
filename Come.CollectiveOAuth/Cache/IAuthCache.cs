namespace Come.CollectiveOAuth.Cache
{
    /// <summary>
    /// 对应 JustAuth AuthCache
    /// </summary>
    public interface IAuthCache
    {
        void set(string key, string value);

        void set(string key, string value, long timeout);

        string get(string key);

        bool containsKey(string key);

        void remove(string key);

        void pruneCache();
    }
}
