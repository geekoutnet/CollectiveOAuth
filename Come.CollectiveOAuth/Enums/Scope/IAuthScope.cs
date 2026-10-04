namespace Come.CollectiveOAuth.Enums.Scope
{
    /// <summary>
    /// 对应 JustAuth AuthScope
    /// </summary>
    public interface IAuthScope
    {
        string scope { get; }

        bool isDefault { get; }
    }
}
