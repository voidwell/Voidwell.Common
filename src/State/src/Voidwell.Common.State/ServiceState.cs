namespace Voidwell.Common.State;

/// <summary>
/// The state of a stateful hosted service.
/// </summary>
public class ServiceState
{
    /// <summary>
    /// The service name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Whether the service is running.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Whether appsettings or environment variables set the service's state, so it can't be changed at runtime.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// The source that locks the service's state, such as <c>appsettings.json</c> or <c>environment variables</c>.
    /// </summary>
    public string? LockedBy { get; set; }

    /// <summary>
    /// Details from the service.
    /// </summary>
    public object? Details { get; set; }
}
