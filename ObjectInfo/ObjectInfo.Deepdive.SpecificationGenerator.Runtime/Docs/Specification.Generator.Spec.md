# ObjectInfo.Deepdive.SpecificationGenerator Software Specification

## 1. Introduction

### 1.1 Purpose
ObjectInfo.Deepdive.SpecificationGenerator is a source generator library that automatically creates specification pattern implementations for .NET entity types. It supports both Entity Framework Core and Dapper, providing a type-safe, compile-time approach to building flexible queries with minimal runtime overhead.

### 1.2 Scope
- Compile-time generation of specification pattern classes
- Full support for Entity Framework Core and Dapper
- Comprehensive property filtering capabilities
- Navigation property support
- Compile-time validation and error reporting
- Runtime support utilities
- IDE integration (Visual Studio, Rider)

### 1.3 Version Information
- Version: 1.0.0
- .NET Version: .NET 8.0+
- IDE Support: Visual Studio 2022, Rider 2023.3+
- Package Dependencies:
  - Microsoft.CodeAnalysis (required)
  - Microsoft.EntityFrameworkCore (optional)
  - Dapper (optional)

## 2. System Architecture

### 2.1 Project Structure
```plaintext
ObjectInfo.Deepdive.SpecificationGenerator/
├── SpecificationGenerator.Attributes/        
│   ├── GenerateSpecificationAttribute.cs
│   ├── SpecificationPropertyAttribute.cs
│   └── SpecificationConfigurationAttribute.cs
├── SpecificationGenerator.Core/              
│   ├── SpecificationGenerator.cs
│   ├── Models/
│   │   └── SpecificationModels.cs
│   └── Emitters/
│       ├── EfCoreSpecificationEmitter.cs
│       └── DapperSpecificationEmitter.cs
├── SpecificationGenerator.Runtime/           
│   ├── BaseSpecification.cs
│   ├── SqlSpecification.cs
│   └── Extensions/
│       ├── QueryableExtensions.cs
│       └── DapperExtensions.cs
└── SpecificationGenerator.Tests/             
    ├── Unit/
    ├── Integration/
    └── Snapshots/
```

### 2.2 Key Components

#### 2.2.1 Attribute System
```csharp
public enum OrmTarget
{
    EntityFrameworkCore,
    Dapper,
    Both
}

[AttributeUsage(AttributeTargets.Class)]
public class GenerateSpecificationAttribute : Attribute
{
    public OrmTarget TargetOrm { get; set; }
    public bool GenerateNavigationSpecs { get; set; } = true;
    public bool GenerateDocumentation { get; set; } = true;
    public bool GenerateAsyncMethods { get; set; } = true;
    public string? TargetNamespace { get; set; }
    public Type? BaseClass { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public class SpecificationPropertyAttribute : Attribute
{
    public bool Ignore { get; set; }
    public bool GenerateContains { get; set; } = true;
    public bool GenerateStartsWith { get; set; } = true;
    public bool GenerateEndsWith { get; set; } = true;
    public bool CaseSensitive { get; set; }
    public bool GenerateRange { get; set; } = true;
    public bool GenerateNullChecks { get; set; } = true;
    public string? CustomExpression { get; set; }
}
```

#### 2.2.2 Generator Core
```csharp
[Generator]
public class SpecificationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Source generation implementation
    }
}
```

#### 2.2.3 Emitter System
```csharp
internal interface ISpecificationEmitter
{
    string EmitSpecification(SpecificationTarget target);
}

internal abstract class BaseSpecificationEmitter : ISpecificationEmitter
{
    protected readonly SourceProductionContext Context;
    public abstract string EmitSpecification(SpecificationTarget target);
}
```

### 2.3 Generated Code Structure

#### 2.3.1 Entity Framework Core Specification
```csharp
public class CustomerSpecification : BaseSpecification<Customer>
{
    // Generated properties
    public string? NameContains { get; set; }
    public DateTime? OrderDateFrom { get; set; }
    public DateTime? OrderDateTo { get; set; }
    public bool? IsActive { get; set; }

    // Navigation specifications
    public OrderSpecification OrderSpecification { get; set; }

    // Generated methods
    private void ApplyCriteria()
    {
        // Generated filter logic
    }

    protected override void AddIncludes()
    {
        // Generated include logic
    }
}
```

