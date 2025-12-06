using System.Text;
using System.Collections;
using System.Reflection;
using System.Collections.Concurrent;
using System.Globalization;
using Rebel.Alliance.Specification.Dapper.Core;

namespace Rebel.Alliance.Specification.Dapper.Core
{
    public class SqlExpressionVisitor<T> : ExpressionVisitor where T : class
    {
        protected readonly StringBuilder _sqlBuilder = new();
        private readonly IParameterManager _parameters;
        private readonly SqlSpecification<T> _specification;
        // Cache for compiled member/constant accessors to avoid repeated Lambda.Compile()
        private readonly ConcurrentDictionary<Expression, Delegate> _valueAccessCache = new();
        // Cache for column name resolution
        private readonly ConcurrentDictionary<MemberInfo, string> _columnNameCache = new();

        public SqlExpressionVisitor(SqlSpecification<T> specification)
        {
            _specification = specification ?? throw new ArgumentNullException(nameof(specification));
            _parameters = specification.ParametersManager;
        }

        public SqlExpressionVisitor(SqlSpecification<T> specification, IParameterManager parameters)
        {
            _specification = specification ?? throw new ArgumentNullException(nameof(specification));
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        public string GetSql() => _sqlBuilder.ToString();

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

            var value = GetValueFast(node);
            var normalized = NormalizeParameterValue(value);
            var paramName = _parameters.CreateParameter(normalized);
            _sqlBuilder.Append(paramName);
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var normalized = NormalizeParameterValue(node.Value);
            var paramName = _parameters.CreateParameter(normalized);
            _sqlBuilder.Append(paramName);
            return node;
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
                collection = (IEnumerable)(GetValueFast(node.Arguments[0]) ?? throw new InvalidOperationException("Collection is null"));
                itemExpr = node.Arguments[1];
            }
            else
            {
                collection = (IEnumerable)(GetValueFast(node.Object!) ?? throw new InvalidOperationException("Collection is null"));
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
            // Cache compiled lambda delegates per expression
            var del = _valueAccessCache.GetOrAdd(expr, e => Expression.Lambda(e).Compile());
            return del.DynamicInvoke();
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
