using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Codestellation.AspNetCore.Logging
{
    public static class LoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseLoggingMiddleware(this IApplicationBuilder app, string categoryName, Predicate<HttpContext> shouldLog = null)
            => app.UseMiddleware<LoggingMiddleware>(categoryName, new PredicateContainer(shouldLog));
    }
}