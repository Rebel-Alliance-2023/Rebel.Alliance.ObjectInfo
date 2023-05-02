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
using ObjInfo = ObjectInfo.Models.ObjectInfo;
using ObjectInfo.Models.PropInfo;
using ObjectInfo.Models.TypeInfo;
using ObjectInfo.Models.ObjectInfo;
using ObjectInfo.Models.ConfigInfo;

namespace ObjectInfo.Brokers.ObjectInfo
{
    public class ObjectInfoBroker : IObjectInfoBroker
    {
        public ObjInfo.IObjInfo GetObjectInfo(object obj, IConfigInfo configuration=null)
        {
            Type type = obj.GetType();
            var propInfos = type.GetProperties();
            var methodInfos = type.GetMethods();
            var intfcs = type.GetInterfaces();
            var attrs = type.GetCustomAttributes(false);

            ObjInfo.ObjInfo objInfo = new ObjInfo.ObjInfo();
            objInfo.Configuration = configuration!=null ? configuration: new ConfigInfo();

            objInfo.TypeInfo = GetTypeInfo(obj);

            GetTypeProps(obj, objInfo, propInfos);
            GetTypeMethods(objInfo, type, methodInfos);
            GetTypelIntfcs(objInfo, intfcs);
            GetTypeAttrs(objInfo, attrs);

            return objInfo;
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
            modeltypeInfo.PropInfos = new List<IPropInfo>();
            modeltypeInfo.MethodInfos = new List<IMethodInfo>();
            modeltypeInfo.ImplementedInterfaces = new List<ITypeInfo>();
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

        public IPropInfo GetPropInfo(ObjInfo.ObjInfo objInfo, object obj, PropertyInfo _propInfo)
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
                if (objInfo.Configuration.ShowSystemInfo == false)
                {
                    if (attr.GetType().Namespace.StartsWith("System"))
                        continue;
                }
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



