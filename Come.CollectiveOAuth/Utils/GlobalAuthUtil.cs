using Come.CollectiveOAuth.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Web;
using System.Globalization;
using System.Security.Cryptography;

namespace Come.CollectiveOAuth.Utils
{
    public static class GlobalAuthUtil
    {
        /// <summary>
        /// 把字典集合拼接成&符号链接的字符串
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public static string parseMapToString(Dictionary<string, object> dicParams)
        {
            StringBuilder builder = new StringBuilder();
            if (dicParams.Count > 0)
            {
                builder.Append("");
                int i = 0;
                foreach (KeyValuePair<string, object> item in dicParams)
                {
                    if (i > 0)
                        builder.Append("&");
                    builder.AppendFormat("{0}={1}", item.Key, Convert.ToString(item.Value));
                    i++;
                }
            }
            return builder.ToString();
        }


        /**
         * 是否为http协议
         *
         * @param url 待验证的url
         * @return true: http协议, false: 非http协议
         */
        public static bool isHttpProtocol(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }
            return url.StartsWith("http://") || url.StartsWith("http%3A%2F%2F");
        }

        /**
         * 是否为https协议
         *
         * @param url 待验证的url
         * @return true: https协议, false: 非https协议
         */
        public static bool isHttpsProtocol(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }
            return url.StartsWith("https://") || url.StartsWith("https%3A%2F%2F");
        }

        /**
         * 是否为本地主机（域名）
         *
         * @param url 待验证的url
         * @return true: 本地主机（域名）, false: 非本地主机（域名）
         */
        public static bool isLocalHost(string url)
        {
            return string.IsNullOrEmpty(url) || url.Contains("127.0.0.1") || url.Contains("localhost");
        }


        /**
           * 获取用户的实际性别，常规网站
           *
           * @param originalGender 用户第三方标注的原始性别
           * @return 用户性别
           */
        public static AuthUserGender getRealGender(string originalGender)
        {
            if (null == originalGender || Convert.ToInt32(AuthUserGender.UNKNOWN).ToString().Equals(originalGender))
            {
                return AuthUserGender.UNKNOWN;
            }
            String[] males = { "m", "男", "1", "male" };
            if (males.ToList().Contains(originalGender.ToLower()))
            {
                return AuthUserGender.MALE;
            }
            return AuthUserGender.FEMALE;
        }

        /**
        * 获取微信平台用户的实际性别，0表示未定义，1表示男性，2表示女性
        *
        * @param originalGender 用户第三方标注的原始性别
        * @return 用户性别
        * @since 1.13.2
        */
        public static AuthUserGender getWechatRealGender(string originalGender)
        {
            if (string.IsNullOrEmpty(originalGender) || "0".Equals(originalGender))
            {
                return AuthUserGender.UNKNOWN;
            }
            return getRealGender(originalGender);
        }


        /**
         * 编码，与 JustAuth 一致：仅保留 A-Za-z0-9 . - _，其余按 UTF-8 编码为大写 %XX（空格为 %20）
         *
         * @param value str
         * @return encode str
         */
        public static string urlEncode(string value)
        {
            if (value == null)
            {
                return "";
            }
            var builder = new StringBuilder();
            foreach (var b in Encoding.UTF8.GetBytes(value))
            {
                char c = (char)b;
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '.' || c == '-' || c == '_')
                {
                    builder.Append(c);
                }
                else
                {
                    builder.Append('%').Append(b.ToString("X2"));
                }
            }
            return builder.ToString();
        }


        /**
         * 解码
         *
         * @param value str
         * @return decode str
         */
        public static string urlDecode(string value)
        {
            if (value == null)
            {
                return "";
            }
            try
            {
                return HttpUtility.UrlDecode(value);  //utf-8 解码
            }
            catch (Exception e)
            {
                throw new Exception("Failed To Decode Uri", e);
            }
        }


        /**
         * 字符串转换成枚举
         *
         * @param value str
         * @return decode str
         */
        public static T enumFromString<T>(string type)
        {
            if (type.IsNullOrEmpty())
            {
                throw new Exception($"没有找到授权类型: {type}");
            }
            try
            {
                T result = (T)Enum.Parse(typeof(T), type); //utf-8 解码
                return result;
            }
            catch (Exception e)
            {
                throw new Exception($"授权类型解析失败: {type}", e);
            }
        }

        //json字符串转换为字典集合
        public static List<Dictionary<string, object>> parseListObject(this string jsonStr)
        {
            var retDic = new List<Dictionary<string, object>>();
            if (!string.IsNullOrWhiteSpace(jsonStr))
            {
                try
                {
                    retDic = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonStr);
                }
                catch (Exception ex)
                {
                }
            }
            return retDic;
        }


        //json字符串转换为字典集合
        public static Dictionary<string, object> parseObject(this string jsonStr)
        {
            var retDic = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(jsonStr))
            {
                try
                {
                    retDic = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonStr);
                }
                catch (Exception ex)
                {
                }
            }
            return retDic;
        }

        /**
	     * 将URL参数解析为Map（也可以解析Post中的键值对参数）
	     *
	     * @param paramsStr 参数字符串（或者带参数的Path）
	     * @param charset   字符集
	     * @return 参数Map
	     */

        public static Dictionary<string, object> parseUrlObject(this string paramsStr)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();
            try
            {
                if (paramsStr.IsNullOrWhiteSpace())
                {
                    return res;
                }
                // 去掉Path部分
                int pathEndPos = paramsStr.IndexOf('?');
                if (pathEndPos > -1)
                {
                    paramsStr = paramsStr.Substring(pathEndPos + 1);
                }

                return parseStringObject(paramsStr);
            }
            catch (Exception e)
            {
                return res;
            }
        }

        /**
        * string字符串转map，str格式为 {@code xxx=xxx&xxx=xxx}
        *
        * @param accessTokenStr 待转换的字符串
        * @return map
        */
        public static Dictionary<string, object> parseStringObject(this string accessTokenStr)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();
            if (accessTokenStr.Contains("&"))
            {
                string[] fields = accessTokenStr.Split("&");
                foreach (var field in fields)
                {
                    if (field.Contains("="))
                    {
                        string[] keyValue = field.Split("=");
                        res.Add(urlDecode(keyValue[0]), keyValue.Length == 2 ? urlDecode(keyValue[1]) : null);
                    }
                }
            }
            return res;
        }

        /// 把字典集合拼接成&符号链接的字符串
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        /// <param name="encode">是否对参数做URL编码，作为表单请求体时需要编码，用于签名时不编码</param>
        public static string spellParams(this Dictionary<string, object> dicParams, bool encode = false)
        {
            StringBuilder builder = new StringBuilder();
            if (dicParams.Count > 0)
            {
                builder.Append("");
                int i = 0;
                foreach (KeyValuePair<string, object> item in dicParams)
                {
                    if (i > 0)
                        builder.Append("&");
                    var value = Convert.ToString(item.Value);
                    builder.AppendFormat("{0}={1}", encode ? Uri.EscapeDataString(item.Key) : item.Key, encode ? Uri.EscapeDataString(value) : value);
                    i++;
                }
            }
            return builder.ToString();
        }

        //object的字典集合
        public static string getString(this Dictionary<string, object> dic, string key)
        {
            if (dic == null)
                return "";
            if (dic.ContainsKey(key))
            {
                return Convert.ToString(dic[key]);
            }
            else
            {
                return "";
            }
        }


        /// <summary>
        /// 获取参数Int32类型
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static int getInt32(this Dictionary<string, object> request, string paramName)
        {
            var paramValue = request.getString(paramName);
            if (!string.IsNullOrWhiteSpace(paramValue))
            {
                try
                {
                    return Convert.ToInt32(paramValue);
                }
                catch (Exception ex)
                {
                    return -1;
                }
            }
            return -1;
        }

        /// <summary>
        /// 获取参数Int64类型
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static long getLong(this Dictionary<string, object> request, string paramName)
        {
            var paramValue = request.getString(paramName);
            if (!string.IsNullOrWhiteSpace(paramValue))
            {
                try
                {
                    return Convert.ToInt64(paramValue);
                }
                catch (Exception ex)
                {
                    return -1;
                }
            }
            return -1;
        }


        /// <summary>
        /// 获取参数Bool类型
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static bool getBool(this Dictionary<string, object> request, string paramName)
        {
            var paramValue = request.getString(paramName);
            if (!string.IsNullOrWhiteSpace(paramValue))
            {
                try
                {
                    return Convert.ToBoolean(paramValue);
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取参数字符串并且转换为字典集合
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static Dictionary<string, object> getJSONObject(this Dictionary<string, object> request, string paramName)
        {
            var paramValue = request.getString(paramName);
            if (!string.IsNullOrWhiteSpace(paramValue))
            {
                try
                {
                    return paramValue.parseObject();
                }
                catch (Exception ex)
                {
                    return new Dictionary<string, object>();
                }
            }
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// 获取参数字符串并且转换为字典集合
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static List<Dictionary<string, object>> getJSONArray(this Dictionary<string, object> request, string paramName)
        {
            var paramValue = request.getString(paramName);
            if (!string.IsNullOrWhiteSpace(paramValue))
            {
                try
                {
                    return paramValue.parseListObject();
                }
                catch (Exception ex)
                {
                    return new List<Dictionary<string, object>>();
                }
            }
            return new List<Dictionary<string, object>>();
        }

        /// <summary>
        /// 获取参数字符串类型
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        /*public static string getString(this Dictionary<string, object> request, string paramName)
        {
            var paramValue = request.getString(paramName);
            if (!string.IsNullOrWhiteSpace(paramValue))
            {
                try
                {
                    return Convert.ToString(paramValue);
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            return null;
        }*/

        /// <summary>
        /// 获取参数日期类型
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static DateTime? GetParamDateTime(this Dictionary<string, object> request, string paramName)
        {
            var paramValue = request.getString(paramName);
            if (!string.IsNullOrWhiteSpace(paramValue))
            {
                try
                {
                    return Convert.ToDateTime(paramValue);
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            return null;
        }


        /// <summary>
        /// 获取参数double类型
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static double? GetParamDouble(this Dictionary<string, object> request, string paramName)
        {
            var paramValue = request.getString(paramName);
            if (!string.IsNullOrWhiteSpace(paramValue))
            {
                try
                {
                    return Convert.ToDouble(paramValue);
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取参数Decimal类型
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static Decimal? GetParamDecimal(this Dictionary<string, object> request, string paramName)
        {
            var paramValue = request.getString(paramName);
            if (!string.IsNullOrWhiteSpace(paramValue))
            {
                try
                {
                    return Convert.ToDecimal(paramValue);
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            return null;
        }


        /// <summary>
        /// 字典排序,asc排序
        /// </summary>
        /// <param name="dic">需排序的字典对象</param>
        /// <param name="isAsc">true=正序，反之倒序</param>
        public static Dictionary<string, object> Sort(this Dictionary<string, object> dic, bool isAsc = true)
        {
            Dictionary<string, object> rdic = new Dictionary<string, object>();
            if (dic.Count > 0)
            {
                List<KeyValuePair<string, object>> lst = new List<KeyValuePair<string, object>>(dic);
                lst.Sort(delegate (KeyValuePair<string, object> s1, KeyValuePair<string, object> s2)
                {
                    if (isAsc)
                    {
                        return String.CompareOrdinal(s1.Key, s2.Key);
                    }
                    else
                    {
                        return String.CompareOrdinal(s2.Key, s1.Key);
                    }
                });

                foreach (KeyValuePair<string, object> kvp in lst)
                    rdic.Add(kvp.Key, kvp.Value);
            }
            return rdic;
        }

        public static bool isHttpsProtocolOrLocalHost(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }
            return isHttpsProtocol(url) || isLocalHost(url);
        }

        /// <summary>
        /// 按 Java String.split 语义拆分：去掉末尾的空串
        /// </summary>
        private static string[] javaSplit(string value, char separator)
        {
            var parts = value.Split(separator);
            int len = parts.Length;
            while (len > 0 && parts[len - 1].Length == 0)
            {
                len--;
            }
            var result = new string[len];
            Array.Copy(parts, result, len);
            return result;
        }

        /**
         * string 转 map，格式为 xxx=xxx&xxx=xxx，与 JustAuth parseStringToMap 一致
         */
        public static Dictionary<string, string> parseStringToMap(string accessTokenStr)
        {
            var res = new Dictionary<string, string>();
            if (accessTokenStr.Contains("&"))
            {
                foreach (var field in javaSplit(accessTokenStr, '&'))
                {
                    if (field.Contains("="))
                    {
                        var keyValue = javaSplit(field, '=');
                        res[urlDecode(keyValue[0])] = keyValue.Length == 2 ? urlDecode(keyValue[1]) : null;
                    }
                }
            }
            return res;
        }

        /**
         * map 转 url 参数串，与 JustAuth parseMapToString 一致：仅编码值，null 值输出为 key=
         */
        public static string parseMapToString(IDictionary<string, string> paramMap, bool encode)
        {
            if (paramMap == null || paramMap.Count == 0)
            {
                return "";
            }
            var paramList = new List<string>();
            foreach (var item in paramMap)
            {
                paramList.Add(item.Value == null ? item.Key + "=" : item.Key + "=" + (encode ? urlEncode(item.Value) : item.Value));
            }
            return string.Join("&", paramList);
        }

        public static string generateNonce(int len)
        {
            const string s = "0123456789QWERTYUIOPLKJHGFDSAZXCVBNMqwertyuioplkjhgfdsazxcvbnm";
            var sb = new StringBuilder();
            for (int i = 0; i < len; i++)
            {
                sb.Append(s[RandomUtil.nextInt(62)]);
            }
            return sb.ToString();
        }

        public static string getTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        }

        private static byte[] hmacSha1(byte[] key, byte[] data)
        {
            using (var hmac = new HMACSHA1(key))
            {
                return hmac.ComputeHash(data);
            }
        }

        private static byte[] hmacSha256(byte[] key, byte[] data)
        {
            using (var hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(data);
            }
        }

        public static string generateDingTalkSignature(string secretKey, string timestamp)
        {
            var signData = hmacSha256(Encoding.UTF8.GetBytes(secretKey), Encoding.UTF8.GetBytes(timestamp));
            return urlEncode(Convert.ToBase64String(signData));
        }

        public static string generateTwitterSignature(IDictionary<string, string> paramMap, string method, string baseUrl, string apiSecret, string tokenSecret)
        {
            var map = new SortedDictionary<string, string>(paramMap, StringComparer.Ordinal);
            string str = parseMapToString(map, true);
            string baseStr = method.ToUpper() + "&" + urlEncode(baseUrl) + "&" + urlEncode(str);
            string signKey = apiSecret + "&" + (string.IsNullOrEmpty(tokenSecret) ? "" : tokenSecret);
            return Convert.ToBase64String(hmacSha1(Encoding.UTF8.GetBytes(signKey), Encoding.UTF8.GetBytes(baseStr)));
        }

        public static string generateXmlySignature(IDictionary<string, string> paramMap, string clientSecret)
        {
            var map = new SortedDictionary<string, string>(paramMap, StringComparer.Ordinal);
            string baseStr = Base64Utils.encode(parseMapToString(map, false));
            var sign = hmacSha1(Encoding.UTF8.GetBytes(clientSecret), Encoding.UTF8.GetBytes(baseStr));
            using (var md5Provider = MD5.Create())
            {
                return toHex(md5Provider.ComputeHash(sign));
            }
        }

        public static string generateElemeSignature(string appKey, string secret, long timestamp, string action, string token, IDictionary<string, object> parameters)
        {
            var sorted = new SortedDictionary<string, object>(parameters, StringComparer.Ordinal);
            sorted["app_key"] = appKey;
            sorted["timestamp"] = timestamp;
            var str = new StringBuilder();
            foreach (var entry in sorted)
            {
                str.Append(entry.Key).Append("=").Append(JsonConvert.SerializeObject(entry.Value));
            }
            return md5($"{action}{token}{str}{secret}").ToUpper();
        }

        public static string md5(string str)
        {
            using (var md5Provider = MD5.Create())
            {
                return toHex(md5Provider.ComputeHash(Encoding.UTF8.GetBytes(str)));
            }
        }

        public static string generateJdSignature(string appSecret, IDictionary<string, object> parameters)
        {
            var treeMap = new SortedDictionary<string, object>(parameters, StringComparer.Ordinal);
            var signBuilder = new StringBuilder(appSecret);
            foreach (var entry in treeMap)
            {
                string value = javaValueOf(entry.Value);
                if (!string.IsNullOrEmpty(entry.Key) && !string.IsNullOrEmpty(value))
                {
                    signBuilder.Append(entry.Key).Append(value);
                }
            }
            signBuilder.Append(appSecret);
            return md5(signBuilder.ToString()).ToUpper();
        }

        /// <summary>
        /// 对应 Java String.valueOf(Object)
        /// </summary>
        public static string javaValueOf(object value)
        {
            if (value == null)
            {
                return "null";
            }
            if (value is bool b)
            {
                return b ? "true" : "false";
            }
            if (value is IFormattable formattable)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }
            return value.ToString();
        }

        private static string toHex(byte[] bytes)
        {
            var builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}