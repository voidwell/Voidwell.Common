namespace Voidwell.Common.Cache;

/// <summary>
/// A cache of single-value entries plus set-like lists, backed by an in-memory cache and optionally Redis.
/// </summary>
public interface ICache
{
    /// <summary>
    /// Stores a value using the default expiry.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to store.</param>
    /// <returns>A task that completes when the value is stored.</returns>
    Task SetAsync(string key, object value);

    /// <summary>
    /// Stores a value that expires after <paramref name="expires"/>.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="expires">How long the value is cached.</param>
    /// <returns>A task that completes when the value is stored.</returns>
    Task SetAsync(string key, object value, TimeSpan expires);

    /// <summary>
    /// Gets a cached value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached value, or the default of <typeparamref name="T"/> when there is none.</returns>
    Task<T> GetAsync<T>(string key);

    /// <summary>
    /// Gets a cached value and passes it to <paramref name="callback"/> when there is one.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="callback">Receives the cached value.</param>
    /// <returns><c>true</c> if a value was found; otherwise <c>false</c>.</returns>
    Task<bool> TryGetAsync<T>(string key, Action<T> callback);

    /// <summary>
    /// Removes a cached value.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>A task that completes when the value is removed.</returns>
    Task RemoveAsync(string key);

    /// <summary>
    /// Returns the cached value for the key, or runs the factory, caches its result for <paramref name="expires"/> and returns it.
    /// Concurrent callers for the same key share a single factory execution.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Produces the value on a cache miss.</param>
    /// <param name="expires">How long the value is cached.</param>
    /// <param name="token">A token to cancel the operation.</param>
    /// <returns>The cached or newly created value.</returns>
    Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan expires, CancellationToken token = default);

    /// <summary>
    /// Same as <see cref="GetOrSetAsync{T}(string, Func{CancellationToken, Task{T}}, TimeSpan, CancellationToken)"/>, but the factory result is only cached when <paramref name="cacheWhen"/> returns true.
    /// A result that is not cached is still returned to the caller.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Produces the value on a cache miss.</param>
    /// <param name="expires">How long the value is cached.</param>
    /// <param name="cacheWhen">Decides whether the factory result is cached.</param>
    /// <param name="token">A token to cancel the operation.</param>
    /// <returns>The cached or newly created value.</returns>
    Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan expires, Func<T, bool> cacheWhen, CancellationToken token = default);

    /// <summary>
    /// Same as <see cref="GetOrSetAsync{T}(string, Func{CancellationToken, Task{T}}, TimeSpan, CancellationToken)"/>, but a <c>null</c> factory result is returned without being cached.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Produces the value on a cache miss.</param>
    /// <param name="expires">How long the value is cached.</param>
    /// <param name="token">A token to cancel the operation.</param>
    /// <returns>The cached or newly created value, which may be <c>null</c>.</returns>
    Task<T?> GetOrSetIfNotNullAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, TimeSpan expires, CancellationToken token = default);

    /// <summary>
    /// Adds an item to a list. Lists hold distinct items.
    /// </summary>
    /// <param name="key">The list key.</param>
    /// <param name="item">The item to add.</param>
    /// <returns>A task that completes when the item is added.</returns>
    Task AddToListAsync(string key, string item);

    /// <summary>
    /// Removes an item from a list.
    /// </summary>
    /// <param name="key">The list key.</param>
    /// <param name="item">The item to remove.</param>
    /// <returns>A task that completes when the item is removed.</returns>
    Task RemoveFromListAsync(string key, string item);

    /// <summary>
    /// Removes every item from a list.
    /// </summary>
    /// <param name="key">The list key.</param>
    /// <returns>A task that completes when the list is cleared.</returns>
    Task ClearListAsync(string key);

    /// <summary>
    /// Gets the items in a list.
    /// </summary>
    /// <param name="key">The list key.</param>
    /// <returns>The items, or an empty collection when the list is empty or the store is unavailable.</returns>
    Task<IEnumerable<string>> GetListAsync(string key);

    /// <summary>
    /// Gets the items in a list and passes them to <paramref name="callback"/> when the store is available.
    /// </summary>
    /// <param name="key">The list key.</param>
    /// <param name="callback">Receives the items.</param>
    /// <returns><c>true</c> if the store was available; otherwise <c>false</c>.</returns>
    Task<bool> TryGetListAsync(string key, Action<IEnumerable<string>> callback);

    /// <summary>
    /// Gets the number of items in a list.
    /// </summary>
    /// <param name="key">The list key.</param>
    /// <returns>The item count, or 0 when the list is empty or the store is unavailable.</returns>
    Task<long> GetListLengthAsync(string key);

    /// <summary>
    /// Gets the number of items in a list and passes it to <paramref name="callback"/> when the store is available.
    /// </summary>
    /// <param name="key">The list key.</param>
    /// <param name="callback">Receives the item count.</param>
    /// <returns><c>true</c> if the store was available; otherwise <c>false</c>.</returns>
    Task<bool> TryGetListLengthAsync(string key, Action<long> callback);
}
