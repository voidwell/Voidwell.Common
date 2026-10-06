namespace Voidwell.Common.Cache;

/// <summary>
/// Settings for <see cref="CacheExtensions.AddCache"/>.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// The Redis connection string. When empty, only the in-memory cache is used.
    /// </summary>
    public string? RedisConfiguration { get; set; }

    /// <summary>
    /// Prefix added to every cache key. Defaults to the entry assembly name.
    /// </summary>
    public string? KeyPrefix { get; set; }
}
