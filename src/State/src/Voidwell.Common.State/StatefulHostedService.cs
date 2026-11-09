using Voidwell.Common.Configuration;

namespace Voidwell.Common.State;

/// <summary>
/// Base class for a hosted service that can be enabled and disabled while the application runs.
/// </summary>
/// <remarks>
/// By default the enabled state is the boolean configuration value at <see cref="ConfigurationKey"/>. Changes are
/// saved through the <see cref="IApplicationConfigurationStore"/>, and appsettings or environment variables that set
/// the key lock it. When nothing sets the key, or its value isn't a boolean, <see cref="DefaultEnabled"/> is used.
/// </remarks>
public abstract class StatefulHostedService : IStatefulHostedService
{
    /// <summary>
    /// The configuration section that holds the enabled state of every stateful hosted service.
    /// </summary>
    public const string ConfigurationSection = "HostedServices";

    /// <summary>
    /// The name the service is managed by. Defaults to the class name. Changing it changes
    /// <see cref="ConfigurationKey"/>, so a saved state would no longer apply.
    /// </summary>
    public virtual string ServiceName => GetType().Name;

    /// <summary>
    /// The configuration key that holds whether the service is enabled, <c>HostedServices:{ServiceName}:Enabled</c>
    /// by default.
    /// </summary>
    public virtual string ConfigurationKey => $"{ConfigurationSection}:{ServiceName}:Enabled";

    /// <summary>
    /// Whether the service runs when nothing sets <see cref="ConfigurationKey"/>. False by default.
    /// </summary>
    public virtual bool DefaultEnabled => false;

    public abstract Task StartAsync(CancellationToken cancellationToken);

    public abstract Task StopAsync(CancellationToken cancellationToken);

    public virtual Task OnApplicationStartup(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnApplicationShutdown(CancellationToken cancellationToken)
    {
        return StopAsync(cancellationToken);
    }

    public virtual Task<object?> GetStatusAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<object?>(null);
    }

    public virtual ConfigurationLock GetLock(StatefulServiceContext context)
    {
        return context.GetLock(ConfigurationKey);
    }

    public virtual Task<bool> IsEnabledAsync(StatefulServiceContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(context.GetBoolean(ConfigurationKey, DefaultEnabled));
    }

    public virtual Task OnStateChangedAsync(StatefulServiceContext context, bool isEnabled, string? modifiedBy,
        CancellationToken cancellationToken)
    {
        return context.Store.SetAsync(ConfigurationKey, isEnabled, modifiedBy, cancellationToken);
    }
}
