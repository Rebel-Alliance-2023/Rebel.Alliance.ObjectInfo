#region Copyright (c) The Rebel Alliance
// [License region to be replaced]
#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using ObjectInfo.Models.TypeInfo;
using ObjectInfo.Models.MethodInfo;

namespace ObjectInfo.Models.EventInfo
{
    /// <summary>
    /// Represents event metadata from reflection.
    /// </summary>
    public class EventInfo : IEventInfo
    {
        public string Name { get; set; }
        public string DeclaringType { get; set; }
        public string EventHandlerType { get; set; }
        public IMethodInfo AddMethod { get; set; }
        public IMethodInfo RemoveMethod { get; set; }
        public IMethodInfo RaiseMethod { get; set; }
        public bool IsMulticast { get; set; }
        public bool IsSpecialName { get; set; }
        public bool IsStatic { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsPublic { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsProtected { get; set; }
        public List<ITypeInfo> CustomAttrs { get; set; }

        /// <summary>
        /// Initializes a new instance of the EventInfo class.
        /// </summary>
        public EventInfo()
        {
            CustomAttrs = new List<ITypeInfo>();
        }

        /// <summary>
        /// Initializes a new instance of the EventInfo class from a System.Reflection.EventInfo.
        /// </summary>
        /// <param name="eventInfo">The reflection event info to wrap.</param>
        /// <param name="getMethodInfo">Function to convert MethodInfo to IMethodInfo.</param>
        /// <param name="getTypeInfo">Function to convert Type to ITypeInfo.</param>
        public EventInfo(System.Reflection.EventInfo eventInfo, 
                        Func<System.Reflection.MethodInfo, IMethodInfo> getMethodInfo,
                        Func<Type, ITypeInfo> getTypeInfo)
        {
            if (eventInfo == null)
                throw new ArgumentNullException(nameof(eventInfo));

            Name = eventInfo.Name;
            DeclaringType = eventInfo.DeclaringType?.Name;
            EventHandlerType = eventInfo.EventHandlerType?.Name;
            CustomAttrs = new List<ITypeInfo>();

            // Get accessor methods
            if (eventInfo.AddMethod != null)
                AddMethod = getMethodInfo(eventInfo.AddMethod);
            
            if (eventInfo.RemoveMethod != null)
                RemoveMethod = getMethodInfo(eventInfo.RemoveMethod);
            
            if (eventInfo.RaiseMethod != null)
                RaiseMethod = getMethodInfo(eventInfo.RaiseMethod);

            // Set event characteristics
            IsMulticast = typeof(MulticastDelegate).IsAssignableFrom(eventInfo.EventHandlerType);
            IsSpecialName = eventInfo.IsSpecialName;

            // Get modifiers from the add method (as per C# spec, all accessors have the same modifiers)
            if (eventInfo.AddMethod != null)
            {
                IsStatic = eventInfo.AddMethod.IsStatic;
                IsAbstract = eventInfo.AddMethod.IsAbstract;
                IsVirtual = eventInfo.AddMethod.IsVirtual;
                IsPublic = eventInfo.AddMethod.IsPublic;
                IsPrivate = eventInfo.AddMethod.IsPrivate;
                IsProtected = eventInfo.AddMethod.IsFamily;
            }

            // Get custom attributes
            var attrs = eventInfo.GetCustomAttributes(false);
            foreach (var attr in attrs)
            {
                var attrType = attr.GetType();
                if (!attrType.Namespace.StartsWith("System"))
                {
                    var attrInfo = getTypeInfo(attrType);
                    if (attrInfo != null)
                    {
                        CustomAttrs.Add(attrInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a collection of EventInfo instances from reflection event infos.
        /// </summary>
        /// <param name="eventInfos">The reflection event infos to process.</param>
        /// <param name="getMethodInfo">Function to convert MethodInfo to IMethodInfo.</param>
        /// <param name="getTypeInfo">Function to convert Type to ITypeInfo.</param>
        /// <param name="includeNonPublic">Whether to include non-public events.</param>
        /// <returns>A list of EventInfo instances.</returns>
        public static List<IEventInfo> CreateMany(
            IEnumerable<System.Reflection.EventInfo> eventInfos,
            Func<System.Reflection.MethodInfo, IMethodInfo> getMethodInfo,
            Func<Type, ITypeInfo> getTypeInfo,
            bool includeNonPublic = false)
        {
            var result = new List<IEventInfo>();
            foreach (var eventInfo in eventInfos)
            {
                // Skip non-public events unless specifically included
                if (!includeNonPublic && eventInfo.AddMethod?.IsPublic != true)
                    continue;

                var evInfo = new EventInfo(eventInfo, getMethodInfo, getTypeInfo);
                result.Add(evInfo);
            }
            return result;
        }
    }
}
