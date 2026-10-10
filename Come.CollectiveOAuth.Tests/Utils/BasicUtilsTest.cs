using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests.Utils
{
    public class BasicUtilsTest
    {
        [Fact]
        public void Uuid_is_32_lowercase_hex()
        {
            Assert.Matches("^[0-9a-f]{32}$", UuidUtils.getUUID());
            Assert.Matches("^[0-9a-f]{32}$", AuthStateUtils.createState());
        }

        [Fact]
        public void RandomString_uses_base_chars()
        {
            Assert.Matches("^[a-z0-9]{50}$", RandomUtil.randomString(50));
            Assert.Equal(1, RandomUtil.randomString(0).Length);
        }

        [Fact]
        public void Base64_standard_and_urlsafe()
        {
            Assert.Equal("5L2g5aW9", Base64Utils.encode("你好"));
            // 0xfb 0xff → 标准 "+/8="，URL 安全且无填充 "-_8"
            Assert.Equal("+/8=", Base64Utils.encode(new byte[] { 0xfb, 0xff }));
            Assert.Equal("-_8", Base64Utils.encodeUrlSafe(new byte[] { 0xfb, 0xff }));
        }

        [Fact]
        public void Pkce_challenge_matches_rfc7636_example()
        {
            // RFC 7636 附录 B 的示例
            Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
                PkceUtil.generateCodeChallenge("S256", "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"));
            Assert.Equal("plain-verifier", PkceUtil.generateCodeChallenge("plain", "plain-verifier"));
            Assert.Matches("^[A-Za-z0-9_-]+$", PkceUtil.generateCodeVerifier());
        }

        [Fact]
        public void Pkce_newStringUsAscii()
        {
            Assert.Equal("abc", PkceUtil.newStringUsAscii(new byte[] { 97, 98, 99 }));
        }
    }
}
