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
            // Existing code...

            return services;
        }

        public static IServiceCollection AddDeepDiveAnalyzers(this IServiceCollection services, params Assembly[] assemblies)
        {
            // Existing code...

            return services;
        }

        public static IServiceCollection AddAnalyzerPlugins(this IServiceCollection services, string pluginDirectory)
        {
            var plugins = PluginLoader.LoadPlugins(pluginDirectory);

            foreach (var plugin in plugins)
            {
                foreach (var analyzer in plugin.GetAnalyzers())
                {
                    services.AddSingleton(analyzer.GetType(), analyzer);
                }
            }

            return services;
        }
    }
}
