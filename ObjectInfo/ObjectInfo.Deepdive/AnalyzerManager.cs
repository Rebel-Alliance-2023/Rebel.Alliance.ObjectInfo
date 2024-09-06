using ObjectInfo.Models.ObjectInfo;
using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Analysis;

namespace ObjectInfo.DeepDive
{
    /// <summary>
    /// Manages the collection of analyzers and orchestrates their execution.
    /// </summary>
    public class AnalyzerManager
    {
        private readonly IEnumerable<IDeepDiveAnalyzer> _analyzers;

        /// <summary>
        /// Initializes a new instance of the AnalyzerManager class.
        /// </summary>
        /// <param name="analyzers">The collection of analyzers to manage.</param>
        public AnalyzerManager(IEnumerable<IDeepDiveAnalyzer> analyzers)
        {
            _analyzers = analyzers;
        }

        /// <summary>
        /// Runs all registered analyzers on the provided ObjectInfo.
        /// </summary>
        /// <param name="objInfo">The ObjectInfo to analyze.</param>
        /// <returns>A collection of analysis results.</returns>
        public async Task<IEnumerable<AnalysisResult>> RunAnalyzersAsync(ObjInfo objInfo)
        {
            var tasks = _analyzers.Select(a => a.AnalyzeAsync(objInfo));
            return await Task.WhenAll(tasks);
        }

        // Additional methods for managing analyzers can be added here
    }
}