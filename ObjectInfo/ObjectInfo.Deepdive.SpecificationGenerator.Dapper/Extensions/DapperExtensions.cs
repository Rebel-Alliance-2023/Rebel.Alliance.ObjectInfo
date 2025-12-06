using Rebel.Alliance.Specification.Dapper.Core;

namespace Rebel.Alliance.Specification.Dapper.Extensions
{
    public static class DapperExtensions
    {
        public static async Task<IEnumerable<T>> QueryWithSpecificationAsync<T>(
            this IDbConnection connection,
            SqlSpecification<T> specification,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CancellationToken cancellationToken = default) where T : class
        {
            var sql = specification.ToSql();
            var parameters = specification.GetParameters();

            var command = new CommandDefinition(
                sql,
                parameters,
                transaction,
                commandTimeout,
                CommandType.Text,
                CommandFlags.None,
                cancellationToken);

            return await connection.QueryAsync<T>(command);
        }

        public static async Task<T?> FirstOrDefaultWithSpecificationAsync<T>(
            this IDbConnection connection,
            SqlSpecification<T> specification,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CancellationToken cancellationToken = default) where T : class
        {
            var sql = specification.ToSql();
            var parameters = specification.GetParameters();

            var command = new CommandDefinition(
                sql,
                parameters,
                transaction,
                commandTimeout,
                CommandType.Text,
                CommandFlags.None,
                cancellationToken);

            return await connection.QueryFirstOrDefaultAsync<T>(command);
        }

        public static async Task<int> CountWithSpecificationAsync<T>(
            this IDbConnection connection,
            SqlSpecification<T> specification,
            IDbTransaction? transaction = null,
            int? commandTimeout = null,
            CancellationToken cancellationToken = default) where T : class
        {
            var sql = $"SELECT COUNT(*) FROM ({specification.ToSql()}) AS CountQuery";
            var parameters = specification.GetParameters();

            var command = new CommandDefinition(
                sql,
                parameters,
                transaction,
                commandTimeout,
                CommandType.Text,
                CommandFlags.None,
                cancellationToken);

            return await connection.ExecuteScalarAsync<int>(command);
        }
    }
}
