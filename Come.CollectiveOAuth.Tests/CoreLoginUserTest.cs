using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Request;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class CoreLoginUserTest
    {
        private class FakeRequest : DefaultAuthRequest
        {
            private readonly AuthUser user;

            public FakeRequest(AuthUser user) : base(new ClientConfig
            {
                clientId = "id",
                clientSecret = "secret",
                redirectUri = "https://example.com/callback",
                ignoreCheckState = true
            }, new GithubAuthSource(), new DefaultAuthStateCache())
            {
                this.user = user;
            }

            protected override AuthToken getAccessToken(AuthCallback authCallback) => new AuthToken { accessToken = "at" };

            protected override AuthUser getUserInfo(AuthToken authToken) => user;
        }

        [Fact]
        public void null_user_fails()
        {
            var result = new FakeRequest(null).login(new AuthCallback { code = "c" });
            Assert.Equal(5000, result.code);
            Assert.Contains("user id", result.msg);
        }

        [Fact]
        public void empty_uuid_fails()
        {
            var result = new FakeRequest(new AuthUser { uuid = "" }).login(new AuthCallback { code = "c" });
            Assert.Equal(5000, result.code);
        }

        [Fact]
        public void uuid_present_succeeds()
        {
            var result = new FakeRequest(new AuthUser { uuid = "u1" }).login(new AuthCallback { code = "c" });
            Assert.Equal(2000, result.code);
        }
    }
}
