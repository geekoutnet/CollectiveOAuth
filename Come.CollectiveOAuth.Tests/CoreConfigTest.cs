using System;
using System.IO;
using System.Threading.Tasks;
using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    public class CoreConfigTest : IDisposable
    {
        private readonly string originalPath = ConfigurationManager.DefaultPath;
        private readonly string dir = Path.Combine(Path.GetTempPath(), "core-config-" + Guid.NewGuid().ToString("N"));

        public CoreConfigTest()
        {
            Directory.CreateDirectory(dir);
        }

        public void Dispose()
        {
            ConfigurationManager.DefaultPath = originalPath;
            ConfigurationManager.RefreshConfiguration();
            try { Directory.Delete(dir, true); } catch { }
        }

        private string Write(string name, string content)
        {
            var path = Path.Combine(dir, name);
            File.WriteAllText(path, content);
            return path;
        }

        private static void Load(string path)
        {
            ConfigurationManager.DefaultPath = path;
            // 环/异常必须在有限时间内结束，不能死循环或抛异常
            Assert.True(Task.Run(() => ConfigurationManager.RefreshConfiguration()).Wait(TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public void reads_app_settings_object()
        {
            Load(Write("ok.json", "{\"AppSettings\":{\"K\":\"V\"}}"));
            Assert.Equal("V", ConfigurationManager.AppSettings["K"]);
        }

        [Fact]
        public void url_cycle_does_not_loop_forever()
        {
            var a = Path.Combine(dir, "a.json");
            var b = Path.Combine(dir, "b.json");
            File.WriteAllText(a, "{\"AppSettings.Url\":" + Newtonsoft.Json.JsonConvert.ToString(b) + "}");
            File.WriteAllText(b, "{\"AppSettings.Url\":" + Newtonsoft.Json.JsonConvert.ToString(a) + "}");
            Load(a);
        }

        [Fact]
        public void invalid_json_is_ignored()
        {
            Load(Write("bad.json", "{ not json"));
            Assert.NotNull(ConfigurationManager.AppSettings);
        }

        [Fact]
        public void non_object_app_settings_is_ignored()
        {
            Load(Write("arr.json", "{\"AppSettings\":[1,2]}"));
            Assert.Null(ConfigurationManager.AppSettings["K"]);
        }
    }
}
