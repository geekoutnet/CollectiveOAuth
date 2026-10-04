using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class JsonExtensionsTest
    {
        private const string Json = "{\"s\":\"x\",\"n\":12,\"f\":1.50,\"b\":true,\"bs\":\"1\",\"nul\":null,\"d\":\"2020-01-01T00:00:00Z\",\"o\":{\"k\":1},\"a\":[1,2],\"ns\":\"7\"}";

        [Fact]
        public void getString_semantics()
        {
            var o = JsonExtensions.parseObject(Json);
            Assert.Equal("x", o.getString("s"));
            Assert.Equal("12", o.getString("n"));
            Assert.Equal("1.50", o.getString("f"));
            Assert.Equal("true", o.getString("b"));
            Assert.Null(o.getString("nul"));
            Assert.Null(o.getString("missing"));
            Assert.Equal("2020-01-01T00:00:00Z", o.getString("d"));
            Assert.Equal("{\"k\":1}", o.getString("o"));
            Assert.Equal("[1,2]", o.getString("a"));
        }

        [Fact]
        public void numeric_and_boolean_semantics()
        {
            var o = JsonExtensions.parseObject(Json);
            Assert.Equal(12, o.getIntValue("n"));
            Assert.Equal(7, o.getIntValue("ns"));
            Assert.Equal(0, o.getIntValue("missing"));
            Assert.Null(o.getInteger("missing"));
            Assert.Equal(12L, o.getLongValue("n"));
            Assert.True(o.getBooleanValue("b"));
            Assert.True(o.getBooleanValue("bs"));
            Assert.False(o.getBooleanValue("missing"));
            Assert.Null(o.getBoolean("missing"));
            Assert.Equal(1, o.getJSONObject("o").getIntValue("k"));
            Assert.Equal(2, o.getJSONArray("a").Count);
            Assert.True(o.containsKey("nul"));
            Assert.False(o.containsKey("missing"));
        }

        [Fact]
        public void parse_empty_returns_null()
        {
            Assert.Null(JsonExtensions.parseObject(""));
            Assert.Null(JsonExtensions.parseObject(null));
        }
    }
}
