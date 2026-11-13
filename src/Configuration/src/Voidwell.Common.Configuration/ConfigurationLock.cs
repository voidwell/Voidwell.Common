namespace Voidwell.Common.Configuration;

/// <summary>
/// Whether a configuration key is set by a source that overrides the application configuration store, such as
/// appsettings or environment variables. A locked key can't be changed through the store.
/// </summary>
/// <param name="IsLocked">True when another configuration source sets the key.</param>
/// <param name="Source">A readable name for the source that sets the key, such as <c>appsettings.json</c>.</param>
public sealed record ConfigurationLock(bool IsLocked, string? Source)
{
    /// <summary>
    /// A key that no overriding source sets.
    /// </summary>
    public static ConfigurationLock Unlocked { get; } = new(false, null);
}
