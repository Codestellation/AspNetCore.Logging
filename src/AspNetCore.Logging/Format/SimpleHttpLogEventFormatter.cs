using System.Text;

namespace Codestellation.AspNetCore.Logging.Format
{
    public class SimpleHttpLogEventFormatter : ILogEventFormatter
    {
        public string Format(HttpContextLogEvent logEvent)
        {
            var builder = new StringBuilder(512);

            // {Method} {Scheme}://{Host}{Path}{QueryString} {Protocol} from {RemoteIpAddress}:{RemotePort} at {startedAt:HH:mm:ss.fff} {RequestBodySize} {StatusCode} {ResponseBodySize} in {Elapsed.TotalSeconds}.{Elapsed.Milliseconds:000} seconds");
            builder.Append(logEvent.Method);
            builder.Append(' ');
            builder.Append(logEvent.Scheme);
            builder.Append("://");
            builder.Append(logEvent.Host);
            builder.Append(logEvent.Path);
            builder.Append(logEvent.QueryString);
            builder.Append(' ');
            builder.Append(logEvent.Protocol);
            builder.AppendFormat(" {0} bytes from ", logEvent.RequestBody.Length);
            builder.Append(logEvent.RemoteIpAddress);
            builder.Append(':');
            builder.Append(logEvent.RemotePort);
            builder.Append(" at ");
            builder.AppendFormat("{0:HH:mm:ss.fff} ", logEvent.Timestamp);
            builder.Append(logEvent.StatusCode);
            builder.AppendFormat(" {0} bytes in ", logEvent.ResponseBody.Length);
            builder.Append((int)logEvent.Elapsed.TotalSeconds);
            builder.Append('.');
            builder.AppendFormat("{0:000}", logEvent.Elapsed.Milliseconds);
            builder.AppendLine(" seconds");
            return builder.ToString();
        }
    }
}