using Microsoft.CodeAnalysis;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Models
{
    public record NavigationPropertyDetails(
        IPropertySymbol Symbol,
        INamedTypeSymbol TypeSymbol,
        bool IsCollection,
        bool IsNullable
    );
}