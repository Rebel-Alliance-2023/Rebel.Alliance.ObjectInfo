using ObjectInfo.Models.MethodInfo;
using ObjectInfo.DeepDive.Analysis;
using System.Threading.Tasks;

namespace ObjectInfo.DeepDive.Analyzers
{
    /// <summary>
    /// Defines the contract for method analyzers.
    /// </summary>
    public interface IMethodAnalyzer : IAnalyzer
    {
        /// <summary>
        /// Performs analysis on the provided method information.
        /// </summary>
        /// <param name="methodInfo">The method information to analyze.</param>
        /// <returns>A task representing the asynchronous operation, containing the method analysis result.</returns>
        Task<MethodAnalysisResult> AnalyzeMethodAsync(IMethodInfo methodInfo);
    }
}
