using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Codestellation.AspNetCore.Logging.Tests
{
    [TestFixture]
    public class LoggingMiddlewareTests
    {
        [Test]
        [Description("See console output")]
        public async Task Smoke()
        {
            // given
            ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddConsole());
            ILogger logger = loggerFactory.CreateLogger<LoggingMiddlewareTests>();
            IWebHostBuilder builder = new WebHostBuilder()
                .Configure(
                    app =>
                    {
                        app.UseMiddleware<LoggingMiddleware>(logger);
                        app.Run(async context =>
                        {
                            await context.Request.Body.DrainAsync(CancellationToken.None).ConfigureAwait(false);
                            await context.Response.WriteAsync("Hello World!").ConfigureAwait(false);
                        });
                    });

            var server = new TestServer(builder);
            HttpClient client = server.CreateClient();
            client.DefaultRequestHeaders.Add("X-MyHeader", "myValue");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "dXNlcjpwYXNzd29yZAo=");

            // when
            HttpResponseMessage response = await client.PostAsync("/path?query=abc", new StringContent("{\"text\": \"Hello!\"}")).ConfigureAwait(false);

            // than
            response.EnsureSuccessStatusCode();

            string data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Assert.That(data, Is.EqualTo("Hello World!"));
        }
    }
}