using ObjectInfo.Models.ObjectInfo;
using ObjectInfo.DeepDive.Analysis;

namespace ObjectInfo.DeepDive.Analyzers
{
    /// <summary>
    /// Defines the contract for all deep dive analyzers.
    /// </summary>
    public interface IDeepDiveAnalyzer
    {
        /// <summary>
        /// Gets the name of the analyzer.
        /// </summary>
        string AnalyzerName { get; }

        /// <summary>
        /// Performs deep analysis on the provided ObjectInfo.
        /// </summary>
        /// <param name="objInfo">The ObjectInfo to analyze.</param>
        /// <returns>A task representing the asynchronous operation, containing the analysis result.</returns>
        Task<AnalysisResult> AnalyzeAsync(ObjInfo objInfo);
    }
}