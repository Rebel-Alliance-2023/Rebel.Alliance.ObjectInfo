using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using Moq;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching.Configuration;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching.Implementation;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestEntities;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestFixtures;
using System.Linq.Expressions;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Caching
{
    public class CompiledQueryCacheTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ICacheKeyGenerator _keyGenerator;
        private readonly ICacheStatistics _statistics;
        private readonly CompiledQueryCacheOptions _options;

        public CompiledQueryCacheTests(DatabaseFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            _keyGenerator = new DefaultCacheKeyGenerator();
            _statistics = new Mock<ICacheStatistics>().Object;
            _options = new CompiledQueryCacheOptions
            {
                MaxCachedQueries = 100,
                QueryTimeout = TimeSpan.FromMinutes(30),
                MinimumHitsForCaching = 2,
                EnableQueryPlanCaching = true
            };
        }

        [Fact]
        public void GetOrAddQueryTransformer_ReturnsSameTransformer_ForSameSpecification()
        {
            // Arrange
            var cache = CreateCache();
            var spec = new TestSpecification<TestEntity>(e => e.IsActive);

            // Act
            var transformer1 = cache.GetOrAddQueryTransformer(spec);
            var transformer2 = cache.GetOrAddQueryTransformer(spec);

            // Assert
            transformer1.Should().BeSameAs(transformer2);
        }

        [Fact]
        public void GetOrAddQueryTransformer_ReturnsDifferentTransformers_ForDifferentSpecifications()
        {
            // Arrange
            var cache = CreateCache();
            var spec1 = new TestSpecification<TestEntity>(e => e.IsActive);
            var spec2 = new TestSpecification<TestEntity>(e => e.Value > 100);

            // Act
            var transformer1 = cache.GetOrAddQueryTransformer(spec1);
            var transformer2 = cache.GetOrAddQueryTransformer(spec2);

            // Assert
            transformer1.Should().NotBeSameAs(transformer2);
        }

        [Fact]
        public void GetOrAddSqlQuery_ReturnsSameQuery_ForSameSpecification()
        {
            // Arrange
            var cache = CreateCache();
            var spec = new TestSqlSpecification<TestEntity>(e => e.IsActive);

            // Act
            var sql1 = cache.GetOrAddSqlQuery(spec);
            var sql2 = cache.GetOrAddSqlQuery(spec);

            // Assert
            sql1.Should().Be(sql2);
        }

        [Fact]
        public void InvalidateQueriesForType_RemovesCachedQueries()
        {
            // Arrange
            var cache = CreateCache();
            var spec = new TestSpecification<TestEntity>(e => e.IsActive);
            var transformer1 = cache.GetOrAddQueryTransformer(spec);

            // Act
            cache.InvalidateQueriesForType<TestEntity>();
            var transformer2 = cache.GetOrAddQueryTransformer(spec);

            // Assert
            transformer1.Should().NotBeSameAs(transformer2);
        }

        [Fact]
        public void InvalidateAllQueries_RemovesAllCachedQueries()
        {
            // Arrange
            var cache = CreateCache();
            var spec1 = new TestSpecification<TestEntity>(e => e.IsActive);
            var spec2 = new TestSpecification<ComplexEntity>(e => e.IsAvailable);

            var transformer1 = cache.GetOrAddQueryTransformer(spec1);
            var transformer2 = cache.GetOrAddQueryTransformer(spec2);

            // Act
            cache.InvalidateAllQueries();
            var newTransformer1 = cache.GetOrAddQueryTransformer(spec1);
            var newTransformer2 = cache.GetOrAddQueryTransformer(spec2);

            // Assert
            transformer1.Should().NotBeSameAs(newTransformer1);
            transformer2.Should().NotBeSameAs(newTransformer2);
        }

        [Fact]
        public void CacheCleanup_RemovesExpiredQueries()
        {
            // Arrange
            _options.QueryTimeout = TimeSpan.FromMilliseconds(50);
            var cache = CreateCache();
            var spec = new TestSpecification<TestEntity>(e => e.IsActive);

            // Act
            var transformer1 = cache.GetOrAddQueryTransformer(spec);
            Task.Delay(_options.QueryTimeout.Add(TimeSpan.FromMilliseconds(10))).Wait();
            var transformer2 = cache.GetOrAddQueryTransformer(spec);

            // Assert
            transformer1.Should().NotBeSameAs(transformer2);
        }

        [Fact]
        public void CacheSize_DoesNotExceedLimit()
        {
            // Arrange
            _options.MaxCachedQueries = 2;
            var cache = CreateCache();

            // Act
            var spec1 = new TestSpecification<TestEntity>(e => e.IsActive);
            var spec2 = new TestSpecification<TestEntity>(e => e.Value > 100);
            var spec3 = new TestSpecification<TestEntity>(e => e.Status == TestEntityStatus.Active);

            var transformer1 = cache.GetOrAddQueryTransformer(spec1);
            var transformer2 = cache.GetOrAddQueryTransformer(spec2);
            var transformer3 = cache.GetOrAddQueryTransformer(spec3);
            var transformerCheck = cache.GetOrAddQueryTransformer(spec1);

            // Assert
            transformer1.Should().NotBeSameAs(transformerCheck);
        }

        private ICompiledQueryCache CreateCache()
        {
            return new CompiledQueryCache(
                Options.Create(_options),
                _keyGenerator,
                _statistics);
        }

        private class TestSpecification<T> : ISpecification<T> where T : class
        {
            private readonly Expression<Func<T, bool>> _criteria;

            public TestSpecification(Expression<Func<T, bool>> criteria)
            {
                _criteria = criteria;
            }

            public Expression<Func<T, bool>> Criteria => _criteria;
            public IEnumerable<Expression<Func<T, object>>> Includes => Enumerable.Empty<Expression<Func<T, object>>>();
            public IEnumerable<string> IncludeStrings => Enumerable.Empty<string>();
            public Expression<Func<T, object>>? OrderBy => null;
            public Expression<Func<T, object>>? OrderByDescending => null;
            public IEnumerable<Expression<Func<T, object>>> ThenByExpressions => Enumerable.Empty<Expression<Func<T, object>>>();
            public IEnumerable<Expression<Func<T, object>>> ThenByDescendingExpressions => Enumerable.Empty<Expression<Func<T, object>>>();
            public int? Skip => null;
            public int? Take => null;
            public bool IsPagingEnabled => false;
            public IDictionary<string, ISpecification<object>> NestedSpecifications => new Dictionary<string, ISpecification<object>>();

            public ISpecification<T> And(ISpecification<T> specification) => throw new NotImplementedException();
            public ISpecification<T> Or(ISpecification<T> specification) => throw new NotImplementedException();
            public ISpecification<T> Not() => throw new NotImplementedException();
            public bool IsSatisfiedBy(T entity) => throw new NotImplementedException();
            public virtual string ToSql() => throw new NotImplementedException();
            public virtual IDictionary<string, object> GetParameters() => throw new NotImplementedException();
            public Task<int> GetCountAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }

        private class TestSqlSpecification<T> : TestSpecification<T> where T : class
        {
            public TestSqlSpecification(Expression<Func<T, bool>> criteria) : base(criteria)
            {
            }

            public override string ToSql()
            {
                return $"SELECT * FROM {typeof(T).Name} WHERE {Criteria}";
            }

            public override IDictionary<string, object> GetParameters()
            {
                return new Dictionary<string, object>();
            }
        }

    }
}
