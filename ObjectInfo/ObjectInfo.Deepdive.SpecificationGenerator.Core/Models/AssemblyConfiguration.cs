namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Models
{
    /// <summary>
    /// Represents assembly-level configuration for specification generation.
    /// </summary>
    public record AssemblyConfiguration
    {
        /// <summary>
        /// Gets the default namespace for generated specifications.
        /// </summary>
        public string DefaultNamespace { get; init; } = "Specifications";

        /// <summary>
        /// Gets a value indicating whether to generate async methods by default.
        /// </summary>
        public bool DefaultGenerateAsync { get; init; }

        /// <summary>
        /// Gets a value indicating whether to generate documentation by default.
        /// </summary>
        public bool DefaultGenerateDocumentation { get; init; }

        /// <summary>
        /// Gets a value indicating whether to generate navigation specifications by default.
        /// </summary>
        public bool DefaultGenerateNavigationSpecs { get; init; }

        /// <summary>
        /// Gets the default string comparison mode.
        /// </summary>
        public string DefaultStringComparison { get; init; } = "OrdinalIgnoreCase";
    }
}