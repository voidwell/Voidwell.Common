using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Voidwell.Common.Configuration.Tests;

public class InMemoryApplicationConfigurationStoreTests
{
    [Fact]
    public async Task SetAsync_StoresValueAndEntry()
    {
        var store = new InMemoryApplicationConfigurationStore(new ConfigurationBuilder().Build());

        await store.SetAsync("Feature:Enabled", true, "user-1", TestContext.Current.CancellationToken);

        Assert.Equal("true", store.GetValue("feature:enabled"));
        var entry = await store.GetEntryAsync("Feature:Enabled", TestContext.Current.CancellationToken);
        Assert.NotNull(entry);
        Assert.Equal("user-1", entry.LastModifiedUser);
        Assert.True(entry.Value.GetBoolean());
    }

    [Fact]
    public async Task SetAsync_LockedKey_Throws()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Feature:Enabled"] = "false" })
            .Build();
        var store = new InMemoryApplicationConfigurationStore(configuration);

        var exception = await Assert.ThrowsAsync<ApplicationConfigurationLockedException>(
            () => store.SetAsync("Feature:Enabled", true, null, TestContext.Current.CancellationToken));

        Assert.Equal("Feature:Enabled", exception.Key);
        Assert.Equal("in-memory configuration", exception.LockedBy);
    }

    [Fact]
    public async Task RemoveAsync_RemovesValue()
    {
        var store = new InMemoryApplicationConfigurationStore(null);
        await store.SetAsync("Feature:Enabled", true, null, TestContext.Current.CancellationToken);

        await store.RemoveAsync("Feature:Enabled", null, TestContext.Current.CancellationToken);

        Assert.Null(store.GetValue("Feature:Enabled"));
        Assert.Null(await store.GetEntryAsync("Feature:Enabled", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddInMemoryApplicationConfiguration_ServesValuesAsConfiguration()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddInMemoryApplicationConfiguration();
        using var host = builder.Build();

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var store = host.Services.GetRequiredService<IApplicationConfigurationStore>();

        var reloaded = false;
        configuration.GetReloadToken().RegisterChangeCallback(_ => reloaded = true, null);

        await store.SetAsync("Feature:Enabled", true, null, TestContext.Current.CancellationToken);

        Assert.Equal("true", configuration["Feature:Enabled"]);
        Assert.True(reloaded);
    }
}
