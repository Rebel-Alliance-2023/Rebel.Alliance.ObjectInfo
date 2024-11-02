# Rebel.Alliance.ObjectInfo.Overlord Software Specification

## 1. Introduction

### 1.1 Purpose
Rebel.Alliance.ObjectInfo.Overlord provides a centralized metadata management system that extends the ObjectInfo ecosystem. It manages type scanning, metadata caching, and analyzer integration while ensuring thread safety and performance.

### 1.2 Scope
Core capabilities:
- Selective type scanning using markers
- Efficient metadata caching
- Thread-safe concurrent operations
- Analyzer integration and management
- Assembly scanning and validation
- Dependency injection integration

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

#### 2.1.2 Metadata Models
```plaintext
AssemblyMetadata
├── Name, FullName, Version
├── Location
├── Types Dictionary (immutable)
└── CreatedAt

TypeMetadata
├── FullName
├── AssemblyQualifiedName
├── ObjectInfo
├── ScanAttribute
├── ImplementsMetadataScanned
├── Analysis Results (ConcurrentDictionary)
└── CreatedAt
```

#### 2.1.3 Infrastructure
```plaintext
MetadataCache
├── Assembly Cache (ConcurrentDictionary)
├── Type Cache (ConcurrentDictionary)
└── Cache Statistics

AssemblyLoader
├── Type Discovery
├── Basic Validation
└── Type Filtering
```

### 2.2 Dependencies
Required:
- ObjectInfo
- ObjectInfo.Deepdive
- Microsoft.Extensions.DependencyInjection
- Serilog

## 3. Core Features

### 3.1 Type Scanning
- Marker-based identification
- Assembly-level scanning
- Configurable filtering
- Thread-safe operation

### 3.2 Metadata Caching
- Thread-safe cache operations
- Size-based limits with configuration
- Age-based eviction strategy
- Basic statistics tracking
- Optional cache disabling

### 3.3 Analyzer Integration
- Named analyzer access
- Concurrent analysis support
- Result caching
- Plugin support through ObjectInfo.Deepdive

## 4. Technical Specifications

### 4.1 Thread Safety
- Concurrent metadata access
- Thread-safe caching
- Safe analyzer execution
- Resource protection

### 4.2 Performance
- Configurable concurrency
- Efficient cache management
- Optimized type scanning

### 4.3 Error Handling
- Assembly loading failures
- Type resolution errors
- Analyzer execution failures
- Cache management errors

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

### 5.2 Options
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

### 6.1 Type Marking
```csharp
[assembly: MetadataScan]

[MetadataScan(Description = "Core domain model")]
public class MyImportantClass { }

public class AnotherImportantClass : IMetadataScanned { }
```

### 6.2 Provider Usage
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
        return await _provider.AnalyzeTypeAsync(typeof(T), analyzerName);
    }

    public async Task<T?> GetAnalysisResultAsync<T, TTarget>(string analyzerName)
    {
        return await _provider.GetAnalysisResultAsync<T>(typeof(TTarget), analyzerName);
    }
}
```

## 7. Implementation Notes

### 7.1 Thread Safety
```csharp
public void SetAnalysisResult(string analyzerName, object result)
{
    ArgumentNullException.ThrowIfNull(analyzerName);
    ArgumentNullException.ThrowIfNull(result);
    
    AnalysisResults.AddOrUpdate(analyzerName, result, (_, _) => result);
}
```

### 7.2 Cache Management
```csharp
private void TrimCacheIfNeeded()
{
    if (!_options.EnableCaching || _typeCache.Count <= _options.MaxCacheSize)
        return;

    lock (_trimLock)
    {
        if (_typeCache.Count <= _options.MaxCacheSize)
            return;

        var itemsToRemove = _typeCache.Count - _options.MaxCacheSize;
        var oldestItems = _typeCache.Values
            .OrderBy(x => x.CreatedAt)
            .Take(itemsToRemove)
            .Select(x => x.AssemblyQualifiedName)
            .ToList();

        foreach (var key in oldestItems)
            _typeCache.TryRemove(key, out _);
    }
}
```

## 8. Testing Requirements

### 8.1 Unit Tests
- Core functionality testing
- Thread safety verification
- Configuration testing
- Error handling verification

### 8.2 Integration Tests
- End-to-end scenarios
- Cross-component functionality
- Real-world usage patterns

## 9. Conclusion
Rebel.Alliance.ObjectInfo.Overlord provides a robust metadata management system focusing on thread safety, performance, and ease of use. It seamlessly integrates with the ObjectInfo ecosystem while maintaining clear separation of concerns and extensibility.
