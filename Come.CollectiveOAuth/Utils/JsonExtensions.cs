using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Come.CollectiveOAuth.Utils
{
    /// <summary>
    /// 以 fastjson JSONObject 的语义读取 JObject，供对齐 JustAuth 的平台代码使用
    /// </summary>
    public static class JsonExtensions
    {
        private static JToken parse(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Decimal })
            {
                return JToken.ReadFrom(reader);
            }
        }

        /// <summary>
        /// 对应 JSONObject.parseObject：空串返回 null，非法 JSON 抛出异常
        /// </summary>
        public static JObject parseObject(string json)
        {
            return (JObject)parse(json);
        }

        /// <summary>
        /// 对应 JSONArray.parseArray
        /// </summary>
        public static JArray parseArray(string json)
        {
            return (JArray)parse(json);
        }

        private static JToken valueOf(JObject obj, string key)
        {
            var token = obj?[key];
            return token == null || token.Type == JTokenType.Null ? null : token;
        }

        public static bool containsKey(this JObject obj, string key)
        {
            return obj != null && obj.ContainsKey(key);
        }

        public static string getString(this JObject obj, string key)
        {
            var token = valueOf(obj, key);
            if (token == null)
            {
                return null;
            }
            switch (token.Type)
            {
                case JTokenType.String:
                    return (string)token;
                case JTokenType.Boolean:
                    return (bool)token ? "true" : "false";
                case JTokenType.Integer:
                case JTokenType.Float:
                    return ((JValue)token).ToString(CultureInfo.InvariantCulture);
                default:
                    return token.ToString(Formatting.None);
            }
        }

        public static int? getInteger(this JObject obj, string key)
        {
            var value = obj.getString(key);
            if (string.IsNullOrEmpty(value) || "null".Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            if (value == "true" || value == "false")
            {
                return value == "true" ? 1 : 0;
            }
            return (int)decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public static int getIntValue(this JObject obj, string key)
        {
            return obj.getInteger(key) ?? 0;
        }

        public static long? getLong(this JObject obj, string key)
        {
            var value = obj.getString(key);
            if (string.IsNullOrEmpty(value) || "null".Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            if (value == "true" || value == "false")
            {
                return value == "true" ? 1 : 0;
            }
            return (long)decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public static long getLongValue(this JObject obj, string key)
        {
            return obj.getLong(key) ?? 0L;
        }

        /// <summary>
        /// fastjson castToBoolean：true/1/Y/T 为真，false/0/N/F 为假，数字非 1 为假
        /// </summary>
        public static bool? getBoolean(this JObject obj, string key)
        {
            var value = obj.getString(key);
            if (string.IsNullOrEmpty(value) || "null".Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            if ("true".Equals(value, StringComparison.OrdinalIgnoreCase) || value == "1" || "Y".Equals(value, StringComparison.OrdinalIgnoreCase) || "T".Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if ("false".Equals(value, StringComparison.OrdinalIgnoreCase) || value == "0" || "N".Equals(value, StringComparison.OrdinalIgnoreCase) || "F".Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                return number == 1;
            }
            throw new FormatException($"can not cast to boolean, value : {value}");
        }

        public static bool getBooleanValue(this JObject obj, string key)
        {
            return obj.getBoolean(key) ?? false;
        }

        public static JObject getJSONObject(this JObject obj, string key)
        {
            var token = valueOf(obj, key);
            if (token == null)
            {
                return null;
            }
            return token.Type == JTokenType.String ? parseObject((string)token) : (JObject)token;
        }

        public static JArray getJSONArray(this JObject obj, string key)
        {
            var token = valueOf(obj, key);
            if (token == null)
            {
                return null;
            }
            return token.Type == JTokenType.String ? parseArray((string)token) : (JArray)token;
        }
    }
}
