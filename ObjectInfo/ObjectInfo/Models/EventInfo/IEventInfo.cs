#region Copyright (c) The Rebel Alliance
// [License region to be replaced]
#endregion

using System.Collections.Generic;
using ObjectInfo.Models.TypeInfo;
using ObjectInfo.Models.MethodInfo;

namespace ObjectInfo.Models.EventInfo
{
    /// <summary>
    /// Defines the contract for event information.
    /// </summary>
    public interface IEventInfo
    {
        /// <summary>
        /// Gets or sets the name of the event.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the declaring type name.
        /// </summary>
        string DeclaringType { get; set; }

        /// <summary>
        /// Gets or sets the event handler type name.
        /// </summary>
        string EventHandlerType { get; set; }

        /// <summary>
        /// Gets or sets the add accessor method information.
        /// </summary>
        IMethodInfo AddMethod { get; set; }

        /// <summary>
        /// Gets or sets the remove accessor method information.
        /// </summary>
        IMethodInfo RemoveMethod { get; set; }

        /// <summary>
        /// Gets or sets the raise accessor method information, if present.
        /// </summary>
        IMethodInfo RaiseMethod { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the event is multicast.
        /// </summary>
        bool IsMulticast { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the event is special name.
        /// </summary>
        bool IsSpecialName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the event is static.
        /// </summary>
        bool IsStatic { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the event is abstract.
        /// </summary>
        bool IsAbstract { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the event is virtual.
        /// </summary>
        bool IsVirtual { get; set; }

        /// <summary>
        /// Gets or sets access modifiers.
        /// </summary>
        bool IsPublic { get; set; }
        bool IsPrivate { get; set; }
        bool IsProtected { get; set; }

        /// <summary>
        /// Gets or sets custom attributes associated with the event.
        /// </summary>
        List<ITypeInfo> CustomAttrs { get; set; }
    }
}
