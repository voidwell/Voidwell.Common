namespace Voidwell.Common.Cache.Tests;

public class MemoryListStoreTests
{
    [Fact]
    public async Task AddAsync_AddsDistinctItems()
    {
        var store = new MemoryListStore();

        await store.AddAsync("players", "a");
        await store.AddAsync("players", "b");
        await store.AddAsync("players", "a");

        Assert.Equal(["a", "b"], (await store.GetAsync("players"))!.Order());
        Assert.Equal(2, await store.GetLengthAsync("players"));
    }

    [Fact]
    public async Task RemoveAsync_RemovesItem()
    {
        var store = new MemoryListStore();
        await store.AddAsync("players", "a");
        await store.AddAsync("players", "b");

        await store.RemoveAsync("players", "a");

        Assert.Equal(["b"], await store.GetAsync("players"));
    }

    [Fact]
    public async Task ClearAsync_RemovesAllItemsForKeyOnly()
    {
        var store = new MemoryListStore();
        await store.AddAsync("players", "a");
        await store.AddAsync("other", "b");

        await store.ClearAsync("players");

        Assert.Empty((await store.GetAsync("players"))!);
        Assert.Equal(["b"], await store.GetAsync("other"));
    }

    [Fact]
    public async Task ClearAsync_UnknownKey_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(() => new MemoryListStore().ClearAsync("missing"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetAsync_UnknownKey_ReturnsEmpty()
    {
        var store = new MemoryListStore();

        Assert.Empty((await store.GetAsync("missing"))!);
        Assert.Equal(0, await store.GetLengthAsync("missing"));
    }
}
