using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure
{
    public interface ISqlWhereClauseBuilder
    {
        void AddToWhereClause(string clause);
    }

    public abstract class SqlSpecificationBase<T> : SqlSpecification<T>, ISqlWhereClauseBuilder where T : class
    {
        public void AddToWhereClause(string clause)
        {
            AddWhereClause(clause);
        }

        public void AddParameter(string name, object value)
        {
            Parameters[name] = value;
        }
    }

    public abstract class SqlSpecification<T> where T : class
    {
        protected Expression<Func<T, bool>> Criteria { get; set; }
        protected List<string> WhereClauses { get; } = new List<string>();

        // Changed access modifier to 'internal'
        internal Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        protected void AddWhereClause(string clause)
        {
            WhereClauses.Add(clause);
        }

        public virtual string ToSql()
        {
            string tableName = GetTableName();
            string sql = $"SELECT * FROM {tableName}";

            if (WhereClauses.Any())
            {
                sql += " WHERE " + string.Join(" AND ", WhereClauses);
            }

            return sql;
        }

        public Dictionary<string, object> GetParameters()
        {
            return Parameters;
        }

        protected virtual string GetTableName()
        {
            // Check for [Table] attribute
            var tableAttr = typeof(T).GetCustomAttribute<TableAttribute>();
            if (tableAttr != null)
            {
                return tableAttr.Name;
            }

            // Simple pluralization
            var typeName = typeof(T).Name;
            return typeName.EndsWith("s") ? typeName : typeName + "s";
        }

        protected abstract void BuildWhereClause();
    }

    public class SqlExpressionVisitor<T> : ExpressionVisitor where T : class
    {
        protected readonly SqlSpecification<T> _specification;
        protected readonly StringBuilder _sqlBuilder = new();
        protected int _parameterIndex;

        public SqlExpressionVisitor(SqlSpecification<T> specification)
        {
            _specification = specification ?? throw new ArgumentNullException(nameof(specification));
        }

        public string GetSql()
        {
            return _sqlBuilder.ToString();
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            bool needsParentheses = node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.OrElse;
            if (needsParentheses)
            {
                _sqlBuilder.Append("(");
            }

            if (IsNullConstant(node.Right))
            {
                Visit(node.Left);
                _sqlBuilder.Append(GetNullOperator(node.NodeType));
            }
            else if (IsNullConstant(node.Left))
            {
                Visit(node.Right);
                _sqlBuilder.Append(GetNullOperator(node.NodeType));
            }
            else
            {
                Visit(node.Left);
                _sqlBuilder.Append($" {GetOperator(node.NodeType)} ");
                Visit(node.Right);
            }

            if (needsParentheses)
            {
                _sqlBuilder.Append(")");
            }
            return node;
        }

        private bool IsNullConstant(Expression expr)
        {
            return expr is ConstantExpression ce && ce.Value == null;
        }

        private string GetNullOperator(ExpressionType nodeType)
        {
            return nodeType switch
            {
                ExpressionType.Equal => " IS NULL",
                ExpressionType.NotEqual => " IS NOT NULL",
                _ => throw new NotSupportedException($"Null comparison not supported for operator {nodeType}")
            };
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is ParameterExpression)
            {
                var member = node.Member;

                // Get column name from [Column] attribute if present
                var columnAttr = member.GetCustomAttribute<ColumnAttribute>();
                var columnName = columnAttr?.Name ?? member.Name;

                if (node.Type == typeof(bool) || node.Type == typeof(bool?))
                {
                    // For boolean properties, generate "ColumnName = 1"
                    _sqlBuilder.Append(columnName);
                    _sqlBuilder.Append(" = 1");
                }
                else
                {
                    // Append the column name to the SQL
                    _sqlBuilder.Append(columnName);
                }
                return node;
            }

            // Evaluate the member expression to get its value
            var value = Expression.Lambda(node).Compile().DynamicInvoke();
            var paramName = $"@p{_parameterIndex++}";
            _specification.Parameters[paramName] = value;
            _sqlBuilder.Append(paramName);
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(string))
            {
                if (node.Method.Name == "Contains")
                {
                    // Handle StringComparison parameter
                    bool ignoreCase = false;
                    Expression searchExpression = null;

                    if (node.Arguments.Count == 2)
                    {
                        // Get the StringComparison argument
                        var comparisonType = (StringComparison)Expression.Lambda(node.Arguments[1]).Compile().DynamicInvoke();
                        if (comparisonType == StringComparison.OrdinalIgnoreCase || comparisonType == StringComparison.CurrentCultureIgnoreCase)
                        {
                            ignoreCase = true;
                        }
                        searchExpression = node.Arguments[0];
                    }
                    else
                    {
                        searchExpression = node.Arguments[0];
                    }

                    Visit(node.Object); // The string property
                    _sqlBuilder.Append(" LIKE ");
                    var paramName = $"@p{_parameterIndex++}";
                    var argumentValue = Expression.Lambda(searchExpression).Compile().DynamicInvoke();
                    _specification.Parameters[paramName] = $"%{argumentValue}%";
                    _sqlBuilder.Append(paramName);

                    if (ignoreCase)
                    {
                        _sqlBuilder.Append(" COLLATE NOCASE"); // For case-insensitive comparison
                    }

                    return node;
                }
                else if (node.Method.Name == "IsNullOrEmpty")
                {
                    // Handle string.IsNullOrEmpty(someString)
                    var argument = node.Arguments[0];

                    _sqlBuilder.Append("(");
                    Visit(argument);
                    _sqlBuilder.Append(" IS NULL OR ");
                    Visit(argument);
                    _sqlBuilder.Append(" = '')");
                    return node;
                }
                else if (node.Method.Name == "StartsWith")
                {
                    // Handle StartsWith method
                    bool ignoreCase = false;
                    Expression searchExpression = null;

                    if (node.Arguments.Count == 2)
                    {
                        // Get the StringComparison argument
                        var comparisonType = (StringComparison)Expression.Lambda(node.Arguments[1]).Compile().DynamicInvoke();
                        if (comparisonType == StringComparison.OrdinalIgnoreCase || comparisonType == StringComparison.CurrentCultureIgnoreCase)
                        {
                            ignoreCase = true;
                        }
                        searchExpression = node.Arguments[0];
                    }
                    else
                    {
                        searchExpression = node.Arguments[0];
                    }

                    Visit(node.Object); // The string property
                    _sqlBuilder.Append(" LIKE ");
                    var paramName = $"@p{_parameterIndex++}";
                    var argumentValue = Expression.Lambda(searchExpression).Compile().DynamicInvoke();
                    _specification.Parameters[paramName] = $"{argumentValue}%";
                    _sqlBuilder.Append(paramName);

                    if (ignoreCase)
                    {
                        _sqlBuilder.Append(" COLLATE NOCASE");
                    }

                    return node;
                }
                else if (node.Method.Name == "EndsWith")
                {
                    // Handle EndsWith method
                    bool ignoreCase = false;
                    Expression searchExpression = null;

                    if (node.Arguments.Count == 2)
                    {
                        // Get the StringComparison argument
                        var comparisonType = (StringComparison)Expression.Lambda(node.Arguments[1]).Compile().DynamicInvoke();
                        if (comparisonType == StringComparison.OrdinalIgnoreCase || comparisonType == StringComparison.CurrentCultureIgnoreCase)
                        {
                            ignoreCase = true;
                        }
                        searchExpression = node.Arguments[0];
                    }
                    else
                    {
                        searchExpression = node.Arguments[0];
                    }

                    Visit(node.Object); // The string property
                    _sqlBuilder.Append(" LIKE ");
                    var paramName = $"@p{_parameterIndex++}";
                    var argumentValue = Expression.Lambda(searchExpression).Compile().DynamicInvoke();
                    _specification.Parameters[paramName] = $"%{argumentValue}";
                    _sqlBuilder.Append(paramName);

                    if (ignoreCase)
                    {
                        _sqlBuilder.Append(" COLLATE NOCASE");
                    }

                    return node;
                }
            }
            else if (node.Method.Name == "Contains")
            {
                IEnumerable collection = null;
                Expression itemExpr = null;

                if (node.Object != null && typeof(IEnumerable).IsAssignableFrom(node.Object.Type))
                {
                    // Instance method call: collection.Contains(item)
                    collection = Expression.Lambda(node.Object).Compile().DynamicInvoke() as IEnumerable;
                    itemExpr = node.Arguments[0];
                }
                else if (node.Method.IsStatic && node.Method.DeclaringType == typeof(Enumerable))
                {
                    // Static method call: Enumerable.Contains(collection, item)
                    collection = Expression.Lambda(node.Arguments[0]).Compile().DynamicInvoke() as IEnumerable;
                    itemExpr = node.Arguments[1];
                }

                if (collection != null && itemExpr != null)
                {
                    string columnName = GetMemberName(itemExpr);
                    _sqlBuilder.Append(columnName);
                    _sqlBuilder.Append(" IN (");

                    var parameters = new List<string>();
                    foreach (var item in collection)
                    {
                        var paramName = $"@p{_parameterIndex++}";
                        _specification.Parameters[paramName] = item;
                        parameters.Add(paramName);
                    }
                    _sqlBuilder.Append(string.Join(", ", parameters));
                    _sqlBuilder.Append(")");
                    return node;
                }
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not)
            {
                _sqlBuilder.Append("NOT (");
                Visit(node.Operand);
                _sqlBuilder.Append(")");
                return node;
            }
            return base.VisitUnary(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var paramName = $"@p{_parameterIndex++}";
            _specification.Parameters[paramName] = node.Value!;
            _sqlBuilder.Append(paramName);
            return node;
        }

        private string GetOperator(ExpressionType nodeType)
        {
            return nodeType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "!=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "AND",
                ExpressionType.OrElse => "OR",
                _ => throw new NotSupportedException($"Operation {nodeType} is not supported")
            };
        }

        private string GetMemberName(Expression expression)
        {
            if (expression is MemberExpression memberExpr)
            {
                if (memberExpr.Expression is ParameterExpression)
                {
                    var member = memberExpr.Member;
                    var columnAttr = member.GetCustomAttribute<ColumnAttribute>();
                    return columnAttr?.Name ?? member.Name;
                }
                else if (memberExpr.Expression is UnaryExpression unaryExpr)
                {
                    return GetMemberName(unaryExpr);
                }
            }
            else if (expression is UnaryExpression unaryExpr)
            {
                return GetMemberName(unaryExpr.Operand);
            }
            throw new NotSupportedException($"Expression type {expression.GetType().Name} is not supported for extracting member name.");
        }
    }
}
