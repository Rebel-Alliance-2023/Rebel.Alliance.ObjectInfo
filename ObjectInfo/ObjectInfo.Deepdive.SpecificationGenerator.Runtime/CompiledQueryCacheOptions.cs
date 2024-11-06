using System;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching.Configuration
{
    /// <summary>
    /// Configuration options for compiled query caching
    /// </summary>
    public class CompiledQueryCacheOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of cached queries
        /// </summary>
        public int MaxCachedQueries { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the query timeout
        /// </summary>
        public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets whether to enable query plan caching
        /// </summary>
        public bool EnableQueryPlanCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum hits before caching a query
        /// </summary>
        public int MinimumHitsForCaching { get; set; } = 2;

        /// <summary>
        /// Gets or sets whether to track query statistics
        /// </summary>
        public bool TrackQueryStatistics { get; set; } = true;

        /// <summary>
        /// Gets or sets the cache cleanup interval
        /// </summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
    }
}
