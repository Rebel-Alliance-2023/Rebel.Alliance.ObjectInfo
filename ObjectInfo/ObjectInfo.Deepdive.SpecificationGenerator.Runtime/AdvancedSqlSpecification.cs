using System;
using System.Collections.Generic;
using System.Text;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery.Models;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery.Base
{
    /// <summary>
    /// Base implementation for SQL-based advanced specifications
    /// </summary>
    public abstract class AdvancedSqlSpecification<T> : SqlSpecification<T>, IAdvancedSpecification<T> where T : class
    {
        private readonly FilterGroup _rootFilter = new();
        private readonly List<SortField> _sortFields = new();
        private readonly ISpecificationCache? _cache;
        private readonly StringBuilder _orderByBuilder = new();

        protected AdvancedSqlSpecification(ISpecificationCache? cache = null)
        {
            _cache = cache;
        }

        public FilterGroup RootFilter => _rootFilter;
        public IList<SortField> SortFields => _sortFields.AsReadOnly();

        public IAdvancedSpecification<T> AddSort(string propertyName, SortDirection direction = SortDirection.Ascending, int order = 0)
        {
            _sortFields.Add(new SortField(propertyName, direction, order));
            return this;
        }

        public IAdvancedSpecification<T> AddSort<TKey>(Expression<Func<T, TKey>> expression, SortDirection direction = SortDirection.Ascending, int order = 0)
        {
            var propertyName = GetPropertyNameFromExpression(expression);
            return AddSort(propertyName, direction, order);
        }

        public IAdvancedSpecification<T> ClearSort()
        {
            _sortFields.Clear();
            _orderByBuilder.Clear();
            return this;
        }

        public IAdvancedSpecification<T> Where(Action<FilterGroup> filterConfig)
        {
            filterConfig(_rootFilter);
            return this;
        }

        public IAdvancedSpecification<T> OrWhere(Action<FilterGroup> filterConfig)
        {
            var group = new FilterGroup { Operator = LogicalOperator.Or };
            filterConfig(group);
            _rootFilter.Groups.Add(group);
            return this;
        }

        public IAdvancedSpecification<T> ClearFilters()
        {
            _rootFilter.Conditions.Clear();
            _rootFilter.Groups.Clear();
            return this;
        }

        public async Task<IEnumerable<T>> ToCachedListAsync(TimeSpan? cacheTime = null, CancellationToken cancellationToken = default)
        {
            if (_cache == null)
            {
                throw new InvalidOperationException("Cache service not configured for this specification.");
            }

            var cacheKey = GenerateCacheKey();
            return await _cache.GetOrSetAsync(
                cacheKey,
                () => QueryAsync(cancellationToken),
                cacheTime,
                cancellationToken);
        }

        protected override void BuildWhereClause()
        {
            BuildFilterClause(_rootFilter);
        }

        public override string ToSql()
        {
            var sql = base.ToSql();
            var orderBy = BuildOrderByClause();
            
            if (!string.IsNullOrEmpty(orderBy))
            {
                sql += $" ORDER BY {orderBy}";
            }

            return sql;
        }

        protected virtual void BuildFilterClause(FilterGroup group, string prefix = "")
        {
            if (!group.Conditions.Any() && !group.Groups.Any()) return;

            if (!string.IsNullOrEmpty(prefix))
            {
                AddWhereClause($"({group.Operator} ");
            }

            foreach (var condition in group.Conditions)
            {
                BuildConditionClause(condition, group.Operator);
            }

            foreach (var nestedGroup in group.Groups)
            {
                BuildFilterClause(nestedGroup, group.Operator.ToString());
            }

            if (!string.IsNullOrEmpty(prefix))
            {
                AddWhereClause(")");
            }
        }

        protected virtual void BuildConditionClause(FilterCondition condition, LogicalOperator op)
        {
            var parameterName = GetUniqueParameterName(condition.PropertyName);
            var sqlOperator = GetSqlOperator(condition.Operator);

            var clause = condition.Operator switch
            {
                FilterOperator.IsNull => $"[{condition.PropertyName}] IS NULL",
                FilterOperator.IsNotNull => $"[{condition.PropertyName}] IS NOT NULL",
                FilterOperator.Contains => $"[{condition.PropertyName}] LIKE '%' + @{parameterName} + '%'",
                FilterOperator.StartsWith => $"[{condition.PropertyName}] LIKE @{parameterName} + '%'",
                FilterOperator.EndsWith => $"[{condition.PropertyName}] LIKE '%' + @{parameterName}",
                _ => $"[{condition.PropertyName}] {sqlOperator} @{parameterName}"
            };

            AddWhereClause(clause);
            if (condition.Value != null)
            {
                AddParameter(parameterName, condition.Value);
            }
        }

        protected virtual string BuildOrderByClause()
        {
            if (!_sortFields.Any()) return string.Empty;

            var sortFields = _sortFields.OrderBy(f => f.Order);
            var orderByParts = new List<string>();

            foreach (var field in sortFields)
            {
                var direction = field.Direction == SortDirection.Ascending ? "ASC" : "DESC";
                orderByParts.Add($"[{field.PropertyName}] {direction}");
            }

            return string.Join(", ", orderByParts);
        }

        protected virtual Expression<Func<T, bool>> BuildFilterExpression(FilterGroup group)
        {
            if (!group.Conditions.Any() && !group.Groups.Any())
                return x => true;

            Expression<Func<T, bool>>? expression = null;

            // Process conditions
            foreach (var condition in group.Conditions)
            {
                var conditionExpression = BuildConditionExpression(condition);
                expression = CombineExpressions(expression, conditionExpression, group.Operator);
            }

            // Process nested groups
            foreach (var nestedGroup in group.Groups)
            {
                var nestedExpression = BuildFilterExpression(nestedGroup);
                expression = CombineExpressions(expression, nestedExpression, group.Operator);
            }

            return expression ?? (x => true);
        }

        protected virtual Expression<Func<T, bool>> BuildConditionExpression(FilterCondition condition)
        {
            if (condition.CustomExpression != null && condition.CustomExpression is Expression<Func<T, bool>> typedExpression)
            {
                return typedExpression;
            }

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, condition.PropertyName);
            var value = Expression.Constant(condition.Value, property.Type);

            Expression body = condition.Operator switch
            {
                FilterOperator.Equals => Expression.Equal(property, value),
                FilterOperator.NotEquals => Expression.NotEqual(property, value),
                FilterOperator.GreaterThan => Expression.GreaterThan(property, value),
                FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, value),
                FilterOperator.LessThan => Expression.LessThan(property, value),
                FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(property, value),
                FilterOperator.Contains when property.Type == typeof(string) =>
                    Expression.Call(property, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, value),
                FilterOperator.StartsWith when property.Type == typeof(string) =>
                    Expression.Call(property, typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!, value),
                FilterOperator.EndsWith when property.Type == typeof(string) =>
                    Expression.Call(property, typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!, value),
                FilterOperator.IsNull => Expression.Equal(property, Expression.Constant(null, property.Type)),
                FilterOperator.IsNotNull => Expression.NotEqual(property, Expression.Constant(null, property.Type)),
                _ => throw new NotSupportedException($"Filter operator {condition.Operator} is not supported.")
            };

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        protected virtual Expression<Func<T, bool>> CombineExpressions(
            Expression<Func<T, bool>>? expr1,
            Expression<Func<T, bool>> expr2,
            LogicalOperator op)
        {
            if (expr1 == null) return expr2;

            var parameter = Expression.Parameter(typeof(T), "x");
            var visitor = new ParameterReplacer(parameter);

            var left = visitor.Visit(expr1.Body);
            var right = visitor.Visit(expr2.Body);

            var body = op == LogicalOperator.And
                ? Expression.AndAlso(left, right)
                : Expression.OrElse(left, right);

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


        private string GetSqlOperator(FilterOperator op) => op switch
        {
            FilterOperator.Equals => "=",
            FilterOperator.NotEquals => "<>",
            FilterOperator.GreaterThan => ">",
            FilterOperator.GreaterThanOrEqual => ">=",
            FilterOperator.LessThan => "<",
            FilterOperator.LessThanOrEqual => "<=",
            _ => "="
        };

        protected virtual string GenerateCacheKey()
        {
            return $"sql_spec_{typeof(T).Name}_{GetHashCode()}";
        }

        private void AddParameter(string name, object value)
        {
            Parameters[name] = value;
        }

        protected virtual string GetPropertyNameFromExpression<TKey>(Expression<Func<T, TKey>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }

            throw new ArgumentException("Expression must be a member expression", nameof(expression));
        }

        private IDictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        protected virtual string GetUniqueParameterName(string baseParameterName)
        {
            return $"{baseParameterName}_{Parameters.Count + 1}";
        }

        /// <summary>
        /// Executes the query and returns the results
        /// </summary>
        protected abstract Task<IEnumerable<T>> QueryAsync(CancellationToken cancellationToken = default);
    }
}
