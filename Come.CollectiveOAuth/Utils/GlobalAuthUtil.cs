using Come.CollectiveOAuth.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Web;

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
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }
            return url.StartsWith("http://");
        }

        /**
         * 是否为https协议
         *
         * @param url 待验证的url
         * @return true: https协议, false: 非https协议
         */
        public static bool isHttpsProtocol(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }
            return url.StartsWith("https://");
        }

        /**
         * 是否为本地主机（域名）
         *
         * @param url 待验证的url
         * @return true: 本地主机（域名）, false: 非本地主机（域名）
         */
        public static bool isLocalHost(string url)
        {
            return string.IsNullOrWhiteSpace(url) || url.Contains("127.0.0.1") || url.Contains("localhost");
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
            if (string.IsNullOrWhiteSpace(originalGender) || "0".Equals(originalGender))
            {
                return AuthUserGender.UNKNOWN;
            }
            return getRealGender(originalGender);
        }


        /**
         * 编码
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
            try
            {
               
                return HttpUtility.UrlEncode(value);
            }
            catch (Exception e)
            {
                throw new Exception("Failed To Encode Uri", e);
            }
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

        //object的字典集合
        public static string GetDicValue(this Dictionary<string, object> dic, string key)
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
        //string的字典集合
        public static string GetDicValue(this Dictionary<string, string> dic, string key)
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
        public static int GetParamInt32(this Dictionary<string, object> request, string paramName)
        {
            var paramValue = request.GetDicValue(paramName);
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
        public static long? GetParamLong(this Dictionary<string, object> request, string paramName)
        {
            var paramValue = request.GetDicValue(paramName);
            if (!string.IsNullOrWhiteSpace(paramValue))
            {
                try
                {
                    return Convert.ToInt64(paramValue);
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            return null;
        }


        /// <summary>
        /// 获取参数Bool类型
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static Boolean? GetParamBoolean(this Dictionary<string, object> request, string paramName)
        {
            var paramValue = request.GetDicValue(paramName);
            if (!string.IsNullOrWhiteSpace(paramValue))
            {
                try
                {
                    return Convert.ToBoolean(paramValue);
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取参数字符串类型
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static string GetParamString(this Dictionary<string, object> request, string paramName)
        {
            var paramValue = request.GetDicValue(paramName);
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
        }

        /// <summary>
        /// 获取参数日期类型
        /// </summary>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static DateTime? GetParamDateTime(this Dictionary<string, object> request, string paramName)
        {
            var paramValue = request.GetDicValue(paramName);
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
            var paramValue = request.GetDicValue(paramName);
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
            var paramValue = request.GetDicValue(paramName);
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
    }
}