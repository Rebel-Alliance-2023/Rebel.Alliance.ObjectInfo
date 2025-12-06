using Microsoft.CodeAnalysis;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Models
{
    /// <summary>
    /// Represents details about a navigation property for specification generation.
    /// </summary>
    /// <param name="Symbol">The property symbol.</param>
    /// <param name="TypeSymbol">The type symbol of the navigation property.</param>
    /// <param name="IsCollection">Whether the property is a collection.</param>
    /// <param name="IsNullable">Whether the property is nullable.</param>
    public record NavigationPropertyDetails(
        IPropertySymbol Symbol,
        INamedTypeSymbol TypeSymbol,
        bool IsCollection,
        bool IsNullable
    );
}