using System;
using System.Reflection;
using System.Threading;
using Come.CollectiveOAuth.Cache;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class CoreStateCacheTest
    {
        private static object PruneTimer()
        {
            return typeof(AuthDefaultCache).GetField("pruneTimer", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        }

        [Fact]
        public void new_instances_share_single_prune_timer()
        {
            new AuthDefaultCache();
            var timer = PruneTimer();
            Assert.NotNull(timer);
            for (int i = 0; i < 20; i++)
            {
                new AuthDefaultCache();
            }
            Assert.Same(timer, PruneTimer());
        }

        [Fact]
        public void schedulePrune_ignores_non_positive_delay()
        {
            var cache = new AuthDefaultCache();
            cache.schedulePrune(0);
            cache.schedulePrune(-1);
            cache.schedulePrune(long.MaxValue);
            cache.schedulePrune(AuthCacheConfig.timeout);
        }

        [Fact]
        public void tryRemove_is_one_shot_and_ignores_expired()
        {
            var cache = new DefaultAuthStateCache();
            cache.cache("core-try-1", "v");
            Assert.True(cache.tryRemove("core-try-1"));
            Assert.False(cache.tryRemove("core-try-1"));
            Assert.False(cache.containsKey("core-try-1"));

            cache.cache("core-try-2", "v", 1);
            Thread.Sleep(20);
            Assert.False(cache.tryRemove("core-try-2"));
            Assert.False(cache.tryRemove("core-missing"));
        }

        [Fact]
        public void distributed_state_cache_operations()
        {
            var cache = new DistributedAuthStateCache(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));
            Assert.IsAssignableFrom<IAtomicAuthStateCache>(cache);
            cache.cache("k1", "v1");
            Assert.Equal("v1", cache.get("k1"));
            Assert.True(cache.containsKey("k1"));
            cache.remove("k1");
            Assert.Null(cache.get("k1"));
            Assert.False(cache.containsKey("k1"));

            cache.cache("k2", "v2", 60 * 1000);
            Assert.True(cache.tryRemove("k2"));
            Assert.False(cache.tryRemove("k2"));
            Assert.False(cache.containsKey("k2"));

            cache.cache("k3", "v3", 1);
            Thread.Sleep(30);
            Assert.False(cache.tryRemove("k3"));
        }

        [Fact]
        public void httpRuntimeCache_usable_after_removeAll()
        {
            HttpRuntimeCache.Set("core-rt-1", "v");
            Assert.Equal("v", HttpRuntimeCache.Get("core-rt-1"));
            HttpRuntimeCache.RemoveAll();
            Assert.Null(HttpRuntimeCache.Get("core-rt-1"));
            HttpRuntimeCache.Set("core-rt-2", "v2");
            Assert.Equal("v2", HttpRuntimeCache.Get("core-rt-2"));
            HttpRuntimeCache.Remove("core-rt-2");
        }
    }
}
