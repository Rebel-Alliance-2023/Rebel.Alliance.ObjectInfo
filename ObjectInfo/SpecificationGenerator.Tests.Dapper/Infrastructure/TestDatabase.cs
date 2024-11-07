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

        public TestDatabase(ILogger logger)
        {
            _logger = logger;
            _dbPath = Path.Combine(Path.GetTempPath(), $"SpecGeneratorTests_{Guid.NewGuid():N}.db");
            _connectionString = $"Data Source={_dbPath};Cache=Shared";
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

                    // Create and upgrade the database
                    var upgrader = DeployChanges.To
                        .SQLiteDatabase(_connectionString)
                        .WithScriptsEmbeddedInAssembly(typeof(TestDatabase).Assembly)
                        .WithPreprocessor(new SQLitePreprocessor())
                        .LogTo(new DbUpLogger(_logger))
                        .Build();

                    var result = upgrader.PerformUpgrade();

                    if (!result.Successful)
                    {
                        throw new Exception("Database initialization failed", result.Error);
                    }

                    // Seed initial test data
                    using var connection = new SqliteConnection(_connectionString);
                    connection.Open();
                    SeedTestData(connection);

                    _initialized = true;
                    _logger.Information("Test database initialized successfully");
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
            _logger.Information("Seeding test data");

            try
            {
                // Begin transaction for all seeding operations
                using var transaction = connection.BeginTransaction();

                // Seed Products
                var products = TestDataGenerator.GenerateProducts();
                connection.Execute(@"
                    INSERT INTO Products (Name, SKU, Price, IsAvailable, Description, Category, 
                                       StockLevel, Weight, TagsJson, CreatedDate, LastRestockDate)
                    VALUES (@Name, @SKU, @Price, @IsAvailable, @Description, @Category, 
                           @StockLevel, @Weight, @TagsJson, @CreatedDate, @LastRestockDate)",
                    products, transaction);

                // Seed Customers
                var customers = TestDataGenerator.GenerateCustomers();
                connection.Execute(@"
                    INSERT INTO Customers (Name, Email, IsActive, DateCreated, LastModified, 
                                        CustomerType, CreditLimit, Notes, PreferredContactMethod, MetaData)
                    VALUES (@Name, @Email, @IsActive, @CreatedDate, @ModifiedDate, 
                           @CustomerType, @CreditLimit, @Notes, @PreferredContact, @MetaDataJson)",
                    customers, transaction);

                // Seed Orders and OrderItems
                var orders = TestDataGenerator.GenerateOrders(customers);
                foreach (var order in orders)
                {
                    var orderId = connection.QuerySingle<int>(@"
                        INSERT INTO Orders (OrderNumber, CustomerId, TotalAmount, Status, 
                                         OrderDate, ShippedDate, ShippingAddress, IsPriority)
                        VALUES (@OrderNumber, @CustomerId, @TotalAmount, @Status, 
                               @OrderDate, @ShippedDate, @ShippingAddress, @IsPriority);
                        SELECT last_insert_rowid();",
                        order, transaction);

                    var orderItems = TestDataGenerator.GenerateOrderItems(orderId, products);
                    connection.Execute(@"
                        INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice, 
                                             Discount, IsGift, Notes)
                        VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice, 
                               @Discount, @IsGift, @Notes)",
                        orderItems, transaction);
                }

                // Generate some audit logs
                var auditLogs = TestDataGenerator.GenerateAuditLogs(customers, orders);
                connection.Execute(@"
                    INSERT INTO Audits (EntityName, EntityId, Action, UserId, Timestamp, 
                                      OldValuesJson, NewValuesJson)
                    VALUES (@EntityName, @EntityId, @Action, @UserId, @Timestamp, 
                           @OldValuesJson, @NewValuesJson)",
                    auditLogs, transaction);

                transaction.Commit();
                _logger.Information("Test data seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to seed test data");
                throw;
            }
        }

        public void Dispose()
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

        private class DbUpLogger : IUpgradeLog
        {
            private readonly ILogger _logger;

            public DbUpLogger(ILogger logger)
            {
                _logger = logger;
            }

            public void WriteInformation(string message) => _logger.Information(message);
            public void WriteError(string message) => _logger.Error(message);
            public void WriteWarning(string message) => _logger.Warning(message);

            public void WriteInformation(string format, params object[] args)
            {
                throw new NotImplementedException();
            }

            public void WriteError(string format, params object[] args)
            {
                throw new NotImplementedException();
            }

            public void WriteWarning(string format, params object[] args)
            {
                throw new NotImplementedException();
            }
        }

        private class SQLitePreprocessor : IScriptPreprocessor
        {
            public string Process(string contents)
            {
                // Convert any SQL Server specific syntax to SQLite
                return contents
                    .Replace("NVARCHAR", "TEXT")
                    .Replace("VARCHAR", "TEXT")
                    .Replace("DECIMAL", "REAL")
                    .Replace("DATETIME2", "TEXT")
                    .Replace("IDENTITY(1,1)", "AUTOINCREMENT")
                    .Replace("GO", "");
            }
        }
    }
}
