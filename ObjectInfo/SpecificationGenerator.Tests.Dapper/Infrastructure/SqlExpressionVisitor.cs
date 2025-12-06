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

            // Evaluate the member expression to get its value (without compiling)
            var value = EvaluateValue(node);
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
                        var comparisonValue = EvaluateValue(node.Arguments[1]);
                        var comparisonType = comparisonValue is StringComparison sc ? sc : StringComparison.Ordinal;
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
                    var argumentValue = EvaluateValue(searchExpression);
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
                        var comparisonValue = EvaluateValue(node.Arguments[1]);
                        var comparisonType = comparisonValue is StringComparison sc ? sc : StringComparison.Ordinal;
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
                    var argumentValue = EvaluateValue(searchExpression);
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
                        var comparisonValue = EvaluateValue(node.Arguments[1]);
                        var comparisonType = comparisonValue is StringComparison sc ? sc : StringComparison.Ordinal;
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
                    var argumentValue = EvaluateValue(searchExpression);
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
                // Support various Contains patterns:
                // 1. Instance method: collection.Contains(item) - node.Object is collection, Arguments[0] is item
                // 2. Enumerable.Contains(collection, item) - Arguments[0] is collection, Arguments[1] is item
                // 3. MemoryExtensions.Contains(span, item, comparer) - Arguments[0] is span (via op_Implicit), Arguments[1] is item
                
                Expression collExpr = null;
                Expression itemExpr = null;
                
                if (node.Object != null)
                {
                    // Instance method
                    collExpr = node.Object;
                    itemExpr = node.Arguments.Count > 0 ? node.Arguments[0] : null;
                }
                else if (node.Method.DeclaringType?.Name == "MemoryExtensions" && node.Arguments.Count >= 2)
                {
                    // MemoryExtensions.Contains(span, item, comparer?)
                    // The first argument is typically op_Implicit(array) or similar
                    collExpr = node.Arguments[0];
                    itemExpr = node.Arguments[1];
                }
                else if (node.Arguments.Count >= 2)
                {
                    // Enumerable.Contains(collection, item)
                    collExpr = node.Arguments[0];
                    itemExpr = node.Arguments[1];
                }
                else if (node.Arguments.Count == 1)
                {
                    // Possibly Array.Contains or similar
                    collExpr = node.Object;
                    itemExpr = node.Arguments[0];
                }

                if (collExpr == null || itemExpr == null)
                {
                    _sqlBuilder.Append("1 = 0");
                    return node;
                }

                string columnName = GetMemberName(itemExpr);

                // Extract the actual array/collection from the expression
                var srcExpr = UnwrapToCollection(collExpr);
                List<object> values = new List<object>();
                
                if (srcExpr is NewArrayExpression nae)
                {
                    foreach (var e in nae.Expressions)
                    {
                        var v = EvaluateValue(e);
                        if (v != null) values.Add(NormalizeForParameter(v));
                    }
                }
                else if (srcExpr is ConstantExpression cec && cec.Value is IEnumerable constEn && cec.Value is not string)
                {
                    foreach (var v in constEn)
                    {
                        if (v != null) values.Add(NormalizeForParameter(v));
                    }
                }
                else
                {
                    var raw = EvaluateValue(srcExpr);
                    if (raw is IEnumerable en && raw is not string)
                    {
                        foreach (var v in en)
                        {
                            if (v != null) values.Add(NormalizeForParameter(v));
                        }
                    }
                }

                if (values.Count == 0)
                {
                    _sqlBuilder.Append("1 = 0");
                    return node;
                }

                _sqlBuilder.Append(columnName).Append(" IN (");
                var ph = new List<string>(values.Count);
                foreach (var v in values)
                {
                    var p = $"@p{_parameterIndex++}";
                    _specification.Parameters[p] = v;
                    ph.Add(p);
                }
                _sqlBuilder.Append(string.Join(", ", ph)).Append(")");
                return node;
            }

            return base.VisitMethodCall(node);
        }

        private static object EvaluateValue(Expression expr)
        {
            switch (expr)
            {
                case ConstantExpression ce:
                    return ce.Value;
                case MemberExpression me:
                    {
                        var instance = me.Expression != null ? EvaluateValue(me.Expression) : null;
                        if (me.Member is FieldInfo fi) return fi.GetValue(instance);
                        if (me.Member is PropertyInfo pi) return pi.GetValue(instance);
                        break;
                    }
                case UnaryExpression ue when ue.NodeType == ExpressionType.Convert || ue.NodeType == ExpressionType.ConvertChecked:
                    return EvaluateValue(ue.Operand);
                case NewArrayExpression nae:
                    {
                        var elementType = nae.Type.GetElementType() ?? typeof(object);
                        var arr = Array.CreateInstance(elementType, nae.Expressions.Count);
                        for (int i = 0; i < nae.Expressions.Count; i++)
                        {
                            arr.SetValue(EvaluateValue(nae.Expressions[i]), i);
                        }
                        return arr;
                    }
            }
            // Safe fallback: interpreter
            try
            {
                var lambda = Expression.Lambda(expr);
                return lambda.Compile(preferInterpretation: true).DynamicInvoke();
            }
            catch
            {
                return null;
            }
        }

        private static IEnumerable EvaluateEnumerable(Expression expr)
        {
            switch (expr)
            {
                case ConstantExpression ce:
                    return ce.Value as IEnumerable;
                case NewArrayExpression nae:
                    {
                        var items = new object[nae.Expressions.Count];
                        for (int i = 0; i < nae.Expressions.Count; i++)
                            items[i] = EvaluateValue(nae.Expressions[i]);
                        return items;
                    }
                case UnaryExpression ue when ue.NodeType == ExpressionType.Convert || ue.NodeType == ExpressionType.ConvertChecked:
                    return EvaluateEnumerable(ue.Operand);
                case MemberExpression me:
                    {
                        var instance = me.Expression != null ? EvaluateValue(me.Expression) : null;
                        if (me.Member is FieldInfo fi) return fi.GetValue(instance) as IEnumerable;
                        if (me.Member is PropertyInfo pi) return pi.GetValue(instance) as IEnumerable;
                        break;
                    }
                case MethodCallExpression mce when mce.Method.IsStatic && mce.Method.DeclaringType == typeof(Enumerable):
                    if (mce.Arguments.Count > 0)
                        return EvaluateEnumerable(mce.Arguments[0]);
                    break;
            }
            try
            {
                var conv = Expression.Convert(expr, typeof(object));
                var obj = Expression.Lambda<Func<object>>(conv).Compile(preferInterpretation: true)();
                return obj as IEnumerable;
            }
            catch
            {
                return null;
            }
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

        private static object NormalizeForParameter(object value)
        {
            var t = value.GetType();
            if (t.IsEnum)
            {
                var underlying = Enum.GetUnderlyingType(t);
                return Convert.ChangeType(value, underlying, System.Globalization.CultureInfo.InvariantCulture);
            }
            return value;
        }

        private static Expression Unwrap(Expression expr)
        {
            while (expr is UnaryExpression ue && (ue.NodeType == ExpressionType.Convert || ue.NodeType == ExpressionType.ConvertChecked))
            {
                expr = ue.Operand;
            }
            return expr;
        }

        /// <summary>
        /// Unwraps expression to find the underlying collection.
        /// Handles: Convert expressions, op_Implicit method calls (e.g., T[] -> ReadOnlySpan&lt;T&gt;)
        /// </summary>
        private static Expression UnwrapToCollection(Expression expr)
        {
            // First unwrap any Convert/ConvertChecked
            expr = Unwrap(expr);
            
            // Handle op_Implicit calls (used in .NET 10 for array -> span conversions)
            while (expr is MethodCallExpression mce && mce.Method.Name == "op_Implicit" && mce.Arguments.Count == 1)
            {
                expr = Unwrap(mce.Arguments[0]);
            }
            
            return expr;
        }
    }
}
