using System;
using System.Collections.Generic;
using Come.CollectiveOAuth.Config;
using Come.CollectiveOAuth.Utils;

namespace Come.CollectiveOAuth.Tests.Support
{
    /// <summary>
    /// 记录请求并按顺序回放响应
    /// </summary>
    internal class RecordingHttpSender : IHttpSender
    {
        private readonly Queue<string> responses;

        public List<HttpRequestRecord> requests { get; } = new List<HttpRequestRecord>();

        public RecordingHttpSender(IEnumerable<string> responses)
        {
            this.responses = new Queue<string>(responses);
        }

        public string Send(HttpRequestRecord request, HttpConfig httpConfig)
        {
            requests.Add(request);
            if (responses.Count == 0)
            {
                throw new InvalidOperationException($"No replay response for request #{requests.Count}: {request.method} {request.url}");
            }
            return responses.Dequeue();
        }
    }
}
