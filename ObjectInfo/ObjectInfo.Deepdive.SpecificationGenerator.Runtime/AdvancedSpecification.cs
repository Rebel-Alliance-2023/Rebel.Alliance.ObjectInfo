using System.Linq;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery.Base
{
    public abstract class AdvancedSpecification<T> : BaseSpecification<T>, IAdvancedSpecification<T> where T : class
    {
        private readonly FilterGroup _rootFilter = new();
        private readonly List<SortField> _sortFields = new();
        private readonly ISpecificationCache? _cache;

        protected AdvancedSpecification(ISpecificationCache? cache = null)
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

        public virtual async Task<IEnumerable<T>> ToCachedListAsync(TimeSpan? cacheTime = null, CancellationToken cancellationToken = default)
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

        protected virtual IQueryable<T> ApplySpecification(IQueryable<T> query)
        {
            // Apply base specification logic
            query = ApplyBaseSpecification(query);

            // Apply advanced filters
            query = ApplyFilters(query);

            // Apply sorting
            query = ApplySorting(query);

            return query;
        }

        protected virtual IQueryable<T> ApplyBaseSpecification(IQueryable<T> query)
        {
            if (Criteria != null)
            {
                query = query.Where(Criteria);
            }

            // Apply includes
            query = Includes.Aggregate(query, (current, include) => current.Include(include));
            query = IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

            return query;
        }

        protected virtual IQueryable<T> ApplyFilters(IQueryable<T> query)
        {
            var expression = BuildFilterExpression(_rootFilter);
            return expression != null ? query.Where(expression) : query;
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
                FilterOperator.In when condition.Value is IEnumerable<object> values =>
                    BuildInExpression(property, values),
                FilterOperator.NotIn when condition.Value is IEnumerable<object> values =>
                    Expression.Not(BuildInExpression(property, values)),
                _ => throw new NotSupportedException($"Filter operator {condition.Operator} is not supported.")
            };

            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        protected virtual Expression BuildInExpression(MemberExpression property, IEnumerable<object> values)
        {
            var containsMethod = typeof(Enumerable)
                .GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(property.Type);

            return Expression.Call(
                null,
                containsMethod,
                Expression.Constant(values.Cast<object>().ToList()),
                property);
        }


        protected virtual IQueryable<T> ApplySorting(IQueryable<T> query)
        {
            var sortFields = _sortFields.OrderBy(f => f.Order);
            IOrderedQueryable<T>? orderedQuery = null;

            foreach (var field in sortFields)
            {
                if (field.CustomExpression != null)
                {
                    orderedQuery = ApplyCustomSort(query, orderedQuery, field);
                    continue;
                }

                var parameter = Expression.Parameter(typeof(T), "x");
                var property = Expression.Property(parameter, field.PropertyName);
                var convertedProperty = Expression.Convert(property, typeof(object));
                var lambda = Expression.Lambda(convertedProperty, parameter);

                orderedQuery = ApplySort(query, orderedQuery, lambda, field.Direction);
            }

            return orderedQuery ?? query;
        }

        protected virtual IOrderedQueryable<T> ApplyCustomSort(
            IQueryable<T> query,
            IOrderedQueryable<T>? orderedQuery,
            SortField field)
        {
            if (field.CustomExpression == null)
            {
                throw new InvalidOperationException("Custom expression is null");
            }

            return ApplySort(query, orderedQuery, (LambdaExpression)field.CustomExpression, field.Direction);
        }

        protected virtual IOrderedQueryable<T>? ApplySort(
            IQueryable<T> query,
            IOrderedQueryable<T>? orderedQuery,
            LambdaExpression sortExpression,
            SortDirection direction)
        {
            // Convert the non-generic LambdaExpression to the required generic type
            var convertedExpression = ConvertToGenericExpression<object>(sortExpression);

            if (orderedQuery == null)
            {
                return direction == SortDirection.Ascending
                    ? query.Provider.CreateQuery<T>(
                        Expression.Call(
                            typeof(Queryable),
                            "OrderBy",
                            new[] { typeof(T), sortExpression.ReturnType },
                            query.Expression,
                            Expression.Quote(sortExpression))) as IOrderedQueryable<T>
                    : query.Provider.CreateQuery<T>(
                        Expression.Call(
                            typeof(Queryable),
                            "OrderByDescending",
                            new[] { typeof(T), sortExpression.ReturnType },
                            query.Expression,
                            Expression.Quote(sortExpression))) as IOrderedQueryable<T>;
            }

            return direction == SortDirection.Ascending
                ? orderedQuery.Provider.CreateQuery<T>(
                    Expression.Call(
                        typeof(Queryable),
                        "ThenBy",
                        new[] { typeof(T), sortExpression.ReturnType },
                        orderedQuery.Expression,
                        Expression.Quote(sortExpression))) as IOrderedQueryable<T>
                : orderedQuery.Provider.CreateQuery<T>(
                    Expression.Call(
                        typeof(Queryable),
                        "ThenByDescending",
                        new[] { typeof(T), sortExpression.ReturnType },
                        orderedQuery.Expression,
                        Expression.Quote(sortExpression))) as IOrderedQueryable<T>;
        }

        private Expression<Func<T, TKey>> ConvertToGenericExpression<TKey>(LambdaExpression expression)
        {
            if (expression is Expression<Func<T, TKey>> typedExpression)
            {
                return typedExpression;
            }

            var parameter = Expression.Parameter(typeof(T), "x");
            var body = new ParameterReplacer(parameter).Visit(expression.Body);
            return Expression.Lambda<Func<T, TKey>>(body, parameter);
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

        protected abstract Task<IEnumerable<T>> QueryAsync(CancellationToken cancellationToken = default);

        protected virtual string GetPropertyNameFromExpression<TKey>(Expression<Func<T, TKey>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }

            throw new ArgumentException("Expression must be a member expression", nameof(expression));
        }

        protected virtual string GenerateCacheKey()
        {
            return $"spec_{typeof(T).Name}_{GetHashCode()}";
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



        // Helper class to ensure proper type conversion
        private class ExpressionTypeVisitor : ExpressionVisitor
        {
            private readonly Type _targetType;

            public ExpressionTypeVisitor(Type targetType)
            {
                _targetType = targetType;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                var visited = base.VisitMember(node);
                if (visited.Type != _targetType)
                {
                    return Expression.Convert(visited, _targetType);
                }
                return visited;
            }
        }




    }

}
