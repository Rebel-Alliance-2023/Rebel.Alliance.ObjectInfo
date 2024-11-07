using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestEntities;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.Helpers;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestFixtures;
using System.Linq.Expressions;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Runtime
{
    public class CompositeSpecificationTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;
        private readonly ITestOutputHelper _output;

        public CompositeSpecificationTests(DatabaseFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact]
        public void And_CombinesTwoSpecifications_WithCorrectLogic()
        {
            // Arrange
            var spec1 = new TestSpecification<TestEntity>(e => e.IsActive);
            var spec2 = new TestSpecification<TestEntity>(e => e.Value > 100);

            // Act
            var combined = spec1.And(spec2);

            // Assert
            var query = _fixture.CreateTestData();
            var result = ApplySpecification(query, combined).ToList();
            result.Should().OnlyContain(e => e.IsActive && e.Value > 100);
        }

        [Fact]
        public void Or_CombinesTwoSpecifications_WithCorrectLogic()
        {
            // Arrange
            var spec1 = new TestSpecification<TestEntity>(e => e.IsActive);
            var spec2 = new TestSpecification<TestEntity>(e => e.Status == TestEntityStatus.Draft);

            // Act
            var combined = spec1.Or(spec2);

            // Assert
            var query = _fixture.CreateTestData();
            var result = ApplySpecification(query, combined).ToList();
            result.Should().OnlyContain(e => e.IsActive || e.Status == TestEntityStatus.Draft);
        }

        [Fact]
        public void Not_InvertsSpecification_WithCorrectLogic()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>(e => e.IsActive);

            // Act
            var notSpec = spec.Not();

            // Assert
            var query = _fixture.CreateTestData();
            var result = ApplySpecification(query, notSpec).ToList();
            result.Should().OnlyContain(e => !e.IsActive);
        }

        [Fact]
        public void ComplexComposition_CombinesMultipleSpecifications_WithCorrectLogic()
        {
            // Arrange
            var activeSpec = new TestSpecification<TestEntity>(e => e.IsActive);
            var draftSpec = new TestSpecification<TestEntity>(e => e.Status == TestEntityStatus.Draft);
            var valueSpec = new TestSpecification<TestEntity>(e => e.Value > 100);

            // Act
            var combined = activeSpec.And(draftSpec).Or(valueSpec);

            // Assert
            var query = _fixture.CreateTestData();
            var result = ApplySpecification(query, combined).ToList();
            result.Should().OnlyContain(e => 
                (e.IsActive && e.Status == TestEntityStatus.Draft) || e.Value > 100);
        }

        [Fact]
        public void Includes_AreMergedCorrectly_WhenCombiningSpecifications()
        {
            // Arrange
            var spec1 = new TestSpecification<TestEntity>(e => e.IsActive)
                .AddInclude(e => e.RelatedEntity);
            var spec2 = new TestSpecification<TestEntity>(e => e.Value > 100)
                .AddInclude(e => e.NestedEntities);

            // Act
            var combined = spec1.And(spec2);

            // Assert
            combined.Includes.Should().HaveCount(2);
            combined.Includes.Select(i => i.ToString()).Should().Contain(i => 
                i.Contains("RelatedEntity") && i.Contains("NestedEntities"));
        }

        [Fact]
        public void Ordering_IsPreserved_WhenCombiningSpecifications()
        {
            // Arrange
            var spec1 = new TestSpecification<TestEntity>(e => e.IsActive)
                .AddOrderBy(e => e.Name);
            var spec2 = new TestSpecification<TestEntity>(e => e.Value > 100)
                .AddOrderBy(e => e.CreatedDate);

            // Act
            var combined = spec1.And(spec2);

            // Assert
            combined.OrderBy.Should().NotBeNull();
            var query = _fixture.CreateTestData();
            var result = ApplySpecification(query, combined).ToList();
            result.Should().BeInAscendingOrder(e => e.Name);
        }

        [Fact]
        public void Paging_IsPreserved_WhenCombiningSpecifications()
        {
            // Arrange
            var spec1 = new TestSpecification<TestEntity>(e => e.IsActive)
                .ApplyPaging(0, 5);
            var spec2 = new TestSpecification<TestEntity>(e => e.Value > 100);

            // Act
            var combined = spec1.And(spec2);

            // Assert
            combined.Skip.Should().Be(0);
            combined.Take.Should().Be(5);
            var query = _fixture.CreateTestData();
            var result = ApplySpecification(query, combined).ToList();
            result.Should().HaveCountLessOrEqualTo(5);
        }

        private IQueryable<T> ApplySpecification<T>(IQueryable<T> query, ISpecification<T> spec) where T : class
        {
            if (spec.Criteria != null)
                query = query.Where(spec.Criteria);

            foreach (var include in spec.Includes)
                query = query.Include(include);

            if (spec.OrderBy != null)
                query = query.OrderBy(spec.OrderBy);
            else if (spec.OrderByDescending != null)
                query = query.OrderByDescending(spec.OrderByDescending);

            if (spec.Skip.HasValue && spec.Take.HasValue)
                query = query.Skip(spec.Skip.Value).Take(spec.Take.Value);

            return query;
        }

        private class TestSpecification<T> : ISpecification<T> where T : class
        {
            private List<Expression<Func<T, object>>> _includes = new();
            private Expression<Func<T, bool>>? _criteria;
            private Expression<Func<T, object>>? _orderBy;
            private Expression<Func<T, object>>? _orderByDescending;
            private int? _skip;
            private int? _take;

            public TestSpecification(Expression<Func<T, bool>>? criteria = null)
            {
                _criteria = criteria;
            }

            public Expression<Func<T, bool>> Criteria => _criteria ?? (x => true);
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


            public ISpecification<T> And(ISpecification<T> specification)
            {
                if (specification == null)
                    throw new ArgumentNullException(nameof(specification));

                var parameter = Expression.Parameter(typeof(T), "x");
                var body = Expression.AndAlso(
                    new ExpressionParameterReplacer(parameter).Visit(Criteria.Body),
                    new ExpressionParameterReplacer(parameter).Visit(specification.Criteria.Body));

                return new TestSpecification<T>(Expression.Lambda<Func<T, bool>>(body, parameter))
                {
                    _includes = _includes.Union(specification.Includes).ToList(),
                    _orderBy = _orderBy ?? specification.OrderBy,
                    _orderByDescending = _orderByDescending ?? specification.OrderByDescending,
                    _skip = _skip ?? specification.Skip,
                    _take = _take ?? specification.Take
                };
            }

            public ISpecification<T> Or(ISpecification<T> specification)
            {
                if (specification == null)
                    throw new ArgumentNullException(nameof(specification));

                var parameter = Expression.Parameter(typeof(T), "x");
                var body = Expression.OrElse(
                    new ExpressionParameterReplacer(parameter).Visit(Criteria.Body),
                    new ExpressionParameterReplacer(parameter).Visit(specification.Criteria.Body));

                return new TestSpecification<T>(Expression.Lambda<Func<T, bool>>(body, parameter))
                {
                    _includes = _includes.Union(specification.Includes).ToList(),
                    _orderBy = _orderBy ?? specification.OrderBy,
                    _orderByDescending = _orderByDescending ?? specification.OrderByDescending,
                    _skip = _skip ?? specification.Skip,
                    _take = _take ?? specification.Take
                };
            }

            public ISpecification<T> Not()
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var body = Expression.Not(new ExpressionParameterReplacer(parameter).Visit(Criteria.Body));
                return new TestSpecification<T>(Expression.Lambda<Func<T, bool>>(body, parameter))
                {
                    _includes = _includes,
                    _orderBy = _orderBy,
                    _orderByDescending = _orderByDescending,
                    _skip = _skip,
                    _take = _take
                };
            }

            public bool IsSatisfiedBy(T entity)
            {
                return Criteria.Compile()(entity);
            }

            public string ToSql()
            {
                throw new NotImplementedException();
            }

            public IDictionary<string, object> GetParameters()
            {
                throw new NotImplementedException();
            }

            public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }

        private class ExpressionParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _parameter;

            public ExpressionParameterReplacer(ParameterExpression parameter)
            {
                _parameter = parameter;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return base.VisitParameter(_parameter);
            }
        }
    }
}