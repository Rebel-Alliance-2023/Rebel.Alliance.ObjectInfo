#region Copyright (c) The Rebel Alliance
// [License region to be replaced]
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ObjectInfo.Models.TypeInfo;

namespace ObjectInfo.Models.GenericInfo
{
    /// <summary>
    /// Represents information about a generic type parameter.
    /// </summary>
    public class GenericParameterInfo : IGenericParameterInfo
    {
        public string Name { get; set; }
        public int Position { get; set; }
        public string DeclaringType { get; set; }
        public GenericParameterVariance Variance { get; set; }
        public bool HasReferenceTypeConstraint { get; set; }
        public bool HasValueTypeConstraint { get; set; }
        public bool HasDefaultConstructorConstraint { get; set; }
        public bool IsCovariant { get; set; }
        public bool IsContravariant { get; set; }
        public List<ITypeInfo> Constraints { get; set; }
        public List<ITypeInfo> CustomAttrs { get; set; }

        /// <summary>
        /// Initializes a new instance of the GenericParameterInfo class.
        /// </summary>
        public GenericParameterInfo()
        {
            Constraints = new List<ITypeInfo>();
            CustomAttrs = new List<ITypeInfo>();
            Variance = GenericParameterVariance.None;
        }

        /// <summary>
        /// Initializes a new instance of the GenericParameterInfo class from a Type representing a generic parameter.
        /// </summary>
        /// <param name="genericParameter">The Type object representing the generic parameter.</param>
        /// <param name="getTypeInfo">Function to convert System.Type to ITypeInfo.</param>
        public GenericParameterInfo(Type genericParameter, Func<Type, ITypeInfo> getTypeInfo)
        {
            if (!genericParameter.IsGenericParameter)
                throw new ArgumentException("Type must be a generic parameter", nameof(genericParameter));

            Name = genericParameter.Name;
            Position = genericParameter.GenericParameterPosition;
            DeclaringType = genericParameter.DeclaringType?.Name;
            Constraints = new List<ITypeInfo>();
            CustomAttrs = new List<ITypeInfo>();

            // Get generic parameter attributes
            var attributes = genericParameter.GenericParameterAttributes;

            // Check variance
            if ((attributes & GenericParameterAttributes.Covariant) != 0)
            {
                Variance = GenericParameterVariance.Covariant;
                IsCovariant = true;
            }
            else if ((attributes & GenericParameterAttributes.Contravariant) != 0)
            {
                Variance = GenericParameterVariance.Contravariant;
                IsContravariant = true;
            }
            else
            {
                Variance = GenericParameterVariance.None;
            }

            // Check special constraints
            HasReferenceTypeConstraint = (attributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0;
            HasValueTypeConstraint = (attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
            HasDefaultConstructorConstraint = (attributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0;

            // Get type constraints
            var constraints = genericParameter.GetGenericParameterConstraints();
            foreach (var constraint in constraints)
            {
                if (getTypeInfo != null)
                {
                    var constraintInfo = getTypeInfo(constraint);
                    if (constraintInfo != null)
                    {
                        Constraints.Add(constraintInfo);
                    }
                }
            }

            // Get custom attributes
            var customAttributes = genericParameter.GetCustomAttributes(false);
            foreach (var attr in customAttributes)
            {
                if (getTypeInfo != null && !attr.GetType().Namespace.StartsWith("System"))
                {
                    var attrInfo = getTypeInfo(attr.GetType());
                    if (attrInfo != null)
                    {
                        CustomAttrs.Add(attrInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a collection of GenericParameterInfo instances from an array of Types.
        /// </summary>
        /// <param name="genericParameters">The array of generic parameter types.</param>
        /// <param name="getTypeInfo">Function to convert System.Type to ITypeInfo.</param>
        /// <returns>A list of GenericParameterInfo instances.</returns>
        public static List<IGenericParameterInfo> CreateMany(Type[] genericParameters, Func<Type, ITypeInfo> getTypeInfo)
        {
            return genericParameters
                .Where(t => t.IsGenericParameter)
                .Select(t => new GenericParameterInfo(t, getTypeInfo))
                .Cast<IGenericParameterInfo>()
                .ToList();
        }
    }
}
