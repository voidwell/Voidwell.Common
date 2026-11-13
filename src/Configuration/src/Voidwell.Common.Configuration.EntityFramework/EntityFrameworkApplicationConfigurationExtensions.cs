using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Voidwell.Common.Configuration.EntityFramework;

/// <summary>
/// Extension methods for storing application configuration with Entity Framework Core.
/// </summary>
public static class EntityFrameworkApplicationConfigurationExtensions
{
    /// <summary>
    /// Serves the application configuration table as the lowest priority configuration source, so appsettings and
    /// environment variables override it, and registers an <see cref="IApplicationConfigurationStore"/> that writes to
    /// the table.
    /// </summary>
    /// <typeparam name="TContext">
    /// The application's context. It must implement <see cref="IApplicationConfigurationDbContext"/>, call
    /// <see cref="ModelBuilderExtensions.ApplyApplicationConfiguration"/> and have a constructor that takes
    /// <see cref="DbContextOptions{TContext}"/>.
    /// </typeparam>
    /// <param name="builder">The application builder.</param>
    /// <param name="configureDbContext">
    /// Configures the context the same way the application does. The configuration source can't use dependency
    /// injection because it's read before the service provider is built.
    /// </param>
    /// <param name="configureOptions">Configures the store options.</param>
    /// <returns>The same <paramref name="builder"/>.</returns>
    public static IHostApplicationBuilder AddApplicationConfiguration<TContext>(this IHostApplicationBuilder builder,
        Action<DbContextOptionsBuilder<TContext>> configureDbContext,
        Action<EntityFrameworkApplicationConfigurationOptions>? configureOptions = null)
        where TContext : DbContext, IApplicationConfigurationDbContext
    {
        var options = new EntityFrameworkApplicationConfigurationOptions();
        configureOptions?.Invoke(options);

        var dbContextOptionsBuilder = new DbContextOptionsBuilder<TContext>();
        configureDbContext(dbContextOptionsBuilder);
        var dbContextOptions = dbContextOptionsBuilder.Options;

        DbContext CreateDbContext()
        {
            return (TContext)Activator.CreateInstance(typeof(TContext), dbContextOptions)!;
        }

#pragma warning disable CA2000 // The configuration root owns and disposes the provider
        var provider = new EntityFrameworkApplicationConfigurationProvider(CreateDbContext, options.ReloadInterval);
#pragma warning restore CA2000
        builder.Configuration.AddApplicationConfigurationProvider(provider);

        builder.Services.TryAddSingleton(provider);
        builder.Services.RemoveAll<IApplicationConfigurationStore>();
        builder.Services.AddSingleton<IApplicationConfigurationStore>(sp =>
        {
            // Prefer the application's own factory, so writes use its pooling
            var factory = sp.GetService<IDbContextFactory<TContext>>();
            Func<DbContext> createDbContext = factory != null ? () => factory.CreateDbContext() : CreateDbContext;

            return new EntityFrameworkApplicationConfigurationStore(sp.GetService<IConfiguration>(), provider, createDbContext);
        });

        return builder;
    }

    /// <summary>
    /// Re-reads the application configuration table. Call this after migrating the database, because the table is
    /// first read before migrations run.
    /// </summary>
    /// <param name="host">The application.</param>
    /// <returns>The same <paramref name="host"/>.</returns>
    public static IHost ReloadApplicationConfiguration(this IHost host)
    {
        host.Services.ReloadApplicationConfiguration();
        return host;
    }

    /// <summary>
    /// Re-reads the application configuration table. Call this after migrating the database, because the table is
    /// first read before migrations run.
    /// </summary>
    /// <param name="services">The application's service provider.</param>
    /// <returns>True when the table was read.</returns>
    public static bool ReloadApplicationConfiguration(this IServiceProvider services)
    {
        var provider = services.GetService<EntityFrameworkApplicationConfigurationProvider>();
        if (provider == null)
        {
            return false;
        }

        if (provider.Refresh())
        {
            return true;
        }

        var logger = services.GetService<ILoggerFactory>()?.CreateLogger(typeof(EntityFrameworkApplicationConfigurationProvider));
        logger?.LogWarning(provider.LastLoadException, "Failed to read the application configuration table. Stored configuration values are not applied.");

        return false;
    }
}
