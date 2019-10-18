using System;
using System.Net;
using Codestellation.AspNetCore.Logging.IO;
using Microsoft.AspNetCore.Http;

namespace Codestellation.AspNetCore.Logging.Format
{
    public class HttpContextLogEvent
    {
        public DateTime StartedAt { get; set; }
        public string Method { get; set; }
        public string Scheme { get; set; }
        public HostString Host { get; set; }
        public PathString Path { get; set; }
        public QueryString QueryString { get; set; }
        public string Protocol { get; set; }
        public IPAddress RemoteIpAddress { get; set; }
        public int RemotePort { get; set; }
        public IHeaderDictionary RequestHeaders { get; set; }
        
        public PooledMemoryStream RequestBody { get; set; }

        public int StatusCode { get; set; }
        public IHeaderDictionary ResponseHeaders { get; set; }
        public PooledMemoryStream ResponseBody { get; set; }
        public TimeSpan Elapsed { get; set; }
    }
}