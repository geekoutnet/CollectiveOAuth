using Come.CollectiveOAuth.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class ModelsTest
    {
        [Fact]
        public void callback_code_falls_back_to_auth_code()
        {
            Assert.Equal("ac", new AuthCallback { auth_code = "ac" }.code);
            Assert.Equal("c", new AuthCallback { code = "c", auth_code = "ac" }.code);
            var twitter = new AuthCallback { oauth_token = "t", oauth_verifier = "v" };
            Assert.Equal("t", twitter.oauthToken);
            Assert.Equal("v", twitter.oauthVerifier);
        }

        [Fact]
        public void new_fields_exist()
        {
            var token = new AuthToken { refreshTokenExpireIn = 1, snapshotUser = true, username = "u", corpId = "c", oauthCallbackConfirmed = null };
            var user = new AuthUser { rawUserInfo = new JObject(), snapshotUser = true, token = token };
            Assert.True(user.snapshotUser);
            Assert.Null(user.token.oauthCallbackConfirmed);
            Assert.True(new AuthResponse(2000, null).ok());
        }
    }
}
