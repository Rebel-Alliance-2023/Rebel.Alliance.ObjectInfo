using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery.Models;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestEntities;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.Helpers;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestFixtures;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery.Base;
using Microsoft.Extensions.Caching.Memory;
using System.Linq.Expressions;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Runtime
{
    public class AdvancedSpecificationTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;
        private readonly ITestOutputHelper _output;

        public AdvancedSpecificationTests(DatabaseFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact]
        public void FilterGroup_WithSingleCondition_GeneratesCorrectExpression()
        {
            // Arrange
            var spec = new TestAdvancedSpecification<TestEntity>();

            // Act
            spec.Where(group =>
            {
                group.AddCondition("Name", FilterOperator.Equals, "Test");
            });

            // Assert
            var query = _fixture.CreateTestData();
            var result = spec.Evaluate(query).ToList();
            result.Should().OnlyContain(e => e.Name == "Test");
        }

        [Fact]
        public void FilterGroup_WithMultipleConditions_GeneratesCorrectExpression()
        {
            // Arrange
            var spec = new TestAdvancedSpecification<TestEntity>();

            // Act
            spec.Where(group =>
            {
                group.AddCondition("IsActive", FilterOperator.Equals, true);
                group.AddCondition("Value", FilterOperator.GreaterThan, 100m);
            });

            // Assert
            var query = _fixture.CreateTestData();
            var result = spec.Evaluate(query).ToList();
            result.Should().OnlyContain(e => e.IsActive && e.Value > 100m);
        }

        [Fact]
        public void FilterGroup_WithNestedGroups_GeneratesCorrectExpression()
        {
            // Arrange
            var spec = new TestAdvancedSpecification<TestEntity>();

            // Act
            spec.Where(group =>
            {
                group.AddGroup(nested =>
                {
                    nested.Operator = LogicalOperator.Or;
                    nested.AddCondition("Status", FilterOperator.Equals, TestEntityStatus.Active);
                    nested.AddCondition("Status", FilterOperator.Equals, TestEntityStatus.Draft);
                });
                group.AddCondition("IsActive", FilterOperator.Equals, true);
            });

            // Assert
            var query = _fixture.CreateTestData();
            var result = spec.Evaluate(query).ToList();
            result.Should().OnlyContain(e => 
                (e.Status == TestEntityStatus.Active || e.Status == TestEntityStatus.Draft) && e.IsActive);
        }

        [Fact]
        public void Sort_WithMultipleFields_AppliesCorrectOrdering()
        {
            // Arrange
            var spec = new TestAdvancedSpecification<TestEntity>();

            // Act
            spec.AddSort("IsActive", SortDirection.Descending, 1)
                .AddSort("Name", SortDirection.Ascending, 2);

            // Assert
            var query = _fixture.CreateTestData();
            var result = spec.Evaluate(query).ToList();
            var expected = query.OrderByDescending(e => e.IsActive)
                              .ThenBy(e => e.Name)
                              .ToList();
            result.Should().Equal(expected);
        }

        [Fact]
        public void Sort_WithCustomExpression_AppliesCorrectOrdering()
        {
            // Arrange
            var spec = new TestAdvancedSpecification<TestEntity>();
            Expression<Func<TestEntity, object>> customSort = e => e.Value ?? 0;

            // Act
            spec.AddSort(customSort, SortDirection.Descending);

            // Assert
            var query = _fixture.CreateTestData();
            var result = spec.Evaluate(query).ToList();
            var expected = query.OrderByDescending(e => e.Value ?? 0).ToList();
            result.Should().Equal(expected);
        }

        [Fact]
        public void ClearFilters_RemovesAllConditions()
        {
            // Arrange
            var spec = new TestAdvancedSpecification<TestEntity>();
            spec.Where(group =>
            {
                group.AddCondition("IsActive", FilterOperator.Equals, true);
            });

            // Act
            spec.ClearFilters();

            // Assert
            var query = _fixture.CreateTestData();
            var result = spec.Evaluate(query).ToList();
            result.Should().HaveCount(query.Count());
        }

        [Fact]
        public void ClearSort_RemovesAllSortFields()
        {
            // Arrange
            var spec = new TestAdvancedSpecification<TestEntity>();
            spec.AddSort("Name", SortDirection.Ascending);

            // Act
            spec.ClearSort();

            // Assert
            spec.SortFields.Should().BeEmpty();
        }

        [Fact]
        public async Task Cache_StoreAndRetrieveResults()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMemoryCache();
            var provider = services.BuildServiceProvider();
            
            var spec = new TestAdvancedSpecification<TestEntity>(provider.GetRequiredService<IMemoryCache>());
            spec.Where(group =>
            {
                group.AddCondition("IsActive", FilterOperator.Equals, true);
            });

            // Act
            var query = _fixture.CreateTestData();
            var firstResult = await spec.ToCachedListAsync(TimeSpan.FromMinutes(5));
            var secondResult = await spec.ToCachedListAsync(TimeSpan.FromMinutes(5));

            // Assert
            firstResult.Should().Equal(secondResult);
        }

        [Fact]
        public void StringOperations_GenerateCorrectFilters()
        {
            // Arrange
            var spec = new TestAdvancedSpecification<TestEntity>();

            // Act
            spec.Where(group =>
            {
                group.AddCondition("Name", FilterOperator.Contains, "test");
                group.AddCondition("Name", FilterOperator.StartsWith, "A");
                group.AddCondition("Name", FilterOperator.EndsWith, "Z");
            });

            // Assert
            var query = _fixture.CreateTestData();
            var result = spec.Evaluate(query).ToList();
            result.Should().OnlyContain(e => 
                e.Name.Contains("test") && 
                e.Name.StartsWith("A") && 
                e.Name.EndsWith("Z"));
        }

        private class TestAdvancedSpecification<T> : AdvancedSpecification<T> where T : class
        {
            public TestAdvancedSpecification(IMemoryCache? cache = null) : base((SpecificationGenerator.Runtime.Caching.ISpecificationCache?)cache)
            {
            }

            public IQueryable<T> Evaluate(IQueryable<T> query)
            {
                return ApplySpecification(query);
            }

            protected override Task<IEnumerable<T>> QueryAsync(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}
