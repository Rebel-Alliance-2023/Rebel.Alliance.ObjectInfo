# Rebel.Alliance.ObjectInfo.Overlord Software Specification

## 1. Introduction

### 1.1 Purpose
ObjectInfo.Overlord is a comprehensive metadata management system designed to extend the ObjectInfo ecosystem. It provides centralized scanning, caching, and analysis of .NET type metadata, integrating seamlessly with ObjectInfo and ObjectInfo.Deepdive analyzers.

### 1.2 Scope
The library provides:
- Selective type scanning using marker-based identification
- Efficient metadata caching with configurable limits
- Thread-safe concurrent analysis
- Integration with existing ObjectInfo analyzers
- Dependency injection support
- Assembly validation and filtering
- Type analysis with named analyzers

### 1.3 Version Information
- Current Version: 1.0.0
- Release Date: 2024
- Minimum .NET Version: .NET 8.0
- Supports: Windows, Linux, macOS (cross-platform)

## 2. System Architecture

### 2.1 Core Components

#### 2.1.1 Markers
```csharp
public interface IMetadataScanned { }

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Assembly)]
public class MetadataScanAttribute : Attribute 
{
    public string? Description { get; set; }
}
```
- Identifies types for metadata scanning
- Supports both interface-based and attribute-based marking
- Can be applied at assembly or type level
- Optional description for documentation purposes

#### 2.1.2 Models
```plaintext
AssemblyMetadata
├── Name, FullName, Version
├── Location
├── Types Dictionary
└── CreatedAt

TypeMetadata
├── FullName
├── AssemblyQualifiedName
├── ObjectInfo
├── ScanAttribute
├── ImplementsMetadataScanned
├── Analysis Results (ConcurrentDictionary)
└── CreatedAt

MetadataOptions
├── EnableConcurrentAnalysis
├── MaxDegreeOfParallelism
├── EnableCaching
├── MaxCacheSize
├── ValidateAssemblies
└── TypeFilters
```

#### 2.1.3 Infrastructure
```plaintext
MetadataCache
├── Assembly Cache (ConcurrentDictionary)
├── Type Cache (ConcurrentDictionary)
└── Cache Statistics
    ├── AssemblyCount
    ├── TypeCount
    └── CreatedAt

AssemblyLoader
├── Type Discovery
├── Assembly Validation
└── Type Filtering
```

#### 2.1.4 Services
```plaintext
IMetadataProvider
├── Assembly Scanning
├── Type Metadata Retrieval
├── Analysis Management
└── Cache Management

AnalyzerManager
├── GetAnalyzer(string name)
├── RunAnalyzerAsync(string name, context)
└── RunAnalyzersAsync(objInfo)

IAnalyzer
├── Name (string)
├── AnalyzeAsync(context)
└── Analysis Results
```

### 2.2 Dependencies
- ObjectInfo (Core Library)
- ObjectInfo.Deepdive
- ObjectInfo.Deepdive.CyclomaticComplexityAnalyzer
- ObjectInfo.Deepdive.LinqComplexityAnalyzer
- ObjectInfo.Deepdive.SolidAnalyzer
- Microsoft.Extensions.DependencyInjection
- Serilog (for logging)

## 3. Functional Specifications

### 3.1 Type Scanning
- Marker-based type identification
- Support for assembly-level scanning
- Configurable type filtering
- Thread-safe scanning operations
- Concurrent scanning capabilities
- Selective type scanning through markers

### 3.2 Metadata Caching
- Thread-safe cache operations using ConcurrentDictionary
- Configurable cache size limits
- Automatic cache trimming based on age
- Cache statistics tracking
- Optional cache disabling
- Memory-efficient storage

### 3.3 Analysis Integration
- Analyzers must implement a unique Name property
- Analyzer results include the analyzer name for traceability
- Support for both single and batch analyzer execution
- Analyzer retrieval by name with proper error handling
- Clear separation between analyzer execution and result handling
- Results caching with concurrent access support
- Support for all ObjectInfo.Deepdive analyzers
- Custom analyzer registration

### 3.3.1 Analyzer Naming Guidelines
- Names must be unique within the system
- Should be descriptive and indicate the analysis purpose
- Follow PascalCase convention
- Common suffixes: Analyzer, Inspector, Validator
- Examples: 
  - CyclomaticComplexityAnalyzer
  - LinqComplexityAnalyzer
  - SolidAnalyzer

