using Microsoft.CodeAnalysis;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Models
{
    internal record PropertyDetails(
        IPropertySymbol Symbol,
        PropertyConfiguration Configuration
    );
}