using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Voidwell.Common.Configuration;

namespace Voidwell.Common.State.Tests;

public class StatefulHostedServiceManagerTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Start_NoState_UsesDefaultEnabled()
    {
        using var host = await StartHostAsync();

        Assert.Equal(0, host.Services.GetRequiredService<TestService>().StartCount);
        Assert.Equal(1, host.Services.GetRequiredService<DefaultEnabledService>().StartCount);
    }

    [Fact]
    public async Task Start_ConfiguredState_OverridesDefault()
    {
        using var host = await StartHostAsync(new()
        {
            ["HostedServices:TestService:Enabled"] = "true",
            ["HostedServices:DefaultEnabledService:Enabled"] = "false"
        });

        Assert.Equal(1, host.Services.GetRequiredService<TestService>().StartCount);
        Assert.Equal(0, host.Services.GetRequiredService<DefaultEnabledService>().StartCount);
    }

    [Fact]
    public async Task Start_InvalidValue_UsesDefault()
    {
        using var host = await StartHostAsync(new() { ["HostedServices:DefaultEnabledService:Enabled"] = "maybe" });

        Assert.Equal(1, host.Services.GetRequiredService<DefaultEnabledService>().StartCount);
    }

    [Fact]
    public async Task StartServiceAsync_StartsAndSavesState()
    {
        using var host = await StartHostAsync();
        var manager = host.Services.GetRequiredService<IStatefulHostedServiceManager>();
        var store = host.Services.GetRequiredService<IApplicationConfigurationStore>();

        await manager.StartServiceAsync("testservice", "user-1", Token);

        Assert.Equal(1, host.Services.GetRequiredService<TestService>().StartCount);
        var entry = await store.GetEntryAsync("HostedServices:TestService:Enabled", Token);
        Assert.NotNull(entry);
        Assert.True(entry.Value.GetBoolean());
        Assert.Equal("user-1", entry.LastModifiedUser);

        var status = await manager.GetServiceStatusAsync("TestService", Token);
        Assert.NotNull(status);
        Assert.True(status.IsEnabled);
        Assert.False(status.IsLocked);
        Assert.Equal("details", status.Details);
    }

    [Fact]
    public async Task StopServiceAsync_StopsAndSavesState()
    {
        using var host = await StartHostAsync();
        var manager = host.Services.GetRequiredService<IStatefulHostedServiceManager>();
        var store = host.Services.GetRequiredService<IApplicationConfigurationStore>();

        await manager.StopServiceAsync("DefaultEnabledService", null, Token);

        Assert.Equal(1, host.Services.GetRequiredService<DefaultEnabledService>().StopCount);
        Assert.Equal("false", store.GetValue("HostedServices:DefaultEnabledService:Enabled"));
    }

    [Fact]
    public async Task StartServiceAsync_StartFails_DoesNotSaveState()
    {
        using var host = await StartHostAsync();
        var manager = host.Services.GetRequiredService<IStatefulHostedServiceManager>();
        var store = host.Services.GetRequiredService<IApplicationConfigurationStore>();
        host.Services.GetRequiredService<TestService>().FailStart = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.StartServiceAsync("TestService", null, Token));

        Assert.Null(store.GetValue("HostedServices:TestService:Enabled"));
        var status = await manager.GetServiceStatusAsync("TestService", Token);
        Assert.False(status!.IsEnabled);
    }

    [Fact]
    public async Task StartServiceAsync_Locked_ThrowsAndReportsLock()
    {
        using var host = await StartHostAsync(new() { ["HostedServices:TestService:Enabled"] = "false" });
        var manager = host.Services.GetRequiredService<IStatefulHostedServiceManager>();

        var exception = await Assert.ThrowsAsync<ApplicationConfigurationLockedException>(
            () => manager.StartServiceAsync("TestService", null, Token));

        Assert.Equal("in-memory configuration", exception.LockedBy);
        Assert.Equal(0, host.Services.GetRequiredService<TestService>().StartCount);

        var status = await manager.GetServiceStatusAsync("TestService", Token);
        Assert.True(status!.IsLocked);
        Assert.Equal("in-memory configuration", status.LockedBy);
    }

    [Fact]
    public async Task ConfigurationChange_StartsAndStopsServices()
    {
        var builder = CreateBuilder();
        builder.AddInMemoryApplicationConfiguration();
        using var host = builder.Build();
        await host.StartAsync(Token);

        var store = host.Services.GetRequiredService<IApplicationConfigurationStore>();
        var service = host.Services.GetRequiredService<TestService>();

        // A change written straight to the store, as another instance of the application would
        await store.SetAsync("HostedServices:TestService:Enabled", true, null, Token);
        await WaitForAsync(() => service.StartCount == 1);

        await store.SetAsync("HostedServices:TestService:Enabled", false, null, Token);
        await WaitForAsync(() => service.StopCount == 1);

        await host.StopAsync(Token);
    }

    [Fact]
    public async Task Registration_ByServiceType_IsManaged()
    {
        using var host = await StartHostAsync();
        var manager = host.Services.GetRequiredService<IStatefulHostedServiceManager>();

        Assert.True(manager.VerifyServiceExists("CustomService"));
        Assert.False(manager.VerifyServiceExists("Missing"));
        Assert.Equal(3, (await manager.GetServiceStatusAsync(Token)).Count());
    }

    [Fact]
    public async Task Stop_ShutsDownServices()
    {
        using var host = await StartHostAsync();

        await host.StopAsync(Token);

        Assert.Equal(1, host.Services.GetRequiredService<DefaultEnabledService>().StopCount);
    }

    private static HostApplicationBuilder CreateBuilder(Dictionary<string, string?>? settings = null)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        if (settings != null)
        {
            builder.Configuration.AddInMemoryCollection(settings);
        }

        builder.Services.AddStatefulHostedService<TestService>();
        builder.Services.AddStatefulHostedService<DefaultEnabledService>();
        builder.Services.AddStatefulHostedService<ICustomService, CustomService>();

        return builder;
    }

    private static async Task<IHost> StartHostAsync(Dictionary<string, string?>? settings = null)
    {
        var host = CreateBuilder(settings).Build();
        await host.StartAsync(Token);
        return host;
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
        {
            await Task.Delay(20, Token);
        }

        Assert.True(condition());
    }
}
