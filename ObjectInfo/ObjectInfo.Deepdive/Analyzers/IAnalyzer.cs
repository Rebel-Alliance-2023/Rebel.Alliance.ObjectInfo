using System.Threading.Tasks;
using ObjectInfo.DeepDive.Analysis;

namespace ObjectInfo.DeepDive.Analyzers
{
    /// <summary>
    /// Defines the contract for all analyzers.
    /// </summary>
    public interface IAnalyzer
    {
        /// <summary>
        /// Gets the name of the analyzer.
        /// This name should be unique within the system and is used to identify specific analyzers.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Performs analysis on the provided analysis context.
        /// </summary>
        /// <param name="context">The context containing information for analysis.</param>
        /// <returns>A task representing the asynchronous operation, containing the analysis result.</returns>
        Task<AnalysisResult> AnalyzeAsync(AnalysisContext context);
    }
}
