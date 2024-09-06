using ObjectInfo.Models.ObjectInfo;
using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Analysis;

namespace ObjectInfo.DeepDive
{
    /// <summary>
    /// Provides advanced analysis capabilities for ObjectInfo instances.
    /// </summary>
    public class DeepDiveAnalysis
    {
        private readonly ObjInfo _baseObjectInfo;
        private readonly AnalyzerManager _analyzerManager;

        /// <summary>
        /// Initializes a new instance of the DeepDiveAnalysis class.
        /// </summary>
        /// <param name="baseObjectInfo">The ObjectInfo instance to analyze.</param>
        /// <param name="analyzerManager">The AnalyzerManager to use for analysis.</param>
        public DeepDiveAnalysis(ObjInfo baseObjectInfo, AnalyzerManager analyzerManager)
        {
            _baseObjectInfo = baseObjectInfo;
            _analyzerManager = analyzerManager;
        }

        /// <summary>
        /// Runs all registered analyzers on the ObjectInfo instance.
        /// </summary>
        /// <returns>A collection of analysis results.</returns>
        public async Task<IEnumerable<AnalysisResult>> RunAllAnalyzersAsync()
        {
            return await _analyzerManager.RunAnalyzersAsync(_baseObjectInfo);
        }

        // Additional analysis methods can be added here
    }
}