using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core.Models
{
    internal record SpecificationTarget(
        INamedTypeSymbol ClassSymbol,
        SpecificationConfiguration Configuration,
        List<PropertyDetails> Properties,
        List<NavigationPropertyDetails> NavigationProperties,
        AssemblyConfiguration AssemblyConfiguration
    );

    internal record SpecificationConfiguration
    {
        public OrmTarget TargetOrm { get; init; }
        public bool GenerateNavigationSpecs { get; init; }
        public bool GenerateDocumentation { get; init; }
        public bool GenerateAsyncMethods { get; init; }
        public string? TargetNamespace { get; init; }
        public INamedTypeSymbol? BaseClass { get; init; }  // Added this property
    }

    internal record PropertyDetails(
        IPropertySymbol Symbol,
        PropertyConfiguration Configuration
    );

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

    internal record NavigationPropertyDetails(
        IPropertySymbol Symbol,
        INamedTypeSymbol TypeSymbol,
        bool IsCollection,
        bool IsNullable
    );

    internal record AssemblyConfiguration
    {
        public string DefaultNamespace { get; init; } = "Specifications";
        public bool DefaultGenerateAsync { get; init; }
        public bool DefaultGenerateDocumentation { get; init; }
        public bool DefaultGenerateNavigationSpecs { get; init; }
        public string DefaultStringComparison { get; init; } = "OrdinalIgnoreCase";
    }

    public enum OrmTarget
    {
        EntityFrameworkCore,
        Dapper,
        Both
    }
}