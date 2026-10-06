using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Voidwell.Common.Cache.Tests;

/// <summary>
/// Redis failures must never surface to callers; they degrade to "no data".
/// </summary>
public class RedisListStoreTests
{
    private static RedisListStore CreateUnreachableStore(bool abortConnect = true)
    {
        return new RedisListStore(Options.Create(new CacheOptions
        {
            KeyPrefix = "test",
            RedisConfiguration = $"127.0.0.1:1,abortConnect={abortConnect},connectTimeout=300,connectRetry=1"
        }));
    }

    [Fact]
    public async Task AddAsync_RedisUnavailable_DoesNotThrow()
    {
        using var sut = CreateUnreachableStore();

        Assert.Null(await Record.ExceptionAsync(() => sut.AddAsync("list", "a")));
    }

    [Fact]
    public async Task RemoveAsync_RedisUnavailable_DoesNotThrow()
    {
        using var sut = CreateUnreachableStore();

        Assert.Null(await Record.ExceptionAsync(() => sut.RemoveAsync("list", "a")));
    }

    [Fact]
    public async Task ClearAsync_RedisUnavailable_DoesNotThrow()
    {
        using var sut = CreateUnreachableStore();

        Assert.Null(await Record.ExceptionAsync(() => sut.ClearAsync("list")));
    }

    [Fact]
    public async Task GetAsync_RedisUnavailable_ReturnsNull()
    {
        using var sut = CreateUnreachableStore();

        Assert.Null(await sut.GetAsync("list"));
    }

    [Fact]
    public async Task GetLengthAsync_RedisUnavailable_ReturnsNull()
    {
        using var sut = CreateUnreachableStore();

        Assert.Null(await sut.GetLengthAsync("list"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RedisUnavailable_FailsFastAfterTheFirstAttempt(bool abortConnect)
    {
        using var sut = CreateUnreachableStore(abortConnect);

        // The first call discovers the outage and starts the cooldown
        await sut.GetAsync("list");

        var stopwatch = Stopwatch.StartNew();
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => sut.GetAsync("list")));
        stopwatch.Stop();

        Assert.All(results, Assert.Null);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(250), $"Calls during the cooldown took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task RedisUnavailable_WithAbortConnectDisabled_DoesNotWaitForTheCommandTimeout()
    {
        using var sut = CreateUnreachableStore(abortConnect: false);

        var stopwatch = Stopwatch.StartNew();
        await sut.GetAsync("list");
        stopwatch.Stop();

        // The default command timeout is 5s; an unconnected multiplexer should be reported immediately
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"The first call took {stopwatch.ElapsedMilliseconds}ms");
    }
}
