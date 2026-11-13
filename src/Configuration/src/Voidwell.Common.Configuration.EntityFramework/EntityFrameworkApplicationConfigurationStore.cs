using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Voidwell.Common.Configuration.EntityFramework;

/// <summary>
/// An <see cref="IApplicationConfigurationStore"/> that saves values in the application configuration table.
/// </summary>
public class EntityFrameworkApplicationConfigurationStore : IApplicationConfigurationStore
{
    private readonly IConfiguration? _configuration;
    private readonly EntityFrameworkApplicationConfigurationProvider _provider;
    private readonly Func<DbContext> _createDbContext;

    /// <summary>
    /// Creates the store.
    /// </summary>
    /// <param name="configuration">The application configuration, used to work out which keys are locked.</param>
    /// <param name="provider">The provider that serves the table as configuration.</param>
    /// <param name="createDbContext">Creates a context that contains the application configuration table.</param>
    public EntityFrameworkApplicationConfigurationStore(IConfiguration? configuration,
        EntityFrameworkApplicationConfigurationProvider provider, Func<DbContext> createDbContext)
    {
        _configuration = configuration;
        _provider = provider;
        _createDbContext = createDbContext;
    }

    public string? GetValue(string key)
    {
        return _provider.GetValue(key);
    }

    public async Task<ApplicationConfigurationEntry?> GetEntryAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var dbContext = _createDbContext();

        var entity = await FindAsync(dbContext, key, false, cancellationToken);
        if (entity == null)
        {
            return null;
        }

        return new ApplicationConfigurationEntry(entity.Key, ApplicationConfigurationJson.Parse(entity.Value),
            entity.LastModifiedDate, entity.LastModifiedUser);
    }

    public async Task SetAsync(string key, JsonElement value, string? modifiedBy, CancellationToken cancellationToken = default)
    {
        this.EnsureUnlocked(key);

        await using (var dbContext = _createDbContext())
        {
            var entity = await FindAsync(dbContext, key, true, cancellationToken);
            if (entity == null)
            {
                entity = new ApplicationConfigurationEntity { Key = key };
                dbContext.Add(entity);
            }

            entity.Value = value.GetRawText();
            entity.LastModifiedDate = DateTime.UtcNow;
            entity.LastModifiedUser = modifiedBy;

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        _provider.Refresh();
    }

    public async Task RemoveAsync(string key, string? modifiedBy, CancellationToken cancellationToken = default)
    {
        this.EnsureUnlocked(key);

        await using (var dbContext = _createDbContext())
        {
            var entity = await FindAsync(dbContext, key, true, cancellationToken);
            if (entity == null)
            {
                return;
            }

            dbContext.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        _provider.Refresh();
    }

    public ConfigurationLock GetLock(string key)
    {
        return _configuration?.GetLock(key) ?? ConfigurationLock.Unlocked;
    }

    private static Task<ApplicationConfigurationEntity?> FindAsync(DbContext dbContext, string key, bool track,
        CancellationToken cancellationToken)
    {
        // Configuration keys are case-insensitive. Contexts may default to no-tracking, so ask for tracking explicitly.
        var query = dbContext.Set<ApplicationConfigurationEntity>().AsQueryable();
        query = track ? query.AsTracking() : query.AsNoTracking();

        var normalizedKey = key.ToLowerInvariant();
#pragma warning disable CA1304, CA1311, CA1862 // ToLower is translated to SQL; culture overloads are not
        return query.FirstOrDefaultAsync(a => a.Key.ToLower() == normalizedKey, cancellationToken);
#pragma warning restore CA1304, CA1311, CA1862
    }
}
