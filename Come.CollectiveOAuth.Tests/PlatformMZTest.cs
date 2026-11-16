using System;
using System.Net.Http;
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
    /// M–Z 平台边界场景（非 golden，C# 侧加固行为）
    /// </summary>
    [Collection("HttpSender")]
    public class PlatformMZTest
    {
        private static ClientConfig Config() => new ClientConfig
        {
            clientId = "cid",
            clientSecret = "csecret",
            redirectUri = "https://example.com/callback",
            stackOverflowKey = "sokey",
            ignoreCheckState = true
        };

        private static AuthCallback Callback() => new AuthCallback { code = "code123", state = "s" };

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

        private static T Replay<T>(Func<T> action, params string[] responses) => WithSender(new RecordingHttpSender(responses), action);

        private class ThrowingSender : IHttpSender
        {
            public string Send(HttpRequestRecord request, HttpConfig httpConfig) => throw new HttpRequestException("network down");
        }

        [Fact]
        public void Renren_without_response_field_login_fails()
        {
            var result = Replay(() => new RenrenAuthRequest(Config()).login(Callback()),
                "{\"token_type\":\"bearer\",\"expires_in\":3600,\"access_token\":\"at\",\"refresh_token\":\"rt\",\"user\":{\"id\":1}}",
                "{\"error\":{\"code\":\"invalid_token\",\"message\":\"bad\"}}");
            Assert.False(result.ok());
            Assert.Contains("Renren", result.msg);
        }

        [Fact]
        public void StackOverflow_empty_items_login_fails_with_clear_message()
        {
            var result = Replay(() => new StackOverflowAuthRequest(Config()).login(Callback()),
                "{\"access_token\":\"at\",\"expires\":86400}",
                "{\"items\":[],\"has_more\":false}");
            Assert.False(result.ok());
            Assert.Contains("items is empty", result.msg);
        }

        [Fact]
        public void StackOverflow_error_id_login_fails_with_error_message()
        {
            var result = Replay(() => new StackOverflowAuthRequest(Config()).login(Callback()),
                "{\"access_token\":\"at\",\"expires\":86400}",
                "{\"error_id\":403,\"error_message\":\"access_token not valid\",\"error_name\":\"access_denied\"}");
            Assert.False(result.ok());
            Assert.Equal(403, result.code);
            Assert.Equal("access_token not valid", result.msg);
        }

        [Fact]
        public void QQ_jsonp_token_error_uses_error_description()
        {
            var result = Replay(() => new QQAuthRequest(Config()).login(Callback()),
                "callback( {\"error\":100019,\"error_description\":\"code to access token error\"} );\n");
            Assert.False(result.ok());
            Assert.Equal("code to access token error", result.msg);
        }

        [Fact]
        public void QQ_empty_expires_in_does_not_crash()
        {
            var result = Replay(() => new QQAuthRequest(Config()).refresh(new AuthToken { refreshToken = "rt" }),
                "access_token=at" + "&expires_in=" + "&refresh_token=rt2");
            Assert.True(result.ok());
            var token = Assert.IsType<AuthToken>(result.data);
            Assert.Equal(0, token.expireIn);
            Assert.Equal("rt2", token.refreshToken);
        }

        [Fact]
        public void Meituan_error_msg_is_used()
        {
            var result = Replay(() => new MeituanAuthRequest(Config()).login(Callback()),
                "{\"error_code\":1001,\"error_msg\":\"invalid code\"}");
            Assert.False(result.ok());
            Assert.Equal("invalid code", result.msg);
        }

        [Fact]
        public void QQ_refresh_network_error_wrapped_as_AuthException()
        {
            var ex = Assert.Throws<AuthException>(() =>
                WithSender<AuthResponse>(new ThrowingSender(), () => new QQAuthRequest(Config()).refresh(new AuthToken { refreshToken = "rt" })));
            Assert.IsType<HttpRequestException>(ex.InnerException);
            Assert.Contains("network down", ex.Message);
        }

        [Fact]
        public void QQ_refresh_null_token_throws_AuthException()
        {
            Assert.Throws<AuthException>(() => new QQAuthRequest(Config()).refresh(null));
        }
    }
}
