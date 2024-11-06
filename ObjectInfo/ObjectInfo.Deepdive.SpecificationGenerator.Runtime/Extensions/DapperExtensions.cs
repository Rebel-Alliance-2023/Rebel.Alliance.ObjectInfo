using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Extensions
{
    /// <summary>
    /// Extension methods for IDbConnection to support SQL specifications
    /// </summary>
    public static class DapperExtensions
    {
        /// <summary>
        /// Executes a SQL specification and returns the results
        /// </summary>
        public static async Task<IEnumerable<T>> QueryWithSpecificationAsync<T>(
            this IDbConnection connection,
            SqlSpecification<T> specification,
            IDbTransaction? transaction = null,
            CancellationToken cancellationToken = default) where T : class
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (specification == null) throw new ArgumentNullException(nameof(specification));

            return await specification.QueryAsync(connection, transaction, cancellationToken);
        }

        /// <summary>
        /// Executes a SQL specification and returns the first result or null
        /// </summary>
        public static async Task<T?> FirstOrDefaultWithSpecificationAsync<T>(
            this IDbConnection connection,
            SqlSpecification<T> specification,
            IDbTransaction? transaction = null,
            CancellationToken cancellationToken = default) where T : class
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (specification == null) throw new ArgumentNullException(nameof(specification));

            return await specification.FirstOrDefaultAsync(connection, transaction, cancellationToken);
        }

        /// <summary>
        /// Gets the count of entities matching a SQL specification
        /// </summary>
        public static async Task<int> CountWithSpecificationAsync<T>(
            this IDbConnection connection,
            SqlSpecification<T> specification,
            IDbTransaction? transaction = null,
            CancellationToken cancellationToken = default) where T : class
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (specification == null) throw new ArgumentNullException(nameof(specification));

            return await specification.GetCountAsync(connection, transaction, cancellationToken);
        }
    }
}
