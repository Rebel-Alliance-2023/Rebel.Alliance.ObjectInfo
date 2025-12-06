using System;
using System.Threading.Tasks;
using Dapper;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure.TestFixtures
{
    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        protected readonly DatabaseFixture Fixture;
        protected readonly ILogger Logger;
        protected readonly IServiceProvider ServiceProvider;
        protected readonly ITestOutputHelper Output;

        protected IntegrationTestBase(DatabaseFixture fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Output = output;
            ServiceProvider = fixture.ServiceProvider;
            
            // Configure logging
            Logger = ServiceProvider.GetRequiredService<ILogger>()
                .ForContext(GetType());

            TestContext.Configure(output);
        }

        public virtual Task InitializeAsync()
        {
            Logger.Information("Starting test: {TestName}", GetType().Name);
            return Task.CompletedTask;
        }

        public virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        protected Task<T> WithConnection<T>(Func<System.Data.IDbConnection, Task<T>> action)
        {
            return action(Fixture.Connection);
        }

        protected async Task WithConnection(Func<System.Data.IDbConnection, Task> action)
        {
            await action(Fixture.Connection);
        }

        protected async Task<T> ExecuteWithRetry<T>(Func<Task<T>> action, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex) when (i < maxRetries - 1)
                {
                    Logger.Warning(ex, "Operation failed, attempt {Attempt} of {MaxRetries}", i + 1, maxRetries);
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, i))); // Exponential backoff
                }
            }
            return await action(); // Last try without catch
        }

        protected void LogTestStart([System.Runtime.CompilerServices.CallerMemberName] string testName = "")
        {
            Logger.Information("Starting test method: {TestMethod}", testName);
        }

        protected void LogTestEnd([System.Runtime.CompilerServices.CallerMemberName] string testName = "")
        {
            Logger.Information("Completed test method: {TestMethod}", testName);
        }
    }
}
