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
            sql.Should().Contain("CustomerType = @p0");
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
            sql.Should().Contain("CustomerType = @p0");
            sql.Should().Contain("OR");
            sql.Should().Contain("CreditLimit > @p1");
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
                c.Name.StartsWith("Test") && c.Email.Contains("@example.com"), _logger);

            // Act
            var sql = spec.ToSql();
            _logger.Information("Generated SQL: {Sql}", sql);

            //SELECT * FROM Customers WHERE (Name LIKE @p1 + '%' AND Email LIKE '%' + @p2 + '%')
            //AND (Name LIKE @p1 + '%' AND Email LIKE '%' + @p2 + '%')

            // Assert
            sql.Should().Contain("Name LIKE @p1 + '%'");
            sql.Should().Contain("Email LIKE '%' + @p2 + '%'");

            DynamicParameters parameters = spec.GetParameters();
            parameters = spec.GetParameters();
            parameters.Get<string>("@p1").Should().Be("Test");
            parameters.Get<string>("@p2").Should().Be("@example.com");
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

            // Adjusted Assertion
            sql.Should().Contain("DateCreated >= @p0"); // Changed from "CreatedDate" to "DateCreated"

            var parameters = spec.GetParameters();
            parameters.Get<DateTime>("@p0").Should().Be(date);
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
            sql.Should().Contain("CustomerType IN (@p0, @p1)");

            var parameters = spec.GetParameters();
            parameters.Get<CustomerType>("@p0").Should().Be(CustomerType.Premium);
            parameters.Get<CustomerType>("@p1").Should().Be(CustomerType.VIP);
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

            // Adjusted Assertions
            sql.Should().Contain("((CustomerType = @p0 OR CreditLimit > @p1)");
            sql.Should().Contain("AND IsActive = 1");
            sql.Should().Contain("AND (Email IS NOT NULL OR PreferredContactMethod != @p2)"); // Updated here
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
                LocalSqlExpressionVisitor<T> visitor = new LocalSqlExpressionVisitor<T>(_logger, this);
                visitor.Visit(Criteria);

                // Get the generated SQL from the visitor
                var whereClause = visitor.GetSql();
                AddToWhereClause(whereClause);

                // Parameters are already stored in the specification's Parameters dictionary
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

        private class LocalSqlExpressionVisitor<T> : SqlExpressionVisitor<T> where T : class
        {
            private readonly ILogger _logger;
            private readonly SqlSpecificationBase<T> specification;

            public LocalSqlExpressionVisitor(ILogger logger, SqlSpecificationBase<T> specification)
                : base(specification)
            {
                _logger = logger;
                this.specification = specification;
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                _logger.Debug("VisitBinary: Creating {NodeType} expression with Left Operand Type: {LeftType}, Right Operand Type: {RightType}",
                              node.NodeType, node.Left.Type, node.Right.Type);
                return base.VisitBinary(node);
            }

            protected override Expression VisitUnary(UnaryExpression node)
            {
                if (node.NodeType == ExpressionType.Convert)
                {
                    return Visit(node.Operand);
                }
                return base.VisitUnary(node);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                _logger.Debug("VisitMethodCall: Processing method {MethodName}", node.Method.Name);

                if (node.Method.DeclaringType == typeof(string))
                {
                    if (node.Method.Name == "StartsWith")
                    {
                        Visit(node.Object);
                        _sqlBuilder.Append(" LIKE ");
                        Visit(node.Arguments[0]);
                        _sqlBuilder.Append(" + '%'");
                        return node;
                    }
                    else if (node.Method.Name == "Contains")
                    {
                        Visit(node.Object);
                        _sqlBuilder.Append(" LIKE '%' + ");
                        Visit(node.Arguments[0]);
                        _sqlBuilder.Append(" + '%'");
                        return node;
                    }
                }
                else if (node.Method.Name == "Contains")
                {
                    IEnumerable collection = null;
                    Expression itemExpr = null;

                    if (node.Object != null && typeof(IEnumerable).IsAssignableFrom(node.Object.Type))
                    {
                        // Instance method call
                        collection = Expression.Lambda(node.Object).Compile().DynamicInvoke() as IEnumerable;
                        itemExpr = node.Arguments[0];
                    }
                    else if (node.Method.IsStatic && node.Method.DeclaringType == typeof(Enumerable))
                    {
                        // Static method call
                        collection = Expression.Lambda(node.Arguments[0]).Compile().DynamicInvoke() as IEnumerable;
                        itemExpr = node.Arguments[1];
                    }

                    if (collection != null && itemExpr != null)
                    {
                        string columnName = GetMemberName(itemExpr);
                        _sqlBuilder.Append(columnName);
                        _sqlBuilder.Append(" IN (");

                        List<string> parameters = new List<string>();
                        //foreach (object? item in collection)
                        //{
                        //    string paramName = $"@p{_parameterIndex++}";
                        //    specification.Parameters[paramName] = item;
                        //    parameters.Add(paramName);
                        //}
                        _sqlBuilder.Append(string.Join(", ", parameters));
                        _sqlBuilder.Append(")");
                        return node;
                    }
                }

                return base.VisitMethodCall(node);
            }

            private string GetMemberName(Expression expression)
            {
                if (expression is MemberExpression memberExpr)
                {
                    if (memberExpr.Expression is ParameterExpression)
                    {
                        return memberExpr.Member.Name;
                    }
                    else if (memberExpr.Expression is UnaryExpression unaryExpr)
                    {
                        return GetMemberName(unaryExpr);
                    }
                }
                else if (expression is UnaryExpression unaryExpr)
                {
                    return GetMemberName(unaryExpr.Operand);
                }
                throw new NotSupportedException($"Expression type {expression.GetType().Name} is not supported for extracting member name.");
            }
        }

    }
}
