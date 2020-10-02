using Microsoft.AspNetCore.Builder;

namespace Codestellation.AspNetCore.Logging
{
    public static class LoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseLoggingMiddleware(this IApplicationBuilder app, string categoryName)
        {
            app.UseMiddleware<LoggingMiddleware>(categoryName);
            return app;
        }
    }
}