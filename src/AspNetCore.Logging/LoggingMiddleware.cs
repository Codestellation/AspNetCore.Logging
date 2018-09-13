using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Codestellation.AspNetCore.Logging.Masking;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Codestellation.AspNetCore.Logging
{
    public class LoggingMiddleware
    {
        private static readonly Encoding Encoding = Encoding.UTF8;

        private const int PoolSize = 8;

        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly ObjectPool<StringBuilder> _stringBuilderPool;
        private readonly ObjectPool<MemoryStream> _streamPool;

        public LoggingMiddleware(RequestDelegate next, ILogger logger)
        {
            _next = next;
            _logger = logger;

            var stringBuilderPolicy = new StringBuilderPooledObjectPolicy(4096);
            _stringBuilderPool = new DefaultObjectPool<StringBuilder>(stringBuilderPolicy, PoolSize);

            var memoryStreamPolicy = new MemoryStreamPooledObjectPolicy(2024);
            _streamPool = new DefaultObjectPool<MemoryStream>(memoryStreamPolicy, 2 * PoolSize); // x2 request+response
        }

        public async Task Invoke(HttpContext context)
        {
            if (!_logger.IsEnabled(LogLevel.Information))
            {
                await _next.Invoke(context).ConfigureAwait(false);
                return;
            }

            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            StringBuilder output = _stringBuilderPool.Get();

            MemoryStream requestBody = _streamPool.Get();
            MemoryStream responseBody = _streamPool.Get();

            Stream originRequestBody = request.Body;
            Stream originResponseBody = response.Body;

            request.Body = Sniff(originRequestBody, requestBody);
            response.Body = Sniff(originResponseBody, responseBody);

            DateTime startedAt = DateTime.Now;
            try
            {
                AppendRequestHeaders(request, output, startedAt); // log request's info before Next.Invoke() called, because HttpRequest may be disposed after

                await _next.Invoke(context).ConfigureAwait(false);
            }
            finally
            {
                DateTime finishedAt = DateTime.Now;
                TimeSpan elapsed = finishedAt - startedAt;

                request.Body = originRequestBody;
                response.Body = originResponseBody;

                AppendBody(requestBody, output);
                AppendResponseHeaders(response, output, elapsed);
                AppendBody(responseBody, output);

                _logger.LogInformation(output.ToString());

                _streamPool.Return(requestBody);
                _streamPool.Return(responseBody);
                _stringBuilderPool.Return(output);
            }
        }

        private static Stream Sniff(Stream master, Stream sink) => new SnifferStream(master, sink);

        private void AppendRequestHeaders(HttpRequest request, StringBuilder output, DateTime startedAt)
        {
            string method = request.Method;
            string uri = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
            string protocol = request.Protocol;

            ConnectionInfo connection = request.HttpContext.Connection;
            string remoteAddress = $"{connection.RemoteIpAddress}:{connection.RemotePort}";

            output.AppendLine($"{method} {uri} {protocol} from {remoteAddress} at {startedAt:HH:mm:ss.fff}");

            AppendHeaders(request.Headers, output);
        }

        private void AppendResponseHeaders(HttpResponse response, StringBuilder output, TimeSpan elapsed)
        {
            output.AppendLine($"{response.StatusCode} in {(long)elapsed.TotalSeconds}.{elapsed.Milliseconds:000} seconds");

            AppendHeaders(response.Headers, output);
        }

        private void AppendHeaders(IHeaderDictionary headers, StringBuilder output)
        {
            foreach (KeyValuePair<string, StringValues> header in headers)
            {
                string name = header.Key;
                string value = header.Value.ToString();

                bool masking = name == HeaderNames.Authorization;
                if (masking)
                {
                    value = Mask(value);
                }

                output.AppendLine($"{name}: {value}");
            }
        }

        private void AppendBody(MemoryStream body, StringBuilder output)
        {
            try
            {
                string bodyText = GetString(body);
                output.AppendLine(bodyText);
            }
            catch (Exception error)
            {
                AppendBodyReadingException(error, output);
            }
        }

        private static string GetString(MemoryStream stream)
        {
            if (!stream.TryGetBuffer(out ArraySegment<byte> segment))
            {
                return string.Empty;
            }

            byte[] buffer = segment.Array;
            if (buffer == null || segment.Count == 0)
            {
                return string.Empty;
            }

            return Encoding.GetString(buffer, segment.Offset, segment.Count);
        }

        private static void AppendBodyReadingException(Exception error, StringBuilder output)
        {
            const string disclaimer = "--- this is not the part of the request or response ---";
            output.AppendLine(disclaimer);
            output.AppendLine("Failed to read the body");
            output.AppendLine(error.ToString());
            output.AppendLine(disclaimer);
        }

        public static string Mask(string value)
        {
            uint mask = FNV1a.Hash(value);
            return $"<FNV-1a:{mask}>";
        }
    }
}