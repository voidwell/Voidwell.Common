using Microsoft.EntityFrameworkCore;

namespace Voidwell.Common.Configuration.EntityFramework;

/// <summary>
/// Extension methods for adding the application configuration table to a model.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// The default table name.
    /// </summary>
    public const string DefaultTableName = "application_configuration";

    /// <summary>
    /// Adds the application configuration table to the model.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    /// <param name="valueColumnType">
    /// The database type of the value column. Defaults to PostgreSQL's <c>jsonb</c>; pass null to use the provider's
    /// default string type.
    /// </param>
    /// <param name="tableName">The table name.</param>
    /// <returns>The same <paramref name="builder"/>.</returns>
    public static ModelBuilder ApplyApplicationConfiguration(this ModelBuilder builder, string? valueColumnType = "jsonb",
        string tableName = DefaultTableName)
    {
        builder.Entity<ApplicationConfigurationEntity>(entity =>
        {
            entity.ToTable(tableName);

            entity.HasKey(a => a.Key);

            entity.Property(a => a.Key)
                .HasColumnName("key")
                .IsRequired();

            var value = entity.Property(a => a.Value)
                .HasColumnName("value");
            if (valueColumnType != null)
            {
                value.HasColumnType(valueColumnType);
            }

            entity.Property(a => a.LastModifiedDate)
                .HasColumnName("last_modified_date")
                .IsRequired();

            entity.Property(a => a.LastModifiedUser)
                .HasColumnName("last_modified_user");
        });

        return builder;
    }
}
