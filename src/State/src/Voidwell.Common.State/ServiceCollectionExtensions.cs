using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Voidwell.Common.Configuration;

namespace Voidwell.Common.State;

/// <summary>
/// Extension methods for registering stateful hosted services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="IStatefulHostedServiceManager"/>. When no <see cref="IApplicationConfigurationStore"/>
    /// is registered, state changes are kept in memory and are lost when the application restarts. Calling this more
    /// than once has no further effect.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/>.</returns>
    public static IServiceCollection AddStatefulServiceDependencies(this IServiceCollection services)
    {
        if (services.Any(a => a.ServiceType == typeof(StatefulHostedServiceManager)))
        {
            return services;
        }

        services.TryAddInMemoryApplicationConfigurationStore();

        services.AddSingleton(sp => new StatefulHostedServiceManager(
            sp,
            sp.GetServices<StatefulHostedServiceRegistration>(),
            sp.GetService<IConfiguration>() ?? new ConfigurationBuilder().Build(),
            sp.GetRequiredService<IApplicationConfigurationStore>(),
            sp.GetService<ILogger<StatefulHostedServiceManager>>() ?? NullLogger<StatefulHostedServiceManager>.Instance));

        services.AddSingleton<IStatefulHostedServiceManager>(sp => sp.GetRequiredService<StatefulHostedServiceManager>());
        services.AddHostedService(sp => sp.GetRequiredService<StatefulHostedServiceManager>());

        return services;
    }

    /// <summary>
    /// Registers a stateful hosted service as a singleton managed by the <see cref="IStatefulHostedServiceManager"/>.
    /// </summary>
    /// <typeparam name="TImplementation">The service type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/>.</returns>
    public static IServiceCollection AddStatefulHostedService<TImplementation>(this IServiceCollection services)
        where TImplementation : class, IStatefulHostedService
    {
        services.AddStatefulServiceDependencies();

        services.AddSingleton<TImplementation>();
        services.AddSingleton(new StatefulHostedServiceRegistration(typeof(TImplementation)));

        return services;
    }

    /// <summary>
    /// Registers a stateful hosted service as a singleton <typeparamref name="TService"/> managed by the
    /// <see cref="IStatefulHostedServiceManager"/>.
    /// </summary>
    /// <typeparam name="TService">The type the service is resolved by.</typeparam>
    /// <typeparam name="TImplementation">The service type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/>.</returns>
    public static IServiceCollection AddStatefulHostedService<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService, IStatefulHostedService
    {
        services.AddStatefulServiceDependencies();

        services.AddSingleton<TService, TImplementation>();
        services.AddSingleton(new StatefulHostedServiceRegistration(typeof(TService)));

        return services;
    }
}
