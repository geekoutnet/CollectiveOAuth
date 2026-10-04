using System.Threading;
using Come.CollectiveOAuth.Cache;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class AuthStateCacheTest
    {
        [Fact]
        public void cache_default_timeout()
        {
            var cache = new DefaultAuthStateCache();
            cache.cache("key1", "value");
            Assert.Equal("value", cache.get("key1"));
            Thread.Sleep(40);
            Assert.Equal("value", cache.get("key1"));
        }

        [Fact]
        public void cache_custom_timeout_expires()
        {
            var cache = new DefaultAuthStateCache();
            cache.cache("key2", "value", 200);
            Assert.Equal("value", cache.get("key2"));
            Thread.Sleep(100);
            Assert.Equal("value", cache.get("key2"));
            Thread.Sleep(150);
            Assert.Null(cache.get("key2"));
            Assert.False(cache.containsKey("key2"));
        }

        [Fact]
        public void instances_share_storage_and_remove()
        {
            new DefaultAuthStateCache().cache("key3", "v");
            Assert.True(new DefaultAuthStateCache().containsKey("key3"));
            new DefaultAuthStateCache().remove("key3");
            Assert.False(new DefaultAuthStateCache().containsKey("key3"));
        }

        [Fact]
        public void prune_removes_expired()
        {
            var cache = new AuthDefaultCache();
            cache.set("key4", "v", 1);
            Thread.Sleep(20);
            cache.pruneCache();
            Assert.Null(cache.get("key4"));
        }
    }
}
