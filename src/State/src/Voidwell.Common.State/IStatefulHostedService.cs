using Microsoft.Extensions.Hosting;
using Voidwell.Common.Configuration;

namespace Voidwell.Common.State;

/// <summary>
/// A hosted service that can be enabled and disabled while the application runs. Derive from
/// <see cref="StatefulHostedService"/> rather than implementing this directly.
/// </summary>
/// <remarks>
/// <see cref="IHostedService.StartAsync"/> and <see cref="IHostedService.StopAsync"/> are called by the
/// <see cref="IStatefulHostedServiceManager"/> whenever the service is enabled or disabled, not only when the
/// application starts and stops.
/// </remarks>
public interface IStatefulHostedService : IHostedService
{
    /// <summary>
    /// The name the service is managed by, such as in the services API.
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Called once when the application starts, before the service is started for the first time.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task OnApplicationStartup(CancellationToken cancellationToken);

    /// <summary>
    /// Called once when the application stops.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task OnApplicationShutdown(CancellationToken cancellationToken);

    /// <summary>
    /// Gets details about the service to show alongside its state.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The details, or null.</returns>
    Task<object?> GetStatusAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets whether the service's enabled state is locked by appsettings or environment variables.
    /// </summary>
    /// <param name="context">The state context.</param>
    /// <returns>The lock state.</returns>
    ConfigurationLock GetLock(StatefulServiceContext context);

    /// <summary>
    /// Gets whether the service should be running.
    /// </summary>
    /// <param name="context">The state context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True when the service should be running.</returns>
    Task<bool> IsEnabledAsync(StatefulServiceContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a change to whether the service should be running.
    /// </summary>
    /// <param name="context">The state context.</param>
    /// <param name="isEnabled">Whether the service should be running.</param>
    /// <param name="modifiedBy">The <c>sub</c> of the user making the change, if known.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task OnStateChangedAsync(StatefulServiceContext context, bool isEnabled, string? modifiedBy, CancellationToken cancellationToken);
}
