using Microsoft.CodeAnalysis;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Models
{
    public record SpecificationTarget(
        INamedTypeSymbol ClassSymbol,
        SpecificationConfiguration Configuration,
        List<PropertyDetails> Properties,
        List<NavigationPropertyDetails> NavigationProperties,
        AssemblyConfiguration AssemblyConfiguration
    );
}