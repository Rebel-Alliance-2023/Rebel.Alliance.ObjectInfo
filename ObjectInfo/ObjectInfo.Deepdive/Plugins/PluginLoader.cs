using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ObjectInfo.DeepDive.Plugins;

namespace ObjectInfo.DeepDive.Plugins
{
    /// <summary>
    /// Provides functionality for loading analyzer plugins.
    /// </summary>
    public static class PluginLoader
    {
        /// <summary>
        /// Loads analyzer plugins from the specified directory.
        /// </summary>
        /// <param name="pluginPath">The directory path containing the plugins.</param>
        /// <returns>A collection of loaded analyzer plugins.</returns>
        public static IEnumerable<IAnalyzerPlugin> LoadPlugins(string pluginPath)
        {
            if (!Directory.Exists(pluginPath))
            {
                throw new DirectoryNotFoundException($"Plugin directory not found: {pluginPath}");
            }

            var pluginAssemblies = Directory.GetFiles(pluginPath, "*.dll")
                .Select(Assembly.LoadFrom);

            var pluginTypes = pluginAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IAnalyzerPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            return pluginTypes.Select(t => (IAnalyzerPlugin)Activator.CreateInstance(t));
        }
    }
}
