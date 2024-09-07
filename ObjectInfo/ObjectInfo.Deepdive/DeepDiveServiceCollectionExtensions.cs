using Microsoft.Extensions.DependencyInjection;
using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Configuration;
using ObjectInfo.DeepDive.Plugins;
using System.Reflection;

namespace ObjectInfo.DeepDive
{
    public static class DeepDiveServiceCollectionExtensions
    {
        public static IServiceCollection AddObjectInfoDeepDive(this IServiceCollection services, DeepDiveConfiguration configuration = null)
        {
            services.AddSingleton(configuration ?? new DeepDiveConfiguration());
            services.AddTransient<AnalyzerManager>();
            return services;
        }

        public static IServiceCollection AddAnalyzerPlugins(this IServiceCollection services, string pluginDirectory)
        {
            var plugins = PluginLoader.LoadPlugins(pluginDirectory);

            foreach (var plugin in plugins)
            {
                foreach (var analyzer in plugin.GetAnalyzers())
                {
                    services.AddSingleton(typeof(IAnalyzer), analyzer.GetType());
                }
            }

            return services;
        }
    }
}
