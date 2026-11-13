using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Voidwell.Common.Configuration;

namespace Voidwell.Common.State;

/// <summary>
/// What a stateful hosted service needs to read and save its state.
/// </summary>
public sealed class StatefulServiceContext
{
    /// <summary>
    /// Creates the context.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="store">The application configuration store.</param>
    /// <param name="logger">The logger for problems reading state.</param>
    public StatefulServiceContext(IConfiguration configuration, IApplicationConfigurationStore store, ILogger logger)
    {
        Configuration = configuration;
        Store = store;
        Logger = logger;
    }

    /// <summary>
    /// The application configuration.
    /// </summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// The application configuration store.
    /// </summary>
    public IApplicationConfigurationStore Store { get; }

    /// <summary>
    /// The logger for problems reading state.
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Gets whether a key is locked by appsettings or environment variables.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The lock state.</returns>
    public ConfigurationLock GetLock(string key)
    {
        return Store.GetLock(key);
    }

    /// <summary>
    /// Gets the value in effect for a key: the locking source's value when the key is locked, otherwise the store's.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The value, or null when nothing sets the key.</returns>
    public string? GetValue(string key)
    {
        return GetLock(key).IsLocked ? Configuration[key] : Store.GetValue(key);
    }

    /// <summary>
    /// Gets the boolean in effect for a key, or a default when nothing sets it or its value isn't a boolean.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The value to use when nothing sets the key or it can't be read.</param>
    /// <returns>The value.</returns>
    public bool GetBoolean(string key, bool defaultValue)
    {
        var value = GetValue(key);
        if (value == null)
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        Logger.LogWarning("Configuration value '{Value}' for '{Key}' is not a boolean. Using the default of {Default}.",
            value, key, defaultValue);
        return defaultValue;
    }
}
