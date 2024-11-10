using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using Dapper;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Models;
using System.Linq.Expressions;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure.TestFixtures;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Tests
{
    public class BatchOperationTests : IntegrationTestBase, IClassFixture<DatabaseFixture>
    {
        public BatchOperationTests(DatabaseFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact]
        public async Task BatchUpdate_ShouldModifyAllMatchingRecords()
        {
            // Arrange
            await SeedTestDataAsync();

            var creditIncrease = 1000m;
            var spec = new TestSpecification<Customer>(c =>
                c.CustomerType == CustomerType.Regular && c.IsActive);

            var whereClause = spec.GetWhereClause();
            var parameters = new DynamicParameters(spec.GetParameters());
            parameters.Add("@CreditIncrease", creditIncrease);

            // Act
            var updateSql = $@"
        UPDATE Customers 
        SET CreditLimit = CreditLimit + @CreditIncrease
        WHERE {whereClause}";

            var affectedRows = await WithConnection(async conn =>
                await conn.ExecuteAsync(updateSql, parameters));

            // Assert
            affectedRows.Should().BeGreaterThan(0);

            var updatedCustomers = await WithConnection(async conn =>
                await conn.QueryAsync<Customer>(
                    $"SELECT * FROM Customers WHERE {whereClause}",
                    spec.GetParameters()));

            updatedCustomers.Should().NotBeEmpty();
            foreach (var customer in updatedCustomers)
            {
                customer.CreditLimit.Should().BeGreaterThan(5000m); // Original CreditLimit + creditIncrease
            }
        }



        [Fact]
        public async Task BatchDelete_ShouldRemoveAllMatchingRecords()
        {
            // Arrange
            await SeedTestDataAsync();

            // Precompute the cutoff date to simplify the expression
            var cutoffDate = DateTime.Now.AddMonths(-6);

            var spec = new TestSpecification<Order>(o =>
                o.Status == OrderStatus.Cancelled &&
                o.OrderDate < cutoffDate);

            var whereClause = spec.GetWhereClause();
            var parameters = spec.GetParameters();

            // Log the SQL and parameters for debugging
            Output.WriteLine($"Delete SQL: DELETE FROM Orders WHERE {whereClause}");
            foreach (var paramName in parameters.Keys)
            {
                var paramValue = parameters[paramName];
                Output.WriteLine($"Parameter {paramName}: {paramValue}");
            }

            // First, count matching records
            var initialCount = await WithConnection(async conn =>
                await conn.QuerySingleAsync<int>(
                    $"SELECT COUNT(*) FROM Orders WHERE {whereClause}",
                    parameters));

            if (initialCount == 0)
            {
                // Seed an order matching the criteria
                await WithConnection(async conn =>
                {
                    var order = new Order
                    {
                        CustomerId = 1,
                        OrderNumber = "ORD-DELETE-TEST",
                        Status = OrderStatus.Cancelled,
                        OrderDate = DateTime.Now.AddMonths(-7),
                        TotalAmount = 0m,
                        IsPriority = false
                    };

                    var insertSql = @"
                INSERT INTO Orders (CustomerId, OrderNumber, Status, OrderDate, TotalAmount, IsPriority)
                VALUES (@CustomerId, @OrderNumber, @Status, @OrderDate, @TotalAmount, @IsPriority)";

                    await conn.ExecuteAsync(insertSql, new
                    {
                        order.CustomerId,
                        order.OrderNumber,
                        order.Status,
                        order.OrderDate,
                        order.TotalAmount,
                        order.IsPriority
                    });
                });

                initialCount = 1;
            }

            // Act
            var deleteSql = $@"
        DELETE FROM Orders
        WHERE {whereClause}";

            // Log the deleteSql and parameters again before execution
            Output.WriteLine($"Executing Delete SQL: {deleteSql}");
            foreach (var paramName in parameters.Keys)
            {
                var paramValue = parameters[paramName];
                Output.WriteLine($"Parameter {paramName}: {paramValue}");
            }

            var affectedRows = await WithConnection(async conn =>
            {
                using var transaction = conn.BeginTransaction();
                try
                {
                    var result = await conn.ExecuteAsync(
                        deleteSql,
                        parameters,
                        transaction);

                    await transaction.CommitAsync();
                    return result;
                }
                catch (Exception ex)
                {
                    Output.WriteLine($"Error executing DELETE: {ex.Message}");
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            // Assert
            affectedRows.Should().Be(initialCount);

            // Verify deletion
            var remainingCount = await WithConnection(async conn =>
                await conn.QuerySingleAsync<int>(
                    $"SELECT COUNT(*) FROM Orders WHERE {whereClause}",
                    parameters));

            remainingCount.Should().Be(0);
        }




        [Fact]
        public async Task BatchInsert_ShouldInsertAllRecords()
        {
            // Arrange
            int batchSize = 10;
            var products = Enumerable.Range(0, batchSize)
                .Select(i => new Product
                {
                    Name = $"Test Product {i}",
                    SKU = $"SKU-{Guid.NewGuid():N}",
                    Price = 100m + i * 10,
                    IsAvailable = true,
                    Category = ProductCategory.Electronics,
                    StockLevel = 100,
                    CreatedDate = DateTime.UtcNow
                })
                .ToList();

            // Act
            var insertSql = @"
                INSERT INTO Products (
                    Name, SKU, Price, IsAvailable, Category, 
                    StockLevel, CreatedDate
                )
                VALUES (
                    @Name, @SKU, @Price, @IsAvailable, @Category, 
                    @StockLevel, @CreatedDate
                )";

            var affectedRows = await WithConnection(async conn =>
            {
                using var transaction = conn.BeginTransaction();
                try
                {
                    var result = await conn.ExecuteAsync(
                        insertSql,
                        products,
                        transaction);

                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            // Assert
            affectedRows.Should().Be(batchSize);

            // Verify insertions
            var spec = new TestSpecification<Product>(p => p.Name.StartsWith("Test Product"));

            var insertedProducts = await WithConnection(async conn =>
                await conn.QueryAsync<Product>(
                    $"SELECT * FROM Products WHERE {spec.GetWhereClause()}",
                    spec.GetParameters()));

            insertedProducts.Should().HaveCount(batchSize);
            foreach (var product in insertedProducts)
            {
                product.Name.Should().StartWith("Test Product");
                product.IsAvailable.Should().BeTrue();
                product.Category.Should().Be(ProductCategory.Electronics);
            }
        }

        [Fact]
        public async Task BatchUpsert_ShouldUpdateOrInsertRecords()
        {
            // Arrange
            await SeedTestDataAsync();

            var spec = new TestSpecification<Product>(p => p.StockLevel < 10);
            var existingProducts = await WithConnection(async conn =>
                await conn.QueryAsync<Product>(
                    $"SELECT * FROM Products WHERE {spec.GetWhereClause()}",
                    spec.GetParameters()));

            // Prepare updates for existing products
            var updates = existingProducts.Select(p => new
            {
                p.SKU,
                NewStockLevel = 100,
                LastRestockDate = DateTime.UtcNow,
                Name = p.Name, // Preserve existing Name
                Price = p.Price, // Preserve existing Price
                IsAvailable = p.IsAvailable, // Preserve existing IsAvailable
                Category = p.Category, // Preserve existing Category
                CreatedDate = p.CreatedDate // Preserve existing CreatedDate
            }).ToList();

            // Prepare new products with all required fields
            var newProducts = Enumerable.Range(0, 5).Select(i => new
            {
                SKU = $"NEW-SKU-{Guid.NewGuid():N}",
                NewStockLevel = 50,
                LastRestockDate = DateTime.UtcNow,
                Name = $"New Product {i}", // Provide a Name for new products
                Price = 75m + i * 5, // Assign a Price for new products
                IsAvailable = true, // Set IsAvailable
                Category = ProductCategory.Electronics, // Assign a Category
                CreatedDate = DateTime.UtcNow // Set CreatedDate
            }).ToList();

            // Combine updates and new inserts
            var allRecords = updates.Concat(newProducts).ToList();

            // Act
            var upsertSql = @"
        INSERT INTO Products (SKU, StockLevel, LastRestockDate, Name, Price, IsAvailable, Category, CreatedDate)
        VALUES (@SKU, @NewStockLevel, @LastRestockDate, @Name, @Price, @IsAvailable, @Category, @CreatedDate)
        ON CONFLICT(SKU) DO UPDATE SET
            StockLevel = excluded.StockLevel,
            LastRestockDate = excluded.LastRestockDate";

            var affectedRows = await WithConnection(async conn =>
            {
                using var transaction = conn.BeginTransaction();
                try
                {
                    var result = await conn.ExecuteAsync(
                        upsertSql,
                        allRecords,
                        transaction);

                    await transaction.CommitAsync();
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            // Assert
            affectedRows.Should().Be(allRecords.Count);

            // Verify upserts
            var updatedProducts = await WithConnection(async conn =>
                await conn.QueryAsync<Product>(
                    "SELECT * FROM Products WHERE SKU IN @Skus",
                    new { Skus = allRecords.Select(r => r.SKU).ToList() }));

            updatedProducts.Should().HaveCount(allRecords.Count);
            foreach (var product in updatedProducts)
            {
                product.StockLevel.Should().BeGreaterOrEqualTo(50);
                product.LastRestockDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

                // Verify that existing products retain their original Name and other fields
                var originalRecord = allRecords.FirstOrDefault(r => r.SKU == product.SKU);
                if (originalRecord != null && existingProducts.Any(p => p.SKU == product.SKU))
                {
                    product.Name.Should().Be(originalRecord.Name);
                    product.Price.Should().Be(originalRecord.Price);
                    product.IsAvailable.Should().Be(originalRecord.IsAvailable);
                    product.Category.Should().Be(originalRecord.Category);
                    product.CreatedDate.Should().Be(originalRecord.CreatedDate);
                }
                else
                {
                    product.Name.Should().StartWith("New Product");
                    product.Price.Should().BeGreaterThan(0);
                    product.IsAvailable.Should().BeTrue();
                    product.Category.Should().Be(ProductCategory.Electronics);
                    product.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
                }
            }
        }


        [Fact]
        public async Task BatchOperations_ShouldBeAtomic()
        {
            // Arrange
            await SeedTestDataAsync();

            var spec = new TestSpecification<Order>(o => o.Status == OrderStatus.Pending);
            var ordersToUpdate = await WithConnection(async conn =>
                await conn.QueryAsync<Order>(
                    $"SELECT * FROM Orders WHERE {spec.GetWhereClause()}",
                    spec.GetParameters()));

            if (!ordersToUpdate.Any())
            {
                // Seed an order with Status == Pending
                await WithConnection(async conn =>
                {
                    var order = new Order
                    {
                        CustomerId = 1,
                        OrderNumber = "ORD-PENDING-TEST",
                        Status = OrderStatus.Pending,
                        OrderDate = DateTime.Now,
                        TotalAmount = 100m,
                        IsPriority = false
                    };

                    var insertSql = @"
                        INSERT INTO Orders (CustomerId, OrderNumber, Status, OrderDate, TotalAmount, IsPriority)
                        VALUES (@CustomerId, @OrderNumber, @Status, @OrderDate, @TotalAmount, @IsPriority)";

                    await conn.ExecuteAsync(insertSql, new
                    {
                        order.CustomerId,
                        order.OrderNumber,
                        order.Status,
                        order.OrderDate,
                        order.TotalAmount,
                        order.IsPriority
                    });
                });

                ordersToUpdate = await WithConnection(async conn =>
                    await conn.QueryAsync<Order>(
                        $"SELECT * FROM Orders WHERE {spec.GetWhereClause()}",
                        spec.GetParameters()));
            }

            // Act & Assert
            await WithConnection(async conn =>
            {
                using var transaction = conn.BeginTransaction();
                try
                {
                    // Update orders
                    await conn.ExecuteAsync(
                        "UPDATE Orders SET Status = @NewStatus WHERE Id IN @OrderIds",
                        new
                        {
                            NewStatus = OrderStatus.Processing,
                            OrderIds = ordersToUpdate.Select(o => o.Id).ToList()
                        },
                        transaction);

                    // Intentionally cause an error (table does not exist)
                    await conn.ExecuteAsync(
                        "UPDATE NonExistentTable SET Column = @Value",
                        new { Value = 1 },
                        transaction);

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                }
            });

            // Verify that no changes were made
            var unchangedOrders = await WithConnection(async conn =>
                await conn.QueryAsync<Order>(
                    $"SELECT * FROM Orders WHERE {spec.GetWhereClause()}",
                    spec.GetParameters()));

            unchangedOrders.Should().BeEquivalentTo(ordersToUpdate,
                options => options.Including(o => o.Id).Including(o => o.Status));
        }


        private async Task SeedTestDataAsync()
        {
            await WithConnection(async conn =>
            {
                // Begin a transaction for atomicity
                using var transaction = conn.BeginTransaction();

                try
                {
                    // Clear existing data in reverse order of foreign key dependencies
                    var deleteQueries = new[]
                    {
                        "DELETE FROM OrderItems",
                        "DELETE FROM Orders",
                        "DELETE FROM Products",
                        "DELETE FROM Customers",
                        "DELETE FROM Audits"
                    };

                    foreach (var query in deleteQueries)
                    {
                        await conn.ExecuteAsync(query, transaction: transaction);
                    }

                    // Seed Customers
                    var customers = new List<Customer>
                    {
                        new Customer
                        {
                            Name = "Alice",
                            CustomerType = CustomerType.Regular,
                            IsActive = true,
                            CreditLimit = 5000m,
                            CreatedDate = DateTime.UtcNow,
                            PreferredContact = ContactMethod.Email
                        },
                        new Customer
                        {
                            Name = "Bob",
                            CustomerType = CustomerType.Premium,
                            IsActive = true,
                            CreditLimit = 10000m,
                            CreatedDate = DateTime.UtcNow,
                            PreferredContact = ContactMethod.Phone
                        }
                        // Add more customers as needed
                    };

                    var customerParams = customers.Select(c => new
                    {
                        c.Name,
                        c.CustomerType,
                        c.IsActive,
                        c.CreditLimit,
                        c.CreatedDate,
                        c.PreferredContact
                    });

                    await conn.ExecuteAsync(@"
                        INSERT INTO Customers (Name, CustomerType, IsActive, CreditLimit, DateCreated, PreferredContactMethod)
                        VALUES (@Name, @CustomerType, @IsActive, @CreditLimit, @CreatedDate, @PreferredContact)", customerParams, transaction);

                    // Retrieve inserted customer IDs
                    var customerIds = await conn.QueryAsync<int>("SELECT Id FROM Customers", transaction: transaction);

                    // Seed Products
                    var products = new List<Product>
                    {
                        new Product
                        {
                            Id = 1,
                            Name = "Widget01",
                            SKU = "WIDGET-001",
                            Price = 50m,
                            StockLevel = 5,
                            CreatedDate = DateTime.UtcNow,
                            IsAvailable = true,
                            Category = ProductCategory.Electronics
                        },
                        new Product
                        {
                            Id = 2,
                            Name = "Gadget01",
                            SKU = "GADGET-001",
                            Price = 100m,
                            StockLevel = 20,
                            CreatedDate = DateTime.UtcNow,
                            IsAvailable = true,
                            Category = ProductCategory.Electronics
                        },
                        new Product
                        {
                            Id = 3,
                            Name = "Widget02",
                            SKU = "WIDGET-002",
                            Price = 50m,
                            StockLevel = 5,
                            CreatedDate = DateTime.UtcNow,
                            IsAvailable = true,
                            Category = ProductCategory.Electronics
                        },
                        new Product
                        {
                            Id = 4,
                            Name = "Gadget02",
                            SKU = "GADGET-002",
                            Price = 100m,
                            StockLevel = 20,
                            CreatedDate = DateTime.UtcNow,
                            IsAvailable = true,
                            Category = ProductCategory.Electronics
                        }
                        // Add more products as needed
                    };

                    var productParams = products.Select(p => new
                    {
                        p.Name,
                        p.SKU,
                        p.Price,
                        p.StockLevel,
                        p.CreatedDate,
                        p.IsAvailable,
                        p.Category
                    });

                    await conn.ExecuteAsync(@"
                        INSERT INTO Products (Name, SKU, Price, StockLevel, CreatedDate, IsAvailable, Category)
                        VALUES (@Name, @SKU, @Price, @StockLevel, @CreatedDate, @IsAvailable, @Category)", productParams, transaction);

                    // Retrieve inserted product IDs
                    var productIds = await conn.QueryAsync<int>("SELECT Id FROM Products", transaction: transaction);

                    // Seed Orders
                    var orders = new List<Order>
                    {
                        new Order
                        {
                            CustomerId = customerIds.First(), // Adjusted to match the first customer's Id
                            OrderNumber = "ORD-001",
                            Status = OrderStatus.Pending,
                            OrderDate = DateTime.UtcNow.AddMonths(-1),
                            TotalAmount = 500m,
                            IsPriority = false
                        },
                        new Order
                        {
                            CustomerId = customerIds.Skip(1).First(), // Adjusted to match the second customer's Id
                            OrderNumber = "ORD-002",
                            Status = OrderStatus.Processing,
                            OrderDate = DateTime.UtcNow,
                            TotalAmount = 1000m,
                            IsPriority = true
                        },
                                                new Order
                        {
                            CustomerId = customerIds.First(), // Adjusted to match the first customer's Id
                            OrderNumber = "ORD-003",
                            Status = OrderStatus.Pending,
                            OrderDate = DateTime.UtcNow.AddMonths(-1),
                            TotalAmount = 500m,
                            IsPriority = false
                        },
                        new Order
                        {
                            CustomerId = customerIds.Skip(1).First(), // Adjusted to match the second customer's Id
                            OrderNumber = "ORD-004",
                            Status = OrderStatus.Processing,
                            OrderDate = DateTime.UtcNow,
                            TotalAmount = 1000m,
                            IsPriority = true
                        },
                                                new Order
                        {
                            CustomerId = customerIds.First(), // Adjusted to match the first customer's Id
                            OrderNumber = "ORD-005",
                            Status = OrderStatus.Pending,
                            OrderDate = DateTime.UtcNow.AddMonths(-1),
                            TotalAmount = 500m,
                            IsPriority = false
                        },
                        new Order
                        {
                            CustomerId = customerIds.Skip(1).First(), // Adjusted to match the second customer's Id
                            OrderNumber = "ORD-006",
                            Status = OrderStatus.Processing,
                            OrderDate = DateTime.UtcNow,
                            TotalAmount = 1000m,
                            IsPriority = true
                        }
                        // Add more orders as needed
                    };

                    var orderParams = orders.Select(o => new
                    {
                        o.CustomerId,
                        o.OrderNumber,
                        o.Status,
                        o.OrderDate,
                        o.TotalAmount,
                        o.IsPriority
                    });

                    await conn.ExecuteAsync(@"
                        INSERT INTO Orders (CustomerId, OrderNumber, Status, OrderDate, TotalAmount, IsPriority)
                        VALUES (@CustomerId, @OrderNumber, @Status, @OrderDate, @TotalAmount, @IsPriority)", orderParams, transaction);

                    // Retrieve inserted order IDs
                    var orderIds = await conn.QueryAsync<int>("SELECT Id FROM Orders", transaction: transaction);

                    // Seed OrderItems
                    var orderItems = new[]
                    {
                        new OrderItem {
                            OrderId = orderIds.First(), // Adjusted to match the first order's Id
                            ProductId = productIds.First(), // Adjusted to match the first product's Id
                            Quantity = 2,
                            UnitPrice = 50m,
                            Discount = 0m,
                            IsGift = false,
                            Notes = null
                        },
                        new OrderItem {
                            OrderId = orderIds.First(), // Adjusted to match the first order's Id
                            ProductId = productIds.Skip(1).First(), // Adjusted to match the second product's Id
                            Quantity = 1,
                            UnitPrice = 100m,
                            Discount = 0m,
                            IsGift = false,
                            Notes = null
                        },
                        new OrderItem {
                            OrderId = orderIds.Skip(1).First(), // Adjusted to match the second order's Id
                            ProductId = productIds.Skip(2).First(), // Adjusted to match the third product's Id
                            Quantity = 6,
                            UnitPrice = 75m,
                            Discount = 0m,
                            IsGift = false,
                            Notes = null
                        },
                        new OrderItem {
                            OrderId = orderIds.Skip(1).First(), // Adjusted to match the second order's Id
                            ProductId = productIds.Skip(3).First(), // Adjusted to match the fourth product's Id
                            Quantity = 3,
                            UnitPrice = 150m,
                            Discount = 0m,
                            IsGift = false,
                            Notes = null
                        }
                    };

                    await conn.ExecuteAsync(@"
                        INSERT INTO OrderItems (
                            OrderId, 
                            ProductId, 
                            Quantity, 
                            UnitPrice,
                            Discount,
                            IsGift,
                            Notes
                        ) VALUES (
                            @OrderId, 
                            @ProductId, 
                            @Quantity, 
                            @UnitPrice,
                            @Discount,
                            @IsGift,
                            @Notes
                        )", orderItems, transaction);

                    // Commit the transaction
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    // Rollback if any error occurs
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }



    }

    // Additional classes...

    public class TestSpecification<T> : SqlSpecification<T> where T : class
    {
        private readonly Expression<Func<T, bool>> _criteria;

        public TestSpecification(Expression<Func<T, bool>> criteria)
        {
            _criteria = criteria;
            Criteria = criteria;
            BuildWhereClause(); // Ensure BuildWhereClause is called
        }

        protected override void BuildWhereClause()
        {
            var visitor = new SqlExpressionVisitor<T>(this);
            visitor.Visit(_criteria);

            // Get the generated SQL and add it to the WhereClauses
            var whereClause = visitor.GetSql();
            AddWhereClause(whereClause);
        }

        public string GetWhereClause()
        {
            return string.Join(" AND ", WhereClauses);
        }

        protected override string GetTableName()
        {
            // Simple pluralization
            var typeName = typeof(T).Name;
            return typeName.EndsWith("s") ? typeName : typeName + "s";
        }
    }

}
