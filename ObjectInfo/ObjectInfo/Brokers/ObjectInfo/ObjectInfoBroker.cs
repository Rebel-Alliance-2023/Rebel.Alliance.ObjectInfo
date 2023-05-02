#region Copyright (c) The Rebel Alliance
// ----------------------------------------------------------------------------------
// Copyright (c) The Rebel Alliance
//⠀⠀⠀⠀⠀⠀⠀⠀⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠠⠀⠀⠀⡇⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠈⠳⣴⣿⠄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠒⠒⠒⠒⠒⢺⢿⣿⢗⠒⠒⠒⠒⠒⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠁⣸⣿⣦⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⢀⣾⡟⠋⢹⣷⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⢀⣿⡟⣴⣶⡄⣿⣧⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⢰⣿⣿⣧⢻⣿⣿⣿⣿⡟⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠈⢻⣿⣿⣷⣿⣿⣿⣿⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠿⣿⣿⣿⣿⣿⣿⣦⣤⣀⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⣾⠀⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣶⣤⡀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⣿⣏⣻⣿⣿⣿⣿⣿⠋⣿⣿⣿⣿⣿⠙⣿⣷⣶⣤⣤⡄
//⠀⠀⠀⠀⢻⢇⣿⣿⣿⣿⣿⠹⠀⢹⣿⣿⣿⡇⠀⢟⣿⣿⡿⠋⠀
//⠀⠀⠀⠀⢘⣼⣿⣿⣿⣿⣿⡆⠀⢸⣿⠛⣿⡇⠀⢸⡿⠋⠀⠀⠀
//⠀⠀⠀⠀⣾⣿⣿⣿⣿⣿⣿⣿⣦⣈⠻⠴⠟⣁⣴⣿⣿⠗⠀⠀⠀
//⠀⠀⠀⢸⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠋⠀⠀⠀⠀
//⠀⠀⢀⣿⣿⠻⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠟⠁⠀⠀⠀⠀⠀
//⠀⠀⣾⣿⡏⠀⠹⣿⠿⠿⠿⠿⣿⣿⣿⠿⠛⠁⠀⠀⠀⠀⠀⠀⠀
//⠀⢰⣿⡿⠀⠀⠀⠀⠀⠀⠀⠀⠀⢿⣿⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⣿⣿⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠘⣿⣧⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⣰⣿⡏⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢿⣿⣄⠀⠀⠀⠀⠀⠀⠀⠀
//⠉⠉⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠉⠉⠀
// ---------------------------------------------------------------------------------- 
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ObjectInfo.Models.MethodInfo;
using objInfo = ObjectInfo.Models.ObjectInfo;
using ObjectInfo.Models.PropInfo;
using ObjectInfo.Models.TypeInfo;

namespace ObjectInfo.Brokers.ObjectInfo
{
    public class ObjectInfoBroker : IObjectInfoBroker
    {
        public objInfo.IObjInfo GetObjectInfo(object obj)
        {
            objInfo.ObjInfo objInfo = new objInfo.ObjInfo();
            objInfo.PropInfos = new List<IPropInfo>();
            objInfo.MethodInfos = new List<IMethodInfo>();
            objInfo.ImplementedInterfaces = new List<ITypeInfo>();
            Type type = obj.GetType();
            var propInfos = type.GetProperties();
            var methodInfos = type.GetMethods();
            var intfcs = type.GetInterfaces();
            var attrs = type.GetCustomAttributes(false);

            foreach (var prop in propInfos)
            {
                objInfo.PropInfos.Add(GetPropInfo(obj, prop));
            }

            foreach (var methodInfo in methodInfos)
            {
                if (methodInfo.DeclaringType.Name != type.Name)
                    continue;
                if (methodInfo.Name.StartsWith("get_") || methodInfo.Name.StartsWith("set_"))
                    continue;

                objInfo.MethodInfos.Add(GetMethodInfo(methodInfo));
            }

            foreach (var intfc in intfcs)
            {
                objInfo.ImplementedInterfaces.Add(GetIntfcTypeInfo(intfc));
            }

            objInfo.TypeInfo = GetTypeInfo(obj);

            foreach (var attr in attrs)
            {
                if (attr.GetType().Namespace.StartsWith("System"))
                    continue;
                objInfo.TypeInfo.CustomAttrs.Add(GetTypeInfo(attr));
            }

            return objInfo;
        }

