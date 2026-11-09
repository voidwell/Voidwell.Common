using System.Text.Json;

namespace Voidwell.Common.Configuration;

/// <summary>
/// A value saved in an <see cref="IApplicationConfigurationStore"/>, with who changed it last and when.
/// </summary>
/// <param name="Key">The configuration key, such as <c>HostedServices:MatchUpdater:Enabled</c>.</param>
/// <param name="Value">The saved value as JSON.</param>
/// <param name="LastModifiedDate">When the value was last changed, in UTC.</param>
/// <param name="LastModifiedUser">The <c>sub</c> of the user who last changed the value, if known.</param>
public sealed record ApplicationConfigurationEntry(
    string Key,
    JsonElement Value,
    DateTime LastModifiedDate,
    string? LastModifiedUser);
