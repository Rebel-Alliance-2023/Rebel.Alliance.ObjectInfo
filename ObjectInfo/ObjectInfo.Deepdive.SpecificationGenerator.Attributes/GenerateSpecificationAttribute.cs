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
}
