using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Voidwell.Common.Configuration;

/// <summary>
/// Extension methods for <see cref="IConfiguration"/>.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Gets the configured <c>ApplicationName</c>, falling back to the entry assembly name.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The application name.</returns>
    public static string GetApplicationName(this IConfiguration configuration)
    {
        var value = configuration.GetValue<string?>("ApplicationName", null);
        if (string.IsNullOrWhiteSpace(value))
        {
            value = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Name!;
        }
        return value;
    }
}
