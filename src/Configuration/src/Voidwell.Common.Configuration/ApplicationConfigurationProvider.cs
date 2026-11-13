using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Voidwell.Common.Configuration;

/// <summary>
/// Serves the values of an <see cref="IApplicationConfigurationStore"/> as configuration. It's added as the lowest
/// priority source, so appsettings and environment variables override it.
/// </summary>
public class ApplicationConfigurationProvider : ConfigurationProvider, IApplicationConfigurationProvider
{
    /// <summary>
    /// Replaces the provider's values and signals a configuration reload when anything changed.
    /// </summary>
    /// <param name="values">The stored values by key.</param>
    public void Replace(IEnumerable<KeyValuePair<string, JsonElement>> values)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            foreach (var (flatKey, flatValue) in ApplicationConfigurationJson.Flatten(key, value))
            {
                data[flatKey] = flatValue;
            }
        }

        if (HasSameData(data))
        {
            return;
        }

        Data = data;
        OnReload();
    }

    /// <summary>
    /// Gets a value served by this provider.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The value, or null.</returns>
    public string? GetValue(string key)
    {
        return TryGet(key, out var value) ? value : null;
    }

    private bool HasSameData(Dictionary<string, string?> data)
    {
        var current = Data;
        if (current.Count != data.Count)
        {
            return false;
        }

        foreach (var (key, value) in data)
        {
            if (!current.TryGetValue(key, out var currentValue) || !string.Equals(currentValue, value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
