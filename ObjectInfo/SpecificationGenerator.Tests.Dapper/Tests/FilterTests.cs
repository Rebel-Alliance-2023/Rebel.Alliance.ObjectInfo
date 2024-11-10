using System;
using System.Threading.Tasks;
using System.Linq;
using Dapper;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure.TestFixtures;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Models;
using System.Linq.Expressions;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure;
using System.Data;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Tests
{
    public class FilterTests : IntegrationTestBase, IClassFixture<DatabaseFixture>
    {
        public FilterTests(DatabaseFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact]
        public async Task FiltersByStringContains()
        {
            // Arrange
            await SeedTestDataAsync();
            Expression<Func<Customer, bool>> criteria = c =>
                c.Name.Contains("test", StringComparison.OrdinalIgnoreCase);

            // Debug output for criteria
            Logger.Information("Test criteria: {Criteria}", criteria.ToString());

            // Act
            var spec = new TestSpecification<Customer>(criteria);
            var sql = spec.ToSql();
            var parameters = spec.GetParameters();

            // Debug output for generated SQL
            Logger.Information("Generated SQL: {SQL}", sql);
            Logger.Information("Parameters: {@Parameters}", parameters);

            // Execute query
            var results = await WithConnection(async conn =>
            {
                // First verify the test data
                var allCustomers = await conn.QueryAsync<Customer>("SELECT * FROM Customers");
                Logger.Information("All customers before query: {@Customers}",
                    allCustomers.Select(c => new { c.Id, c.Name }));

                return await conn.QueryAsync<Customer>(sql, parameters);
            });

            // Debug output for results
            Logger.Information("Query returned {Count} results", results.Count());
            if (results.Any())
            {
                Logger.Information("First few results: {@Results}",
                    results.Take(3).Select(c => new { c.Id, c.Name }));
            }

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(c =>
                c.Name.Should().ContainEquivalentOf("test"));
        }

        [Fact]
        public async Task FiltersBySimpleEquality()
        {
            // Arrange
            var spec = new TestSpecification<Customer>(c => c.CustomerType == CustomerType.Premium);

            // Act
            var sql = spec.ToSql();
            var parameters = spec.GetParameters();
            Logger.Information("Generated SQL: {Sql}", sql);
            Logger.Information("Parameters: {@Parameters}", parameters);

            var results = await WithConnection(async conn =>
                await conn.QueryAsync<Customer>(sql, parameters));

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(c => c.CustomerType.Should().Be(CustomerType.Premium));
        }

        [Fact]
        public async Task FiltersByNumericRange()
        {
            await SeedTestDataAsync();
            // Arrange
            var minLimit = 5000m;
            var maxLimit = 10000m;
            var spec = new TestSpecification<Customer>(c =>
                c.CreditLimit >= minLimit && c.CreditLimit <= maxLimit);

            // Act
            var sql = spec.ToSql();
            var parameters = spec.GetParameters();
            Logger.Information("Generated SQL: {Sql}", sql);
            Logger.Information("Parameters: {@Parameters}", parameters);

            var results = await WithConnection(async conn =>
                await conn.QueryAsync<Customer>(sql, parameters));

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(c =>
                c.CreditLimit.Should().BeInRange(minLimit, maxLimit));
        }

        [Fact]
        public async Task FiltersByDateRange()
        {
            await SeedTestDataAsync();
            // Arrange
            var startDate = DateTime.Today.AddMonths(-1);
            var endDate = DateTime.Today;
            var spec = new TestSpecification<Order>(o =>
                o.OrderDate >= startDate && o.OrderDate <= endDate);

            // Act
            var sql = spec.ToSql();
            var parameters = spec.GetParameters();
            Logger.Information("Generated SQL: {Sql}", sql);
            Logger.Information("Parameters: {@Parameters}", parameters);

            var results = await WithConnection(async conn =>
                await conn.QueryAsync<Order>(sql, parameters));

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(o =>
                o.OrderDate.Should().BeOnOrAfter(startDate).And.BeOnOrBefore(endDate));
        }

        [Fact]
        public async Task FiltersByEnumeration()
        {
            await SeedTestDataAsync();
            // Arrange
            var validStatuses = new[] { OrderStatus.Processing, OrderStatus.Shipped };
            var spec = new TestSpecification<Order>(o => validStatuses.Contains(o.Status));

            // Act
            var sql = spec.ToSql();
            var parameters = spec.GetParameters();
            Logger.Information("Generated SQL: {Sql}", sql);
            Logger.Information("Parameters: {@Parameters}", parameters);

            var results = await WithConnection(async conn =>
                await conn.QueryAsync<Order>(sql, parameters));

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(o =>
                o.Status.Should().BeOneOf(validStatuses));
        }

        [Fact]
        public async Task FiltersByMultipleConditions()
        {
            await SeedTestDataAsync();
            // Arrange
            var spec = new TestSpecification<Customer>(c =>
                c.IsActive &&
                c.CustomerType == CustomerType.Premium &&
                c.CreditLimit > 5000m);

            // Act
            var sql = spec.ToSql();
            var parameters = spec.GetParameters();
            Logger.Information("Generated SQL: {Sql}", sql);
            Logger.Information("Parameters: {@Parameters}", parameters);

            var results = await WithConnection(async conn =>
                await conn.QueryAsync<Customer>(sql, parameters));

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(c =>
            {
                c.IsActive.Should().BeTrue();
                c.CustomerType.Should().Be(CustomerType.Premium);
                c.CreditLimit.Should().BeGreaterThan(5000m);
            });
        }

        [Fact]
        public async Task FiltersByNestedProperties()
        {
            // Arrange
            await SeedTestDataAsync(); // Add this line

            var results = await WithConnection(async conn =>
            {
                var query = @"
            SELECT o.*, c.*, oi.*
            FROM Orders o
            JOIN Customers c ON o.CustomerId = c.Id
            JOIN OrderItems oi ON oi.OrderId = o.Id
            WHERE c.CustomerType = @CustomerType
              AND oi.Quantity > @QuantityThreshold";

                var parameters = new { CustomerType = CustomerType.VIP, QuantityThreshold = 5 };

                var orderDictionary = new Dictionary<int, Order>();

                return (await conn.QueryAsync<Order, Customer, OrderItem, Order>(
                    query,
                    (order, customer, orderItem) =>
                    {
                        if (!orderDictionary.TryGetValue(order.Id, out var orderEntry))
                        {
                            orderEntry = order;
                            orderEntry.Customer = customer;
                            orderEntry.Items = new List<OrderItem>();
                            orderDictionary.Add(orderEntry.Id, orderEntry);
                        }
                        orderEntry.Items.Add(orderItem);
                        return orderEntry;
                    },
                    parameters,
                    splitOn: "Id,Id")).Distinct().ToList();
            });

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(o =>
            {
                o.Customer.CustomerType.Should().Be(CustomerType.VIP);
                o.Items.Should().Contain(i => i.Quantity > 5);
            });
        }

        [Fact]
        public async Task FiltersWithComplexLogic()
        {
            await SeedTestDataAsync();

            // Arrange
            var spec = new TestSpecification<Customer>(c =>
                (c.CustomerType == CustomerType.Premium || c.CreditLimit > 10000m) &&
                c.IsActive &&
                (!string.IsNullOrEmpty(c.Email) || c.PreferredContact != ContactMethod.Email));

            // Act
            var sql = spec.ToSql();
            var parameters = spec.GetParameters();
            Logger.Information("Generated SQL: {Sql}", sql);
            Logger.Information("Parameters: {@Parameters}", parameters);

            var results = await WithConnection(async conn =>
                await conn.QueryAsync<Customer>(sql, parameters));

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(c =>
            {
                (c.CustomerType == CustomerType.Premium || c.CreditLimit > 10000m)
                    .Should().BeTrue();
                c.IsActive.Should().BeTrue();
                (!string.IsNullOrEmpty(c.Email) || c.PreferredContact != ContactMethod.Email)
                    .Should().BeTrue();
            });
        }


        //__________________________________

        private class TestSpecification<T> : Infrastructure.SqlSpecification<T> where T : class
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

            protected override string GetTableName() => typeof(T).Name + "s"; // Simple pluralization
        }


        private async Task SeedTestDataAsync()
        {
            await WithConnection(async conn =>
            {
                try
                {
                    using var transaction = conn.BeginTransaction();

                    try
                    {
                        // Enable foreign key constraints
                        await conn.ExecuteAsync("PRAGMA foreign_keys = ON;", transaction);

                        // Clear existing data in correct order
                        await conn.ExecuteAsync("DELETE FROM OrderItems", transaction);
                        await conn.ExecuteAsync("DELETE FROM Orders", transaction);
                        await conn.ExecuteAsync("DELETE FROM Customers", transaction);
                        await conn.ExecuteAsync("DELETE FROM Products", transaction);

                        // Seed Products
                        Product[] products = new[]
                        {
                    new Product
                    {
                        Name = "Product 1",
                        SKU = "SKU001",
                        Price = 50m,
                        IsAvailable = true,
                        Description = "Electronics product",
                        Category = ProductCategory.Electronics,
                        StockLevel = 100,
                        Weight = 1.2m,
                        TagsJson = null,
                        CreatedDate = DateTime.UtcNow,
                        LastRestockDate = DateTime.UtcNow
                    },
                    new Product
                    {
                        Name = "Product 2",
                        SKU = "SKU002",
                        Price = 100m,
                        IsAvailable = true,
                        Description = "Clothing product",
                        Category = ProductCategory.Clothing,
                        StockLevel = 50,
                        Weight = 0.5m,
                        TagsJson = null,
                        CreatedDate = DateTime.UtcNow,
                        LastRestockDate = DateTime.UtcNow
                    },
                    new Product
                    {
                        Name = "Product 3",
                        SKU = "SKU003",
                        Price = 75m,
                        IsAvailable = true,
                        Description = "Book product",
                        Category = ProductCategory.Books,
                        StockLevel = 200,
                        Weight = 0.3m,
                        TagsJson = null,
                        CreatedDate = DateTime.UtcNow,
                        LastRestockDate = DateTime.UtcNow
                    },
                    new Product
                    {
                        Name = "Product 4",
                        SKU = "SKU004",
                        Price = 150m,
                        IsAvailable = true,
                        Description = "Home product",
                        Category = ProductCategory.Home,
                        StockLevel = 30,
                        Weight = 2.0m,
                        TagsJson = null,
                        CreatedDate = DateTime.UtcNow,
                        LastRestockDate = DateTime.UtcNow
                    }
                };

                        await conn.ExecuteAsync(@"
                    INSERT INTO Products (
                        Name, 
                        SKU, 
                        Price, 
                        IsAvailable, 
                        Description,
                        Category, 
                        StockLevel, 
                        Weight,
                        TagsJson,
                        CreatedDate,
                        LastRestockDate
                    ) VALUES (
                        @Name, 
                        @SKU, 
                        @Price, 
                        @IsAvailable, 
                        @Description,
                        @Category, 
                        @StockLevel, 
                        @Weight,
                        @TagsJson,
                        @CreatedDate,
                        @LastRestockDate
                    )", products, transaction);

                        // Get inserted product IDs
                        var productIds = (await conn.QueryAsync<int>(
                            "SELECT Id FROM Products ORDER BY Id", transaction: transaction)).ToList();

                        // Seed Customers
                        Customer[] customers = new[]
                        {
                    new Customer {
                        Name = "Test Company A",
                        Email = "test@example.com",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = null,
                        CustomerType = CustomerType.Regular,
                        CreditLimit = 1000m,
                        PreferredContact = ContactMethod.Email,
                        Notes = null,
                        MetaDataJson = null
                    },
                    new Customer {
                        Name = "Another Test Inc",
                        Email = "test2@example.com",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = null,
                        CustomerType = CustomerType.Premium,
                        CreditLimit = 7500m,
                        PreferredContact = ContactMethod.Phone,
                        Notes = null,
                        MetaDataJson = null
                    },
                    new Customer {
                        Name = "VIP Client",
                        Email = "vip@example.com",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = null,
                        CustomerType = CustomerType.VIP,
                        CreditLimit = 15000m,
                        PreferredContact = ContactMethod.Email,
                        Notes = null,
                        MetaDataJson = null
                    },
                    new Customer {
                        Name = "Regular Company Test",
                        Email = null,
                        IsActive = false,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = null,
                        CustomerType = CustomerType.Regular,
                        CreditLimit = 3000m,
                        PreferredContact = ContactMethod.Mail,
                        Notes = null,
                        MetaDataJson = null
                    },
                    new Customer {
                        Name = "Premium Customer",
                        Email = "premium@test.com",
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = null,
                        CustomerType = CustomerType.Premium,
                        CreditLimit = 12000m,
                        PreferredContact = ContactMethod.Email,
                        Notes = null,
                        MetaDataJson = null
                    }
                };

                        await conn.ExecuteAsync(@"
                    INSERT INTO Customers (
                        Name, 
                        Email, 
                        IsActive, 
                        DateCreated,
                        LastModified,
                        CustomerType, 
                        CreditLimit, 
                        PreferredContactMethod,
                        Notes,
                        MetaData
                    ) VALUES (
                        @Name, 
                        @Email, 
                        @IsActive, 
                        @CreatedDate,
                        @ModifiedDate,
                        @CustomerType, 
                        @CreditLimit, 
                        @PreferredContact,
                        @Notes,
                        @MetaDataJson
                    )", customers, transaction);

                        // Get inserted customer IDs
                        var customerIds = (await conn.QueryAsync<int>(
                            "SELECT Id FROM Customers ORDER BY Id", transaction: transaction)).ToList();

                        // Seed Orders
                        Order[] orders = new[]
                        {
                    new Order {
                        CustomerId = customerIds[0],
                        OrderNumber = "ORD-001",
                        Status = OrderStatus.Processing,
                        OrderDate = DateTime.Today.AddDays(-5),
                        ShippedDate = null,
                        ShippingAddress = "123 Test St",
                        TotalAmount = 200m, // Adjusted to match OrderItems total
                        IsPriority = false
                    },
                    new Order {
                        CustomerId = customerIds[1],
                        OrderNumber = "ORD-002",
                        Status = OrderStatus.Shipped,
                        OrderDate = DateTime.Today.AddDays(-15),
                        ShippedDate = DateTime.Today.AddDays(-13),
                        ShippingAddress = "456 Ship Lane",
                        TotalAmount = 450m, // Adjusted to match OrderItems total
                        IsPriority = true
                    },
                    new Order {
                        CustomerId = customerIds[2],
                        OrderNumber = "ORD-003",
                        Status = OrderStatus.Processing,
                        OrderDate = DateTime.Today.AddDays(-2),
                        ShippedDate = null,
                        ShippingAddress = null,
                        TotalAmount = 1050m, // 7 * 150m
                        IsPriority = false
                    }
                };

                        await conn.ExecuteAsync(@"
                    INSERT INTO Orders (
                        CustomerId, 
                        OrderNumber, 
                        Status, 
                        OrderDate,
                        ShippedDate, 
                        ShippingAddress, 
                        TotalAmount, 
                        IsPriority
                    ) VALUES (
                        @CustomerId, 
                        @OrderNumber, 
                        @Status, 
                        @OrderDate,
                        @ShippedDate, 
                        @ShippingAddress, 
                        @TotalAmount, 
                        @IsPriority
                    )", orders, transaction);

                        // Get inserted order IDs
                        var orderIds = (await conn.QueryAsync<int>(
                            "SELECT Id FROM Orders ORDER BY Id", transaction: transaction)).ToList();

                        // Seed OrderItems
                        OrderItem[] orderItems = new[]
                        {
                    new OrderItem {
                        OrderId = orderIds[0],
                        ProductId = productIds[0],
                        Quantity = 2,
                        UnitPrice = 50m,
                        Discount = 0m,
                        IsGift = false,
                        Notes = null
                    },
                    new OrderItem {
                        OrderId = orderIds[0],
                        ProductId = productIds[1],
                        Quantity = 1,
                        UnitPrice = 100m,
                        Discount = 0m,
                        IsGift = false,
                        Notes = null
                    },
                    new OrderItem {
                        OrderId = orderIds[1],
                        ProductId = productIds[2],
                        Quantity = 6,
                        UnitPrice = 75m,
                        Discount = 0m,
                        IsGift = false,
                        Notes = null
                    },
                    new OrderItem {
                        OrderId = orderIds[2], // Order for VIP customer
                        ProductId = productIds[3],
                        Quantity = 7, // Changed from 3 to 7
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

                        transaction.Commit();

                        // Log seeded data
                        LogSeededData(conn);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error while seeding test data");
                        transaction.Rollback();
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Critical error during test data seeding");
                    throw;
                }
            });
        }

        private void LogSeededData(IDbConnection conn)
        {
            var customers = conn.Query<Customer>("SELECT * FROM Customers");
            var orders = conn.Query<Order>("SELECT * FROM Orders");
            var orderItems = conn.Query<OrderItem>("SELECT * FROM OrderItems");

            Logger.Information("Seeded Customers: {@Customers}",
                customers.Select(c => new { c.Id, c.Name, c.CustomerType, c.CreditLimit }));
            Logger.Information("Seeded Orders: {@Orders}",
                orders.Select(o => new { o.Id, o.OrderNumber, o.Status }));
            Logger.Information("Seeded OrderItems: {@OrderItems}",
                orderItems.Select(i => new { i.OrderId, i.Quantity }));
        }

    }
}