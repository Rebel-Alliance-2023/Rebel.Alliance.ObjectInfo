# ObjectInfo.Deepdive.SpecificationGenerator Software Specification

## 1. Introduction

### 1.1 Purpose
ObjectInfo.Deepdive.SpecificationGenerator is a .NET source generator that automatically generates specification pattern classes for entity types, supporting both Entity Framework Core and Dapper. It provides a type-safe, compile-time approach to building flexible queries.

### 1.2 Scope
- Source generation of specification pattern classes
- Support for Entity Framework Core and Dapper
- Compile-time validation and error reporting
- Optional runtime support utilities
- IDE integration for IntelliSense and debugging

### 1.3 Version Information
- Version: 1.0.0
- .NET Version: .NET 8.0
- IDE Support: Visual Studio 2022, Rider 2023.3+
- Target Frameworks: .NET 8.0+

## 2. System Architecture

### 2.1 Project Structure
```plaintext
ObjectInfo.Deepdive.SpecificationGenerator/
├── SpecificationGenerator.Attributes/        # Marker attributes
├── SpecificationGenerator.Core/              # Source generator
├── SpecificationGenerator.Runtime/           # Optional runtime utilities
└── SpecificationGenerator.Tests/             # Test projects
```

### 2.2 Key Components

#### 2.2.1 Marker Attributes
```csharp
[GenerateSpecification(TargetOrm = OrmTarget.EntityFrameworkCore)]
public class Customer
{
    // Entity properties
}
```

#### 2.2.2 Source Generator
```csharp
[Generator]
public class SpecificationGenerator : IIncrementalGenerator
{
    // Source generation implementation
}
```

#### 2.2.3 Runtime Support
```csharp
public static class SpecificationExtensions
{
    public static IQueryable<T> ApplySpecification<T>(
        this IQueryable<T> query, 
        ISpecification<T> spec);
}
```

### 2.3 Dependencies
- Microsoft.CodeAnalysis (for source generation)
- Microsoft.EntityFrameworkCore (optional, for EF Core support)
- Dapper (optional, for Dapper support)

## 3. Detailed Design

### 3.1 Attributes Project

#### 3.1.1 Core Attributes
```csharp
public enum OrmTarget
{
    EntityFrameworkCore,
    Dapper
}

[AttributeUsage(AttributeTargets.Class)]
public class GenerateSpecificationAttribute : Attribute
{
    public OrmTarget TargetOrm { get; set; }
    public bool GenerateNavigationSpecs { get; set; } = true;
    public string? CustomBaseClass { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public class SpecificationOptionsAttribute : Attribute
{
    public bool IncludeInSpecification { get; set; } = true;
    public bool SupportContains { get; set; } = true;
    public bool CaseInsensitive { get; set; } = true;
}
```

### 3.2 Source Generator Project

#### 3.2.1 Generator Context
```csharp
public class SpecificationGeneratorContext
{
    public Compilation Compilation { get; }
    public INamedTypeSymbol TypeSymbol { get; }
    public AttributeData[] Attributes { get; }
    public GeneratorSyntaxContext SyntaxContext { get; }
}
```

#### 3.2.2 Syntax Receiver
```csharp
public class SpecificationSyntaxReceiver : ISyntaxContextReceiver
{
    public void OnVisitSyntaxNode(GeneratorSyntaxContext context);
}
```

#### 3.2.3 Code Generation
```csharp
public interface ISpecificationEmitter
{
    void EmitSpecification(SourceProductionContext context, TypeToGenerate type);
}

public class EFCoreSpecificationEmitter : ISpecificationEmitter
{
    // EF Core specific implementation
}

public class DapperSpecificationEmitter : ISpecificationEmitter
{
    // Dapper specific implementation
}
```

### 3.3 Generated Code Structure

