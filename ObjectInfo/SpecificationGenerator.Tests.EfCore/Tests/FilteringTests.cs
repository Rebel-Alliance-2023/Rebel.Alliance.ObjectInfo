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
        }

        [Fact]
        public async Task FiltersByEqualityCondition()
        {
            // Arrange
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
