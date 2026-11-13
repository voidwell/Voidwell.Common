using Microsoft.EntityFrameworkCore;

namespace Voidwell.Common.Configuration.EntityFramework.Tests;

public class TestDbContext : DbContext, IApplicationConfigurationDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApplicationConfigurationEntity> ApplicationConfiguration => Set<ApplicationConfigurationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQLite has no jsonb type
        modelBuilder.ApplyApplicationConfiguration(valueColumnType: null);
    }
}
