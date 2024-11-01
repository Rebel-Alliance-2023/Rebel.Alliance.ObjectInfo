# Rebel.Alliance.ObjectInfo

![image](https://user-images.githubusercontent.com/3196088/235502858-8f615664-a196-45c8-bb07-df0ec6fc2e2a.png)

https://www.nuget.org/packages/ObjectInfo/1.0.0

Presenting a comprehensive library to easily query the DotNet Reflection API which multi-targets .NetStandard2.0, .NetStandard2.1, and .NET 8.0

The ObjectInfo Broker queries the Reflection API and converts the data from the various internal types to string properties, so that any client can read the data without needing references to hidden or protected libraries. Thus, this library is ideal for developers developing an "Object Inspector" in Blazor for instance.

The top-level object is ObjectInfo, which contains the TypeInfo class. TypeInfo now provides comprehensive metadata about:
- Implemented Interfaces
- Properties (PropInfo)
- Methods (MethodInfo)
- Constructors (ConstructorInfo)
- Fields (FieldInfo)
- Events (EventInfo)
- Generic Type Information
  - Generic Parameters
  - Generic Constraints
  - Generic Type Arguments

All member types (Type, Method, Property, Constructor, Field, and Event) contain CustomAttributes collections, providing complete metadata coverage.

ObjectInfo includes a configuration object that controls system type visibility and will be expanded to provide "slices" of the metadata when performance is an issue.

## Usage (from our unit tests)

### Basic Type Information

```csharp
TestClass testClass = new TestClass() { Name = "Joe The Tester" };
IObjectInfoBroker objectInfoBroker = new ObjectInfoBroker();

// Get ObjectInfo object
ObjInfo expectedObjectInfo = ObjectInfoService.RetrieveObjectInfo(objectInfoBroker, testClass);
```

### Member Navigation

```csharp
// Navigate Implemented Interfaces
string? implementedInterfaceName = expectedObjectInfo!.TypeInfo!.ImplementedInterfaces!
    .FirstOrDefault(a => a.Name.Equals("ITestClass")).Name;

// Navigate Methods
string? methodInfo = expectedObjectInfo!.TypeInfo!.MethodInfos!
    .FirstOrDefault(a => a.Name.Equals("EnsureCompliance")).Name;

// Navigate Properties
string? propInfo = expectedObjectInfo!.TypeInfo!.PropInfos!
    .FirstOrDefault(a => a.Name.Equals("Name")).Name;

// Navigate Constructors
var constructor = expectedObjectInfo!.TypeInfo!.ConstructorInfos!.FirstOrDefault();

// Navigate Fields
var field = expectedObjectInfo!.TypeInfo!.FieldInfos!
    .FirstOrDefault(f => f.Name.Equals("FieldName"));

// Navigate Events
var eventInfo = expectedObjectInfo!.TypeInfo!.EventInfos!
    .FirstOrDefault(e => e.Name.Equals("EventName"));
```

### Generic Type Information

```csharp
// For generic types
var genericType = expectedObjectInfo!.TypeInfo;
if (genericType.IsGenericTypeDefinition)
{
    // Access generic parameters and constraints
    var genericParam = genericType.GenericParameters.FirstOrDefault();
    bool hasConstructorConstraint = genericParam.HasDefaultConstructorConstraint;
}
```

### Attribute Navigation

```csharp
// Navigate Type Attributes
string? typeAttrInfo = expectedObjectInfo!.TypeInfo!.CustomAttrs!
    .FirstOrDefault(a => a.Name.Equals("IsCompliant")).Name;

// Navigate Method Attributes
var methodInfo = expectedObjectInfo!.TypeInfo!.MethodInfos!
    .FirstOrDefault(a => a.Name.Equals("EnsureCompliance"));
string? methodAttrInfo = methodInfo.CustomAttrs!
    .FirstOrDefault(a => a.Name.Equals("IsCompliant")).Name;

// Navigate Property Attributes
var propInfo = expectedObjectInfo!.TypeInfo!.PropInfos!
    .FirstOrDefault(a => a.Name.Equals("Name"));
string? propAttrInfo = propInfo.CustomAttrs!
    .FirstOrDefault(a => a.Name.Equals("IsCompliant")).Name;
```

The library now provides complete reflection metadata access with a clean, consistent API surface, making it ideal for inspection tools, code analysis, and metadata-driven applications.
