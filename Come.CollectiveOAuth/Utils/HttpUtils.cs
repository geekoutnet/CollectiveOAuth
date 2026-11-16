using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Exceptions;

namespace Come.CollectiveOAuth.Utils
{
    /// <summary>
    /// 一次 HTTP 请求的完整描述
    /// </summary>
    public class HttpRequestRecord
    {
        public string method { get; set; }

        public string url { get; set; }

        /// <summary>
        /// 调用方显式传入的请求头（不含 Content-Type、Host）
        /// </summary>
        public Dictionary<string, string> headers { get; set; } = new Dictionary<string, string>();

        public string body { get; set; }

        /// <summary>
        /// 不含 charset 的媒体类型，GET 请求为 null
        /// </summary>
        public string contentType { get; set; }

        public string charset { get; set; } = "UTF-8";

        /// <summary>
        /// 是否为 RequestJsonGet（附加 Accept: application/json 与 User-Agent: Foo）
        /// </summary>
        public bool acceptJson { get; set; }
    }

    /// <summary>
    /// HTTP 发送器，测试时可替换为回放实现
    /// </summary>
    public interface IHttpSender
    {
        string Send(HttpRequestRecord request, HttpConfig httpConfig);
    }

    public class HttpUtils
    {
        /// <summary>
        /// 与 simple-http 一致的默认超时（毫秒）
        /// </summary>
        public const int DEFAULT_TIMEOUT = 3000;

        /// <summary>
        /// 与 simple-http 一致的默认 User-Agent
        /// </summary>
        public const string DEFAULT_USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.142 Safari/537.36 simple-http";

        internal static IHttpSender Sender { get; set; } = new DefaultHttpSender();

        /// <summary>
        /// 模拟Form表单post请求
        /// </summary>
        public static string RequestFormPost(string postUrl, string postData = null, Dictionary<string, object> header = null, string charset = null, HttpConfig httpConfig = null)
        {
            return Post(postUrl, postData, header, charset, "application/x-www-form-urlencoded", httpConfig);
        }

        /// <summary>
        /// 普通post请求
        /// </summary>
        public static string RequestPost(string postUrl, string postData = null, Dictionary<string, object> header = null, string charset = null, HttpConfig httpConfig = null)
        {
            return Post(postUrl, postData, header, charset, "application/json", httpConfig);
        }

        /// <summary>
        /// 有一些请求比较特殊，需要标记Accept为application/json
        /// </summary>
        public static string RequestJsonGet(string url, Dictionary<string, object> header = null, HttpConfig httpConfig = null)
        {
            var record = new HttpRequestRecord { method = "GET", url = url, headers = ToHeaders(header), acceptJson = true };
            return Sender.Send(record, httpConfig);
        }

        /// <summary>
        /// 发送Get请求
        /// </summary>
        public static string RequestGet(string url, Dictionary<string, object> header = null, HttpConfig httpConfig = null)
        {
            var record = new HttpRequestRecord { method = "GET", url = url, headers = ToHeaders(header) };
            return Sender.Send(record, httpConfig);
        }

        private static string Post(string postUrl, string postData, Dictionary<string, object> header, string charset, string mediaType, HttpConfig httpConfig)
        {
            var record = new HttpRequestRecord
            {
                method = "POST",
                url = postUrl,
                headers = ToHeaders(header),
                body = postData ?? "",
                contentType = mediaType,
                charset = string.IsNullOrWhiteSpace(charset) ? "UTF-8" : charset
            };
            // 调用方显式指定的 Content-Type 优先
            foreach (var item in header ?? new Dictionary<string, object>())
            {
                if ("CONTENT-TYPE".Equals(item.Key.ToUpper()))
                {
                    record.contentType = Convert.ToString(item.Value).Split(';')[0].Trim();
                }
            }
            return Sender.Send(record, httpConfig);
        }

        private static Dictionary<string, string> ToHeaders(Dictionary<string, object> header)
        {
            var headers = new Dictionary<string, string>();
            foreach (var item in header ?? new Dictionary<string, object>())
            {
                var key = item.Key.ToUpper();
                if (key == "CONTENT-TYPE" || key == "HOST")
                {
                    continue;
                }
                headers[item.Key] = Convert.ToString(item.Value);
            }
            return headers;
        }
    }

    /// <summary>
    /// 基于 HttpClient 的默认发送器
    /// </summary>
    internal class DefaultHttpSender : IHttpSender
    {
        private static readonly HttpClient defaultClient;

        /// <summary>
        /// 按代理复用 HttpClient。WebProxy 按配置取值作键（调用方每次 new 也能复用），其它 IWebProxy 实现按引用作键
        /// </summary>
        private static readonly ConcurrentDictionary<object, HttpClient> proxyClients = new ConcurrentDictionary<object, HttpClient>();

