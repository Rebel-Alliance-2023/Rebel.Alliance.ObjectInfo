namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Models
{
    internal record PropertyConfiguration
    {
        public bool GenerateContains { get; init; }
        public bool GenerateStartsWith { get; init; }
        public bool GenerateEndsWith { get; init; }
        public bool CaseSensitive { get; init; }
        public bool GenerateRange { get; init; }
        public string? CustomExpression { get; init; }
        public bool GenerateNullChecks { get; init; }
    }
}