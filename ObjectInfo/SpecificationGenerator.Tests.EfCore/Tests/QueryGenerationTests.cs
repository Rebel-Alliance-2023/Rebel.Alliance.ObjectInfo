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
            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add entity with null Value
            var nullValueEntity = new TestEntity
            {
                Name = "Null Value Entity",
                IsActive = true,
                CreatedDate = DateTime.Today,
                Value = null
            };
            Fixture.DbContext.TestEntities.Add(nullValueEntity);

            // Add entity with Premium RelatedEntity
            var premiumEntity = new TestEntity
            {
                Name = "Premium Entity",
                IsActive = true,
                CreatedDate = DateTime.Today,
                Value = 100m,
                RelatedEntity = new RelatedEntity
                {
                    Title = "Premium Related",
                    Type = TestEntityType.Premium,
                    Price = 199.99m
                }
            };
            Fixture.DbContext.TestEntities.Add(premiumEntity);

            // Add entity with Public Children
            var entityWithChildren = new TestEntity
            {
                Name = "Parent Entity",
                IsActive = true,
                CreatedDate = DateTime.Today,
                Value = 150m,
                Children = new List<ChildEntity>
                {
                    new ChildEntity
                    {
                        Name = "Public Child",
                        Scope = ChildEntityScope.Public,
                        Order = 1
                    }
                }
            };
            Fixture.DbContext.TestEntities.Add(entityWithChildren);

            Fixture.DbContext.SaveChanges();
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
            sql.Should().Contain("\"t\".\"IsActive\"");  // Just check for the column reference
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
            sql.Should().Contain("\"t\".\"IsActive\"");
            sql.Should().Contain("ef_compare");
            sql.Should().Contain("500.0");
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
            sql.Should().Contain("\"t\".\"Status\" = 1");
            sql.Should().Contain("ef_compare");
            sql.Should().Contain("1000.0");
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
                (e.Status == TestEntityStatus.Active || e.Value > 1000m).Should().BeTrue());
        }

        [Fact]
        public async Task GeneratesQueryWithNavigationProperties()
        {
            // Arrange - Add required Include for the test
            var spec = new TestSpecification<TestEntity>(e =>
                e.RelatedEntity != null && e.RelatedEntity.Type == TestEntityType.Premium);

            // Act - Apply Include in the query
            var query = Fixture.DbContext.Set<TestEntity>()
                .Include(e => e.RelatedEntity)  // Add Include for navigation property
                .Where(spec.Criteria);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("\"r\".\"Type\"");
            sql.Should().Contain("\"r\".\"Id\" IS NOT NULL");
            results.Should().NotBeEmpty();  // Should find our seeded premium entity
            results.Should().AllSatisfy(e =>
            {
                e.RelatedEntity.Should().NotBeNull();
                e.RelatedEntity!.Type.Should().Be(TestEntityType.Premium);
            });
        }

        [Fact]
        public async Task GeneratesQueryWithCollectionNavigation()
        {
            // Arrange - Add required Include for the test
            var spec = new TestSpecification<TestEntity>(e =>
                e.Children.Any(c => c.Scope == ChildEntityScope.Public));

            // Act - Apply Include in the query
            var query = Fixture.DbContext.Set<TestEntity>()
                .Include(e => e.Children)  // Add Include for navigation property
                .Where(spec.Criteria);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("EXISTS");
            sql.Should().Contain("\"c\".\"Scope\"");
            sql.Should().Contain("= 2");  // Public enum value
            results.Should().NotBeEmpty(); // Should find our seeded entity with public children
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
            sql.Should().Contain("\"t\".\"CreatedDate\" >=");
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
            sql.Should().Contain("\"t\".\"Value\" IS NULL");
            results.Should().NotBeEmpty();  // We now have seeded data with null Value
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