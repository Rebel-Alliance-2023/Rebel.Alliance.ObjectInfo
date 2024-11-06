using Microsoft.CodeAnalysis;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Models
{
    internal record SpecificationTarget(
        INamedTypeSymbol ClassSymbol,
        SpecificationConfiguration Configuration,
        List<PropertyDetails> Properties,
        List<NavigationPropertyDetails> NavigationProperties,
        AssemblyConfiguration AssemblyConfiguration
    );
}