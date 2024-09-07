using ObjectInfo.Models.TypeInfo;
using ObjectInfo.DeepDive.Analysis;
using System.Threading.Tasks;

namespace ObjectInfo.DeepDive.Analyzers
{
    /// <summary>
    /// Defines the contract for type analyzers.
    /// </summary>
    public interface ITypeAnalyzer : IAnalyzer
    {
        /// <summary>
        /// Performs analysis on the provided type information.
        /// </summary>
        /// <param name="typeInfo">The type information to analyze.</param>
        /// <returns>A task representing the asynchronous operation, containing the type analysis result.</returns>
        Task<TypeAnalysisResult> AnalyzeTypeAsync(ITypeInfo typeInfo);
    }
}
