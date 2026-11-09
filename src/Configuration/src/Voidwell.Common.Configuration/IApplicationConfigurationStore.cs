using System.Text.Json;

namespace Voidwell.Common.Configuration;

/// <summary>
/// A key/value store for configuration that can be changed while the application runs. Its values sit below
/// appsettings and environment variables: a key that either of them sets is locked and can't be changed here.
/// </summary>
public interface IApplicationConfigurationStore
{
    /// <summary>
    /// Gets the stored value of a key as a configuration string, or null when nothing is stored. Objects and arrays
    /// are flattened like other configuration, so <c>Parent:Child</c> reads a nested value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The stored value, or null.</returns>
    string? GetValue(string key);

    /// <summary>
    /// Gets the stored row for a key, with who changed it last and when.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stored entry, or null when nothing is stored.</returns>
    Task<ApplicationConfigurationEntry?> GetEntryAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The value as JSON.</param>
    /// <param name="modifiedBy">The <c>sub</c> of the user making the change, if known.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ApplicationConfigurationLockedException">The key is locked.</exception>
    Task SetAsync(string key, JsonElement value, string? modifiedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a stored value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="modifiedBy">The <c>sub</c> of the user making the change, if known.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ApplicationConfigurationLockedException">The key is locked.</exception>
    Task RemoveAsync(string key, string? modifiedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether a key is locked by a configuration source that overrides this store.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The lock state.</returns>
    ConfigurationLock GetLock(string key);
}
