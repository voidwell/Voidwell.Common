using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Voidwell.Common.Configuration;

/// <summary>
/// Converts stored JSON values into flat configuration keys and values.
/// </summary>
public static class ApplicationConfigurationJson
{
    /// <summary>
    /// Flattens a JSON value the same way appsettings files are read: object properties and array items become child
    /// keys, and scalar values become strings.
    /// </summary>
    /// <param name="key">The key the value is stored under.</param>
    /// <param name="value">The value.</param>
    /// <returns>The configuration keys and values.</returns>
    public static IEnumerable<KeyValuePair<string, string?>> Flatten(string key, JsonElement value)
    {
        var data = new List<KeyValuePair<string, string?>>();
        FlattenInto(data, key, value);
        return data;
    }

    /// <summary>
    /// Parses stored text as JSON. Text that isn't valid JSON is treated as a plain string.
    /// </summary>
    /// <param name="text">The stored text.</param>
    /// <returns>The value as JSON.</returns>
    public static JsonElement Parse(string? text)
    {
        if (text == null)
        {
            return JsonSerializer.SerializeToElement<string?>(null);
        }

        try
        {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonSerializer.SerializeToElement(text);
        }
    }

    private static void FlattenInto(List<KeyValuePair<string, string?>> data, string key, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                var hasProperties = false;
                foreach (var property in value.EnumerateObject())
                {
                    hasProperties = true;
                    FlattenInto(data, ConfigurationPath.Combine(key, property.Name), property.Value);
                }

                if (!hasProperties)
                {
                    data.Add(new(key, null));
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in value.EnumerateArray())
                {
                    FlattenInto(data, ConfigurationPath.Combine(key, index.ToString(System.Globalization.CultureInfo.InvariantCulture)), item);
                    index++;
                }

                if (index == 0)
                {
                    data.Add(new(key, null));
                }
                break;

            case JsonValueKind.String:
                data.Add(new(key, value.GetString()));
                break;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                data.Add(new(key, null));
                break;

            default:
                // Numbers and booleans keep their JSON text, e.g. "true" or "1.5"
                data.Add(new(key, value.GetRawText()));
                break;
        }
    }
}
