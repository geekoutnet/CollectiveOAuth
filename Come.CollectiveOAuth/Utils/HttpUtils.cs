using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Come.CollectiveOAuth.Utils
{
    public class HttpUtils
    {
        /// <summary>
        /// 全局复用的 HttpClient，避免每次请求新建连接导致端口耗尽
        /// </summary>
        private static readonly HttpClient httpClient;

        static HttpUtils()
        {
            // 在原有协议基础上追加 TLS1.2（.NET Framework 默认不开启），只设置一次，不覆盖全局配置
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
#if NETCOREAPP3_1
            // 定期重建连接，使 DNS 变更能够生效
            var handler = new SocketsHttpHandler
            {
                UseCookies = false,
                AllowAutoRedirect = true,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            };
#else
            var handler = new HttpClientHandler
            {
                UseCookies = false,
                AllowAutoRedirect = true
            };
#endif
            // 不共享 Cookie，防止不同用户的请求互相串号
            httpClient = new HttpClient(handler);
        }

        /// <summary>
        /// 模拟Form表单post请求
        /// </summary>
        /// <param name="postUrl"></param>
        /// <param name="postData"></param>
        /// <param name="header"></param>
        /// <param name="charset"></param>
        /// <returns></returns>
        public static string RequestFormPost(string postUrl, string postData = null, Dictionary<string, object> header = null, string charset = null)
        {
            return RequestPost(postUrl, postData, header, charset, "application/x-www-form-urlencoded");
        }

        /// <summary>
        /// 普通post请求
        /// </summary>
        /// <param name="postUrl"></param>
        /// <param name="postData"></param>
        /// <param name="header"></param>
        /// <param name="charset"></param>
        /// <returns></returns>
        public static string RequestPost(string postUrl, string postData = null, Dictionary<string, object> header = null, string charset = null)
        {
            return RequestPost(postUrl, postData, header, charset, "application/json");
        }

        /// <summary>
        /// 有一些请求比较特殊，需要标记Accept为application/json
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string RequestJsonGet(string url, Dictionary<string, object> header = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", "Foo");
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            SetRequestHeader(request, header);
            return Send(request, Encoding.UTF8);
        }

        /// <summary>
        /// 发送Get请求
        /// </summary>
        /// <param name="url">地址</param>
        /// <param name="header">请求头</param>
        public static string RequestGet(string url, Dictionary<string, object> header = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            SetRequestHeader(request, header);
            return Send(request, Encoding.UTF8);
        }

        private static string RequestPost(string postUrl, string postData, Dictionary<string, object> header, string charset, string mediaType)
        {
            if (string.IsNullOrWhiteSpace(charset))
            {
                charset = "UTF-8";
            }
            Encoding encoding = Encoding.GetEncoding(charset);

            var request = new HttpRequestMessage(HttpMethod.Post, postUrl);
            request.Content = new ByteArrayContent(encoding.GetBytes(postData ?? ""));
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse($"{mediaType};charset={charset.ToLower()}");
            SetRequestHeader(request, header);
            return Send(request, encoding);
        }

        /// <summary>
        /// 通用的设置RequestHeader方法
        /// </summary>
        private static void SetRequestHeader(HttpRequestMessage request, Dictionary<string, object> header)
        {
            if (header == null || header.Count == 0)
            {
                return;
            }
            foreach (var item in header)
            {
                var value = Convert.ToString(item.Value);
                switch (item.Key.ToUpper())
                {
                    case "HOST":
                        // Host 由请求地址决定，无需手动设置
                        break;
                    case "CONTENT-TYPE":
                        if (request.Content != null)
                        {
                            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(value);
                        }
                        break;
                    default:
                        request.Headers.Remove(item.Key);
                        request.Headers.TryAddWithoutValidation(item.Key, value);
                        break;
                }
            }
        }

        /// <summary>
        /// 发送请求，非 2xx 响应统一抛出异常，异常信息包含状态码和响应内容
        /// </summary>
        private static string Send(HttpRequestMessage request, Encoding encoding)
        {
            using (request)
            using (var response = httpClient.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult())
            {
                var bytes = response.Content.ReadAsByteArrayAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                var content = encoding.GetString(bytes);
                if (!response.IsSuccessStatusCode)
                {
                    // 响应可能是整页 HTML，截断后再放入异常信息
                    var detail = content.Length > 500 ? content.Substring(0, 500) + "..." : content;
                    throw new Exception($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {detail}");
                }
                return content;
            }
        }
    }
}
