# Getting Started with ObjectInfo.Deepdive.SpecificationGenerator

## Introduction
ObjectInfo.Deepdive.SpecificationGenerator is a powerful source generation system that automatically creates specification pattern implementations for your entity classes. It supports both Entity Framework Core and Dapper, providing a type-safe, compile-time approach to building flexible queries.

## Architecture Overview

The specification generator system consists of three main libraries that work together:

### 1. ObjectInfo.Deepdive.SpecificationGenerator.Attributes (.NET Standard 2.0)
- Contains the marker attributes used to annotate your entity classes
- Defines configuration attributes for customizing specification generation
- Targets .NET Standard 2.0 for maximum compatibility
- Used at both compile-time (by the generator) and runtime (by your code)
- Lightweight with minimal dependencies

### 2. ObjectInfo.Deepdive.SpecificationGenerator.Core (.NET 8.0)
- Implements the actual source generation logic
- Uses the Roslyn compiler API to analyze your code and generate specifications
- Runs during compilation as a development-time dependency
- Never referenced directly by your code
- Integrated automatically through the build process
- Contains:
  - Code analysis logic
  - Specification template generation
  - EF Core and Dapper-specific emitters
  - Build-time validation

### 3. ObjectInfo.Deepdive.SpecificationGenerator.Runtime (.NET 8.0)
- Provides the base classes and interfaces used by generated specifications
- Contains implementations for both EF Core and Dapper
- Referenced directly by your application code
- Includes pre-built components for:
  - Base specification classes
  - Query builders
  - Expression handling
  - Database integration

## Installation

In your project file, add the following package references:

```xml
<ItemGroup>
    <!-- Runtime components and base classes -->
    <PackageReference Include="ObjectInfo.Deepdive.SpecificationGenerator.Runtime" Version="1.0.0" />
    
    <!-- Optional ORM packages -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.10" />
    <PackageReference Include="Dapper" Version="2.1.35" />
</ItemGroup>
```

Note: You don't need to reference the Attributes or Core packages directly - they are included automatically through the Runtime package.

## How It Works

1. **Build-Time Processing**:
   - When you build your project, the Core generator activates
   - It scans your code for classes marked with `[GenerateSpecification]`
   - Analyzes the entity properties and relationships
   - Generates specification classes tailored to your entities

2. **Generated Code**:
   - Specifications are generated as partial classes in your project
   - They inherit from the Runtime base classes
   - Include strongly-typed query builders
   - Support both EF Core and Dapper implementations

3. **Runtime Execution**:
   - Your code uses the generated specifications
   - The Runtime library provides the execution infrastructure
   - Queries are executed through your chosen ORM

## Basic Usage

### 1. Mark Your Entity Classes

```csharp
using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;

[GenerateSpecification(TargetOrm = OrmTarget.Both)]
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    
    public List<Order> Orders { get; set; }
}
```

### 2. Configure Property Behavior

```csharp
public class Customer
{
    [SpecificationProperty(
        GenerateContains = true,
        GenerateStartsWith = true,
        CaseSensitive = false)]
    public string Name { get; set; }

    [SpecificationProperty(GenerateRange = true)]
    public DateTime CreatedDate { get; set; }

    [SpecificationProperty(Ignore = true)]
    public string InternalNotes { get; set; }
}
```

### 3. Use Generated Specifications

#### Entity Framework Core:
```csharp
public async Task<List<Customer>> GetActiveCustomersAsync(DbContext context)
{
    var spec = new CustomerSpecification
    {
        IsActive = true,
        NameContains = "Smith",
        CreatedDateFrom = DateTime.Now.AddDays(-30)
    };

    return await context.Customers
        .ApplySpecification(spec)
        .ToListAsync();
}
```

#### Dapper:
```csharp
public async Task<IEnumerable<Customer>> GetCustomersAsync(IDbConnection connection)
{
    var spec = new CustomerSpecification_Dapper
    {
        IsActive = true,
        CreatedDateFrom = DateTime.Now.AddDays(-30)
    };

    return await connection.QueryWithSpecificationAsync(spec);
}
```

## Advanced Features

### 1. Navigation Properties
```csharp
var spec = new CustomerSpecification()
    .WithOrders(orders => 
    {
        orders.Status = OrderStatus.Pending;
        orders.AmountFrom = 1000m;
    });
```

### 2. Combining Specifications
```csharp
var activeSpec = new CustomerSpecification { IsActive = true };
var premiumSpec = new CustomerSpecification { IsPremium = true };
var combinedSpec = activeSpec.Or(premiumSpec);
```

### 3. Assembly-Level Configuration
```csharp
[assembly: SpecificationConfiguration(
    DefaultNamespace = "MyApp.Specifications",
    DefaultGenerateAsync = true,
    DefaultGenerateDocumentation = true
)]
```

## Best Practices

1. **Performance**:
   - Use appropriate indexes for commonly filtered properties
   - Consider pagination for large result sets
   - Use eager loading appropriately with navigation properties

2. **Organization**:
   - Keep specifications close to their entity definitions
   - Use a consistent naming convention for specifications
   - Consider creating base specifications for common filters

3. **Error Handling**:
   - Validate specification parameters before execution
   - Handle null values appropriately
   - Use proper exception handling for database operations

4. **Navigation Properties**:
   - Use `GenerateNavigationSpecs = true` for entities with navigation properties
   - Consider the impact on query performance
   - Use explicit loading when appropriate

## Troubleshooting

1. **Build Errors**:
   - Ensure all required packages are installed
   - Clean and rebuild the solution
   - Check the build output for generator diagnostics

2. **Runtime Issues**:
   - Verify ORM configuration
   - Check generated SQL using logging
   - Validate specification parameters

3. **Performance Problems**:
   - Review generated queries
   - Check database indexing
   - Consider query optimization

## Contributing

Contributions are welcome! Please see our [Contribution Guidelines](CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues and questions:
- GitHub Issues: [Project Issues](https://github.com/Rebel-Alliance-2023/ObjectInfo.Deepdive/issues)
- Documentation: [Project Wiki](https://github.com/Rebel-Alliance-2023/ObjectInfo.Deepdive/wiki)
