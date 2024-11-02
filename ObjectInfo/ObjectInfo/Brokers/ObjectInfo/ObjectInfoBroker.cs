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
        public IObjInfo GetObjectInfo(object obj, IConfigInfo configuration = null)
        {
            Type type = obj is Type t ? t : obj.GetType();
            ObjInfo.ObjInfo objInfo = new ObjInfo.ObjInfo
            {
                Configuration = configuration ?? new ConfigInfo(),
                TypeInfo = GetTypeInfo(type)
            };

            try
            {
                var instance = obj is Type ? null : obj;
                GetTypeProps(instance, objInfo, type.GetProperties());
                GetTypeMethods(objInfo, type, type.GetMethods());
                GetTypeConstructors(objInfo, type, type.GetConstructors());
                GetTypeFields(instance, objInfo, type.GetFields());
                GetTypeEvents(objInfo, type, type.GetEvents());
                GetTypelIntfcs(objInfo, type.GetInterfaces());
                GetTypeAttrs(objInfo, type.GetCustomAttributes(false));
                GetTypeGenericInfo(objInfo, type);
            }
            catch
            {
                // Log error but return what we have
            }

            return objInfo;
        }

        private void GetTypeProps(object obj, ObjInfo.ObjInfo objInfo, PropertyInfo[] propInfos)
        {
            foreach (var prop in propInfos)
            {
                try
                {
                    if (obj is Type)
                    {
                        // If obj is a Type, add property info without values
                        var propInfo = new PropInfo
                        {
                            Name = prop.Name,
                            CanWrite = prop.CanWrite,
                            CanRead = prop.CanRead,
                            PropertyType = prop.PropertyType?.Name,
                            DeclaringType = prop.DeclaringType?.Name,
                            ReflectedType = prop.ReflectedType?.Name,
                            CustomAttrs = new List<ITypeInfo>()
                        };
                        objInfo.TypeInfo.PropInfos.Add(propInfo);
                    }
                    else
                    {
                        // Keep existing behavior for actual instances
                        objInfo.TypeInfo.PropInfos.Add(GetPropInfo(objInfo, obj, prop));
                    }
                }
                catch (Exception)
                {
                    // Log but continue to next property
                }
            }
        }

        private ITypeInfo GetTypeInfo(object obj)
        {
            Type type;
            if (obj is Type t)
            {
                type = t;
            }
            else
            {
                type = obj.GetType();
            }

            System.Reflection.TypeInfo typeInfo = type.GetTypeInfo();
            Models.TypeInfo.TypeInfo modelTypeInfo = new Models.TypeInfo.TypeInfo(typeInfo)
            {
                PropInfos = new List<IPropInfo>(),
                MethodInfos = new List<IMethodInfo>(),
                ImplementedInterfaces = new List<ITypeInfo>(),
                CustomAttrs = new List<ITypeInfo>(),
                ConstructorInfos = new List<IConstructorInfo>(),
                FieldInfos = new List<IFieldInfo>(),
                GenericParameters = new List<IGenericParameterInfo>(),
                GenericTypeArguments = new List<ITypeInfo>(),
                Namespace = typeInfo.Namespace,
                Name = typeInfo.Name,
                Assembly = typeInfo.Assembly?.FullName,
                AssemblyQualifiedName = typeInfo.AssemblyQualifiedName,
                FullName = typeInfo.FullName,
                BaseType = typeInfo.BaseType?.Name,
                Module = typeInfo.Module?.Name,
                GUID = typeInfo.GUID,
                UnderlyingSystemType = typeInfo.UnderlyingSystemType?.Name,
                IsAbstract = typeInfo.IsAbstract,
                IsGenericTypeDefinition = typeInfo.IsGenericTypeDefinition,
                IsConstructedGenericType = type.IsGenericType && !type.IsGenericTypeDefinition
            };

            if (type.IsGenericType)
            {
                PopulateGenericTypeInfo(type, modelTypeInfo);
            }

            return modelTypeInfo;
        }

        private void PopulateGenericTypeInfo(Type type, Models.TypeInfo.TypeInfo modelTypeInfo)
        {
            if (type.IsGenericTypeDefinition)
            {
                modelTypeInfo.IsGenericTypeDefinition = true;
                var genericParams = type.GetGenericArguments();
                foreach (var param in genericParams)
                {
                    var paramInfo = new GenericParameterInfo(param, t => GetTypeInfo(t));
                    modelTypeInfo.GenericParameters.Add(paramInfo);
                }
            }
            else if (type.IsGenericType)
            {
                modelTypeInfo.IsConstructedGenericType = true;
                var genericArgs = type.GetGenericArguments();

                foreach (var arg in genericArgs)
                {
                    var argTypeInfo = new Models.TypeInfo.TypeInfo(arg.GetTypeInfo())
                    {
                        Name = arg.Name,
                        FullName = arg.FullName,
                        Namespace = arg.Namespace,
                        Assembly = arg.Assembly?.FullName,
                        AssemblyQualifiedName = arg.AssemblyQualifiedName
                    };
                    modelTypeInfo.GenericTypeArguments.Add(argTypeInfo);
                }

                var genericTypeDef = type.GetGenericTypeDefinition();
                modelTypeInfo.GenericTypeDefinition = genericTypeDef.Name;
            }
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
            // This method can now be empty since we handle generic info in GetTypeInfo
            // Or you can remove this method entirely and remove its call from GetObjectInfo
        }

        //private void GetTypeGenericInfo(ObjInfo.ObjInfo objInfo, Type type)
        //{
        //    if (type.IsGenericTypeDefinition)
        //    {
        //        objInfo.TypeInfo.IsGenericTypeDefinition = true;
        //        var genericParams = type.GetGenericArguments();
        //        foreach (var param in genericParams)
        //        {
        //            var paramInfo = new GenericParameterInfo(param, t => GetTypeInfo(t));
        //            objInfo.TypeInfo.GenericParameters.Add(paramInfo);
        //        }
        //    }
        //    else if (type.IsGenericType)
        //    {
        //        objInfo.TypeInfo.IsConstructedGenericType = true;
        //        var genericArgs = type.GetGenericArguments();
        //        foreach (var arg in genericArgs)
        //        {
        //            if (!objInfo.Configuration.ShowSystemInfo && arg.Namespace?.StartsWith("System") == true)
        //                continue;

        //            objInfo.TypeInfo.GenericTypeArguments.Add(GetTypeInfo(arg));
        //        }

        //        var genericTypeDef = type.GetGenericTypeDefinition();
        //        objInfo.TypeInfo.GenericTypeDefinition = genericTypeDef.Name;
        //    }
        //}


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
            var fieldInfo = new FieldInfoModel(_fieldInfo, obj is Type ? null : obj);
            fieldInfo.CustomAttrs = new List<ITypeInfo>();

            try
            {
                if (obj != null && !(obj is Type))
                {
                    fieldInfo.Value = _fieldInfo.GetValue(obj);
                }
            }
            catch
            {
                // Log but continue
            }

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
            if (objInfo?.TypeInfo?.MethodInfos == null)
                return;

            foreach (var methodInfo in methodInfos)
            {
                try
                {
                    if (methodInfo == null)
                        continue;

                    // Skip if we're not showing system info and this is not from the target type
                    if (objInfo.Configuration.ShowSystemInfo == false)
                    {
                        if (methodInfo.DeclaringType?.Name != type.Name)
                            continue;

                        // Skip property accessors
                        if (methodInfo.Name.StartsWith("get_") || methodInfo.Name.StartsWith("set_"))
                            continue;
                    }

                    var info = GetMethodInfo(methodInfo);
                    if (info != null)
                    {
                        objInfo.TypeInfo.MethodInfos.Add(info);
                    }
                }
                catch
                {
                    // Continue with next method if there's an error
                    continue;
                }
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
            if (_methodInfo == null)
                return null;

            try
            {
                var methodInfo = new Models.MethodInfo.MethodInfo(_methodInfo)
                {
                    Name = _methodInfo.Name,
                    ReflectedType = _methodInfo.ReflectedType?.Name,
                    DeclaringType = _methodInfo.DeclaringType?.Name,
                    CustomAttrs = new List<ITypeInfo>(),
                    IsVirtual = _methodInfo.IsVirtual
                };

                if (_methodInfo.DeclaringType != null)
                {
                    var myMembers = _methodInfo.DeclaringType.GetMembers();

                    foreach (var member in myMembers)
                    {
                        try
                        {
                            if (member.DeclaringType != _methodInfo.DeclaringType ||
                                member.DeclaringType.AssemblyQualifiedName != _methodInfo.DeclaringType.AssemblyQualifiedName)
                                continue;

                            var attrs = member.GetCustomAttributes(false);
                            foreach (var attr in attrs)
                            {
                                if (attr is Attribute attribute &&
                                    !attribute.GetType().Namespace.StartsWith("System") &&
                                    !methodInfo.CustomAttrs.Any(a => a.Name.Equals(GetTypeInfo(attribute).Name)))
                                {
                                    methodInfo.CustomAttrs.Add(GetTypeInfo(attribute));
                                }
                            }
                        }
                        catch
                        {
                            // Continue with next member if there's an error
                            continue;
                        }
                    }
                }

                return methodInfo;
            }
            catch
            {
                return null;
            }
        }


    }

}

