using Come.CollectiveOAuth.Config;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class ClientConfigTest
    {
        [Fact]
        public void defaults_match_justauth()
        {
            var config = new ClientConfig();
            Assert.Equal("default", config.authServerId);
            Assert.Equal("CorpApp", config.loginType);
            Assert.Equal("zh", config.lang);
            Assert.False(config.ignoreCheckState);
            Assert.Null(config.clientOsType);
            config.authServerId = "aus1";
            Assert.Equal("aus1", config.authServerId);
        }

        [Fact]
        public void unionId_string_is_boolean_flag()
        {
            Assert.True(new ClientConfig { unionId = "true" }.isUnionId());
            Assert.True(new ClientConfig { unionId = "True" }.isUnionId());
            Assert.False(new ClientConfig { unionId = "" }.isUnionId());
        }
    }
}
