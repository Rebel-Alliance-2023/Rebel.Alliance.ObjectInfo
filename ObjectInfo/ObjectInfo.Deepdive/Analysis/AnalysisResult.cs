namespace ObjectInfo.DeepDive.Analysis
{
    /// <summary>
    /// Represents the result of a deep dive analysis operation.
    /// </summary>
    public class AnalysisResult
    {
        /// <summary>
        /// Gets or sets the name of the analyzer that produced this result.
        /// </summary>
        public string AnalyzerName { get; set; }

        /// <summary>
        /// Gets or sets a brief summary of the analysis result.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets detailed information about the analysis result.
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// Initializes a new instance of the AnalysisResult class.
        /// </summary>
        /// <param name="analyzerName">The name of the analyzer.</param>
        /// <param name="summary">A brief summary of the result.</param>
        /// <param name="details">Detailed information about the result.</param>
        public AnalysisResult(string analyzerName, string summary, string details)
        {
            AnalyzerName = analyzerName;
            Summary = summary;
            Details = details;
        }
    }
}
