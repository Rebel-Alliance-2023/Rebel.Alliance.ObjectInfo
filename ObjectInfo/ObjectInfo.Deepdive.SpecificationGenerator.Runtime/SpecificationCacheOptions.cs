using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching.Configuration
{
    /// <summary>
    /// Configuration options for specification caching
    /// </summary>
    public class SpecificationCacheOptions
    {
        /// <summary>
        /// Gets or sets the default cache duration
        /// </summary>
        public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets whether distributed caching is enabled
        /// </summary>
        public bool EnableDistributedCache { get; set; }

        /// <summary>
        /// Gets or sets the distributed cache connection string
        /// </summary>
        public string? DistributedCacheConnection { get; set; }

        /// <summary>
        /// Gets or sets the sliding expiration for cache entries
        /// </summary>
        public TimeSpan? SlidingExpiration { get; set; }

        /// <summary>
        /// Gets or sets the absolute expiration for cache entries
        /// </summary>
        public DateTimeOffset? AbsoluteExpiration { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items to keep in memory cache
        /// </summary>
        public int MaxMemoryCacheItems { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the memory cache compression level (0-9)
        /// </summary>
        public int CompressionLevel { get; set; } = 3;

        /// <summary>
        /// Gets or sets whether to enable cache statistics
        /// </summary>
        public bool EnableStatistics { get; set; } = true;

        /// <summary>
        /// Gets the memory cache options
        /// </summary>
        public MemoryCacheOptions GetMemoryCacheOptions()
        {
            return new MemoryCacheOptions
            {
                SizeLimit = MaxMemoryCacheItems,
                ExpirationScanFrequency = TimeSpan.FromMinutes(1),
                CompactionPercentage = 0.25
            };
        }
    }
}
