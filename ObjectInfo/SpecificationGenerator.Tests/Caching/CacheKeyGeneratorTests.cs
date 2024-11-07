using System;
using System.Linq.Expressions;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching.Implementation;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestEntities;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Caching
{
    public class CacheKeyGeneratorTests
    {
        private readonly ITestOutputHelper _output;
        private readonly ICacheKeyGenerator _generator;

        public CacheKeyGeneratorTests(ITestOutputHelper output)
        {
            _output = output;
            _generator = new DefaultCacheKeyGenerator();
        }

        [Fact]
        public void GenerateKey_ForSameSpecification_ReturnsSameKey()
        {
            // Arrange
            var spec1 = new TestSpecification<TestEntity>(e => e.IsActive);
            var spec2 = new TestSpecification<TestEntity>(e => e.IsActive);

            // Act
            var key1 = _generator.GenerateKey(spec1);
            var key2 = _generator.GenerateKey(spec2);

            // Assert
            key1.Should().Be(key2);
        }

        [Fact]
        public void GenerateKey_ForDifferentSpecifications_ReturnsDifferentKeys()
        {
            // Arrange
            var spec1 = new TestSpecification<TestEntity>(e => e.IsActive);
            var spec2 = new TestSpecification<TestEntity>(e => e.Value > 100);

            // Act
            var key1 = _generator.GenerateKey(spec1);
            var key2 = _generator.GenerateKey(spec2);

            // Assert
            key1.Should().NotBe(key2);
        }

        [Fact]
        public void GenerateKey_IncludesTypeName()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => e.IsActive);

            // Act
            var key = _generator.GenerateKey(spec);

            // Assert
            key.Should().Contain(typeof(TestEntity).FullName);
        }

        [Fact]
        public void GenerateKey_WithIncludes_GeneratesDifferentKeys()
        {
            // Arrange
            var spec1 = new TestSpecification<TestEntity>(e => e.IsActive)
                .AddInclude(e => e.RelatedEntity);
            var spec2 = new TestSpecification<TestEntity>(e => e.IsActive)
                .AddInclude(e => e.NestedEntities);

            // Act
            var key1 = _generator.GenerateKey(spec1);
            var key2 = _generator.GenerateKey(spec2);

            // Assert
            key1.Should().NotBe(key2);
        }

        [Fact]
        public void GenerateKey_WithOrdering_GeneratesDifferentKeys()
        {
            // Arrange
            var spec1 = new TestSpecification<TestEntity>(e => e.IsActive)
                .AddOrderBy(e => e.Name);
            var spec2 = new TestSpecification<TestEntity>(e => e.IsActive)
                .AddOrderByDescending(e => e.Name);

            // Act
            var key1 = _generator.GenerateKey(spec1);
            var key2 = _generator.GenerateKey(spec2);

            // Assert
            key1.Should().NotBe(key2);
        }

        [Fact]
        public void GenerateKey_WithPaging_GeneratesDifferentKeys()
        {
            // Arrange
            var spec1 = new TestSpecification<TestEntity>(e => e.IsActive)
                .ApplyPaging(0, 10);
            var spec2 = new TestSpecification<TestEntity>(e => e.IsActive)
                .ApplyPaging(10, 10);

            // Act
            var key1 = _generator.GenerateKey(spec1);
            var key2 = _generator.GenerateKey(spec2);

            // Assert
            key1.Should().NotBe(key2);
        }

        [Fact]
        public void GenerateKey_WithComplexCriteria_GeneratesConsistentKeys()
        {
            // Arrange
            var spec1 = new TestSpecification<TestEntity>(e => 
                e.IsActive && e.Value > 100 || e.Status == TestEntityStatus.Active);
            var spec2 = new TestSpecification<TestEntity>(e => 
                e.IsActive && e.Value > 100 || e.Status == TestEntityStatus.Active);

            // Act
            var key1 = _generator.GenerateKey(spec1);
            var key2 = _generator.GenerateKey(spec2);

            // Assert
            key1.Should().Be(key2);
        }

        private class TestSpecification<T> : ISpecification<T> where T : class
        {
            private readonly List<Expression<Func<T, object>>> _includes = new();
            private readonly Expression<Func<T, bool>> _criteria;
            private Expression<Func<T, object>>? _orderBy;
            private Expression<Func<T, object>>? _orderByDescending;
            private int? _skip;
            private int? _take;

            public TestSpecification(Expression<Func<T, bool>> criteria)
            {
                _criteria = criteria;
            }

            public Expression<Func<T, bool>> Criteria => _criteria;
            public IEnumerable<Expression<Func<T, object>>> Includes => _includes;
            public IEnumerable<string> IncludeStrings => Enumerable.Empty<string>();
            public Expression<Func<T, object>>? OrderBy => _orderBy;
            public Expression<Func<T, object>>? OrderByDescending => _orderByDescending;
            public IEnumerable<Expression<Func<T, object>>> ThenByExpressions => Enumerable.Empty<Expression<Func<T, object>>>();
            public IEnumerable<Expression<Func<T, object>>> ThenByDescendingExpressions => Enumerable.Empty<Expression<Func<T, object>>>();
            public int? Skip => _skip;
            public int? Take => _take;
            public bool IsPagingEnabled => Skip.HasValue && Take.HasValue;
            public IDictionary<string, ISpecification<object>> NestedSpecifications => new Dictionary<string, ISpecification<object>>();

            public TestSpecification<T> AddInclude(Expression<Func<T, object>> includeExpression)
            {
                _includes.Add(includeExpression);
                return this;
            }

            public TestSpecification<T> AddOrderBy(Expression<Func<T, object>> orderByExpression)
            {
                _orderBy = orderByExpression;
                return this;
            }

            public TestSpecification<T> AddOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
            {
                _orderByDescending = orderByDescExpression;
                return this;
            }

            public TestSpecification<T> ApplyPaging(int skip, int take)
            {
                _skip = skip;
                _take = take;
                return this;
            }

            public ISpecification<T> And(ISpecification<T> specification) => throw new NotImplementedException();
            public ISpecification<T> Or(ISpecification<T> specification) => throw new NotImplementedException();
            public ISpecification<T> Not() => throw new NotImplementedException();
            public bool IsSatisfiedBy(T entity) => throw new NotImplementedException();
            public string ToSql() => throw new NotImplementedException();
            public IDictionary<string, object> GetParameters() => throw new NotImplementedException();
            public Task<int> GetCountAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }
    }
}
