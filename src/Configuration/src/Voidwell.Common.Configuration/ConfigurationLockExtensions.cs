using Microsoft.Extensions.Configuration;

namespace Voidwell.Common.Configuration;

/// <summary>
/// Extension methods for working out whether a configuration key is locked.
/// </summary>
public static class ConfigurationLockExtensions
{
    /// <summary>
    /// Gets whether a key is set by a configuration source other than the application configuration store, such as
    /// appsettings or environment variables. A key is also locked when a source sets any value beneath it.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="key">The configuration key.</param>
    /// <returns>The lock state, naming the highest priority source that sets the key.</returns>
    public static ConfigurationLock GetLock(this IConfiguration configuration, string key)
    {
        if (configuration is not IConfigurationRoot root)
        {
            return ConfigurationLock.Unlocked;
        }

        // Providers later in the list win, so report the one whose value is actually in effect
        foreach (var provider in root.Providers.Reverse())
        {
            if (provider is IApplicationConfigurationProvider)
            {
                continue;
            }

            if (provider.TryGet(key, out _) || provider.GetChildKeys([], key).Any())
            {
                return new ConfigurationLock(true, DescribeProvider(provider));
            }
        }

        return ConfigurationLock.Unlocked;
    }

    internal static string DescribeProvider(IConfigurationProvider provider)
    {
        if (provider is FileConfigurationProvider fileProvider && !string.IsNullOrEmpty(fileProvider.Source.Path))
        {
            return Path.GetFileName(fileProvider.Source.Path);
        }

        // Match by name so this package doesn't need a reference to every configuration provider package
        return provider.GetType().Name switch
        {
            "EnvironmentVariablesConfigurationProvider" => "environment variables",
            "CommandLineConfigurationProvider" => "command line arguments",
            "MemoryConfigurationProvider" => "in-memory configuration",
            "ChainedConfigurationProvider" => "host configuration",
            var name when name.EndsWith("ConfigurationProvider", StringComparison.Ordinal) && name.Length > "ConfigurationProvider".Length
                => name[..^"ConfigurationProvider".Length],
            var name => name
        };
    }
}
