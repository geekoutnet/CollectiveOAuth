using System.Collections.Generic;
using Come.CollectiveOAuth.Tests.Support;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class GoldenNormalizerTest
    {
        [Fact]
        public void json_body_ignores_key_names_and_dotted_paths()
        {
            var ignore = new HashSet<string> { "signature", "metas.timestamp" };
            var a = GoldenNormalizer.Body("{\"signature\":\"x\",\"metas\":{\"timestamp\":1,\"app\":\"k\"},\"inner\":{\"timestamp\":5}}", false, ignore);
            var b = GoldenNormalizer.Body("{\"signature\":\"y\",\"metas\":{\"timestamp\":2,\"app\":\"k\"},\"inner\":{\"timestamp\":5}}", false, ignore);
            Assert.True(JToken.DeepEquals(a, b));
            // 点分路径只匹配对应位置，其他位置的同名字段仍参与比较
            var c = GoldenNormalizer.Body("{\"signature\":\"y\",\"metas\":{\"timestamp\":2,\"app\":\"k\"},\"inner\":{\"timestamp\":6}}", false, ignore);
            Assert.False(JToken.DeepEquals(a, c));
        }
    }
}
