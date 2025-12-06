using Rebel.Alliance.Specification.Dapper.Core;

namespace Rebel.Alliance.Specification.Dapper.Extensions
{
    public static class QueryableExtensions
    {
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
