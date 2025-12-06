using System.Text;
using System.Collections;
using System.Reflection;
using System.Collections.Concurrent;
using System.Globalization;
using Rebel.Alliance.Specification.Dapper.Core;

namespace Rebel.Alliance.Specification.Dapper.Core
{
    /// <summary>
    /// Visits LINQ expression trees and converts them to SQL WHERE clauses for Dapper queries.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    public class SqlExpressionVisitor<T> : ExpressionVisitor where T : class
    {
        /// <summary>
        /// The StringBuilder used to construct the SQL statement.
        /// </summary>
        protected readonly StringBuilder _sqlBuilder = new();
        private readonly IParameterManager _parameters;
        private readonly SqlSpecification<T> _specification;
        // Cache for compiled member/constant accessors to avoid repeated Lambda.Compile()
        private readonly ConcurrentDictionary<Expression, Delegate> _valueAccessCache = new();
        // Cache for column name resolution
        private readonly ConcurrentDictionary<MemberInfo, string> _columnNameCache = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlExpressionVisitor{T}"/> class.
        /// </summary>
        /// <param name="specification">The SQL specification to use for parameter management.</param>
        public SqlExpressionVisitor(SqlSpecification<T> specification)
        {
            _specification = specification ?? throw new ArgumentNullException(nameof(specification));
            _parameters = specification.ParametersManager;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlExpressionVisitor{T}"/> class with a custom parameter manager.
        /// </summary>
        /// <param name="specification">The SQL specification.</param>
        /// <param name="parameters">The parameter manager to use.</param>
        public SqlExpressionVisitor(SqlSpecification<T> specification, IParameterManager parameters)
        {
            _specification = specification ?? throw new ArgumentNullException(nameof(specification));
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        /// <summary>
        /// Gets the generated SQL string.
        /// </summary>
        /// <returns>The SQL WHERE clause.</returns>
        public string GetSql() => _sqlBuilder.ToString();

        /// <inheritdoc/>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            bool needsParentheses = node.NodeType == ExpressionType.AndAlso || node.NodeType == ExpressionType.OrElse;
            
            if (needsParentheses) _sqlBuilder.Append('(');

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

            if (needsParentheses) _sqlBuilder.Append(')');
            
            return node;
        }

        /// <inheritdoc/>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(string))
            {
                return VisitStringMethod(node);
            }
            else if (node.Method.Name == "Contains")
            {
                return VisitContainsMethod(node);
            }

            return base.VisitMethodCall(node);
        }

        /// <inheritdoc/>
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is ParameterExpression)
            {
                var columnName = GetColumnNameCached(node.Member);
                
                if (node.Type == typeof(bool))
                {
                    _sqlBuilder.Append(columnName)
                              .Append(" = 1");
                }
                else
                {
                    _sqlBuilder.Append(columnName);
                }
                return node;
            }

