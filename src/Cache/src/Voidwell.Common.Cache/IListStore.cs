namespace Voidwell.Common.Cache;

/// <summary>
/// Set-like list storage. FusionCache only handles single-value entries, so lists are stored separately.
/// </summary>
public interface IListStore
{
    /// <summary>
    /// Adds an item to a list.
    /// </summary>
    /// <param name="key">The list key.</param>
    /// <param name="item">The item to add.</param>
    /// <returns>A task that completes when the item is added.</returns>
    Task AddAsync(string key, string item);

    /// <summary>
    /// Removes an item from a list.
    /// </summary>
    /// <param name="key">The list key.</param>
    /// <param name="item">The item to remove.</param>
    /// <returns>A task that completes when the item is removed.</returns>
    Task RemoveAsync(string key, string item);

    /// <summary>
    /// Removes every item from a list.
    /// </summary>
    /// <param name="key">The list key.</param>
    /// <returns>A task that completes when the list is cleared.</returns>
    Task ClearAsync(string key);

    /// <summary>
    /// Gets the items in a list.
    /// </summary>
    /// <param name="key">The list key.</param>
    /// <returns>The items, or <c>null</c> when the store is unavailable.</returns>
    Task<IReadOnlyCollection<string>?> GetAsync(string key);

    /// <summary>
    /// Gets the number of items in a list.
    /// </summary>
    /// <param name="key">The list key.</param>
    /// <returns>The item count, or <c>null</c> when the store is unavailable.</returns>
    Task<long?> GetLengthAsync(string key);
}
