using ObjectInfo.Models.TypeInfo;
using System.Collections.Generic;

namespace ObjectInfo.Models.ConstructorInfo
{
    /// <summary>
    /// Defines the contract for constructor information.
    /// </summary>
    public interface IConstructorInfo
    {
        /// <summary>
        /// Gets or sets the declaring type name.
        /// </summary>
        string DeclaringType { get; set; }

        /// <summary>
        /// Gets or sets the reflected type name.
        /// </summary>
        string ReflectedType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the constructor is static.
        /// </summary>
        bool IsStatic { get; set; }

        /// <summary>
        /// Gets or sets a list of parameter type names.
        /// </summary>
        List<string> ParameterTypes { get; set; }

        /// <summary>
        /// Gets or sets a list of parameter names.
        /// </summary>
        List<string> ParameterNames { get; set; }

        /// <summary>
        /// Gets or sets custom attributes associated with the constructor.
        /// </summary>
        List<ITypeInfo> CustomAttrs { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the constructor is public.
        /// </summary>
        bool IsPublic { get; set; }
    }
}