### 3.3.2 Analysis Results
- Must include analyzer name for traceability
- Should provide both summary and detailed information
- Can be cached for performance
- Thread-safe access through ConcurrentDictionary
- Support for different result types (MethodAnalysisResult, TypeAnalysisResult, etc.)

### 3.4 Assembly Management
- Assembly validation
- Dynamic assembly loading
- Assembly metadata tracking
- Error handling for assembly operations
- Support for multiple assembly versions

## 4. Technical Specifications

### 4.1 Performance Requirements
- Thread-safe operations
- Configurable concurrency levels
- Minimal memory footprint
- Efficient cache management
- Fast type lookup
- Optimized analyzer execution

### 4.2 Security Considerations
- Safe assembly loading
- Validation of loaded assemblies
- Protection against excessive memory usage
- Thread-safe operations
- Resource consumption limits

### 4.3 Error Handling
Comprehensive exception handling including:
- Analyzer not found scenarios
- Invalid analyzer name handling
- Analyzer execution failures
- Result caching errors
- Thread-safety considerations in concurrent analysis
- Assembly loading failures
- Type resolution errors
- Cache overflow scenarios

## 5. Configuration

### 5.1 Dependency Injection
```csharp
services.AddObjectInfoOverlord(options => {
    options.ScanAssemblyContaining<Program>()
           .EnableAllAnalyzers()
           .WithConcurrentAnalysis()
           .WithCaching(maxCacheSize: 5000)
           .WithAssemblyValidation();
});
```

### 5.2 Options Configuration
```csharp
public class MetadataOptions
{
    public bool EnableConcurrentAnalysis { get; set; } = true;
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    public bool EnableCaching { get; set; } = true;
    public int MaxCacheSize { get; set; } = 10000;
    public bool ValidateAssemblies { get; set; } = true;
    public HashSet<Assembly> AssembliesToScan { get; } = new();
    public List<Func<Type, bool>> TypeFilters { get; } = new();
    public HashSet<Type> EnabledAnalyzers { get; } = new();
}
```

## 6. Usage Examples

### 6.1 Marking Types for Scanning
```csharp
[assembly: MetadataScan]

[MetadataScan(Description = "Core domain model")]
public class MyImportantClass { }

public class AnotherImportantClass : IMetadataScanned { }
```

### 6.2 Using the Provider
```csharp
public class MetadataService
{
    private readonly IMetadataProvider _provider;

    public async Task<TypeMetadata?> GetTypeInfoAsync<T>()
    {
        return await _provider.GetTypeMetadataAsync(typeof(T));
    }

    public async Task<AnalysisResult> AnalyzeTypeAsync<T>(string analyzerName)
    {
        // The analyzerName must match the Name property of an IAnalyzer implementation
        return await _provider.AnalyzeTypeAsync(typeof(T), analyzerName);
    }

    public async Task<T?> GetAnalysisResultAsync<T, TTarget>(string analyzerName)
    {
        return await _provider.GetAnalysisResultAsync<T>(typeof(TTarget), analyzerName);
    }
}
```

## 7. Testing Strategy

### 7.1 Unit Testing
- Component-level testing
- Mocking of dependencies
- Coverage requirements
- Performance testing

### 7.2 Integration Testing
- End-to-end scenarios
- Cross-component testing
- Concurrency testing
- Memory leak testing

## 8. Performance Considerations

### 8.1 Caching Strategy
- In-memory caching
- Cache size management
- Cache eviction policies
- Cache statistics

### 8.2 Concurrency
- Thread-safe operations
- Parallel scanning
- Concurrent analysis
- Resource management

## 9. Deployment Requirements

### 9.1 Dependencies
- .NET 8.0
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- System.Collections.Concurrent

### 9.2 Package Distribution
- NuGet package
- MIT License
- XML documentation
- README and usage examples

## 10. Future Enhancements

### 10.1 Planned Features
- Distributed caching support
- Custom analyzer framework
- Real-time metadata updates
- Performance optimization tools

### 10.2 Extension Points
- Custom type filters
- Analyzer plugins
- Cache providers
- Metadata exporters

#### 10.2.1 Analyzer Development:
- Implementing IAnalyzer interface
- Providing unique analyzer names
- Result type definitions
- Integration with analysis pipeline
- Custom analyzer registration

## 11. Limitations and Constraints

### 11.1 Known Limitations
- Assembly loading restrictions
- Memory usage with large type sets
- Analysis performance constraints
- Framework compatibility


### 11.2 Security Considerations
- Assembly validation
- Type loading safety
- Resource consumption limits
- Thread safety requirements

## 12. Documentation Requirements

