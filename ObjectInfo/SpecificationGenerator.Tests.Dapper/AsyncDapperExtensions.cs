using System.Data;
using System.Threading.Tasks;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper
{
    public static class AsyncDapperExtensions
    {
        public static Task CommitAsync(this IDbTransaction transaction)
        {
            transaction.Commit();
            return Task.CompletedTask;
        }

        public static Task RollbackAsync(this IDbTransaction transaction)
        {
            transaction.Rollback();
            return Task.CompletedTask;
        }
    }
}