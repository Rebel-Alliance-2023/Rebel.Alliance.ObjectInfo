using Rebel.Alliance.Specification.Dapper.Core;

namespace Rebel.Alliance.Specification.Dapper.Extensions
{
    /// <summary>
    /// Extension methods for executing Dapper queries with specifications.
    /// </summary>
    public static class DapperExtensions
    {
        /// <summary>
        /// Executes a query using the specification and returns all matching entities.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="transaction">The optional transaction.</param>
        /// <param name="commandTimeout">The optional command timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The matching entities.</returns>
        public static async Task<IEnumerable<T>> QueryWithSpecificationAsync<T>(
            this IDbConnection connection,
            SqlSpecification<T> specification,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CancellationToken cancellationToken = default) where T : class
        {
            var sql = specification.ToSql();
            var parameters = specification.GetParameters();

            var command = new CommandDefinition(
                sql,
                parameters,
                transaction,
                commandTimeout,
                CommandType.Text,
                CommandFlags.None,
                cancellationToken);

            return await connection.QueryAsync<T>(command);
        }

        /// <summary>
        /// Executes a query using the specification and returns the first matching entity or null.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="transaction">The optional transaction.</param>
        /// <param name="commandTimeout">The optional command timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The first matching entity or null.</returns>
        public static async Task<T?> FirstOrDefaultWithSpecificationAsync<T>(
            this IDbConnection connection,
            SqlSpecification<T> specification,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CancellationToken cancellationToken = default) where T : class
        {
            var sql = specification.ToSql();
            var parameters = specification.GetParameters();

            var command = new CommandDefinition(
                sql,
                parameters,
                transaction,
                commandTimeout,
                CommandType.Text,
                CommandFlags.None,
                cancellationToken);

            return await connection.QueryFirstOrDefaultAsync<T>(command);
        }

        /// <summary>
        /// Executes a count query using the specification.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="transaction">The optional transaction.</param>
        /// <param name="commandTimeout">The optional command timeout.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The count of matching entities.</returns>
        public static async Task<int> CountWithSpecificationAsync<T>(
            this IDbConnection connection,
            SqlSpecification<T> specification,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CancellationToken cancellationToken = default) where T : class
        {
            var sql = $"SELECT COUNT(*) FROM ({specification.ToSql()}) AS CountQuery";
            var parameters = specification.GetParameters();

            var command = new CommandDefinition(
                sql,
                parameters,
                transaction,
                commandTimeout,
                CommandType.Text,
                CommandFlags.None,
                cancellationToken);

            return await connection.ExecuteScalarAsync<int>(command);
        }
    }
}
