namespace Rebel.Alliance.Specification.Dapper.Configuration
{
    public class DapperSpecificationOptions
    {
        /// <summary>
        /// Default command timeout in seconds
        /// </summary>
        public int DefaultCommandTimeout { get; set; } = 30;

        /// <summary>
        /// Whether to enable case-sensitive string comparisons by default
        /// </summary>
        public bool CaseSensitiveStringComparisons { get; set; } = false;

        /// <summary>
        /// Whether to enable query result caching
        /// </summary>
        public bool EnableCaching { get; set; } = false;

        /// <summary>
        /// Cache duration in seconds (if caching is enabled)
        /// </summary>
        public int CacheDuration { get; set; } = 300;  // 5 minutes

        /// <summary>
        /// Default schema name for table queries
        /// </summary>
        public string DefaultSchema { get; set; } = "dbo";

        /// <summary>
        /// Maximum number of parameters allowed in a single query
        /// </summary>
        public int MaxParameterCount { get; set; } = 2000;
    }
}
