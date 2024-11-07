using System.Linq.Expressions;
using Xunit;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.Helpers
{
    public static class AssertionHelpers
    {
        public static void AssertQueryEquals<T>(IQueryable<T> expected, IQueryable<T> actual) where T : class
        {
            var expectedList = expected.ToList();
            var actualList = actual.ToList();

            Assert.Equal(expectedList.Count, actualList.Count);
            for (int i = 0; i < expectedList.Count; i++)
            {
                AssertObjectsEqual(expectedList[i], actualList[i]);
            }
        }

        public static void AssertObjectsEqual<T>(T expected, T actual) where T : class
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && !p.GetGetMethod()!.IsVirtual); // Exclude navigation properties

            foreach (var property in properties)
            {
                var expectedValue = property.GetValue(expected);
                var actualValue = property.GetValue(actual);

                Assert.Equal(expectedValue, actualValue);
            }
        }

        public static void AssertExpressionEquals<T>(Expression<Func<T, bool>> expected, Expression<Func<T, bool>> actual)
        {
            var expectedNormalized = NormalizeExpression(expected);
            var actualNormalized = NormalizeExpression(actual);

            Assert.Equal(expectedNormalized.ToString(), actualNormalized.ToString());
        }

        public static void AssertSpecificationResult<T>(IQueryable<T> query, Expression<Func<T, bool>> expectedPredicate) where T : class
        {
            var expected = query.Where(expectedPredicate).ToList();
            var actual = query.ToList();

            AssertQueryEquals(expected.AsQueryable(), actual.AsQueryable());
        }

        public static void AssertOrderingEquals<T, TKey>(IQueryable<T> query, Expression<Func<T, TKey>> orderBy, bool ascending = true)
        {
            var orderedExpected = ascending
                ? query.OrderBy(orderBy).ToList()
                : query.OrderByDescending(orderBy).ToList();

            var actual = query.ToList();

            Assert.Equal(orderedExpected, actual);
        }

        public static void AssertIncludesProperty<T, TProperty>(IQueryable<T> query, Expression<Func<T, TProperty>> propertyExpression)
            where T : class
        {
            var includes = (query.Provider as EntityQueryProvider)!
                .CreateQuery<T>(query.Expression)
                .GetType()
                .GetProperty("Includes")!
                .GetValue(query) as IEnumerable<string>;

            var propertyPath = GetPropertyPath(propertyExpression);
            Assert.Contains(propertyPath, includes!);
        }

        public static void AssertPagingCorrect<T>(IQueryable<T> query, int skip, int take)
        {
            var expectedCount = Math.Min(take, query.Count() - skip);
            var actual = query.ToList();

            Assert.Equal(expectedCount, actual.Count);
        }

        private static Expression NormalizeExpression<T>(Expression<Func<T, bool>> expression)
        {
            return new ExpressionNormalizer().Visit(expression);
        }

        private static string GetPropertyPath<T, TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            var memberExpression = propertyExpression.Body as MemberExpression;
            if (memberExpression == null)
                throw new ArgumentException("Expression must be a member expression", nameof(propertyExpression));

            var parts = new List<string>();
            while (memberExpression != null)
            {
                parts.Add(memberExpression.Member.Name);
                memberExpression = memberExpression.Expression as MemberExpression;
            }

            parts.Reverse();
            return string.Join(".", parts);
        }

        private class ExpressionNormalizer : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, ParameterExpression> _parameterMap = new();

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (!_parameterMap.TryGetValue(node, out var newParameter))
                {
                    newParameter = Expression.Parameter(node.Type, "x");
                    _parameterMap[node] = newParameter;
                }
                return newParameter;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                var expression = Visit(node.Expression);
                return expression == node.Expression ? node : Expression.MakeMemberAccess(expression, node.Member);
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                var left = Visit(node.Left);
                var right = Visit(node.Right);
                
                // Normalize comparison order for commutative operators
                if (IsCommutative(node.NodeType) && ShouldSwapOperands(left, right))
                {
                    return Expression.MakeBinary(node.NodeType, right, left);
                }

                return left == node.Left && right == node.Right
                    ? node
                    : Expression.MakeBinary(node.NodeType, left, right);
            }

            private static bool IsCommutative(ExpressionType nodeType)
            {
                return nodeType == ExpressionType.Equal ||
                       nodeType == ExpressionType.NotEqual ||
                       nodeType == ExpressionType.Add ||
                       nodeType == ExpressionType.Multiply;
            }

            private static bool ShouldSwapOperands(Expression left, Expression right)
            {
                // Simple heuristic: constants go to the right
                return left is ConstantExpression && !(right is ConstantExpression);
            }
        }
    }
}
