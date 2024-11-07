using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Serilog;
using Serilog.Events;
using Xunit;
using Serilog.Sinks.XUnit;
using Microsoft.Extensions.DependencyInjection;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching.Implementation;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching.Configuration;
using Xunit.Abstractions;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure.TestFixtures
{
    public class DatabaseFixture : IAsyncLifetime
    {
        private TestDatabase? _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        
        public IDbConnection Connection => _db?.GetConnection() 
            ?? throw new InvalidOperationException("Database not initialized");

        public IServiceProvider ServiceProvider => _serviceProvider;

        public DatabaseFixture()
        {
            // Configure logging
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(new TestOutputConverter(), LogEventLevel.Debug)
                .CreateLogger()
                .ForContext<DatabaseFixture>();

            // Configure services
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Add logging
            services.AddSingleton<ILogger>(_logger);

            // Add caching services
            services.Configure<SpecificationCacheOptions>(options =>
            {
                options.DefaultDuration = TimeSpan.FromMinutes(5);
                options.EnableDistributedCache = false;
                options.MaxMemoryCacheItems = 1000;
            });

            services.AddMemoryCache();
            services.AddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();
            services.AddSingleton<ICacheStatistics, DefaultCacheStatistics>();
            services.AddSingleton<ISpecificationCache, SpecificationCache>();

            // Add database
            services.AddSingleton(sp => _db ?? throw new InvalidOperationException("Database not initialized"));
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.Information("Initializing database fixture");
                _db = new TestDatabase(_logger);
                
                // Verify database setup
                using var connection = _db.GetConnection();
                var tableCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='table'");
                
                _logger.Information("Database initialized with {TableCount} tables", tableCount);

                // Log some statistics about the test data
                var stats = await GetDatabaseStats(connection);
                LogDatabaseStats(stats);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initialize database fixture");
                throw;
            }
        }

        public Task DisposeAsync()
        {
            _logger.Information("Disposing database fixture");
            _db?.Dispose();
            return Task.CompletedTask;
        }

        public async Task ResetDatabase()
        {
            _logger.Information("Resetting database to initial state");
            
            try
            {
                using var connection = Connection;
                using var transaction = connection.BeginTransaction();

                // Delete all data from tables in correct order
                await connection.ExecuteAsync("DELETE FROM OrderItems", transaction: transaction);
                await connection.ExecuteAsync("DELETE FROM Orders", transaction: transaction);
                await connection.ExecuteAsync("DELETE FROM Products", transaction: transaction);
                await connection.ExecuteAsync("DELETE FROM Customers", transaction: transaction);
                await connection.ExecuteAsync("DELETE FROM Audits", transaction: transaction);

                // Reset auto-increment counters
                await connection.ExecuteAsync("DELETE FROM sqlite_sequence", transaction: transaction);

                transaction.Commit();

                // Re-seed the data
                _db = new TestDatabase(_logger);
                
                _logger.Information("Database reset completed successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to reset database");
                throw;
            }
        }

        private async Task<DatabaseStats> GetDatabaseStats(IDbConnection connection)
        {
            var stats = new DatabaseStats();
            
            try
            {
                stats.CustomerCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Customers");
                
                stats.ProductCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Products");
                
                stats.OrderCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Orders");
                
                stats.OrderItemCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM OrderItems");
                
                stats.AuditCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Audits");

                stats.TotalOrderValue = await connection.ExecuteScalarAsync<decimal>(
                    "SELECT COALESCE(SUM(TotalAmount), 0) FROM Orders");
                
                stats.ActiveCustomers = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Customers WHERE IsActive = 1");
                
                stats.AvailableProducts = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Products WHERE IsAvailable = 1");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to gather database statistics");
            }

            return stats;
        }

        private void LogDatabaseStats(DatabaseStats stats)
        {
            _logger.Information("Database Statistics:");
            _logger.Information(" - Customers: {Count} ({ActiveCount} active)", 
                stats.CustomerCount, stats.ActiveCustomers);
            _logger.Information(" - Products: {Count} ({AvailableCount} available)", 
                stats.ProductCount, stats.AvailableProducts);
            _logger.Information(" - Orders: {Count} (${TotalValue:N2} total value)", 
                stats.OrderCount, stats.TotalOrderValue);
            _logger.Information(" - Order Items: {Count}", stats.OrderItemCount);
            _logger.Information(" - Audit Logs: {Count}", stats.AuditCount);
        }

        private class TestOutputConverter : ITestOutputHelper
        {
            public void WriteLine(string message) => TestContext.WriteLine(message);

            public void WriteLine(string format, params object[] args)
            {
                throw new NotImplementedException();
            }
        }

        private class DatabaseStats
        {
            public int CustomerCount { get; set; }
            public int ProductCount { get; set; }
            public int OrderCount { get; set; }
            public int OrderItemCount { get; set; }
            public int AuditCount { get; set; }
            public decimal TotalOrderValue { get; set; }
            public int ActiveCustomers { get; set; }
            public int AvailableProducts { get; set; }
        }
    }

    public static class TestContext
    {
        private static ITestOutputHelper? _outputHelper;

        public static void Configure(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        public static void WriteLine(string message)
        {
            _outputHelper?.WriteLine(message);
        }
    }
}
