namespace Voidwell.Common.Configuration.EntityFramework;

/// <summary>
/// Options for the Entity Framework application configuration store.
/// </summary>
public class EntityFrameworkApplicationConfigurationOptions
{
    /// <summary>
    /// How often to re-read the table, so changes made by other instances of the application are picked up. Null, the
    /// default, reads it only at startup and after this instance changes a value.
    /// </summary>
    public TimeSpan? ReloadInterval { get; set; }
}
