using System;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Attributes
{

    /// <summary>
    /// Configures generation of the specification class
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class SpecificationAttributes : Attribute
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
