#region Copyright (c) The Rebel Alliance
// [License region to be replaced]
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ObjectInfo.Models.MethodInfo;
using ObjInfo = ObjectInfo.Models.ObjectInfo;
using ObjectInfo.Models.PropInfo;
using ObjectInfo.Models.TypeInfo;
using ObjectInfo.Models.ObjectInfo;
using ObjectInfo.Models.ConfigInfo;
using ObjectInfo.Models.ConstructorInfo;
using ObjectInfo.Models.FieldInfo;
using ObjectInfo.Models.GenericInfo;
using ObjectInfo.Models.EventInfo;
using ConstructorInfoModel = ObjectInfo.Models.ConstructorInfo.ConstructorInfo;
using FieldInfoModel = ObjectInfo.Models.FieldInfo.FieldInfo;
using EventInfoModel = ObjectInfo.Models.EventInfo.EventInfo;
using SystemConstructorInfo = System.Reflection.ConstructorInfo;
using SystemFieldInfo = System.Reflection.FieldInfo;
using SystemEventInfo = System.Reflection.EventInfo;

namespace ObjectInfo.Brokers.ObjectInfo
{
    public class ObjectInfoBroker : IObjectInfoBroker
    {
        public ObjInfo.IObjInfo GetObjectInfo(object obj, IConfigInfo configuration = null)
        {
            Type type = obj.GetType();
            ObjInfo.ObjInfo objInfo = new ObjInfo.ObjInfo
            {
                Configuration = configuration ?? new ConfigInfo(),
                TypeInfo = GetTypeInfo(obj)
            };

            GetTypeProps(obj, objInfo, type.GetProperties());
            GetTypeMethods(objInfo, type, type.GetMethods());
            GetTypeConstructors(objInfo, type, type.GetConstructors());
            GetTypeFields(obj, objInfo, type.GetFields());
            GetTypeEvents(objInfo, type, type.GetEvents());
            GetTypelIntfcs(objInfo, type.GetInterfaces());
            GetTypeAttrs(objInfo, type.GetCustomAttributes(false));
            GetTypeGenericInfo(objInfo, type);

            return objInfo;
        }

        private void GetTypeEvents(ObjInfo.ObjInfo objInfo, Type type, SystemEventInfo[] eventInfos)
        {
            foreach (var eventInfo in eventInfos)
            {
                if (objInfo.Configuration.ShowSystemInfo == false)
                {
                    if (eventInfo.DeclaringType?.Name != type.Name)
                        continue;
                }

                objInfo.TypeInfo.EventInfos.Add(GetEventInfo(objInfo, eventInfo));
            }
        }

        public IEventInfo GetEventInfo(ObjInfo.ObjInfo objInfo, SystemEventInfo _eventInfo)
        {
            var eventInfo = new EventInfoModel(
                _eventInfo,
                methodInfo => GetMethodInfo(methodInfo),
                type => GetTypeInfo(type.GetTypeInfo())
            );

            // Get custom attributes
            var attrs = _eventInfo.GetCustomAttributes(false);
            foreach (var attr in attrs)
            {
                if (objInfo.Configuration.ShowSystemInfo == false)
                {
                    if (attr.GetType().Namespace.StartsWith("System"))
                        continue;
                }
                eventInfo.CustomAttrs.Add(GetTypeInfo(attr.GetType().GetTypeInfo()));
            }

            return eventInfo;
        }



        private void GetTypeGenericInfo(ObjInfo.ObjInfo objInfo, Type type)
        {
            var typeInfo = type.GetTypeInfo();

            // Handle generic type definition
            if (typeInfo.IsGenericTypeDefinition)
            {
                var genericParams = typeInfo.GetGenericArguments();
                foreach (var param in genericParams)
                {
                    var paramInfo = new GenericParameterInfo(param, t => GetTypeInfo(t.GetTypeInfo()));
                    objInfo.TypeInfo.GenericParameters.Add(paramInfo);
                }
            }

            // Handle constructed generic type
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                var genericArgs = type.GetGenericArguments();
                foreach (var arg in genericArgs)
                {
                    if (!objInfo.Configuration.ShowSystemInfo && arg.Namespace?.StartsWith("System") == true)
                        continue;

                    objInfo.TypeInfo.GenericTypeArguments.Add(GetTypeInfo(arg.GetTypeInfo()));
                }

                var genericTypeDef = type.GetGenericTypeDefinition();
                objInfo.TypeInfo.GenericTypeDefinition = genericTypeDef.Name;
            }
        }

        private void GetTypeFields(object obj, ObjInfo.ObjInfo objInfo, SystemFieldInfo[] fieldInfos)
        {
            foreach (var fieldInfo in fieldInfos)
            {
                if (objInfo.Configuration.ShowSystemInfo == false)
                {
                    if (fieldInfo.DeclaringType?.Name != objInfo.TypeInfo.Name)
                        continue;

                    // Skip compiler-generated fields unless explicitly shown
                    if (fieldInfo.Name.StartsWith("<") && fieldInfo.Name.EndsWith(">k__BackingField"))
                        continue;
                }

                objInfo.TypeInfo.FieldInfos.Add(GetFieldInfo(objInfo, obj, fieldInfo));
            }
        }

