using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestEntities;
using Xunit;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestFixtures;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests
{
    public class BaseSpecificationTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;

        public BaseSpecificationTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Criteria_DefaultsToAllTrue()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>();

            // Act
            var criteria = spec.GetCriteria().Compile();

            // Assert
            var testEntity = new TestEntity();
            Assert.True(criteria(testEntity));
        }

        [Fact]
        public void And_CombinesSpecificationsCorrectly()
        {
            // Arrange
            var spec1 = new TestSpecification<TestEntity>();
            var spec2 = new TestSpecification<TestEntity>();
            spec1.SetCriteria(e => e.Id > 0);
            spec2.SetCriteria(e => e.Name != null);

            // Act
            var combinedSpec = spec1.AndSpecification(spec2);

            // Assert
            var criteria = ((TestSpecification<TestEntity>)combinedSpec).GetCriteria().Compile();
            Assert.True(criteria(new TestEntity { Id = 1, Name = "Test" }));
            Assert.False(criteria(new TestEntity { Id = 0, Name = "Test" }));
            Assert.False(criteria(new TestEntity { Id = 1, Name = null }));
        }

        [Fact]
        public void Or_CombinesSpecificationsCorrectly()
        {
            // Arrange
            var spec1 = new TestSpecification<TestEntity>();
            var spec2 = new TestSpecification<TestEntity>();
            spec1.SetCriteria(e => e.Id > 0);
            spec2.SetCriteria(e => e.Name == "Test");

            // Act
            var combinedSpec = spec1.OrSpecification(spec2);

            // Assert
            var criteria = ((TestSpecification<TestEntity>)combinedSpec).GetCriteria().Compile();
            Assert.True(criteria(new TestEntity { Id = 1, Name = "Other" }));
            Assert.True(criteria(new TestEntity { Id = 0, Name = "Test" }));
            Assert.False(criteria(new TestEntity { Id = 0, Name = "Other" }));
        }

        [Fact]
        public void IsSatisfiedBy_EvaluatesCorrectly()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>();
            spec.SetCriteria(e => e.Id > 0 && e.Name != null);
            var entity1 = new TestEntity { Id = 1, Name = "Test" };
            var entity2 = new TestEntity { Id = 0, Name = null };

            // Act & Assert
            Assert.True(spec.SatisfiedBy(entity1));
            Assert.False(spec.SatisfiedBy(entity2));
        }

        [Fact]
        public void Includes_WorkAsExpected()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>();
            spec.AddIncludeExpression(e => e.RelatedEntity);

            // Assert
            Assert.Single(spec.GetIncludes());
            Assert.Contains(spec.GetIncludes(), i => i.ToString().Contains("RelatedEntity"));
        }

        [Fact]
        public void OrderBy_AppliesCorrectly()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>();
            spec.SetOrderBy(e => e.Name);

            // Act
            var query = _fixture.CreateTestData().AsQueryable();
            var result = spec.Evaluate(query).ToList();

            // Assert
            Assert.Equal(
                result.Select(e => e.Name),
                result.Select(e => e.Name).OrderBy(n => n));
        }

        [Fact]
        public void Paging_WorksAsExpected()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>();
            spec.SetPaging(1, 2);

            // Act
            var query = _fixture.CreateTestData().AsQueryable();
            var result = spec.Evaluate(query).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(_fixture.CreateTestData().Skip(1).Take(2).Select(e => e.Id), 
                        result.Select(e => e.Id));
        }

        private class TestSpecification<T> : BaseSpecification<T> where T : class
        {
            private Expression<Func<T, bool>> _criteria = x => true;
            private readonly List<Expression<Func<T, object>>> _includes = new();
            private Expression<Func<T, object>>? _orderBy;
            private int? _skip;
            private int? _take;

            public Expression<Func<T, bool>> GetCriteria() => _criteria;
            public IEnumerable<Expression<Func<T, object>>> GetIncludes() => _includes;

            public void SetCriteria(Expression<Func<T, bool>> criteria) => _criteria = criteria;
            public void SetOrderBy(Expression<Func<T, object>> orderBy) => _orderBy = orderBy;
            public void AddIncludeExpression(Expression<Func<T, object>> include) => _includes.Add(include);
            public void SetPaging(int skip, int take)
            {
                _skip = skip;
                _take = take;
            }

            public IQueryable<T> Evaluate(IQueryable<T> query)
            {
                query = query.Where(_criteria);

                foreach (var include in _includes)
                {
                    query = query.Include(include);
                }

                if (_orderBy != null)
                {
                    query = query.OrderBy(_orderBy);
                }

                if (_skip.HasValue && _take.HasValue)
                {
                    query = query.Skip(_skip.Value).Take(_take.Value);
                }

                return query;
            }

            public bool SatisfiedBy(T entity)
            {
                return _criteria.Compile()(entity);
            }

            public ISpecification<T> AndSpecification(ISpecification<T> specification)
            {
                var otherSpec = (TestSpecification<T>)specification;
                var newSpec = new TestSpecification<T>();
                newSpec.SetCriteria(CombineExpressions(_criteria, otherSpec.GetCriteria(), Expression.AndAlso));
                return newSpec;
            }

            public ISpecification<T> OrSpecification(ISpecification<T> specification)
            {
                var otherSpec = (TestSpecification<T>)specification;
                var newSpec = new TestSpecification<T>();
                newSpec.SetCriteria(CombineExpressions(_criteria, otherSpec.GetCriteria(), Expression.OrElse));
                return newSpec;
            }

            private static Expression<Func<T, bool>> CombineExpressions(
                Expression<Func<T, bool>> expr1,
                Expression<Func<T, bool>> expr2,
                Func<Expression, Expression, Expression> combiner)
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var visitor = new ParameterReplacer(parameter);

                var left = visitor.Visit(expr1.Body);
                var right = visitor.Visit(expr2.Body);
                var body = combiner(left, right);

                return Expression.Lambda<Func<T, bool>>(body, parameter);
            }

            private class ParameterReplacer : ExpressionVisitor
            {
                private readonly ParameterExpression _parameter;

                public ParameterReplacer(ParameterExpression parameter)
                {
                    _parameter = parameter;
                }

                protected override Expression VisitParameter(ParameterExpression node)
                {
                    return _parameter;
                }
            }
        }
    }
}
