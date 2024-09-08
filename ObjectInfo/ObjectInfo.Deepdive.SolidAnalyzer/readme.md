# Solid Analyzer

## Introduction

The Solid Analyzer is a tool designed to analyze C# code for adherence to the SOLID principles of object-oriented design. This document provides an overview of the analyzer's implementation and its corresponding tests.

## SolidAnalyzer.cs

The `SolidAnalyzer` class implements the `IAnalyzer` interface and provides methods to analyze code for each of the SOLID principles.

### Key Components

1. **Constructor**: Initializes the analyzer with a logger and configuration.
2. **AnalyzeAsync**: The main entry point for analysis, which delegates to `AnalyzeTypeAsync`.
3. **AnalyzeTypeAsync**: Coordinates the analysis of all SOLID principles for a given type.
4. Individual analysis methods for each SOLID principle:
   - `AnalyzeSrp`
   - `AnalyzeOcp`
   - `AnalyzeLsp`
   - `AnalyzeIsp`
   - `AnalyzeDip`



### SolidAnalyzerTests.RunAnalysisForObject Method

The `RunAnalysisForObject` method is a crucial component of the testing framework. It encapsulates the process of running the SOLID analysis on a given object and returning the results. Let's break down this method in detail:

```csharp
private async Task<SolidAnalysisResult?> RunAnalysisForObject<T>(T testObject) where T : class
```

#### Method Signature

- **Return Type**: `Task<SolidAnalysisResult?>`
  - This method is asynchronous, returning a Task that resolves to a nullable SolidAnalysisResult.
- **Generic Parameter**: `<T> where T : class`
  - The method is generic, accepting any reference type as the test object.
- **Parameter**: `T testObject`
  - The object to be analyzed for SOLID principles adherence.

#### Method Body

1. **Service Retrieval**:
   ```csharp
   var objectInfoBroker = _serviceProvider.GetRequiredService<IObjectInfoBroker>();
   var analyzerManager = _serviceProvider.GetRequiredService<AnalyzerManager>();
   ```
   - Retrieves necessary services from the dependency injection container.

2. **Logging**:
   ```csharp
   _logger.Information($"Analyzing object of type: {typeof(T).Name}");
   ```
   - Logs the type of object being analyzed for debugging purposes.

3. **Object Info Creation**:
   ```csharp
   var objInfo = (ObjInfo)objectInfoBroker.GetObjectInfo(testObject);
   _logger.Information($"ObjectInfo created: {objInfo.TypeInfo.Name}");
   ```
   - Uses the `IObjectInfoBroker` to create an `ObjInfo` instance from the test object.
   - This `ObjInfo` contains metadata about the object, which is used in the analysis.

4. **Analysis Execution**:
   ```csharp
   var deepDiveAnalysis = new DeepDiveAnalysis(objInfo, analyzerManager, _logger);
   _logger.Information("Running all analyzers...");
   var results = await deepDiveAnalysis.RunAllAnalyzersAsync();
   ```
   - Creates a `DeepDiveAnalysis` instance and runs all registered analyzers.
   - This is where the actual SOLID analysis occurs.

5. **Results Processing**:
   ```csharp
   _logger.Information($"Number of analysis results: {results.Count()}");
   if (results.Any())
   {
       foreach (var result in results)
       {
           _logger.Information($"Analyzer: {result.AnalyzerName}, Summary: {result.Summary}");
       }
       // ...
   }
   ```
   - Logs the number of results and details of each analyzer's output.

6. **SOLID Result Extraction**:
   ```csharp
   var solidResult = results.FirstOrDefault(r => r.AnalyzerName == "SOLID Principles Analyzer") as SolidAnalysisResult;
   if (solidResult == null)
   {
       _logger.Warning("SOLID Principles Analyzer result not found in the results");
   }
   else
   {
       _logger.Information("SOLID Principles Analyzer result found");
   }
   return solidResult;
   ```
   - Extracts the SOLID analysis result from the list of all results.
   - Logs whether the SOLID result was found or not.

7. **Error Handling**:
   ```csharp
   else
   {
       _logger.Warning("No analysis results returned");
       return null;
   }
   ```
   - If no results are returned, it logs a warning and returns null.

#### Purpose and Usage

This method serves as a bridge between the test cases and the SOLID analyzer. It encapsulates the complexity of:
- Setting up the analysis environment
- Running the analysis
- Extracting relevant results
- Providing detailed logging for troubleshooting

By using this method, each test case can focus on asserting the expected outcomes rather than the mechanics of running the analysis. This promotes cleaner, more maintainable test code.

#### Error Handling and Robustness

The method includes several error handling and logging steps:
- It logs each stage of the process, aiding in debugging.
- It handles cases where no results are returned or where the SOLID analyzer result is not found.
- By returning a nullable result, it allows test cases to handle scenarios where analysis fails or produces unexpected results.

This robust approach ensures that tests can provide meaningful feedback even when the analysis doesn't proceed as expected.



### SOLID Principle Analysis

#### 1. Single Responsibility Principle (SRP)

