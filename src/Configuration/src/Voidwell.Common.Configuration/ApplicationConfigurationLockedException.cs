namespace Voidwell.Common.Configuration;

/// <summary>
/// Thrown when a change is made to a configuration key that another configuration source overrides.
/// </summary>
public class ApplicationConfigurationLockedException : InvalidOperationException
{
    public ApplicationConfigurationLockedException()
    {
    }

    public ApplicationConfigurationLockedException(string message)
        : base(message)
    {
    }

    public ApplicationConfigurationLockedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates the exception for a locked key.
    /// </summary>
    /// <param name="key">The locked key.</param>
    /// <param name="source">The source that sets the key.</param>
    public ApplicationConfigurationLockedException(string key, string? source)
        : base($"'{key}' is locked because it is set by {source ?? "another configuration source"}.")
    {
        Key = key;
        LockedBy = source;
    }

    /// <summary>
    /// The locked key.
    /// </summary>
    public string? Key { get; }

    /// <summary>
    /// The source that sets the key.
    /// </summary>
    public string? LockedBy { get; }
}