### 12.1 API Documentation
- XML documentation
- Code examples
- Best practices
- Usage guidelines

### 12.2 User Documentation
- Getting started guide
- Configuration guide
- Troubleshooting guide
- Performance tuning guide

## 13. Support and Maintenance

### 13.1 Support Plan
- Issue tracking
- Bug fixing
- Feature requests
- Documentation updates

### 13.2 Maintenance Schedule
- Regular updates
- Security patches
- Performance improvements
- Compatibility updates

## 14. Implementation Notes

### 14.1 Analyzer Implementation Examples

#### 14.1.1 Base Analyzer
```csharp
public class SimpleAnalyzer : IAnalyzer
{
    public string Name => "Simple Analyzer";

    public async Task<AnalysisResult> AnalyzeAsync(AnalysisContext context)
    {
        return new AnalysisResult(Name, "Basic analysis", "Details...");
    }
}
```

#### 14.1.2 Method Analyzer
```csharp
public class MethodComplexityAnalyzer : IMethodAnalyzer
{
    public string Name => "Method Complexity Analyzer";

    public async Task<MethodAnalysisResult> AnalyzeMethodAsync(IMethodInfo methodInfo)
    {
        var complexity = CalculateComplexity(methodInfo);
        return new MethodAnalysisResult(
            Name, 
            methodInfo.Name, 
            $"Method complexity: {complexity}",
            GetDetailedAnalysis(methodInfo, complexity));
    }

    private string GetDetailedAnalysis(IMethodInfo methodInfo, int complexity)
    {
        return $"Method {methodInfo.Name} has complexity of {complexity}. " +
               $"Threshold for concern is 10. " +
               (complexity > 10 ? "Consider refactoring." : "Complexity is acceptable.");
    }
}
```

#### 14.1.3 Type Analyzer
```csharp
public class TypeStructureAnalyzer : ITypeAnalyzer
{
    public string Name => "Type Structure Analyzer";

    public async Task<TypeAnalysisResult> AnalyzeTypeAsync(ITypeInfo typeInfo)
    {
        var methodCount = typeInfo.MethodInfos.Count;
        var propertyCount = typeInfo.PropInfos.Count;
        
        return new TypeAnalysisResult(
            Name, 
            typeInfo.Name, 
            $"Type contains {methodCount} methods and {propertyCount} properties",
            GenerateDetailedAnalysis(typeInfo),
            methodCount,
            propertyCount);
    }

    private string GenerateDetailedAnalysis(ITypeInfo typeInfo)
    {
        return $"Detailed analysis of {typeInfo.Name}:\n" +
               $"- Method count: {typeInfo.MethodInfos.Count}\n" +
               $"- Property count: {typeInfo.PropInfos.Count}\n" +
               $"- Interface implementations: {typeInfo.ImplementedInterfaces.Count}\n" +
               $"- Custom attributes: {typeInfo.CustomAttrs.Count}";
    }
}
```

### 14.2 Thread Safety Implementations

#### 14.2.1 Result Storage
```csharp
public void SetAnalysisResult(string analyzerName, object result)
{
    ArgumentNullException.ThrowIfNull(analyzerName);
    ArgumentNullException.ThrowIfNull(result);
    
    AnalysisResults.AddOrUpdate(analyzerName, result, (_, _) => result);
}
```

#### 14.2.2 Cache Management
```csharp
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
    }
}
```

#### 14.2.3 Concurrent Scanning
```csharp
public async Task<IReadOnlyCollection<AssemblyMetadata>> ScanAssembliesAsync(
    IEnumerable<Assembly> assemblies)
{
    var results = new ConcurrentBag<AssemblyMetadata>();

    if (_options.EnableConcurrentAnalysis)
    {
        await Parallel.ForEachAsync(
            assemblies,
            new ParallelOptions 
            { 
                MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism 
            },
            async (assembly, ct) =>
            {
                var metadata = await ScanAssemblyAsync(assembly);
                results.Add(metadata);
            });
    }
    else
    {
        foreach (var assembly in assemblies)
        {
            var metadata = await ScanAssemblyAsync(assembly);
            results.Add(metadata);
        }
    }

    return results.ToArray();
}
```

## 15. Conclusion
ObjectInfo.Overlord provides a robust and efficient solution for .NET metadata management, integrating seamlessly with the existing ObjectInfo ecosystem while adding powerful new capabilities for type scanning, analysis, and caching. The system's architecture ensures thread safety, performance, and extensibility while maintaining ease of use through clear APIs and comprehensive documentation.
