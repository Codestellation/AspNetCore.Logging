using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Codestellation.AspNetCore.Logging.Tests
{
    [TestFixture]
    public class LoggingMiddlewareTests
    {
        [Test]
        [Description("See console output")]
        [TestCase(LogLevel.Information)]
        [TestCase(LogLevel.Debug)]
        public async Task Smoke(LogLevel logLevel)
        {
            HttpClient client = SetupHost(logLevel, app => app.UseLoggingMiddleware("Requests"));

            // when
            HttpResponseMessage response = await client.PostAsync("/path?query=abc", new StringContent("{\"text\": \"Hello!\"}")).ConfigureAwait(false);

            // than
            response.EnsureSuccessStatusCode();

            string data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Assert.That(data, Is.EqualTo("Hello World!"));
        }


        [Test]
        [Description("See console output")]
        [TestCase(true)]
        [TestCase(false)]
        public async Task Smoke(bool shouldLog)
        {
            HttpClient client = SetupHost(LogLevel.Information, app => app.UseLoggingMiddleware("Requests", ctx => shouldLog));

            // when
            HttpResponseMessage response = await client.PostAsync("/path?query=abc", new StringContent("{\"text\": \"Hello!\"}")).ConfigureAwait(false);

            // than
            response.EnsureSuccessStatusCode();

            string data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Assert.That(data, Is.EqualTo("Hello World!"));
        }

        private static HttpClient SetupHost(LogLevel logLevel, Action<IApplicationBuilder> setupLogging)
        {
            // given
            IWebHostBuilder builder = new WebHostBuilder()
                .ConfigureServices(s => s
                    .AddLogging(l => l
                        .AddConsole()
                        .SetMinimumLevel(logLevel)))
                .Configure(
                    app =>
                    {
                        setupLogging(app);

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
            return client;
        }
    }
}