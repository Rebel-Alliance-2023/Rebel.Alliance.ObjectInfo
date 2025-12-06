using Microsoft.Extensions.Options;
using Rebel.Alliance.Specification.Dapper.Configuration;

namespace Rebel.Alliance.Specification.Dapper.Infrastructure
{
    /// <summary>
    /// Manages database transactions for Dapper operations.
    /// </summary>
    public interface ITransactionManager
    {
        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <returns>The transaction.</returns>
        IDbTransaction BeginTransaction(IDbConnection connection);

        /// <summary>
        /// Begins a new database transaction asynchronously.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The transaction.</returns>
        Task<IDbTransaction> BeginTransactionAsync(IDbConnection connection, CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the transaction asynchronously.
        /// </summary>
        /// <param name="transaction">The transaction to commit.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task CommitAsync(IDbTransaction transaction, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the transaction asynchronously.
        /// </summary>
        /// <param name="transaction">The transaction to roll back.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task RollbackAsync(IDbTransaction transaction, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Default implementation of <see cref="ITransactionManager"/>.
    /// </summary>
    public class TransactionManager : ITransactionManager
    {
        private readonly DapperSpecificationOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionManager"/> class.
        /// </summary>
        /// <param name="options">The Dapper specification options.</param>
        public TransactionManager(IOptions<DapperSpecificationOptions> options)
        {
            _options = options.Value;
        }

        /// <inheritdoc/>
        public IDbTransaction BeginTransaction(IDbConnection connection)
        {
            return connection.BeginTransaction();
        }

        /// <inheritdoc/>
        public Task<IDbTransaction> BeginTransactionAsync(
            IDbConnection connection,
            CancellationToken cancellationToken = default)
        {
            // Most ADO.NET providers don't support async transactions
            // So we'll use sync version wrapped in Task
            return Task.FromResult(connection.BeginTransaction());
        }

        /// <inheritdoc/>
        public Task CommitAsync(IDbTransaction transaction, CancellationToken cancellationToken = default)
        {
            transaction.Commit();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RollbackAsync(IDbTransaction transaction, CancellationToken cancellationToken = default)
        {
            transaction.Rollback();
            return Task.CompletedTask;
        }
    }
}
