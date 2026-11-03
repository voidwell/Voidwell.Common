using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Voidwell.Common.RateLimiting;

/// <summary>
/// Extension methods for limiting how often a client, and optionally a whole address block, can call an API.
/// </summary>
public static class RateLimitingExtensions
{
    private const string _preflightPartition = "preflight";
    private const string _unknownClientPartition = "unknown";

    // A single client is an IPv4 address or an IPv6 /64, since one client can use any address within a /64.
    private const int _clientIPv4PrefixLength = 32;
    private const int _clientIPv6PrefixLength = 64;

    /// <summary>
    /// Registers a global rate limit. Each client IP address is limited on its own and, when
    /// <see cref="RateLimitingOptions.BlockPermitLimit"/> is set, all clients in the same address block are also limited
    /// together. A request has to be within both. Requests over a limit receive <c>429 Too Many Requests</c> with a
    /// <c>Retry-After</c> header. CORS preflight (<c>OPTIONS</c>) requests are not limited.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the rate limits; the defaults are used when omitted.</param>
    /// <returns>The same <paramref name="services"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A configured limit, window or prefix length is out of range.</exception>
    public static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services, Action<RateLimitingOptions>? configure = null)
    {
        var options = new RateLimitingOptions();
        configure?.Invoke(options);

        Validate(options);

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    var seconds = Math.Ceiling(retryAfter.TotalSeconds);
                    context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                }

                return ValueTask.CompletedTask;
            };

#pragma warning disable CA2000 // Ownership of the limiters passes to the rate limiter options
            var clientLimiter = CreateLimiter(options.PermitLimit, options.Window, _clientIPv4PrefixLength, _clientIPv6PrefixLength);

            if (options.BlockPermitLimit is null)
            {
                limiter.GlobalLimiter = clientLimiter;
                return;
            }

            var blockLimiter = CreateLimiter(options.BlockPermitLimit.Value, options.BlockWindow, options.BlockIPv4PrefixLength, options.BlockIPv6PrefixLength);

            // The client limit is checked first, so a request an individual client is refused for does not use up the
            // block's allowance and push out other clients in the same block.
            limiter.GlobalLimiter = PartitionedRateLimiter.CreateChained(clientLimiter, blockLimiter);
#pragma warning restore CA2000
        });

        return services;
    }

    /// <summary>
    /// Enforces the limits registered by <see cref="AddApplicationRateLimiting"/>. Add this after
    /// <c>UseForwardedHeaders</c> so clients are identified by their real address rather than the proxy's.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same <paramref name="app"/>.</returns>
    public static IApplicationBuilder UseApplicationRateLimiting(this IApplicationBuilder app)
    {
        return app.UseRateLimiter();
    }

    private static void Validate(RateLimitingOptions options)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.PermitLimit, nameof(options.PermitLimit));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.Window, TimeSpan.Zero, nameof(options.Window));

        if (options.BlockPermitLimit is null)
        {
            return;
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.BlockPermitLimit.Value, nameof(options.BlockPermitLimit));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.BlockWindow, TimeSpan.Zero, nameof(options.BlockWindow));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.BlockIPv4PrefixLength, 1, nameof(options.BlockIPv4PrefixLength));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(options.BlockIPv4PrefixLength, _clientIPv4PrefixLength, nameof(options.BlockIPv4PrefixLength));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.BlockIPv6PrefixLength, 1, nameof(options.BlockIPv6PrefixLength));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(options.BlockIPv6PrefixLength, _clientIPv6PrefixLength, nameof(options.BlockIPv6PrefixLength));
    }

    private static PartitionedRateLimiter<HttpContext> CreateLimiter(int permitLimit, TimeSpan window, int ipv4PrefixLength, int ipv6PrefixLength)
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            if (HttpMethods.IsOptions(httpContext.Request.Method))
            {
                return RateLimitPartition.GetNoLimiter(_preflightPartition);
            }

            var key = GetPartitionKey(httpContext.Connection.RemoteIpAddress, ipv4PrefixLength, ipv6PrefixLength);

            return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
                AutoReplenishment = true,
            });
        });
    }

    private static string GetPartitionKey(IPAddress? address, int ipv4PrefixLength, int ipv6PrefixLength)
    {
        if (address is null)
        {
            return _unknownClientPartition;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var prefixLength = address.AddressFamily == AddressFamily.InterNetworkV6 ? ipv6PrefixLength : ipv4PrefixLength;

        Span<byte> buffer = stackalloc byte[16];
        address.TryWriteBytes(buffer, out var length);
        var bytes = buffer[..length];

        // Keep the first prefixLength bits and clear the rest, so every address in the block gets the same key
        for (var i = 0; i < bytes.Length; i++)
        {
            var bitsKept = Math.Clamp(prefixLength - (i * 8), 0, 8);
            bytes[i] &= (byte)(0xFF << (8 - bitsKept));
        }

        return $"{new IPAddress(bytes)}/{prefixLength}";
    }
}
