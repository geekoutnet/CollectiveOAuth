using System;
using System.Collections.Generic;
using System.Linq;
using Come.CollectiveOAuth.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Come.CollectiveOAuth.Tests.Support
{
    internal static class GoldenNormalizer
    {
        public static JObject Url(string url, IDictionary<string, string> extraParams, ISet<string> ignore)
        {
            if (url == null)
            {
                return null;
            }
            int index = url.IndexOf('?');
            string baseUrl = index < 0 ? url : url.Substring(0, index);
            var pairs = new List<KeyValuePair<string, string>>();
            if (index >= 0)
            {
                pairs.AddRange(ParsePairs(url.Substring(index + 1)));
            }
            if (extraParams != null)
            {
                pairs.AddRange(extraParams.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value)));
            }
            return new JObject { ["base"] = baseUrl, ["params"] = Params(pairs, ignore) };
        }

        public static IEnumerable<KeyValuePair<string, string>> ParsePairs(string query)
        {
            foreach (var part in query.Split('&'))
            {
                if (part.Length == 0)
                {
                    continue;
                }
                int eq = part.IndexOf('=');
                string key = eq < 0 ? part : part.Substring(0, eq);
                string value = eq < 0 ? null : part.Substring(eq + 1);
                yield return new KeyValuePair<string, string>(GlobalAuthUtil.urlDecode(key), value == null ? null : GlobalAuthUtil.urlDecode(value));
            }
        }

        public static JArray Params(IEnumerable<KeyValuePair<string, string>> pairs, ISet<string> ignore)
        {
            return new JArray(pairs
                .Select(kv => ignore.Contains(kv.Key) ? kv.Key + "=<ignored>" : kv.Key + "=" + (kv.Value ?? ""))
                .OrderBy(s => s, StringComparer.Ordinal));
        }

        public static JToken Body(string body, bool isForm, ISet<string> ignore)
        {
            if (string.IsNullOrEmpty(body))
            {
                return null;
            }
            if (isForm)
            {
                return Params(ParsePairs(body), ignore);
            }
            try
            {
                return Numbers(JToken.Parse(body));
            }
            catch (JsonReaderException)
            {
                return body;
            }
        }

        public static JObject Headers(IDictionary<string, string> headers, ISet<string> ignore)
        {
            var result = new JObject();
            foreach (var kv in headers.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
            {
                var key = kv.Key.ToUpperInvariant();
                if (key == "CONTENT-TYPE" || key == "HOST" || key == "USER-AGENT")
                {
                    continue;
                }
                result[key] = ignore.Contains(kv.Key) ? "<ignored>" : kv.Value;
            }
            return result;
        }

        /// <summary>
        /// 把数字统一为 decimal（避免 1 与 1.0 不等），并删除值为 null 的属性（fastjson 输出时会省略 null）
        /// </summary>
        public static JToken Numbers(JToken token)
        {
            switch (token)
            {
                case JObject obj:
                    foreach (var prop in obj.Properties().ToList())
                    {
                        if (prop.Value.Type == JTokenType.Null)
                        {
                            prop.Remove();
                            continue;
                        }
                        prop.Value = Numbers(prop.Value);
                    }
                    return obj;
                case JArray arr:
                    for (int i = 0; i < arr.Count; i++)
                    {
                        arr[i] = Numbers(arr[i]);
                    }
                    return arr;
                case JValue v when v.Type == JTokenType.Integer || v.Type == JTokenType.Float:
                    return new JValue(Convert.ToDecimal(v.Value));
                default:
                    return token;
            }
        }

        public static JToken Result(object csharpResult)
        {
            if (csharpResult == null)
            {
                return null;
            }
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Converters = { new StringEnumConverter() } };
            var token = JToken.Parse(JsonConvert.SerializeObject(csharpResult, settings));
            RemoveCsharpOnly(token);
            if (token is JObject obj)
            {
                if (obj["msg"] is JValue msg && msg.Type == JTokenType.String)
                {
                    obj["msg"] = MapSourceInMessage((string)msg);
                }
                if (obj["data"] is JObject data && data["source"] is JValue source)
                {
                    data["source"] = AuthSourceNames.toJustAuthName((string)source);
                }
            }
            return Numbers(token);
        }

        public static string MapSourceInMessage(string msg)
        {
            int start = msg.LastIndexOf('[');
            if (start < 0 || !msg.EndsWith("]"))
            {
                return msg;
            }
            string name = msg.Substring(start + 1, msg.Length - start - 2);
            return msg.Substring(0, start + 1) + AuthSourceNames.toJustAuthName(name) + "]";
        }

        private static void RemoveCsharpOnly(JToken token)
        {
            if (token is JObject obj)
            {
                obj.Remove("originalUser");
                obj.Remove("originalUserStr");
                foreach (var prop in obj.Properties())
                {
                    RemoveCsharpOnly(prop.Value);
                }
            }
            else if (token is JArray arr)
            {
                foreach (var child in arr)
                {
                    RemoveCsharpOnly(child);
                }
            }
        }
    }
}
