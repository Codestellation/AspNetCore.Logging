using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging.Console;
using NUnit.Framework;

namespace Codestellation.AspNetCore.Logging.Tests
{
    [TestFixture]
    public class LoggingMiddlewareTests
    {
        [Test, Description("See console output")]
        public async Task Smoke()
        {
            // given
            var logger = new ConsoleLogger(nameof(LoggingMiddlewareTests), null, true);

            var builder = new WebHostBuilder()
                .Configure(
                    app =>
                    {
                        app.UseMiddleware<LoggingMiddleware>(logger);
                        app.Run(async context => { await context.Response.WriteAsync("Hello World!").ConfigureAwait(false); });
                    });

            var server = new TestServer(builder);
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Add("X-MyHeader", "myValue");

            // when
            var response = await client.GetAsync("/path?query=abc").ConfigureAwait(false);

            // than
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Assert.That(data, Is.EqualTo("Hello World!"));
        }
    }
}