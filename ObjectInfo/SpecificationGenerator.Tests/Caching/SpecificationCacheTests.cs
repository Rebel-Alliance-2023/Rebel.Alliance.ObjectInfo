using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using Moq;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching.Configuration;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching.Implementation;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestEntities;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Caching
{
    public class SpecificationCacheTests
    {
        private readonly ITestOutputHelper _output;
        private readonly IMemoryCache _memoryCache;
        private readonly ICacheKeyGenerator _keyGenerator;
        private readonly ICacheStatistics _statistics;
        private readonly SpecificationCacheOptions _options;

        public SpecificationCacheTests(ITestOutputHelper output)
        {
            _output = output;
            
            var services = new ServiceCollection();
            services.AddMemoryCache();
            var serviceProvider = services.BuildServiceProvider();
            
            _memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
            _keyGenerator = new DefaultCacheKeyGenerator();
            _statistics = new Mock<ICacheStatistics>().Object;
            _options = new SpecificationCacheOptions
            {
                DefaultDuration = TimeSpan.FromMinutes(5),
                MaxMemoryCacheItems = 100
            };
        }

        [Fact]
        public async Task GetOrSetAsync_WhenNotCached_InvokesFactory()
        {
            // Arrange
            var cache = CreateCache();
            var factoryExecutions = 0;

            async Task<string> Factory()
            {
                factoryExecutions++;
                return "test value";
            }

            // Act
            var result1 = await cache.GetOrSetAsync("key1", Factory);
            var result2 = await cache.GetOrSetAsync("key1", Factory);

            // Assert
            factoryExecutions.Should().Be(1);
            result1.Should().Be(result2);
        }

        [Fact]
        public async Task GetOrSetAsync_WithDuration_RespectsExpiry()
        {
            // Arrange
            var cache = CreateCache();
            var duration = TimeSpan.FromMilliseconds(50);
            var factoryExecutions = 0;

            async Task<string> Factory()
            {
                factoryExecutions++;
                return "test value";
            }

            // Act
            var result1 = await cache.GetOrSetAsync("key1", Factory, duration);
            await Task.Delay(duration.Add(TimeSpan.FromMilliseconds(10))); // Wait for expiry
            var result2 = await cache.GetOrSetAsync("key1", Factory, duration);

            // Assert
            factoryExecutions.Should().Be(2);
            result1.Should().Be(result2);
        }

        [Fact]
        public async Task RemoveAsync_RemovesItemFromCache()
        {
            // Arrange
            var cache = CreateCache();
            var factoryExecutions = 0;

            async Task<string> Factory()
            {
                factoryExecutions++;
                return "test value";
            }

            // Act
            await cache.GetOrSetAsync("key1", Factory);
            await cache.RemoveAsync("key1");
            await cache.GetOrSetAsync("key1", Factory);

            // Assert
            factoryExecutions.Should().Be(2);
        }

        [Fact]
        public async Task ExistsAsync_ReturnsCorrectState()
        {
            // Arrange
            var cache = CreateCache();

            // Act
            await cache.GetOrSetAsync("key1", async () => "test value");
            var exists1 = await cache.ExistsAsync("key1");
            var exists2 = await cache.ExistsAsync("nonexistent");

            // Assert
            exists1.Should().BeTrue();
            exists2.Should().BeFalse();
        }

        [Fact]
        public async Task GetOrSetAsync_WithFailedFactory_DoesNotCache()
        {
            // Arrange
            var cache = CreateCache();
            var factoryExecutions = 0;

            async Task<string> Factory()
            {
                factoryExecutions++;
                throw new Exception("Factory failed");
            }

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => cache.GetOrSetAsync("key1", Factory));
            await Assert.ThrowsAsync<Exception>(() => cache.GetOrSetAsync("key1", Factory));
            factoryExecutions.Should().Be(2);
        }

        [Fact]
        public async Task GetOrSetAsync_WithCacheSizeLimit_EvictsOldItems()
        {
            // Arrange
            _options.MaxMemoryCacheItems = 2;
            var cache = CreateCache();
            var factoryExecutions = 0;

            async Task<string> Factory(string key)
            {
                factoryExecutions++;
                return $"value-{key}";
            }

            // Act
            await cache.GetOrSetAsync("key1", () => Factory("key1"));
            await cache.GetOrSetAsync("key2", () => Factory("key2"));
            await cache.GetOrSetAsync("key3", () => Factory("key3")); // Should trigger eviction
            await cache.GetOrSetAsync("key1", () => Factory("key1")); // Should be re-cached

            // Assert
            factoryExecutions.Should().Be(4);
        }

        [Fact]
        public async Task GetOrSetAsync_WithNullValue_DoesNotCache()
        {
            // Arrange
            var cache = CreateCache();
            var factoryExecutions = 0;

            async Task<string?> Factory()
            {
                factoryExecutions++;
                return null;
            }

            // Act
            var result1 = await cache.GetOrSetAsync("key1", Factory);
            var result2 = await cache.GetOrSetAsync("key1", Factory);

            // Assert
            factoryExecutions.Should().Be(2);
            result1.Should().BeNull();
            result2.Should().BeNull();
        }

        [Fact]
        public async Task GetOrSetAsync_WithCancellation_StopsOperation()
        {
            // Arrange
            var cache = CreateCache();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                cache.GetOrSetAsync("key1", async () => "test", cancellationToken: cts.Token));
        }

        private ISpecificationCache CreateCache()
        {
            return new SpecificationCache(
                _memoryCache,
                Options.Create(_options),
                _keyGenerator,
                _statistics);
        }
    }
}
