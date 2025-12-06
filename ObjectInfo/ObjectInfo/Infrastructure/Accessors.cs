using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ObjectInfo.Infrastructure
{
    /// <summary>
    /// Factory helpers for creating fast, open-instance delegates for property getters and setters.
    /// Falls back to expression compilation for non-property member expressions (e.g., fields or indexers).
    /// </summary>
    internal static class Accessors
    {
        /// <summary>
        /// Creates a <see cref="Func{T,TResult}"/> getter delegate for a property access expression.
        /// If the expression targets a property, uses <see cref="Delegate.CreateDelegate(Type, MethodInfo)"/>
        /// for near-zero overhead delegate creation; otherwise, falls back to <see cref="LambdaExpression.Compile()"/>.
        /// </summary>
        /// <typeparam name="TTarget">Target (row) type.</typeparam>
        /// <typeparam name="TProp">Property value type.</typeparam>
        /// <param name="expr">Member access expression (e.g., <c>x =&gt; x.Property</c>).</param>
        /// <returns>Getter delegate for the property.</returns>
        public static Func<TTarget, TProp> CreateGetter<TTarget, TProp>(Expression<Func<TTarget, TProp>> expr)
        {
            if (expr.Body is MemberExpression me && me.Member is PropertyInfo pi && pi.GetMethod is MethodInfo get)
            {
                return (Func<TTarget, TProp>)Delegate.CreateDelegate(typeof(Func<TTarget, TProp>), get);
            }
            return expr.Compile();
        }

        /// <summary>
        /// Creates an <see cref="Action{T1,T2}"/> setter delegate for a property access expression, or <c>null</c>
        /// if the property has no setter or the expression does not target a property. Uses the fast
        /// <see cref="Delegate.CreateDelegate(Type, MethodInfo)"/> path when available.
        /// </summary>
        /// <typeparam name="TTarget">Target (row) type.</typeparam>
        /// <typeparam name="TProp">Property value type.</typeparam>
        /// <param name="expr">Member access expression (e.g., <c>x =&gt; x.Property</c>).</param>
        /// <returns>Setter delegate or <c>null</c> if unavailable.</returns>
        public static Action<TTarget, TProp> CreateSetter<TTarget, TProp>(Expression<Func<TTarget, TProp>> expr)
        {
            if (expr.Body is MemberExpression me && me.Member is PropertyInfo pi && pi.SetMethod is MethodInfo set)
            {
                return (Action<TTarget, TProp>)Delegate.CreateDelegate(typeof(Action<TTarget, TProp>), set);
            }
            return null;
        }
    }
}
