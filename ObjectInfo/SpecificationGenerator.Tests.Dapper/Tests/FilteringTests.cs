using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
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
    public class FilteringTests : IntegrationTestBase, IClassFixture<DatabaseFixture>
    {
        public FilteringTests(DatabaseFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact]
        public async Task FiltersBySimpleEquality()
        {
            // Arrange
            var spec = new CustomerSpecification(c => c.CustomerType == CustomerType.Premium);

            // Act
            var results = await WithConnection(async conn =>
                await conn.QueryAsync<Customer>(spec.ToSql(), spec.GetParameters()));

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(c => c.CustomerType.Should().Be(CustomerType.Premium));
        }

        [Fact]
        public async Task FiltersByNumericRange()
        {
            // Arrange
            var minLimit = 5000m;
            var maxLimit = 10000m;
            var spec = new CustomerSpecification(c =>
                c.CreditLimit >= minLimit && c.CreditLimit <= maxLimit);

            // Act
            var results = await WithConnection(async conn =>
                await conn.QueryAsync<Customer>(spec.ToSql(), spec.GetParameters()));

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(c =>
                c.CreditLimit.Should().BeInRange(minLimit, maxLimit));
        }

        [Fact]
        public async Task FiltersByDateRange()
        {
            // Arrange
            var startDate = DateTime.Today.AddMonths(-1);
            var endDate = DateTime.Today;
            var spec = new OrderSpecification(o =>
                o.OrderDate >= startDate && o.OrderDate <= endDate);

            // Act
            var results = await WithConnection(async conn =>
                await conn.QueryAsync<Order>(spec.ToSql(), spec.GetParameters()));

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(o =>
                o.OrderDate.Should().BeOnOrAfter(startDate).And.BeOnOrBefore(endDate));
        }

        [Fact]
        public async Task FiltersByEnumeration()
        {
            // Arrange
            var validStatuses = new[] { OrderStatus.Processing, OrderStatus.Shipped };
            var spec = new OrderSpecification(o => validStatuses.Contains(o.Status));

            // Act
            var results = await WithConnection(async conn =>
                await conn.QueryAsync<Order>(spec.ToSql(), spec.GetParameters()));

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(o =>
                o.Status.Should().BeOneOf(validStatuses));
        }

        [Fact]
        public async Task FiltersByStringContains()
        {
            // Arrange
            var searchTerm = "test";
            var spec = new CustomerSpecification(c =>
                c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

            // Act
            var results = await WithConnection(async conn =>
                await conn.QueryAsync<Customer>(spec.ToSql(), spec.GetParameters()));

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(c =>
                c.Name.Should().ContainEquivalentOf(searchTerm));
        }

        [Fact]
        public async Task FiltersByMultipleConditions()
        {
            // Arrange
            var spec = new CustomerSpecification(c =>
                c.IsActive &&
                c.CustomerType == CustomerType.Premium &&
                c.CreditLimit > 5000m);

            // Act
            var results = await WithConnection(async conn =>
                await conn.QueryAsync<Customer>(spec.ToSql(), spec.GetParameters()));

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
            var spec = new OrderSpecification(o =>
                o.Customer.CustomerType == CustomerType.VIP &&
                o.Items.Any(i => i.Quantity > 5));

            // Act
            // Replace the current implementation with:
            var results = await WithConnection(async conn =>
            {
                var query = @"
        SELECT o.*, 
               c.Id as Customer_Id, 
               c.Name as Customer_Name,
               c.CustomerType as Customer_Type
        FROM Orders o
        JOIN Customers c ON o.CustomerId = c.Id
        WHERE " + spec.ToSql();

                var parameters = new DynamicParameters(spec.GetParameters());

                return await conn.QueryAsync<Order, Customer, Order>(
                    query,
                    (order, customer) =>
                    {
                        order.Customer = customer;
                        return order;
                    },
                    parameters,
                    splitOn: "Customer_Id");
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
            // Arrange
            var spec = new CustomerSpecification(c =>
                (c.CustomerType == CustomerType.Premium || c.CreditLimit > 10000m) &&
                c.IsActive &&
                (!string.IsNullOrEmpty(c.Email) || c.PreferredContact != ContactMethod.Email));

            // Act
            var results = await WithConnection(async conn =>
                await conn.QueryAsync<Customer>(spec.ToSql(), spec.GetParameters()));

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

        [Fact]
        public async Task FiltersByNullableProperties()
        {
            // Arrange
            var spec = new OrderSpecification(o =>
                o.ShippedDate.HasValue &&
                o.ShippingAddress != null);

            // Act
            var results = await WithConnection(async conn =>
                await conn.QueryAsync<Order>(spec.ToSql(), spec.GetParameters()));

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(o =>
            {
                o.ShippedDate.Should().NotBeNull();
                o.ShippingAddress.Should().NotBeNullOrEmpty();
            });
        }

        private class CustomerSpecification : SqlSpecificationBase<Customer>
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

        private class OrderSpecification : SqlSpecificationBase<Order>
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

    }
}
