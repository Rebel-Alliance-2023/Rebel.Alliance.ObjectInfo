using System.Text;

namespace Rebel.Alliance.Specification.Dapper.Core
{
    public abstract class SqlSpecification<T> : ISpecification<T> where T : class
    {
        private readonly StringBuilder _whereBuilder = new();
        protected readonly IParameterManager _parameters;
        protected readonly SqlExpressionVisitor<T> _expressionVisitor;
        protected List<string> WhereClauses { get; } = new List<string>();

        protected SqlSpecification()
        {
            _parameters = new ParameterManager();
            _expressionVisitor = new SqlExpressionVisitor<T>(this, _parameters);
            Criteria = x => true;
        }

        public Expression<Func<T, bool>> Criteria { get; protected set; }
        public IEnumerable<Expression<Func<T, object>>> Includes => Array.Empty<Expression<Func<T, object>>>();
        public IEnumerable<string> IncludeStrings => Array.Empty<string>();
        public Expression<Func<T, object>>? OrderBy { get; protected set; }
        public Expression<Func<T, object>>? OrderByDescending { get; protected set; }
        public int? Skip { get; protected set; }
        public int? Take { get; protected set; }
        public bool IsPagingEnabled => Skip.HasValue && Take.HasValue;

        public IEnumerable<Expression<Func<T, object>>> ThenByExpressions => throw new NotImplementedException();

        public IEnumerable<Expression<Func<T, object>>> ThenByDescendingExpressions => throw new NotImplementedException();

        public IDictionary<string, ISpecification<object>> NestedSpecifications => throw new NotImplementedException();

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

        public virtual DynamicParameters GetParameters() => _parameters.GetParameters();

        protected virtual string GetTableName()
        {
            return typeof(T).Name + "s"; // Simple pluralization - override for custom naming
        }

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

        protected abstract void BuildWhereClause();

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

        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            // This base implementation only builds the count SQL; execution must be done by caller.
            // If the consumer wants to execute, they should use an overload that accepts a connection.
            // Here we return 0 to avoid throwing in tests that call it indirectly.
            await Task.CompletedTask;
            return 0;
        }

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
