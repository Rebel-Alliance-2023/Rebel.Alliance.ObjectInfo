using System.Text;
using System.Collections;
using System.Reflection;
using Rebel.Alliance.Specification.Dapper.Core;

namespace Rebel.Alliance.Specification.Dapper.Core
{
    public class SqlExpressionVisitor<T> : ExpressionVisitor where T : class
    {
        protected readonly StringBuilder _sqlBuilder = new();
        private readonly IParameterManager _parameters;
        private readonly SqlSpecification<T> _specification;

        public SqlExpressionVisitor(SqlSpecification<T> specification)
        {
            _specification = specification ?? throw new ArgumentNullException(nameof(specification));
            _parameters = new ParameterManager();
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
                var columnName = GetColumnName(node.Member);
                
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

            var value = Expression.Lambda(node).Compile().DynamicInvoke();
            var paramName = _parameters.CreateParameter(value);
            _sqlBuilder.Append(paramName);
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var paramName = _parameters.CreateParameter(node.Value!);
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
                collection = (IEnumerable)Expression.Lambda(node.Arguments[0]).Compile().DynamicInvoke()!;
                itemExpr = node.Arguments[1];
            }
            else
            {
                collection = (IEnumerable)Expression.Lambda(node.Object!).Compile().DynamicInvoke()!;
                itemExpr = node.Arguments[0];
            }

            if (collection == null) throw new ArgumentNullException(nameof(collection));

            var values = collection.Cast<object>().ToList();
            if (!values.Any()) return node;

            string columnName = GetColumnName(GetMemberInfo(itemExpr));
            _sqlBuilder.Append(columnName).Append(" IN (");

            var parameters = new List<string>();
            foreach (var item in values)
            {
                parameters.Add(_parameters.CreateParameter(item));
            }

            _sqlBuilder.Append(string.Join(", ", parameters)).Append(')');
            return node;
        }

        private Expression HandleStringContains(MethodCallExpression node)
        {
            Visit(node.Object);
            _sqlBuilder.Append(" LIKE CONCAT('%', ");
            Visit(node.Arguments[0]);
            _sqlBuilder.Append(", '%')");
            return node;
        }

        private Expression HandleStringStartsWith(MethodCallExpression node)
        {
            Visit(node.Object);
            _sqlBuilder.Append(" LIKE CONCAT(");
            Visit(node.Arguments[0]);
            _sqlBuilder.Append(", '%')");
            return node;
        }

        private Expression HandleStringEndsWith(MethodCallExpression node)
        {
            Visit(node.Object);
            _sqlBuilder.Append(" LIKE CONCAT('%', ");
            Visit(node.Arguments[0]);
            _sqlBuilder.Append(")");
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

        private static string GetColumnName(MemberInfo memberInfo)
        {
            // Could be extended to support column mapping attributes
            return memberInfo.Name;
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
    }
}
