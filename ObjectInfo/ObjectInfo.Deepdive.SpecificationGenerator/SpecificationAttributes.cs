using ObjectInfo.Deepdive.SpecificationGenerator.Core.Models;
using System;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Attributes
{
    
    /// <summary>
    /// Marks a class for specification generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class GenerateSpecificationAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the target ORM for the specification
        /// </summary>
        public OrmTarget TargetOrm { get; set; } = OrmTarget.EntityFrameworkCore;

        /// <summary>
        /// Gets or sets whether to generate nested specifications for navigation properties
        /// </summary>
        public bool GenerateNavigationSpecs { get; set; } = true;

        /// <summary>
        /// Gets or sets the base class for the generated specification
        /// Default is BaseSpecification<T>
        /// </summary>
        public Type? BaseClass { get; set; }

        /// <summary>
        /// Gets or sets whether to include XML documentation in generated code
        /// </summary>
        public bool GenerateDocumentation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to generate async methods for Dapper specifications
        /// </summary>
        public bool GenerateAsyncMethods { get; set; } = true;

        /// <summary>
        /// Gets or sets the namespace for the generated specification
        /// If not specified, will use the entity's namespace + ".Specifications"
        /// </summary>
        public string? TargetNamespace { get; set; }
    }

    /// <summary>
    /// Configures specification generation for a property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class SpecificationPropertyAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether to exclude this property from specification generation
        /// </summary>
        public bool Ignore { get; set; }

        /// <summary>
        /// Gets or sets whether to generate string contains operation for string properties
        /// </summary>
        public bool GenerateContains { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to generate starts with operation for string properties
        /// </summary>
        public bool GenerateStartsWith { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to generate ends with operation for string properties
        /// </summary>
        public bool GenerateEndsWith { get; set; } = true;

        /// <summary>
        /// Gets or sets whether string comparisons should be case sensitive
        /// </summary>
        public bool CaseSensitive { get; set; }

        /// <summary>
        /// Gets or sets whether to generate range operations for numeric/datetime properties
        /// </summary>
        public bool GenerateRange { get; set; } = true;

        /// <summary>
        /// Gets or sets a custom expression template for the property
        /// Supports {value} placeholder for the property value
        /// Example: "LOWER({property}) LIKE LOWER({value})"
        /// </summary>
        public string? CustomExpression { get; set; }

        /// <summary>
        /// Gets or sets whether null checks should be generated for the property
        /// </summary>
        public bool GenerateNullChecks { get; set; } = true;
    }

    /// <summary>
    /// Configures generation of the specification class
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class SpecificationConfigurationAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the default namespace for generated specifications
        /// </summary>
        public string DefaultNamespace { get; set; } = "Specifications";

        /// <summary>
        /// Gets or sets whether to generate async methods by default
        /// </summary>
        public bool DefaultGenerateAsync { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include XML documentation by default
        /// </summary>
        public bool DefaultGenerateDocumentation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to generate navigation specifications by default
        /// </summary>
        public bool DefaultGenerateNavigationSpecs { get; set; } = true;

        /// <summary>
        /// Gets or sets the default string comparison for specifications
        /// </summary>
        public StringComparison DefaultStringComparison { get; set; } = StringComparison.OrdinalIgnoreCase;
    }
}
