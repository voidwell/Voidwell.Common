namespace Voidwell.Common.Configuration.EntityFramework;

/// <summary>
/// A row in the <c>application_configuration</c> table.
/// </summary>
public class ApplicationConfigurationEntity
{
    /// <summary>
    /// The configuration key, such as <c>HostedServices:MatchUpdater:Enabled</c>.
    /// </summary>
    public string Key { get; set; } = null!;

    /// <summary>
    /// The value as JSON text.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// When the value was last changed, in UTC.
    /// </summary>
    public DateTime LastModifiedDate { get; set; }

    /// <summary>
    /// The <c>sub</c> of the user who last changed the value, if known.
    /// </summary>
    public string? LastModifiedUser { get; set; }
}
