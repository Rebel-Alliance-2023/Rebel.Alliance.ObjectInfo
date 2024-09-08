using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using ObjectInfo.Brokers.ObjectInfo;
using ObjectInfo.DeepDive;
using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Analysis;
using ObjectInfo.DeepDive.Configuration;
using ObjectInfo.Models.ObjectInfo;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace ObjectInfo.Deepdive.SolidAnalyzer.Tests
{
    public class SolidAnalyzerTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public SolidAnalyzerTests(ITestOutputHelper output)
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

            // Explicitly register the SolidAnalyzer
            services.AddSingleton<IAnalyzer, SolidAnalyzer>();

            _serviceProvider = services.BuildServiceProvider();

            // Log registered services
            _logger.Information("Registered services:");
            foreach (var service in services)
            {
                _logger.Information($"- {service.ServiceType.Name} - {service.ImplementationType?.Name ?? "N/A"}");
            }

            // Verify that AnalyzerManager and SolidAnalyzer are registered
            var analyzerManager = _serviceProvider.GetService<AnalyzerManager>();
            var solidAnalyzer = _serviceProvider.GetService<IAnalyzer>();

            if (analyzerManager == null)
            {
                _logger.Error("AnalyzerManager is not registered");
            }
            else
            {
                _logger.Information("AnalyzerManager is registered");
            }

            if (solidAnalyzer == null)
            {
                _logger.Error("SolidAnalyzer is not registered");
            }
            else
            {
                _logger.Information($"SolidAnalyzer is registered: {solidAnalyzer.GetType().Name}");
            }
        }

        private async Task<SolidAnalysisResult?> RunAnalysisForObject<T>(T testObject) where T : class
        {
            var objectInfoBroker = _serviceProvider.GetRequiredService<IObjectInfoBroker>();
            var analyzerManager = _serviceProvider.GetRequiredService<AnalyzerManager>();

            _logger.Information($"Analyzing object of type: {typeof(T).Name}");
            var objInfo = (ObjInfo)objectInfoBroker.GetObjectInfo(testObject);

            _logger.Information($"ObjectInfo created: {objInfo.TypeInfo.Name}");

            var deepDiveAnalysis = new DeepDiveAnalysis(objInfo, analyzerManager, _logger);

            _logger.Information("Running all analyzers...");
            var results = await deepDiveAnalysis.RunAllAnalyzersAsync();

            _logger.Information($"Number of analysis results: {results.Count()}");

            if (results.Any())
            {
                foreach (var result in results)
                {
                    _logger.Information($"Analyzer: {result.AnalyzerName}, Summary: {result.Summary}");
                }

                var solidResult = results.FirstOrDefault(r => r.AnalyzerName == "SOLID Principles Analyzer") as SolidAnalysisResult;
                if (solidResult == null)
                {
                    _logger.Warning("SOLID Principles Analyzer result not found in the results");
                }
                else
                {
                    _logger.Information("SOLID Principles Analyzer result found");
                }
                return solidResult!;
            }
            else
            {
                _logger.Warning("No analysis results returned");
                return null;
            }
        }


        [Fact]
        public async Task AnalyzeType_ShouldIdentifySrpViolation()
        {
            var solidResult = await RunAnalysisForObject(new SrpViolationClass());

            Assert.NotNull(solidResult);
            Assert.Contains("Class has", solidResult.SingleResponsibilityAnalysis.ToString());
            Assert.Contains("exceeds the recommended maximum", solidResult.SingleResponsibilityAnalysis.ToString());
        }

        [Fact]
        public async Task AnalyzeType_ShouldRecognizeGoodOcp()
        {
            var solidResult = await RunAnalysisForObject(new ConcreteOcpCompliantClass());

            Assert.NotNull(solidResult);
            Assert.Empty(solidResult.OpenClosedAnalysis.Violations);
            Assert.False(solidResult.OpenClosedAnalysis.IsAbstract);
            Assert.True(solidResult.OpenClosedAnalysis.VirtualMethodCount > 0);
        }

        [Fact]
        public async Task AnalyzeType_ShouldIdentifyLspViolation()
        {
            var solidResult = await RunAnalysisForObject(new LspViolationClass());

            Assert.NotNull(solidResult);
            Assert.Contains("may violate LSP", solidResult.LiskovSubstitutionAnalysis.ToString());
        }

        [Fact]
        public async Task AnalyzeType_ShouldIdentifyIspViolation()
        {
            var solidResult = await RunAnalysisForObject(new IspViolationClass());

            Assert.NotNull(solidResult);
            Assert.Contains("does not fully implement interface", solidResult.InterfaceSegregationAnalysis.ToString());
        }

        [Fact]
        public async Task AnalyzeType_ShouldIdentifyDipViolation()
        {
            var solidResult = await RunAnalysisForObject(new DipViolationClass(new ConcreteClass()));

            Assert.NotNull(solidResult);
            Assert.Contains("is a concrete type", solidResult.DependencyInversionAnalysis.ToString());
        }
    }

    // Test classes
    public class SrpViolationClass
    {
        public void Method1() { }
        public void Method2() { }
        public void Method3() { }
        public void Method4() { }
        public void Method5() { }
        public void Method6() { }
        public void Method7() { }
        public void Method8() { }
        public void Method9() { }
        public void Method10() { }
        public void Method11() { }
    }

    public abstract class OcpCompliantClass
    {
        public virtual void Method1() { }
        public virtual void Method2() { }
    }

    public abstract class OcpCompliantClassImplementation
    {
        public virtual void Method1() { }
        public virtual void Method2() { }
    }

    public class ConcreteOcpCompliantClass : OcpCompliantClassImplementation
    {
        public override void Method1() { }
        public override void Method2() { }
    }

    public class Parent
    {
        public virtual int Method(int a) => a + 1;
    }

    public class LspViolationClass : Parent
    {
        public override int Method(int a) => a * 2; // Violates LSP by changing behavior
    }

    public interface ILargeInterface
    {
        void Method1();
        void Method2();
        void Method3();
    }

    public class IspViolationClass : ILargeInterface
    {
        public void Method1() { }
        public void Method2() { }
        public void Method3() { throw new NotImplementedException(); } // Violates ISP
    }

    public class ConcreteClass { }

    public class DipViolationClass
    {
        private readonly ConcreteClass _dependency;

        public DipViolationClass(ConcreteClass dependency) // Violates DIP
        {
            _dependency = dependency;
        }
    }
}
