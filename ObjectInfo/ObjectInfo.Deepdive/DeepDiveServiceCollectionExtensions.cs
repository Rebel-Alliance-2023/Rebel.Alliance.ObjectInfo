using Microsoft.Extensions.DependencyInjection;
using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Configuration;
using ObjectInfo.DeepDive.Plugins;
using System.Reflection;

namespace ObjectInfo.DeepDive
{
    /// <summary>
    /// Extension methods for configuring ObjectInfo DeepDive services.
    /// </summary>
    public static class DeepDiveServiceCollectionExtensions
    {
        /// <summary>
        /// Adds ObjectInfo DeepDive services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The optional configuration.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddObjectInfoDeepDive(this IServiceCollection services, DeepDiveConfiguration? configuration = null)
        {
            services.AddSingleton(configuration ?? new DeepDiveConfiguration());
            services.AddTransient<AnalyzerManager>();
            return services;
        }

        /// <summary>
        /// Adds analyzer plugins from the specified directory.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="pluginDirectory">The directory containing analyzer plugins.</param>
        /// <returns>The service collection.</returns>
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
