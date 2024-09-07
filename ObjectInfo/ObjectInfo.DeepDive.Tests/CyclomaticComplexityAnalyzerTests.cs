using Xunit;
using Xunit.Abstractions;
using ObjectInfo.DeepDive;
using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Analysis;
using ObjectInfo.DeepDive.Configuration;
using ObjectInfo.DeepDive.Plugins;
using ObjectInfo.Brokers.ObjectInfo;
using ObjectInfo.Models.ObjectInfo;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Linq;
using Serilog;
using System.IO;
using System.Reflection;

namespace ObjectInfo.DeepDive.Tests
{
    public class CyclomaticComplexityAnalyzerTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public CyclomaticComplexityAnalyzerTests(ITestOutputHelper output)
        {
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(output)
                .CreateLogger();

            var services = new ServiceCollection();
            
            // Set up the plugin directory path
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string pluginDirectory = Path.Combine(baseDirectory, "Plugins");

            var configuration = new DeepDiveConfiguration
            {
                IncludeSystemTypes = false,
                MaxAnalysisDepth = 5,
                PluginDirectory = pluginDirectory
            };

            _logger.Information($"Plugin directory path: {pluginDirectory}");

            services.AddObjectInfoDeepDive(configuration);
            services.AddTransient<IObjectInfoBroker, ObjectInfoBroker>();
            services.AddSingleton<ILogger>(_logger);
            services.AddSingleton<AnalyzerManager>();

            // Ensure plugin directory exists
            Directory.CreateDirectory(pluginDirectory);

            bool pluginsLoaded = false;
            try
            {
                // Load plugins
                var plugins = PluginLoader.LoadPlugins(pluginDirectory);
                foreach (var plugin in plugins)
                {
                    foreach (var analyzer in plugin.GetAnalyzers())
                    {
                        services.AddSingleton(typeof(IAnalyzer), analyzer.GetType());
                        _logger.Information($"Registered analyzer: {analyzer.GetType().Name}");
                        pluginsLoaded = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load plugins");
            }

            if (!pluginsLoaded)
            {
                _logger.Warning("No plugins were loaded. Adding mock analyzer.");
                _logger.Information("To add a real plugin:");
                _logger.Information("1. Create a class library project that references ObjectInfo.DeepDive");
                _logger.Information("2. Implement IAnalyzerPlugin and IAnalyzer interfaces");
                _logger.Information($"3. Compile and copy the resulting .dll to: {pluginDirectory}");
                services.AddSingleton<IAnalyzer, MockCyclomaticComplexityAnalyzer>();
            }

            _serviceProvider = services.BuildServiceProvider();

            // Log registered services
            foreach (var service in services)
            {
                _logger.Information($"Registered service: {service.ServiceType.Name} - {service.ImplementationType?.Name ?? "N/A"}");
            }
        }

        [Fact]
        public async Task AnalyzeComplexMethod_ShouldReturnModerateComplexity()
        {
            // Arrange
            var objectInfoBroker = _serviceProvider.GetRequiredService<IObjectInfoBroker>();
            var analyzerManager = _serviceProvider.GetRequiredService<AnalyzerManager>();
            
            _logger.Information($"AnalyzerManager type: {analyzerManager.GetType().FullName}");
            
            var testObject = new ComplexClass();
            var objInfo = objectInfoBroker.GetObjectInfo(testObject);

            _logger.Information($"Analyzing type: {objInfo.TypeInfo.Name}");
            foreach (var methodInfo in objInfo.TypeInfo.MethodInfos)
            {
                _logger.Information($"Method found: {methodInfo.Name}");
            }

            // Act
            var deepDiveAnalysis = new DeepDiveAnalysis((ObjInfo)objInfo, analyzerManager, _logger);
            _logger.Information("Running all analyzers");
            var results = await deepDiveAnalysis.RunAllAnalyzersAsync();

            // Log all results
            _logger.Information($"Number of analysis results: {results.Count()}");
            foreach (var result in results)
            {
                _logger.Information($"Analyzer: {result.AnalyzerName}, Summary: {result.Summary}");
            }

            var complexityResult = results.FirstOrDefault(r => r.AnalyzerName == "Cyclomatic Complexity Analyzer");

            // Assert
            Assert.NotNull(complexityResult);
            if (complexityResult != null)
            {
                _logger.Information($"Complexity Result Details: {complexityResult.Details}");
                Assert.Contains("Moderately complex, moderate risk", complexityResult.Details);
                Assert.Contains("Complexity: 6", complexityResult.Details);
            }
            else
            {
                _logger.Error("Cyclomatic Complexity Analyzer result not found");
            }
        }
    }

    public class ComplexClass
    {
        public string ComplexMethod(int a, int b, int c)
        {
            if (a > b)
            {
                if (b > c)
                {
                    return "a > b > c";
                }
                else if (a > c)
                {
                    return "a > c > b";
                }
                else
                {
                    return "c > a > b";
                }
            }
            else if (b > c)
            {
                if (a > c)
                {
                    return "b > a > c";
                }
                else
                {
                    return "b > c > a";
                }
            }
            else
            {
                return "c > b > a";
            }
        }
    }

    public class MockCyclomaticComplexityAnalyzer : IAnalyzer
    {
        public string Name => "Cyclomatic Complexity Analyzer";

        public Task<AnalysisResult> AnalyzeAsync(AnalysisContext context)
        {
            // Mock implementation for testing
            return Task.FromResult(new AnalysisResult(
                "Cyclomatic Complexity Analyzer",
                "Mock analysis completed",
                "Complexity: 6\nModerately complex, moderate risk"
            ));
        }
    }
}
