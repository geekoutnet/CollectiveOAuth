using System;
using System.Collections.Generic;
using System.Net.Http;
using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Request;
using Come.CollectiveOAuth.Tests.Support;
using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    /// <summary>
    /// A–L 平台的健壮性补充测试（golden 未覆盖的异常场景）
    /// </summary>
    [Collection("HttpSender")]
    public class PlatformALTest
    {
        private class ThrowingSender : IHttpSender
        {
            public string Send(HttpRequestRecord request, HttpConfig httpConfig)
            {
                throw new HttpRequestException("connection refused");
            }
        }

        private static ClientConfig Config(string redirectUri = "https://example.com/callback")
        {
            return new ClientConfig { clientId = "cid", clientSecret = "csecret", redirectUri = redirectUri, ignoreCheckState = true };
        }

        private static AuthCallback Callback()
        {
            return new AuthCallback { code = "code", state = "state" };
        }

        private static T WithSender<T>(IHttpSender sender, Func<T> action)
        {
            var original = HttpUtils.Sender;
            HttpUtils.Sender = sender;
            try
            {
                return action();
            }
            finally
            {
                HttpUtils.Sender = original;
            }
        }

        [Fact]
        public void Douyin_login_fails_when_data_missing()
        {
            var request = new DouyinAuthRequest(Config(), new DefaultAuthStateCache());
            var sender = new RecordingHttpSender(new[] { "{\"message\":\"error\"}" });
            var response = WithSender(sender, () => request.login(Callback()));
            Assert.False(response.ok());
            Assert.Contains("data", response.msg);
        }

        [Fact]
        public void Douyin_login_fails_on_err_no_format()
        {
            var request = new DouyinAuthRequest(Config(), new DefaultAuthStateCache());
            var sender = new RecordingHttpSender(new[] { "{\"err_no\":28001003,\"err_tips\":\"access_token无效\"}" });
            var response = WithSender(sender, () => request.login(Callback()));
            Assert.False(response.ok());
            Assert.Equal(28001003, response.code);
            Assert.Equal("access_token无效", response.msg);
        }

        [Fact]
        public void Coding_login_fails_when_data_missing()
        {
            var config = Config();
            config.domainPrefix = "team";
            var request = new CodingAuthRequest(config, new DefaultAuthStateCache());
            var sender = new RecordingHttpSender(new[]
            {
                "{\"access_token\":\"at\",\"refresh_token\":\"rt\",\"expires_in\":\"3600\"}",
                "{\"code\":0}"
            });
            var response = WithSender(sender, () => request.login(Callback()));
            Assert.False(response.ok());
            Assert.Contains("data", response.msg);
        }

        [Fact]
        public void Microsoft_defaults_authorization_to_bearer_when_token_type_missing()
        {
            var request = new MicrosoftAuthRequest(Config(), new DefaultAuthStateCache());
            var sender = new RecordingHttpSender(new[]
            {
                "{\"access_token\":\"msat\",\"expires_in\":3599}",
                "{\"id\":\"uid\",\"displayName\":\"Adele\"}"
            });
            var response = WithSender(sender, () => request.login(Callback()));
            Assert.True(response.ok(), response.msg);
            Assert.Equal("Bearer msat", sender.requests[1].headers["Authorization"]);
        }

        [Fact]
        public void Facebook_string_error_is_reported()
        {
            var request = new FackbookAuthRequest(Config(), new DefaultAuthStateCache());
            var sender = new RecordingHttpSender(new[] { "{\"error\":\"invalid_request\",\"error_description\":\"bad code\"}" });
            var response = WithSender(sender, () => request.login(Callback()));
            Assert.False(response.ok());
            Assert.Contains("invalid_request", response.msg);
            Assert.Contains("bad code", response.msg);
        }

        [Fact]
        public void Refresh_wraps_network_error_into_auth_exception()
        {
            var request = new HuaweiAuthRequest(Config(), new DefaultAuthStateCache());
            var ex = Assert.Throws<AuthException>(() => WithSender(new ThrowingSender(), () => request.refresh(new AuthToken { refreshToken = "rt" })));
            Assert.IsType<HttpRequestException>(ex.InnerException);
            Assert.Contains("connection refused", ex.Message);
        }

        [Fact]
        public void Refresh_with_null_token_throws_auth_exception()
        {
            var request = new KujialeAuthRequest(Config(), new DefaultAuthStateCache());
            Assert.Throws<AuthException>(() => request.refresh(null));
        }
    }
}
