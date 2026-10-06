using ZiggyCreatures.Caching.Fusion;

namespace Voidwell.Common.Cache.Tests;

public class CacheTests : IDisposable
{
    private readonly FusionCache _fusion = new(new FusionCacheOptions());
    private readonly Cache _sut;

    public CacheTests()
    {
        _sut = new Cache(_fusion, new MemoryListStore());
    }

    public void Dispose()
    {
        _fusion.Dispose();
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsValue()
    {
        await _sut.SetAsync("key", "value");

        Assert.Equal("value", await _sut.GetAsync<string>("key"));
    }

    [Fact]
    public async Task SetAsync_WithExpiry_ExpiresEntry()
    {
        await _sut.SetAsync("key", "value", TimeSpan.FromMilliseconds(50));

        await Task.Delay(300, TestContext.Current.CancellationToken);

        Assert.Null(await _sut.GetAsync<string>("key"));
    }

    [Fact]
    public async Task GetAsync_Missing_ReturnsDefault()
    {
        Assert.Null(await _sut.GetAsync<string>("missing"));
        Assert.Equal(0, await _sut.GetAsync<int>("missing"));
    }

    [Fact]
    public async Task TryGetAsync_Hit_InvokesCallbackAndReturnsTrue()
    {
        await _sut.SetAsync("key", 42);
        var received = 0;

        var found = await _sut.TryGetAsync<int>("key", v => received = v);

        Assert.True(found);
        Assert.Equal(42, received);
    }

    [Fact]
    public async Task TryGetAsync_Miss_DoesNotInvokeCallback()
    {
        var invoked = false;

        var found = await _sut.TryGetAsync<int>("missing", _ => invoked = true);

        Assert.False(found);
        Assert.False(invoked);
    }

    [Fact]
    public async Task RemoveAsync_RemovesEntry()
    {
        await _sut.SetAsync("key", "value");

        await _sut.RemoveAsync("key");

        Assert.Null(await _sut.GetAsync<string>("key"));
    }

    [Fact]
    public async Task GetOrSetAsync_CacheWhenFalse_ReturnsValueButDoesNotCacheIt()
    {
        var calls = 0;

        Task<List<int>> Factory(CancellationToken ct)
        {
            calls++;
            return Task.FromResult(new List<int>());
        }

        var token = TestContext.Current.CancellationToken;
        await _sut.GetOrSetAsync("key", Factory, TimeSpan.FromMinutes(1), list => list.Count > 0, token);
        await _sut.GetOrSetAsync("key", Factory, TimeSpan.FromMinutes(1), list => list.Count > 0, token);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task GetOrSetAsync_CacheWhenTrue_CachesValue()
    {
        var calls = 0;

        Task<List<int>> Factory(CancellationToken ct)
        {
            calls++;
            return Task.FromResult(new List<int> { 1 });
        }

        var token = TestContext.Current.CancellationToken;
        await _sut.GetOrSetAsync("key", Factory, TimeSpan.FromMinutes(1), list => list.Count > 0, token);
        var second = await _sut.GetOrSetAsync("key", Factory, TimeSpan.FromMinutes(1), list => list.Count > 0, token);

        Assert.Equal(1, calls);
        Assert.Equal([1], second);
    }

    [Fact]
    public async Task GetOrSetAsync_ConcurrentCallers_ShareOneFactoryExecution()
    {
        var calls = 0;
        var gate = new TaskCompletionSource();

        async Task<string> Factory(CancellationToken ct)
        {
            Interlocked.Increment(ref calls);
            await gate.Task;
            return "value";
        }

        var token = TestContext.Current.CancellationToken;
        var tasks = Enumerable.Range(0, 5).Select(_ => _sut.GetOrSetAsync("key", Factory, TimeSpan.FromMinutes(1), token)).ToList();
        gate.SetResult();

        Assert.All(await Task.WhenAll(tasks), v => Assert.Equal("value", v));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task GetOrSetAsync_FactoryThrows_PropagatesAndDoesNotCache()
    {
        var token = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.GetOrSetAsync<string>("key", _ => throw new InvalidOperationException("boom"), TimeSpan.FromMinutes(1), token));

        Assert.Equal("recovered", await _sut.GetOrSetAsync("key", _ => Task.FromResult("recovered"), TimeSpan.FromMinutes(1), token));
    }

    [Fact]
    public async Task ListMethods_DelegateToListStore()
    {
        var lists = new FakeListStore { Items = ["a", "b"], Length = 2 };
        var sut = new Cache(_fusion, lists);

        await sut.AddToListAsync("list", "a");
        await sut.RemoveFromListAsync("list", "b");

        Assert.Equal([("list", "a")], lists.Added);
        Assert.Equal([("list", "b")], lists.Removed);
        Assert.Equal(["a", "b"], await sut.GetListAsync("list"));
        Assert.Equal(2, await sut.GetListLengthAsync("list"));
    }

    [Fact]
    public async Task ClearListAsync_RemovesAllItems()
    {
        await _sut.AddToListAsync("list", "a");
        await _sut.AddToListAsync("list", "b");

        await _sut.ClearListAsync("list");

        Assert.Empty(await _sut.GetListAsync("list"));
        Assert.Equal(0, await _sut.GetListLengthAsync("list"));
    }

    [Fact]
    public async Task RemoveAsync_DoesNotAffectLists()
    {
        await _sut.AddToListAsync("list", "a");

        await _sut.RemoveAsync("list");

        Assert.Equal(["a"], await _sut.GetListAsync("list"));
    }

    [Fact]
    public async Task GetListAsync_StoreUnavailable_ReturnsEmpty()
    {
        var sut = new Cache(_fusion, new FakeListStore());

        Assert.Empty(await sut.GetListAsync("list"));
        Assert.Equal(0, await sut.GetListLengthAsync("list"));
    }

    [Fact]
    public async Task TryGetListAsync_StoreUnavailable_ReturnsFalse()
    {
        var sut = new Cache(_fusion, new FakeListStore());
        var invoked = false;

        var found = await sut.TryGetListAsync("list", _ => invoked = true);

        Assert.False(found);
        Assert.False(invoked);
    }

    [Fact]
    public async Task TryGetListAsync_Hit_InvokesCallback()
    {
        await _sut.AddToListAsync("list", "a");
        IEnumerable<string>? received = null;

        var found = await _sut.TryGetListAsync("list", items => received = items);

        Assert.True(found);
        Assert.Equal(["a"], received);
    }

    [Fact]
    public async Task TryGetListLengthAsync_Hit_InvokesCallback()
    {
        await _sut.AddToListAsync("list", "a");
        await _sut.AddToListAsync("list", "b");
        long length = -1;

        var found = await _sut.TryGetListLengthAsync("list", l => length = l);

        Assert.True(found);
        Assert.Equal(2, length);
    }

    [Fact]
    public async Task TryGetListLengthAsync_StoreUnavailable_ReturnsFalse()
    {
        var sut = new Cache(_fusion, new FakeListStore());

        Assert.False(await sut.TryGetListLengthAsync("list", _ => { }));
    }

    /// <summary>A list store that records writes and returns canned reads; with no canned data it behaves as unavailable.</summary>
    private sealed class FakeListStore : IListStore
    {
        public IReadOnlyCollection<string>? Items { get; init; }

        public long? Length { get; init; }

        public List<(string Key, string Item)> Added { get; } = [];

        public List<(string Key, string Item)> Removed { get; } = [];

        public Task AddAsync(string key, string item)
        {
            Added.Add((key, item));
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, string item)
        {
            Removed.Add((key, item));
            return Task.CompletedTask;
        }

        public Task ClearAsync(string key) => Task.CompletedTask;

        public Task<IReadOnlyCollection<string>?> GetAsync(string key) => Task.FromResult(Items);

        public Task<long?> GetLengthAsync(string key) => Task.FromResult(Length);
    }
}
