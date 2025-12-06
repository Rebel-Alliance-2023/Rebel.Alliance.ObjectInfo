using Microsoft.CodeAnalysis;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Models
{
    /// <summary>
    /// Represents details about a property for specification generation.
    /// </summary>
    /// <param name="Symbol">The property symbol.</param>
    /// <param name="Configuration">The property configuration.</param>
    public record PropertyDetails(
        IPropertySymbol Symbol,
        PropertyConfiguration Configuration
    );
}