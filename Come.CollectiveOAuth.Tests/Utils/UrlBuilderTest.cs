using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests.Utils
{
    public class UrlBuilderTest
    {
        [Fact]
        public void build_without_encode()
        {
            string url = UrlBuilder.fromBaseUrl("https://www.zhyd.me").queryParam("name", "yadong.zhang").build(false);
            Assert.Equal("https://www.zhyd.me?name=yadong.zhang", url);
            url = UrlBuilder.fromBaseUrl(url).queryParam("github", "https://github.com/zhangyd-c").build(false);
            Assert.Equal("https://www.zhyd.me?name=yadong.zhang&github=https://github.com/zhangyd-c", url);
        }

        [Fact]
        public void build_default_encodes_values_only()
        {
            // 有意差异：JustAuth 的 build() 默认不编码，C# 默认编码
            string url = UrlBuilder.fromBaseUrl("https://www.zhyd.me").queryParam("name", "yadong.zhang").build();
            Assert.Equal("https://www.zhyd.me?name=yadong.zhang", url);
            url = UrlBuilder.fromBaseUrl(url).queryParam("github", "https://github.com/zhangyd-c").build(true);
            Assert.Equal("https://www.zhyd.me?name=yadong.zhang&github=https%3A%2F%2Fgithub.com%2Fzhangyd-c", url);
        }

        [Fact]
        public void same_key_overwrites_and_null_value()
        {
            string url = UrlBuilder.fromBaseUrl("https://a.com").queryParam("k", "1").queryParam("k", "2").queryParam("n", null).build();
            Assert.Equal("https://a.com?k=2&n=", url);
        }
    }
}
