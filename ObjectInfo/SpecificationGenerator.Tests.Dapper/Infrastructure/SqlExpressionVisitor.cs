using System.Linq.Expressions;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime;

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
    }

    public class SqlExpressionVisitor<T> : ExpressionVisitor where T : class
    {
        private readonly ISqlWhereClauseBuilder _builder;
        private int _parameterIndex;

        public SqlExpressionVisitor(SqlSpecification<T> specification)
        {
            _builder = specification as ISqlWhereClauseBuilder
                ?? throw new ArgumentException(
                    $"Specification must implement {nameof(ISqlWhereClauseBuilder)}",
                    nameof(specification));
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            // Basic implementation for the tests
            var paramName = $"@p{_parameterIndex++}";
            _builder.AddToWhereClause($"1=1");
            return node;
        }
    }
}