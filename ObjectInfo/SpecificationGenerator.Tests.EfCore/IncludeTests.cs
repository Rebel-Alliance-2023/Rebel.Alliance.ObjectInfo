
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery.Base;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Infrastructure.TestFixtures;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Models;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Tests
{
    public class IncludeTests : TestBase
    {
        public IncludeTests(DatabaseFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact]
        public async Task IncludesSingleNavigationProperty()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => e.IsActive)
                .Include(e => e.RelatedEntity);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("INNER JOIN");
            sql.Should().Contain("\"RelatedEntity\"");
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
                e.RelatedEntity.Should().NotBeNull());
        }

        [Fact]
        public async Task IncludesCollectionNavigationProperty()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => e.IsActive)
                .Include(e => e.Children);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("LEFT JOIN");
            sql.Should().Contain("\"Children\"");
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
                e.Children.Should().NotBeNull());
        }

        [Fact]
        public async Task IncludesMultipleNavigationProperties()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => e.IsActive)
                .Include(e => e.RelatedEntity)
                .Include(e => e.Children);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("\"RelatedEntity\"");
            sql.Should().Contain("\"Children\"");
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
            {
                e.RelatedEntity.Should().NotBeNull();
                e.Children.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task IncludesNestedNavigationProperties()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => e.IsActive)
                .Include(e => e.Children.Select(c => c.Parent));

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("\"Children\"");
            sql.Should().Contain("\"Parent\"");
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
                e.Children.Should().NotBeNull().And
                    .AllSatisfy(c => c.Parent.Should().NotBeNull()));
        }

        [Fact]
        public async Task IncludesWithStringPath()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => e.IsActive)
                .IncludeString("RelatedEntity.Children");

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("\"RelatedEntity\"");
            sql.Should().Contain("\"Children\"");
            results.Should().NotBeEmpty();
        }

        [Fact]
        public async Task CombinesIncludesWithFiltering()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e =>
                e.IsActive && e.RelatedEntity.Type == TestEntityType.Premium)
                .Include(e => e.RelatedEntity)
                .Include(e => e.Children.Where(c => c.Scope == ChildEntityScope.Public));

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("\"RelatedEntity\"");
            sql.Should().Contain("\"Children\"");
            sql.Should().Contain("\"Type\" = ");
            sql.Should().Contain("\"Scope\" = ");
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
            {
                e.RelatedEntity.Should().NotBeNull();
                e.RelatedEntity!.Type.Should().Be(TestEntityType.Premium);
                e.Children.Should().NotBeNull()
                    .And.AllSatisfy(c => c.Scope.Should().Be(ChildEntityScope.Public));
            });
        }

        [Fact]
        public async Task IncludesHandleNullNavigationProperties()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => e.RelatedEntity == null)
                .Include(e => e.RelatedEntity);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("LEFT JOIN");
            sql.Should().Contain("IS NULL");
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
                e.RelatedEntity.Should().BeNull());
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

            public TestSpecification<T> Include(Expression<Func<T, object>> includeExpression)
            {
                AddInclude(includeExpression);
                return this;
            }

            public TestSpecification<T> IncludeString(string includeString)
            {
                AddInclude(includeString);
                return this;
            }
        }
    }
}