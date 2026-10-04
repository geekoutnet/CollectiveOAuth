using System.Collections.Generic;
using Come.CollectiveOAuth.Tests.Support;
using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    [Collection("HttpSender")]
    public class HttpUtilsTest
    {
        [Fact]
        public void records_requests_through_sender()
        {
            var sender = new RecordingHttpSender(new[] { "r1", "r2", "r3" });
            var original = HttpUtils.Sender;
            HttpUtils.Sender = sender;
            try
            {
                Assert.Equal("r1", HttpUtils.RequestGet("https://a.com/x?y=1", new Dictionary<string, object> { { "Authorization", "token t" } }));
                Assert.Equal("r2", HttpUtils.RequestPost("https://a.com/p", "{\"a\":1}"));
                Assert.Equal("r3", HttpUtils.RequestFormPost("https://a.com/f", "a=1&b=2"));
            }
            finally
            {
                HttpUtils.Sender = original;
            }

            Assert.Equal("GET", sender.requests[0].method);
            Assert.Equal("token t", sender.requests[0].headers["Authorization"]);
            Assert.Equal("POST", sender.requests[1].method);
            Assert.Equal("application/json", sender.requests[1].contentType);
            Assert.Equal("{\"a\":1}", sender.requests[1].body);
            Assert.Equal("application/x-www-form-urlencoded", sender.requests[2].contentType);
        }
    }
}
