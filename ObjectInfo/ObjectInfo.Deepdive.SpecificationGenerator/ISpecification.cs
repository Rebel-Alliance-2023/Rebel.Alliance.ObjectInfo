using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime
{
    /// <summary>
    /// Represents a specification pattern implementation that can be used to define
    /// query specifications for both Entity Framework Core and Dapper.
    /// </summary>
    /// <typeparam name="T">The type of entity this specification queries</typeparam>
    public interface ISpecification<T> where T : class
    {
        /// <summary>
        /// Gets the filter expression representing this specification's criteria
        /// </summary>
        /// <returns>An expression tree representing the specification's criteria</returns>
        Expression<Func<T, bool>> Criteria { get; }

        /// <summary>
        /// Gets a collection of include expressions for eager loading related data
        /// </summary>
        IEnumerable<Expression<Func<T, object>>> Includes { get; }

        /// <summary>
        /// Gets a collection of string-based include paths for eager loading related data
        /// </summary>
        IEnumerable<string> IncludeStrings { get; }

        /// <summary>
        /// Gets the order by expression for the specification
        /// </summary>
        Expression<Func<T, object>>? OrderBy { get; }

        /// <summary>
        /// Gets the order by descending expression for the specification
        /// </summary>
        Expression<Func<T, object>>? OrderByDescending { get; }

        /// <summary>
        /// Gets additional order by expressions for secondary sorting
        /// </summary>
        IEnumerable<Expression<Func<T, object>>> ThenByExpressions { get; }

        /// <summary>
        /// Gets additional order by descending expressions for secondary sorting
        /// </summary>
        IEnumerable<Expression<Func<T, object>>> ThenByDescendingExpressions { get; }

        /// <summary>
        /// Gets the number of entities to skip
        /// </summary>
        int? Skip { get; }

        /// <summary>
        /// Gets the number of entities to take
        /// </summary>
        int? Take { get; }

        /// <summary>
        /// Gets a value indicating whether pagination is enabled
        /// </summary>
        bool IsPagingEnabled { get; }

        /// <summary>
        /// Gets any nested specifications for navigation properties
        /// </summary>
        IDictionary<string, ISpecification<object>> NestedSpecifications { get; }

        /// <summary>
        /// Evaluates whether the specification is satisfied by the given entity
        /// </summary>
        /// <param name="entity">The entity to test</param>
        /// <returns>True if the entity satisfies the specification, false otherwise</returns>
        bool IsSatisfiedBy(T entity);

        #region Dapper Support

        /// <summary>
        /// Gets the SQL query for Dapper implementations
        /// </summary>
        /// <returns>The SQL query string</returns>
        string ToSql();

        /// <summary>
        /// Gets the parameters for the SQL query
        /// </summary>
        /// <returns>Dictionary of parameter names and values</returns>
        IDictionary<string, object> GetParameters();

        /// <summary>
        /// Asynchronously gets the count of entities matching the specification
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Count of matching entities</returns>
        Task<int> GetCountAsync(CancellationToken cancellationToken = default);

        #endregion

        #region Expression Composition

        /// <summary>
        /// Combines this specification with another using AND logic
        /// </summary>
        /// <param name="other">The specification to combine with</param>
        /// <returns>A new specification representing the combination</returns>
        ISpecification<T> And(ISpecification<T> other);

        /// <summary>
        /// Combines this specification with another using OR logic
        /// </summary>
        /// <param name="other">The specification to combine with</param>
        /// <returns>A new specification representing the combination</returns>
        ISpecification<T> Or(ISpecification<T> other);

        /// <summary>
        /// Negates this specification
        /// </summary>
        /// <returns>A new specification representing the negation</returns>
        ISpecification<T> Not();

        #endregion
    }

    /// <summary>
    /// Extension methods for ISpecification interface
    /// </summary>
    public static class SpecificationExtensions
    {
        /// <summary>
        /// Evaluates a collection of entities against the specification
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="specification">The specification to evaluate</param>
        /// <param name="entities">The entities to test</param>
        /// <returns>Entities that satisfy the specification</returns>
        public static IEnumerable<T> SatisfyingEntitiesFrom<T>(
            this ISpecification<T> specification,
            IEnumerable<T> entities) where T : class
        {
            return entities.Where(specification.Criteria.Compile());
        }

        /// <summary>
        /// Creates a new specification that combines the current specification with a simple expression
        /// </summary>
        /// <typeparam name="T">The type of entity</typeparam>
        /// <param name="specification">The current specification</param>
        /// <param name="expression">The expression to add</param>
        /// <returns>A new combined specification</returns>
        public static ISpecification<T> AndExpression<T>(
            this ISpecification<T> specification,
            Expression<Func<T, bool>> expression) where T : class
        {
            var expressionSpec = new ExpressionSpecification<T>(expression);
            return specification.And(expressionSpec);
        }
    }

    /// <summary>
    /// A simple specification implementation that wraps an expression
    /// </summary>
    internal class ExpressionSpecification<T> : ISpecification<T> where T : class
    {
        private readonly Expression<Func<T, bool>> _expression;

        public ExpressionSpecification(Expression<Func<T, bool>> expression)
        {
            _expression = expression;
        }

        public Expression<Func<T, bool>> Criteria => _expression;
        public IEnumerable<Expression<Func<T, object>>> Includes => Array.Empty<Expression<Func<T, object>>>();
        public IEnumerable<string> IncludeStrings => Array.Empty<string>();
        public Expression<Func<T, object>>? OrderBy => null;
        public Expression<Func<T, object>>? OrderByDescending => null;
        public IEnumerable<Expression<Func<T, object>>> ThenByExpressions => Array.Empty<Expression<Func<T, object>>>();
        public IEnumerable<Expression<Func<T, object>>> ThenByDescendingExpressions => Array.Empty<Expression<Func<T, object>>>();
        public int? Skip => null;
        public int? Take => null;
        public bool IsPagingEnabled => false;
        public IDictionary<string, ISpecification<object>> NestedSpecifications => new Dictionary<string, ISpecification<object>>();

        public bool IsSatisfiedBy(T entity) => Criteria.Compile()(entity);

        public string ToSql() => throw new NotImplementedException();
        public IDictionary<string, object> GetParameters() => new Dictionary<string, object>();
        public Task<int> GetCountAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public ISpecification<T> And(ISpecification<T> other) => throw new NotImplementedException();
        public ISpecification<T> Or(ISpecification<T> other) => throw new NotImplementedException();
        public ISpecification<T> Not() => throw new NotImplementedException();
    }
}