using Rebel.Alliance.Specification.Dapper.Core;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure
{
    public abstract class SqlSpecificationBase<T> : SqlSpecification<T>, ISqlWhereClauseBuilder where T : class
    {
        public void AddToWhereClause(string clause)
        {
            AddWhereClause(clause);
        }

        //public void AddParameter(string name, object value)
        //{
        //    base._parameters.CreateParameter(name);

        //    Parameters[name] = value;
        //}
    }
}
