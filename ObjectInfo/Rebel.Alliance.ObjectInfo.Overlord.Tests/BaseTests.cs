using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;

namespace Rebel.Alliance.ObjectInfo.Overlord.Tests
{
    public abstract class BaseTests
    {
        protected readonly Microsoft.Extensions.Logging.ILogger Logger;
        protected readonly Serilog.ILogger SerilogLogger;
        private readonly SerilogLoggerFactory _loggerFactory;

        protected ILogger<T> GetLogger<T>() => _loggerFactory.CreateLogger<T>();

        protected BaseTests(ITestOutputHelper output)
        {
            SerilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.TestOutput(output, LogEventLevel.Debug)
                .CreateLogger();

            _loggerFactory = new SerilogLoggerFactory(SerilogLogger);
            Logger = _loggerFactory.CreateLogger(GetType().Name);
        }
    }
}
