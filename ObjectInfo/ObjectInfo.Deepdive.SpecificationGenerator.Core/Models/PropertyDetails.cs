using Microsoft.CodeAnalysis;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Models
{
    public record PropertyDetails(
        IPropertySymbol Symbol,
        PropertyConfiguration Configuration
    );
}