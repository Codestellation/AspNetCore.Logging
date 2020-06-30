using System;
using System.Collections.Generic;
using System.Text;
using Codestellation.AspNetCore.Logging.IO;
using Codestellation.AspNetCore.Logging.Masking;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Codestellation.AspNetCore.Logging.Format
{
    public class FullLogEventFormatter : ILogEventFormatter
    {
        public string Format(HttpContextLogEvent logEvent)
        {
            var builder = new StringBuilder(512);
            FormatRequest(builder, logEvent);
            FormatResponse(builder, logEvent);
            return builder.ToString();
        }

        private static void FormatRequest(StringBuilder builder, HttpContextLogEvent logEvent)
        {
            // {Method} {Scheme}://{Host}{Path}{QueryString} {Protocol} from {RemoteIpAddress}:{RemotePort} at {startedAt:HH:mm:ss.fff}
            builder.Append(logEvent.Method);
            builder.Append(' ');
            builder.Append(logEvent.Scheme);
            builder.Append("://");
            builder.Append(logEvent.Host);
            builder.Append(logEvent.Path);
            builder.Append(logEvent.QueryString);
            builder.Append(' ');
            builder.Append(logEvent.Protocol);
            builder.Append(" from ");
            builder.Append(logEvent.RemoteIpAddress);
            builder.Append(':');
            builder.Append(logEvent.RemotePort);
            builder.Append(" at ");
            builder.AppendFormat("{0:HH:mm:ss.fff}", logEvent.Timestamp);
            builder.AppendLine();
            AppendHeaders(builder, logEvent.RequestHeaders);
            AppendBody(builder, logEvent.RequestBody as PooledMemoryStream);
        }

        private static void FormatResponse(StringBuilder builder, HttpContextLogEvent logEvent)
        {
            // {StatusCode} in {Elapsed.TotalSeconds}.{Elapsed.Milliseconds:000} seconds");
            builder.Append(logEvent.StatusCode);
            builder.Append(" in ");
            builder.Append((int)logEvent.Elapsed.TotalSeconds);
            builder.Append('.');
            builder.AppendFormat("{0:000}", logEvent.Elapsed.Milliseconds);
            builder.AppendLine(" seconds");
            AppendHeaders(builder, logEvent.ResponseHeaders);
            AppendBody(builder, logEvent.ResponseBody as PooledMemoryStream);
        }

        private static void AppendHeaders(StringBuilder builder, IHeaderDictionary headers)
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

                builder.Append(name);
                builder.Append(": ");
                builder.AppendLine(value);
            }
        }

        private static void AppendBody(StringBuilder builder, PooledMemoryStream body)
        {
            if (body == null)
            {
                builder.AppendLine("--- Can't read the body. This is not the part of the request or response. ---");
                return;
            }

            try
            {
                string bodyText = body.GetString(Encoding.UTF8);
                builder.AppendLine(bodyText);
            }
            catch
            {
                builder.AppendLine("--- Failed to read the body. This is not the part of the request or response. ---");
            }
        }

        private static string Mask(string value)
        {
            uint mask = FNV1a.Hash(value);
            return $"<FNV-1a:{mask}>";
        }
    }
}