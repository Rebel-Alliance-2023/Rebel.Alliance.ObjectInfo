using ObjectInfo.DeepDive.Analyzers;
using System.Collections.Generic;

namespace ObjectInfo.DeepDive.Plugins
{
    /// <summary>
    /// Defines the contract for an analyzer plugin.
    /// </summary>
    public interface IAnalyzerPlugin
    {
        /// <summary>
        /// Gets the name of the analyzer plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the version of the analyzer plugin.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets the collection of analyzers provided by this plugin.
        /// </summary>
        /// <returns>An enumerable collection of IAnalyzer instances.</returns>
        IEnumerable<IAnalyzer> GetAnalyzers();
    }
}