#### 3.3.1 EF Core Specification
```csharp
public class CustomerSpecification : Specification<Customer>
{
    public string? NameContains { get; set; }
    public DateTime? OrderDateFrom { get; set; }
    public DateTime? OrderDateTo { get; set; }

    public Expression<Func<Customer, bool>> ToExpression()
    {
        var expression = PredicateBuilder.True<Customer>();
        
        if (!string.IsNullOrEmpty(NameContains))
            expression = expression.And(x => x.Name.Contains(NameContains));
            
        if (OrderDateFrom.HasValue)
            expression = expression.And(x => x.OrderDate >= OrderDateFrom.Value);
            
        return expression;
    }
}
```

#### 3.3.2 Dapper Specification
```csharp
public class CustomerSpecification : SqlSpecification<Customer>
{
    public string? NameContains { get; set; }
    public DateTime? OrderDateFrom { get; set; }
    public DateTime? OrderDateTo { get; set; }

    public (string Sql, DynamicParameters Parameters) ToSql()
    {
        var builder = new SqlBuilder();
        var parameters = new DynamicParameters();
        
        if (!string.IsNullOrEmpty(NameContains))
        {
            builder.Where("Name LIKE @NamePattern");
            parameters.Add("NamePattern", $"%{NameContains}%");
        }
        
        return (builder.ToString(), parameters);
    }
}
```

### 3.4 Runtime Support

#### 3.4.1 Query Extensions
```csharp
public static class QueryableExtensions
{
    public static IQueryable<T> Apply<T>(
        this IQueryable<T> query, 
        ISpecification<T> spec);
}

public static class DapperExtensions
{
    public static Task<IEnumerable<T>> QueryWithSpec<T>(
        this IDbConnection connection,
        SqlSpecification<T> spec);
}
```

## 4. Generation Process

### 4.1 Compilation Analysis
1. Identify types marked with [GenerateSpecification]
2. Analyze type properties and relationships
3. Determine required filter operations
4. Validate configuration

### 4.2 Code Generation
1. Generate specification class structure
2. Create filter properties based on entity properties
3. Generate expression/SQL building logic
4. Add navigation property handling
5. Implement required interfaces

### 4.3 Error Reporting
- Invalid attribute configuration
- Unsupported property types
- Missing dependencies
- Navigation property issues

## 5. Testing Strategy

### 5.1 Unit Tests
- Generator functionality
- Filter generation
- Expression building
- SQL generation

### 5.2 Snapshot Tests
- Generated code verification
- Multiple entity scenarios
- Different ORM targets

### 5.3 Integration Tests
- EF Core integration
- Dapper integration
- Runtime behavior
- Performance benchmarks

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

// Usage:
var spec = new OrderSpecification 
{
    OrderDateFrom = DateTime.Today.AddDays(-30),
    TotalAmountFrom = 1000m,
    Customer = new CustomerSpecification 
    { 
        NameContains = "Smith" 
    }
};

var orders = await context.Orders
    .Apply(spec)
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
    public string Category { get; set; }
}

// Usage:
var spec = new ProductSpecification 
{
    NameContains = "Widget",
    PriceFrom = 50m,
    Category = "Electronics"
};

var (sql, parameters) = spec.ToSql();
var products = await connection.QueryAsync<Product>(sql, parameters);
```

## 7. Performance Considerations

### 7.1 Compile Time
- Incremental generation support
- Minimal syntax tree traversal
- Efficient code generation
- Smart caching of type analysis

### 7.2 Runtime
- Efficient expression building
- Smart parameter handling
- Minimal allocations
- Query optimization

## 8. Future Enhancements

### 8.1 Version 1.1
- Custom specification base classes
- Additional filter operations
- Advanced sorting support
- Pagination integration

### 8.2 Version 1.2
- MongoDB support
- Custom SQL dialects
- Specification composition
- Advanced caching

## 9. Limitations and Constraints

### 9.1 Current Limitations
- Only supports EF Core and Dapper
- Limited to basic filter operations
- Single database context support
- Basic navigation property handling

### 9.2 Known Issues
- Complex nested navigations
- Dynamic specification composition
- Cross-context queries
- Generic type constraints

