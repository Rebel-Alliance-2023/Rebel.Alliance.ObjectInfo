using DbUp;
using DbUp.Builder;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;
using Microsoft.Data.Sqlite;
using System.Data;


namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure
{
    public static class DbUpExtensions
    {
        public static UpgradeEngineBuilder SQLiteDatabase(
            this SupportedDatabases supported,
            string connectionString)
        {
            return DeployChanges.To
                .SqlDatabase(connectionString)
                .JournalToSqlTable("SchemaVersions", "dbo")
                .WithExecutionTimeout(TimeSpan.FromSeconds(180))
                .WithTransaction();
        }
    }

    internal class SqliteScriptExecutor : DbUp.Engine.IScriptExecutor
    {
        // Implementation details...
        public int? ExecutionTimeoutSeconds { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Execute(SqlScript script)
        {
            throw new NotImplementedException();
        }

        public void Execute(SqlScript script, IDictionary<string, string> variables)
        {
            throw new NotImplementedException();
        }

        public void VerifySchema()
        {
            throw new NotImplementedException();
        }
    }

    internal class SqliteConnectionManager : DbUp.Engine.Transactions.IConnectionManager
    {
        private readonly string _connectionString;

        public SqliteConnectionManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public TransactionMode TransactionMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsScriptOutputLogged { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void ExecuteCommandsWithManagedConnection(Action<Func<IDbCommand>> action)
        {
            throw new NotImplementedException();
        }

        public T ExecuteCommandsWithManagedConnection<T>(Func<Func<IDbCommand>, T> actionWithResult)
        {
            throw new NotImplementedException();
        }

        public IDisposable OperationStarting(IUpgradeLog upgradeLog, List<SqlScript> executedScripts)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> SplitScriptIntoCommands(string scriptContents)
        {
            throw new NotImplementedException();
        }

        public bool TryConnect(IUpgradeLog upgradeLog, out string errorMessage)
        {
            throw new NotImplementedException();
        }

        // Implementation details...
    }
}