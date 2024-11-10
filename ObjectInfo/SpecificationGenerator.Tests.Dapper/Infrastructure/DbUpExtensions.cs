using DbUp;
using DbUp.Builder;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;
using DbUp.SQLite;
using Microsoft.Data.Sqlite;
using Serilog;
using System.Data;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure
{
    public static class DbUpExtensions
    {
        public static UpgradeEngineBuilder SQLiteDatabase(
            this SupportedDatabases supported,
            string connectionString)
        {
            var builder = DeployChanges.To
                .SqlDatabase(connectionString);  // Store the builder

            // Configure settings
            builder.Configure(settings =>
            {
                settings.ConnectionManager = new SqliteConnectionManager(connectionString);
                settings.ScriptExecutor = new SqliteScriptExecutor(connectionString);
                settings.Journal = new SQLiteTableJournal(
                    () => new SqliteConnectionManager(connectionString),
                    () => new SerilogDbUpLogger(Log.Logger),
                    "SchemaVersions");
            });

            // Add additional configurations
            builder = builder
                .WithPreprocessor(new SQLitePreprocessor())
                .WithExecutionTimeout(TimeSpan.FromSeconds(180))
                .WithTransaction();

            return builder;
        }

        internal class SqliteConnectionManager : IConnectionManager
        {
            private readonly string _connectionString;

            public SqliteConnectionManager(string connectionString)
            {
                _connectionString = connectionString;
            }

            public TransactionMode TransactionMode { get; set; } = TransactionMode.SingleTransaction;
            public bool IsScriptOutputLogged { get; set; } = true;

            public void ExecuteCommandsWithManagedConnection(Action<Func<IDbCommand>> action)
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                action(() => connection.CreateCommand());
            }

            public T ExecuteCommandsWithManagedConnection<T>(Func<Func<IDbCommand>, T> actionWithResult)
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                return actionWithResult(() => connection.CreateCommand());
            }

            public IDisposable OperationStarting(IUpgradeLog upgradeLog, List<SqlScript> executedScripts)
            {
                return new DisposableOperation();
            }

            public IEnumerable<string> SplitScriptIntoCommands(string scriptContents)
            {
                return new[] { scriptContents };
            }

            public bool TryConnect(IUpgradeLog upgradeLog, out string errorMessage)
            {
                try
                {
                    using var connection = new SqliteConnection(_connectionString);
                    connection.Open();
                    errorMessage = string.Empty;
                    return true;
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    return false;
                }
            }

            private class DisposableOperation : IDisposable
            {
                public void Dispose() { }
            }
        }

        internal class SqliteScriptExecutor : IScriptExecutor
        {
            private readonly string _connectionString;

            public SqliteScriptExecutor(string connectionString)
            {
                _connectionString = connectionString;
            }

            public int? ExecutionTimeoutSeconds { get; set; }

            public void Execute(SqlScript script)
            {
                Execute(script, new Dictionary<string, string>());
            }

            public void Execute(SqlScript script, IDictionary<string, string> variables)
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                using var transaction = connection.BeginTransaction();
                try
                {
                    using var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = script.Contents;
                    command.CommandTimeout = ExecutionTimeoutSeconds ?? 30;

                    foreach (var variable in variables)
                    {
                        command.CommandText = command.CommandText.Replace($"$${variable.Key}$$", variable.Value);
                    }

                    command.ExecuteNonQuery();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            public void VerifySchema()
            {
                // No schema verification needed for SQLite
            }
        }

        internal class SQLiteTableJournal : IJournal
        {
            private readonly string _tableName;
            private readonly Func<IConnectionManager> _connectionManager;
            private readonly Func<IUpgradeLog> _log;

            public SQLiteTableJournal(Func<IConnectionManager> connectionManager, Func<IUpgradeLog> log, string tableName)
            {
                _connectionManager = connectionManager;
                _log = log;
                _tableName = tableName;
            }

            public string[] GetExecutedScripts()
            {
                EnsureTableExistsAndIsLatestVersion(null);

                var scripts = new List<string>();
                _connectionManager().ExecuteCommandsWithManagedConnection(dbCommandFactory =>
                {
                    var command = dbCommandFactory();
                    command.CommandText = $"SELECT ScriptName FROM {_tableName} ORDER BY SchemaVersionID";
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        scripts.Add((string)reader[0]);
                    }
                });

                return scripts.ToArray();
            }

            public void StoreExecutedScript(SqlScript script, Func<IDbCommand> dbCommandFactory)
            {
                var command = dbCommandFactory();
                command.CommandText = $"INSERT INTO {_tableName} (ScriptName, Applied) VALUES ($scriptName, $applied)";

                var scriptNameParam = command.CreateParameter();
                scriptNameParam.ParameterName = "$scriptName";
                scriptNameParam.Value = script.Name;
                command.Parameters.Add(scriptNameParam);

                var appliedParam = command.CreateParameter();
                appliedParam.ParameterName = "$applied";
                appliedParam.Value = DateTime.UtcNow;
                command.Parameters.Add(appliedParam);

                command.ExecuteNonQuery();
            }

            public void EnsureTableExistsAndIsLatestVersion(Func<IDbCommand> dbCommandFactory)
            {
                _log().WriteInformation($"Checking whether journal table {_tableName} exists..");

                _connectionManager().ExecuteCommandsWithManagedConnection(commandFactory =>
                {
                    // Check if table exists
                    var command = commandFactory();
                    command.CommandText = $@"
                SELECT name 
                FROM sqlite_master 
                WHERE type='table' 
                AND name='{_tableName}'";

                    var tableName = command.ExecuteScalar() as string;

                    if (string.IsNullOrEmpty(tableName))
                    {
                        _log().WriteInformation($"Journal table {_tableName} does not exist. Creating it...");

                        command = commandFactory();
                        command.CommandText = $@"
                    CREATE TABLE {_tableName} (
                        SchemaVersionID INTEGER PRIMARY KEY AUTOINCREMENT,
                        ScriptName TEXT NOT NULL,
                        Applied DATETIME NOT NULL
                    )";
                        command.ExecuteNonQuery();

                        _log().WriteInformation($"Journal table {_tableName} created.");
                    }
                    else
                    {
                        // Verify table schema is up to date
                        command = commandFactory();
                        command.CommandText = $@"
                    SELECT sql 
                    FROM sqlite_master 
                    WHERE type='table' 
                    AND name='{_tableName}'";

                        var tableDefinition = command.ExecuteScalar() as string;

                        // Check if we need to update the schema
                        if (!tableDefinition.Contains("SchemaVersionID") ||
                            !tableDefinition.Contains("ScriptName") ||
                            !tableDefinition.Contains("Applied"))
                        {
                            _log().WriteInformation($"Updating journal table {_tableName} schema...");

                            // SQLite doesn't support ALTER TABLE ADD COLUMN with PRIMARY KEY
                            // So we need to recreate the table
                            command = commandFactory();
                            command.CommandText = $@"
                        BEGIN TRANSACTION;

                        CREATE TABLE {_tableName}_new (
                            SchemaVersionID INTEGER PRIMARY KEY AUTOINCREMENT,
                            ScriptName TEXT NOT NULL,
                            Applied DATETIME NOT NULL
                        );

                        INSERT INTO {_tableName}_new (ScriptName, Applied)
                        SELECT ScriptName, COALESCE(Applied, DATETIME('now'))
                        FROM {_tableName};

                        DROP TABLE {_tableName};

                        ALTER TABLE {_tableName}_new RENAME TO {_tableName};

                        COMMIT;";

                            command.ExecuteNonQuery();

                            _log().WriteInformation($"Journal table {_tableName} schema updated.");
                        }
                    }
                });
            }
        }
    }
}
