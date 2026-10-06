# Voidwell.Common.Cache

[![NuGet](https://img.shields.io/nuget/v/Voidwell.Common.Cache.svg?style=for-the-badge)](https://www.nuget.org/packages/Voidwell.Common.Cache/)

A FusionCache-based cache with optional Redis support and set-like lists.

## Installation

```shell
dotnet add package Voidwell.Common.Cache
```

## Usage

```csharp
services.AddCache(options =>
{
    options.KeyPrefix = "my-app:";
    options.RedisConfiguration = configuration["Redis"]; // optional; memory only when empty
});
```

Inject `ICache`:

```csharp
var value = await cache.GetOrSetAsync("key", ct => LoadAsync(ct), TimeSpan.FromMinutes(5));

await cache.AddToListAsync("players", "alice");
var players = await cache.GetListAsync("players");
```

It uses an in-memory cache, with Redis as a shared second level and a backplane when `RedisConfiguration` is set. Concurrent callers for the same key share one factory execution. A Redis outage degrades to the in-memory cache instead of failing the call, and list reads return an empty list while Redis is unavailable. [FusionCache](https://github.com/ZiggyCreatures/FusionCache) only stores single values, so lists are kept separately, as Redis sets or in memory.

## License

[MIT](../../LICENSE)
