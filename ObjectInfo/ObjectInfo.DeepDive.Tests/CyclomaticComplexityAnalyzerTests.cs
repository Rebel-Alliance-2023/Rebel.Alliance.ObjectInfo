using Xunit;
using Xunit.Abstractions;
using ObjectInfo.DeepDive;
using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Analysis;
using ObjectInfo.DeepDive.Configuration;
using ObjectInfo.Brokers.ObjectInfo;
using ObjectInfo.Models.ObjectInfo;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Linq;
using Serilog;

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
            var configuration = new DeepDiveConfiguration
            {
                IncludeSystemTypes = false,
                MaxAnalysisDepth = 5
            };
            services.AddObjectInfoDeepDive(configuration);
            services.AddTransient<IObjectInfoBroker, ObjectInfoBroker>();
            services.AddSingleton<ILogger>(_logger);
            services.AddTransient<CyclomaticComplexityAnalyzer>();
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task AnalyzeSimpleMethod_ShouldReturnLowComplexity()
        {
            // Arrange
            var objectInfoBroker = _serviceProvider.GetRequiredService<IObjectInfoBroker>();
            var analyzerManager = _serviceProvider.GetRequiredService<AnalyzerManager>();
            var testObject = new SimpleClass();
            var objInfo = objectInfoBroker.GetObjectInfo(testObject);

            // Act
            var deepDiveAnalysis = new DeepDiveAnalysis((ObjInfo)objInfo, analyzerManager);
            var results = await deepDiveAnalysis.RunAllAnalyzersAsync();
            var complexityResult = results.FirstOrDefault(r => r.AnalyzerName == "Cyclomatic Complexity Analyzer");

            // Assert
            Assert.NotNull(complexityResult);
            Assert.Contains("Simple, low-risk code", complexityResult.Details);
            Assert.Contains("Complexity: 1", complexityResult.Details);
        }

        [Fact]
        public async Task AnalyzeComplexMethod_ShouldReturnModeratecomplexity()
        {
            // Arrange
            var objectInfoBroker = _serviceProvider.GetRequiredService<IObjectInfoBroker>();
            var analyzerManager = _serviceProvider.GetRequiredService<AnalyzerManager>();
            var testObject = new ComplexClass();
            var objInfo = objectInfoBroker.GetObjectInfo(testObject);

            _logger.Information($"Analyzing type: {objInfo.TypeInfo.Name}");
            foreach (var methodInfo in objInfo.TypeInfo.MethodInfos)
            {
                _logger.Information($"Method found: {methodInfo.Name}");
            }

            // Act
            var deepDiveAnalysis = new DeepDiveAnalysis((ObjInfo)objInfo, analyzerManager);
            var results = await deepDiveAnalysis.RunAllAnalyzersAsync();
            var complexityResult = results.FirstOrDefault(r => r.AnalyzerName == "Cyclomatic Complexity Analyzer");

            // Assert
            Assert.NotNull(complexityResult);
            Assert.Contains("Simple, low-risk code", complexityResult.Details);
            Assert.Contains("Complexity: 6", complexityResult.Details);
        }
    }

    public class SimpleClass
    {
        public int SimpleMethod(int a, int b)
        {
            return a + b;
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
}