using Microsoft.Extensions.Caching.Distributed;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace Voidwell.Common.Cache.Tests;

/// <summary>
/// Entry options passed per call replace the FusionCache defaults rather than merging with them,
/// so Cache must derive them from the defaults configured by AddCache.
/// </summary>
public class CacheDefaultOptionsTests
{
    [Fact]
    public async Task SetAsync_HonorsConfiguredDefaults()
    {
        using var fusion = CreateFusion(out var distributed);

        await new Cache(fusion, new MemoryListStore()).SetAsync("key", "value");

        Assert.Equal(0, distributed.Writes);
    }

    [Fact]
    public async Task SetAsync_WithExpiry_HonorsConfiguredDefaults()
    {
        using var fusion = CreateFusion(out var distributed);

        await new Cache(fusion, new MemoryListStore()).SetAsync("key", "value", TimeSpan.FromMinutes(1));

        Assert.Equal(0, distributed.Writes);
    }

    [Fact]
    public async Task GetOrSetAsync_HonorsConfiguredDefaults()
    {
        using var fusion = CreateFusion(out var distributed);

        await new Cache(fusion, new MemoryListStore()).GetOrSetAsync("key", _ => Task.FromResult("value"), TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);

        Assert.Equal(0, distributed.Writes);
    }

    [Fact]
    public async Task GetOrSetAsync_WithCacheWhen_HonorsConfiguredDefaults()
    {
        using var fusion = CreateFusion(out var distributed);

        await new Cache(fusion, new MemoryListStore()).GetOrSetAsync("key", _ => Task.FromResult("value"), TimeSpan.FromMinutes(1), _ => true, TestContext.Current.CancellationToken);

        Assert.Equal(0, distributed.Writes);
    }

    // Skipping distributed writes is an observable stand-in for any other default, such as SkipBackplaneNotifications
    private static FusionCache CreateFusion(out RecordingDistributedCache distributed)
    {
        var options = new FusionCacheOptions();
        options.DefaultEntryOptions.SkipDistributedCacheWrite = true;

        distributed = new RecordingDistributedCache();
        var fusion = new FusionCache(options);
        fusion.SetupDistributedCache(distributed, new FusionCacheSystemTextJsonSerializer());

        return fusion;
    }

    private sealed class RecordingDistributedCache : IDistributedCache
    {
        public int Writes { get; private set; }

        public byte[]? Get(string key) => null;

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult<byte[]?>(null);

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key)
        {
        }

        public Task RemoveAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => Writes++;

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Writes++;
            return Task.CompletedTask;
        }
    }
}
