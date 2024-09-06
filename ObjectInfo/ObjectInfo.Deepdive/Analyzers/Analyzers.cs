using ObjectInfo.Models.MethodInfo;
using ObjectInfo.Models.TypeInfo;
using ObjectInfo.DeepDive.Analysis;

namespace ObjectInfo.DeepDive.Analyzers
{
    public interface IMethodAnalyzer : IDeepDiveAnalyzer
    {
        Task<MethodAnalysisResult> AnalyzeMethodAsync(IMethodInfo methodInfo);
    }


    /// <summary>
    /// Defines the contract for type analyzers.
    /// </summary>
    public interface ITypeAnalyzer : IDeepDiveAnalyzer
    {
        /// <summary>
        /// Performs deep analysis on the provided TypeInfo.
        /// </summary>
        /// <param name="typeInfo">The TypeInfo to analyze.</param>
        /// <returns>A task representing the asynchronous operation, containing the type analysis result.</returns>
        Task<TypeAnalysisResult> AnalyzeTypeAsync(TypeInfo typeInfo);
    }

}