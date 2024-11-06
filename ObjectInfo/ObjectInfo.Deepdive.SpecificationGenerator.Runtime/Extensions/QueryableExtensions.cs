using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Extensions
{
    /// <summary>
    /// Extension methods for IQueryable to support specifications
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Applies a specification to a queryable
        /// </summary>
        public static IQueryable<T> ApplySpecification<T>(this IQueryable<T> query, ISpecification<T> specification) 
            where T : class
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (specification == null) throw new ArgumentNullException(nameof(specification));

            // Get internal expression
            var expr = GetExpression(specification);
            if (expr != null)
            {
                query = query.Where(expr);
            }

            // Apply includes for navigation properties using expressions
            var includes = GetIncludes(specification);
            query = includes.Aggregate(query, (current, include) => 
                current.Include(include));

            // Apply string-based includes (useful for multi-level includes)
            var includeStrings = GetIncludeStrings(specification);
            query = includeStrings.Aggregate(query, (current, include) => 
                current.Include(include));

            // Apply ordering
            var orderBy = GetOrderBy(specification);
            var orderByDescending = GetOrderByDescending(specification);
            
            if (orderBy != null)
            {
                query = query.OrderBy(orderBy);
                
                // Apply ThenBy operations
                var thenByExpressions = GetThenByExpressions(specification);
                query = thenByExpressions.Aggregate(query.OrderBy(orderBy), 
                    (current, thenBy) => current.ThenBy(thenBy));
            }
            else if (orderByDescending != null)
            {
                query = query.OrderByDescending(orderByDescending);
                
                // Apply ThenByDescending operations
                var thenByDescendingExpressions = GetThenByDescendingExpressions(specification);
                query = thenByDescendingExpressions.Aggregate(query.OrderByDescending(orderByDescending), 
                    (current, thenBy) => current.ThenByDescending(thenBy));
            }

            // Apply paging
            if (specification.IsPagingEnabled)
            {
                query = query.Skip(specification.Skip.GetValueOrDefault())
                           .Take(specification.Take.GetValueOrDefault());
            }

            return query;
        }

        private static Expression<Func<T, bool>>? GetExpression<T>(ISpecification<T> specification)
            where T : class
        {
            return specification.Criteria;
        }

        private static IEnumerable<Expression<Func<T, object>>> GetIncludes<T>(ISpecification<T> specification)
            where T : class
        {
            return specification.Includes;
        }

        private static IEnumerable<string> GetIncludeStrings<T>(ISpecification<T> specification)
            where T : class
        {
            return specification.IncludeStrings;
        }

        private static Expression<Func<T, object>>? GetOrderBy<T>(ISpecification<T> specification)
            where T : class
        {
            return specification.OrderBy;
        }

        private static Expression<Func<T, object>>? GetOrderByDescending<T>(ISpecification<T> specification)
            where T : class
        {
            return specification.OrderByDescending;
        }

        private static IEnumerable<Expression<Func<T, object>>> GetThenByExpressions<T>(ISpecification<T> specification)
            where T : class
        {
            return specification.ThenByExpressions;
        }

        private static IEnumerable<Expression<Func<T, object>>> GetThenByDescendingExpressions<T>(ISpecification<T> specification)
            where T : class
        {
            return specification.ThenByDescendingExpressions;
        }
    }
}
