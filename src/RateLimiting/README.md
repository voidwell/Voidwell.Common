# Voidwell.Common.RateLimiting

[![NuGet](https://img.shields.io/nuget/v/Voidwell.Common.RateLimiting.svg?style=for-the-badge)](https://www.nuget.org/packages/Voidwell.Common.RateLimiting/)

Per-client and per-address-block rate limiting for APIs, built on the ASP.NET Core rate limiter.

## Installation

```shell
dotnet add package Voidwell.Common.RateLimiting
```

## Usage

```csharp
builder.Services.AddApplicationRateLimiting(options =>
{
    // Each client address
    options.PermitLimit = 120;
    options.Window = TimeSpan.FromMinutes(1);

    // Optional: every client in the same address block, together
    options.BlockPermitLimit = 60;
    options.BlockWindow = TimeSpan.FromMinutes(1);
});

var app = builder.Build();
app.UseForwardedHeaders(/* ... */);
app.UseApplicationRateLimiting();
```

Add `UseApplicationRateLimiting` **after** `UseForwardedHeaders`. Clients are identified by `HttpContext.Connection.RemoteIpAddress`, which is the proxy's address unless forwarded headers have been applied.

## Limits

Two limits can apply, and a request has to be within both. Each is a fixed window, and requests over a limit are rejected with `429 Too Many Requests` and a `Retry-After` header, and are not queued.

| Limit | Counts | Options | Default |
| --- | --- | --- | --- |
| Client | each client on its own: an IPv4 address, or an IPv6 /64 | `PermitLimit`, `Window` | 300 per minute |
| Block | every client in the same address block together | `BlockPermitLimit`, `BlockWindow`, `BlockIPv4PrefixLength`, `BlockIPv6PrefixLength` | off |

The client limit bounds one noisy caller. The block limit is for traffic spread across many addresses in the same network, such as a scraper rotating through a hosting provider's range, where no single address looks excessive. All the addresses in the block share one allowance.

The block is the leading bits of the address: `BlockIPv4PrefixLength` defaults to 16 (so `203.0.x.x` is one block) and `BlockIPv6PrefixLength` to 48. Each must be wider than a single client, so 1 to 31 for IPv4 and 1 to 63 for IPv6.

- The client limit is checked first. A request refused because one client is over its own limit does not use any of the block's allowance, so one noisy client cannot push out the others in its block.
- CORS preflight (`OPTIONS`) requests are not limited or counted.
- Limits are held in memory, per instance. Running several instances multiplies the effective limit.
- A block can contain many real users, for example an ISP or carrier-grade NAT. Set `BlockPermitLimit` well above the legitimate traffic you see from a block, and check it against your logs before enabling it.

## License

[MIT](../../LICENSE)
