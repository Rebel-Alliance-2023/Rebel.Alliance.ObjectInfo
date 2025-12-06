using Microsoft.Extensions.Options;
using Rebel.Alliance.Specification.Dapper.Configuration;

namespace Rebel.Alliance.Specification.Dapper.Infrastructure
{
    /// <summary>
    /// Manages database connections for Dapper operations.
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// Creates and opens a database connection.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>An open database connection.</returns>
        IDbConnection CreateConnection(string connectionString);

        /// <summary>
        /// Creates and opens a database connection asynchronously.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An open database connection.</returns>
        Task<IDbConnection> CreateConnectionAsync(string connectionString, CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases a database connection.
        /// </summary>
        /// <param name="connection">The connection to release.</param>
        void ReleaseConnection(IDbConnection connection);
    }

    /// <summary>
    /// Default implementation of <see cref="IConnectionManager"/>.
    /// </summary>
    public class ConnectionManager : IConnectionManager
    {
        private readonly DapperSpecificationOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManager"/> class.
        /// </summary>
        /// <param name="options">The Dapper specification options.</param>
        public ConnectionManager(IOptions<DapperSpecificationOptions> options)
        {
            _options = options.Value;
        }

        /// <inheritdoc/>
        public IDbConnection CreateConnection(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        /// <inheritdoc/>
        public async Task<IDbConnection> CreateConnectionAsync(
            string connectionString, 
            CancellationToken cancellationToken = default)
        {
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        /// <inheritdoc/>
        public void ReleaseConnection(IDbConnection connection)
        {
            if (connection.State != ConnectionState.Closed)
            {
                connection.Close();
            }
            
            if (connection is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
