using Microsoft.Extensions.Configuration;

namespace Voidwell.Common.Configuration;

/// <summary>
/// A configuration source that returns an existing <see cref="ApplicationConfigurationProvider"/>, so the same
/// instance can be shared with the store that writes to it.
/// </summary>
/// <param name="provider">The provider to return.</param>
public sealed class ApplicationConfigurationSource(ApplicationConfigurationProvider provider) : IConfigurationSource
{
    /// <summary>
    /// The provider this source returns.
    /// </summary>
    public ApplicationConfigurationProvider Provider { get; } = provider;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return Provider;
    }
}
