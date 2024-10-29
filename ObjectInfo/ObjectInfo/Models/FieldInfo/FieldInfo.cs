#region Copyright (c) The Rebel Alliance
// ----------------------------------------------------------------------------------
// Copyright (c) The Rebel Alliance
// [Copyright ASCII art would be here]
// ---------------------------------------------------------------------------------- 
#endregion

using ObjectInfo.Models.TypeInfo;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ObjectInfo.Models.FieldInfo
{
    /// <summary>
    /// Represents field metadata from reflection.
    /// </summary>
    public class FieldInfo : IFieldInfo
    {
        public string Name { get; set; }
        public string DeclaringType { get; set; }
        public string FieldType { get; set; }
        public object Value { get; set; }
        public bool IsStatic { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsConstant { get; set; }
        public bool IsInitOnly { get; set; }
        public bool IsPublic { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsProtected { get; set; }
        public List<ITypeInfo> CustomAttrs { get; set; }

        /// <summary>
        /// Initializes a new instance of the FieldInfo class.
        /// </summary>
        public FieldInfo()
        {
            CustomAttrs = new List<ITypeInfo>();
        }

        /// <summary>
        /// Initializes a new instance of the FieldInfo class from a System.Reflection.FieldInfo.
        /// </summary>
        /// <param name="fieldInfo">The reflection field info to wrap.</param>
        /// <param name="instance">Optional instance to get field value from for non-static fields.</param>
        public FieldInfo(System.Reflection.FieldInfo fieldInfo, object instance = null)
        {
            Name = fieldInfo.Name;
            DeclaringType = fieldInfo.DeclaringType?.Name;
            FieldType = fieldInfo.FieldType.Name;
            CustomAttrs = new List<ITypeInfo>();

            // Field modifiers
            IsStatic = fieldInfo.IsStatic;
            IsReadOnly = fieldInfo.IsInitOnly;
            IsConstant = fieldInfo.IsLiteral;
            IsInitOnly = fieldInfo.IsInitOnly;

            // Accessibility
            IsPublic = fieldInfo.IsPublic;
            IsPrivate = fieldInfo.IsPrivate;
            IsProtected = fieldInfo.IsFamily;

            // Handle field value
            try
            {
                if (IsConstant)
                {
                    // Get constant value
                    Value = fieldInfo.GetRawConstantValue();
                }
                else if (IsStatic)
                {
                    // Get static field value
                    Value = fieldInfo.GetValue(null);
                }
                else if (instance != null)
                {
                    // Get instance field value if instance provided
                    Value = fieldInfo.GetValue(instance);
                }
            }
            catch (Exception)
            {
                // If we can't get the value for any reason, leave it as null
                Value = null;
            }
        }

        /// <summary>
        /// Creates a FieldInfo instance for a field, including handling retrieval of its value.
        /// </summary>
        /// <param name="fieldInfo">The reflection field info.</param>
        /// <param name="instance">Optional instance for non-static field values.</param>
        /// <param name="includeNonPublic">Whether to include non-public fields.</param>
        /// <returns>A FieldInfo instance or null if the field should be excluded.</returns>
        public static FieldInfo Create(System.Reflection.FieldInfo fieldInfo, object instance = null, bool includeNonPublic = false)
        {
            // Skip compiler-generated backing fields unless specifically included
            if (fieldInfo.Name.StartsWith("<") && fieldInfo.Name.EndsWith(">k__BackingField"))
            {
                return null;
            }

            // Skip non-public fields unless specifically included
            if (!includeNonPublic && !fieldInfo.IsPublic)
            {
                return null;
            }

            return new FieldInfo(fieldInfo, instance);
        }

        /// <summary>
        /// Creates a collection of FieldInfo instances from reflection field infos.
        /// </summary>
        /// <param name="fieldInfos">The reflection field infos to process.</param>
        /// <param name="instance">Optional instance for non-static field values.</param>
        /// <param name="includeNonPublic">Whether to include non-public fields.</param>
        /// <returns>A list of FieldInfo instances.</returns>
        public static List<IFieldInfo> CreateMany(IEnumerable<System.Reflection.FieldInfo> fieldInfos, object instance = null, bool includeNonPublic = false)
        {
            var result = new List<IFieldInfo>();
            foreach (var fieldInfo in fieldInfos)
            {
                var field = Create(fieldInfo, instance, includeNonPublic);
                if (field != null)
                {
                    result.Add(field);
                }
            }
            return result;
        }
    }
}
