using System;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching
{
    /// <summary>
    /// Defines the contract for specification result caching
    /// </summary>
    public interface ISpecificationCache
    {
        /// <summary>
        /// Gets a value from cache or creates and stores it if it doesn't exist
        /// </summary>
        /// <typeparam name="T">The type of value to cache</typeparam>
        /// <param name="key">The cache key</param>
        /// <param name="factory">Factory function to create the value if not cached</param>
        /// <param name="duration">Optional cache duration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The cached or created value</returns>
        Task<T?> GetOrSetAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? duration = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a value from cache
        /// </summary>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes values from cache matching a pattern
        /// </summary>
        Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines if a key exists in the cache
        /// </summary>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    }
}