        public IFieldInfo GetFieldInfo(ObjInfo.ObjInfo objInfo, object obj, SystemFieldInfo _fieldInfo)
        {
            var fieldInfo = new FieldInfoModel(_fieldInfo, obj);
            fieldInfo.CustomAttrs = new List<ITypeInfo>();

            // Get custom attributes
            var attrs = _fieldInfo.GetCustomAttributes(false);
            foreach (var attr in attrs)
            {
                if (objInfo.Configuration.ShowSystemInfo == false)
                {
                    if (attr.GetType().Namespace.StartsWith("System"))
                        continue;
                }
                fieldInfo.CustomAttrs.Add(GetTypeInfo(attr));
            }

            return fieldInfo;
        }


        private void GetTypeConstructors(ObjInfo.ObjInfo objInfo, Type type, System.Reflection.ConstructorInfo[] constructorInfos)
        {
            foreach (var constructorInfo in constructorInfos)
            {
                if(objInfo.Configuration.ShowSystemInfo == false)
                {
                    if (constructorInfo.DeclaringType?.Name != type.Name)
                        continue;
                }

                objInfo.TypeInfo.ConstructorInfos.Add(GetConstructorInfo(objInfo, constructorInfo));
            }
        }

        private void GetTypelIntfcs(ObjInfo.ObjInfo objInfo, Type[] intfcs)
        {
            foreach (var intfc in intfcs)
            {
                objInfo.TypeInfo.ImplementedInterfaces.Add(GetIntfcTypeInfo(intfc));
            }
        }

        private void GetTypeAttrs(ObjInfo.ObjInfo objInfo, object[] attrs)
        {
            foreach (var attr in attrs)
            {
                if (attr.GetType().Namespace.StartsWith("System"))
                    continue;
                objInfo.TypeInfo.CustomAttrs.Add(GetTypeInfo(attr));
            }
        }

        private void GetTypeMethods(ObjInfo.ObjInfo objInfo, Type type, System.Reflection.MethodInfo[] methodInfos)
        {
            foreach (var methodInfo in methodInfos)
            {
                if(objInfo.Configuration.ShowSystemInfo == false)
                {
                    if (methodInfo.DeclaringType.Name != type.Name)
                        continue;

                    if (methodInfo.Name.StartsWith("get_") || methodInfo.Name.StartsWith("set_"))
                        continue;
                }

                objInfo.TypeInfo.MethodInfos.Add(GetMethodInfo(methodInfo));
            }
        }

        private void GetTypeProps(object obj, ObjInfo.ObjInfo objInfo, PropertyInfo[] propInfos)
        {
            foreach (var prop in propInfos)
            {
                objInfo.TypeInfo.PropInfos.Add(GetPropInfo(objInfo, obj, prop));
            }
        }

        private ITypeInfo GetIntfcTypeInfo(Type intfcType)
        {
            System.Reflection.TypeInfo typeInfo = intfcType.GetTypeInfo();
            Models.TypeInfo.TypeInfo modeltypeInfo = new Models.TypeInfo.TypeInfo(typeInfo);
            modeltypeInfo.Namespace = typeInfo.Namespace;
            modeltypeInfo.Name = typeInfo.Name;
            modeltypeInfo.Assembly = typeInfo.Assembly != null ? typeInfo.Assembly.FullName : null;
            modeltypeInfo.AssemblyQualifiedName = typeInfo.AssemblyQualifiedName;
            modeltypeInfo.FullName = typeInfo.FullName;
            modeltypeInfo.BaseType = typeInfo.BaseType != null ? typeInfo.BaseType.Name : null;
            modeltypeInfo.Module = typeInfo.Module != null ? typeInfo.Module.Name : null;
            modeltypeInfo.GUID = typeInfo.GUID;
            modeltypeInfo.UnderlyingSystemType = typeInfo.UnderlyingSystemType != null ? typeInfo.UnderlyingSystemType.Name : null;
            modeltypeInfo.IsAbstract = intfcType.IsAbstract;

            return modeltypeInfo;
        }

