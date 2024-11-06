namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime
{
    /// <summary>
    /// Defines the contract for SQL-based specifications
    /// </summary>
    public interface ISqlSpecification<T> where T : class
    {
        /// <summary>
        /// Converts the specification to a SQL query
        /// </summary>
        string ToSql();

        /// <summary>
        /// Gets the SQL parameters for the query
        /// </summary>
        IDictionary<string, object> GetParameters();
    }
}
