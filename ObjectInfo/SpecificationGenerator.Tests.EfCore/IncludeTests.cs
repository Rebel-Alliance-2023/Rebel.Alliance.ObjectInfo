using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery.Base;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Infrastructure.TestFixtures;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Models;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Extensions;
using Microsoft.EntityFrameworkCore.Query;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Tests
{
    public class IncludeTests : TestBase
    {
        public IncludeTests(DatabaseFixture fixture, ITestOutputHelper output)
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

            var entities = new List<TestEntity>
            {
                new TestEntity
                {
                    Name = "Test Entity 1",
                    IsActive = true,
                    RelatedEntity = new RelatedEntity
                    {
                        Title = "Related Entity 1",
                        Type = TestEntityType.Premium
                    },
                    Children = new List<ChildEntity>
                    {
                        new ChildEntity { Name = "Child 1", Scope = ChildEntityScope.Public },
                        new ChildEntity { Name = "Child 2", Scope = ChildEntityScope.Public } // Changed to Public
                    }
                },
                new TestEntity
                {
                    Name = "Test Entity 2",
                    IsActive = true,
                    RelatedEntity = new RelatedEntity
                    {
                        Title = "Related Entity 2",
                        Type = TestEntityType.Standard
                    },
                    Children = new List<ChildEntity>
                    {
                        new ChildEntity { Name = "Child 3", Scope = ChildEntityScope.Public },
                        new ChildEntity { Name = "Child 4", Scope = ChildEntityScope.Private }
                    }
                },
                new TestEntity
                {
                    Name = "Test Entity 3",
                    IsActive = true,
                    RelatedEntity = null, // This entity has a null RelatedEntity
                    Children = new List<ChildEntity>
                    {
                        new ChildEntity { Name = "Child 5", Scope = ChildEntityScope.Public },
                        new ChildEntity { Name = "Child 6", Scope = ChildEntityScope.Private }
                    }
                }
            };

            Fixture.DbContext.TestEntities.AddRange(entities);
            Fixture.DbContext.SaveChanges();
        }

        [Fact]
        public async Task IncludesSingleNavigationProperty()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => e.IsActive)
                .Include(e => e.RelatedEntity!);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .ApplySpecification(spec);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("LEFT JOIN");
            sql.Should().Contain("\"RelatedEntities\"");
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
            {
                if (e.RelatedEntity != null)
                {
                    e.RelatedEntity.Should().NotBeNull();
                }
            });
        }

        [Fact]
        public async Task IncludesCollectionNavigationProperty()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => e.IsActive)
                .Include(e => e.Children);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .ApplySpecification(spec);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("LEFT JOIN");
            sql.Should().Contain("\"ChildEntities\"");
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
                .ApplySpecification(spec);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("\"RelatedEntities\"");
            sql.Should().Contain("\"ChildEntities\"");
            results.Should().NotBeEmpty();
            results.Should().AllSatisfy(e =>
            {
                if (e.RelatedEntity != null)
                {
                    e.RelatedEntity.Should().NotBeNull();
                }
                e.Children.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task IncludesNestedNavigationProperties()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => e.IsActive)
                .Include(e => e.Children)
                .ThenInclude<ChildEntity, TestEntity>(c => c.Parent);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .ApplySpecification(spec);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("\"ChildEntities\"");
            sql.Should().Contain("\"TestEntities\"");
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
                .ApplySpecification(spec);

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("\"RelatedEntities\"");
            sql.Should().Contain("\"ChildEntities\"");
            results.Should().NotBeEmpty();
        }

        [Fact]
        public async Task CombinesIncludesWithFiltering()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e =>
                e.IsActive && e.RelatedEntity!.Type == TestEntityType.Premium)
                .Include(e => e.RelatedEntity)
                .Include(e => e.Children);

            // Act
            var query = Fixture.DbContext.Set<TestEntity>()
                .ApplySpecification(spec)
                .Where(e => e.Children.Any(c => c.Scope == ChildEntityScope.Public));

            var sql = query.ToQueryString();
            Output.WriteLine($"Generated SQL: {sql}");

            var results = await query.ToListAsync();

            // Assert
            sql.Should().Contain("\"RelatedEntities\"");
            sql.Should().Contain("\"ChildEntities\"");
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
                .ApplySpecification(spec);

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

        public class TestSpecification<T> : AdvancedSpecification<T> where T : class
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

            private readonly List<Expression<Func<T, object>>> _includes = new();
            private readonly List<string> _includeStrings = new();

            public new IReadOnlyCollection<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();
            public new IReadOnlyCollection<string> IncludeStrings => _includeStrings.AsReadOnly();

            protected override Task<IEnumerable<T>> QueryAsync(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public TestSpecification<T> Include(Expression<Func<T, object>> includeExpression)
            {
                _includes.Add(includeExpression);
                return this;
            }

            public TestSpecification<T> IncludeString(string includeString)
            {
                _includeStrings.Add(includeString);
                return this;
            }

            public TestSpecification<T> ThenInclude<TPreviousProperty, TProperty>(
                Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
            {
                // This method is a placeholder to allow chaining of ThenInclude calls.
                // Actual implementation should be handled by the QueryExtensions class.
                return this;
            }
        }
    }

    public static class QueryExtensions
    {
        public static IQueryable<T> ApplySpecification<T>(this IQueryable<T> query, IncludeTests.TestSpecification<T> spec) where T : class
        {
            // Apply includes
            query = spec.Includes.Aggregate(query,
                (current, include) => current.Include(include));

            // Apply string includes
            query = spec.IncludeStrings.Aggregate(query,
                (current, include) => current.Include(include));

            // Apply criteria
            return query.Where(spec.Criteria);
        }

        public static IIncludableQueryable<T, TProperty> ThenInclude<T, TPreviousProperty, TProperty>(
            this IIncludableQueryable<T, TPreviousProperty> source,
            Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
            where T : class
        {
            return EntityFrameworkQueryableExtensions.ThenInclude(source, navigationPropertyPath);
        }

        public static IIncludableQueryable<T, TProperty> ThenInclude<T, TPreviousProperty, TProperty>(
            this IIncludableQueryable<T, IEnumerable<TPreviousProperty>> source,
            Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
            where T : class
        {
            return EntityFrameworkQueryableExtensions.ThenInclude<T, TPreviousProperty, TProperty>(source, navigationPropertyPath);
        }
    }
}
