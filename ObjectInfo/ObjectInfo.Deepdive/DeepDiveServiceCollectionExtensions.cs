using Microsoft.Extensions.DependencyInjection;
using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Configuration;
using System.Reflection;

namespace ObjectInfo.DeepDive
{
    public static class DeepDiveServiceCollectionExtensions
    {
        public static IServiceCollection AddObjectInfoDeepDive(this IServiceCollection services, DeepDiveConfiguration configuration = null)
        {
            // Register configuration
            if (configuration != null)
            {
                services.AddSingleton(configuration);
            }
            else
            {
                services.AddSingleton(new DeepDiveConfiguration());
            }

            // Register core services
            services.AddSingleton<AnalyzerManager>();
            services.AddTransient<DeepDiveAnalysis>();

            // Register built-in analyzers
            services.AddTransient<IDeepDiveAnalyzer, CyclomaticComplexityAnalyzer>();

            return services;
        }

        public static IServiceCollection AddDeepDiveAnalyzers(this IServiceCollection services, params Assembly[] assemblies)
        {
            var analyzerTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IDeepDiveAnalyzer).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var analyzerType in analyzerTypes)
            {
                services.AddTransient(typeof(IDeepDiveAnalyzer), analyzerType);
            }

            return services;
        }
    }
}