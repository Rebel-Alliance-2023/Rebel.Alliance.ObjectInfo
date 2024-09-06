namespace ObjectInfo.DeepDive.Configuration
{
    /// <summary>
    /// Provides configuration options for ObjectInfo.DeepDive analysis.
    /// </summary>
    public class DeepDiveConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether to include system types in the analysis.
        /// </summary>
        public bool IncludeSystemTypes { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum depth for recursive analysis.
        /// </summary>
        public int MaxAnalysisDepth { get; set; } = 3;

        /// <summary>
        /// Gets or sets the directory path for loading plugins.
        /// </summary>
        public string? PluginDirectory { get; set; }

        // Additional configuration options can be added here
    }
}
