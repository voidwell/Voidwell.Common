using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Voidwell.Common.Configuration;

/// <summary>
/// An <see cref="IApplicationConfigurationStore"/> that keeps values in memory for the life of the process.
/// </summary>
public class InMemoryApplicationConfigurationStore : IApplicationConfigurationStore
{
    private readonly IConfiguration? _configuration;
    private readonly ApplicationConfigurationProvider _provider;
    private readonly Dictionary<string, ApplicationConfigurationEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();

    /// <summary>
    /// Creates the store.
    /// </summary>
    /// <param name="configuration">The application configuration, used to work out which keys are locked.</param>
    /// <param name="provider">
    /// The provider that serves the stored values as configuration, when it has been added to the configuration.
    /// </param>
    public InMemoryApplicationConfigurationStore(IConfiguration? configuration, ApplicationConfigurationProvider? provider = null)
    {
        _configuration = configuration;
        _provider = provider ?? new ApplicationConfigurationProvider();
    }

    public string? GetValue(string key)
    {
        return _provider.GetValue(key);
    }

    public Task<ApplicationConfigurationEntry?> GetEntryAsync(string key, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_entries.GetValueOrDefault(key));
        }
    }

    public Task SetAsync(string key, JsonElement value, string? modifiedBy, CancellationToken cancellationToken = default)
    {
        this.EnsureUnlocked(key);

        lock (_lock)
        {
            _entries[key] = new ApplicationConfigurationEntry(key, value.Clone(), DateTime.UtcNow, modifiedBy);
            Publish();
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, string? modifiedBy, CancellationToken cancellationToken = default)
    {
        this.EnsureUnlocked(key);

        lock (_lock)
        {
            if (_entries.Remove(key))
            {
                Publish();
            }
        }

        return Task.CompletedTask;
    }

    public ConfigurationLock GetLock(string key)
    {
        return _configuration?.GetLock(key) ?? ConfigurationLock.Unlocked;
    }

    private void Publish()
    {
        _provider.Replace(_entries.Values.Select(a => new KeyValuePair<string, JsonElement>(a.Key, a.Value)).ToList());
    }
}
