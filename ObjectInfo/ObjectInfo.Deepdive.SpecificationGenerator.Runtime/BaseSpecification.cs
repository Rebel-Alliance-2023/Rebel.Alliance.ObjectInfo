using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime
{
    /// <summary>
    /// Base class for specifications that provides core implementation and builder pattern support
    /// </summary>
    /// <typeparam name="T">The type of entity this specification targets</typeparam>
    public abstract class BaseSpecification<T> : ISpecification<T> where T : class
    {
        private readonly List<Expression<Func<T, object>>> _includes = new();
        private readonly List<string> _includeStrings = new();
        private readonly List<Expression<Func<T, object>>> _thenByExpressions = new();
        private readonly List<Expression<Func<T, object>>> _thenByDescendingExpressions = new();
        private readonly Dictionary<string, ISpecification<object>> _nestedSpecifications = new();

        protected BaseSpecification()
        {
            Criteria = DefaultExpression;
        }

        protected BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }

        private static readonly Expression<Func<T, bool>> DefaultExpression = x => true;

        public virtual Expression<Func<T, bool>> Criteria { get; protected set; }
        public virtual Expression<Func<T, object>>? OrderBy { get; protected set; }
        public virtual Expression<Func<T, object>>? OrderByDescending { get; protected set; }

        public virtual IEnumerable<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();
        public virtual IEnumerable<string> IncludeStrings => _includeStrings.AsReadOnly();
        public virtual IEnumerable<Expression<Func<T, object>>> ThenByExpressions => _thenByExpressions.AsReadOnly();
        public virtual IEnumerable<Expression<Func<T, object>>> ThenByDescendingExpressions => _thenByDescendingExpressions.AsReadOnly();
        public virtual IDictionary<string, ISpecification<object>> NestedSpecifications => _nestedSpecifications;

        public virtual int? Skip { get; protected set; }
        public virtual int? Take { get; protected set; }
        public virtual bool IsPagingEnabled => Skip.HasValue && Take.HasValue;

        protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            _includes.Add(includeExpression);
        }

        protected virtual void AddInclude(string includeString)
        {
            _includeStrings.Add(includeString);
        }

        protected virtual void AddNestedSpecification<TProperty>(
            Expression<Func<T, TProperty>> propertySelector,
            ISpecification<TProperty> specification) where TProperty : class
        {
            var memberExpression = propertySelector.Body as MemberExpression
                ?? throw new ArgumentException("Property selector must be a member expression", nameof(propertySelector));

            _nestedSpecifications[memberExpression.Member.Name] = specification as ISpecification<object>
                ?? throw new ArgumentException("Invalid specification type", nameof(specification));
        }

        protected virtual void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
        }

        protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
        {
            OrderBy = orderByExpression;
        }

        protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
        {
            OrderByDescending = orderByDescendingExpression;
        }

        protected virtual void AddThenBy(Expression<Func<T, object>> thenByExpression)
        {
            _thenByExpressions.Add(thenByExpression);
        }

        protected virtual void AddThenByDescending(Expression<Func<T, object>> thenByDescendingExpression)
        {
            _thenByDescendingExpressions.Add(thenByDescendingExpression);
        }

        public virtual bool IsSatisfiedBy(T entity)
        {
            return Criteria.Compile()(entity);
        }

        public virtual string ToSql()
        {
            var sqlBuilder = new SqlBuilder();
            BuildSql(sqlBuilder);
            return sqlBuilder.ToString();
        }

        public virtual IDictionary<string, object> GetParameters()
        {
            var parameters = new Dictionary<string, object>();
            BuildParameters(parameters);
            return parameters;
        }

        public virtual Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Count implementation must be provided by the concrete specification or ORM implementation");
        }

        public virtual ISpecification<T> And(ISpecification<T> other)
        {
            var compositeSpec = new CompositeSpecification<T>(this, other, ExpressionComposition.And);
            return compositeSpec;
        }

        public virtual ISpecification<T> Or(ISpecification<T> other)
        {
            var compositeSpec = new CompositeSpecification<T>(this, other, ExpressionComposition.Or);
            return compositeSpec;
        }

        public virtual ISpecification<T> Not()
        {
            var notSpec = new NotSpecification<T>(this);
            return notSpec;
        }

        protected virtual void BuildSql(SqlBuilder builder)
        {
            // Default implementation for simple WHERE clause
            builder.Where(TranslateExpressionToSql(Criteria));

            if (OrderBy != null)
            {
                builder.OrderBy(GetPropertyNameFromExpression(OrderBy));
            }
            else if (OrderByDescending != null)
            {
                builder.OrderByDescending(GetPropertyNameFromExpression(OrderByDescending));
            }

            if (IsPagingEnabled)
            {
                builder.Offset(Skip!.Value).Limit(Take!.Value);
            }
        }

        protected virtual void BuildParameters(IDictionary<string, object> parameters)
        {
            // Implementation will be provided by concrete specifications
        }

        protected virtual string TranslateExpressionToSql(Expression expression)
        {
            // Basic SQL translation - concrete specifications should override for specific needs
            return new SqlExpressionTranslator().Translate(expression);
        }

        private static string GetPropertyNameFromExpression<TProp>(Expression<Func<T, TProp>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }

            throw new ArgumentException("Expression must be a member expression", nameof(expression));
        }
    }

    public class SqlBuilder
    {
        private readonly List<string> _whereConditions = new();
        private readonly List<string> _orderByColumns = new();
        private int? _offset;
        private int? _limit;

        public SqlBuilder Where(string condition)
        {
            _whereConditions.Add(condition);
            return this;
        }

        public SqlBuilder OrderBy(string column)
        {
            _orderByColumns.Add($"{column} ASC");
            return this;
        }

        public SqlBuilder OrderByDescending(string column)
        {
            _orderByColumns.Add($"{column} DESC");
            return this;
        }

        public SqlBuilder Offset(int offset)
        {
            _offset = offset;
            return this;
        }

        public SqlBuilder Limit(int limit)
        {
            _limit = limit;
            return this;
        }

        public override string ToString()
        {
            var sql = new List<string>();

            if (_whereConditions.Any())
            {
                sql.Add($"WHERE {string.Join(" AND ", _whereConditions)}");
            }

            if (_orderByColumns.Any())
            {
                sql.Add($"ORDER BY {string.Join(", ", _orderByColumns)}");
            }

            if (_limit.HasValue)
            {
                if (_offset.HasValue)
                {
                    sql.Add($"OFFSET {_offset.Value} ROWS FETCH NEXT {_limit.Value} ROWS ONLY");
                }
                else
                {
                    sql.Add($"LIMIT {_limit.Value}");
                }
            }

            return string.Join(" ", sql);
        }
    }

    internal class SqlExpressionTranslator
    {
        public string Translate(Expression expression)
        {
            // Basic implementation - to be expanded based on needs
            return "1=1";
        }
    }

    internal enum ExpressionComposition
    {
        And,
        Or
    }

    internal class CompositeSpecification<T> : BaseSpecification<T> where T : class
    {
        private readonly ISpecification<T> _left;
        private readonly ISpecification<T> _right;
        private readonly ExpressionComposition _composition;

        public CompositeSpecification(
            ISpecification<T> left,
            ISpecification<T> right,
            ExpressionComposition composition)
        {
            _left = left;
            _right = right;
            _composition = composition;
            BuildCompositeCriteria();
        }

        private void BuildCompositeCriteria()
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var leftExpr = new ParameterReplacer(parameter).Visit(_left.Criteria.Body);
            var rightExpr = new ParameterReplacer(parameter).Visit(_right.Criteria.Body);

            Expression body = _composition switch
            {
                ExpressionComposition.And => Expression.AndAlso(leftExpr, rightExpr),
                ExpressionComposition.Or => Expression.OrElse(leftExpr, rightExpr),
                _ => throw new ArgumentOutOfRangeException()
            };

            Criteria = Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }

    internal class NotSpecification<T> : BaseSpecification<T> where T : class
    {
        public NotSpecification(ISpecification<T> specification)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var body = Expression.Not(new ParameterReplacer(parameter).Visit(specification.Criteria.Body));
            Criteria = Expression.Lambda<Func<T, bool>>(body, parameter);
        }
    }

    internal class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;

        protected override Expression VisitParameter(ParameterExpression node)
            => base.VisitParameter(_parameter);

        internal ParameterReplacer(ParameterExpression parameter)
        {
            _parameter = parameter;
        }
    }
}
