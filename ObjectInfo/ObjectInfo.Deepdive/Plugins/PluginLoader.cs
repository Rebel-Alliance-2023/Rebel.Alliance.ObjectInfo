using System.Reflection;

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
        /// <returns>A collection of loaded plugin assemblies.</returns>
        public static IEnumerable<Assembly> LoadPlugins(string pluginPath)
        {
            if (!Directory.Exists(pluginPath))
            {
                throw new DirectoryNotFoundException($"Plugin directory not found: {pluginPath}");
            }

            return Directory.GetFiles(pluginPath, "*.dll")
                .Select(Assembly.LoadFrom);
        }
    }
}
