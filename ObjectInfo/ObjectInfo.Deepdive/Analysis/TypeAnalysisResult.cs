namespace ObjectInfo.DeepDive.Analysis
{
    /// <summary>
    /// Represents the result of a type-specific analysis operation.
    /// </summary>
    public class TypeAnalysisResult : AnalysisResult
    {
        /// <summary>
        /// Gets or sets the name of the analyzed type.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Gets or sets the number of methods in the type.
        /// </summary>
        public int MethodCount { get; set; }

        /// <summary>
        /// Gets or sets the number of properties in the type.
        /// </summary>
        public int PropertyCount { get; set; }

        /// <summary>
        /// Initializes a new instance of the TypeAnalysisResult class.
        /// </summary>
        /// <param name="analyzerName">The name of the analyzer.</param>
        /// <param name="typeName">The name of the analyzed type.</param>
        /// <param name="summary">A brief summary of the result.</param>
        /// <param name="details">Detailed information about the result.</param>
        /// <param name="methodCount">The number of methods in the type.</param>
        /// <param name="propertyCount">The number of properties in the type.</param>
        public TypeAnalysisResult(string analyzerName, string typeName, string summary, string details, int methodCount, int propertyCount)
            : base(analyzerName, summary, details)
        {
            TypeName = typeName;
            MethodCount = methodCount;
            PropertyCount = propertyCount;
        }
    }
}