        private ITypeInfo GetTypeInfo(object obj)
        {
            System.Reflection.TypeInfo typeInfo = obj.GetType().GetTypeInfo();
            Models.TypeInfo.TypeInfo modeltypeInfo = new Models.TypeInfo.TypeInfo(typeInfo);
            modeltypeInfo.PropInfos = new List<IPropInfo>();
            modeltypeInfo.MethodInfos = new List<IMethodInfo>();
            modeltypeInfo.ImplementedInterfaces = new List<ITypeInfo>();
            modeltypeInfo.CustomAttrs = new List<ITypeInfo>();
            modeltypeInfo.ConstructorInfos = new List<IConstructorInfo>();
            modeltypeInfo.FieldInfos = new List<IFieldInfo>();
            modeltypeInfo.Namespace = typeInfo.Namespace;
            modeltypeInfo.Name = typeInfo.Name;
            modeltypeInfo.Assembly = typeInfo.Assembly != null ? typeInfo.Assembly.FullName : null;
            modeltypeInfo.AssemblyQualifiedName = typeInfo.AssemblyQualifiedName;
            modeltypeInfo.FullName = typeInfo.FullName;
            modeltypeInfo.BaseType = typeInfo.BaseType != null ? typeInfo.BaseType.Name : null;
            modeltypeInfo.Module = typeInfo.Module.Name;
            modeltypeInfo.GUID = typeInfo.GUID;
            modeltypeInfo.UnderlyingSystemType = typeInfo.UnderlyingSystemType != null ? typeInfo.UnderlyingSystemType.Name : null;
            modeltypeInfo.IsAbstract = obj.GetType().IsAbstract;

            return modeltypeInfo;
        }

        public IPropInfo GetPropInfo(ObjInfo.ObjInfo objInfo, object obj, PropertyInfo _propInfo)
        {
            PropInfo propInfo = new PropInfo
            {
                Name = _propInfo.Name,
                CanWrite = _propInfo.CanWrite,
                CanRead = _propInfo.CanRead,
                PropertyType = _propInfo.PropertyType != null ? _propInfo.PropertyType.Name : null,
                DeclaringType = _propInfo.DeclaringType != null ? _propInfo.DeclaringType.Name : null,
                ReflectedType = _propInfo.ReflectedType != null ? _propInfo.ReflectedType.Name : null,
                CustomAttrs = new List<ITypeInfo>()
            };

            // Check if the property type is a generic parameter before getting its value
            if (!_propInfo.PropertyType.IsGenericParameter)
            {
                try
                {
                    propInfo.Value = _propInfo.GetValue(obj);
                }
                catch (TargetInvocationException ex)
                {
                    // Handle or log the exception as needed
                    Console.WriteLine($"Error getting value for property {_propInfo.Name}: {ex.InnerException?.Message}");
                    propInfo.Value = null;
                }
            }
            else
            {
                propInfo.Value = null;
            }

            var attrs = _propInfo.GetCustomAttributes(false);
            foreach (var attr in attrs)
            {
                if (objInfo.Configuration.ShowSystemInfo == false)
                {
                    if (attr.GetType().Namespace.StartsWith("System"))
                        continue;
                }
                propInfo.CustomAttrs.Add(GetTypeInfo(attr));
            }

            return propInfo;
        }

        public IConstructorInfo GetConstructorInfo(ObjInfo.ObjInfo objInfo, System.Reflection.ConstructorInfo _constructorInfo)
        {
            var constructorInfo = new ConstructorInfoModel(_constructorInfo);

            // Get custom attributes
            var attrs = _constructorInfo.GetCustomAttributes(false);
            foreach (var attr in attrs)
            {
                if (objInfo.Configuration.ShowSystemInfo == false)
                {
                    if (attr.GetType().Namespace.StartsWith("System"))
                        continue;
                }
                constructorInfo.CustomAttrs.Add(GetTypeInfo(attr));
            }

            return constructorInfo;
        }


        public IMethodInfo GetMethodInfo(System.Reflection.MethodInfo _methodInfo)
        {
            Models.MethodInfo.MethodInfo methodInfo = new Models.MethodInfo.MethodInfo(_methodInfo)
            {
                Name = _methodInfo.Name,
                ReflectedType = _methodInfo.ReflectedType?.Name,
                DeclaringType = _methodInfo.DeclaringType?.Name,
                CustomAttrs = new List<ITypeInfo>(),
                IsVirtual = _methodInfo.IsVirtual
            };

            MemberInfo[] myMembers = _methodInfo.DeclaringType.GetMembers();

            for (int i = 0; i < myMembers.Length; i++)
            {
                if (myMembers[i].DeclaringType != _methodInfo.DeclaringType ||
                    myMembers[i].DeclaringType.AssemblyQualifiedName != _methodInfo.DeclaringType.AssemblyQualifiedName)
                    continue;

                object[] attrs = myMembers[i].GetCustomAttributes(false);

                if (attrs.Length > 0)
                {
                    for (int j = 0; j < attrs.Length; j++)
                    {
                        Attribute attr = attrs[j] as Attribute;
                        if (attr.GetType().Namespace.StartsWith("System"))
                            continue;
                        if (!methodInfo.CustomAttrs.Any(a => a.Name.Equals(GetTypeInfo(attr).Name)))
                        {
                            methodInfo.CustomAttrs.Add(GetTypeInfo(attr));
                        }
                    }
                }
            }
            return methodInfo;
        }
    }

}

