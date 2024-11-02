using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ObjectInfo.Brokers.ObjectInfo;
using ObjectInfo.DeepDive;
using ObjectInfo.DeepDive.Analysis;
using ObjectInfo.DeepDive.Analyzers;
using Overlord.Test.Library;
using Rebel.Alliance.ObjectInfo.Overlord.Infrastructure;
using Rebel.Alliance.ObjectInfo.Overlord.Models;
using Rebel.Alliance.ObjectInfo.Overlord.Services;
using Rebel.Alliance.ObjectInfo.Overlord.Tests;
using Serilog;
using Serilog.Extensions.Logging;
using System.Reflection;
using Xunit.Abstractions;
using ILogger = Serilog.ILogger;


namespace Rebel.Alliance.ObjectInfo.Overlord.Tests
{
    public class MetadataProviderTests : BaseTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Assembly _testAssembly;

        public MetadataProviderTests(ITestOutputHelper output) : base(output)
        {
            var services = new ServiceCollection();

            // Configure logging
            services.AddSingleton<ILogger>(SerilogLogger);
            services.AddLogging(builder =>
            {
                builder.Services.AddSingleton<ILoggerFactory>(sp =>
                    new SerilogLoggerFactory(SerilogLogger, true));
            });

            // Register services
            services.AddSingleton<IObjectInfoBroker, ObjectInfoBroker>();
            services.AddSingleton<AnalyzerManager>();
            services.AddSingleton<MetadataCache>();
            services.AddSingleton<AssemblyLoader>();
            services.AddSingleton<IMetadataProvider, MetadataProvider>();
            services.AddSingleton(new MetadataOptions());

            // Add mock analyzer
            services.AddSingleton<IAnalyzer>(new MockAnalyzer());

            _serviceProvider = services.BuildServiceProvider();
            _testAssembly = typeof(BaseModel).Assembly;
        }

        // Add MockAnalyzer class within MetadataProviderTests
        private class MockAnalyzer : IAnalyzer
        {
            public string Name => "TestAnalyzer";

            public Task<AnalysisResult> AnalyzeAsync(AnalysisContext context)
            {
                return Task.FromResult(new AnalysisResult(
                    Name,
                    "Test Analysis Complete",
                    "Test Analysis Details"));
            }
        }

        [Fact]
        public async Task GetTypeMetadata_WhenTypeHasMetadataScan_ReturnsMetadata()
        {
            // Arrange
            var provider = _serviceProvider.GetRequiredService<IMetadataProvider>();
            var instance = new BaseModel { Id = 1 };

            // Act
            var metadata = await provider.GetTypeMetadataAsync(instance.GetType());

            // Assert
            Assert.NotNull(metadata);
            Assert.True(metadata.ScanAttribute != null);
            Assert.Equal(typeof(BaseModel).FullName, metadata.FullName);
        }

        [Fact]
        public async Task GetTypeMetadata_WhenTypeImplementsInterface_ReturnsMetadata()
        {
            // Arrange
            var provider = _serviceProvider.GetRequiredService<IMetadataProvider>();
            var instance = new DerivedModel { Id = 1, Name = "Test" };

            // Act
            var metadata = await provider.GetTypeMetadataAsync(instance.GetType());

            // Assert
            Assert.NotNull(metadata);
            Assert.True(metadata.ImplementsMetadataScanned);
        }

        [Fact]
        public async Task ScanAssembly_ReturnsValidMetadata()
        {
            // Arrange
            var provider = _serviceProvider.GetRequiredService<IMetadataProvider>();
            var testInstances = CreateTestInstances();

            // Act
            var metadata = await ((IMetadataProvider)provider).ScanAssemblyAsync(_testAssembly);

            // Assert
            Assert.NotNull(metadata);
            Assert.Equal(_testAssembly.GetName().Name, metadata.Name);
            Assert.NotEmpty(metadata.Types);

            // Log found types for debugging
            foreach (var type in metadata.Types)
            {
                SerilogLogger.Information("Found type: {TypeName}", type.Key);
            }
        }

        [Theory]
        [InlineData(typeof(ConcreteTestModel))]
        [InlineData(typeof(DerivedModel))]
        [InlineData(typeof(AnotherModel))]
        public async Task GetTypeMetadata_WithInstantiableTypes_ReturnsCorrectMetadata(Type type)
        {
            // Arrange
            var provider = _serviceProvider.GetRequiredService<IMetadataProvider>();
            var instance = CreateInstance(type);

            // Act
            var metadata = await provider.GetTypeMetadataAsync(instance.GetType());

            // Assert
            Assert.NotNull(metadata);
            Assert.Equal(type.FullName, metadata.FullName);
        }

        [Fact]
        public async Task GetAnalysisResult_WhenAnalyzerExists_ReturnsResult()
        {
            // Arrange
            var provider = _serviceProvider.GetRequiredService<IMetadataProvider>();
            var instance = new DerivedModel { Id = 1, Name = "Test" };

            // Act
            var result = await provider.AnalyzeTypeAsync(instance.GetType(), "TestAnalyzer");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestAnalyzer", result.AnalyzerName);
        }

        private Dictionary<Type, object> CreateTestInstances()
        {
            return new Dictionary<Type, object>
        {
            { typeof(BaseModel), new BaseModel { Id = 1 } },
            { typeof(DerivedModel), new DerivedModel { Id = 2, Name = "Test" } },
            { typeof(AnotherModel), new AnotherModel(42) },
            { typeof(ConcreteTestModel), new ConcreteTestModel() },
            { typeof(ContainerModel), new ContainerModel { ContainerName = "Test" } }
        };
        }

        private object CreateInstance(Type type)
        {
            return type.Name switch
            {
                nameof(BaseModel) => new BaseModel { Id = 1 },
                nameof(DerivedModel) => new DerivedModel { Id = 2, Name = "Test" },
                nameof(AnotherModel) => new AnotherModel(42),
                nameof(ConcreteTestModel) => new ConcreteTestModel(),
                nameof(ContainerModel) => new ContainerModel { ContainerName = "Test" },
                _ => Activator.CreateInstance(type)
            };
        }
    }
}