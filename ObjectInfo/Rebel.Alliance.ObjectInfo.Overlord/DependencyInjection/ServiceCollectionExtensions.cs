#region Copyright (c) The Rebel Alliance
// ----------------------------------------------------------------------------------
// Copyright (c) The Rebel Alliance
// [ASCII Art Copyright Banner]
// ---------------------------------------------------------------------------------- 
#endregion

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ObjectInfo.Brokers.ObjectInfo;
using ObjectInfo.DeepDive;
using ObjectInfo.DeepDive.Analyzers;
using Rebel.Alliance.ObjectInfo.Overlord.Infrastructure;
using Rebel.Alliance.ObjectInfo.Overlord.Models;
using Rebel.Alliance.ObjectInfo.Overlord.Services;

namespace Rebel.Alliance.ObjectInfo.Overlord.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring metadata services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds metadata services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">An optional action to configure the metadata options.</param>
        /// <returns>The builder for chaining.</returns>
        public static IServiceCollection AddObjectInfoOverlord(
            this IServiceCollection services,
            Action<MetadataProviderBuilder>? configureOptions = null)
        {
            // Add core ObjectInfo services if not already registered
            services.TryAddSingleton<IObjectInfoBroker, ObjectInfoBroker>();

            // Add DeepDive services if not already registered
            services.TryAddSingleton<AnalyzerManager>();

            // Create options
            var options = new MetadataOptions();
            var builder = new MetadataProviderBuilder(services, options);
            configureOptions?.Invoke(builder);

            // Register options
            services.AddSingleton(options);

            // Register infrastructure services
            services.AddSingleton<MetadataCache>();
            services.AddSingleton<AssemblyLoader>();

            // Register the metadata provider
            services.AddSingleton<IMetadataProvider, MetadataProvider>();

            return services;
        }

        /// <summary>
        /// Adds metadata services with custom options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">The metadata options.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddObjectInfoOverlord(
            this IServiceCollection services,
            MetadataOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return services.AddObjectInfoOverlord(builder =>
            {
                // Copy options to the builder's options instance
                var builderOptions = new MetadataOptions
                {
                    EnableConcurrentAnalysis = options.EnableConcurrentAnalysis,
                    MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
                    EnableCaching = options.EnableCaching,
                    MaxCacheSize = options.MaxCacheSize,
                    ValidateAssemblies = options.ValidateAssemblies
                };

                foreach (var assembly in options.AssembliesToScan)
                {
                    builderOptions.AssembliesToScan.Add(assembly);
                }

                foreach (var filter in options.TypeFilters)
                {
                    builderOptions.TypeFilters.Add(filter);
                }

                foreach (var analyzer in options.EnabledAnalyzers)
                {
                    builderOptions.EnabledAnalyzers.Add(analyzer);
                }
            });
        }
    }
}
