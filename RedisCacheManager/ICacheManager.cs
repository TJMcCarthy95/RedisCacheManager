using System;
using System.Threading.Tasks;

namespace RedisCacheManager
{
    public interface ICacheManager
    {
        /// <summary>
        /// Retrieve an item from the cache by a predefined unique <paramref name="cacheKey"/>.
        /// </summary>
        /// <param name="cacheKey">Unique key used to cache item.</param>
        /// <typeparam name="TItem">The item type in which is being retrieved from the cache.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="cacheKey"/> is invalid.</exception>
        /// <returns>The cached item if exists or null of doesn't exist.</returns>
        Task<TItem?> GetCacheItemAsync<TItem>(CacheKey<TItem> cacheKey)
            where TItem : class;

        /// <summary>
        /// Retrieve an item from the cache by a predefined unique <paramref name="cacheKey"/>.
        /// When null invokes <paramref name="action"/> to retrieve the item and cache the result.
        /// </summary>
        /// <param name="cacheKey">Unique key used to cache item.</param>
        /// <param name="action">A function invoked to retrieve the item a cache result when not found in the cache.</param>
        /// <param name="cacheExpiry">The time for which to cache the result from <paramref name="action"/>.</param>
        /// <typeparam name="TItem">The item type in which is being retrieved from the cache.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="cacheKey"/> is invalid.</exception>
        /// <returns>The cached item if exists or the item returned from <paramref name="action"/>.</returns>
        Task<TItem> GetCacheItemAsync<TItem>(CacheKey<TItem> cacheKey, Func<Task<TItem>> action, TimeSpan cacheExpiry)
            where TItem : class;

        /// <summary>
        /// Cache the provided <paramref name="item"/> under the specific <paramref name="cacheKey"/> for a time defined by <paramref name="cacheExpiry"/>.
        /// </summary>
        /// <param name="cacheKey">The unique key used to cache item.</param>
        /// <param name="item">The object being cached.</param>
        /// <param name="cacheExpiry">The time for which the <paramref name="item"/> will be cached.</param>
        /// <typeparam name="TItem">The item type in which is being cached.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="cacheKey"/> is invalid.</exception>
        Task CacheItemAsync<TItem>(CacheKey<TItem> cacheKey, TItem item, TimeSpan cacheExpiry)
            where TItem : class;

        /// <summary>
        /// Invalidate a cache item by its unique key.
        /// </summary>
        /// <param name="cacheKey">The unique key used to cache item.</param>
        /// <typeparam name="TItem">The item type in which is being invalidated.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="cacheKey"/> is invalid.</exception>
        Task InvalidateCacheItemAsync<TItem>(CacheKey<TItem> cacheKey)
            where TItem : class;

        /// <summary>
        /// Invalidate cache items where like <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key pattern being invalidated.</param>
        Task InvalidateCacheItemsByKeyPatternAsync(string key);
    }
}
