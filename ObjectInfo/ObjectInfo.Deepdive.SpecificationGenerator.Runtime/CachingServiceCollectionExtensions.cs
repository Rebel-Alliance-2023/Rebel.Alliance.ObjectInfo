using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching.Configuration;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching.Implementation;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching
{
    public static class CachingServiceCollectionExtensions
    {
        public static IServiceCollection AddSpecificationCaching(
            this IServiceCollection services,
            Action<SpecificationCacheOptions>? configureOptions = null)
        {
            services.TryAddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();
            services.TryAddSingleton<ICacheStatistics, DefaultCacheStatistics>();

            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            services.TryAddSingleton<IMemoryCache>(sp =>
            {
                var options = sp.GetRequiredService<SpecificationCacheOptions>();
                return new MemoryCache(options.GetMemoryCacheOptions());
            });

            services.TryAddSingleton<ISpecificationCache, SpecificationCache>();

            return services;
        }

        public static IServiceCollection AddDistributedSpecificationCaching(
            this IServiceCollection services,
            Action<SpecificationCacheOptions> configureOptions,
            Action<RedisCacheOptions>? configureRedis = null)
        {
            services.AddSpecificationCaching(configureOptions);

            if (configureRedis != null)
            {
                services.AddStackExchangeRedisCache(configureRedis);
            }

            return services;
        }

        public static IServiceCollection AddCompiledQueryCaching(
            this IServiceCollection services,
            Action<CompiledQueryCacheOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            services.TryAddSingleton<ICompiledQueryCache, CompiledQueryCache>();

            return services;
        }
    }

    internal class DefaultCacheStatistics : ICacheStatistics
    {
        private long _hits;
        private long _distributedHits;
        private long _misses;
        private long _evictions;

        public void RecordHit() => Interlocked.Increment(ref _hits);
        public void RecordDistributedHit() => Interlocked.Increment(ref _distributedHits);
        public void RecordMiss() => Interlocked.Increment(ref _misses);
        public void RecordEviction() => Interlocked.Increment(ref _evictions);

        public (long Hits, long DistributedHits, long Misses, long Evictions) GetStatistics() =>
            (_hits, _distributedHits, _misses, _evictions);
    }
}
