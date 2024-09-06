namespace ObjectInfo.DeepDive.Analysis
{
    /// <summary>
    /// Represents the result of a method-specific analysis operation.
    /// </summary>
    public class MethodAnalysisResult : AnalysisResult
    {
        /// <summary>
        /// Gets or sets the name of the analyzed method.
        /// </summary>
        public string MethodName { get; set; }

        /// <summary>
        /// Gets or sets the cyclomatic complexity of the method, if calculated.
        /// </summary>
        public int? CyclomaticComplexity { get; set; }

        /// <summary>
        /// Initializes a new instance of the MethodAnalysisResult class.
        /// </summary>
        /// <param name="analyzerName">The name of the analyzer.</param>
        /// <param name="methodName">The name of the analyzed method.</param>
        /// <param name="summary">A brief summary of the result.</param>
        /// <param name="details">Detailed information about the result.</param>
        /// <param name="cyclomaticComplexity">The cyclomatic complexity of the method, if calculated.</param>
        public MethodAnalysisResult(string analyzerName, string methodName, string summary, string details, int? cyclomaticComplexity = null)
            : base(analyzerName, summary, details)
        {
            MethodName = methodName;
            CyclomaticComplexity = cyclomaticComplexity;
        }
    }
}
