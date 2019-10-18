using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using Codestellation.AspNetCore.Logging.Format;
using Codestellation.AspNetCore.Logging.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Codestellation.AspNetCore.Logging
{
    public class LoggingMiddleware
    {
        private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;
        private const int ChunkSize = 256;
        private static readonly HttpContextLogEventFormatter Formatter = new HttpContextLogEventFormatter();

        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger logger)
        {
            _next = next;
            _logger = logger;
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

            var requestBody = new PooledMemoryStream(BufferPool, ChunkSize);
            var responseBody = new PooledMemoryStream(BufferPool, ChunkSize);

            Stream originRequestBody = request.Body;
            Stream originResponseBody = response.Body;

            request.Body = Sniff(originRequestBody, requestBody);
            response.Body = Sniff(originResponseBody, responseBody);

            DateTime startedAt = DateTime.Now;

            var logEvent = new HttpContextLogEvent
            {
                StartedAt = startedAt,
                RequestBody = requestBody,
                ResponseBody = responseBody
            };

            try
            {
                LogRequest(request, logEvent); // log request's info before Next.Invoke() called, because HttpRequest may be disposed after

                await _next.Invoke(context).ConfigureAwait(false);
            }
            finally
            {
                DateTime finishedAt = DateTime.Now;
                logEvent.Elapsed = finishedAt - startedAt;

                request.Body = originRequestBody;
                response.Body = originResponseBody;

                LogResponse(response, logEvent);

                string message = Formatter.Format(logEvent);
                _logger.LogInformation(message);

                requestBody.Dispose();
                responseBody.Dispose();
            }
        }

        private static Stream Sniff(Stream master, Stream sink) => new SnifferStream(master, sink);

        private static void LogRequest(HttpRequest request, HttpContextLogEvent logEvent)
        {
            logEvent.Method = request.Method;
            logEvent.Scheme = request.Scheme;
            logEvent.Host = request.Host;
            logEvent.Path = request.Path;
            logEvent.QueryString = request.QueryString;
            logEvent.Protocol = request.Protocol;
            logEvent.RemoteIpAddress = request.HttpContext.Connection.RemoteIpAddress;
            logEvent.RemotePort = request.HttpContext.Connection.RemotePort;
            logEvent.RequestHeaders = request.Headers;
        }

        private static void LogResponse(HttpResponse response, HttpContextLogEvent logEvent)
        {
            logEvent.StatusCode = response.StatusCode;
            logEvent.ResponseHeaders = response.Headers;
        }
    }
}