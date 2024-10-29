#region Copyright (c) The Rebel Alliance
// [License region to be replaced]
#endregion

using ObjectInfo.Models.ConfigInfo;
using ObjectInfo.Models.MethodInfo;
using ObjectInfo.Models.ObjectInfo;
using ObjectInfo.Models.PropInfo;
using ObjectInfo.Models.ConstructorInfo;
using ObjectInfo.Models.FieldInfo;
using System.Reflection;

namespace ObjectInfo.Brokers.ObjectInfo
{
    public interface IObjectInfoBroker
    {
        /// <summary>
        /// Gets detailed information about an object including its type, methods, properties, constructors, and fields.
        /// </summary>
        /// <param name="obj">The object to analyze.</param>
        /// <param name="configuration">Optional configuration settings.</param>
        /// <returns>A detailed object information model.</returns>
        IObjInfo GetObjectInfo(object obj, IConfigInfo configuration = null);

        /// <summary>
        /// Gets detailed information about a method.
        /// </summary>
        /// <param name="_methodInfo">The reflection method info to analyze.</param>
        /// <returns>A method information model.</returns>
        IMethodInfo GetMethodInfo(System.Reflection.MethodInfo _methodInfo);

        /// <summary>
        /// Gets detailed information about a property.
        /// </summary>
        /// <param name="objInfo">The parent object info.</param>
        /// <param name="obj">The object containing the property.</param>
        /// <param name="_propInfo">The reflection property info to analyze.</param>
        /// <returns>A property information model.</returns>
        IPropInfo GetPropInfo(ObjInfo objInfo, object obj, PropertyInfo _propInfo);

        /// <summary>
        /// Gets detailed information about a constructor.
        /// </summary>
        /// <param name="objInfo">The parent object info.</param>
        /// <param name="_constructorInfo">The reflection constructor info to analyze.</param>
        /// <returns>A constructor information model.</returns>
        IConstructorInfo GetConstructorInfo(ObjInfo objInfo, System.Reflection.ConstructorInfo _constructorInfo);

        /// <summary>
        /// Gets detailed information about a field.
        /// </summary>
        /// <param name="objInfo">The parent object info.</param>
        /// <param name="obj">The object containing the field.</param>
        /// <param name="_fieldInfo">The reflection field info to analyze.</param>
        /// <returns>A field information model.</returns>
        IFieldInfo GetFieldInfo(ObjInfo objInfo, object obj, System.Reflection.FieldInfo _fieldInfo);
    }
}
