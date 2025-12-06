using Microsoft.Extensions.Options;
using Rebel.Alliance.Specification.Dapper.Configuration;

namespace Rebel.Alliance.Specification.Dapper.Infrastructure
{
    public interface IConnectionManager
    {
        IDbConnection CreateConnection(string connectionString);
        Task<IDbConnection> CreateConnectionAsync(string connectionString, CancellationToken cancellationToken = default);
        void ReleaseConnection(IDbConnection connection);
    }

    public class ConnectionManager : IConnectionManager
    {
        private readonly DapperSpecificationOptions _options;

        public ConnectionManager(IOptions<DapperSpecificationOptions> options)
        {
            _options = options.Value;
        }

        public IDbConnection CreateConnection(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        public async Task<IDbConnection> CreateConnectionAsync(
            string connectionString, 
            CancellationToken cancellationToken = default)
        {
            var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

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
