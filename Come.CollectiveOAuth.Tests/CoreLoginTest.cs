using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Request;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class CoreLoginTest
    {
        private class FakeRequest : DefaultAuthRequest
        {
            public AuthUser user = new AuthUser { uuid = "u" };

            public FakeRequest(ClientConfig config, IAuthStateCache cache, IAuthSource source = null)
                : base(config, source ?? new GithubAuthSource(), cache)
            {
            }

            protected override AuthToken getAccessToken(AuthCallback authCallback)
            {
                return new AuthToken { accessToken = "t" };
            }

            protected override AuthUser getUserInfo(AuthToken authToken)
            {
                return user;
            }
        }

        /// <summary>
        /// 只实现 IAuthStateCache（非原子），验证旧的 containsKey + remove 路径仍然可用
        /// </summary>
        private class PlainStateCache : IAuthStateCache
        {
            private readonly Dictionary<string, string> map = new Dictionary<string, string>();
            public void cache(string key, string value) { map[key] = value; }
            public void cache(string key, string value, long timeout) { map[key] = value; }
            public string get(string key) { return map.TryGetValue(key, out var v) ? v : null; }
            public bool containsKey(string key) { return map.ContainsKey(key); }
            public void remove(string key) { map.Remove(key); }
        }

        private static ClientConfig Config()
        {
            return new ClientConfig { clientId = "id", clientSecret = "secret", redirectUri = "https://a.com/cb" };
        }

        [Fact]
        public void concurrent_replay_of_same_state_succeeds_only_once()
        {
            var cache = new DefaultAuthStateCache();
            var request = new FakeRequest(Config(), cache);
            request.authorize("core-concurrent-state");
            int success = 0;
            int illegalState = 0;
            Parallel.For(0, 100, _ =>
            {
                var code = request.login(new AuthCallback { code = "c", state = "core-concurrent-state" }).code;
                if (code == 2000) Interlocked.Increment(ref success);
                if (code == 5009) Interlocked.Increment(ref illegalState);
            });
            Assert.Equal(1, success);
            Assert.Equal(99, illegalState);
        }

        [Fact]
        public void non_atomic_cache_keeps_legacy_behavior()
        {
            var cache = new PlainStateCache();
            var request = new FakeRequest(Config(), cache);
            request.authorize("plain-state");
            Assert.Equal(2000, request.login(new AuthCallback { code = "c", state = "plain-state" }).code);
            Assert.False(cache.containsKey("plain-state"));
            Assert.Equal(5009, request.login(new AuthCallback { code = "c", state = "plain-state" }).code);
            Assert.Equal(5009, request.login(new AuthCallback { code = "c" }).code);
        }

        [Fact]
        public void empty_or_unknown_state_is_illegal_with_atomic_cache()
        {
            var request = new FakeRequest(Config(), new DefaultAuthStateCache());
            Assert.Equal(5009, request.login(new AuthCallback { code = "c" }).code);
            Assert.Equal(5009, request.login(new AuthCallback { code = "c", state = "never-issued" }).code);
        }

        [Fact]
        public void twitter_skips_state_check_and_removes_state()
        {
            var cache = new DefaultAuthStateCache();
            var request = new FakeRequest(Config(), cache, new TwitterAuthSource());
            Assert.Equal(2000, request.login(new AuthCallback()).code);
            cache.cache("tw-state", "tw-state");
            Assert.Equal(2000, request.login(new AuthCallback { state = "tw-state" }).code);
            Assert.False(cache.containsKey("tw-state"));
        }
    }
}
