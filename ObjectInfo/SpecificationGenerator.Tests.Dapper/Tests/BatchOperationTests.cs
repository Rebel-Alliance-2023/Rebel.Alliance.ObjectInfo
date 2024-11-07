using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure.TestFixtures;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Models;
using System.Linq.Expressions;
using Dapper;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Tests
{
    public class BatchOperationTests : IntegrationTestBase, IClassFixture<DatabaseFixture>
    {
        public BatchOperationTests(DatabaseFixture fixture, ITestOutputHelper output) 
            : base(fixture, output)
        {
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        public async Task BatchUpdate_ShouldModifyAllMatchingRecords(int batchSize)
        {
            // Arrange
            var creditIncrease = 1000m;
            var spec = new CustomerSpecification(c => 
                c.CustomerType == CustomerType.Regular && 
                c.IsActive);

            // Act
            var updateSql = $@"
                UPDATE Customers 
                SET CreditLimit = CreditLimit + @CreditIncrease
                WHERE EXISTS (
                    SELECT 1 
                    FROM ({spec.ToSql()}) AS Selected
                    WHERE Selected.Id = Customers.Id
                )";

            var parameters = new DynamicParameters(spec.GetParameters());
            parameters.Add("@CreditIncrease", creditIncrease);

            var affectedRows = await WithConnection(async conn =>
                await conn.ExecuteAsync(updateSql, parameters));

            // Assert
            affectedRows.Should().BeGreaterOrEqualTo(batchSize / 2);

            // Verify updates
            var updatedCustomers = await WithConnection(async conn =>
                await conn.QueryAsync<Customer>(spec.ToSql(), spec.GetParameters()));

            updatedCustomers.Should().NotBeEmpty();
            foreach (var customer in updatedCustomers)
            {
                customer.CreditLimit.Should().BeGreaterThan(creditIncrease);
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        public async Task BatchDelete_ShouldRemoveAllMatchingRecords(int batchSize)
        {
            // Arrange
            var spec = new OrderSpecification(o =>
                o.Status == OrderStatus.Cancelled &&
                o.OrderDate < DateTime.Now.AddMonths(-6));

            // First, count matching records
            var initialCount = await WithConnection(async conn =>
                await conn.QuerySingleAsync<int>(
                    $"SELECT COUNT(*) FROM ({spec.ToSql()}) AS Selected",
                    spec.GetParameters()));

            // Act
            var deleteSql = $@"
                DELETE FROM Orders
                WHERE EXISTS (
                    SELECT 1 
                    FROM ({spec.ToSql()}) AS Selected
                    WHERE Selected.Id = Orders.Id
                )";

            var affectedRows = await WithConnection(async conn =>
            {
                using var transaction = conn.BeginTransaction();
                try
                {
                    var result = await conn.ExecuteAsync(
                        deleteSql, 
                        spec.GetParameters(),
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
            affectedRows.Should().Be(initialCount);

            // Verify deletion
            var remainingCount = await WithConnection(async conn =>
                await conn.QuerySingleAsync<int>(
                    $"SELECT COUNT(*) FROM ({spec.ToSql()}) AS Selected",
                    spec.GetParameters()));

            remainingCount.Should().Be(0);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(50)]
        public async Task BatchInsert_ShouldInsertAllRecords(int batchSize)
        {
            // Arrange
            var products = Enumerable.Range(0, batchSize)
                .Select(i => new Product
                {
                    Name = $"Test Product {i}",
                    SKU = $"SKU-{Guid.NewGuid():N}",
                    Price = (decimal)(100 + i * 10),
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
            var spec = new ProductSpecification(p => 
                p.Name.StartsWith("Test Product"));

            var insertedProducts = await WithConnection(async conn =>
                            await conn.QueryAsync<Product>(spec.ToSql(), spec.GetParameters()));

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
            var spec = new ProductSpecification(p => p.StockLevel < 10);
            var existingProducts = await WithConnection(async conn =>
                await conn.QueryAsync<Product>(spec.ToSql(), spec.GetParameters()));

            var updates = existingProducts.Select(p => new
            {
                p.SKU,
                NewStockLevel = 100,
                LastRestockDate = DateTime.UtcNow
            });

            var newProducts = Enumerable.Range(0, 5).Select(i => new
            {
                SKU = $"NEW-SKU-{Guid.NewGuid():N}",
                NewStockLevel = 50,
                LastRestockDate = DateTime.UtcNow
            });

            var allRecords = updates.Concat(newProducts).ToList();

            // Act
            var upsertSql = @"
                INSERT INTO Products (SKU, StockLevel, LastRestockDate)
                VALUES (@SKU, @NewStockLevel, @LastRestockDate)
                ON CONFLICT (SKU) DO UPDATE SET
                    StockLevel = @NewStockLevel,
                    LastRestockDate = @LastRestockDate";

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
            }
        }

        [Fact]
        public async Task BatchOperations_ShouldBeAtomic()
        {
            // Arrange
            var spec = new OrderSpecification(o => o.Status == OrderStatus.Pending);
            var ordersToUpdate = await WithConnection(async conn =>
                await conn.QueryAsync<Order>(spec.ToSql(), spec.GetParameters()));

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

                    // Intentionally cause an error
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
                await conn.QueryAsync<Order>(spec.ToSql(), spec.GetParameters()));

            unchangedOrders.Should().BeEquivalentTo(ordersToUpdate,
                options => options.Including(o => o.Id).Including(o => o.Status));
        }

        private class CustomerSpecification : SqlSpecification<Customer>
        {
            public CustomerSpecification(Expression<Func<Customer, bool>> criteria)
            {
                Criteria = criteria;
            }

            protected override void BuildWhereClause()
            {
                var visitor = new SqlExpressionVisitor<Customer>(this);
                visitor.Visit(Criteria);
            }
        }

        private class OrderSpecification : SqlSpecification<Order>
        {
            public OrderSpecification(Expression<Func<Order, bool>> criteria)
            {
                Criteria = criteria;
            }

            protected override void BuildWhereClause()
            {
                var visitor = new SqlExpressionVisitor<Order>(this);
                visitor.Visit(Criteria);
            }
        }

        private class ProductSpecification : SqlSpecification<Product>
        {
            public ProductSpecification(Expression<Func<Product, bool>> criteria)
            {
                Criteria = criteria;
            }

            protected override void BuildWhereClause()
            {
                var visitor = new SqlExpressionVisitor<Product>(this);
                visitor.Visit(Criteria);
            }
        }

        private class SqlExpressionVisitor<T> : ExpressionVisitor where T : class
        {
            private readonly SqlSpecification<T> _specification;
            private int _parameterIndex;

            public SqlExpressionVisitor(SqlSpecification<T> specification)
            {
                _specification = specification;
            }

            // Implementation details
        }
    }
}
