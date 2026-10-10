using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class AuthCheckerTest
    {
        [Fact]
        public void names_map_both_ways()
        {
            Assert.Equal("MI", AuthSourceNames.toJustAuthName("XIAOMI"));
            Assert.Equal("WECHAT_ENTERPRISE_WEB", AuthSourceNames.toJustAuthName("WECHAT_ENTERPRISE"));
            Assert.Equal("WECHAT_ENTERPRISE", AuthSourceNames.toJustAuthName("WECHAT_ENTERPRISE_SCAN"));
            Assert.Equal("GITHUB", AuthSourceNames.toJustAuthName("GITHUB"));
            Assert.Equal("WECHAT_ENTERPRISE_SCAN", AuthSourceNames.fromJustAuthName("WECHAT_ENTERPRISE"));
            Assert.Equal("XIAOMI", AuthSourceNames.fromJustAuthName("MI"));
        }

        [Fact]
        public void isSupportedAuth_rules()
        {
            Assert.True(AuthChecker.isSupportedAuth(new ClientConfig { clientId = "a", clientSecret = "b" }, new GithubAuthSource()));
            Assert.False(AuthChecker.isSupportedAuth(new ClientConfig { clientId = "a" }, new GithubAuthSource()));
            Assert.False(AuthChecker.isSupportedAuth(new ClientConfig { clientId = "a", clientSecret = "b" }, new StackOverflowAuthSource()));
            // C# 的 WECHAT_ENTERPRISE_SCAN 对应 JustAuth 的 WECHAT_ENTERPRISE，需要 agentId
            Assert.False(AuthChecker.isSupportedAuth(new ClientConfig { clientId = "a", clientSecret = "b" }, new WechatEnterpriseScanAuthSource()));
            Assert.False(AuthChecker.isSupportedAuth(new ClientConfig { clientId = "a", clientSecret = "b" }, new CodingAuthSource()));
        }

        [Fact]
        public void checkConfig_rules()
        {
            var ex = Assert.Throws<AuthException>(() => AuthChecker.checkConfig(new ClientConfig(), new GithubAuthSource()));
            Assert.Equal(5006, ex.errorCode);
            Assert.Throws<AuthException>(() => AuthChecker.checkConfig(new ClientConfig { redirectUri = "ftp://a" }, new GithubAuthSource()));
            AuthChecker.checkConfig(new ClientConfig { ignoreCheckRedirectUri = true }, new GithubAuthSource());
            AuthChecker.checkConfig(new ClientConfig { redirectUri = "http://localhost/cb" }, new GithubAuthSource());
        }

        [Fact]
        public void checkCode_rules()
        {
            var ex = Assert.Throws<AuthException>(() => AuthChecker.checkCode(new GithubAuthSource(), new AuthCallback()));
            Assert.Equal("Illegal code [GITHUB]", ex.errorMsg);
            AuthChecker.checkCode(new GithubAuthSource(), new AuthCallback { auth_code = "x" });
            AuthChecker.checkCode(new HuaweiAuthSource(), new AuthCallback { authorization_code = "x" });
        }

        [Fact]
        public void checkState_rules()
        {
            var cache = new DefaultAuthStateCache();
            cache.cache("s-ok", "s-ok");
            AuthChecker.checkState("s-ok", new GithubAuthSource(), cache);
            var ex = Assert.Throws<AuthException>(() => AuthChecker.checkState("nope", new GithubAuthSource(), cache));
            Assert.Equal(5009, ex.errorCode);
        }

        [Fact]
        public void github_source_refresh_revoke_unsupported_like_java()
        {
            Assert.Equal(5003, Assert.Throws<AuthException>(() => new GithubAuthSource().refresh()).errorCode);
            Assert.Equal(5003, Assert.Throws<AuthException>(() => new GithubAuthSource().revoke()).errorCode);
        }
    }
}
