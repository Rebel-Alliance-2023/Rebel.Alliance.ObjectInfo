#region Copyright (c) The Rebel Alliance
// ----------------------------------------------------------------------------------
// Copyright (c) The Rebel Alliance
// [ASCII Art Copyright Banner]
// ---------------------------------------------------------------------------------- 
#endregion

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ObjectInfo.DeepDive.Analyzers;
using Rebel.Alliance.ObjectInfo.Overlord.Markers;
using Rebel.Alliance.ObjectInfo.Overlord.Models;

/*
builder.Services.AddObjectInfoOverlord(options => {
    options.ScanAssemblyContaining<Program>()
           .EnableAllAnalyzers()
           .WithConcurrentAnalysis()
           .WithCaching(maxCacheSize: 5000)
           .WithAssemblyValidation();
});

OR

var options = new MetadataOptions 
{
    EnableConcurrentAnalysis = true,
    MaxCacheSize = 5000
};
builder.Services.AddObjectInfoOverlord(options);
 
 */

namespace Rebel.Alliance.ObjectInfo.Overlord.DependencyInjection
{
    /// <summary>
    /// Provides a fluent interface for configuring the metadata provider.
    /// </summary>
    public sealed class MetadataProviderBuilder
    {
        private readonly IServiceCollection _services;
        private readonly MetadataOptions _options;

        /// <summary>
        /// Initializes a new instance of the MetadataProviderBuilder class.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">The metadata options.</param>
        internal MetadataProviderBuilder(IServiceCollection services, MetadataOptions options)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Scans the assembly containing the specified type.
        /// </summary>
        /// <typeparam name="T">The type whose assembly should be scanned.</typeparam>
        /// <returns>The builder instance for method chaining.</returns>
        public MetadataProviderBuilder ScanAssemblyContaining<T>()
        {
            _options.AssembliesToScan.Add(typeof(T).Assembly);
            return this;
        }

        /// <summary>
        /// Enables concurrent analysis with the specified degree of parallelism.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public MetadataProviderBuilder WithConcurrentAnalysis(int maxDegreeOfParallelism = -1)
        {
            _options.EnableConcurrentAnalysis = true;
            _options.MaxDegreeOfParallelism = maxDegreeOfParallelism > 0 
                ? maxDegreeOfParallelism 
                : Environment.ProcessorCount;
            return this;
        }

        /// <summary>
        /// Configures caching options.
        /// </summary>
        /// <param name="maxCacheSize">The maximum number of types to cache.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public MetadataProviderBuilder WithCaching(int maxCacheSize = 10000)
        {
            _options.EnableCaching = true;
            _options.MaxCacheSize = maxCacheSize;
            return this;
        }

        /// <summary>
        /// Disables metadata caching.
        /// </summary>
        /// <returns>The builder instance for method chaining.</returns>
        public MetadataProviderBuilder WithoutCaching()
        {
            _options.EnableCaching = false;
            return this;
        }

        /// <summary>
        /// Adds a custom type filter.
        /// </summary>
        /// <param name="filter">The filter predicate.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public MetadataProviderBuilder AddTypeFilter(Func<Type, bool> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);
            _options.TypeFilters.Add(filter);
            return this;
        }

        /// <summary>
        /// Enables all available analyzers.
        /// </summary>
        /// <returns>The builder instance for method chaining.</returns>
        public MetadataProviderBuilder EnableAllAnalyzers()
        {
            var analyzerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IAnalyzer).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var analyzerType in analyzerTypes)
            {
                _services.AddSingleton(typeof(IAnalyzer), analyzerType);
                _options.EnabledAnalyzers.Add(analyzerType);
            }

            return this;
        }

        /// <summary>
        /// Enables a specific analyzer.
        /// </summary>
        /// <typeparam name="TAnalyzer">The type of analyzer to enable.</typeparam>
        /// <returns>The builder instance for method chaining.</returns>
        public MetadataProviderBuilder EnableAnalyzer<TAnalyzer>() where TAnalyzer : class, IAnalyzer
        {
            _services.AddSingleton<IAnalyzer, TAnalyzer>();
            _options.EnabledAnalyzers.Add(typeof(TAnalyzer));
            return this;
        }

        /// <summary>
        /// Enables assembly validation.
        /// </summary>
        /// <returns>The builder instance for method chaining.</returns>
        public MetadataProviderBuilder WithAssemblyValidation()
        {
            _options.ValidateAssemblies = true;
            return this;
        }

        /// <summary>
        /// Disables assembly validation.
        /// </summary>
        /// <returns>The builder instance for method chaining.</returns>
        public MetadataProviderBuilder WithoutAssemblyValidation()
        {
            _options.ValidateAssemblies = false;
            return this;
        }

        /// <summary>
        /// Automatically discovers and scans assemblies containing marked types.
        /// </summary>
        /// <returns>The builder instance for method chaining.</returns>
        public MetadataProviderBuilder ScanForMarkers()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Where(a => HasMetadataMarkers(a))
                .ToList();

            foreach (var assembly in assemblies)
            {
                _options.AssembliesToScan.Add(assembly);
            }

            return this;
        }

        private bool HasMetadataMarkers(Assembly assembly)
        {
            try
            {
                return assembly.GetCustomAttribute<MetadataScanAttribute>() != null ||
                       assembly.GetTypes().Any(t =>
                           t.GetCustomAttribute<MetadataScanAttribute>() != null ||
                           typeof(IMetadataScanned).IsAssignableFrom(t));
            }
            catch
            {
                // If we can't load the assembly's types, assume it doesn't have markers
                return false;
            }
        }
    }
}
