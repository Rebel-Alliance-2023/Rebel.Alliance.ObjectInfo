using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery.Models
{
    /// <summary>
    /// Represents a group of filter conditions
    /// </summary>
    public class FilterGroup
    {
        /// <summary>
        /// Gets or sets the logical operator for this group
        /// </summary>
        public LogicalOperator Operator { get; set; } = LogicalOperator.And;

        /// <summary>
        /// Gets the list of filter conditions in this group
        /// </summary>
        public IList<FilterCondition> Conditions { get; } = new List<FilterCondition>();

        /// <summary>
        /// Gets the list of nested filter groups
        /// </summary>
        public IList<FilterGroup> Groups { get; } = new List<FilterGroup>();

        /// <summary>
        /// Adds a condition to the group
        /// </summary>
        public FilterGroup AddCondition(string propertyName, FilterOperator op, object? value = null)
        {
            Conditions.Add(new FilterCondition(propertyName, op, value));
            return this;
        }

        /// <summary>
        /// Adds a condition using an expression
        /// </summary>
        public FilterGroup AddCondition<T>(Expression<Func<T, bool>> expression) where T : class
        {
            Conditions.Add(new FilterCondition<T>(expression));
            return this;
        }

        /// <summary>
        /// Adds a nested group
        /// </summary>
        public FilterGroup AddGroup(Action<FilterGroup> groupConfig)
        {
            var group = new FilterGroup();
            groupConfig(group);
            Groups.Add(group);
            return this;
        }
    }

    /// <summary>
    /// Represents a single filter condition
    /// </summary>
    public class FilterCondition
    {
        /// <summary>
        /// Gets or sets the property name to filter on
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets or sets the filter operator
        /// </summary>
        public FilterOperator Operator { get; set; }

        /// <summary>
        /// Gets or sets the filter value
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Gets or sets a custom filter expression
        /// </summary>
        public LambdaExpression? CustomExpression { get; set; }

        public FilterCondition(string propertyName, FilterOperator op, object? value = null)
        {
            PropertyName = propertyName;
            Operator = op;
            Value = value;
        }
    }

    /// <summary>
    /// Type-safe filter condition
    /// </summary>
    public class FilterCondition<T> : FilterCondition where T : class
    {
        public FilterCondition(Expression<Func<T, bool>> expression)
            : base(string.Empty, FilterOperator.Custom)
        {
            CustomExpression = expression;
        }
    }

    /// <summary>
    /// Defines filter operators
    /// </summary>
    public enum FilterOperator
    {
        Equals,
        NotEquals,
        Contains,
        StartsWith,
        EndsWith,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        In,
        NotIn,
        IsNull,
        IsNotNull,
        Custom
    }

    /// <summary>
    /// Defines logical operators for combining filters
    /// </summary>
    public enum LogicalOperator
    {
        And,
        Or
    }
}
