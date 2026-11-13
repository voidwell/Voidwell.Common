using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Voidwell.Common.Configuration.EntityFramework;

/// <summary>
/// Serves the application configuration table as configuration.
/// </summary>
/// <remarks>
/// The provider is loaded while the application is being built, before migrations run, so it treats a missing table
/// or an unreachable database as empty. Call <see cref="Refresh"/>, or
/// <see cref="EntityFrameworkApplicationConfigurationExtensions.ReloadApplicationConfiguration(Microsoft.Extensions.Hosting.IHost)"/>,
/// once the database has been migrated.
/// </remarks>
public sealed class EntityFrameworkApplicationConfigurationProvider : ApplicationConfigurationProvider, IDisposable
{
    private readonly Func<DbContext> _createDbContext;
    private readonly Lock _refreshLock = new();
    private Timer? _timer;

    /// <summary>
    /// Creates the provider.
    /// </summary>
    /// <param name="createDbContext">Creates a context that contains the application configuration table.</param>
    /// <param name="reloadInterval">How often to re-read the table, or null to read it only when asked.</param>
    public EntityFrameworkApplicationConfigurationProvider(Func<DbContext> createDbContext, TimeSpan? reloadInterval = null)
    {
        _createDbContext = createDbContext;

        if (reloadInterval is { } interval && interval > TimeSpan.Zero)
        {
            _timer = new Timer(_ => Refresh(), null, interval, interval);
        }
    }

    /// <summary>
    /// The error from the last attempt to read the table, or null when it succeeded.
    /// </summary>
    public Exception? LastLoadException { get; private set; }

    public override void Load()
    {
        Refresh();
    }

    /// <summary>
    /// Re-reads the table and signals a configuration reload when anything changed. When the table can't be read, the
    /// previous values are kept and the error is saved in <see cref="LastLoadException"/>.
    /// </summary>
    /// <returns>True when the table was read.</returns>
    public bool Refresh()
    {
        lock (_refreshLock)
        {
            List<ApplicationConfigurationEntity> rows;

            try
            {
                using var dbContext = _createDbContext();
                rows = dbContext.Set<ApplicationConfigurationEntity>().AsNoTracking().ToList();
            }
#pragma warning disable CA1031 // A configuration source must not stop the application starting
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LastLoadException = ex;
                return false;
            }

            LastLoadException = null;
            Replace(rows.Select(a => new KeyValuePair<string, JsonElement>(a.Key, ApplicationConfigurationJson.Parse(a.Value))));
            return true;
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
