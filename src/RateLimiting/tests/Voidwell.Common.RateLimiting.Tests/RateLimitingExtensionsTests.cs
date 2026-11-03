using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Voidwell.Common.RateLimiting.Tests;

public class RateLimitingExtensionsTests
{
    [Fact]
    public async Task RequestsOverTheLimit_AreRejectedWithRetryAfter()
    {
        using var host = await StartHostAsync(options => options.PermitLimit = 2);
        var client = host.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "203.0.113.7")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "203.0.113.7")).StatusCode);

        using var rejected = await SendAsync(client, "203.0.113.7");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.RetryAfter?.Delta > TimeSpan.Zero);
    }

    [Fact]
    public async Task EachClientAddress_HasItsOwnLimit()
    {
        using var host = await StartHostAsync(options => options.PermitLimit = 1);
        var client = host.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "203.0.113.7")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await SendAsync(client, "203.0.113.7")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "203.0.113.8")).StatusCode);
    }

    [Fact]
    public async Task Ipv6Addresses_InTheSamePrefix_ShareALimit()
    {
        using var host = await StartHostAsync(options => options.PermitLimit = 1);
        var client = host.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "2001:db8:1:2::1")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await SendAsync(client, "2001:db8:1:2::ffff")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "2001:db8:1:3::1")).StatusCode);
    }

    [Fact]
    public async Task PreflightRequests_AreNotLimited()
    {
        using var host = await StartHostAsync(options => options.PermitLimit = 1);
        var client = host.GetTestClient();

        for (var i = 0; i < 5; i++)
        {
            using var preflight = await SendAsync(client, "203.0.113.7", HttpMethod.Options);
            Assert.Equal(HttpStatusCode.OK, preflight.StatusCode);
        }

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "203.0.113.7")).StatusCode);
    }

    [Fact]
    public async Task WithoutABlockLimit_ClientsInTheSameBlockAreNotLimitedTogether()
    {
        using var host = await StartHostAsync(options => options.PermitLimit = 1);
        var client = host.GetTestClient();

        for (var i = 1; i <= 5; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, $"198.51.100.{i}")).StatusCode);
        }
    }

    [Fact]
    public async Task ClientsInTheSameBlock_ShareTheBlockLimit()
    {
        using var host = await StartHostAsync(options =>
        {
            options.PermitLimit = 100;
            options.BlockPermitLimit = 3;
        });
        var client = host.GetTestClient();

        // Different addresses, all in 198.51.0.0/16, so each is far below its own limit
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "198.51.1.1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "198.51.2.2")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "198.51.3.3")).StatusCode);

        using var rejected = await SendAsync(client, "198.51.4.4");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.RetryAfter?.Delta > TimeSpan.Zero);
    }

    [Fact]
    public async Task ABlockLimit_DoesNotAffectOtherBlocks()
    {
        using var host = await StartHostAsync(options =>
        {
            options.PermitLimit = 100;
            options.BlockPermitLimit = 1;
        });
        var client = host.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "198.51.1.1")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await SendAsync(client, "198.51.2.2")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "203.0.113.9")).StatusCode);
    }

    [Fact]
    public async Task WithABlockLimit_EachClientIsStillLimitedOnItsOwn()
    {
        using var host = await StartHostAsync(options =>
        {
            options.PermitLimit = 1;
            options.BlockPermitLimit = 100;
        });
        var client = host.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "198.51.1.1")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await SendAsync(client, "198.51.1.1")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "198.51.1.2")).StatusCode);
    }

    [Fact]
    public async Task RequestsRefusedForAClient_DoNotUseUpTheBlockLimit()
    {
        // One noisy client must not use up the allowance of everyone else in its block.
        using var host = await StartHostAsync(options =>
        {
            options.PermitLimit = 1;
            options.BlockPermitLimit = 2;
        });
        var client = host.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "198.51.1.1")).StatusCode);

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(HttpStatusCode.TooManyRequests, (await SendAsync(client, "198.51.1.1")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "198.51.2.2")).StatusCode);
    }

    [Fact]
    public async Task BlockPrefixLength_DecidesWhichClientsShareTheLimit()
    {
        using var host = await StartHostAsync(options =>
        {
            options.PermitLimit = 100;
            options.BlockPermitLimit = 1;
            options.BlockIPv4PrefixLength = 24;
        });
        var client = host.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "198.51.1.1")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await SendAsync(client, "198.51.1.200")).StatusCode);

        // Same /16 as above but a different /24
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "198.51.2.1")).StatusCode);
    }

    [Fact]
    public async Task Ipv6ClientsInTheSameBlock_ShareTheBlockLimit()
    {
        using var host = await StartHostAsync(options =>
        {
            options.PermitLimit = 100;
            options.BlockPermitLimit = 1;
        });
        var client = host.GetTestClient();

        // Different /64s inside the same /48
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "2001:db8:1:1::1")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await SendAsync(client, "2001:db8:1:2::1")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "2001:db8:2:1::1")).StatusCode);
    }

    [Fact]
    public async Task PreflightRequests_AreNotCountedAgainstTheBlockLimit()
    {
        using var host = await StartHostAsync(options =>
        {
            options.PermitLimit = 100;
            options.BlockPermitLimit = 1;
        });
        var client = host.GetTestClient();

        for (var i = 0; i < 5; i++)
        {
            using var preflight = await SendAsync(client, "198.51.1.1", HttpMethod.Options);
            Assert.Equal(HttpStatusCode.OK, preflight.StatusCode);
        }

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(client, "198.51.1.1")).StatusCode);
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(-1, 60)]
    [InlineData(10, 0)]
    [InlineData(10, -5)]
    public void InvalidClientOptions_Throw(int permitLimit, int windowSeconds)
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentOutOfRangeException>(() => services.AddApplicationRateLimiting(options =>
        {
            options.PermitLimit = permitLimit;
            options.Window = TimeSpan.FromSeconds(windowSeconds);
        }));
    }

    [Theory]
    [InlineData(0, 60, 16, 48)]
    [InlineData(-1, 60, 16, 48)]
    [InlineData(10, 0, 16, 48)]
    [InlineData(10, 60, 0, 48)]
    [InlineData(10, 60, 32, 48)]
    [InlineData(10, 60, 16, 0)]
    [InlineData(10, 60, 16, 64)]
    public void InvalidBlockOptions_Throw(int blockPermitLimit, int blockWindowSeconds, int ipv4PrefixLength, int ipv6PrefixLength)
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentOutOfRangeException>(() => services.AddApplicationRateLimiting(options =>
        {
            options.BlockPermitLimit = blockPermitLimit;
            options.BlockWindow = TimeSpan.FromSeconds(blockWindowSeconds);
            options.BlockIPv4PrefixLength = ipv4PrefixLength;
            options.BlockIPv6PrefixLength = ipv6PrefixLength;
        }));
    }

    [Fact]
    public void BlockSettings_AreNotValidated_WhenThereIsNoBlockLimit()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() => services.AddApplicationRateLimiting(options => options.BlockIPv4PrefixLength = 99));

        Assert.Null(exception);
    }

    private static async Task<IHost> StartHostAsync(Action<RateLimitingOptions> configure)
    {
        return await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services => services.AddApplicationRateLimiting(options =>
                {
                    options.Window = TimeSpan.FromMinutes(1);
                    options.BlockWindow = TimeSpan.FromMinutes(1);
                    configure(options);
                }))
                .Configure(app =>
                {
                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor,
                        RequireHeaderSymmetry = false,
                        ForwardLimit = null,
                    });
                    app.UseApplicationRateLimiting();
                    app.Run(context => context.Response.WriteAsync("ok"));
                }))
            .StartAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string clientAddress, HttpMethod? method = null)
    {
        using var request = new HttpRequestMessage(method ?? HttpMethod.Get, "/ps2/test");
        request.Headers.Add("X-Forwarded-For", clientAddress);

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
