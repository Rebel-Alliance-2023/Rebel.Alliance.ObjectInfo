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
using System.Linq;
using System.Reflection;

namespace ObjectInfo.Models.TypeInfo
{
    public class TypeInfo : ITypeInfo
    {
        public string Assembly { get; set; }
        public string AssemblyQualifiedName { get; set; }
        public string BaseType { get; set; }
        public string FullName { get; set; }
        public Guid GUID { get; set; }
        public string Module { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string UnderlyingSystemType { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsGenericTypeDefinition { get; set; }
        public bool IsConstructedGenericType { get; set; }
        public bool IsGenericParameter { get; set; }
        public string GenericTypeDefinition { get; set; }
        public List<IGenericParameterInfo> GenericParameters { get; set; }
        public List<ITypeInfo> GenericTypeArguments { get; set; }
        public List<ITypeInfo> CustomAttrs { get; set; }
        public List<IPropInfo> PropInfos { get; set; }
        public List<IMethodInfo> MethodInfos { get; set; }
        public List<ITypeInfo> ImplementedInterfaces { get; set; }
        public List<IConstructorInfo> ConstructorInfos { get; set; }
        public List<IFieldInfo> FieldInfos { get; set; }
        public List<IEventInfo> EventInfos { get; set; }

        public TypeInfo(System.Reflection.TypeInfo typeInfo)
        {
            Initialize();
            PopulateFromTypeInfo(typeInfo);
        }

        public TypeInfo(Type type) : this(type.GetTypeInfo())
        {
            // No additional logic needed - all handled in PopulateFromTypeInfo
        }

        private void Initialize()
        {
            CustomAttrs = new List<ITypeInfo>();
            PropInfos = new List<IPropInfo>();
            MethodInfos = new List<IMethodInfo>();
            ImplementedInterfaces = new List<ITypeInfo>();
            ConstructorInfos = new List<IConstructorInfo>();
            FieldInfos = new List<IFieldInfo>();
            GenericParameters = new List<IGenericParameterInfo>();
            GenericTypeArguments = new List<ITypeInfo>();
            EventInfos = new List<IEventInfo>();
        }

        private void PopulateFromTypeInfo(System.Reflection.TypeInfo typeInfo)
        {
            // Basic type information
            IsAbstract = typeInfo.IsAbstract;
            Namespace = typeInfo.Namespace;
            Name = typeInfo.Name;
            Assembly = typeInfo.Assembly?.FullName;
            AssemblyQualifiedName = typeInfo.AssemblyQualifiedName;
            FullName = typeInfo.FullName;
            BaseType = typeInfo.BaseType?.Name;
            Module = typeInfo.Module?.Name;
            GUID = typeInfo.GUID;
            UnderlyingSystemType = typeInfo.UnderlyingSystemType?.Name;

            // Generic type information
            IsGenericTypeDefinition = typeInfo.IsGenericTypeDefinition;
            IsConstructedGenericType = typeInfo.IsGenericType && !typeInfo.IsGenericTypeDefinition;
            IsGenericParameter = typeInfo.IsGenericParameter;

            if (IsGenericTypeDefinition)
            {
                var genericParams = typeInfo.GetGenericArguments();
                foreach (var param in genericParams)
                {
                    GenericParameters.Add(new GenericParameterInfo(param, t => new TypeInfo(t.GetTypeInfo())));
                }
            }

            if (IsConstructedGenericType)
            {
                var genericTypeDef = typeInfo.GetGenericTypeDefinition();
                GenericTypeDefinition = genericTypeDef.Name;

                foreach (var argType in typeInfo.GenericTypeArguments)
                {
                    GenericTypeArguments.Add(new TypeInfo(argType.GetTypeInfo()));
                }
            }

            // Initialize member information
            MethodInfos = typeInfo.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)
                              .Select(m => new MethodInfo.MethodInfo(m) as IMethodInfo)
                              .ToList();

            ConstructorInfos = typeInfo.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)
                                  .Select(c => new ConstructorInfo.ConstructorInfo(c) as IConstructorInfo)
                                  .ToList();

            FieldInfos = FieldInfo.FieldInfo.CreateMany(
                typeInfo.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)
            );

            EventInfos = EventInfo.EventInfo.CreateMany(
                typeInfo.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic),
                m => new MethodInfo.MethodInfo(m),
                t => new TypeInfo(t)
            );
        }
    }
}
