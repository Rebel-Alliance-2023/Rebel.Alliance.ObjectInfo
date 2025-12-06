# Rebel.Alliance.ObjectInfo.Overlord

![RebelAlliance](Rebel.Alliance.Icon.01.png)

A thread-safe, high-performance metadata management system that extends the ObjectInfo ecosystem with advanced type scanning, caching, and analysis capabilities.

## Features

- 🔍 **Selective Type Scanning**: Discover and analyze types using markers and attributes
- ⚡ **High-Performance Caching**: Thread-safe metadata caching with configurable limits
- 🔄 **Concurrent Operations**: Full support for parallel analysis and type scanning
- 🧩 **Analyzer Integration**: Seamless integration with ObjectInfo.Deepdive analyzers
- 🛡️ **Assembly Validation**: Robust assembly scanning and validation
- 💉 **DI Support**: Full integration with Microsoft.Extensions.DependencyInjection

## Installation

Install via NuGet:

```bash
dotnet add package Rebel.Alliance.ObjectInfo.Overlord
```

## Quick Start

1. Mark your types for scanning:

```csharp
// Using attribute
[MetadataScan(Description = "Core domain model")]
public class MyDomainModel 
{
    // Your model properties
}

// Using interface
public class MyService : IMetadataScanned 
{
    // Your service implementation
}
```

2. Configure services:

```csharp
services.AddObjectInfoOverlord(options => {
    options.ScanAssemblyContaining<Program>()
           .EnableAllAnalyzers()
           .WithConcurrentAnalysis()
           .WithCaching(maxCacheSize: 5000);
});
```

3. Use the metadata provider:

```csharp
public class MetadataService
{
    private readonly IMetadataProvider _provider;

    public MetadataService(IMetadataProvider provider)
    {
        _provider = provider;
    }

    public async Task<TypeMetadata?> GetTypeInfoAsync<T>()
    {
        return await _provider.GetTypeMetadataAsync(typeof(T));
    }

    public async Task<AnalysisResult> AnalyzeTypeAsync<T>(string analyzerName)
    {
        return await _provider.AnalyzeTypeAsync(typeof(T), analyzerName);
    }
}
```

## Configuration Options

The library provides extensive configuration options through `MetadataOptions`:

```csharp
public class MetadataOptions
{
    public bool EnableConcurrentAnalysis { get; set; } = true;
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    public bool EnableCaching { get; set; } = true;
    public int MaxCacheSize { get; set; } = 10000;
    public bool ValidateAssemblies { get; set; } = true;
}
```

## Advanced Usage

### Assembly-Level Scanning

```csharp
[assembly: MetadataScan]
```

### Custom Type Filtering

```csharp
services.AddObjectInfoOverlord(options => {
    options.AddTypeFilter(type => !type.IsAbstract)
           .AddTypeFilter(type => type.GetCustomAttribute<ObsoleteAttribute>() == null);
});
```

### Analyzer Integration

```csharp
public async Task<T?> GetAnalysisResultAsync<T>(Type targetType, string analyzerName)
{
    var result = await _provider.GetAnalysisResultAsync<T>(targetType, analyzerName);
    return result;
}
```

## Performance Considerations

- Metadata is cached by default for optimal performance
- Thread-safe operations with minimal locking
- Configurable parallelism for concurrent analysis
- Efficient cache eviction based on size and age

## Integration with ObjectInfo Ecosystem

ObjectInfo.Overlord seamlessly integrates with:
- ObjectInfo core library for reflection metadata
- ObjectInfo.Deepdive for advanced code analysis
- Custom analyzers through the plugin system

## Requirements

- .NET 8.0 or higher
- Supported platforms: Windows, Linux, macOS

## Dependencies

- ObjectInfo
- ObjectInfo.Deepdive
- Microsoft.Extensions.DependencyInjection
- Serilog

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Related Projects

- [ObjectInfo](https://github.com/Rebel-Alliance-2023/ObjectInfo)
- [ObjectInfo.Deepdive](https://github.com/Rebel-Alliance-2023/ObjectInfo.Deepdive)

## Support

For support and questions:
- Open an issue in the GitHub repository
- Join our Discord community [link]
- Check our documentation [link]

## Acknowledgments

Special thanks to all contributors and The Rebel Alliance team for making this project possible.
