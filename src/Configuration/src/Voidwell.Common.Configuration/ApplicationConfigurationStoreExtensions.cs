using System.Text.Json;

namespace Voidwell.Common.Configuration;

/// <summary>
/// Extension methods for <see cref="IApplicationConfigurationStore"/>.
/// </summary>
public static class ApplicationConfigurationStoreExtensions
{
    /// <summary>
    /// Serializes a value to JSON and saves it.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="store">The store.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The value.</param>
    /// <param name="modifiedBy">The <c>sub</c> of the user making the change, if known.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ApplicationConfigurationLockedException">The key is locked.</exception>
    public static Task SetAsync<T>(this IApplicationConfigurationStore store, string key, T value, string? modifiedBy,
        CancellationToken cancellationToken = default)
    {
        return store.SetAsync(key, JsonSerializer.SerializeToElement(value), modifiedBy, cancellationToken);
    }

    /// <summary>
    /// Throws when a key is locked.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <param name="key">The configuration key.</param>
    /// <exception cref="ApplicationConfigurationLockedException">The key is locked.</exception>
    public static void EnsureUnlocked(this IApplicationConfigurationStore store, string key)
    {
        var configurationLock = store.GetLock(key);
        if (configurationLock.IsLocked)
        {
            throw new ApplicationConfigurationLockedException(key, configurationLock.Source);
        }
    }
}
