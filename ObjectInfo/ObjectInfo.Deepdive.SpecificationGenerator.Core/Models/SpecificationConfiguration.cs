using Microsoft.CodeAnalysis;
using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Models
{
    /// <summary>
    /// Configuration for specification generation.
    /// </summary>
    public record SpecificationConfiguration
    {
        /// <summary>
        /// Gets the target ORM for the specification.
        /// </summary>
        public OrmTarget TargetOrm { get; init; }

        /// <summary>
        /// Gets a value indicating whether to generate navigation specifications.
        /// </summary>
        public bool GenerateNavigationSpecs { get; init; }

        /// <summary>
        /// Gets a value indicating whether to generate documentation.
        /// </summary>
        public bool GenerateDocumentation { get; init; }

        /// <summary>
        /// Gets a value indicating whether to generate async methods.
        /// </summary>
        public bool GenerateAsyncMethods { get; init; }

        /// <summary>
        /// Gets the target namespace for the generated specification.
        /// </summary>
        public string? TargetNamespace { get; init; }

        /// <summary>
        /// Gets the base class for the generated specification.
        /// </summary>
        public INamedTypeSymbol? BaseClass { get; init; }
    }
}