# Voidwell.Common.State

[![NuGet](https://img.shields.io/nuget/v/Voidwell.Common.State.svg?style=for-the-badge)](https://www.nuget.org/packages/Voidwell.Common.State/)

Hosted services that can be enabled and disabled while the application runs, with their state kept in the
[application configuration store](../Configuration).

## Installation

```shell
dotnet add package Voidwell.Common.State
```

## Usage

```csharp
public class MatchUpdater : StatefulHostedService
{
    public override Task OnApplicationStartup(CancellationToken cancellationToken) { /* prepare, don't start */ }
    public override Task StartAsync(CancellationToken cancellationToken) { /* start work */ }
    public override Task StopAsync(CancellationToken cancellationToken) { /* stop work */ }
    public override Task<object?> GetStatusAsync(CancellationToken cancellationToken) { /* details for the UI */ }
}

services.AddStatefulHostedService<MatchUpdater>();
services.AddStatefulHostedService<IStoreUpdaterService, StoreUpdaterScheduler>();
```

Inject `IStatefulHostedServiceManager` to report on services and enable or disable them:

```csharp
var states = await manager.GetServiceStatusAsync(ct); // Name, IsEnabled, IsLocked, LockedBy, Details
await manager.StartServiceAsync("MatchUpdater", modifiedBy: User.FindFirst("sub")?.Value, ct);
```

## State

Each service's state is the boolean configuration value `HostedServices:{ServiceName}:Enabled`:

- When nothing sets it, or it isn't a boolean, the service's `DefaultEnabled` is used, which is `false` unless the
  service overrides it.
- Enabling or disabling a service starts or stops it, then saves the new state through the registered
  `IApplicationConfigurationStore`. A service that fails to start isn't saved as enabled.
- When appsettings or an environment variable sets the key, the service is **locked**: `StartServiceAsync` and
  `StopServiceAsync` throw `ApplicationConfigurationLockedException`, and its state reports `IsLocked` and `LockedBy`.
  For example, `HostedServices__MatchUpdater__Enabled=false` keeps a service off in one deployment.
- When configuration reloads, services are started or stopped to match it, so a change saved by another instance of
  the application is applied once that instance's store reloads.
- When no store is registered, state is kept in memory and is lost when the application restarts.

At startup the manager calls every service's `OnApplicationStartup`, then starts the enabled ones. At shutdown it calls
every service's `OnApplicationShutdown`, which stops the service by default.

Override `ServiceName`, `ConfigurationKey`, `DefaultEnabled`, `GetLock`, `IsEnabledAsync` or `OnStateChangedAsync` to
change where a service's state comes from or where changes are saved.

## License

[MIT](../../LICENSE)
