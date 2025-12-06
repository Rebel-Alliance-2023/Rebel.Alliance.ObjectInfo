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
        }

        protected abstract void BuildWhereClause();

        public bool IsSatisfiedBy(T entity)
        {
            throw new NotImplementedException();
        }

        IDictionary<string, object> ISpecification<T>.GetParameters()
        {
            throw new NotImplementedException();
        }

        public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ISpecification<T> And(ISpecification<T> other)
        {
            throw new NotImplementedException();
        }

        public ISpecification<T> Or(ISpecification<T> other)
        {
            throw new NotImplementedException();
        }

        public ISpecification<T> Not()
        {
            throw new NotImplementedException();
        }
    }
}
