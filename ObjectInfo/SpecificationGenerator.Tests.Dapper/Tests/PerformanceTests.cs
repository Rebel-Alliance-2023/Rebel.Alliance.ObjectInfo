using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Dapper;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Models;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching;
using System.Reflection;
using System.ComponentModel.DataAnnotations.Schema;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure.TestFixtures;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Tests
{
    public class PerformanceTests : IntegrationTestBase, IClassFixture<DatabaseFixture>
    {
        private readonly ICompiledQueryCache _queryCache;

        public PerformanceTests(DatabaseFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            _queryCache = ServiceProvider.GetRequiredService<ICompiledQueryCache>();
        }

        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        public async Task MeasureQueryGenerationTime(int iterations)
        {
            // Arrange
            var specs = Enumerable.Range(0, iterations)
                .Select(i => new TestSpecification<Customer>(c =>
                    c.CustomerType == (CustomerType)(i % 4) &&
                    c.CreditLimit > i * 1000))
                .ToList();

            var stopwatch = new Stopwatch();
            var generationTimes = new List<long>();

            // Act
            foreach (var spec in specs)
            {
                stopwatch.Restart();
                var sql = spec.ToSql();
                stopwatch.Stop();
                generationTimes.Add(stopwatch.ElapsedTicks);
            }

            // Assert & Log
            var avgTicks = generationTimes.Average();
            var maxTicks = generationTimes.Max();
            var p95Ticks = generationTimes.OrderByDescending(t => t).Skip((int)(iterations * 0.05)).First();

            Logger.Information(
                "Query Generation Performance (iterations: {Iterations}):\n" +
                "  Average: {AverageMs:F3}ms\n" +
                "  Maximum: {MaxMs:F3}ms\n" +
                "  95th percentile: {P95Ms:F3}ms",
                iterations,
                TimeSpan.FromTicks((long)avgTicks).TotalMilliseconds,
                TimeSpan.FromTicks(maxTicks).TotalMilliseconds,
                TimeSpan.FromTicks(p95Ticks).TotalMilliseconds);

            // Performance assertions
            TimeSpan.FromTicks((long)avgTicks).TotalMilliseconds.Should().BeLessThan(1.0,
                "Query generation should take less than 1ms on average");
        }

        [Fact]
        public async Task MeasureQueryExecutionTime()
        {
            // Arrange
            var complexSpec = new TestSpecification<Order>(o =>
                o.Status == OrderStatus.Processing &&
                o.Customer.CustomerType == CustomerType.Premium &&
                o.OrderDate >= DateTime.Now.AddMonths(-1));

            var stopwatch = new Stopwatch();
            var executionTimes = new List<long>();
            const int iterations = 50;

            // Act
            await WithConnection(async conn =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    stopwatch.Restart();
                    var results = await conn.QueryAsync<Order>(
                        complexSpec.ToSql(),
                        complexSpec.GetParameters());
                    stopwatch.Stop();
                    executionTimes.Add(stopwatch.ElapsedTicks);
                }
            });

            // Assert & Log
            var avgMs = TimeSpan.FromTicks((long)executionTimes.Average()).TotalMilliseconds;
            var maxMs = TimeSpan.FromTicks(executionTimes.Max()).TotalMilliseconds;
            var p95Ms = TimeSpan.FromTicks(executionTimes.OrderByDescending(t => t)
                .Skip((int)(iterations * 0.05)).First()).TotalMilliseconds;

            Logger.Information(
                "Query Execution Performance (iterations: {Iterations}):\n" +
                "  Average: {AverageMs:F3}ms\n" +
                "  Maximum: {MaxMs:F3}ms\n" +
                "  95th percentile: {P95Ms:F3}ms",
                iterations, avgMs, maxMs, p95Ms);

            avgMs.Should().BeLessThan(50,
                "Complex query execution should take less than 50ms on average");
        }

        [Fact]
        public async Task MeasureQueryCachePerformance()
        {
            // Arrange
            var spec = new TestSpecification<Customer>(c =>
                c.CustomerType == CustomerType.Premium &&
                c.CreditLimit > 5000);

            var stopwatch = new Stopwatch();
            var uncachedTimes = new List<long>();
            var cachedTimes = new List<long>();
            const int iterations = 100;

            // Act
            // First run - uncached
            for (int i = 0; i < iterations; i++)
            {
                stopwatch.Restart();
                var transformer = _queryCache.GetOrAddQueryTransformer<Customer>((ISpecification<Customer>)spec);
                stopwatch.Stop();
                uncachedTimes.Add(stopwatch.ElapsedTicks);
            }

            // Second run - cached
            for (int i = 0; i < iterations; i++)
            {
                stopwatch.Restart();
                var transformer = _queryCache.GetOrAddQueryTransformer<Customer>((ISpecification<Customer>)spec);
                stopwatch.Stop();
                cachedTimes.Add(stopwatch.ElapsedTicks);
            }

            // Assert & Log
            var avgUncachedMs = TimeSpan.FromTicks((long)uncachedTimes.Average()).TotalMilliseconds;
            var avgCachedMs = TimeSpan.FromTicks((long)cachedTimes.Average()).TotalMilliseconds;

            Logger.Information(
                "Query Cache Performance (iterations: {Iterations}):\n" +
                "  Average uncached: {UncachedMs:F3}ms\n" +
                "  Average cached: {CachedMs:F3}ms\n" +
                "  Cache speedup factor: {SpeedupFactor:F2}x",
                iterations, avgUncachedMs, avgCachedMs, avgUncachedMs / avgCachedMs);

            avgCachedMs.Should().BeLessThan(avgUncachedMs / 2,
                "Cached queries should be at least twice as fast as uncached queries");
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public async Task MeasureComplexQueryPerformance(int resultSize)
        {
            // Arrange
            var spec = new TestSpecification<OrderSummaryDto>(o =>
                o.TotalAmount > 1000 &&
                o.Status != OrderStatus.Cancelled &&
                o.ItemCount > 2);

            var complexQuery = @"
                WITH OrderSummaries AS (
                    SELECT 
                        o.OrderNumber,
                        c.Name AS CustomerName,
                        o.TotalAmount,
                        COUNT(oi.Id) AS ItemCount,
                        o.OrderDate,
                        o.Status
                    FROM Orders o
                    JOIN Customers c ON o.CustomerId = c.Id
                    JOIN OrderItems oi ON o.Id = oi.OrderId
                    GROUP BY 
                        o.OrderNumber,
                        c.Name,
                        o.TotalAmount,
                        o.OrderDate,
                        o.Status
                )
                SELECT *
                FROM OrderSummaries
                WHERE " + spec.ToSql();

            // Act
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var results = await WithConnection(async conn =>
                await conn.QueryAsync<OrderSummaryDto>(
                    complexQuery,
                    spec.GetParameters()));

            stopwatch.Stop();

            // Assert & Log
            var executionMs = stopwatch.ElapsedMilliseconds;
            var resultCount = results.Count();

            Logger.Information(
                "Complex Query Performance:\n" +
                "  Execution time: {ExecutionMs}ms\n" +
                "  Results: {ResultCount}\n" +
                "  Time per result: {TimePerResult:F3}ms",
                executionMs, resultCount, (double)executionMs / resultCount);

            executionMs.Should().BeLessThan(1000,
                "Complex queries should complete within 1 second");
            resultCount.Should().BeGreaterOrEqualTo(resultSize / 2,
                "Query should return approximately the expected number of results");
        }

        private class TestSpecification<T> : SqlSpecificationBase<T> where T : class
        {
            public TestSpecification(Expression<Func<T, bool>> criteria)
            {
                Criteria = criteria;
                BuildWhereClause();
            }

            protected override void BuildWhereClause()
            {
                var visitor = new SqlExpressionVisitor<T>(this);
                visitor.Visit(Criteria);

                var whereClause = visitor.GetSql();
                AddToWhereClause(whereClause);

                // Parameters are already stored in the specification's Parameters dictionary
            }

            protected override string GetTableName()
            {
                var tableAttr = typeof(T).GetCustomAttribute<TableAttribute>();
                if (tableAttr != null)
                {
                    return tableAttr.Name;
                }

                // Simple pluralization
                var typeName = typeof(T).Name;
                return typeName.EndsWith("s") ? typeName : typeName + "s";
            }
        }

        // Definition for OrderSummaryDto
        private class OrderSummaryDto
        {
            public string OrderNumber { get; set; }
            public string CustomerName { get; set; }
            public decimal TotalAmount { get; set; }
            public int ItemCount { get; set; }
            public DateTime OrderDate { get; set; }
            public OrderStatus Status { get; set; }
        }
    }
}
