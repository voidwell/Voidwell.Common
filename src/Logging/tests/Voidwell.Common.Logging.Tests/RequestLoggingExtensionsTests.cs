using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Voidwell.Common.Logging.Tests;

[Collection("StaticLogger")]
public class RequestLoggingExtensionsTests
{
    [Fact]
    public async Task UseApplicationRequestLogging_LogsClientIpAndUserAgent()
    {
        var sink = new CollectingSink();
        var previous = Log.Logger;

        try
        {
            using var host = await new HostBuilder()
                .ConfigureLogging((context, logging) => logging.AddApplicationLogging(context.HostingEnvironment, context.Configuration))
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .Configure(app =>
                    {
                        // AddApplicationLogging assigns Log.Logger; swap in a collecting logger before the middleware is created.
                        Log.Logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();

                        app.UseForwardedHeaders(new ForwardedHeadersOptions
                        {
                            ForwardedHeaders = ForwardedHeaders.XForwardedFor,
                            RequireHeaderSymmetry = false,
                            ForwardLimit = null,
                        });
                        app.UseApplicationRequestLogging();
                        app.Run(context => context.Response.WriteAsync("ok"));
                    }))
                .StartAsync(TestContext.Current.CancellationToken);

            var client = host.GetTestClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/ps2/test");
            request.Headers.Add("X-Forwarded-For", "203.0.113.7");
            request.Headers.UserAgent.ParseAdd("TestAgent/1.0");

            (await client.SendAsync(request, TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();
        }
        finally
        {
            Log.Logger = previous;
        }

        var logEvent = Assert.Single(sink.Events);
        Assert.Equal("203.0.113.7", Property(logEvent, "ClientIp"));
        Assert.Equal("TestAgent/1.0", Property(logEvent, "UserAgent"));
        Assert.Equal("/ps2/test", Property(logEvent, "RequestPath"));
    }

    private static string? Property(LogEvent logEvent, string name) =>
        logEvent.Properties.TryGetValue(name, out var value) ? ((ScalarValue)value).Value?.ToString() : null;

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
