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
    public class FilteringTests : TestBase
    {
        public FilteringTests(DatabaseFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            //SeedTestData();
        }
        private void SeedTestData()
        {
            // Clear existing data first
            if (Fixture.DbContext.TestEntities.Any())
            {
                Fixture.DbContext.TestEntities.RemoveRange(Fixture.DbContext.TestEntities);
                Fixture.DbContext.SaveChanges();
            }

            var entities = new List<TestEntity>
            {
                new TestEntity
                {
                    Name = "Test Entity 1",
                    IsActive = true,
                    Status = TestEntityStatus.Active, // Ensure status is Active
                    RelatedEntity = new RelatedEntity
                    {
                        Title = "Related Entity 1",
                        Type = TestEntityType.Premium,
                        Price = 150m // Ensure price is greater than 100m
                    },
                    Children = new List<ChildEntity>
                    {
                        new ChildEntity { Name = "Child 1", Scope = ChildEntityScope.Public },
                        new ChildEntity { Name = "Child 2", Scope = ChildEntityScope.Private },
                        new ChildEntity { Name = "Child 3", Scope = ChildEntityScope.Public } // Ensure more than 2 children
                    }
                },
                new TestEntity
                {
                    Name = "Test Entity 2",
                    IsActive = true,
                    Status = TestEntityStatus.Inactive, // Status is Inactive
                    RelatedEntity = new RelatedEntity
                    {
                        Title = "Related Entity 2",
                        Type = TestEntityType.Standard,
                        Price = 50m // Price less than 100m
                    },
                    Children = new List<ChildEntity>
                    {
                        new ChildEntity { Name = "Child 4", Scope = ChildEntityScope.Public },
                        new ChildEntity { Name = "Child 5", Scope = ChildEntityScope.Private }
                    }
                },
                new TestEntity
                {
                    Name = "Test Entity 3",
                    IsActive = true,
                    Status = TestEntityStatus.Inactive, // Status is Inactive
                    RelatedEntity = null, // This entity has a null RelatedEntity
                    Children = new List<ChildEntity>
                    {
                        new ChildEntity { Name = "Child 6", Scope = ChildEntityScope.Public },
                        new ChildEntity { Name = "Child 7", Scope = ChildEntityScope.Private }
                    }
                }
            };

            Fixture.DbContext.TestEntities.AddRange(entities);
            Fixture.DbContext.SaveChanges();
        }


        [Fact]
        public async Task FiltersByEqualityCondition()
        {
            // Arrange
            SeedTestData();
            var spec = new TestSpecification<TestEntity>(e => e.Status == TestEntityStatus.Active);

            // Act
            var results = await Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria)
                .ToListAsync();

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e => e.Status.Should().Be(TestEntityStatus.Active));
        }

        [Fact]
        public async Task FiltersByNumericRange()
        {
            // Arrange
            var minValue = 300m;
            var maxValue = 700m;
            var spec = new TestSpecification<TestEntity>(e =>
                e.Value >= minValue && e.Value <= maxValue);

            // Act
            var results = await Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria)
                .ToListAsync();

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
                e.Value.Should().BeInRange(minValue, maxValue));
        }

        [Fact]
        public async Task FiltersByDateRange()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(-7);
            var endDate = DateTime.Today.AddDays(-2);
            var spec = new TestSpecification<TestEntity>(e =>
                e.CreatedDate >= startDate && e.CreatedDate <= endDate);

            // Act
            var results = await Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria)
                .ToListAsync();

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
                e.CreatedDate.Should().BeOnOrAfter(startDate)
                    .And.BeOnOrBefore(endDate));
        }

        [Fact]
        public async Task FiltersByStringContains()
        {
            // Arrange
            var searchTerm = "Test";
            var spec = new TestSpecification<TestEntity>(e =>
                e.Name.Contains(searchTerm));

            // Act
            var results = await Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria)
                .ToListAsync();

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
                e.Name.Should().Contain(searchTerm));
        }

        [Fact]
        public async Task FiltersByStringStartsWith()
        {
            // Arrange
            var prefix = "Test";
            var spec = new TestSpecification<TestEntity>(e =>
                e.Name.StartsWith(prefix));

            // Act
            var results = await Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria)
                .ToListAsync();

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
                e.Name.Should().StartWith(prefix));
        }

        [Fact]
        public async Task FiltersByRelatedEntityProperties()
        {
            // Arrange
            SeedTestData();
            var spec = new TestSpecification<TestEntity>(e =>
                e.RelatedEntity != null &&
                e.RelatedEntity.Price > 100m &&
                e.RelatedEntity.Type == TestEntityType.Premium);

            // Act
            var results = await Fixture.DbContext.Set<TestEntity>()
                .Include(e => e.RelatedEntity)
                .Where(spec.Criteria)
                .ToListAsync();

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
            {
                e.RelatedEntity.Should().NotBeNull();
                e.RelatedEntity!.Price.Should().BeGreaterThan(100m);
                e.RelatedEntity.Type.Should().Be(TestEntityType.Premium);
            });
        }

        [Fact]
        public async Task FiltersByChildCollectionProperties()
        {
            // Arrange
            SeedTestData();
            var spec = new TestSpecification<TestEntity>(e =>
                e.Children.Any(c => c.Scope == ChildEntityScope.Public) &&
                e.Children.Count > 2);

            // Act
            var results = await Fixture.DbContext.Set<TestEntity>()
                .Include(e => e.Children)
                .Where(spec.Criteria)
                .ToListAsync();

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
            {
                e.Children.Should().Contain(c => c.Scope == ChildEntityScope.Public);
                e.Children.Should().HaveCountGreaterThan(2);
            });
        }

        [Fact]
        public async Task FiltersByComplexConditions()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e =>
                (e.Status == TestEntityStatus.Active || e.Value > 1000m) &&
                e.IsActive &&
                (e.RelatedEntity == null || e.RelatedEntity.Type != TestEntityType.Basic));

            // Act
            var results = await Fixture.DbContext.Set<TestEntity>()
                .Include(e => e.RelatedEntity)
                .Where(spec.Criteria)
                .ToListAsync();

            // Assert
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
            {
                (e.Status == TestEntityStatus.Active || e.Value > 1000m)
                    .Should().BeTrue();
                e.IsActive.Should().BeTrue();
                (e.RelatedEntity == null || e.RelatedEntity.Type != TestEntityType.Basic)
                    .Should().BeTrue();
            });
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
