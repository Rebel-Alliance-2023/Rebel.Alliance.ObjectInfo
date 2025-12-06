#region Copyright (c) The Rebel Alliance
// ----------------------------------------------------------------------------------
// Copyright (c) The Rebel Alliance
//⠀⠀⠀⠀⠀⠀⠀⠀⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠠⠀⠀⠀⡇⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠈⠳⣴⣿⠄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠒⠒⠒⠒⠒⢺⢿⣿⢗⠒⠒⠒⠒⠒⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⠀⠁⣸⣿⣦⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⠀⢀⣾⡟⠋⢹⣷⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⢀⣿⡟⣴⣶⡄⣿⣧⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⢰⣿⣿⣧⢻⣿⣿⣿⣿⡟⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠈⢻⣿⣿⣷⣿⣿⣿⣿⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⢸⠿⣿⣿⣿⣿⣿⣿⣦⣤⣀⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⣾⠀⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣶⣤⡀⠀⠀⠀⠀⠀
//⠀⠀⠀⠀⣿⣏⣻⣿⣿⣿⣿⣿⠋⣿⣿⣿⣿⣿⠙⣿⣷⣶⣤⣤⡄
//⠀⠀⠀⠀⢻⢇⣿⣿⣿⣿⣿⠹⠀⢹⣿⣿⣿⡇⠀⢟⣿⣿⡿⠋⠀
//⠀⠀⠀⠀⢘⣼⣿⣿⣿⣿⣿⡆⠀⢸⣿⠛⣿⡇⠀⢸⡿⠋⠀⠀⠀
//⠀⠀⠀⠀⣾⣿⣿⣿⣿⣿⣿⣿⣦⣈⠻⠴⠟⣁⣴⣿⣿⠗⠀⠀⠀
//⠀⠀⠀⢸⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠋⠀⠀⠀⠀
//⠀⠀⢀⣿⣿⠻⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠟⠁⠀⠀⠀⠀⠀
//⠀⠀⣾⣿⡏⠀⠹⣿⠿⠿⠿⠿⣿⣿⣿⠿⠛⠁⠀⠀⠀⠀⠀⠀⠀
//⠀⢰⣿⡿⠀⠀⠀⠀⠀⠀⠀⠀⠀⢿⣿⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⠀⣿⣿⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠘⣿⣧⠀⠀⠀⠀⠀⠀⠀⠀⠀
//⣰⣿⡏⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢿⣿⣄⠀⠀⠀⠀⠀⠀⠀⠀
//⠉⠉⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠉⠉⠀
// ---------------------------------------------------------------------------------- 
#endregion

using ObjectInfo.Models.TypeInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using ObjectInfo.Infrastructure;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace ObjectInfo.Models.PropInfo
{
    public class PropInfo : IPropInfo
    {
        public bool CanRead { get; set; }

        public bool CanWrite { get; set; }

        public string PropertyType { get; set; }

        public string DeclaringType { get; set; }

        public string Name { get; set; }

        public string ReflectedType { get; set; }

        public object Value { get; set; }
        public List<ITypeInfo> CustomAttrs { get; set; }

        private static readonly ConcurrentDictionary<Type, Func<object, object, CultureInfo, string>> s_formatForInputCache = new ConcurrentDictionary<Type, Func<object, object, CultureInfo, string>>();
        private static readonly ConcurrentDictionary<Type, Func<object, CultureInfo, string>> s_toOptionValueCache = new ConcurrentDictionary<Type, Func<object, CultureInfo, string>>();

        /// <summary>
        /// Formats the current <see cref="Value"/> using invariant, stable rules provided by <see cref="TypeTraits{T}"/>.
        /// This is intended for UI and logging where culture-independent output is required.
        /// 
        /// Behavior:
        /// - DateTime: "yyyy-MM-dd" or "yyyy-MM-ddTHH:mm" when kindOverride is "DateTimeLocal".
        /// - Numeric: invariant formatting (no locale-specific thousands or decimal separators).
        /// - Enum: stable name.
        /// - Others: falls back to ToString().
        /// </summary>
        /// <param name="kindOverride">Optional editor-kind hint (e.g., "DateTimeLocal") to influence DateTime formatting.</param>
        /// <param name="culture">Culture for numeric formatting when applicable (dates use invariant formats). Pass null to use InvariantCulture.</param>
        /// <returns>Invariant, stable string representation of the value.</returns>
        public string GetFormattedValue(object kindOverride = null, CultureInfo culture = null)
        {
            if (Value == null) return string.Empty;
            var runtimeType = Value.GetType();
            var nonNullable = Nullable.GetUnderlyingType(runtimeType) ?? runtimeType;

            var useCulture = culture ?? CultureInfo.InvariantCulture;

            // Try cached compiled delegate for FormatForInput
            var formatter = s_formatForInputCache.GetOrAdd(nonNullable, t =>
            {
                var traitsType = typeof(TypeTraits<>).MakeGenericType(t);
                var method = traitsType.GetMethod("FormatForInput", BindingFlags.Public | BindingFlags.Static);
                if (method == null) return null;
                // Build lambda: (object value, object kindOverride, CultureInfo culture) => TypeTraits<T>.FormatForInput((T)value, kindOverride, culture)
                var valueParam = Expression.Parameter(typeof(object), "value");
                var kindParam = Expression.Parameter(typeof(object), "kind");
                var cultureParam = Expression.Parameter(typeof(CultureInfo), "culture");
                var call = Expression.Call(method,
                    Expression.Convert(valueParam, t),
                    kindParam,
                    cultureParam);
                var lambda = Expression.Lambda<Func<object, object, CultureInfo, string>>(call, valueParam, kindParam, cultureParam);
                return lambda.Compile();
            });

            if (formatter != null)
            {
                var s = formatter(Value, kindOverride, useCulture);
                return s ?? string.Empty;
            }

            // Fallback to ToOptionValueString via cached delegate
            var toOption = s_toOptionValueCache.GetOrAdd(nonNullable, t =>
            {
                var traitsType = typeof(TypeTraits<>).MakeGenericType(t);
                var method = traitsType.GetMethod("ToOptionValueString", BindingFlags.Public | BindingFlags.Static);
                if (method == null) return null;
                var valueParam = Expression.Parameter(typeof(object), "value");
                var cultureParam = Expression.Parameter(typeof(CultureInfo), "culture");
                var call = Expression.Call(method,
                    Expression.Convert(valueParam, t),
                    cultureParam);
                var lambda = Expression.Lambda<Func<object, CultureInfo, string>>(call, valueParam, cultureParam);
                return lambda.Compile();
            });

            if (toOption != null)
            {
                var s = toOption(Value, useCulture);
                return s ?? string.Empty;
            }

            return Value.ToString() ?? string.Empty;
        }
    }
}
