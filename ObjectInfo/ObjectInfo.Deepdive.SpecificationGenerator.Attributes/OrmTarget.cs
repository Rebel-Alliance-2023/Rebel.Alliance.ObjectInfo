namespace ObjectInfo.Deepdive.SpecificationGenerator.Attributes
{
    /// <summary>
    /// Specifies the target ORM framework for specification generation.
    /// </summary>
    public enum OrmTarget
    {
        /// <summary>
        /// Generate specifications for Entity Framework Core.
        /// </summary>
        EntityFrameworkCore,

        /// <summary>
        /// Generate specifications for Dapper.
        /// </summary>
        Dapper,

        /// <summary>
        /// Generate specifications for both Entity Framework Core and Dapper.
        /// </summary>
        Both
    }
}