#region Copyright (c) The Rebel Alliance
// ----------------------------------------------------------------------------------
// Copyright (c) The Rebel Alliance
// [Copyright ASCII art would be here]
// ---------------------------------------------------------------------------------- 
#endregion

using ObjectInfo.Models.TypeInfo;
using System.Collections.Generic;

namespace ObjectInfo.Models.FieldInfo
{
    /// <summary>
    /// Defines the contract for field information.
    /// </summary>
    public interface IFieldInfo
    {
        /// <summary>
        /// Gets or sets the name of the field.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the declaring type name.
        /// </summary>
        string DeclaringType { get; set; }

        /// <summary>
        /// Gets or sets the field type name.
        /// </summary>
        string FieldType { get; set; }

        /// <summary>
        /// Gets or sets the field's value if it's a constant.
        /// </summary>
        object Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the field is static.
        /// </summary>
        bool IsStatic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the field is readonly.
        /// </summary>
        bool IsReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the field is a constant.
        /// </summary>
        bool IsConstant { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the field is init-only.
        /// </summary>
        bool IsInitOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the field is public.
        /// </summary>
        bool IsPublic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the field is private.
        /// </summary>
        bool IsPrivate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the field is protected.
        /// </summary>
        bool IsProtected { get; set; }

        /// <summary>
        /// Gets or sets custom attributes associated with the field.
        /// </summary>
        List<ITypeInfo> CustomAttrs { get; set; }
    }
}
