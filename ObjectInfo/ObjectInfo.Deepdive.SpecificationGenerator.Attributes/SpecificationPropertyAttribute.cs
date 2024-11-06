namespace ObjectInfo.Deepdive.SpecificationGenerator.Attributes
{
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
}
