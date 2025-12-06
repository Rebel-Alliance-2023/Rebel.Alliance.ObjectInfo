using Microsoft.Extensions.Options;
using Rebel.Alliance.Specification.Dapper.Configuration;

namespace Rebel.Alliance.Specification.Dapper.Infrastructure
{
    public interface ITransactionManager
    {
        IDbTransaction BeginTransaction(IDbConnection connection);
        Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection, CancellationToken cancellationToken = default);
        Task CommitAsync(IDbTransaction transaction, CancellationToken cancellationToken = default);
        Task RollbackAsync(IDbTransaction transaction, CancellationToken cancellationToken = default);
    }

    public class TransactionManager : ITransactionManager
    {
        private readonly DapperSpecificationOptions _options;

        public TransactionManager(IOptions<DapperSpecificationOptions> options)
        {
            _options = options.Value;
        }

        public IDbTransaction BeginTransaction(IDbConnection connection)
        {
            return connection.BeginTransaction();
        }

        public Task<IDbTransaction> BeginTransactionAsync(
            IDbConnection connection,
            CancellationToken cancellationToken = default)
        {
            // Most ADO.NET providers don't support async transactions
            // So we'll use sync version wrapped in Task
            return Task.FromResult(connection.BeginTransaction());
        }

        public Task CommitAsync(IDbTransaction transaction, CancellationToken cancellationToken = default)
        {
            transaction.Commit();
            return Task.CompletedTask;
        }

        public Task RollbackAsync(IDbTransaction transaction, CancellationToken cancellationToken = default)
        {
            transaction.Rollback();
            return Task.CompletedTask;
        }
    }
}
