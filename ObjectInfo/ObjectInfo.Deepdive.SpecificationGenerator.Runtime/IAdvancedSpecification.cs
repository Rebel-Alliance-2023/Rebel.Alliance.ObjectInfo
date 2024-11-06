using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery.Models;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery
{
    /// <summary>
    /// Defines the contract for advanced query capabilities
    /// </summary>
    public interface IAdvancedSpecification<T> where T : class
    {
        FilterGroup RootFilter { get; }
        IList<SortField> SortFields { get; }

        IAdvancedSpecification<T> AddSort(string propertyName, SortDirection direction = SortDirection.Ascending, int order = 0);
        IAdvancedSpecification<T> AddSort<TKey>(Expression<Func<T, TKey>> expression, SortDirection direction = SortDirection.Ascending, int order = 0);
        IAdvancedSpecification<T> ClearSort();
        IAdvancedSpecification<T> Where(Action<FilterGroup> filterConfig);
        IAdvancedSpecification<T> OrWhere(Action<FilterGroup> filterConfig);
        IAdvancedSpecification<T> ClearFilters();
        Task<IEnumerable<T>> ToCachedListAsync(TimeSpan? cacheTime = null, CancellationToken cancellationToken = default);
    }
}
