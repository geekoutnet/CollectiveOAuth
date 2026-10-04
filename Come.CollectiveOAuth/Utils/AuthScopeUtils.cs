using System.Collections.Generic;
using System.Linq;
using Come.CollectiveOAuth.Enums.Scope;

namespace Come.CollectiveOAuth.Utils
{
    /// <summary>
    /// 对应 JustAuth AuthScopeUtils
    /// </summary>
    public static class AuthScopeUtils
    {
        public static List<string> getDefaultScopes(IAuthScope[] scopes)
        {
            if (scopes == null || scopes.Length == 0)
            {
                return null;
            }
            return scopes.Where(s => s.isDefault).Select(s => s.scope).ToList();
        }

        public static List<string> getScopes(params IAuthScope[] scopes)
        {
            if (scopes == null || scopes.Length == 0)
            {
                return null;
            }
            return scopes.Select(s => s.scope).ToList();
        }
    }
}
