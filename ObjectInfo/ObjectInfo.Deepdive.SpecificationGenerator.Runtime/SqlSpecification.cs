using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime
{
    internal enum SqlCombineOperation
    {
        And,
        Or
    }

    /// <summary>
    /// Base class for SQL-based specifications used with Dapper
    /// </summary>
    /// <typeparam name="T">The type of entity this specification targets</typeparam>
    public abstract class SqlSpecification<T> : ISpecification<T> where T : class
    {
        private readonly StringBuilder _whereBuilder = new();
        private readonly Dictionary<string, object> _parameters = new();
        private int _parameterIndex;

        // ISpecification implementation
        public Expression<Func<T, bool>> Criteria { get; protected set; } = x => true;
        public IEnumerable<Expression<Func<T, object>>> Includes => Enumerable.Empty<Expression<Func<T, object>>>();
        public IEnumerable<string> IncludeStrings => Enumerable.Empty<string>();
        public Expression<Func<T, object>>? OrderBy { get; protected set; }
        public Expression<Func<T, object>>? OrderByDescending { get; protected set; }
        public IEnumerable<Expression<Func<T, object>>> ThenByExpressions { get; } = new List<Expression<Func<T, object>>>();
        public IEnumerable<Expression<Func<T, object>>> ThenByDescendingExpressions { get; } = new List<Expression<Func<T, object>>>();
        public int? Skip { get; protected set; }
        public int? Take { get; protected set; }
        public IDictionary<string, ISpecification<object>> NestedSpecifications { get; } = new Dictionary<string, ISpecification<object>>();
        public bool IsPagingEnabled => Skip.HasValue && Take.HasValue;

        /// <summary>
        /// Gets the SQL query for this specification
        /// </summary>
        public virtual string ToSql()
        {
            BuildWhereClause();

            var sql = new StringBuilder(GetBaseQuery());

            if (_whereBuilder.Length > 0)
            {
                sql.Append(" WHERE ").Append(_whereBuilder);
            }

            if (IsPagingEnabled)
            {
                sql.Append($" OFFSET {Skip!.Value} ROWS");
                sql.Append($" FETCH NEXT {Take!.Value} ROWS ONLY");
            }

            return sql.ToString();
        }

        /// <summary>
        /// Gets the parameters for the SQL query
        /// </summary>
        public virtual IDictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>(_parameters);
        }

        public virtual bool IsSatisfiedBy(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Use Dapper's execution to evaluate against a single entity
            using var connection = new SqlConnection("Data Source=:memory:");
            connection.Open();

            var tempTableName = $"#{typeof(T).Name}_{Guid.NewGuid():N}";
            connection.Execute($"CREATE TABLE {tempTableName} AS SELECT * FROM @Entity", new { Entity = entity });

            try
            {
                var result = connection.QueryFirstOrDefault<bool>($"SELECT 1 FROM {tempTableName} WHERE {_whereBuilder}",
                    new DynamicParameters(GetParameters()));
                return result;
            }
            finally
            {
                connection.Execute($"DROP TABLE {tempTableName}");
            }
        }

        public virtual Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Use the extension method CountWithSpecificationAsync with a database connection instead.");
        }

        public virtual ISpecification<T> And(ISpecification<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (other is not SqlSpecification<T> sqlSpec)
                throw new ArgumentException("Can only combine with other SQL specifications", nameof(other));

            return new CombinedSqlSpecification<T>(this, sqlSpec, SqlCombineOperation.And);
        }

        public virtual ISpecification<T> Or(ISpecification<T> other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (other is not SqlSpecification<T> sqlSpec)
                throw new ArgumentException("Can only combine with other SQL specifications", nameof(other));

            return new CombinedSqlSpecification<T>(this, sqlSpec, SqlCombineOperation.Or);
        }

        public virtual ISpecification<T> Not()
        {
            return new NotSqlSpecification<T>(this);
        }

        /// <summary>
        /// Gets the base SQL query for the entity
        /// </summary>
        protected virtual string GetBaseQuery() => $"SELECT * FROM {GetTableName()}";

        /// <summary>
        /// Gets the table name for the entity
        /// </summary>
        protected virtual string GetTableName() => typeof(T).Name;

        /// <summary>
        /// Builds the WHERE clause for the query
        /// </summary>
        protected abstract void BuildWhereClause();

        /// <summary>
        /// Adds a WHERE clause to the query
        /// </summary>
        protected void AddWhereClause(string clause)
        {
            if (_whereBuilder.Length > 0)
            {
                _whereBuilder.Append(" AND ");
            }
            _whereBuilder.Append(clause);
        }

        /// <summary>
        /// Adds a parameterized WHERE clause to the query
        /// </summary>
        protected void AddParameterizedWhereClause(string clause, string parameterName, object value)
        {
            if (!string.IsNullOrEmpty(clause))
            {
                AddWhereClause(clause);
            }
            _parameters.Add(parameterName, value);
        }

        /// <summary>
        /// Gets a unique parameter name
        /// </summary>
        protected string GetUniqueParameterName(string baseName) => $"@{baseName}{_parameterIndex++}";

        // Extension methods for Dapper-specific functionality
        public virtual async Task<IEnumerable<T>> QueryAsync(
            IDbConnection connection,
            IDbTransaction? transaction = null,
            CancellationToken cancellationToken = default)
        {
            return await connection.QueryAsync<T>(
                ToSql(),
                new DynamicParameters(GetParameters()),
                transaction);
        }

        public virtual async Task<T?> FirstOrDefaultAsync(
            IDbConnection connection,
            IDbTransaction? transaction = null,
            CancellationToken cancellationToken = default)
        {
            return await connection.QueryFirstOrDefaultAsync<T>(
                ToSql(),
                new DynamicParameters(GetParameters()),
                transaction);
        }

        public virtual async Task<int> GetCountAsync(
            IDbConnection connection,
            IDbTransaction? transaction = null,
            CancellationToken cancellationToken = default)
        {
            BuildWhereClause();

            var sql = $"SELECT COUNT(*) FROM {GetTableName()}";
            if (_whereBuilder.Length > 0)
            {
                sql += $" WHERE {_whereBuilder}";
            }

            return await connection.ExecuteScalarAsync<int>(
                sql,
                new DynamicParameters(GetParameters()),
                transaction);
        }
    }

    internal class NotSqlSpecification<T> : SqlSpecification<T> where T : class
    {
        private readonly SqlSpecification<T> _original;

        public NotSqlSpecification(SqlSpecification<T> original)
        {
            _original = original ?? throw new ArgumentNullException(nameof(original));
        }

        protected override void BuildWhereClause()
        {
            // Get the SQL from the original specification
            var originalSql = _original.ToSql();

            // Extract the WHERE clause
            var whereIndex = originalSql.IndexOf(" WHERE ", StringComparison.OrdinalIgnoreCase);
            if (whereIndex >= 0)
            {
                var whereClause = originalSql.Substring(whereIndex + 7);
                AddWhereClause($"NOT ({whereClause})");
            }

            // Copy parameters from original specification
            foreach (var param in _original.GetParameters())
            {
                AddParameterizedWhereClause(string.Empty, param.Key, param.Value);
            }
        }
    }

    internal class CombinedSqlSpecification<T> : SqlSpecification<T> where T : class
    {
        private readonly SqlSpecification<T> _left;
        private readonly SqlSpecification<T> _right;
        private readonly SqlCombineOperation _operation;

        public CombinedSqlSpecification(
            SqlSpecification<T> left,
            SqlSpecification<T> right,
            SqlCombineOperation operation)
        {
            _left = left ?? throw new ArgumentNullException(nameof(left));
            _right = right ?? throw new ArgumentNullException(nameof(right));
            _operation = operation;
        }

        protected override void BuildWhereClause()
        {
            var leftSql = _left.ToSql();
            var rightSql = _right.ToSql();

            // Extract WHERE clauses
            var leftWhere = ExtractWhereClause(leftSql);
            var rightWhere = ExtractWhereClause(rightSql);

            if (!string.IsNullOrEmpty(leftWhere) || !string.IsNullOrEmpty(rightWhere))
            {
                var op = _operation == SqlCombineOperation.And ? "AND" : "OR";
                AddWhereClause($"({leftWhere} {op} {rightWhere})");
            }

            // Merge parameters from both specifications
            foreach (var param in _left.GetParameters())
            {
                AddParameterizedWhereClause(string.Empty, param.Key, param.Value);
            }
            foreach (var param in _right.GetParameters())
            {
                AddParameterizedWhereClause(string.Empty, param.Key, param.Value);
            }
        }

        private string ExtractWhereClause(string sql)
        {
            var whereIndex = sql.IndexOf(" WHERE ", StringComparison.OrdinalIgnoreCase);
            return whereIndex >= 0 ? sql.Substring(whereIndex + 7) : string.Empty;
        }
    }
}
