using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Request;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class DefaultAuthRequestTest
    {
        private class FakeRequest : DefaultAuthRequest
        {
            public Exception toThrow;

            public FakeRequest(ClientConfig config, IAuthStateCache cache) : base(config, new GithubAuthSource(), cache)
            {
            }

            protected override AuthToken getAccessToken(AuthCallback authCallback)
            {
                if (toThrow != null)
                {
                    throw toThrow;
                }
                return new AuthToken { accessToken = "t" };
            }

            protected override AuthUser getUserInfo(AuthToken authToken)
            {
                return new AuthUser { uuid = "u", token = authToken };
            }

            public string scopes(string separator, bool encode, List<string> defaults)
            {
                return getScopes(separator, encode, defaults);
            }
        }

        private static ClientConfig Config()
        {
            return new ClientConfig { clientId = "id", clientSecret = "secret", redirectUri = "https://a.com/cb" };
        }

        [Fact]
        public void constructor_validates_config()
        {
            var ex = Assert.Throws<AuthException>(() => new FakeRequest(new ClientConfig { clientId = "id" }, new DefaultAuthStateCache()));
            Assert.Equal(5002, ex.errorCode);
            Assert.Equal("Parameter incomplete [GITHUB]", ex.errorMsg);
            Assert.Throws<AuthException>(() => new FakeRequest(new ClientConfig { clientId = "id", clientSecret = "s" }, new DefaultAuthStateCache()));
        }

        [Fact]
        public void login_success_consumes_state()
        {
            var cache = new DefaultAuthStateCache();
            var request = new FakeRequest(Config(), cache);
            request.authorize("st-1");
            var response = request.login(new AuthCallback { code = "c", state = "st-1" });
            Assert.Equal(2000, response.code);
            Assert.False(cache.containsKey("st-1"));
            Assert.Equal(5009, request.login(new AuthCallback { code = "c", state = "st-1" }).code);
        }

        [Fact]
        public void login_ignore_check_state()
        {
            var config = Config();
            config.ignoreCheckState = true;
            Assert.Equal(2000, new FakeRequest(config, new DefaultAuthStateCache()).login(new AuthCallback { code = "c" }).code);
        }

        [Fact]
        public void login_error_mapping()
        {
            var cache = new DefaultAuthStateCache();
            var request = new FakeRequest(Config(), cache);
            request.authorize("st-2");
            request.toThrow = new AuthException(5011, "bad token");
            var response = request.login(new AuthCallback { code = "c", state = "st-2" });
            Assert.Equal(5011, response.code);
            Assert.Equal("bad token", response.msg);

            request.authorize("st-3");
            request.toThrow = new InvalidOperationException("plain");
            response = request.login(new AuthCallback { code = "c", state = "st-3" });
            Assert.Equal(5000, response.code);
            Assert.Equal("plain", response.msg);
        }

        [Fact]
        public void getScopes_rules()
        {
            var config = Config();
            var request = new FakeRequest(config, new DefaultAuthStateCache());
            Assert.Equal("", request.scopes(" ", false, null));
            Assert.Equal("a b", request.scopes(null, false, new List<string> { "a", "b" }));
            Assert.Equal("a%20b", request.scopes(" ", true, new List<string> { "a", "b" }));
            config.scopes = new List<string> { "x", "y" };
            Assert.Equal("x,y", request.scopes(",", false, new List<string> { "a" }));
            config.scopes = null;
            config.scope = "legacy scope";
            Assert.Equal("legacy scope", request.scopes(",", false, new List<string> { "a" }));
        }

        [Fact]
        public void refresh_and_revoke_not_implemented_throw()
        {
            var request = new FakeRequest(Config(), new DefaultAuthStateCache());
            Assert.Equal(5001, Assert.Throws<AuthException>(() => request.refresh(new AuthToken())).errorCode);
            Assert.Equal(5001, Assert.Throws<AuthException>(() => request.revoke(new AuthToken())).errorCode);
        }

        [Fact]
        public void authorize_generates_uuid_state()
        {
            var url = new FakeRequest(Config(), new DefaultAuthStateCache()).authorize();
            Assert.Matches("state=[0-9a-f]{32}$", url);
        }

        [Fact]
        public void login_auth_exception_with_null_message_returns_null_msg()
        {
            var request = new FakeRequest(Config(), new DefaultAuthStateCache());
            request.authorize("st-null");
            request.toThrow = new AuthException(5000, null);
            var response = request.login(new AuthCallback { code = "c", state = "st-null" });
            Assert.Equal(5000, response.code);
            Assert.Null(response.msg);
        }
    }
}
