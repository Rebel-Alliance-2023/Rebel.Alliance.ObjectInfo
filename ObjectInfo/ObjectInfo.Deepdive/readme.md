# ObjectInfo.Deepdive

ObjectInfo.Deepdive is an extension library for ObjectInfo, designed to provide advanced code analysis capabilities. It leverages .NET 8+ features and the Roslyn compiler platform to offer deep insights into .NET objects and codebases.

## Key Components

1. **DeepDiveAnalysis**: The main entry point for deep analysis operations. It wraps an ObjectInfo instance and provides methods for various types of advanced analysis.

2. **AnalyzerManager**: Manages the collection of analyzers and orchestrates their execution.

3. **IAnalyzer Interface**: The base interface for all analyzers, defining the contract for analysis operations.

4. **IMethodAnalyzer and ITypeAnalyzer Interfaces**: Specialized interfaces for method and type analysis respectively.

5. **PluginLoader**: Responsible for discovering and loading external analyzer plugins.

6. **DeepDiveConfiguration**: Holds configuration options for customizing the analysis process.

## Plugin System

ObjectInfo.Deepdive implements a flexible plugin system allowing for easy extension with custom analyzers:

- Plugins implement the `IAnalyzerPlugin` interface.
- The `PluginLoader` class discovers and loads plugins from a specified directory.
- Analyzers from plugins are registered using dependency injection.

## Integration with ObjectInfo

- Uses extension methods (like `ToDeepDive()`) to provide a seamless transition from ObjectInfo to DeepDive analysis.
- Extends ObjectInfo's capabilities without duplicating its functionality.

## Key Features

- Method body analysis (e.g., cyclomatic complexity, LINQ complexity)
- Type hierarchy and dependency analysis
- Performance optimizations for large-scale analysis
- Async-first API design for responsive analysis of large codebases

## Current State

The library has a working core architecture with the first analyzers (CyclomaticComplexityAnalyzer and LinqComplexityAnalyzer) implemented and tested. It successfully integrates with ObjectInfo and provides a foundation for advanced code analysis.

## Future Developments

- Implementation of additional analyzers
- Enhancement of Roslyn integration for more sophisticated code analysis
- Development of specific analysis features like dependency graph generation
- Expansion of the plugin system
- Implementation of caching mechanisms for improved performance
- Expansion of test coverage and creation of integration tests

ObjectInfo.Deepdive is designed to provide developers with powerful insights into their code, enabling better understanding, maintenance, and optimization of .NET applications.
