#region Copyright (c) The Rebel Alliance
// ----------------------------------------------------------------------------------
// Copyright (c) The Rebel Alliance
// [ASCII Art Copyright Banner]
// ---------------------------------------------------------------------------------- 
#endregion

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Rebel.Alliance.ObjectInfo.Overlord.Models;
using System.Diagnostics.CodeAnalysis;

namespace Rebel.Alliance.ObjectInfo.Overlord.Infrastructure
{
    /// <summary>
    /// Provides thread-safe caching of assembly and type metadata.
    /// </summary>
    public sealed class MetadataCache : IDisposable
    {
        private readonly ConcurrentDictionary<string, AssemblyMetadata> _assemblyCache;
        private readonly ConcurrentDictionary<string, TypeMetadata> _typeCache;
        private readonly ILogger<MetadataCache> _logger;
        private readonly MetadataOptions _options;
        private readonly object _trimLock = new();
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the MetadataCache class.
        /// </summary>
        /// <param name="options">The metadata options.</param>
        /// <param name="logger">The logger instance.</param>
        public MetadataCache(MetadataOptions options, ILogger<MetadataCache> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _assemblyCache = new ConcurrentDictionary<string, AssemblyMetadata>();
            _typeCache = new ConcurrentDictionary<string, TypeMetadata>();
        }

        /// <summary>
        /// Gets the current cache statistics.
        /// </summary>
        public CacheStatistics Statistics => new(
            AssemblyCount: _assemblyCache.Count,
            TypeCount: _typeCache.Count,
            CreatedAt: DateTimeOffset.UtcNow
        );

        /// <summary>
        /// Stores assembly metadata in the cache.
        /// </summary>
        /// <param name="metadata">The assembly metadata to store.</param>
        public void StoreAssembly(AssemblyMetadata metadata)
        {
            ArgumentNullException.ThrowIfNull(metadata);
            
            _assemblyCache.AddOrUpdate(metadata.FullName, metadata, (_, _) => metadata);
            
            foreach (var type in metadata.Types.Values)
            {
                StoreType(type);
            }

            TrimCacheIfNeeded();
            _logger.LogDebug("Stored assembly metadata for {AssemblyName}", metadata.Name);
        }

        /// <summary>
        /// Stores type metadata in the cache.
        /// </summary>
        /// <param name="metadata">The type metadata to store.</param>
        public void StoreType(TypeMetadata metadata)
        {
            ArgumentNullException.ThrowIfNull(metadata);
            
            _typeCache.AddOrUpdate(metadata.AssemblyQualifiedName, metadata, (_, _) => metadata);
            TrimCacheIfNeeded();
            _logger.LogTrace("Stored type metadata for {TypeName}", metadata.FullName);
        }

        /// <summary>
        /// Attempts to retrieve assembly metadata from the cache.
        /// </summary>
        /// <param name="assemblyName">The full name of the assembly.</param>
        /// <param name="metadata">The retrieved metadata, if found.</param>
        /// <returns>True if the metadata was found; otherwise, false.</returns>
        public bool TryGetAssembly(string assemblyName, [NotNullWhen(true)] out AssemblyMetadata? metadata)
        {
            ArgumentException.ThrowIfNullOrEmpty(assemblyName);
            return _assemblyCache.TryGetValue(assemblyName, out metadata);
        }

        /// <summary>
        /// Attempts to retrieve type metadata from the cache.
        /// </summary>
        /// <param name="typeFullName">The assembly-qualified name of the type.</param>
        /// <param name="metadata">The retrieved metadata, if found.</param>
        /// <returns>True if the metadata was found; otherwise, false.</returns>
        public bool TryGetType(string typeFullName, [NotNullWhen(true)] out TypeMetadata? metadata)
        {
            ArgumentException.ThrowIfNullOrEmpty(typeFullName);
            return _typeCache.TryGetValue(typeFullName, out metadata);
        }

        /// <summary>
        /// Clears all cached metadata.
        /// </summary>
        public void Clear()
        {
            _assemblyCache.Clear();
            _typeCache.Clear();
            _logger.LogInformation("Cache cleared");
        }

        /// <summary>
        /// Gets an enumerable of all cached assembly metadata.
        /// </summary>
        /// <returns>An enumerable of AssemblyMetadata.</returns>
        public IEnumerable<AssemblyMetadata> GetAllAssemblies()
        {
            return _assemblyCache.Values.ToArray();
        }

        /// <summary>
        /// Gets an enumerable of all cached type metadata.
        /// </summary>
        /// <returns>An enumerable of TypeMetadata.</returns>
        public IEnumerable<TypeMetadata> GetAllTypes()
        {
            return _typeCache.Values.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TrimCacheIfNeeded()
        {
            if (!_options.EnableCaching || _typeCache.Count <= _options.MaxCacheSize)
            {
                return;
            }

            lock (_trimLock)
            {
                if (_typeCache.Count <= _options.MaxCacheSize)
                {
                    return;
                }

                var itemsToRemove = _typeCache.Count - _options.MaxCacheSize;
                var oldestItems = _typeCache.Values
                    .OrderBy(x => x.CreatedAt)
                    .Take(itemsToRemove)
                    .Select(x => x.AssemblyQualifiedName)
                    .ToList();

                foreach (var key in oldestItems)
                {
                    _typeCache.TryRemove(key, out _);
                }

                _logger.LogInformation("Trimmed {Count} items from cache", itemsToRemove);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Clear();
                }
                _disposedValue = true;
            }
        }

        /// <summary>
        /// Disposes of the cache resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Represents statistics about the metadata cache.
    /// </summary>
    /// <param name="AssemblyCount">The number of cached assemblies.</param>
    /// <param name="TypeCount">The number of cached types.</param>
    /// <param name="CreatedAt">The timestamp when these statistics were created.</param>
    public readonly record struct CacheStatistics(
        int AssemblyCount,
        int TypeCount,
        DateTimeOffset CreatedAt
    );
}
