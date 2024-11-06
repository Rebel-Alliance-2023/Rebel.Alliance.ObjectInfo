using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text.Json;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching.Configuration;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching.Implementation
{
    /// <summary>
    /// Implementation of specification caching using memory and distributed caches
    /// </summary>
    public class SpecificationCache : ISpecificationCache
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache? _distributedCache;
        private readonly SpecificationCacheOptions _options;
        private readonly ICacheKeyGenerator _keyGenerator;
        private readonly ICacheStatistics _statistics;

        public SpecificationCache(
            IMemoryCache memoryCache,
            IOptions<SpecificationCacheOptions> options,
            ICacheKeyGenerator keyGenerator,
            ICacheStatistics statistics,
            IDistributedCache? distributedCache = null)
        {
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
            _options = options.Value;
            _keyGenerator = keyGenerator;
            _statistics = statistics;
        }

        public async Task<T?> GetOrSetAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? duration = null,
            CancellationToken cancellationToken = default)
        {
            // Try memory cache first
            if (_memoryCache.TryGetValue(key, out T? cachedValue))
            {
                _statistics.RecordHit();
                return cachedValue;
            }

            // Try distributed cache if enabled
            if (_distributedCache != null)
            {
                var distributedValue = await _distributedCache.GetAsync(key, cancellationToken);
                if (distributedValue != null)
                {
                    var value = DeserializeValue<T>(distributedValue);
                    await SetInMemoryCacheAsync(key, value, duration);
                    _statistics.RecordDistributedHit();
                    return value;
                }
            }

            // Generate value
            _statistics.RecordMiss();
            var newValue = await factory();

            // Set in caches
            await SetInCacheAsync(key, newValue, duration, cancellationToken);

            return newValue;
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _memoryCache.Remove(key);
            if (_distributedCache != null)
            {
                await _distributedCache.RemoveAsync(key, cancellationToken);
            }
            _statistics.RecordEviction();
        }

        public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            // Note: This is a simplified implementation.
            // In a production environment, you would need a more sophisticated
            // way to track and remove cache entries by pattern.
            throw new NotImplementedException(
                "Pattern-based cache removal requires additional infrastructure for key tracking.");
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_memoryCache.TryGetValue(key, out _))
            {
                return true;
            }

            if (_distributedCache != null)
            {
                var value = await _distributedCache.GetAsync(key, cancellationToken);
                return value != null;
            }

            return false;
        }

        private async Task SetInCacheAsync<T>(
            string key,
            T value,
            TimeSpan? duration = null,
            CancellationToken cancellationToken = default)
        {
            await SetInMemoryCacheAsync(key, value, duration);

            if (_distributedCache != null)
            {
                await SetInDistributedCacheAsync(key, value, duration, cancellationToken);
            }
        }

        private async Task SetInMemoryCacheAsync<T>(string key, T value, TimeSpan? duration = null)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = _options.AbsoluteExpiration,
                SlidingExpiration = _options.SlidingExpiration,
                Size = 1
            };

            if (duration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = duration;
            }

            _memoryCache.Set(key, value, options);
        }

        private async Task SetInDistributedCacheAsync<T>(
            string key,
            T value,
            TimeSpan? duration = null,
            CancellationToken cancellationToken = default)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = _options.AbsoluteExpiration,
                SlidingExpiration = _options.SlidingExpiration
            };

            if (duration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = duration;
            }

            var serializedValue = SerializeValue(value);
            await _distributedCache!.SetAsync(key, serializedValue, options, cancellationToken);
        }

        private byte[] SerializeValue<T>(T value)
        {
            return JsonSerializer.SerializeToUtf8Bytes(value);
        }

        private T? DeserializeValue<T>(byte[] value)
        {
            return JsonSerializer.Deserialize<T>(value);
        }
    }

    public interface ICacheStatistics
    {
        void RecordHit();
        void RecordDistributedHit();
        void RecordMiss();
        void RecordEviction();
    }
}
