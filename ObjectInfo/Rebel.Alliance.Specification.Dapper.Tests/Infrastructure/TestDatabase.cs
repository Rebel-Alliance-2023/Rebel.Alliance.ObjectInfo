using System;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;
using Dapper;
using DbUp;
using Serilog;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Models;
using DbUp.Engine.Output;
using DbUp.Engine;
using DbUp.SQLite;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure
{
    public class TestDatabase : IDisposable
    {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private readonly ILogger _logger;
        private readonly object _lock = new();
        private bool _initialized;
        private SqliteConnection? _connection;
        private bool disposedValue;

        public TestDatabase(ILogger logger)
        {
            _logger = logger;
            _dbPath = Path.Combine(Path.GetTempPath(), $"SpecGeneratorTests_{Guid.NewGuid():N}.db");
            _connectionString = $"Data Source={_dbPath};Mode=ReadWriteCreate;Cache=Shared";
            _initialized = false;
        }

        public IDbConnection GetConnection()
        {
            EnsureInitialized();
            
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                _connection = new SqliteConnection(_connectionString);
                _connection.Open();
            }
            
            return _connection;
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                try
                {
                    _logger.Information("Initializing test database at {DbPath}", _dbPath);

                    var scriptPath = Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Scripts", "schema-script.sql");
                    _logger.Information("Looking for migration script at: {Path}", scriptPath);

                    if (!File.Exists(scriptPath))
                    {
                        _logger.Error("Migration script not found at {Path}", scriptPath);
                        throw new FileNotFoundException("Migration script not found", scriptPath);
                    }

                    // Create and upgrade the database with a single script
                    var upgrader = DeployChanges.To
                        .SQLiteDatabase(_connectionString)
                        .WithScript("SchemaScript", File.ReadAllText(scriptPath))
                        .WithPreprocessor(new SQLitePreprocessor())
                        .LogTo(new SerilogDbUpLogger(_logger))
                        .Build();


                    var result = upgrader.PerformUpgrade();

                    if (!result.Successful)
                    {
                        throw new Exception("Database initialization failed", result.Error);
                    }

                    _logger.Information("Test database initialized successfully");
                    _initialized = true;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to initialize test database");
                    throw;
                }
            }
        }

        private void SeedTestData(IDbConnection connection)
        {
            _logger.Information("Starting test data seeding...");

            try
            {
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Verify tables exist
                    var tables = connection.Query<string>(
                        "SELECT name FROM sqlite_master WHERE type='table'");

                    _logger.Information("Existing tables: {Tables}",
                        string.Join(", ", tables));

                    // Generate and insert test data
                    var products = TestDataGenerator.GenerateProducts(50).ToList();
                    connection.Execute(@"
                INSERT INTO Products (Name, SKU, Price, IsAvailable, Description, 
                    Category, StockLevel, Weight, TagsJson, CreatedDate, LastRestockDate)
                VALUES (@Name, @SKU, @Price, @IsAvailable, @Description,
                    @Category, @StockLevel, @Weight, @TagsJson, @CreatedDate, @LastRestockDate)",
                        products,
                        transaction);

                    var customers = TestDataGenerator.GenerateCustomers(20).ToList();
                    // ... rest of seeding code ...

                    transaction.Commit();
                    _logger.Information("Test data seeding completed successfully");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _logger.Error(ex, "Error during test data seeding");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Critical error during test data seeding");
                throw;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        _connection?.Dispose();

                        if (File.Exists(_dbPath))
                        {
                            File.Delete(_dbPath);
                            _logger.Information("Test database deleted: {DbPath}", _dbPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Error cleaning up test database");
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    internal class SerilogDbUpLogger : IUpgradeLog
    {
        private readonly ILogger _logger;

        public SerilogDbUpLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void WriteInformation(string message) => _logger.Information(message);
        public void WriteError(string message) => _logger.Error(message);
        public void WriteWarning(string message) => _logger.Warning(message);

        public void WriteInformation(string format, params object[] args)
        {
            _logger.Information(format, args);
        }

        public void WriteError(string format, params object[] args)
        {
            _logger.Error(format, args);
        }

        public void WriteWarning(string format, params object[] args)
        {
            _logger.Warning(format, args);
        }
    }
}
