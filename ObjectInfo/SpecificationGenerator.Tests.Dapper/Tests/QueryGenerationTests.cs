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

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Tests
{
    public class QueryGenerationTests : IntegrationTestBase, IClassFixture<DatabaseFixture>
    {
        public QueryGenerationTests(DatabaseFixture fixture, ITestOutputHelper output) 
            : base(fixture, output)
        {
        }

        [Fact]
        public void GeneratesBasicSelectQuery()
        {
            // Arrange
            var spec = new TestSqlSpecification<Customer>(c => c.IsActive);

            // Act
            var sql = spec.ToSql();

            // Assert
            sql.Should().Contain("SELECT * FROM Customers");
            sql.Should().Contain("WHERE IsActive = @p0");
        }

        [Fact]
        public void GeneratesQueryWithMultipleConditions()
        {
            // Arrange
            var spec = new TestSqlSpecification<Customer>(c => 
                c.IsActive && c.CustomerType == CustomerType.Premium);

            // Act
            var sql = spec.ToSql();

            // Assert
            sql.Should().Contain("IsActive = @p0");
            sql.Should().Contain("CustomerType = @p1");
            sql.Should().Contain("AND");
        }

        [Fact]
        public void GeneratesQueryWithOrConditions()
        {
            // Arrange
            var spec = new TestSqlSpecification<Customer>(c => 
                c.CustomerType == CustomerType.VIP || c.CreditLimit > 10000);

            // Act
            var sql = spec.ToSql();

            // Assert
            sql.Should().Contain("CustomerType = @p0");
            sql.Should().Contain("OR");
            sql.Should().Contain("CreditLimit > @p1");
        }

        [Fact]
        public void GeneratesQueryWithNullChecks()
        {
            // Arrange
            var spec = new TestSqlSpecification<Customer>(c => c.Email != null);

            // Act
            var sql = spec.ToSql();

            // Assert
            sql.Should().Contain("Email IS NOT NULL");
        }

        [Fact]
        public void GeneratesQueryWithStringOperations()
        {
            // Arrange
            var spec = new TestSqlSpecification<Customer>(c => 
                c.Name.StartsWith("Test") && c.Email.Contains("@example.com"));

            // Act
            var sql = spec.ToSql();

            // Assert
            sql.Should().Contain("Name LIKE @p0");
            sql.Should().Contain("Email LIKE @p1");
            
            var parameters = spec.GetParameters();
            parameters["@p0"].Should().Be("Test%");
            parameters["@p1"].Should().Be("%@example.com%");
        }

        [Fact]
        public void GeneratesQueryWithDateComparisons()
        {
            // Arrange
            var date = DateTime.Today;
            var spec = new TestSqlSpecification<Customer>(c => c.CreatedDate >= date);

            // Act
            var sql = spec.ToSql();

            // Assert
            sql.Should().Contain("DateCreated >= @p0");
            
            var parameters = spec.GetParameters();
            parameters["@p0"].Should().Be(date);
        }

        [Fact]
        public void GeneratesQueryWithCollectionOperations()
        {
            // Arrange
            var types = new[] { CustomerType.Premium, CustomerType.VIP };
            var spec = new TestSqlSpecification<Customer>(c => types.Contains(c.CustomerType));

            // Act
            var sql = spec.ToSql();

            // Assert
            sql.Should().Contain("CustomerType IN @p0");
            
            var parameters = spec.GetParameters();
            parameters["@p0"].Should().BeEquivalentTo(types);
        }

        [Fact]
        public void GeneratesQueryWithPaging()
        {
            // Arrange
            var spec = new TestSqlSpecification<Customer>(c => c.IsActive)
                .AddPaging(10, 20);

            // Act
            var sql = spec.ToSql();

            // Assert
            sql.Should().Contain("OFFSET 10 ROWS");
            sql.Should().Contain("FETCH NEXT 20 ROWS ONLY");
        }

        [Fact]
        public void GeneratesQueryWithOrdering()
        {
            // Arrange
            var spec = new TestSqlSpecification<Customer>(c => true)
                .AddOrderBy(c => c.Name)
                .AddThenByDescending(c => c.CreditLimit);

            // Act
            var sql = spec.ToSql();

            // Assert
            sql.Should().Contain("ORDER BY [Name] ASC");
            sql.Should().Contain("CreditLimit DESC");
        }

        [Fact]
        public void GeneratesQueryWithComplexConditions()
        {
            // Arrange
            var spec = new TestSqlSpecification<Customer>(c =>
                (c.CustomerType == CustomerType.Premium || c.CreditLimit > 10000) &&
                c.IsActive &&
                (c.Email != null || c.PreferredContact != ContactMethod.Email));

            // Act
            var sql = spec.ToSql();

            // Assert
            sql.Should().Contain("(CustomerType = @p0 OR CreditLimit > @p1)");
            sql.Should().Contain("AND IsActive = @p2");
            sql.Should().Contain("AND (Email IS NOT NULL OR PreferredContactMethod != @p3)");
        }

        [Fact]
        public void GeneratesQueryForNestedProperties()
        {
            // Arrange
            var spec = new TestSqlSpecification<Order>(o =>
                o.Customer.CustomerType == CustomerType.VIP &&
                o.Items.Any(i => i.Quantity > 10));

            // Act
            var sql = spec.ToSql();

            // Assert
            sql.Should().Contain("JOIN Customers");
            sql.Should().Contain("JOIN OrderItems");
            sql.Should().Contain("CustomerType = @p0");
            sql.Should().Contain("Quantity > @p1");
        }

        private class TestSqlSpecification<T> : SqlSpecification<T> where T : class
        {
            public TestSqlSpecification(Expression<Func<T, bool>> criteria)
            {
                Criteria = criteria;
            }

            public TestSqlSpecification<T> AddPaging(int skip, int take)
            {
                Skip = skip;
                Take = take;
                return this;
            }

            public TestSqlSpecification<T> AddOrderBy(Expression<Func<T, object>> orderBy)
            {
                OrderBy = orderBy;
                return this;
            }

            public TestSqlSpecification<T> AddThenByDescending(Expression<Func<T, object>> thenBy)
            {
                ((List<Expression<Func<T, object>>>)ThenByDescendingExpressions).Add(thenBy);
                return this;
            }

            protected override void BuildWhereClause()
            {
                AddWhereClause("1=1"); // Default true condition
            }
        }
    }
}
