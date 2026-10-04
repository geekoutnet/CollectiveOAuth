using System;
using System.Linq;
using Come.CollectiveOAuth.Enums.Scope;
using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class ScopeTest
    {
        [Fact]
        public void default_scopes_match_java_for_all_platforms()
        {
            // 期望值由 tools/gen_scopes.py 直接从 JustAuth 源码中 isDefault=true 的项生成
            Assert.Equal(30, GeneratedExpectations.DefaultScopes.Count);
            foreach (var expected in GeneratedExpectations.DefaultScopes)
            {
                var type = typeof(IAuthScope).Assembly.GetType("Come.CollectiveOAuth.Enums.Scope." + expected.Key);
                Assert.NotNull(type);
                var values = (IAuthScope[])type.GetMethod("values").Invoke(null, null);
                var defaults = AuthScopeUtils.getDefaultScopes(values);
                Assert.True(expected.Value.SequenceEqual(defaults), $"{expected.Key}: 期望 [{string.Join(",", expected.Value)}]，实际 [{string.Join(",", defaults)}]");
            }
        }

        [Fact]
        public void okta_defaults_are_openid_family()
        {
            Assert.Equal(new[] { "openid", "profile", "email", "address", "phone", "offline_access" },
                AuthScopeUtils.getDefaultScopes(AuthOktaScope.values()));
        }

        [Fact]
        public void getScopes_and_empty()
        {
            Assert.Equal(new[] { "user", "gist" }, AuthScopeUtils.getScopes(AuthGithubScope.USER, AuthGithubScope.GIST));
            Assert.Null(AuthScopeUtils.getScopes());
            Assert.Null(AuthScopeUtils.getDefaultScopes(Array.Empty<IAuthScope>()));
        }
    }
}
