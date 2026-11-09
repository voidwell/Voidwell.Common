using Voidwell.Common.Configuration;

namespace Voidwell.Common.State;

/// <summary>
/// Starts, stops and reports on stateful hosted services.
/// </summary>
public interface IStatefulHostedServiceManager
{
    /// <summary>
    /// Gets the state of every service.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The states.</returns>
    Task<IEnumerable<ServiceState>> GetServiceStatusAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the state of a service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The state, or null when no service has the name.</returns>
    Task<ServiceState?> GetServiceStatusAsync(string serviceName, CancellationToken cancellationToken);

    /// <summary>
    /// Starts a service and saves that it's enabled.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="modifiedBy">The <c>sub</c> of the user making the change, if known.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ApplicationConfigurationLockedException">The service's state is locked.</exception>
    Task StartServiceAsync(string serviceName, string? modifiedBy, CancellationToken cancellationToken);

    /// <summary>
    /// Stops a service and saves that it's disabled.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="modifiedBy">The <c>sub</c> of the user making the change, if known.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ApplicationConfigurationLockedException">The service's state is locked.</exception>
    Task StopServiceAsync(string serviceName, string? modifiedBy, CancellationToken cancellationToken);

    /// <summary>
    /// Gets whether a service has the name.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <returns>True when a service has the name.</returns>
    bool VerifyServiceExists(string serviceName);
}
