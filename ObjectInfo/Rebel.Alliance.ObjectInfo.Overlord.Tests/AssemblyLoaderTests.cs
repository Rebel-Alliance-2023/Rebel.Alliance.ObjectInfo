using Microsoft.Extensions.Logging;
using Overlord.Test.Library;
using Rebel.Alliance.ObjectInfo.Overlord.Infrastructure;
using Rebel.Alliance.ObjectInfo.Overlord.Markers;
using Rebel.Alliance.ObjectInfo.Overlord.Models;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Rebel.Alliance.ObjectInfo.Overlord.Tests
{
    public class AssemblyLoaderTests : BaseTests
    {
        private readonly MetadataOptions _options;
        private readonly Assembly _testAssembly;

        public AssemblyLoaderTests(ITestOutputHelper output) : base(output)
        {
            _options = new MetadataOptions
            {
                ValidateAssemblies = true
            };
            _testAssembly = typeof(BaseModel).Assembly;
        }

        [Fact]
        public void DiscoverTypes_FindsMarkedTypes()
        {
            // Arrange
            var loader = new AssemblyLoader(_options, GetLogger<AssemblyLoader>());

            // Act
            var types = loader.DiscoverTypes(_testAssembly).ToList();

            // Assert
            Assert.NotEmpty(types);
            Assert.Contains(types, t => t == typeof(BaseModel));
            Assert.Contains(types, t => t == typeof(ContainerModel));
            Assert.Contains(types, t => t == typeof(StaticTestModel));
            Assert.Contains(types, t => t == typeof(AbstractTestModel));
        }

        [Fact]
        public void DiscoverTypes_FindsTypesImplementingInterface()
        {
            // Arrange
            var loader = new AssemblyLoader(_options, GetLogger<AssemblyLoader>());

            // Act
            var types = loader.DiscoverTypes(_testAssembly).ToList();

            // Assert
            Assert.NotEmpty(types);
            Assert.Contains(types, t => t == typeof(DerivedModel));
            Assert.Contains(types, t => t == typeof(AnotherModel));
            Assert.Contains(types, t => t == typeof(TestInterfaceImplementation));
            Assert.Contains(types, t => typeof(IMetadataScanned).IsAssignableFrom(t));
        }

        [Fact]
        public void DiscoverTypes_FindsNestedTypes()
        {
            // Arrange
            var loader = new AssemblyLoader(_options, GetLogger<AssemblyLoader>());

            // Act
            var types = loader.DiscoverTypes(_testAssembly).ToList();

            // Assert
            Assert.Contains(types, t => t == typeof(ContainerModel.NestedModel));
        }

        [Fact]
        public void DiscoverTypes_FindsGenericTypes()
        {
            // Arrange
            var loader = new AssemblyLoader(_options, GetLogger<AssemblyLoader>());

            // Act
            var types = loader.DiscoverTypes(_testAssembly).ToList();

            // Assert
            Assert.Contains(types, t => t.IsGenericTypeDefinition && t == typeof(GenericModel<>));
            Assert.Contains(types, t => t.IsGenericTypeDefinition && t == typeof(GenericModelWithConstraints<>));
        }

        [Fact]
        public void DiscoverTypes_WithFilter_AppliesFilter()
        {
            // Arrange
            var loader = new AssemblyLoader(_options, GetLogger<AssemblyLoader>());

            // Filter for only concrete classes (not abstract or interfaces)
            _options.AddTypeFilter(t => !t.IsAbstract && !t.IsInterface);

            // Act
            var types = loader.DiscoverTypes(_testAssembly).ToList();

            // Assert
            Assert.DoesNotContain(types, t => t.IsAbstract);
            Assert.DoesNotContain(types, t => t.IsInterface);
            Assert.Contains(types, t => t == typeof(ConcreteTestModel));
            Assert.Contains(types, t => t == typeof(DerivedModel));
        }

        [Fact]
        public void DiscoverTypes_WithInvalidAssembly_HandlesErrorGracefully()
        {
            // Arrange
            var loader = new AssemblyLoader(_options, GetLogger<AssemblyLoader>());
            var invalidAssembly = Assembly.GetExecutingAssembly(); // Use test assembly as "invalid"

            // Act & Assert
            var exception = Record.Exception(() => loader.DiscoverTypes(invalidAssembly));
            Assert.Null(exception);
        }

        [Fact]
        public void DiscoverTypes_FindsAttributeDecoratedTypes()
        {
            // Arrange
            var loader = new AssemblyLoader(_options, GetLogger<AssemblyLoader>());

            // Act
            var types = loader.DiscoverTypes(_testAssembly).ToList();

            // Assert
            Assert.Contains(types, t => t.GetCustomAttributes(typeof(MetadataScanAttribute), false).Any());
            Assert.Contains(types, t => t.GetCustomAttributes(typeof(CustomAttribute), false).Any());
        }
    }
}
