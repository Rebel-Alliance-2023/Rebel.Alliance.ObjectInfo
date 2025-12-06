namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Dapper.Infrastructure
{
    public interface ISqlWhereClauseBuilder
    {
        void AddToWhereClause(string clause);
    }
}
