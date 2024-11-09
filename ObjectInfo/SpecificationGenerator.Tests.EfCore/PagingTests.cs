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
    public class PagingTests : TestBase
    {
        public PagingTests(DatabaseFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Clear existing data first
            if (Fixture.DbContext.TestEntities.Any())
            {
                Fixture.DbContext.TestEntities.RemoveRange(Fixture.DbContext.TestEntities);
                Fixture.DbContext.SaveChanges();
            }

            // Add enough test entities for paging tests
            var entities = Enumerable.Range(1, 20).Select(i =>
            {
                var testEntity = new TestEntity
                {
                    Name = $"Test Entity {i}",
                    IsActive = i % 2 == 0,
                    CreatedDate = DateTime.Today.AddDays(-i),
                    Value = i * 100m
                };

                var relatedEntity = new RelatedEntity
                {
                    Title = $"Related {i}",
                    Type = TestEntityType.Basic,
                    Price = i * 10.5m,
                    TestEntityId = i  // Set the foreign key explicitly
                };

                testEntity.RelatedEntity = relatedEntity;

                return testEntity;
            }).ToList();

            // Save the entities
            try
            {
                Fixture.DbContext.TestEntities.AddRange(entities);
                Fixture.DbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                Output.WriteLine($"Error saving entities: {ex.Message}");
                throw;
            }
        }

        [Theory]
        [InlineData(0, 5)]
        [InlineData(5, 5)]
        [InlineData(10, 5)]
        public async Task PaginatesResults(int skip, int take)
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => true)
                .OrderBy(e => e.Id)
                .ApplyPaging(skip, take);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria)
                .OrderBy(e => e.Id)
                .Skip(skip)
                .Take(take);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("LIMIT");
            sql.Should().Contain("OFFSET");
            results.Should().HaveCount(take);
        }

        [Fact]
        public async Task PaginatesWithFiltering()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => e.IsActive)
                .OrderBy(e => e.Id)
                .ApplyPaging(0, 5);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria)
                .OrderBy(e => e.Id)
                .Skip(0)
                .Take(5);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("WHERE");
            sql.Should().Contain("LIMIT");
            sql.Should().Contain("OFFSET");
            results.Should().HaveCountLessOrEqualTo(5);
            results.Should().AllSatisfy(e => e.IsActive.Should().BeTrue());
        }

        [Fact]
        public async Task PaginatesWithOrdering()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => true)
                .OrderBy(e => e.CreatedDate)
                .ApplyPaging(0, 5);

            // Act
            // Using only CreatedDate for ordering due to SQLite decimal limitation
            var query = Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria)
                .OrderBy(e => e.CreatedDate)
                .Skip(0)
                .Take(5);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("ORDER BY");
            sql.Should().Contain("LIMIT");
            sql.Should().Contain("OFFSET");
            results.Should().HaveCount(5);
            results.Should().BeInAscendingOrder(e => e.CreatedDate);
        }

        [Fact]
        public async Task PaginatesWithIncludes()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => true)
                .Include(e => e.RelatedEntity)
                .OrderBy(e => e.Id)
                .ApplyPaging(0, 5);

            // Act
            var query = Fixture.DbContext.TestEntities
                .Include(e => e.RelatedEntity)
                .AsNoTracking()  // Add this to prevent tracking issues
                .Where(spec.Criteria)
                .OrderBy(e => e.Id)
                .Take(5);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            // Debug: Check what's in the database
            var dataCheck = await Fixture.DbContext.TestEntities
                .Include(e => e.RelatedEntity)
                .AsNoTracking()
                .Select(e => new { e.Id, HasRelated = e.RelatedEntity != null })
                .Take(10)  // Limit debug output
                .ToListAsync();

            Output.WriteLine("First 10 entities check:");
            foreach (var item in dataCheck)
            {
                Output.WriteLine($"Entity {item.Id}: HasRelated = {item.HasRelated}");
            }

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("LEFT JOIN");
            sql.Should().Contain("LIMIT");
            results.Should().HaveCount(5);
            results.Should().AllSatisfy(e => e.RelatedEntity.Should().NotBeNull());
        }

        [Fact]
        public async Task GetsTotalCount()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => e.IsActive);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>();
            var totalCount = await query.Where(spec.Criteria).CountAsync();
            var results = await query
                .Where(spec.Criteria)
                .OrderBy(e => e.Id)
                .Skip(5)
                .Take(5)
                .ToListAsync();

            // Assert
            totalCount.Should().BeGreaterThan(5); // We seeded 20 entities
            results.Should().HaveCountLessOrEqualTo(5);
        }

        [Theory]
        [InlineData(1, 10)]
        [InlineData(2, 10)]
        [InlineData(3, 5)]
        public async Task CalculatesPageMetadata(int pageNumber, int pageSize)
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => true)
                .OrderBy(e => e.Id)
                .ApplyPaging((pageNumber - 1) * pageSize, pageSize);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>();
            var totalCount = await query.CountAsync(spec.Criteria);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var hasNextPage = pageNumber < totalPages;
            var hasPreviousPage = pageNumber > 1;

            var results = await query
                .Where(spec.Criteria)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Assert
            results.Should().HaveCountLessOrEqualTo(pageSize);
            if (hasNextPage)
            {
                results.Should().HaveCount(pageSize);
            }
            if (hasPreviousPage)
            {
                ((pageNumber - 1) * pageSize).Should().BeGreaterThan(0);
            }
        }

        [Fact]
        public async Task HandlesEmptyPage()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => false)
                .ApplyPaging(0, 10);

            // Act
            var results = await Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria)
                .Skip(0)
                .Take(10)
                .ToListAsync();

            // Assert
            results.Should().BeEmpty();
        }

        [Fact]
        public async Task HandlesOutOfRangePage()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => true)
                .ApplyPaging(1000, 10);

            // Act
            var results = await Fixture.DbContext.Set<TestEntity>()
                .Where(spec.Criteria)
                .Skip(1000)
                .Take(10)
                .ToListAsync();

            // Assert
            results.Should().BeEmpty();
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

            public TestSpecification<T> OrderBy(Expression<Func<T, object>> orderByExpression)
            {
                base.ApplyOrderBy(orderByExpression);
                return this;
            }

            public TestSpecification<T> ThenByDescending(Expression<Func<T, object>> thenByExpression)
            {
                base.AddThenByDescending(thenByExpression);
                return this;
            }

            public TestSpecification<T> Include(Expression<Func<T, object>> includeExpression)
            {
                base.AddInclude(includeExpression);
                return this;
            }

            public TestSpecification<T> ApplyPaging(int skip, int take)
            {
                base.Skip = skip;
                base.Take = take;
                return this;
            }
        }
    }
}
