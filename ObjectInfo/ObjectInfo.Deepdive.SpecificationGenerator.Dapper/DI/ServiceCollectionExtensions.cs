using Microsoft.Extensions.DependencyInjection;
using Rebel.Alliance.Specification.Dapper.Configuration;
using Rebel.Alliance.Specification.Dapper.Core;
using Rebel.Alliance.Specification.Dapper.Infrastructure;

namespace Rebel.Alliance.Specification.Dapper.DI
{
    /// <summary>
    /// Extension methods for configuring Dapper specification services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Dapper specification support to the service collection
        /// </summary>
        public static IServiceCollection AddDapperSpecifications(
            this IServiceCollection services,
            Action<DapperSpecificationOptions>? configureOptions = null)
        {
            // Register options
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<DapperSpecificationOptions>(_ => { });
            }

            // Register core services
            services.AddSingleton<IConnectionManager, ConnectionManager>();
            services.AddSingleton<ITransactionManager, TransactionManager>();
            services.AddSingleton<IParameterManager, ParameterManager>();

            return services;
        }
    }
}
