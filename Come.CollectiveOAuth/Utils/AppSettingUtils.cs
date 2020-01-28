using System.Configuration;

namespace Come.CollectiveOAuth.Utils
{
    public class AppSettingUtils
    {
        /// <summary>
        /// 根据Key取Value值
        /// </summary>
        /// <param name="key"></param>
        public static string GetStrValue(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.ToString().Trim();
            }
            return value;
        }
    }
}