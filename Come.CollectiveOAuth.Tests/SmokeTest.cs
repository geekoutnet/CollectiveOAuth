using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class SmokeTest
    {
        [Fact]
        public void Library_is_referenced()
        {
            Assert.NotNull(typeof(Come.CollectiveOAuth.Config.ClientConfig));
        }
    }
}
