using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Exceptions;
using Come.CollectiveOAuth.Utils;
using Xunit;

namespace Come.CollectiveOAuth.Tests
{
    /// <summary>
    /// 直接测试 DefaultHttpSender（不替换 HttpUtils.Sender），用本地 TcpListener 返回手写的 HTTP 响应
    /// </summary>
    public class DefaultHttpSenderTest
    {
        /// <summary>
        /// 在 127.0.0.1 随机端口起一个只处理一次请求的服务；delayMs 内不响应用于模拟超时
        /// </summary>
        private sealed class OneShotServer : IDisposable
        {
            private readonly TcpListener listener;
            private readonly CancellationTokenSource stop = new CancellationTokenSource();

            public string Url { get; }

            public OneShotServer(int status, string reason, string contentType, byte[] body, string extraHeaders = "", int delayMs = 0)
            {
                listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                Url = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/path?secret=1";
                Task.Run(async () =>
                {
                    try
                    {
                        using (var client = await listener.AcceptTcpClientAsync())
                        using (var stream = client.GetStream())
                        {
                            // 读到请求头结束（测试只发 GET）
                            var buffer = new byte[8192];
                            var received = new StringBuilder();
                            while (!received.ToString().Contains("\r\n\r\n"))
                            {
                                int n = await stream.ReadAsync(buffer, 0, buffer.Length);
                                if (n == 0) return;
                                received.Append(Encoding.ASCII.GetString(buffer, 0, n));
                            }
                            if (delayMs > 0)
                            {
                                await Task.Delay(delayMs, stop.Token);
                            }
                            var head = $"HTTP/1.1 {status} {reason}\r\nContent-Type: {contentType}\r\nContent-Length: {body.Length}\r\nConnection: close\r\n{extraHeaders}\r\n";
                            var headBytes = Encoding.ASCII.GetBytes(head);
                            await stream.WriteAsync(headBytes, 0, headBytes.Length);
                            await stream.WriteAsync(body, 0, body.Length);
                            await stream.FlushAsync();
                        }
                    }
                    catch (Exception)
                    {
                        // 测试结束或客户端超时断开
                    }
                });
            }

            public void Dispose()
            {
                stop.Cancel();
                listener.Stop();
            }
        }

        private static string Get(string url, HttpConfig config = null)
        {
            return new DefaultHttpSender().Send(new HttpRequestRecord { method = "GET", url = url }, config);
        }

        [Fact]
        public void gzip_response_is_decompressed()
        {
            const string json = "{\"items\":[{\"user_id\":1}]}";
            byte[] gz;
            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionMode.Compress))
                {
                    var raw = Encoding.UTF8.GetBytes(json);
                    gzip.Write(raw, 0, raw.Length);
                }
                gz = ms.ToArray();
            }
            using (var server = new OneShotServer(200, "OK", "application/json", gz, "Content-Encoding: gzip\r\n"))
            {
                Assert.Equal(json, Get(server.Url));
            }
        }

        [Fact]
        public void error_status_with_json_body_returns_body()
        {
            const string json = "  {\"error\":\"invalid_grant\",\"error_description\":\"Bad Request\"}";
            using (var server = new OneShotServer(400, "Bad Request", "application/json", Encoding.UTF8.GetBytes(json)))
            {
                Assert.Equal(json, Get(server.Url));
            }
        }

        [Fact]
        public void error_status_with_html_body_throws_truncated_auth_exception()
        {
            var html = "<html>" + new string('x', 1000) + "</html>";
            using (var server = new OneShotServer(500, "Internal Server Error", "text/html", Encoding.UTF8.GetBytes(html)))
            {
                var e = Assert.Throws<AuthException>(() => Get(server.Url));
                Assert.Equal("HTTP 500 Internal Server Error: " + html.Substring(0, 500) + "...", e.Message);
                Assert.Equal(5000, e.errorCode);
            }
        }

        [Fact]
        public void timeout_throws_auth_exception_without_query()
        {
            using (var server = new OneShotServer(200, "OK", "application/json", Encoding.UTF8.GetBytes("{}"), delayMs: 5000))
            {
                var e = Assert.Throws<AuthException>(() => Get(server.Url, new HttpConfig { timeout = 300 }));
                Assert.Equal("HTTP request timeout after 300ms: " + server.Url.Split('?')[0], e.Message);
                Assert.Equal(5000, e.errorCode);
            }
        }

        [Fact]
        public void connection_failure_throws_auth_exception()
        {
            // 取一个空闲端口后立即释放，连接会被拒绝
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            var e = Assert.Throws<AuthException>(() => Get($"http://127.0.0.1:{port}/x?code=1"));
            Assert.StartsWith($"HTTP request failed: http://127.0.0.1:{port}/x: ", e.Message);
            Assert.Equal(5000, e.errorCode);
        }

        [Fact]
        public void http_config_default_timeout_is_3000()
        {
            Assert.Equal(3000, new HttpConfig().timeout);
            Assert.Equal(HttpUtils.DEFAULT_TIMEOUT, new HttpConfig { proxy = new WebProxy("http://127.0.0.1:1") }.timeout);
        }

        [Fact]
        public void web_proxies_with_same_address_share_client()
        {
            // 用本测试独有的地址，避免与其它测试互相影响
            var address = "http://127.0.0.1:" + new Random().Next(20000, 60000);
            var first = DefaultHttpSender.GetClient(new HttpConfig { proxy = new WebProxy(address) });
            int count = DefaultHttpSender.ProxyClientCount;
            var second = DefaultHttpSender.GetClient(new HttpConfig { proxy = new WebProxy(address) });
            Assert.Same(first, second);
            Assert.Equal(count, DefaultHttpSender.ProxyClientCount);

            var other = DefaultHttpSender.GetClient(new HttpConfig { proxy = new WebProxy(address) { BypassProxyOnLocal = true } });
            Assert.NotSame(first, other);
        }
    }
}
