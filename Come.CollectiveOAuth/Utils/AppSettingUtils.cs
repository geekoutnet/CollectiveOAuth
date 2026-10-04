
namespace Come.CollectiveOAuth.Utils
{
    public class AppSettingUtils
    {
#if !NET45
        private static Microsoft.Extensions.Configuration.IConfiguration _configuration;

        /// <summary>
        /// 使用 ASP.NET Core 的 IConfiguration 读取配置（读取 AppSettings 节点），
        /// 支持 appsettings.{Environment}.json、环境变量等全部配置源；未调用时退回读取 appsettings.json
        /// </summary>
        /// <param name="configuration"></param>
        public static void UseConfiguration(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _configuration = configuration;
        }
#endif

        /// <summary>
        /// 根据Key取Value值
        /// </summary>
        /// <param name="key"></param>
        public static string GetStrValue(string key)
        {
            var value = "";
#if NET45
            value = System.Configuration.ConfigurationManager.AppSettings[key];
#else
            value = _configuration != null
                ? _configuration[$"{Come.CollectiveOAuth.Utils.ConfigurationManager.ConfigSection}:{key}"]
                : Come.CollectiveOAuth.Utils.ConfigurationManager.AppSettings[key];
#endif
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.ToString().Trim();
            }
            return value;
        }
    }
}