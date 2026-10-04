using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Come.CollectiveOAuth.Utils
{
    public class UrlBuilder
    {
        private readonly Dictionary<string, string> paramDic = new Dictionary<string, string>();
        private string baseUrl;

        private UrlBuilder()
        {

        }

        /**
         * @param baseUrl 基础路径
         * @return the new {@code UrlBuilder}
         */
        public static UrlBuilder fromBaseUrl(string baseUrl)
        {
            UrlBuilder builder = new UrlBuilder();
            builder.baseUrl = baseUrl;
            return builder;
        }

        /**
         * 只读的参数Map
         *
         * @return unmodifiableMap
         */
        public IReadOnlyDictionary<string, string> getReadOnlyParams()
        {
            return new ReadOnlyDictionary<string, string>(this.paramDic);
        }

        /**
         * 添加参数，同名参数覆盖（与 JustAuth 一致）
         *
         * @param key   参数名称
         * @param value 参数值
         * @return this UrlBuilder
         */
        public UrlBuilder queryParam(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("参数名不能为空");
            }
            this.paramDic[key] = value?.ToString();
            return this;
        }

        /**
         * 构造url，有意差异：默认对参数值编码（JustAuth 默认不编码）
         *
         * @return url
         */
        public string build()
        {
            return this.build(true);
        }

        /**
         * 构造url
         *
         * @param encode 转码
         * @return url
         */
        public string build(bool encode)
        {
            if (this.paramDic.Count == 0)
            {
                return this.baseUrl;
            }
            string baseUrl = this.appendIfNotContain(this.baseUrl, "?", "&");
            return baseUrl + GlobalAuthUtil.parseMapToString(this.paramDic, encode);
        }

        /**
        * 如果给定字符串{@code str}中不包含{@code appendStr}，则在{@code str}后追加{@code appendStr}；
        * 如果已包含{@code appendStr}，则在{@code str}后追加{@code otherwise}
        *
        * @param str       给定的字符串
        * @param appendStr 需要追加的内容
        * @param otherwise 当{@code appendStr}不满足时追加到{@code str}后的内容
        * @return 追加后的字符串
        */
        public string appendIfNotContain(string str, string appendStr, string otherwise)
        {
            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(appendStr))
            {
                return str;
            }
            if (str.Contains(appendStr))
            {
                return str + otherwise;
            }
            return str + appendStr;
        }
    }
}