        internal static int ProxyClientCount => proxyClients.Count;

        static DefaultHttpSender()
        {
            // 在原有协议基础上追加 TLS1.2（.NET Framework 默认不开启），只设置一次，不覆盖全局配置
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            defaultClient = CreateClient(null);
        }

        private static HttpClient CreateClient(IWebProxy proxy)
        {
#if NETCOREAPP
            // 定期重建连接，使 DNS 变更能够生效
            var handler = new SocketsHttpHandler
            {
                UseCookies = false,
                AllowAutoRedirect = true,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                Proxy = proxy,
                UseProxy = proxy != null
            };
#else
            var handler = new HttpClientHandler
            {
                UseCookies = false,
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                Proxy = proxy,
                UseProxy = proxy != null
            };
#endif
            // 不共享 Cookie，防止不同用户的请求互相串号；超时由每次请求单独控制
            return new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        }

        private static object ProxyKey(IWebProxy proxy)
        {
            if (proxy is WebProxy webProxy)
            {
                // Credentials 按引用比较；BypassList 拼成字符串比较
                return Tuple.Create(webProxy.Address, webProxy.BypassProxyOnLocal, webProxy.UseDefaultCredentials,
                    string.Join("\n", webProxy.BypassList ?? new string[0]), (object)webProxy.Credentials);
            }
            return proxy;
        }

        internal static HttpClient GetClient(HttpConfig httpConfig)
        {
            var proxy = httpConfig?.proxy;
            return proxy == null ? defaultClient : proxyClients.GetOrAdd(ProxyKey(proxy), _ => CreateClient(proxy));
        }

        public string Send(HttpRequestRecord record, HttpConfig httpConfig)
        {
            var client = GetClient(httpConfig);
            int timeout = httpConfig == null ? HttpUtils.DEFAULT_TIMEOUT : httpConfig.timeout;
            var encoding = Encoding.GetEncoding(record.charset);

            using (var request = new HttpRequestMessage(new HttpMethod(record.method), record.url))
            using (var cts = timeout > 0 ? new CancellationTokenSource(timeout) : new CancellationTokenSource())
            {
                if (record.contentType != null)
                {
                    request.Content = new ByteArrayContent(encoding.GetBytes(record.body ?? ""));
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse($"{record.contentType};charset={record.charset.ToLower()}");
                }
                if (record.acceptJson)
                {
                    request.Headers.TryAddWithoutValidation("User-Agent", "Foo");
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                }
                else
                {
                    request.Headers.TryAddWithoutValidation("User-Agent", HttpUtils.DEFAULT_USER_AGENT);
                }
                foreach (var item in record.headers)
                {
                    request.Headers.Remove(item.Key);
                    request.Headers.TryAddWithoutValidation(item.Key, item.Value);
                }

                HttpResponseMessage response;
                byte[] bytes;
                try
                {
                    response = client.SendAsync(request, cts.Token).ConfigureAwait(false).GetAwaiter().GetResult();
                    bytes = response.Content.ReadAsByteArrayAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    throw new AuthException($"HTTP request timeout after {timeout}ms: {StripQuery(record.url)}");
                }
                catch (HttpRequestException e)
                {
                    // 不用 AuthException(string, Exception)：该构造不设置 errorCode（为 0），会使 responseError 返回 code=0
                    throw new AuthException($"HTTP request failed: {StripQuery(record.url)}: {e.GetBaseException().Message}");
                }

                using (response)
                {
                    var content = encoding.GetString(bytes);
                    // 与 simple-http 一致不按状态码抛异常：非 2xx 的 JSON 错误体交给各平台 checkResponse 解析
                    if (!response.IsSuccessStatusCode && !LooksLikeJson(content))
                    {
                        // HTML 错误页对调用方没有意义，只保留状态码；其他文本截断后放入异常信息
                        var trimmed = content.Trim();
                        if (trimmed.Length == 0 || trimmed.StartsWith("<"))
                        {
                            throw new AuthException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                        }
                        var detail = trimmed.Length > 500 ? trimmed.Substring(0, 500) + "..." : trimmed;
                        throw new AuthException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {detail}");
                    }
                    return content;
                }
            }
        }

        private static bool LooksLikeJson(string content)
        {
            var trimmed = content.Trim();
            return trimmed.StartsWith("{") || trimmed.StartsWith("[");
        }

        private static string StripQuery(string url)
        {
            int index = url.IndexOf('?');
            return index < 0 ? url : url.Substring(0, index);
        }
    }
}