#### 2.3.2 Dapper Specification
```csharp
public class CustomerSpecification : SqlSpecification<Customer>
{
    // Generated properties
    public string? NameContains { get; set; }
    public DateTime? OrderDateFrom { get; set; }
    public DateTime? OrderDateTo { get; set; }
    public bool? IsActive { get; set; }

    // SQL generation
    protected override void BuildWhereClause()
    {
        // Generated SQL building logic
    }

    // Async query methods
    public async Task<IEnumerable<Customer>> QueryAsync(
        IDbConnection connection, 
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        // Generated query implementation
    }
}
```

## 3. Feature Details

### 3.1 Property Filtering Support
- String Operations:
  - Contains (case-sensitive/insensitive)
  - StartsWith (case-sensitive/insensitive)
  - EndsWith (case-sensitive/insensitive)
  - Custom SQL expressions
- Numeric/DateTime Operations:
  - Range (From/To)
  - Equality
  - Null checks
- Boolean Operations:
  - True/False/Null conditions
- Custom Operations:
  - User-defined expressions
  - Custom SQL templates

### 3.2 Navigation Property Support
- Nested specifications
- Eager loading configuration
- Join clause generation
- Related entity filtering

### 3.3 Query Features
- Pagination support
- Sorting capabilities
- Async execution
- Transaction support
- Parameter management
- SQL injection prevention

## 4. Runtime Support

### 4.1 Base Classes
```csharp
public abstract class BaseSpecification<T>
{
    protected Expression<Func<T, bool>> Criteria { get; set; }
    protected List<Expression<Func<T, object>>> Includes { get; }
    protected Expression<Func<T, object>>? OrderBy { get; set; }
}

public abstract class SqlSpecification<T>
{
    protected StringBuilder WhereClause { get; }
    protected DynamicParameters Parameters { get; }
    public abstract string ToSql();
}
```

### 4.2 Extension Methods
```csharp
public static class QueryableExtensions
{
    public static IQueryable<T> Apply<T>(
        this IQueryable<T> query, 
        ISpecification<T> spec);
}

public static class DapperExtensions
{
    public static Task<IEnumerable<T>> QueryAsync<T>(
        this IDbConnection connection,
        SqlSpecification<T> spec);
}
```

## 5. Testing Strategy

### 5.1 Unit Tests
- Generator functionality
- Attribute processing
- Code emission
- SQL generation
- Expression building

### 5.2 Integration Tests
- EF Core query generation
- Dapper query execution
- Navigation property handling
- Transaction support
- Async operations

### 5.3 Snapshot Tests
- Generated code validation
- Multi-target verification
- Edge case handling

## 6. Usage Examples

### 6.1 Entity Framework Core
```csharp
[GenerateSpecification(TargetOrm = OrmTarget.EntityFrameworkCore)]
public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public Customer Customer { get; set; }
}

// Usage
var spec = new OrderSpecification()
    .WithCustomer(c => c.NameContains = "Smith")
    .WithDateRange(startDate, endDate)
    .WithMinAmount(1000m);

var orders = await context.Orders
    .ApplySpecification(spec)
    .ToListAsync();
```

### 6.2 Dapper
```csharp
[GenerateSpecification(TargetOrm = OrmTarget.Dapper)]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// Usage
var spec = new ProductSpecification 
{
    NameContains = "Widget",
    PriceFrom = 50m,
    Category = "Electronics"
};

using var connection = new SqlConnection(connectionString);
var products = await spec.QueryAsync(connection);
```

## 7. Error Handling

### 7.1 Compile-Time Errors
- Invalid attribute configuration
- Unsupported property types
- Missing dependencies
- Navigation property issues

### 7.2 Runtime Validation
- Parameter validation
- SQL injection prevention
- Type safety checks
- Connection state validation

## 8. Future Enhancements

### 8.1 Version 1.1
- Advanced sorting capabilities
- Dynamic specification composition
- Custom filter operations
- Performance optimizations

### 8.2 Version 1.2
- MongoDB support
- Custom SQL dialects
- Specification caching
- Query plan optimization

## 9. Dependencies

### 9.1 Required
- Microsoft.CodeAnalysis.CSharp (>= 4.8.0)
- Microsoft.CodeAnalysis.Analyzers (>= 3.3.4)

### 9.2 Optional
- Microsoft.EntityFrameworkCore (>= 8.0.0)
- Dapper (>= 2.1.24)