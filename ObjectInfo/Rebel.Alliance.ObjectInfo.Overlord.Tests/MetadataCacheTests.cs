using Microsoft.Extensions.Logging;
using Overlord.Test.Library;
using Rebel.Alliance.ObjectInfo.Overlord.Infrastructure;
using Rebel.Alliance.ObjectInfo.Overlord.Models;
using ObjectInfo.Models.ObjectInfo;
using Xunit;
using Xunit.Abstractions;
using System.Reflection;

namespace Rebel.Alliance.ObjectInfo.Overlord.Tests
{
    public class MetadataCacheTests : BaseTests
    {
        private readonly MetadataOptions _options;
        private readonly Assembly _testAssembly;

        public MetadataCacheTests(ITestOutputHelper output) : base(output)
        {
            _options = new MetadataOptions
            {
                EnableCaching = true,
                MaxCacheSize = 10
            };
            _testAssembly = typeof(BaseModel).Assembly;
        }

        [Fact]
        public void StoreType_WhenCachingEnabled_TypeIsStored()
        {
            // Arrange
            var cache = new MetadataCache(_options, GetLogger<MetadataCache>());
            var metadata = CreateTestTypeMetadata(typeof(DerivedModel));

            // Act
            cache.StoreType(metadata);
            var found = cache.TryGetType(metadata.AssemblyQualifiedName, out var retrieved);

            // Assert
            Assert.True(found);
            Assert.Equal(metadata.FullName, retrieved.FullName);
            Assert.True(metadata.ImplementsMetadataScanned);
        }

        [Fact]
        public void StoreAssembly_WhenCachingEnabled_AssemblyIsStored()
        {
            // Arrange
            var cache = new MetadataCache(_options, GetLogger<MetadataCache>());
            var metadata = CreateTestAssemblyMetadata();

            // Act
            cache.StoreAssembly(metadata);
            var found = cache.TryGetAssembly(metadata.FullName, out var retrieved);

            // Assert
            Assert.True(found);
            Assert.Equal(metadata.Name, retrieved.Name);
        }

        [Fact]
        public void CacheSize_WhenExceedsLimit_TrimsOldestItems()
        {
            // Arrange
            _options.MaxCacheSize = 2;
            var cache = new MetadataCache(_options, GetLogger<MetadataCache>());

            // Act
            cache.StoreType(CreateTestTypeMetadata(typeof(BaseModel)));
            cache.StoreType(CreateTestTypeMetadata(typeof(DerivedModel)));
            cache.StoreType(CreateTestTypeMetadata(typeof(AnotherModel)));
            cache.StoreType(CreateTestTypeMetadata(typeof(ContainerModel)));

            // Assert
            var stats = cache.Statistics;
            Assert.Equal(2, stats.TypeCount);
        }

        [Fact]
        public void GetAllTypes_ReturnsAllCachedTypes()
        {
            // Arrange
            var cache = new MetadataCache(_options, GetLogger<MetadataCache>());
            var metadata1 = CreateTestTypeMetadata(typeof(DerivedModel));
            var metadata2 = CreateTestTypeMetadata(typeof(AnotherModel));

            // Act
            cache.StoreType(metadata1);
            cache.StoreType(metadata2);
            var types = cache.GetAllTypes();

            // Assert
            Assert.Equal(2, types.Count());
            Assert.Contains(types, t => t.FullName == typeof(DerivedModel).FullName);
            Assert.Contains(types, t => t.FullName == typeof(AnotherModel).FullName);
        }

        private TypeMetadata CreateTestTypeMetadata(Type type)
        {
            var objInfo = new ObjInfo();
            return new TypeMetadata(type, objInfo);
        }

        private AssemblyMetadata CreateTestAssemblyMetadata()
        {
            var assembly = _testAssembly;
            var types = new Dictionary<string, TypeMetadata>
            {
                { typeof(BaseModel).AssemblyQualifiedName!, CreateTestTypeMetadata(typeof(BaseModel)) },
                { typeof(DerivedModel).AssemblyQualifiedName!, CreateTestTypeMetadata(typeof(DerivedModel)) }
            };
            return new AssemblyMetadata(assembly, types);
        }
    }
}
