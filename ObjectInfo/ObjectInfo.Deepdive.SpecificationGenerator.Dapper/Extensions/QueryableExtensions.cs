using Rebel.Alliance.Specification.Dapper.Core;

namespace Rebel.Alliance.Specification.Dapper.Extensions
{
    /// <summary>
    /// Extension methods for applying specifications to IQueryable.
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Applies the specification criteria to the query.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="query">The query to apply the specification to.</param>
        /// <param name="specification">The specification to apply.</param>
        /// <returns>The query with the specification applied.</returns>
        public static IQueryable<T> ApplySpecification<T>(
            this IQueryable<T> query, 
            SqlSpecification<T> specification) where T : class
        {
            // Apply criteria
            var criteriaQuery = query.Where(specification.Criteria);

            // Apply paging
            if (specification.IsPagingEnabled)
            {
                criteriaQuery = criteriaQuery
                    .Skip(specification.Skip!.Value)
                    .Take(specification.Take!.Value);
            }

            // Apply ordering
            if (specification.OrderBy != null)
            {
                criteriaQuery = criteriaQuery.OrderBy(specification.OrderBy);
            }
            else if (specification.OrderByDescending != null)
            {
                criteriaQuery = criteriaQuery.OrderByDescending(specification.OrderByDescending);
            }

            return criteriaQuery;
        }
    }
}
