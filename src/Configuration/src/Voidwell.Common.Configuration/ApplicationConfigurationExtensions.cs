using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Voidwell.Common.Configuration;

/// <summary>
/// Extension methods for adding an application configuration store.
/// </summary>
public static class ApplicationConfigurationExtensions
{
    /// <summary>
    /// Adds a provider as the lowest priority configuration source, so every other source overrides it.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="provider">The provider to add.</param>
    /// <returns>The same <paramref name="builder"/>.</returns>
    public static IConfigurationBuilder AddApplicationConfigurationProvider(this IConfigurationBuilder builder,
        ApplicationConfigurationProvider provider)
    {
        builder.Sources.Insert(0, new ApplicationConfigurationSource(provider));
        return builder;
    }

    /// <summary>
    /// Registers an <see cref="IApplicationConfigurationStore"/> that keeps values in memory for the life of the
    /// process, and serves them as the lowest priority configuration source.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The same <paramref name="builder"/>.</returns>
    public static IHostApplicationBuilder AddInMemoryApplicationConfiguration(this IHostApplicationBuilder builder)
    {
        var provider = new ApplicationConfigurationProvider();
        builder.Configuration.AddApplicationConfigurationProvider(provider);

        builder.Services.RemoveAll<IApplicationConfigurationStore>();
        builder.Services.AddSingleton<IApplicationConfigurationStore>(sp =>
            new InMemoryApplicationConfigurationStore(sp.GetService<IConfiguration>(), provider));

        return builder;
    }

    /// <summary>
    /// Registers an in-memory <see cref="IApplicationConfigurationStore"/> when no other store has been registered.
    /// Its values aren't served as configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/>.</returns>
    public static IServiceCollection TryAddInMemoryApplicationConfigurationStore(this IServiceCollection services)
    {
        services.TryAddSingleton<IApplicationConfigurationStore>(sp =>
            new InMemoryApplicationConfigurationStore(sp.GetService<IConfiguration>()));

        return services;
    }
}
