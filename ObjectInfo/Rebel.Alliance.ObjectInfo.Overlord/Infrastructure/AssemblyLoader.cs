#region Copyright (c) The Rebel Alliance
// ----------------------------------------------------------------------------------
// Copyright (c) The Rebel Alliance
// [ASCII Art Copyright Banner]
// ---------------------------------------------------------------------------------- 
#endregion

using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Rebel.Alliance.ObjectInfo.Overlord.Markers;
using Rebel.Alliance.ObjectInfo.Overlord.Models;

namespace Rebel.Alliance.ObjectInfo.Overlord.Infrastructure
{
    /// <summary>
    /// Handles loading and validation of assemblies for metadata scanning.
    /// </summary>
    public sealed class AssemblyLoader
    {
        private readonly ILogger<AssemblyLoader> _logger;
        private readonly MetadataOptions _options;
        private readonly HashSet<string> _loadedAssemblies;

        /// <summary>
        /// Initializes a new instance of the AssemblyLoader class.
        /// </summary>
        /// <param name="options">The metadata options.</param>
        /// <param name="logger">The logger instance.</param>
        public AssemblyLoader(MetadataOptions options, ILogger<AssemblyLoader> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loadedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Discovers types marked for scanning in the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        /// <returns>An enumerable of discovered types.</returns>
        public IEnumerable<Type> DiscoverTypes(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            if (!ValidateAssembly(assembly))
            {
                yield break;
            }

            var assemblyMarked = assembly.GetCustomAttribute<MetadataScanAttribute>() != null;

            foreach (var type in assembly.GetTypes())
            {
                if (ShouldScanType(type, assemblyMarked))
                {
                    yield return type;
                }
            }
        }

        /// <summary>
        /// Validates the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to validate.</param>
        /// <returns>True if the assembly is valid; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ValidateAssembly(Assembly assembly)
        {
            if (!_options.ValidateAssemblies)
            {
                return true;
            }

            var assemblyName = assembly.GetName().FullName;
            if (_loadedAssemblies.Contains(assemblyName))
            {
                _logger.LogTrace("Assembly {AssemblyName} already validated", assemblyName);
                return true;
            }

            try
            {
                // Basic validation - ensure we can access types
                _ = assembly.GetTypes();
                _loadedAssemblies.Add(assemblyName);
                _logger.LogDebug("Validated assembly {AssemblyName}", assemblyName);
                return true;
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogError(ex, "Failed to load types from assembly {AssemblyName}", assemblyName);
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    _logger.LogError(loaderException, "Loader exception for assembly {AssemblyName}", assemblyName);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate assembly {AssemblyName}", assemblyName);
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldScanType(Type type, bool assemblyMarked)
        {
            // Skip if type is compiler-generated
            if (type.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                return false;
            }

            // Apply custom filters
            foreach (var filter in _options.TypeFilters)
            {
                if (!filter(type))
                {
                    return false;
                }
            }

            return assemblyMarked ||
                   type.GetCustomAttribute<MetadataScanAttribute>() != null ||
                   typeof(IMetadataScanned).IsAssignableFrom(type);
        }
    }
}
