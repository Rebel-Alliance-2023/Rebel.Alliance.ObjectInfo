#region Copyright (c) The Rebel Alliance
// ----------------------------------------------------------------------------------
// Copyright (c) The Rebel Alliance
// [ASCII Art Copyright Banner]
// ---------------------------------------------------------------------------------- 
#endregion

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using ObjectInfo.Models.ObjectInfo;
using System.Diagnostics.CodeAnalysis;
using Rebel.Alliance.ObjectInfo.Overlord.Markers;

namespace Rebel.Alliance.ObjectInfo.Overlord.Models
{
    /// <summary>
    /// Represents metadata for an entire assembly, including all its scanned types.
    /// </summary>
    public sealed class AssemblyMetadata
    {
        /// <summary>
        /// Gets the assembly name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the full name of the assembly.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Gets the version of the assembly.
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// Gets the location of the assembly file.
        /// </summary>
        public string Location { get; }

        /// <summary>
        /// Gets a read-only dictionary of all scanned types in this assembly.
        /// </summary>
        public IReadOnlyDictionary<string, TypeMetadata> Types { get; }

        /// <summary>
        /// Gets the timestamp when this metadata was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; }

        /// <summary>
        /// Initializes a new instance of the AssemblyMetadata class.
        /// </summary>
        /// <param name="assembly">The assembly to create metadata for.</param>
        /// <param name="types">Dictionary of type metadata for the assembly.</param>
        public AssemblyMetadata(Assembly assembly, IDictionary<string, TypeMetadata> types)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentNullException.ThrowIfNull(types);

            Name = assembly.GetName().Name ?? throw new ArgumentException("Assembly name cannot be null", nameof(assembly));
            FullName = assembly.FullName ?? throw new ArgumentException("Assembly full name cannot be null", nameof(assembly));
            Version = assembly.GetName().Version ?? new Version(1, 0);
            Location = assembly.Location;
            Types = types.ToImmutableDictionary();
            CreatedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Represents metadata for a specific type, including its ObjectInfo and analysis results.
    /// </summary>
    public sealed class TypeMetadata
    {
        /// <summary>
        /// Gets the full name of the type.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Gets the assembly-qualified name of the type.
        /// </summary>
        public string AssemblyQualifiedName { get; }

        /// <summary>
        /// Gets the ObjectInfo for this type.
        /// </summary>
        public IObjInfo ObjectInfo { get; }

        /// <summary>
        /// Gets the metadata scan attribute if present.
        /// </summary>
        public MetadataScanAttribute? ScanAttribute { get; }

        /// <summary>
        /// Gets a value indicating whether the type implements IMetadataScanned.
        /// </summary>
        public bool ImplementsMetadataScanned { get; }

        /// <summary>
        /// Gets a concurrent dictionary of analysis results, keyed by analyzer name.
        /// </summary>
        public ConcurrentDictionary<string, object> AnalysisResults { get; }

        /// <summary>
        /// Gets the timestamp when this metadata was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; }

        /// <summary>
        /// Initializes a new instance of the TypeMetadata class.
        /// </summary>
        /// <param name="type">The Type to create metadata for.</param>
        /// <param name="objectInfo">The ObjectInfo for the type.</param>
        public TypeMetadata(Type type, IObjInfo objectInfo)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(objectInfo);

            FullName = type.FullName ?? throw new ArgumentException("Type full name cannot be null", nameof(type));
            AssemblyQualifiedName = type.AssemblyQualifiedName ?? throw new ArgumentException("Type assembly qualified name cannot be null", nameof(type));
            ObjectInfo = objectInfo;
            ScanAttribute = type.GetCustomAttribute<MetadataScanAttribute>();
            ImplementsMetadataScanned = typeof(IMetadataScanned).IsAssignableFrom(type);
            AnalysisResults = new ConcurrentDictionary<string, object>();
            CreatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Attempts to get an analysis result of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of analysis result to retrieve.</typeparam>
        /// <param name="analyzerName">The name of the analyzer.</param>
        /// <param name="result">When this method returns, contains the analysis result if found; otherwise, the default value.</param>
        /// <returns>True if the analysis result was found; otherwise, false.</returns>
        public bool TryGetAnalysisResult<T>(string analyzerName, out T? result)
        {
            if (AnalysisResults.TryGetValue(analyzerName, out var value) && value is T typedResult)
            {
                result = typedResult;
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Adds or updates an analysis result.
        /// </summary>
        /// <param name="analyzerName">The name of the analyzer.</param>
        /// <param name="result">The analysis result to store.</param>
        public void SetAnalysisResult(string analyzerName, object result)
        {
            ArgumentNullException.ThrowIfNull(analyzerName);
            ArgumentNullException.ThrowIfNull(result);

            AnalysisResults.AddOrUpdate(analyzerName, result, (_, _) => result);
        }
    }
}
