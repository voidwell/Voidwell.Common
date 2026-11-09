using Microsoft.Extensions.Configuration;

namespace Voidwell.Common.Configuration;

/// <summary>
/// Marks a configuration provider that serves values from an <see cref="IApplicationConfigurationStore"/>.
/// These providers are ignored when working out whether a key is locked.
/// </summary>
public interface IApplicationConfigurationProvider : IConfigurationProvider
{
}
