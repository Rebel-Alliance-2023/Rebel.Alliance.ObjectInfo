using System;
using System.Linq.Expressions;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery.Models
{
    /// <summary>
    /// Represents a sort field in a specification
    /// </summary>
    public class SortField
    {
        /// <summary>
        /// Gets or sets the name of the property to sort by
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets or sets the sort direction
        /// </summary>
        public SortDirection Direction { get; set; }

        /// <summary>
        /// Gets or sets the order priority (lower numbers are applied first)
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets a custom sort expression
        /// </summary>
        public Expression? CustomExpression { get; set; }

        /// <summary>
        /// Creates a new sort field instance
        /// </summary>
        public SortField(string propertyName, SortDirection direction = SortDirection.Ascending, int order = 0)
        {
            PropertyName = propertyName;
            Direction = direction;
            Order = order;
        }

        /// <summary>
        /// Creates a new sort field instance with a custom expression
        /// </summary>
        public SortField(Expression customExpression, SortDirection direction = SortDirection.Ascending, int order = 0)
        {
            CustomExpression = customExpression;
            Direction = direction;
            Order = order;
        }
    }

    /// <summary>
    /// Defines sort directions
    /// </summary>
    public enum SortDirection
    {
        Ascending,
        Descending
    }
}
