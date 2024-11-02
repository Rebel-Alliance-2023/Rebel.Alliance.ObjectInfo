#region Copyright (c) The Rebel Alliance
// [Copyright Banner]
#endregion

using System.Reflection;
using ObjectInfo.DeepDive.Analysis;
using Rebel.Alliance.ObjectInfo.Overlord.Infrastructure;
using Rebel.Alliance.ObjectInfo.Overlord.Models;

namespace Rebel.Alliance.ObjectInfo.Overlord.Services
{
    /// <summary>
    /// Defines the contract for providing metadata services.
    /// </summary>
    public interface IMetadataProvider
    {
        /// <summary>
        /// Scans an assembly for metadata.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        /// <returns>The assembly metadata.</returns>
        Task<AssemblyMetadata> ScanAssemblyAsync(Assembly assembly);

        /// <summary>
        /// Scans multiple assemblies for metadata.
        /// </summary>
        /// <param name="assemblies">The assemblies to scan.</param>
        /// <returns>A collection of assembly metadata.</returns>
        Task<IReadOnlyCollection<AssemblyMetadata>> ScanAssembliesAsync(IEnumerable<Assembly> assemblies);

        /// <summary>
        /// Gets metadata for a specific type.
        /// </summary>
        Task<TypeMetadata?> GetTypeMetadataAsync(Type type);

        /// <summary>
        /// Gets metadata for a type by its full name.
        /// </summary>
        Task<TypeMetadata?> GetTypeMetadataAsync(string typeFullName);

        /// <summary>
        /// Gets analysis results for a type.
        /// </summary>
        Task<T?> GetAnalysisResultAsync<T>(Type type, string analyzerName);

        /// <summary>
        /// Gets type metadata for all implementations of an interface.
        /// </summary>
        Task<IReadOnlyCollection<TypeMetadata>> GetImplementationsAsync<T>() where T : class;

        /// <summary>
        /// Gets type metadata for all types in a namespace.
        /// </summary>
        Task<IReadOnlyCollection<TypeMetadata>> GetNamespaceTypesAsync(string @namespace);

        /// <summary>
        /// Analyzes a type using a specific analyzer.
        /// </summary>
        Task<AnalysisResult> AnalyzeTypeAsync(Type type, string analyzerName);

        /// <summary>
        /// Gets current cache statistics.
        /// </summary>
        CacheStatistics GetCacheStatistics();

        /// <summary>
        /// Clears the metadata cache.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Gets all scanned assemblies.
        /// </summary>
        IReadOnlyCollection<AssemblyMetadata> GetAllAssemblies();
    }
}
