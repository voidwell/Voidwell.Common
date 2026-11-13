using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Voidwell.Common.Configuration;

namespace Voidwell.Common.State;

/// <summary>
/// Runs each stateful hosted service when its state says it should be enabled, and starts or stops services when the
/// state changes, whether through the API or a configuration reload.
/// </summary>
public sealed class StatefulHostedServiceManager : IStatefulHostedServiceManager, IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<StatefulHostedServiceRegistration> _registrations;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StatefulHostedServiceManager> _logger;
    private readonly StatefulServiceContext _context;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Lazy<IReadOnlyList<ManagedService>> _services;

    private IDisposable? _changeSubscription;
    private volatile bool _isStopping;

    internal StatefulHostedServiceManager(IServiceProvider serviceProvider, IEnumerable<StatefulHostedServiceRegistration> registrations,
        IConfiguration configuration, IApplicationConfigurationStore store, ILogger<StatefulHostedServiceManager> logger)
    {
        _serviceProvider = serviceProvider;
        _registrations = registrations;
        _configuration = configuration;
        _logger = logger;
        _context = new StatefulServiceContext(configuration, store, logger);

        // Services are resolved on first use so building the manager doesn't build every service
        _services = new Lazy<IReadOnlyList<ManagedService>>(ResolveServices);
    }

    private IReadOnlyList<ManagedService> Services => _services.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var service in Services)
        {
            await service.Service.OnApplicationStartup(cancellationToken);
        }

        await ReconcileAsync(cancellationToken);

        _changeSubscription = ChangeToken.OnChange(_configuration.GetReloadToken, OnConfigurationChanged);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _isStopping = true;
        _changeSubscription?.Dispose();
        _changeSubscription = null;

        await _lock.WaitAsync(cancellationToken);

        try
        {
            foreach (var service in Services)
            {
                try
                {
                    await service.Service.OnApplicationShutdown(cancellationToken);
                }
#pragma warning disable CA1031 // One service failing to shut down must not stop the others
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    _logger.LogError(ex, "Failed to shut down {ServiceName}.", service.Name);
                }

                service.IsRunning = false;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IEnumerable<ServiceState>> GetServiceStatusAsync(CancellationToken cancellationToken)
    {
        var states = new List<ServiceState>();
        foreach (var service in Services)
        {
            states.Add(await GetServiceStatusAsync(service, cancellationToken));
        }

        return states;
    }

    public async Task<ServiceState?> GetServiceStatusAsync(string serviceName, CancellationToken cancellationToken)
    {
        var service = Find(serviceName);
        if (service == null)
        {
            return null;
        }

        return await GetServiceStatusAsync(service, cancellationToken);
    }

    public Task StartServiceAsync(string serviceName, string? modifiedBy, CancellationToken cancellationToken)
    {
        return ChangeServiceStateAsync(serviceName, true, modifiedBy, cancellationToken);
    }

    public Task StopServiceAsync(string serviceName, string? modifiedBy, CancellationToken cancellationToken)
    {
        return ChangeServiceStateAsync(serviceName, false, modifiedBy, cancellationToken);
    }

    public bool VerifyServiceExists(string serviceName)
    {
        return Find(serviceName) != null;
    }

    public void Dispose()
    {
        _changeSubscription?.Dispose();
        _lock.Dispose();
    }

    private async Task ChangeServiceStateAsync(string serviceName, bool isEnabled, string? modifiedBy, CancellationToken cancellationToken)
    {
        var service = Find(serviceName);
        if (service == null)
        {
            return;
        }

        var configurationLock = service.Service.GetLock(_context);
        if (configurationLock.IsLocked)
        {
            throw new ApplicationConfigurationLockedException(service.Name, configurationLock.Source);
        }

        await _lock.WaitAsync(cancellationToken);

        try
        {
            var wasRunning = service.IsRunning;

            // Change the running state first so a service that fails to start isn't saved as enabled
            await SetRunningAsync(service, isEnabled, cancellationToken);

            try
            {
                await service.Service.OnStateChangedAsync(_context, isEnabled, modifiedBy, cancellationToken);
            }
            catch
            {
                // The change wasn't saved, so put the service back how it was rather than leave it out of step
                await SetRunningAsync(service, wasRunning, CancellationToken.None);
                throw;
            }

            _logger.LogInformation("{ServiceName} was {State} by {ModifiedBy}.", service.Name,
                isEnabled ? "enabled" : "disabled", modifiedBy ?? "an unknown user");
        }
        finally
        {
            _lock.Release();
        }
    }

    private void OnConfigurationChanged()
    {
        if (_isStopping)
        {
            return;
        }

        _ = ReconcileFromConfigurationChangeAsync();
    }

    private async Task ReconcileFromConfigurationChangeAsync()
    {
        try
        {
            await ReconcileAsync(CancellationToken.None);
        }
#pragma warning disable CA1031 // Nothing awaits a reload callback, so errors are logged instead
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogError(ex, "Failed to apply a configuration change to stateful hosted services.");
        }
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            foreach (var service in Services)
            {
                if (_isStopping)
                {
                    return;
                }

                try
                {
                    var isEnabled = await service.Service.IsEnabledAsync(_context, cancellationToken);
                    if (isEnabled != service.IsRunning)
                    {
                        _logger.LogInformation("{State} {ServiceName} to match its configured state.",
                            isEnabled ? "Starting" : "Stopping", service.Name);

                        await SetRunningAsync(service, isEnabled, cancellationToken);
                    }
                }
#pragma warning disable CA1031 // One service failing must not stop the others starting
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    _logger.LogError(ex, "Failed to apply the configured state of {ServiceName}.", service.Name);
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private static async Task SetRunningAsync(ManagedService service, bool isRunning, CancellationToken cancellationToken)
    {
        if (service.IsRunning == isRunning)
        {
            return;
        }

        if (isRunning)
        {
            await service.Service.StartAsync(cancellationToken);
        }
        else
        {
            await service.Service.StopAsync(cancellationToken);
        }

        service.IsRunning = isRunning;
    }

    private async Task<ServiceState> GetServiceStatusAsync(ManagedService service, CancellationToken cancellationToken)
    {
        var configurationLock = service.Service.GetLock(_context);

        return new ServiceState
        {
            Name = service.Name,
            IsEnabled = service.IsRunning,
            IsLocked = configurationLock.IsLocked,
            LockedBy = configurationLock.Source,
            Details = await service.Service.GetStatusAsync(cancellationToken)
        };
    }

    private ManagedService? Find(string serviceName)
    {
        return Services.FirstOrDefault(a => string.Equals(a.Name, serviceName, StringComparison.OrdinalIgnoreCase));
    }

    private List<ManagedService> ResolveServices()
    {
        return _registrations
            .Select(a => (IStatefulHostedService)_serviceProvider.GetRequiredService(a.ServiceType))
            .Select(a => new ManagedService(a))
            .ToList();
    }

    private sealed class ManagedService(IStatefulHostedService service)
    {
        public IStatefulHostedService Service { get; } = service;

        public string Name { get; } = service.ServiceName;

        public bool IsRunning { get; set; }
    }
}
