using Xunit;
using Xunit.Abstractions;
using ObjectInfo.DeepDive;
using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Analysis;
using ObjectInfo.DeepDive.Configuration;
using ObjectInfo.Brokers.ObjectInfo;
using ObjectInfo.Models.ObjectInfo;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Linq;
using Serilog;
using System.IO;
using System.Reflection;

namespace ObjectInfo.DeepDive.LinqComplexityAnalyzer.Tests
{
    public class LinqComplexityAnalyzerTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public LinqComplexityAnalyzerTests(ITestOutputHelper output)
        {
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(output)
                .CreateLogger();

            var services = new ServiceCollection();
            
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

            Directory.CreateDirectory(pluginDirectory);

            services.AddSingleton<IAnalyzer, LinqComplexityAnalyzer>();

            _serviceProvider = services.BuildServiceProvider();

            foreach (var service in services)
            {
                _logger.Information($"Registered service: {service.ServiceType.Name} - {service.ImplementationType?.Name ?? "N/A"}");
            }
        }

        [Fact]
        public async Task AnalyzeComplexLinqQuery_ShouldReturnHighComplexity()
        {
            var objectInfoBroker = _serviceProvider.GetRequiredService<IObjectInfoBroker>();
            var analyzerManager = _serviceProvider.GetRequiredService<AnalyzerManager>();
            
            _logger.Information($"AnalyzerManager type: {analyzerManager.GetType().FullName}");
            
            var testObject = new ComplexLinqClass();
            var objInfo = (ObjInfo)objectInfoBroker.GetObjectInfo(testObject);

            _logger.Information($"Analyzing type: {objInfo.TypeInfo.Name}");
            foreach (var methodInfo in objInfo.TypeInfo.MethodInfos)
            {
                _logger.Information($"Method found: {methodInfo.Name}");
            }

            var testAssembly = Assembly.GetExecutingAssembly();
            var extendedMethods = objInfo.TypeInfo.MethodInfos.Select(m => 
                new ExtendedMethodInfo((Models.MethodInfo.MethodInfo)m, _logger, testAssembly)).ToList();

            var context = new AnalysisContext(objInfo);
            context.ExtendedMethods = (IEnumerable<IExtendedMethodInfo>)extendedMethods;

            var analyzer = new LinqComplexityAnalyzer(_logger);
            var result = await analyzer.AnalyzeAsync(context);

            Assert.NotNull(result);
            _logger.Information($"Analysis Result: {result.Summary}");
            _logger.Information($"Analysis Details: {result.Details}");

            Assert.Contains("ComplexLinqMethod", result.Details);
            Assert.Contains("6", result.Details);

            Assert.DoesNotContain("Error", result.Details);
        }
    }

    public class ComplexLinqClass
    {
        public IEnumerable<int> ComplexLinqMethod(IEnumerable<IGrouping<string, int>> data)
        {
            return data
                .Where(g => g.Key.Length > 5)
                .SelectMany(g => g)
                .Where(i => i > 10)
                .OrderByDescending(i => i)
                .Take(5);
        }
    }
}
