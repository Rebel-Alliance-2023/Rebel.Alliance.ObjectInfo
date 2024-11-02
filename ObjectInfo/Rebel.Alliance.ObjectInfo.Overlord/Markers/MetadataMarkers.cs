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

namespace Rebel.Alliance.ObjectInfo.Overlord.Markers
{
    /// <summary>
    /// Marker interface to identify types for metadata scanning.
    /// Implementing this interface indicates that a type should be included
    /// in the metadata scanning process.
    /// </summary>
    /// <remarks>
    /// This is one of two ways to mark types for scanning, the other being
    /// the MetadataScan attribute. Using this interface is preferred when
    /// you want to ensure compile-time checking and when the type naturally
    /// fits into the metadata scanning hierarchy.
    /// </remarks>
    public interface IMetadataScanned
    {
        // Marker interface - no members required
    }

    /// <summary>
    /// Attribute to mark types, interfaces, or assemblies for metadata scanning.
    /// This attribute provides a more flexible way to mark elements for scanning
    /// compared to implementing the IMetadataScanned interface.
    /// </summary>
    /// <remarks>
    /// When applied to an assembly, all types within that assembly will be
    /// considered for scanning. When applied to a type or interface, only
    /// that specific element will be scanned.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Assembly, 
                   AllowMultiple = false, 
                   Inherited = true)]
    public class MetadataScanAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets a description of why this element is being scanned.
        /// This can be useful for documentation and debugging purposes.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Initializes a new instance of the MetadataScanAttribute class.
        /// </summary>
        public MetadataScanAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the MetadataScanAttribute class with a description.
        /// </summary>
        /// <param name="description">A description of why this element is being scanned.</param>
        public MetadataScanAttribute(string description)
        {
            Description = description;
        }
    }
}
