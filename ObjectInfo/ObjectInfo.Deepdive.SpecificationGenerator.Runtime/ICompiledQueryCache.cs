using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching
{
    /// <summary>
    /// Defines the contract for caching compiled queries
    /// </summary>
    public interface ICompiledQueryCache
    {
        /// <summary>
        /// Gets or adds a cached query transformer for Entity Framework Core
        /// </summary>
        Func<IQueryable<T>, IQueryable<T>> GetOrAddQueryTransformer<T>(
            ISpecification<T> specification,
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Gets or adds a cached SQL query for Dapper
        /// </summary>
        string GetOrAddSqlQuery<T>(
            ISpecification<T> specification,
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Invalidates all cached queries for a given type
        /// </summary>
        void InvalidateQueriesForType<T>() where T : class;

        /// <summary>
        /// Invalidates all cached queries
        /// </summary>
        void InvalidateAllQueries();
    }
}
