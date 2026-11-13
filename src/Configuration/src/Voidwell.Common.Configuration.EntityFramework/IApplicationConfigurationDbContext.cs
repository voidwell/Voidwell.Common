using Microsoft.EntityFrameworkCore;

namespace Voidwell.Common.Configuration.EntityFramework;

/// <summary>
/// Implemented by an application's <see cref="DbContext"/> to hold the application configuration table. Call
/// <see cref="ModelBuilderExtensions.ApplyApplicationConfiguration"/> from <c>OnModelCreating</c> and add a migration
/// to the application, the same way ASP.NET Core Identity's tables are added.
/// </summary>
public interface IApplicationConfigurationDbContext
{
    /// <summary>
    /// The application configuration rows.
    /// </summary>
    DbSet<ApplicationConfigurationEntity> ApplicationConfiguration { get; }
}
