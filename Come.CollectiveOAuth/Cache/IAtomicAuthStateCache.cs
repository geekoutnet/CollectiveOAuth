namespace Come.CollectiveOAuth.Cache
{
    /// <summary>
    /// 支持原子“校验并删除”的 state 缓存。
    /// 实现该接口后，login 以 <see cref="tryRemove"/> 的返回值作为 state 校验结果，
    /// 保证同一 state 在并发重放时只有一次校验通过；未实现时退回 containsKey + remove。
    /// </summary>
    public interface IAtomicAuthStateCache : IAuthStateCache
    {
        /// <summary>
        /// 原子地删除缓存：仅当 key 存在且未过期、并且由本次调用删除成功时返回 true
        /// </summary>
        /// <param name="key">缓存key</param>
        /// <returns>true：本次调用删除了一个有效的 key；false：key 不存在、已过期或已被其他调用删除</returns>
        bool tryRemove(string key);
    }
}
