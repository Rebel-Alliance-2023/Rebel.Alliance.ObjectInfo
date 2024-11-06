using Microsoft.CodeAnalysis;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Models
{
    internal record NavigationPropertyDetails(
        IPropertySymbol Symbol,
        INamedTypeSymbol TypeSymbol,
        bool IsCollection,
        bool IsNullable
    );
}