            var value = EvaluateValue(node);
            var normalized = NormalizeParameterValue(value ?? DBNull.Value);
            var paramName = _parameters.CreateParameter(normalized);
            _sqlBuilder.Append(paramName);
            return node;
        }

        /// <inheritdoc/>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            var normalized = NormalizeParameterValue(node.Value ?? DBNull.Value);
            var paramName = _parameters.CreateParameter(normalized);
            _sqlBuilder.Append(paramName);
            return node;
        }

        /// <inheritdoc/>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not)
            {
                _sqlBuilder.Append("NOT (");
                Visit(node.Operand);
                _sqlBuilder.Append(")");
                return node;
            }
            if (node.NodeType == ExpressionType.Convert)
            {
                return Visit(node.Operand);
            }
            return base.VisitUnary(node);
        }

        private Expression VisitStringMethod(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case "Contains":
                    return HandleStringContains(node);
                case "StartsWith":
                    return HandleStringStartsWith(node);
                case "EndsWith":
                    return HandleStringEndsWith(node);
                case "IsNullOrEmpty":
                    return HandleStringIsNullOrEmpty(node);
                default:
                    throw new NotSupportedException($"String method {node.Method.Name} is not supported");
            }
        }

        private Expression VisitContainsMethod(MethodCallExpression node)
        {
            IEnumerable collection;
            Expression itemExpr;

            if (node.Method.IsStatic)
            {
                var collExpr = node.Arguments[0];
                var maybe = ResolveCollection(collExpr);
                collection = (IEnumerable)(maybe ?? throw new InvalidOperationException("Collection is null"));
                itemExpr = node.Arguments[1];
            }
            else
            {
                var collExpr = node.Object!;
                var maybe = ResolveCollection(collExpr);
                collection = (IEnumerable)(maybe ?? throw new InvalidOperationException("Collection is null"));
                itemExpr = node.Arguments[0];
            }

            if (collection == null) throw new ArgumentNullException(nameof(collection));

            var values = collection.Cast<object>().ToList();
            if (!values.Any()) return node;

            string columnName = GetColumnNameCached(GetMemberInfo(itemExpr));
            _sqlBuilder.Append(columnName).Append(" IN (");

            var parameters = new List<string>();
            foreach (var item in values)
            {
                parameters.Add(_parameters.CreateParameter(NormalizeParameterValue(item)));
            }

            _sqlBuilder.Append(string.Join(", ", parameters)).Append(')');
            return node;
        }

        private IEnumerable? ResolveCollection(Expression expr)
        {
            // Try specific shapes first
            switch (expr)
            {
                case UnaryExpression ue when ue.NodeType == ExpressionType.Convert || ue.NodeType == ExpressionType.ConvertChecked:
                    return ResolveCollection(ue.Operand);
                case NewArrayExpression nae:
                    {
                        var items = new object?[nae.Expressions.Count];
                        for (int i = 0; i < nae.Expressions.Count; i++)
                            items[i] = EvaluateValue(nae.Expressions[i]);
                        return items;
                    }
                case ConstantExpression ce:
                    return ce.Value as IEnumerable;
                case MemberExpression me:
                    {
                        var instance = me.Expression != null ? EvaluateValue(me.Expression) : null;
                        if (me.Member is FieldInfo fi) return fi.GetValue(instance) as IEnumerable;
                        if (me.Member is PropertyInfo pi) return pi.GetValue(instance) as IEnumerable;
                        return null;
                    }
                case MethodCallExpression mce:
                    {
                        // Collapse LINQ wrappers like AsEnumerable/ToArray/ToList/Cast/OfType around a resolvable source
                        if (mce.Arguments.Count > 0)
                        {
                            var src = ResolveCollection(mce.Arguments[0]);
                            if (src != null) return src;
                        }
                        break;
                    }
            }
            // Fallback: interpreter
            try
            {
                Expression conv = expr.Type == typeof(IEnumerable)
                    ? expr
                    : (typeof(IEnumerable).IsAssignableFrom(expr.Type)
                        ? expr
                        : Expression.Convert(expr, typeof(IEnumerable)));
                var func = Expression.Lambda<Func<IEnumerable>>(conv).Compile(preferInterpretation: true);
                return func();
            }
            catch
            {
                return null;
            }
        }

        private IEnumerable? EvaluateEnumerable(Expression expr)
        {
            // Handle common cases without compiling
            switch (expr)
            {
                case UnaryExpression ue when ue.NodeType == ExpressionType.Convert || ue.NodeType == ExpressionType.ConvertChecked:
                    return EvaluateEnumerable(ue.Operand);
                case ConstantExpression ce:
                    return ce.Value as IEnumerable;
                case MemberExpression me:
                    {
                        var instance = me.Expression != null ? EvaluateValue(me.Expression) : null;
                        return me.Member switch
                        {
                            FieldInfo fi => fi.GetValue(instance) as IEnumerable,
                            PropertyInfo pi => pi.GetValue(instance) as IEnumerable,
                            _ => null
                        };
                    }
                case MethodCallExpression mce when mce.Method.IsStatic && mce.Method.DeclaringType == typeof(Enumerable):
                    // Collapse common LINQ wrappers around a constant/member enumerable
                    if (mce.Arguments.Count > 0)
                    {
                        var src = EvaluateEnumerable(mce.Arguments[0]);
                        return src; // We only need to iterate values later
                    }
                    break;
                case NewArrayExpression nae:
                    {
                        var items = new object?[nae.Expressions.Count];
                        for (int i = 0; i < nae.Expressions.Count; i++)
                        {
                            items[i] = EvaluateValue(nae.Expressions[i]);
                        }
                        return items;
                    }
            }
            // Fallback: attempt to compile as IEnumerable without boxing to object to avoid invalid IL on .NET 10
            try
            {
                if (expr is ParameterExpression)
                {
                    return null; // cannot evaluate at this stage
                }
                Expression conv = expr.Type == typeof(IEnumerable)
                    ? expr
                    : (typeof(IEnumerable).IsAssignableFrom(expr.Type)
                        ? expr
                        : Expression.Convert(expr, typeof(IEnumerable)));
                var lambda = Expression.Lambda<Func<IEnumerable>>(conv);
                var func = lambda.Compile(preferInterpretation: true);
                return func();
            }
            catch
            {
                return null;
            }
        }

        private object? EvaluateValue(Expression expr)
        {
            switch (expr)
            {
                case ConstantExpression ce:
                    return ce.Value;
                case MethodCallExpression mce when mce.Method.IsStatic && mce.Method.DeclaringType == typeof(Enumerable):
                    return EvaluateEnumerable(mce);
                case MemberExpression me:
                    {
                        var instance = me.Expression != null ? EvaluateValue(me.Expression) : null;
                        if (me.Member is FieldInfo fi) return fi.GetValue(instance);
                        if (me.Member is PropertyInfo pi) return pi.GetValue(instance);
                        return null;
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
            return null;
        }

        private Expression HandleStringContains(MethodCallExpression node)
        {
            Visit(node.Object);
            _sqlBuilder.Append(" LIKE '%' || ");
            Visit(node.Arguments[0]);
            _sqlBuilder.Append(" || '%'");
            return node;
        }

        private Expression HandleStringStartsWith(MethodCallExpression node)
        {
            Visit(node.Object);
            _sqlBuilder.Append(" LIKE ");
            Visit(node.Arguments[0]);
            _sqlBuilder.Append(" || '%'");
            return node;
        }

        private Expression HandleStringEndsWith(MethodCallExpression node)
        {
            Visit(node.Object);
            _sqlBuilder.Append(" LIKE '%' || ");
            Visit(node.Arguments[0]);
            return node;
        }

        private Expression HandleStringIsNullOrEmpty(MethodCallExpression node)
        {
            // node.Arguments[0] is the string expression
            _sqlBuilder.Append("(");
            Visit(node.Arguments[0]);
            _sqlBuilder.Append(" IS NULL OR ");
            Visit(node.Arguments[0]);
            _sqlBuilder.Append(" = ''")
                      .Append(")");
            return node;
        }

        private static bool IsNullConstant(Expression expr)
            => expr is ConstantExpression ce && ce.Value == null;

        private static string GetOperator(ExpressionType type)
        {
            return type switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "!=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "AND",
                ExpressionType.OrElse => "OR",
                _ => throw new NotSupportedException($"Operator {type} is not supported")
            };
        }

        private static string GetNullOperator(ExpressionType type)
        {
            return type switch
            {
                ExpressionType.Equal => " IS NULL",
                ExpressionType.NotEqual => " IS NOT NULL",
                _ => throw new NotSupportedException($"Null comparison not supported for operator {type}")
            };
        }

        private string GetColumnNameCached(MemberInfo memberInfo)
        {
            // Respect [Column] attribute if present for proper DB column mapping
            var columnAttr = memberInfo.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>();
            return _columnNameCache.GetOrAdd(memberInfo, _ => columnAttr?.Name ?? memberInfo.Name);
        }

        private static MemberInfo GetMemberInfo(Expression expression)
        {
            return expression switch
            {
                MemberExpression m => m.Member,
                UnaryExpression u when u.Operand is MemberExpression m => m.Member,
                _ => throw new InvalidOperationException($"Cannot get member info from expression type: {expression.GetType()}")
            };
        }

        private object GetValueFast(Expression expr)
        {
            // Fast-path evaluation without compiling for constants, member access, and simple conversions.
            switch (expr)
            {
                case ConstantExpression ce:
                    return ce.Value!;
                case MemberExpression me:
                    {
                        object? instance = me.Expression != null ? GetValueFast(me.Expression) : null;
                        if (me.Member is FieldInfo fi)
                            return fi.GetValue(instance)!;
                        if (me.Member is PropertyInfo pi)
                            return pi.GetValue(instance)!;
                        throw new NotSupportedException($"Unsupported member access: {me.Member.MemberType}");
                    }
                case UnaryExpression ue when ue.NodeType == ExpressionType.Convert || ue.NodeType == ExpressionType.ConvertChecked:
                    {
                        var val = GetValueFast(ue.Operand);
                        if (val == null) return null!;
                        var targetType = Nullable.GetUnderlyingType(ue.Type) ?? ue.Type;
                        // If already assignable, return as-is
                        if (targetType.IsInstanceOfType(val)) return val;
                        return Convert.ChangeType(val, targetType, CultureInfo.InvariantCulture)!;
                    }
                case NewArrayExpression nae:
                    {
                        var elementType = nae.Type.GetElementType() ?? typeof(object);
                        var arr = Array.CreateInstance(elementType, nae.Expressions.Count);
                        for (int i = 0; i < nae.Expressions.Count; i++)
                        {
                            var v = GetValueFast(nae.Expressions[i]);
                            arr.SetValue(v, i);
                        }
                        return arr;
                    }
            }
            // Fallback: compile to a strongly-typed delegate using interpreter to avoid invalid IL on .NET 10
            var del = _valueAccessCache.GetOrAdd(expr, e =>
            {
                var body = e.Type.IsValueType ? Expression.Convert(e, typeof(object)) : e;
                return Expression.Lambda<Func<object>>(body).Compile(preferInterpretation: true);
            });
            return ((Func<object>)del).Invoke();
        }

        private static object NormalizeParameterValue(object value)
        {
            if (value == null) return DBNull.Value;
            var t = value.GetType();
            if (t.IsEnum)
            {
                var underlying = Enum.GetUnderlyingType(t);
                return Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
            }
            // Keep DateTime and numeric as-is; strings unchanged
            return value;
        }
    }
}
