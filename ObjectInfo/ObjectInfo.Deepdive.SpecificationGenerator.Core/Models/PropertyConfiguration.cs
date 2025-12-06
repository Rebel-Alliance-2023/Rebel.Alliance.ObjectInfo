namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Models
{
    /// <summary>
    /// Configuration options for property-level specification generation.
    /// </summary>
    public record PropertyConfiguration
    {
        /// <summary>
        /// Gets a value indicating whether to generate Contains filter.
        /// </summary>
        public bool GenerateContains { get; init; }

        /// <summary>
        /// Gets a value indicating whether to generate StartsWith filter.
        /// </summary>
        public bool GenerateStartsWith { get; init; }

        /// <summary>
        /// Gets a value indicating whether to generate EndsWith filter.
        /// </summary>
        public bool GenerateEndsWith { get; init; }

        /// <summary>
        /// Gets a value indicating whether string comparisons are case sensitive.
        /// </summary>
        public bool CaseSensitive { get; init; }

        /// <summary>
        /// Gets a value indicating whether to generate range filters.
        /// </summary>
        public bool GenerateRange { get; init; }

        /// <summary>
        /// Gets the custom expression for the property filter.
        /// </summary>
        public string? CustomExpression { get; init; }

        /// <summary>
        /// Gets a value indicating whether to generate null checks.
        /// </summary>
        public bool GenerateNullChecks { get; init; }
    }
}