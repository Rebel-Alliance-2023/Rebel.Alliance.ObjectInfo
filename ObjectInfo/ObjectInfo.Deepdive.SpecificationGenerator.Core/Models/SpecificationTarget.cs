using Microsoft.CodeAnalysis;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Models
{
    /// <summary>
    /// Represents the target for specification generation.
    /// </summary>
    /// <param name="ClassSymbol">The class symbol to generate a specification for.</param>
    /// <param name="Configuration">The specification configuration.</param>
    /// <param name="Properties">The properties to include in the specification.</param>
    /// <param name="NavigationProperties">The navigation properties to include.</param>
    /// <param name="AssemblyConfiguration">The assembly-level configuration.</param>
    public record SpecificationTarget(
        INamedTypeSymbol ClassSymbol,
        SpecificationConfiguration Configuration,
        List<PropertyDetails> Properties,
        List<NavigationPropertyDetails> NavigationProperties,
        AssemblyConfiguration AssemblyConfiguration
    );
}