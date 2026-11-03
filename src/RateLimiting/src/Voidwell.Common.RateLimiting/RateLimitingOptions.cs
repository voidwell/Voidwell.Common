namespace Voidwell.Common.RateLimiting;

/// <summary>
/// Settings for <see cref="RateLimitingExtensions.AddApplicationRateLimiting"/>.
/// </summary>
/// <remarks>
/// Two limits can apply to a request. The client limit counts each address on its own (IPv4 /32, IPv6 /64). The optional
/// block limit counts every address in a wider range together, which catches traffic spread across many addresses in
/// the same network. A request must be within both.
/// </remarks>
public class RateLimitingOptions
{
    /// <summary>
    /// The number of requests a single client may make in each <see cref="Window"/>. Defaults to 300.
    /// </summary>
    public int PermitLimit { get; set; } = 300;

    /// <summary>
    /// The length of the fixed window the <see cref="PermitLimit"/> applies to. Defaults to one minute.
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The number of requests all clients in the same address block may make together in each <see cref="BlockWindow"/>.
    /// When <see langword="null"/>, the default, there is no block limit.
    /// </summary>
    public int? BlockPermitLimit { get; set; }

    /// <summary>
    /// The length of the fixed window the <see cref="BlockPermitLimit"/> applies to. Defaults to one minute.
    /// </summary>
    public TimeSpan BlockWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How many leading bits of an IPv4 address identify its block. Defaults to 16, so <c>203.0.x.x</c> is one block.
    /// Must be from 1 to 31, so a block is wider than a single client.
    /// </summary>
    public int BlockIPv4PrefixLength { get; set; } = 16;

    /// <summary>
    /// How many leading bits of an IPv6 address identify its block. Defaults to 48. Must be from 1 to 63, so a block is
    /// wider than a single client (a /64).
    /// </summary>
    public int BlockIPv6PrefixLength { get; set; } = 48;
}
