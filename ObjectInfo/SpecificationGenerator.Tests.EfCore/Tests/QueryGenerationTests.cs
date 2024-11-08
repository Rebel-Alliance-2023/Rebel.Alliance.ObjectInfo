using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Infrastructure.TestFixtures;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Models;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery.Base;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Tests
{
    public class QueryGenerationTests : TestBase
    {
        public QueryGenerationTests(DatabaseFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact]
        public async Task GeneratesBasicQuery()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => e.IsActive);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("\"IsActive\" = 1");
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e => e.IsActive.Should().BeTrue());
        }

        [Fact]
        public async Task GeneratesQueryWithMultipleConditions()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e =>
                e.IsActive && e.Value > 500m);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("\"IsActive\" = 1");
            sql.Should().Contain("\"Value\" > 500");
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
            {
                e.IsActive.Should().BeTrue();
                e.Value.Should().BeGreaterThan(500m);
            });
        }

        [Fact]
        public async Task GeneratesQueryWithOrConditions()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e =>
                e.Status == TestEntityStatus.Active || e.Value > 1000m);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("OR");
            sql.Should().Contain("\"Status\" = 1");
            sql.Should().Contain("\"Value\" > 1000");
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
                (e.Status == TestEntityStatus.Active || e.Value > 1000m).Should().BeTrue());
        }

        [Fact]
        public async Task GeneratesQueryWithNavigationProperties()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e =>
                e.RelatedEntity != null && e.RelatedEntity.Type == TestEntityType.Premium);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("\"RelatedEntity\".\"Type\"");
            sql.Should().Contain("INNER JOIN");
            results.Should().AllSatisfy(e =>
            {
                e.RelatedEntity.Should().NotBeNull();
                e.RelatedEntity!.Type.Should().Be(TestEntityType.Premium);
            });
        }

        [Fact]
        public async Task GeneratesQueryWithCollectionNavigation()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e =>
                e.Children.Any(c => c.Scope == ChildEntityScope.Public));

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("EXISTS");
            sql.Should().Contain("\"Children\".\"Scope\"");
            results.Should().AllSatisfy(e =>
                e.Children.Any(c => c.Scope == ChildEntityScope.Public).Should().BeTrue());
        }

        [Fact]
        public async Task GeneratesQueryWithDateComparisons()
        {
            // Arrange
            var cutoffDate = DateTime.Today.AddDays(-5);
            var spec = new TestSpecification<TestEntity>(e => e.CreatedDate >= cutoffDate);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("\"CreatedDate\" >=");
            results.Should().AllSatisfy(e =>
                e.CreatedDate.Should().BeOnOrAfter(cutoffDate));
        }

        [Fact]
        public async Task GeneratesQueryWithNullChecks()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => e.Value == null);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("\"Value\" IS NULL");
            results.Should().AllSatisfy(e => e.Value.Should().BeNull());
        }

        private class TestSpecification<T> : AdvancedSpecification<T> where T : class
        {
            public TestSpecification(Expression<Func<T, bool>> criteria)
            {
                _criteria = criteria;
            }
            private Expression<Func<T, bool>> _criteria;
            public override Expression<Func<T, bool>> Criteria
            {
                get => _criteria;
                protected set => _criteria = value;
            }
            protected override Task<IEnumerable<T>> QueryAsync(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}