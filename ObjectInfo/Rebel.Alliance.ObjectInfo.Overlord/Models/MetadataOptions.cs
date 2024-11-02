#region Copyright (c) The Rebel Alliance
// ----------------------------------------------------------------------------------
// Copyright (c) The Rebel Alliance
// [ASCII Art Copyright Banner]
// ---------------------------------------------------------------------------------- 
#endregion

using System.Reflection;

namespace Rebel.Alliance.ObjectInfo.Overlord.Models
{
    /// <summary>
    /// Configures the behavior of the metadata scanning process.
    /// </summary>
    public sealed class MetadataOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to enable concurrent analysis.
        /// </summary>
        public bool EnableConcurrentAnalysis { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum degree of parallelism for concurrent operations.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Gets or sets a value indicating whether to cache analysis results.
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of types to cache.
        /// </summary>
        public int MaxCacheSize { get; set; } = 10000;

        /// <summary>
        /// Gets or sets a value indicating whether to validate all assemblies before scanning.
        /// </summary>
        public bool ValidateAssemblies { get; set; } = true;

        /// <summary>
        /// Gets the collection of assemblies to scan.
        /// </summary>
        internal HashSet<Assembly> AssembliesToScan { get; } = new();

        /// <summary>
        /// Gets the collection of type filters.
        /// </summary>
        internal List<Func<Type, bool>> TypeFilters { get; } = new();

        /// <summary>
        /// Gets the collection of analyzer types to enable.
        /// </summary>
        internal HashSet<Type> EnabledAnalyzers { get; } = new();

        /// <summary>
        /// Adds a custom type filter.
        /// </summary>
        /// <param name="filter">The filter predicate.</param>
        public void AddTypeFilter(Func<Type, bool> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);
            TypeFilters.Add(filter);
        }

        /// <summary>
        /// Creates a clone of the current options.
        /// </summary>
        /// <returns>A new instance of MetadataOptions with the same values.</returns>
        public MetadataOptions Clone()
        {
            return new MetadataOptions
            {
                EnableConcurrentAnalysis = this.EnableConcurrentAnalysis,
                MaxDegreeOfParallelism = this.MaxDegreeOfParallelism,
                EnableCaching = this.EnableCaching,
                MaxCacheSize = this.MaxCacheSize,
                ValidateAssemblies = this.ValidateAssemblies
            };
        }
    }
}
