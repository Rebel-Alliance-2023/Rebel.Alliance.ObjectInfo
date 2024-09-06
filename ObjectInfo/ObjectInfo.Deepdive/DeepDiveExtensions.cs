using ObjectInfo.Models.ObjectInfo;

namespace ObjectInfo.DeepDive
{
    /// <summary>
    /// Provides extension methods for integrating DeepDive analysis with ObjectInfo.
    /// </summary>
    public static class DeepDiveExtensions
    {
        /// <summary>
        /// Extends ObjectInfo with DeepDive analysis capabilities.
        /// </summary>
        /// <param name="objInfo">The ObjectInfo instance to analyze.</param>
        /// <param name="analyzerManager">The AnalyzerManager to use for analysis.</param>
        /// <returns>A DeepDiveAnalysis instance for the provided ObjectInfo.</returns>
        public static DeepDiveAnalysis ToDeepDive(this ObjInfo objInfo, AnalyzerManager analyzerManager)
        {
            return new DeepDiveAnalysis(objInfo, analyzerManager);
        }
    }
}