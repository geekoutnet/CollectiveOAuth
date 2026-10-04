using System.Collections.Generic;
using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests.Utils
{
    public class GlobalAuthUtilTest
    {
        private const string TwitterUserInfo = "https://api.twitter.com/1.1/account/verify_credentials.json";
        private const string TwitterAccessToken = "https://api.twitter.com/oauth/access_token";

        [Fact]
        public void generateDingTalkSignature()
        {
            Assert.Equal("mLTZEMqIlpAA3xtJ43KcRT0EDLwgSamFe%2FNis5lq9ik%3D", GlobalAuthUtil.generateDingTalkSignature("SHA-256", "1562325753000 "));
        }

        [Fact]
        public void urlDecode()
        {
            Assert.Equal("", GlobalAuthUtil.urlDecode(null));
            Assert.Equal("https://www.foo.bar", GlobalAuthUtil.urlDecode("https://www.foo.bar"));
            Assert.Equal("mLTZEMqIlpAA3xtJ43KcRT0EDLwgSamFe/Nis5lq9ik=", GlobalAuthUtil.urlDecode("mLTZEMqIlpAA3xtJ43KcRT0EDLwgSamFe%2FNis5lq9ik%3D"));
        }

        [Fact]
        public void parseStringToMap()
        {
            var map = GlobalAuthUtil.parseStringToMap("foo&bar=baz");
            Assert.Single(map);
            Assert.Equal("baz", map["bar"]);
            Assert.Empty(GlobalAuthUtil.parseStringToMap("bar=baz"));
            Assert.Null(GlobalAuthUtil.parseStringToMap("a=&b=1")["a"]);
        }

        [Fact]
        public void protocols()
        {
            Assert.False(GlobalAuthUtil.isHttpProtocol(""));
            Assert.False(GlobalAuthUtil.isHttpProtocol("foo"));
            Assert.True(GlobalAuthUtil.isHttpProtocol("http://www.foo.bar"));
            Assert.True(GlobalAuthUtil.isHttpProtocol("http%3A%2F%2Fwww.foo.bar"));
            Assert.False(GlobalAuthUtil.isHttpsProtocol(""));
            Assert.True(GlobalAuthUtil.isHttpsProtocol("https://www.foo.bar"));
            Assert.False(GlobalAuthUtil.isLocalHost("foo"));
            Assert.True(GlobalAuthUtil.isLocalHost(""));
            Assert.True(GlobalAuthUtil.isLocalHost("127.0.0.1"));
            Assert.True(GlobalAuthUtil.isLocalHost("localhost"));
        }

        [Fact]
        public void twitterSignatureForRequestToken()
        {
            var p = new Dictionary<string, string>
            {
                { "oauth_consumer_key", "HD0XLqzi5Wz0G08rh45Cg8mgh" },
                { "oauth_nonce", "sTj7Ivg73u052eXstpoS1AWQCynuDEPN" },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_timestamp", "1569750981" },
                { "oauth_callback", "https://codinglife.tech" },
                { "oauth_version", "1.0" }
            };
            // Java 用例断言整段 Authorization 头，其参数顺序取决于 Java HashMap，这里只比对签名本身
            Assert.Equal("+L5Jq/TaKubge04cWw+4yfjFlaU=", GlobalAuthUtil.generateTwitterSignature(p, "POST", "https://api.twitter.com/oauth/request_token", "0YX3RH2DnPiT77pgzLzFdfpMKX8ENLIWQKYQ7lG5TERuZNgXN5", null));
        }

        [Fact]
        public void twitterSignatureForAccessToken()
        {
            var p = new Dictionary<string, string>
            {
                { "oauth_consumer_key", "HD0XLqzi5Wz0G08rh45Cg8mgh" },
                { "oauth_nonce", "sTj7Ivg73u052eXstpoS1AWQCynuDEPN" },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_timestamp", "1569751082" },
                { "oauth_token", "W_KLmAAAAAAAxq5LAAABbXxJeD0" },
                { "oauth_verifier", "lYou4gxfA6S5KioUa8VF8HCShzA2nSxp" },
                { "oauth_version", "1.0" }
            };
            Assert.Equal("9i0lmWgvphtkl2KcCO9VyZ3K2/0=", GlobalAuthUtil.generateTwitterSignature(p, "POST", TwitterAccessToken, "0YX3RH2DnPiT77pgzLzFdfpMKX8ENLIWQKYQ7lG5TERuZNgXN5", "W_KLmAAAAAAAxq5LAAABbXxJeD0"));
        }

        [Fact]
        public void twitterSignatureForUserInfo()
        {
            var p = new Dictionary<string, string>
            {
                { "user_id", "1961977975" },
                { "screen_name", "pengisgood" },
                { "include_entities", "true" }
            };
            Assert.Equal("OsqHjRmBf7syxlz8lB7MRdzqEjY=", GlobalAuthUtil.generateTwitterSignature(p, "GET", TwitterUserInfo, "0YX3RH2DnPiT77pgzLzFdfpMKX8ENLIWQKYQ7lG5TERuZNgXN5", "ffyKe39GYYf8tAyhliSe3QmazpO65kZp5b49xOFX6wHho"));
        }

        [Fact]
        public void md5()
        {
            Assert.Equal("b0d923de4289b69976448cac718528b8", GlobalAuthUtil.md5("helloworld,iamjustauth"));
        }

        [Fact]
        public void urlEncode()
        {
            Assert.Equal("", GlobalAuthUtil.urlEncode(null));
            Assert.Equal("https%3A%2F%2Fwww.foo.bar", GlobalAuthUtil.urlEncode("https://www.foo.bar"));
            Assert.Equal("mLTZEMqIlpAA3xtJ43KcRT0EDLwgSamFe%252FNis5lq9ik%253D", GlobalAuthUtil.urlEncode("mLTZEMqIlpAA3xtJ43KcRT0EDLwgSamFe%2FNis5lq9ik%3D"));
            Assert.Equal("a%20b%2A%7E%21", GlobalAuthUtil.urlEncode("a b*~!"));
        }

        [Fact]
        public void parseMapToString()
        {
            var p = new Dictionary<string, string>
            {
                { "user_id", "1" },
                { "screen_name", "史上最全的第三方授权登录库" },
                { "include_entities", "true" }
            };
            Assert.Equal("user_id=1&screen_name=史上最全的第三方授权登录库&include_entities=true", GlobalAuthUtil.parseMapToString(p, false));
            Assert.Equal("user_id=1&screen_name=%E5%8F%B2%E4%B8%8A%E6%9C%80%E5%85%A8%E7%9A%84%E7%AC%AC%E4%B8%89%E6%96%B9%E6%8E%88%E6%9D%83%E7%99%BB%E5%BD%95%E5%BA%93&include_entities=true", GlobalAuthUtil.parseMapToString(p, true));
            Assert.Equal("", GlobalAuthUtil.parseMapToString((IDictionary<string, string>)null, true));
        }

        [Fact]
        public void nonce_and_timestamp()
        {
            Assert.Equal(10, GlobalAuthUtil.generateNonce(10).Length);
            Assert.Matches("^[0-9]{10}$", GlobalAuthUtil.getTimestamp());
        }

        [Fact]
        public void generateTwitterSignature()
        {
            var p = new Dictionary<string, string> { { "name", "你好" }, { "gender", "male" } };
            Assert.Equal("20FYnV2aZnxNQtp+I0tpMRTvcx0=", GlobalAuthUtil.generateTwitterSignature(p, "GET", TwitterUserInfo, "xxxxx", "xxxxx"));
        }

        [Fact]
        public void generateElemeSignature()
        {
            var p = new Dictionary<string, object> { { "name", "你好" }, { "gender", "male" } };
            Assert.Equal("26FEB8BF7E84FED2619D9C5D97F421BD", GlobalAuthUtil.generateElemeSignature("appKey", "appKey", 1233456789, "appKey", "appKey", p));
        }

        [Fact]
        public void generateJdSignature()
        {
            var p = new Dictionary<string, object> { { "name", "你好" }, { "gender", "male" } };
            Assert.Equal("FE04EC03BA8A619802CF309959C2B43F", GlobalAuthUtil.generateJdSignature("appKey", p));
        }
    }
}
