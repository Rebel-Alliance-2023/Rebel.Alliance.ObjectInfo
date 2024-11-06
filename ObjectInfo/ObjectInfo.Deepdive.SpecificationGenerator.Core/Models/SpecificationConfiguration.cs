using Microsoft.CodeAnalysis;
using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Models
{
    internal record SpecificationConfiguration
    {
        public OrmTarget TargetOrm { get; init; }
        public bool GenerateNavigationSpecs { get; init; }
        public bool GenerateDocumentation { get; init; }
        public bool GenerateAsyncMethods { get; init; }
        public string? TargetNamespace { get; init; }
        public INamedTypeSymbol? BaseClass { get; init; }  // Added this property
    }
}