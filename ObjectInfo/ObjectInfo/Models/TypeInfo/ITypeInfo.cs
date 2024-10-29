#region Copyright (c) The Rebel Alliance
// [License region to be replaced]
#endregion

using ObjectInfo.Models.MethodInfo;
using ObjectInfo.Models.PropInfo;
using ObjectInfo.Models.ConstructorInfo;
using ObjectInfo.Models.FieldInfo;
using ObjectInfo.Models.GenericInfo;
using ObjectInfo.Models.EventInfo;
using System;
using System.Collections.Generic;

namespace ObjectInfo.Models.TypeInfo
{
    public interface ITypeInfo
    {
        string Assembly { get; set; }
        string AssemblyQualifiedName { get; set; }
        string BaseType { get; set; }
        string FullName { get; set; }
        Guid GUID { get; set; }
        string Module { get; set; }
        string Name { get; set; }
        string Namespace { get; set; }
        string UnderlyingSystemType { get; set; }
        bool IsAbstract { get; set; }
        bool IsGenericTypeDefinition { get; set; }
        bool IsConstructedGenericType { get; set; }
        bool IsGenericParameter { get; set; }
        string GenericTypeDefinition { get; set; }
        List<IGenericParameterInfo> GenericParameters { get; set; }
        List<ITypeInfo> GenericTypeArguments { get; set; }
        List<ITypeInfo> CustomAttrs { get; set; }
        List<IMethodInfo> MethodInfos { get; set; }
        List<IPropInfo> PropInfos { get; set; }
        List<ITypeInfo> ImplementedInterfaces { get; set; }
        List<IConstructorInfo> ConstructorInfos { get; set; }
        List<IFieldInfo> FieldInfos { get; set; }

        /// <summary>
        /// Gets or sets the list of events defined in the type.
        /// </summary>
        List<IEventInfo> EventInfos { get; set; }
    }
}
