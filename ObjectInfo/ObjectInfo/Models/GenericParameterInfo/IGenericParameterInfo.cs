#region Copyright (c) The Rebel Alliance
// [License region to be replaced]
#endregion

using System;
using System.Collections.Generic;
using ObjectInfo.Models.TypeInfo;

namespace ObjectInfo.Models.GenericInfo
{
    /// <summary>
    /// Defines the contract for generic type parameter information.
    /// </summary>
    public interface IGenericParameterInfo
    {
        /// <summary>
        /// Gets or sets the name of the generic parameter.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the position of the parameter in the generic argument list.
        /// </summary>
        int Position { get; set; }

        /// <summary>
        /// Gets or sets the declaring type name.
        /// </summary>
        string DeclaringType { get; set; }

        /// <summary>
        /// Gets or sets the variance of the generic parameter (in, out, or invariant).
        /// </summary>
        GenericParameterVariance Variance { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter has a reference type constraint.
        /// </summary>
        bool HasReferenceTypeConstraint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter has a value type constraint.
        /// </summary>
        bool HasValueTypeConstraint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter has a default constructor constraint.
        /// </summary>
        bool HasDefaultConstructorConstraint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter is covariant (out).
        /// </summary>
        bool IsCovariant { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter is contravariant (in).
        /// </summary>
        bool IsContravariant { get; set; }

        /// <summary>
        /// Gets or sets the list of type constraints on the generic parameter.
        /// </summary>
        List<ITypeInfo> Constraints { get; set; }

        /// <summary>
        /// Gets or sets custom attributes associated with the generic parameter.
        /// </summary>
        List<ITypeInfo> CustomAttrs { get; set; }
    }

    /// <summary>
    /// Represents the variance of a generic type parameter.
    /// </summary>
    public enum GenericParameterVariance
    {
        /// <summary>
        /// The type parameter is invariant.
        /// </summary>
        None,

        /// <summary>
        /// The type parameter is covariant (out).
        /// </summary>
        Covariant,

        /// <summary>
        /// The type parameter is contravariant (in).
        /// </summary>
        Contravariant
    }
}
