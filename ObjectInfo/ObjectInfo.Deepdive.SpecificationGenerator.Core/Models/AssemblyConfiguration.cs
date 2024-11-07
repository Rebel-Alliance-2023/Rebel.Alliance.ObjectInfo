namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Models
{
    public record AssemblyConfiguration
    {
        public string DefaultNamespace { get; init; } = "Specifications";
        public bool DefaultGenerateAsync { get; init; }
        public bool DefaultGenerateDocumentation { get; init; }
        public bool DefaultGenerateNavigationSpecs { get; init; }
        public string DefaultStringComparison { get; init; } = "OrdinalIgnoreCase";
    }
}