using System;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Enums;
using Come.CollectiveOAuth.Exceptions;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class AuthExceptionTest
    {
        [Fact]
        public void status_with_source_appends_name()
        {
            var e = new AuthException(AuthResponseStatus.ILLEGAL_STATUS, new GithubAuthSource());
            Assert.Equal(5009, e.errorCode);
            Assert.Equal("Illegal state [GITHUB]", e.errorMsg);
            Assert.Equal("Illegal state [GITHUB]", e.Message);
        }

        [Fact]
        public void message_only_is_failure()
        {
            var e = new AuthException("boom");
            Assert.Equal(5000, e.errorCode);
            Assert.Equal("boom", e.errorMsg);
        }

        [Fact]
        public void cause_constructor_has_no_code()
        {
            var e = new AuthException(new InvalidOperationException("x"));
            Assert.Equal(0, e.errorCode);
            Assert.Null(e.errorMsg);
            Assert.Equal("System.InvalidOperationException: x", e.Message);
        }

        [Fact]
        public void new_status_codes()
        {
            Assert.Equal(5016, (int)AuthResponseStatus.ILLEGAL_WECHAT_AGENT_ID);
            Assert.Equal("Invalid token", AuthResponseStatus.ILLEGAL_TOKEN.GetDesc());
        }

        [Fact]
        public void null_message_stays_null_like_java()
        {
            // Java: new AuthException(code, null).getMessage() 为 null
            var e = new AuthException(5000, null);
            Assert.Null(e.Message);
            Assert.Null(e.errorMsg);
        }
    }
}
