# Voidwell.Common.Configuration

[![NuGet](https://img.shields.io/nuget/v/Voidwell.Common.Configuration.svg?style=for-the-badge)](https://www.nuget.org/packages/Voidwell.Common.Configuration/)
[![NuGet](https://img.shields.io/nuget/v/Voidwell.Common.Configuration.EntityFramework.svg?style=for-the-badge&label=nuget%20%28EntityFramework%29)](https://www.nuget.org/packages/Voidwell.Common.Configuration.EntityFramework/)

Configuration helpers, and an application configuration store for settings that can be changed while the application runs.

| Package | Description |
| --- | --- |
| `Voidwell.Common.Configuration` | Configuration helpers, `IApplicationConfigurationStore`, lock detection and an in-memory store. |
| `Voidwell.Common.Configuration.EntityFramework` | Keeps the store in an `application_configuration` table in the application's own `DbContext`. |

## Installation

```shell
dotnet add package Voidwell.Common.Configuration
dotnet add package Voidwell.Common.Configuration.EntityFramework
```

## Application name

```csharp
var applicationName = configuration.GetApplicationName();
```

Returns the configured `AppName`, falling back to the entry assembly name when it isn't set.

## Application configuration store

Stored values are served through `IConfiguration` as the **lowest priority** source:

```
application_configuration table   ← changed at runtime through IApplicationConfigurationStore
appsettings.json / appsettings.{env}.json
environment variables / command line
```

A key that appsettings or an environment variable sets is **locked**: its value always comes from that source, and
`IApplicationConfigurationStore.SetAsync` and `RemoveAsync` throw `ApplicationConfigurationLockedException`. Check a key
with `store.GetLock(key)` or `configuration.GetLock(key)`, which also name the source that locks it.

Keys use configuration syntax, such as `HostedServices:MatchUpdater:Enabled`, and are case-insensitive. Values are
stored as JSON; objects and arrays are flattened into child keys the same way appsettings files are.

```csharp
await store.SetAsync("Feature:Enabled", true, modifiedBy: user.FindFirst("sub")?.Value);
var entry = await store.GetEntryAsync("Feature:Enabled"); // value, last_modified_date, last_modified_user
```

### Entity Framework

Add the table to the application's context, the same way ASP.NET Core Identity's tables are added, then add a
migration to the application (`dotnet ef migrations add AddApplicationConfiguration`):

```csharp
public class MyDbContext : DbContext, IApplicationConfigurationDbContext
{
    public DbSet<ApplicationConfigurationEntity> ApplicationConfiguration => Set<ApplicationConfigurationEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyApplicationConfiguration(); // value column is jsonb; pass null for other databases
    }
}
```

| Column | Type |
| --- | --- |
| `key` | text, primary key |
| `value` | jsonb |
| `last_modified_date` | timestamp, UTC |
| `last_modified_user` | text, the `sub` claim of the user who made the change; null when unknown |

Register it on the application builder. The table is read while the application is built, before dependency injection
exists, so pass the same context configuration the application uses:

```csharp
builder.AddApplicationConfiguration<MyDbContext>(
    options => options.UseNpgsql(connectionString),
    options => options.ReloadInterval = TimeSpan.FromSeconds(30)); // optional, picks up changes from other instances

var app = builder.Build();

app.MigrateDatabase();                  // the application's own migration step
app.ReloadApplicationConfiguration();   // the table didn't exist yet on the first start
```

A missing table or unreachable database is treated as empty rather than stopping the application. Writes use the
application's `IDbContextFactory<TContext>` when one is registered.

### In memory

`builder.AddInMemoryApplicationConfiguration()` registers a store that keeps values for the life of the process and
serves them as configuration, which is useful for tests and local development.

## License

[MIT](../../LICENSE)
