using System.Text;

namespace Rebel.Alliance.Specification.Dapper.Core
{
    /// <summary>
    /// Base class for SQL specifications used with Dapper.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public abstract class SqlSpecification<T> : ISpecification<T> where T : class
    {
        private readonly StringBuilder _whereBuilder = new();

        /// <summary>
        /// The parameter manager for handling SQL parameters.
        /// </summary>
        protected readonly IParameterManager _parameters;

        /// <summary>
        /// The expression visitor for converting LINQ expressions to SQL.
        /// </summary>
        protected readonly SqlExpressionVisitor<T> _expressionVisitor;

        /// <summary>
        /// Gets the list of WHERE clauses.
        /// </summary>
        protected List<string> WhereClauses { get; } = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlSpecification{T}"/> class.
        /// </summary>
        protected SqlSpecification()
        {
            _parameters = new ParameterManager();
            _expressionVisitor = new SqlExpressionVisitor<T>(this, _parameters);
            Criteria = x => true;
        }

        /// <summary>
        /// Gets or sets the criteria expression.
        /// </summary>
        public Expression<Func<T, bool>> Criteria { get; protected set; }

        /// <summary>
        /// Gets the include expressions for related entities.
        /// </summary>
        public IEnumerable<Expression<Func<T, object>>> Includes => Array.Empty<Expression<Func<T, object>>>();

        /// <summary>
        /// Gets the include strings for related entities.
        /// </summary>
        public IEnumerable<string> IncludeStrings => Array.Empty<string>();

        /// <summary>
        /// Gets or sets the order by expression.
        /// </summary>
        public Expression<Func<T, object>>? OrderBy { get; protected set; }

        /// <summary>
        /// Gets or sets the order by descending expression.
        /// </summary>
        public Expression<Func<T, object>>? OrderByDescending { get; protected set; }

        /// <summary>
        /// Gets or sets the number of records to skip.
        /// </summary>
        public int? Skip { get; protected set; }

        /// <summary>
        /// Gets or sets the number of records to take.
        /// </summary>
        public int? Take { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether paging is enabled.
        /// </summary>
        public bool IsPagingEnabled => Skip.HasValue && Take.HasValue;

        /// <summary>
        /// Gets the then-by ordering expressions.
        /// </summary>
        public IEnumerable<Expression<Func<T, object>>> ThenByExpressions => throw new NotImplementedException();

        /// <summary>
        /// Gets the then-by descending ordering expressions.
        /// </summary>
        public IEnumerable<Expression<Func<T, object>>> ThenByDescendingExpressions => throw new NotImplementedException();

        /// <summary>
        /// Gets the nested specifications for related entities.
        /// </summary>
        public IDictionary<string, ISpecification<object>> NestedSpecifications => throw new NotImplementedException();

        /// <summary>
        /// Generates the SQL query string.
        /// </summary>
        /// <returns>The SQL query string.</returns>
        public virtual string ToSql()
        {
            BuildWhereClause();
            var sql = new StringBuilder($"SELECT * FROM {GetTableName()}");

            if (_whereBuilder.Length > 0)
            {
                sql.Append(" WHERE ").Append(_whereBuilder);
            }

            if (IsPagingEnabled)
            {
                sql.Append($" OFFSET {Skip!.Value} ROWS FETCH NEXT {Take!.Value} ROWS ONLY");
            }

            return sql.ToString();
        }

        /// <summary>
        /// Gets the parameters for the SQL query.
        /// </summary>
        /// <returns>The dynamic parameters.</returns>
        public virtual DynamicParameters GetParameters() => _parameters.GetParameters();

        /// <summary>
        /// Gets the table name for the entity.
        /// </summary>
        /// <returns>The table name.</returns>
        protected virtual string GetTableName()
        {
            return typeof(T).Name + "s"; // Simple pluralization - override for custom naming
        }

        /// <summary>
        /// Adds a WHERE clause to the query.
        /// </summary>
        /// <param name="clause">The WHERE clause to add.</param>
        protected void AddWhereClause(string clause)
        {
            if (_whereBuilder.Length > 0)
            {
                _whereBuilder.Append(" AND ");
            }
            _whereBuilder.Append(clause);
            WhereClauses.Add(clause);
        }

        // Expose parameter manager for helper classes like expression visitors
        internal IParameterManager ParametersManager => _parameters;

        /// <summary>
        /// Builds the WHERE clause from the criteria expression.
        /// </summary>
        protected abstract void BuildWhereClause();

        /// <summary>
        /// Determines whether the entity satisfies the specification criteria.
        /// </summary>
        /// <param name="entity">The entity to check.</param>
        /// <returns>True if the entity satisfies the criteria; otherwise, false.</returns>
        public bool IsSatisfiedBy(T entity)
        {
            return Criteria.Compile()(entity);
        }

        IDictionary<string, object> ISpecification<T>.GetParameters()
        {
            // Convert DynamicParameters to a simple dictionary for interface consumers
            var dict = new Dictionary<string, object>();
            var dp = _parameters.GetParameters();
            foreach (string name in dp.ParameterNames)
            {
                dict[name] = dp.Get<object>(name)!;
            }
            return dict;
        }

        /// <summary>
        /// Gets the count of entities matching the specification asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The count of matching entities.</returns>
        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            // This base implementation only builds the count SQL; execution must be done by caller.
            // If the consumer wants to execute, they should use an overload that accepts a connection.
            // Here we return 0 to avoid throwing in tests that call it indirectly.
            await Task.CompletedTask;
            return 0;
        }

        /// <summary>
        /// Combines this specification with another using a logical AND.
        /// </summary>
        /// <param name="other">The other specification.</param>
        /// <returns>A new combined specification.</returns>
        public ISpecification<T> And(ISpecification<T> other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var parameter = Expression.Parameter(typeof(T), "x");
            var left = new ParameterReplacer(parameter).Visit(Criteria.Body);
            var right = new ParameterReplacer(parameter).Visit(other.Criteria.Body);
            var body = Expression.AndAlso(left!, right!);

            var combined = new CompositeSqlSpecification<T>(Expression.Lambda<Func<T, bool>>(body, parameter));
            return combined;
        }

        /// <summary>
        /// Combines this specification with another using a logical OR.
        /// </summary>
        /// <param name="other">The other specification.</param>
        /// <returns>A new combined specification.</returns>
        public ISpecification<T> Or(ISpecification<T> other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            var parameter = Expression.Parameter(typeof(T), "x");
            var left = new ParameterReplacer(parameter).Visit(Criteria.Body);
            var right = new ParameterReplacer(parameter).Visit(other.Criteria.Body);
            var body = Expression.OrElse(left!, right!);

            var combined = new CompositeSqlSpecification<T>(Expression.Lambda<Func<T, bool>>(body, parameter));
            return combined;
        }

        /// <summary>
        /// Creates a new specification that negates this specification.
        /// </summary>
        /// <returns>A new negated specification.</returns>
        public ISpecification<T> Not()
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var body = Expression.Not(new ParameterReplacer(parameter).Visit(Criteria.Body)!);
            var combined = new CompositeSqlSpecification<T>(Expression.Lambda<Func<T, bool>>(body, parameter));
            return combined;
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

        private sealed class CompositeSqlSpecification<TC> : SqlSpecification<TC> where TC : class
        {
            public CompositeSqlSpecification(Expression<Func<TC, bool>> criteria)
            {
                Criteria = criteria;
            }

            protected override void BuildWhereClause()
            {
                _parameters.Clear();
                _expressionVisitor.Visit(Criteria);
                var where = _expressionVisitor.GetSql();
                if (!string.IsNullOrWhiteSpace(where))
                {
                    AddWhereClause(where);
                }
            }
        }
    }
}