        private ITypeInfo GetIntfcTypeInfo(Type intfcType)
        {
            System.Reflection.TypeInfo typeInfo = intfcType.GetTypeInfo();
            Models.TypeInfo.TypeInfo modeltypeInfo = new Models.TypeInfo.TypeInfo();
            modeltypeInfo.Namespace = typeInfo.Namespace;
            modeltypeInfo.Name = typeInfo.Name;
            modeltypeInfo.Assembly = typeInfo.Assembly;
            modeltypeInfo.AssemblyQualifiedName = typeInfo.AssemblyQualifiedName;
            modeltypeInfo.FullName = typeInfo.FullName;
            modeltypeInfo.BaseType = typeInfo.BaseType;
            modeltypeInfo.Module = typeInfo.Module;
            modeltypeInfo.GUID = typeInfo.GUID;
            modeltypeInfo.UnderlyingSystemType = typeInfo.UnderlyingSystemType;

            return modeltypeInfo;
        }

        private ITypeInfo GetTypeInfo(object obj)
        {
            System.Reflection.TypeInfo typeInfo = obj.GetType().GetTypeInfo();
            Models.TypeInfo.TypeInfo modeltypeInfo = new Models.TypeInfo.TypeInfo();
            modeltypeInfo.CustomAttrs = new List<ITypeInfo>();
            modeltypeInfo.Namespace = typeInfo.Namespace;
            modeltypeInfo.Name = typeInfo.Name;
            modeltypeInfo.Assembly = typeInfo.Assembly;
            modeltypeInfo.AssemblyQualifiedName = typeInfo.AssemblyQualifiedName;
            modeltypeInfo.FullName = typeInfo.FullName;
            modeltypeInfo.BaseType = typeInfo.BaseType;
            modeltypeInfo.Module = typeInfo.Module;
            modeltypeInfo.GUID = typeInfo.GUID;
            modeltypeInfo.UnderlyingSystemType = typeInfo.UnderlyingSystemType;

            return modeltypeInfo;
        }

        public IPropInfo GetPropInfo(object obj, PropertyInfo _propInfo)
        {
            PropInfo propInfo = new PropInfo();
            propInfo.Name = _propInfo.Name;
            propInfo.CanWrite = _propInfo.CanWrite;
            propInfo.CanRead = _propInfo.CanRead;
            propInfo.Value = _propInfo.GetValue(obj);
            propInfo.PropertyType = _propInfo.PropertyType;
            propInfo.DeclaringType = _propInfo.DeclaringType;
            propInfo.ReflectedType = _propInfo.ReflectedType;
            propInfo.CustomAttrs = new List<ITypeInfo>();
            var attrs = _propInfo.GetCustomAttributes(false);

            foreach (var attr in attrs)
            {
                if (attr.GetType().Namespace.StartsWith("System"))
                    continue;
                propInfo.CustomAttrs.Add(GetTypeInfo(attr));
            }

            return propInfo;
        }

        public IMethodInfo GetMethodInfo(System.Reflection.MethodInfo _methodInfo)
        {
            Models.MethodInfo.MethodInfo methodInfo = new Models.MethodInfo.MethodInfo();
            methodInfo.Name = _methodInfo.Name;
            methodInfo.ReflectedType = _methodInfo.ReflectedType;
            methodInfo.DeclaringType = _methodInfo.DeclaringType;
            methodInfo.CustomAttrs = new List<ITypeInfo>();
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



