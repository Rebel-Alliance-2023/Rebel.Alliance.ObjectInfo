#region Copyright (c) The Rebel Alliance
// [Copyright Banner]
#endregion

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using ObjectInfo.Brokers.ObjectInfo;
using ObjectInfo.DeepDive;
using ObjectInfo.DeepDive.Analysis;
using ObjectInfo.DeepDive.Analyzers;
using Rebel.Alliance.ObjectInfo.Overlord.Infrastructure;
using Rebel.Alliance.ObjectInfo.Overlord.Models;

namespace Rebel.Alliance.ObjectInfo.Overlord.Services
{
    /// <summary>
    /// Provides metadata services for assemblies and types.
    /// </summary>
    public sealed class MetadataProvider : IMetadataProvider, IDisposable
    {
        private readonly MetadataCache _cache;
        private readonly AssemblyLoader _assemblyLoader;
        private readonly IObjectInfoBroker _objectInfoBroker;
        private readonly AnalyzerManager _analyzerManager;
        private readonly ILogger<MetadataProvider> _logger;
        private readonly MetadataOptions _options;
        private readonly SemaphoreSlim _scanLock;
        private bool _disposedValue;

        public MetadataProvider(
            MetadataCache cache,
            AssemblyLoader assemblyLoader,
            IObjectInfoBroker objectInfoBroker,
            AnalyzerManager analyzerManager,
            ILogger<MetadataProvider> logger,
            MetadataOptions options)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _assemblyLoader = assemblyLoader ?? throw new ArgumentNullException(nameof(assemblyLoader));
            _objectInfoBroker = objectInfoBroker ?? throw new ArgumentNullException(nameof(objectInfoBroker));
            _analyzerManager = analyzerManager ?? throw new ArgumentNullException(nameof(analyzerManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scanLock = new SemaphoreSlim(1, 1);
        }

        async Task<AssemblyMetadata> IMetadataProvider.ScanAssemblyAsync(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            try
            {
                await _scanLock.WaitAsync();

                if (_cache.TryGetAssembly(assembly.FullName!, out var cached))
                {
                    return cached;
                }

                var typeDict = new ConcurrentDictionary<string, TypeMetadata>();
                var types = _assemblyLoader.DiscoverTypes(assembly);

                if (_options.EnableConcurrentAnalysis)
                {
                    await Parallel.ForEachAsync(types, 
                        new ParallelOptions { MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism },
                        async (type, ct) =>
                        {
                            var metadata = await CreateTypeMetadataAsync(type);
                            if (metadata != null)
                            {
                                typeDict.TryAdd(metadata.AssemblyQualifiedName, metadata);
                            }
                        });
                }
                else
                {
                    foreach (var type in types)
                    {
                        var metadata = await CreateTypeMetadataAsync(type);
                        if (metadata != null)
                        {
                            typeDict.TryAdd(metadata.AssemblyQualifiedName, metadata);
                        }
                    }
                }

                var assemblyMetadata = new AssemblyMetadata(assembly, typeDict);
                _cache.StoreAssembly(assemblyMetadata);

                return assemblyMetadata;
            }
            finally
            {
                _scanLock.Release();
            }
        }

        async Task<IReadOnlyCollection<AssemblyMetadata>> IMetadataProvider.ScanAssembliesAsync(IEnumerable<Assembly> assemblies)
        {
            ArgumentNullException.ThrowIfNull(assemblies);

            var results = new ConcurrentBag<AssemblyMetadata>();

            if (_options.EnableConcurrentAnalysis)
            {
                await Parallel.ForEachAsync(assemblies,
                    new ParallelOptions { MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism },
                    async (assembly, ct) =>
                    {
                        var metadata = await ((IMetadataProvider)this).ScanAssemblyAsync(assembly);
                        results.Add(metadata);
                    });
            }
            else
            {
                foreach (var assembly in assemblies)
                {
                    var metadata = await ((IMetadataProvider)this).ScanAssemblyAsync(assembly);
                    results.Add(metadata);
                }
            }

            return results.ToArray();
        }

        public async Task<TypeMetadata?> GetTypeMetadataAsync(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return await GetTypeMetadataAsync(type.AssemblyQualifiedName!);
        }

        public async Task<TypeMetadata?> GetTypeMetadataAsync(string typeFullName)
        {
            ArgumentException.ThrowIfNullOrEmpty(typeFullName);

            if (_cache.TryGetType(typeFullName, out var cached))
            {
                return cached;
            }

            try
            {
                var type = Type.GetType(typeFullName);
                if (type == null)
                {
                    _logger.LogWarning("Type not found: {TypeName}", typeFullName);
                    return null;
                }

                return await CreateTypeMetadataAsync(type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting type metadata for {TypeName}", typeFullName);
                return null;
            }
        }

        public async Task<T?> GetAnalysisResultAsync<T>(Type type, string analyzerName)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentException.ThrowIfNullOrEmpty(analyzerName);

            var metadata = await GetTypeMetadataAsync(type);
            if (metadata == null)
            {
                return default;
            }

            if (metadata.TryGetAnalysisResult<T>(analyzerName, out var cached))
            {
                return cached;
            }

            var result = await AnalyzeTypeAsync(type, analyzerName);
            return result is T typedResult ? typedResult : default;
        }

        public async Task<IReadOnlyCollection<TypeMetadata>> GetImplementationsAsync<T>() where T : class
        {
            var results = new List<TypeMetadata>();
            var interfaceType = typeof(T);

            foreach (var assembly in _cache.GetAllAssemblies())
            {
                foreach (var type in assembly.Types.Values)
                {
                    if (type.ObjectInfo.TypeInfo.ImplementedInterfaces.Any(i => i.Name == interfaceType.Name))
                    {
                        results.Add(type);
                    }
                }
            }

            return results;
        }

        public async Task<IReadOnlyCollection<TypeMetadata>> GetNamespaceTypesAsync(string @namespace)
        {
            ArgumentException.ThrowIfNullOrEmpty(@namespace);

            return _cache.GetAllTypes()
                .Where(t => t.FullName.StartsWith(@namespace + ".", StringComparison.Ordinal))
                .ToArray();
        }

        public async Task<AnalysisResult> AnalyzeTypeAsync(Type type, string analyzerName)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentException.ThrowIfNullOrEmpty(analyzerName);

            var metadata = await GetTypeMetadataAsync(type);
            if (metadata == null)
            {
                throw new InvalidOperationException($"Type metadata not found for {type.FullName}");
            }

            var context = new AnalysisContext((global::ObjectInfo.Models.ObjectInfo.ObjInfo)metadata.ObjectInfo);
            var result = await _analyzerManager.RunAnalyzerAsync(analyzerName, context);

            metadata.SetAnalysisResult(analyzerName, result);
            return result;
        }

        public CacheStatistics GetCacheStatistics() => _cache.Statistics;

        public void ClearCache() => _cache.Clear();

        public IReadOnlyCollection<AssemblyMetadata> GetAllAssemblies() => 
            _cache.GetAllAssemblies().ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<TypeMetadata?> CreateTypeMetadataAsync(Type type)
        {
            try
            {
                var objectInfo = _objectInfoBroker.GetObjectInfo(type);
                var metadata = new TypeMetadata(type, objectInfo);

                if (_options.EnableCaching)
                {
                    _cache.StoreType(metadata);
                }

                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating type metadata for {TypeName}", type.FullName);
                return null;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _scanLock.Dispose();
                    _cache.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