**Analysis Method**: `AnalyzeSrp`

This method counts the number of public members (methods and properties) in a class. If this count exceeds a configurable threshold, it's considered a violation of SRP.

**Key Points**:
- Uses `typeInfo.MethodInfos` and `typeInfo.PropInfos` to count public members.
- Compares against `_config.MaxMethodsPerClass` (default: 10).

#### 2. Open-Closed Principle (OCP)

**Analysis Method**: `AnalyzeOcp`

This method checks if a class is open for extension but closed for modification. It does this by examining if the class is abstract or contains virtual methods.

**Key Points**:
- Checks `typeInfo.IsAbstract` to determine if the class is abstract.
- Counts virtual methods using `typeInfo.MethodInfos.Count(m => m.IsVirtual)`.
- Logs details about the analysis for debugging purposes.

#### 3. Liskov Substitution Principle (LSP)

**Analysis Method**: `AnalyzeLsp`

This method attempts to identify potential LSP violations by examining method overrides in derived classes.

**Key Points**:
- Uses reflection to load the assembly and find the analyzed type.
- Compares methods in the derived class with those in the base class.
- Flags methods that override base methods as potential LSP violations.

#### 4. Interface Segregation Principle (ISP)

**Analysis Method**: `AnalyzeIsp`

This method checks if a class fully implements its interfaces and if those interfaces are not too large or general.

**Key Points**:
- Examines `typeInfo.ImplementedInterfaces`.
- Checks if all interface methods are implemented in the class.
- Logs warnings for null interfaces or methods for debugging.

#### 5. Dependency Inversion Principle (DIP)

**Analysis Method**: `AnalyzeDip`

This method analyzes a class's dependencies, checking if it depends on abstractions rather than concrete implementations.

**Key Points**:
- Examines properties and constructor parameters.
- Uses reflection to get constructor information.
- Checks if dependencies are interfaces or abstract types.

## SolidAnalyzerTests.cs

This file contains unit tests for the SolidAnalyzer, verifying its ability to detect violations of each SOLID principle.

### Key Components

1. **Constructor**: Sets up the testing environment, including dependency injection and logging.
2. **RunAnalysisForObject**: A helper method that runs the analysis for a given object.
3. Individual test methods for each SOLID principle:
   - `AnalyzeType_ShouldIdentifySrpViolation`
   - `AnalyzeType_ShouldRecognizeGoodOcp`
   - `AnalyzeType_ShouldIdentifyLspViolation`
   - `AnalyzeType_ShouldIdentifyIspViolation`
   - `AnalyzeType_ShouldIdentifyDipViolation`

### Test Classes and SOLID Principle Violations

#### 1. SRP Violation

**Test Class**: `SrpViolationClass`

This class violates SRP by having too many methods (11), exceeding the default threshold of 10.

```csharp
public class SrpViolationClass
{
    public void Method1() { }
    // ... (11 methods in total)
    public void Method11() { }
}
```

#### 2. OCP Compliance

**Test Classes**: `OcpCompliantClass`, `OcpCompliantClassImplementation`, `ConcreteOcpCompliantClass`

These classes demonstrate OCP compliance through the use of abstract classes and virtual methods.

```csharp
public abstract class OcpCompliantClass
{
    public virtual void Method1() { }
    public virtual void Method2() { }
}

public class ConcreteOcpCompliantClass : OcpCompliantClassImplementation
{
    public override void Method1() { }
    public override void Method2() { }
}
```

#### 3. LSP Violation

**Test Classes**: `Parent`, `LspViolationClass`

`LspViolationClass` violates LSP by changing the behavior of the `Method` inherited from `Parent`.

```csharp
public class Parent
{
    public virtual int Method(int a) => a + 1;
}

public class LspViolationClass : Parent
{
    public override int Method(int a) => a * 2; // Violates LSP
}
```

#### 4. ISP Violation

**Test Classes**: `ILargeInterface`, `IspViolationClass`

`IspViolationClass` violates ISP by implementing `ILargeInterface` but not properly implementing all its methods.

```csharp
public interface ILargeInterface
{
    void Method1();
    void Method2();
    void Method3();
}

public class IspViolationClass : ILargeInterface
{
    public void Method1() { }
    public void Method2() { }
    public void Method3() { throw new NotImplementedException(); } // Violates ISP
}
```

#### 5. DIP Violation

**Test Classes**: `ConcreteClass`, `DipViolationClass`

`DipViolationClass` violates DIP by depending on a concrete implementation (`ConcreteClass`) rather than an abstraction.

```csharp
public class ConcreteClass { }

public class DipViolationClass
{
    private readonly ConcreteClass _dependency;

    public DipViolationClass(ConcreteClass dependency) // Violates DIP
    {
        _dependency = dependency;
    }
}
```

## End Note

The SolidAnalyzer provides a starting point for analysis of SOLID principle adherence in C# code. While it can detect some common violations, it's important to note there are many more samples to add to the tests before we call this library complete. Moreover, work on this project has shown that the underlying ObjectInfo library needs some tweaking to take some of the Reflection strain on this library.
