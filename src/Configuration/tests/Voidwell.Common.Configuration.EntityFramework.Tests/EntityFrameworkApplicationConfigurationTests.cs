using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Voidwell.Common.Configuration.EntityFramework.Tests;

public sealed class EntityFrameworkApplicationConfigurationTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public EntityFrameworkApplicationConfigurationTests()
    {
        // An in-memory SQLite database lives as long as its connection stays open
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public void Model_UsesApplicationConfigurationTable()
    {
        using var dbContext = CreateDbContext();
        var entity = dbContext.Model.FindEntityType(typeof(ApplicationConfigurationEntity))!;

        Assert.Equal("application_configuration", entity.GetTableName());
        Assert.Equal(["key", "value", "last_modified_date", "last_modified_user"],
            entity.GetProperties().Select(a => a.GetColumnName()).OrderBy(a => a == "key" ? 0 : a == "value" ? 1 : a == "last_modified_date" ? 2 : 3));
    }

    [Fact]
    public void Load_ServesStoredValuesAsConfiguration()
    {
        CreateTable();
        Seed("Feature:Enabled", "true");
        Seed("Feature:Options", """{ "Size": 3 }""");

        using var host = BuildHost();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        Assert.Equal("true", configuration["Feature:Enabled"]);
        Assert.Equal("3", configuration["Feature:Options:Size"]);
    }

    [Fact]
    public void Load_MissingTable_IsEmptyUntilReloaded()
    {
        using var host = BuildHost();
        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var provider = host.Services.GetRequiredService<EntityFrameworkApplicationConfigurationProvider>();

        Assert.Null(configuration["Feature:Enabled"]);
        Assert.NotNull(provider.LastLoadException);

        CreateTable();
        Seed("Feature:Enabled", "true");
        host.ReloadApplicationConfiguration();

        Assert.Null(provider.LastLoadException);
        Assert.Equal("true", configuration["Feature:Enabled"]);
    }

    [Fact]
    public async Task SetAsync_WritesRowAndReloadsConfiguration()
    {
        CreateTable();
        using var host = BuildHost();
        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var store = host.Services.GetRequiredService<IApplicationConfigurationStore>();

        await store.SetAsync("Feature:Enabled", true, "user-1", TestContext.Current.CancellationToken);

        Assert.Equal("true", configuration["Feature:Enabled"]);
        Assert.Equal("true", store.GetValue("Feature:Enabled"));

        var entry = await store.GetEntryAsync("feature:enabled", TestContext.Current.CancellationToken);
        Assert.NotNull(entry);
        Assert.Equal("user-1", entry.LastModifiedUser);
        Assert.True(entry.Value.GetBoolean());
    }

    [Fact]
    public async Task SetAsync_ExistingKeyWithDifferentCase_UpdatesSameRow()
    {
        CreateTable();
        using var host = BuildHost();
        var store = host.Services.GetRequiredService<IApplicationConfigurationStore>();

        await store.SetAsync("Feature:Enabled", true, "user-1", TestContext.Current.CancellationToken);
        await store.SetAsync("feature:enabled", false, "user-2", TestContext.Current.CancellationToken);

        using var dbContext = CreateDbContext();
        var row = Assert.Single(dbContext.ApplicationConfiguration);
        Assert.Equal("Feature:Enabled", row.Key);
        Assert.Equal("false", row.Value);
        Assert.Equal("user-2", row.LastModifiedUser);
    }

    [Fact]
    public async Task SetAsync_KeySetByOtherSource_IsLocked()
    {
        CreateTable();
        Seed("Feature:Enabled", "true");
        using var host = BuildHost(new Dictionary<string, string?> { ["Feature:Enabled"] = "false" });
        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var store = host.Services.GetRequiredService<IApplicationConfigurationStore>();

        Assert.Equal("false", configuration["Feature:Enabled"]);
        Assert.True(store.GetLock("Feature:Enabled").IsLocked);
        await Assert.ThrowsAsync<ApplicationConfigurationLockedException>(
            () => store.SetAsync("Feature:Enabled", true, null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RemoveAsync_DeletesRow()
    {
        CreateTable();
        Seed("Feature:Enabled", "true");
        using var host = BuildHost();
        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var store = host.Services.GetRequiredService<IApplicationConfigurationStore>();

        await store.RemoveAsync("Feature:Enabled", null, TestContext.Current.CancellationToken);

        Assert.Null(configuration["Feature:Enabled"]);
        using var dbContext = CreateDbContext();
        Assert.Empty(dbContext.ApplicationConfiguration);
    }

    private IHost BuildHost(Dictionary<string, string?>? settings = null)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        if (settings != null)
        {
            builder.Configuration.AddInMemoryCollection(settings);
        }

        builder.AddApplicationConfiguration<TestDbContext>(options => options.UseSqlite(_connection));

        return builder.Build();
    }

    private TestDbContext CreateDbContext()
    {
        return new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options);
    }

    private void CreateTable()
    {
        using var dbContext = CreateDbContext();
        dbContext.Database.EnsureCreated();
    }

    private void Seed(string key, string value)
    {
        using var dbContext = CreateDbContext();
        dbContext.ApplicationConfiguration.Add(new ApplicationConfigurationEntity
        {
            Key = key,
            Value = value,
            LastModifiedDate = DateTime.UtcNow
        });
        dbContext.SaveChanges();
    }
}
