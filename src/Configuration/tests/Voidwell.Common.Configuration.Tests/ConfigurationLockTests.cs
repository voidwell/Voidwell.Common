using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Voidwell.Common.Configuration.Tests;

public sealed class ConfigurationLockTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory().FullName;

    public void Dispose()
    {
        Directory.Delete(_directory, true);
    }

    [Fact]
    public void GetLock_KeyOnlyInStore_IsUnlocked()
    {
        var provider = new ApplicationConfigurationProvider();
        provider.Replace([new("Feature:Enabled", JsonSerializer.SerializeToElement(true))]);

        var configuration = new ConfigurationBuilder()
            .AddApplicationConfigurationProvider(provider)
            .Build();

        Assert.Equal("true", configuration["Feature:Enabled"]);
        Assert.False(configuration.GetLock("Feature:Enabled").IsLocked);
    }

    [Fact]
    public void GetLock_KeyInAppSettings_IsLockedByFile()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(WriteSettings("""{ "Feature": { "Enabled": false } }"""))
            .Build();

        var configurationLock = configuration.GetLock("Feature:Enabled");

        Assert.True(configurationLock.IsLocked);
        Assert.Equal("appsettings.json", configurationLock.Source);
    }

    [Fact]
    public void GetLock_ChildKeyInAppSettings_LocksParent()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(WriteSettings("""{ "Feature": { "Enabled": false } }"""))
            .Build();

        Assert.True(configuration.GetLock("Feature").IsLocked);
        Assert.False(configuration.GetLock("Other").IsLocked);
    }

    [Fact]
    public void GetLock_ReportsHighestPrioritySource()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(WriteSettings("""{ "Feature": { "Enabled": false } }"""))
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Feature:Enabled"] = "true" })
            .Build();

        Assert.Equal("in-memory configuration", configuration.GetLock("Feature:Enabled").Source);
    }

    [Fact]
    public void GetLock_EnvironmentVariable_IsLocked()
    {
        var name = $"VWTEST_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable($"{name}__Enabled", "false");

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            Assert.Equal("environment variables", configuration.GetLock($"{name}:Enabled").Source);
        }
        finally
        {
            Environment.SetEnvironmentVariable($"{name}__Enabled", null);
        }
    }

    [Fact]
    public void AddApplicationConfigurationProvider_HasLowestPriority()
    {
        var provider = new ApplicationConfigurationProvider();
        provider.Replace([new("Feature:Enabled", JsonSerializer.SerializeToElement(true))]);

        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Feature:Enabled"] = "false" });
        builder.AddApplicationConfigurationProvider(provider);
        var configuration = builder.Build();

        Assert.Equal("false", configuration["Feature:Enabled"]);
    }

    private string WriteSettings(string json)
    {
        var path = Path.Combine(_directory, "appsettings.json");
        File.WriteAllText(path, json);
        return path;
    }
}
