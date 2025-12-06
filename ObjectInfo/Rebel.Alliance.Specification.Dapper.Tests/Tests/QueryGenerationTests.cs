using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Generic;
using Xunit;
using Dapper;
using Xunit.Abstractions;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.XUnit;
using FluentAssertions;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Models;
using System.ComponentModel.DataAnnotations.Schema;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure.TestFixtures;
using System.Collections;
using Rebel.Alliance.Specification.Dapper.Core;


namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Tests
{
    public class QueryGenerationTests : IntegrationTestBase, IClassFixture<DatabaseFixture>
    {
        private readonly ILogger _logger;

        public QueryGenerationTests(DatabaseFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            // Initialize Serilog logger
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(output)
                .CreateLogger();
        }

        [Fact]
        public void GeneratesSimplifiedJoinQuery()
        {
            try
            {
                // Arrange
                TestSqlSpecification<Order> spec = new TestSqlSpecification<Order>(o => o.TotalAmount > 1000 && o.Status == OrderStatus.Delivered, _logger);

                // Act
                var sql = spec.ToSql();
                _logger.Information("Generated SQL: {Sql}", sql);

                //SELECT * FROM Orders WHERE (TotalAmount > @p1 AND Status = @p2) AND (TotalAmount > @p1 AND Status = @p2)

                // Assert
                sql.Should().Contain("SELECT * FROM Orders");
                sql.Should().Contain("TotalAmount > @p1");
                sql.Should().Contain("Status = @p2");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred during test execution.");
                throw;
            }
        }

        [Fact]
        public void GeneratesBasicSelectQuery()
        {
            // Arrange
            TestSqlSpecification<Customer> spec = new TestSqlSpecification<Customer>(c => c.IsActive, _logger);

            // Act
            var sql = spec.ToSql();
            _logger.Information("Generated SQL: {Sql}", sql);

            // Assert
            sql.Should().Contain("SELECT * FROM Customers");
            sql.Should().Contain("WHERE IsActive = 1");
        }

        [Fact]
        public void GeneratesQueryWithMultipleConditions()
        {
            // Arrange
            TestSqlSpecification<Customer> spec = new TestSqlSpecification<Customer>(c =>
                c.IsActive && c.CustomerType == CustomerType.Premium, _logger);

            // Act
            var sql = spec.ToSql();
            _logger.Information("Generated SQL: {Sql}", sql);

            // Assert
            sql.Should().Contain("IsActive = 1");
            sql.Should().Contain("AND");
            sql.Should().MatchRegex(@"CustomerType = @p\d+");
        }


        [Fact]
        public void GeneratesQueryWithOrConditions()
        {
            // Arrange
            TestSqlSpecification<Customer> spec = new TestSqlSpecification<Customer>(c =>
                c.CustomerType == CustomerType.VIP || c.CreditLimit > 10000, _logger);

            // Act
            var sql = spec.ToSql();
            _logger.Information("Generated SQL: {Sql}", sql);

            // Assert
            sql.Should().MatchRegex(@"CustomerType = @p\d+");
            sql.Should().Contain("OR");
            sql.Should().MatchRegex(@"CreditLimit > @p\d+");
        }

        [Fact]
        public void GeneratesQueryWithNullEqualityChecks()
        {
            // Arrange
            TestSqlSpecification<Customer> spec = new TestSqlSpecification<Customer>(c => c.Email == null, _logger);

            // Act
            var sql = spec.ToSql();
            _logger.Information("Generated SQL: {Sql}", sql);

            // Assert
            sql.Should().Contain("Email IS NULL");
        }


        [Fact]
        public void GeneratesQueryWithStringOperations()
        {
            // Arrange
            TestSqlSpecification<Customer> spec = new TestSqlSpecification<Customer>(c =>
                c.Name!.StartsWith("Test") && c.Email!.Contains("@example.com"), _logger);

            // Act
            var sql = spec.ToSql();
            _logger.Information("Generated SQL: {Sql}", sql);

            //SELECT * FROM Customers WHERE (Name LIKE @p1 + '%' AND Email LIKE '%' + @p2 + '%')
            //AND (Name LIKE @p1 + '%' AND Email LIKE '%' + @p2 + '%')

            // Assert (SQLite uses || for concatenation)
            sql.Should().Contain("Name LIKE ");
            sql.Should().Contain("|| '%'");
            sql.Should().Contain("Email LIKE '%' || ");
            sql.Should().Contain(" || '%'");

            DynamicParameters parameters = spec.GetParameters();
            parameters = spec.GetParameters();
            parameters.Get<string>("@p1")!.Should().Be("Test");
            parameters.Get<string>("@p2")!.Should().Be("@example.com");
        }

        [Fact]
        public void GeneratesQueryWithDateComparisons()
        {
            // Arrange
            DateTime date = DateTime.Today;
            TestSqlSpecification<Customer> spec = new TestSqlSpecification<Customer>(c => c.CreatedDate >= date, _logger);

            // Act
            var sql = spec.ToSql();
            _logger.Information("Generated SQL: {Sql}", sql);

            // Adjusted Assertion: match parameter indices dynamically
            sql.Should().MatchRegex(@"DateCreated >= @p\d+");

            var parameters = spec.GetParameters();
            var paramNames = parameters.ParameterNames.ToList();
            paramNames.Should().NotBeEmpty();
            parameters.Get<DateTime>(paramNames.First()).Should().Be(date);
        }


        [Fact]
        public void GeneratesQueryWithCollectionOperations()
        {
            // Arrange
            CustomerType[] types = new[] { CustomerType.Premium, CustomerType.VIP };
            TestSqlSpecification<Customer> spec = new TestSqlSpecification<Customer>(c => types.Contains(c.CustomerType), _logger);

            // Act
            var sql = spec.ToSql();
            _logger.Information("Generated SQL: {Sql}", sql);

            // Assert
            sql.Should().MatchRegex(@"CustomerType IN \(@p\d+, @p\d+\)");

            var parameters = spec.GetParameters();
            var names = parameters.ParameterNames.ToList();
            parameters.Get<CustomerType>(names[0]).Should().Be(CustomerType.Premium);
            parameters.Get<CustomerType>(names[1]).Should().Be(CustomerType.VIP);
        }

        // Note: Paging and ordering functionalities are not implemented in the current code.
        // If needed, they would require additional methods and properties in the base classes.

        [Fact]
        public void GeneratesQueryWithComplexConditions()
        {
            // Arrange
            TestSqlSpecification<Customer> spec = new TestSqlSpecification<Customer>(c =>
                (c.CustomerType == CustomerType.Premium || c.CreditLimit > 10000) &&
                c.IsActive &&
                (c.Email != null || c.PreferredContact != ContactMethod.Email), _logger);

            // Act
            var sql = spec.ToSql();
            _logger.Information("Generated SQL: {Sql}", sql);

            // Adjusted Assertions (indices may vary)
            sql.Should().MatchRegex(@"\(\(CustomerType = @p\d+ OR CreditLimit > @p\d+\)");
            sql.Should().Contain("AND IsActive = 1");
            sql.Should().MatchRegex(@"AND \(Email IS NOT NULL OR PreferredContactMethod != @p\d+\)");
        }





        private class TestSqlSpecification<T> : SqlSpecificationBase<T> where T : class
        {
            private readonly ILogger _logger;

            public TestSqlSpecification(Expression<Func<T, bool>> criteria, ILogger logger)
            {
                Criteria = criteria;
                _logger = logger;

                BuildWhereClause();
            }

            protected override void BuildWhereClause()
            {
                var visitor = new Rebel.Alliance.Specification.Dapper.Core.SqlExpressionVisitor<T>(this);
                visitor.Visit(Criteria);

                var whereClause = visitor.GetSql();
                AddToWhereClause(whereClause);
            }

            // Override to get the correct table name
            protected override string GetTableName()
            {
                TableAttribute? tableAttr = typeof(T).GetCustomAttribute<TableAttribute>();
                if (tableAttr != null)
                {
                    return tableAttr.Name;
                }

                // If no attribute, use pluralized form (simple pluralization)
                string typeName = typeof(T).Name;
                return typeName.EndsWith("s") ? typeName : typeName + "s";
            }
        }

        

    }
}
