using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Come.CollectiveOAuth.Cache;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Models;
using Come.CollectiveOAuth.Tests.Support;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    [Collection("HttpSender")]
    public class GoldenTests
    {
        private static string Dir(string name) => Path.Combine(AppContext.BaseDirectory, name);

        public static IEnumerable<object[]> Cases()
        {
            foreach (var file in Directory.GetFiles(Dir("Fixtures"), "*.json").OrderBy(f => f))
            {
                var fixture = JObject.Parse(File.ReadAllText(file));
                yield return new object[] { fixture.Value<string>("source"), "authorize" };
                foreach (var scenario in fixture["scenarios"])
                {
                    yield return new object[] { fixture.Value<string>("source"), scenario.Value<string>("name") };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Cases))]
        public void Matches_justauth(string source, string scenarioName)
        {
            Assert.True(GoldenRequestFactory.IsRegistered(source), $"{source} 未在 GoldenRequestFactory 登记");
            var fixture = JObject.Parse(File.ReadAllText(Path.Combine(Dir("Fixtures"), source + ".json")));
            var golden = JObject.Parse(File.ReadAllText(Path.Combine(Dir("Golden"), source + ".json")));
            var ignore = new HashSet<string>(fixture["ignore"]?["params"]?.ToObject<string[]>() ?? new string[0]);
            var config = fixture["config"].ToObject<ClientConfig>();
            string state = fixture.Value<string>("state");

            if (scenarioName == "authorize")
            {
                var goldenCtorError = golden["constructorError"];
                if (goldenCtorError != null && goldenCtorError.Type != JTokenType.Null)
                {
                    var ex = Assert.Throws<AuthException>(() => GoldenRequestFactory.Create(source, config, new DefaultAuthStateCache()));
                    Assert.Equal(goldenCtorError.Value<int>("code"), ex.errorCode);
                    Assert.Equal(goldenCtorError.Value<string>("msg"), GoldenNormalizer.MapSourceInMessage(ex.errorMsg));
                    return;
                }
                var request = GoldenRequestFactory.Create(source, config, new DefaultAuthStateCache());
                AssertJson(GoldenNormalizer.Url(golden.Value<string>("authorizeUrl"), null, ignore), GoldenNormalizer.Url(request.authorize(state), null, ignore), "authorizeUrl");
                return;
            }

            var fixtureScenario = fixture["scenarios"].First(s => s.Value<string>("name") == scenarioName);
            var goldenScenario = golden["scenarios"].First(s => s.Value<string>("name") == scenarioName);
            var sender = new RecordingHttpSender(fixtureScenario["responses"].ToObject<string[]>());
            var original = HttpUtils.Sender;
            HttpUtils.Sender = sender;
            object result = null;
            Exception error = null;
            try
            {
                var request = GoldenRequestFactory.Create(source, config, new DefaultAuthStateCache());
                request.authorize(state);
                switch (fixtureScenario.Value<string>("action"))
                {
                    case "login":
                        result = request.login(fixtureScenario["callback"].ToObject<AuthCallback>());
                        break;
                    case "refresh":
                        result = request.refresh(fixtureScenario["token"].ToObject<AuthToken>());
                        break;
                    case "revoke":
                        result = request.revoke(fixtureScenario["token"].ToObject<AuthToken>());
                        break;
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                HttpUtils.Sender = original;
            }

            // 请求序列
            var goldenRequests = (JArray)goldenScenario["requests"];
            Assert.True(goldenRequests.Count == sender.requests.Count,
                $"请求数量不同：Java {goldenRequests.Count}，C# {sender.requests.Count}\nJava: {goldenRequests}\nC#: {string.Join("\n", sender.requests.Select(r => r.method + " " + r.url))}");
            for (int i = 0; i < goldenRequests.Count; i++)
            {
                var g = (JObject)goldenRequests[i];
                var c = sender.requests[i];
                Assert.Equal(g.Value<string>("method"), c.method);
                var gParams = g["params"]?.Type == JTokenType.Object ? g["params"].ToObject<Dictionary<string, string>>() : null;
                bool gIsGet = g.Value<string>("method") == "GET";
                AssertJson(GoldenNormalizer.Url(g.Value<string>("url"), gIsGet ? gParams : null, ignore), GoldenNormalizer.Url(c.url, null, ignore), $"request[{i}].url");

                JToken gBody = !gIsGet && gParams != null
                    ? GoldenNormalizer.Params(gParams.Select(kv => kv), ignore)
                    : GoldenNormalizer.Body(g.Value<string>("data"), false, ignore);
                JToken cBody = GoldenNormalizer.Body(c.body, c.contentType == "application/x-www-form-urlencoded", ignore);
                AssertJson(gBody, cBody, $"request[{i}].body");

                AssertJson(GoldenNormalizer.Headers(g["headers"].ToObject<Dictionary<string, string>>(), ignore), GoldenNormalizer.Headers(c.headers, ignore), $"request[{i}].headers");
            }

            // 结果或异常
            var goldenError = goldenScenario["error"];
            if (goldenError != null && goldenError.Type != JTokenType.Null)
            {
                var authException = Assert.IsType<AuthException>(error);
                Assert.Equal(goldenError.Value<int?>("code") ?? 0, authException.errorCode);
                Assert.Equal(goldenError.Value<string>("msg"), authException.errorMsg == null ? null : GoldenNormalizer.MapSourceInMessage(authException.errorMsg));
                return;
            }
            Assert.Null(error);
            AssertJson(GoldenNormalizer.Numbers(goldenScenario["result"].DeepClone()), GoldenNormalizer.Result(result), "result");
        }

        private static void AssertJson(JToken expected, JToken actual, string what)
        {
            Assert.True(JToken.DeepEquals(expected, actual), $"{what} 不一致\nJava: {expected}\nC#:   {actual}");
        }
    }
}
