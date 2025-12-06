using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;

namespace ObjectInfo.Infrastructure
{
    /// <summary>
    /// Describes the primitive shape/category of a generic value type <typeparamref name="T"/>,

    /// allowing fast switching between formatting and parsing behaviors without repeated reflection.
    /// </summary>
    /// <remarks>
    /// The values represent common UI/editor-relevant groupings. They are intentionally coarse-grained
    /// to keep hot-path branching cheap while still providing high-quality input/output behavior.
    /// </remarks>
    internal enum ValueKind
    {
        /// <summary>Boolean values (true/false).</summary>
        Boolean,
        /// <summary>Date-only semantic values (e.g., <see cref="DateOnly"/>).</summary>
        Date,
        /// <summary>Time-only semantic values (e.g., <see cref="TimeOnly"/>).</summary>
        Time,
        /// <summary>DateTime semantic values (e.g., <see cref="DateTime"/>).</summary>
        DateTime,
        /// <summary>32-bit integral numeric values (<see cref="int"/>).</summary>
        Int32,
        /// <summary>64-bit integral numeric values (<see cref="long"/>).</summary>
        Int64,
        /// <summary>High-precision decimal numeric values (<see cref="decimal"/>).</summary>
        Decimal,
        /// <summary>Double-precision floating point values (<see cref="double"/>).</summary>
        Double,
        /// <summary>Single-precision floating point values (<see cref="float"/>).</summary>
        Single,
        /// <summary>Enumeration types (including nullable enums).</summary>
        Enum,
        /// <summary>Strings.</summary>
        String,
        /// <summary>Any other value not covered by the specific categories.</summary>
        Other
    }

    

    /// <summary>
    /// Simple option pair for enum value/name lists.
    /// </summary>
    internal readonly struct SelectOption<T>
    {
        public SelectOption(T value, string name)
        {
            Value = value;
            Name = name;
        }

        public T Value { get; }
        public string Name { get; }
    }

    /// <summary>
    /// Provides cached type information and helpers for <typeparamref name="T"/> that are computed once per
    /// closed generic type and reused. This eliminates repeated calls to reflection in rendering and input hot paths.
    /// </summary>
    /// <typeparam name="T">The value type that the cached traits apply to.</typeparam>
    /// <remarks>
    /// Initialization is performed exactly once per closed generic type by the CLR and is thread-safe by design.
    /// After initialization, lookups are simple static field reads and switch statements.
    /// </remarks>
    internal static class TypeTraits<T>
    {
        /// <summary>
        /// The generic type parameter <typeparamref name="T"/> as a <see cref="Type"/> instance.
        /// </summary>
        public static readonly Type Type = typeof(T);

        /// <summary>
        /// When <typeparamref name="T"/> is nullable, contains the underlying non-nullable type; otherwise <c>null</c>.
        /// </summary>
        public static readonly Type NullableUnderlying = Nullable.GetUnderlyingType(Type);

        /// <summary>
        /// The non-nullable representation of <typeparamref name="T"/> (same as <see cref="Type"/> when not nullable).
        /// </summary>
        public static readonly Type NonNullableType = NullableUnderlying ?? Type;

        /// <summary>
        /// Indicates whether <typeparamref name="T"/> is a nullable type (i.e., <c>Nullable&lt;&gt;</c>).
        /// </summary>
        public static readonly bool IsNullable = NullableUnderlying != null;

        /// <summary>
        /// Indicates whether the non-nullable representation is an enum type.
        /// </summary>
        public static readonly bool IsEnum = NonNullableType.IsEnum;

        /// <summary>
        /// A cheap, precomputed categorization of <typeparamref name="T"/> used to drive parsing/formatting logic.
        /// </summary>
        public static readonly ValueKind Kind = ComputeKind(NonNullableType);

        /// <summary>
        /// Cached delegate that boxes a non-nullable value into its corresponding <c>Nullable&lt;&gt;</c> wrapper and
        /// returns it as <see cref="object"/>. This avoids using <see cref="Activator.CreateInstance(Type, object[])"/>
        /// on hot paths when dealing with nullable values.
        /// </summary>
        private static readonly Func<object, object> s_nullableBoxer = CreateNullableBoxer();

        /// <summary>
        /// Cached enum options computed once per closed generic type when applicable.
        /// Empty list for non-enum kinds to avoid allocations.
        /// </summary>
        private static readonly IReadOnlyList<SelectOption<T>> s_enumOptions = InitializeEnumOptions();

        private static ValueKind ComputeKind(Type t)
        {
            if (t == typeof(bool)) return ValueKind.Boolean;
            // DateOnly/TimeOnly are not supported on .NET Standard 2.0 target
            if (t == typeof(DateTime)) return ValueKind.DateTime;
            if (t == typeof(int)) return ValueKind.Int32;
            if (t == typeof(long)) return ValueKind.Int64;
            if (t == typeof(decimal)) return ValueKind.Decimal;
            if (t == typeof(double)) return ValueKind.Double;
            if (t == typeof(float)) return ValueKind.Single;
            if (t.IsEnum) return ValueKind.Enum;
            if (t == typeof(string)) return ValueKind.String;
            return ValueKind.Other;
        }

        /// <summary>
        /// Formats a value of <typeparamref name="T"/> for use in HTML input elements using stable, culture-invariant
        /// formats where required (e.g., date/time). For non-special types, falls back to <see cref="object.ToString"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="kindOverride">Optional explicit editor kind that affects Date vs DateTimeLocal formatting.</param>
        /// <param name="culture">Culture for numeric formatting when applicable (dates use invariant formats).</param>
        /// <returns>String representation appropriate for the input element's <c>value</c> attribute.</returns>
        public static string FormatForInput(T value, object kindOverride, CultureInfo culture)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(value, default(T))) return string.Empty;

            switch (Kind)
            {
                case ValueKind.DateTime:
                {
                    var dt = (DateTime)(object)value;
                    var isDateTimeLocal = kindOverride != null && string.Equals(kindOverride.ToString(), "DateTimeLocal", StringComparison.Ordinal);
                    return isDateTimeLocal
                        ? dt.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)
                        : dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                case ValueKind.Int32:
                case ValueKind.Int64:
                case ValueKind.Decimal:
                case ValueKind.Double:
                case ValueKind.Single:
                    return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                default:
                    return value != null ? value.ToString() : string.Empty;
            }
        }

        /// <summary>
        /// Converts a value of <typeparamref name="T"/> to a stable string suitable for <c>&lt;option value&gt;</c>
        /// attributes and radio input values, using invariant formats for date/time and numeric kinds.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="culture">Culture for numeric formatting when applicable (dates use invariant formats).</param>
        /// <returns>A stable, culture-invariant string representation for option values.</returns>
        public static string ToOptionValueString(T value, CultureInfo culture)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(value, default(T))) return string.Empty;

            switch (Kind)
            {
                case ValueKind.Enum:
                    return value != null ? value.ToString() : string.Empty; // enum name
                case ValueKind.DateTime:
                    return ((DateTime)(object)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                case ValueKind.Int32:
                case ValueKind.Int64:
                case ValueKind.Decimal:
                case ValueKind.Double:
                case ValueKind.Single:
                    return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                default:
                    return value != null ? value.ToString() : string.Empty;
            }
        }

        /// <summary>
        /// Attempts to parse a raw event value (from <see cref="Microsoft.AspNetCore.Components.ChangeEventArgs.Value"/>)
        /// into <typeparamref name="T"/> using fast, non-throwing paths for common kinds. For empty strings, returns
        /// <c>true</c> and sets <paramref name="parsed"/> to default (null for reference/nullable types).
        /// </summary>
        /// <param name="eventValue">The raw event value (often <see cref="string"/> or <see cref="bool"/>).</param>
        /// <param name="culture">Culture for numeric parsing when applicable.</param>
        /// <param name="parsed">Receives the parsed value when parsing succeeds or empty input is provided.</param>
        /// <returns>
        /// <c>true</c> when a parse attempt occurred (including empty-to-default); <c>false</c> only for unexpected
        /// failures (e.g., invalid enum name) where the value cannot be produced.
        /// </returns>
        public static bool TryParseFromEventValue(object eventValue, CultureInfo culture, out T parsed)
        {
            if (Kind == ValueKind.Boolean)
            {
                bool? pb = null;
                switch (eventValue)
                {
                    case bool b:
                        pb = b; break;
                    case string sb:
                        if (string.IsNullOrWhiteSpace(sb)) pb = null;
                        else if (sb == "on") pb = true;
                        else if (bool.TryParse(sb, out var b2)) pb = b2;
                        break;
                }
                parsed = pb == null ? default(T) : (T)(object)pb.Value;
                return true;
            }

            var s = eventValue != null ? eventValue.ToString() : null;
            if (string.IsNullOrWhiteSpace(s))
            {
                parsed = default(T);
                return true;
            }

            switch (Kind)
            {
                case ValueKind.Enum:
                {
                    try
                    {
                        var ev = Enum.Parse(NonNullableType, s, ignoreCase: true);
                        object boxed = ev;
                        if (IsNullable && s_nullableBoxer != null) boxed = s_nullableBoxer(ev);
                        parsed = (T)boxed;
                        return true;
                    }
                    catch
                    {
                        parsed = default(T);
                        return false;
                    }
                }
                case ValueKind.DateTime:
                {
                    // Span-based manual parsing to reduce overhead
                    // Supported inputs: "yyyy-MM-dd" (10) and "yyyy-MM-ddTHH:mm" (16)
                    if (s.Length == 10)
                    {
                        // yyyy-MM-dd
                        if (TryParseDateString(s, out var year, out var month, out var day))
                        {
                            var dt = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local);
                            parsed = (T)(object)dt; return true;
                        }
                        parsed = default(T); return true;
                    }
                    else if (s.Length == 16)
                    {
                        // yyyy-MM-ddTHH:mm
                        if (TryParseDateTimeLocalString(s, out var year, out var month, out var day, out var hour, out var minute))
                        {
                            var dt = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Local);
                            parsed = (T)(object)dt; return true;
                        }
                        parsed = default(T); return true;
                    }
                    // Fallback: attempt general parse
                    if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt2))
                    { parsed = (T)(object)dt2; return true; }
                    parsed = default(T); return true;
                }
                case ValueKind.Int32:
                    {
                        if (TryParseInt32Fast(s, out var i32))
                        { parsed = (T)(object)i32; return true; }
                        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out i32))
                        { parsed = (T)(object)i32; return true; }
                        parsed = default(T); return false;
                    }
                case ValueKind.Int64:
                    {
                        if (TryParseInt64Fast(s, out var i64))
                        { parsed = (T)(object)i64; return true; }
                        if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out i64))
                        { parsed = (T)(object)i64; return true; }
                        parsed = default(T); return false;
                    }
                case ValueKind.Decimal:
                    {
                        if (TryParseDecimalFast(s, out var dec))
                        { parsed = (T)(object)dec; return true; }
                        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out dec))
                        { parsed = (T)(object)dec; return true; }
                        parsed = default(T); return false;
                    }
                case ValueKind.Double:
                    {
                        if (TryParseDoubleFast(s, out var dbl))
                        { parsed = (T)(object)dbl; return true; }
                        if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out dbl))
                        { parsed = (T)(object)dbl; return true; }
                        parsed = default(T); return false;
                    }
                case ValueKind.Single:
                    {
                        if (TryParseFloatFast(s, out var fl))
                        { parsed = (T)(object)fl; return true; }
                        if (float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out fl))
                        { parsed = (T)(object)fl; return true; }
                        parsed = default(T); return false;
                    }
                case ValueKind.String:
                    parsed = (T)(object)s; return true;
                default:
                    try
                    {
                        var obj = Convert.ChangeType(s, NonNullableType, CultureInfo.InvariantCulture);
                        if (IsNullable && s_nullableBoxer != null) obj = s_nullableBoxer(obj);
                        parsed = (T)obj;
                        return true;
                    }
                    catch
                    {
                        parsed = default(T);
                        return false;
                    }
            }
        }

        /// <summary>
        /// Builds an immutable list of <see cref="SelectOption{T}"/> for enum types (including nullable enums).
        /// For non-enum types, returns <see cref="Array.Empty{T}()"/>.
        /// </summary>
        /// <returns>An immutable list of enum values and their string names.</returns>
        public static IReadOnlyList<SelectOption<T>> BuildEnumOptions()
        {
            return s_enumOptions;
        }

        /// <summary>
        /// Creates a compiled delegate that wraps a non-nullable <see cref="object"/> into <c>Nullable&lt;NonNullableType&gt;</c>
        /// and returns it as <see cref="object"/>. Returns <c>null</c> when <typeparamref name="T"/> is not nullable.
        /// </summary>
        private static Func<object, object> CreateNullableBoxer()
        {
            if (!IsNullable) return null;
            var inner = NonNullableType;
            var ctor = typeof(Nullable<>).MakeGenericType(inner).GetConstructor(new[] { inner });
            var objParam = Expression.Parameter(typeof(object), "o");
            var newExpr = Expression.New(ctor, Expression.Convert(objParam, inner));
            var body = Expression.Convert(newExpr, typeof(object));
            return Expression.Lambda<Func<object, object>>(body, objParam).Compile();
        }

        private static IReadOnlyList<SelectOption<T>> InitializeEnumOptions()
        {
            if (!IsEnum) return Array.Empty<SelectOption<T>>();
            var names = Enum.GetNames(NonNullableType);
            var values = Enum.GetValues(NonNullableType);
            var list = new List<SelectOption<T>>(names.Length);
            int i = 0;
            foreach (var v in values)
            {
                object boxed = v;
                if (IsNullable && s_nullableBoxer != null) boxed = s_nullableBoxer(v);
                list.Add(new SelectOption<T>((T)boxed, names[i++]));
            }
            return list;
        }

        /// <summary>
        /// Legacy helper retained for compatibility; prefer <see cref="s_nullableBoxer"/> for hot paths.
        /// </summary>
        private static object CreateNullable(Type innerType, object value)
            => Activator.CreateInstance(typeof(Nullable<>).MakeGenericType(innerType), value);

        // Fast numeric parse helpers
        private static bool TryParseDateString(string s, out int year, out int month, out int day)
        {
            year = month = day = 0;
            if (s == null || s.Length != 10) return false; // yyyy-MM-dd
            // yyyy
            if (!TryParse4DigitsString(s, 0, out year)) return false;
            // '-'
            if (s[4] != '-') return false;
            // MM
            if (!TryParse2DigitsString(s, 5, out month)) return false;
            if (s[7] != '-') return false;
            // dd
            if (!TryParse2DigitsString(s, 8, out day)) return false;
            // Basic validation
            if ((uint)month - 1u >= 12u) return false;
            if ((uint)day - 1u >= 31u) return false;
            return true;
        }

        private static bool TryParseDateTimeLocalString(string s, out int year, out int month, out int day, out int hour, out int minute)
        {
            year = month = day = hour = minute = 0;
            if (s == null || s.Length != 16) return false; // yyyy-MM-ddTHH:mm
            if (!TryParseDateString(s.Substring(0, 10), out year, out month, out day)) return false;
            if (s[10] != 'T') return false;
            if (!TryParse2DigitsString(s, 11, out hour)) return false;
            if (s[13] != ':') return false;
            if (!TryParse2DigitsString(s, 14, out minute)) return false;
            if ((uint)hour > 23u) return false;
            if ((uint)minute > 59u) return false;
            return true;
        }

        private static bool TryParse2DigitsString(string s, int start, out int value)
        {
            value = 0;
            if (s == null || s.Length < start + 2) return false;
            int d1 = s[start] - '0';
            int d2 = s[start + 1] - '0';
            if ((uint)d1 > 9u || (uint)d2 > 9u) return false;
            value = d1 * 10 + d2;
            return true;
        }

        private static bool TryParse4DigitsString(string s, int start, out int value)
        {
            value = 0;
            if (s == null || s.Length < start + 4) return false;
            int d1 = s[start] - '0';
            int d2 = s[start + 1] - '0';
            int d3 = s[start + 2] - '0';
            int d4 = s[start + 3] - '0';
            if ((uint)d1 > 9u || (uint)d2 > 9u || (uint)d3 > 9u || (uint)d4 > 9u) return false;
            value = (((d1 * 10) + d2) * 10 + d3) * 10 + d4;
            return true;
        }

        private static bool TryParseInt32Fast(string s, out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(s)) return false;
            int i = 0;
            bool neg = false;
            if (s[0] == '+' || s[0] == '-') { neg = s[0] == '-'; i = 1; }
            if (i >= s.Length) return false;
            int acc = 0;
            for (; i < s.Length; i++)
            {
                int d = s[i] - '0';
                if ((uint)d > 9u) return false;
                acc = acc * 10 + d;
            }
            value = neg ? -acc : acc;
            return true;
        }

        private static bool TryParseInt64Fast(string s, out long value)
        {
            value = 0;
            if (string.IsNullOrEmpty(s)) return false;
            int i = 0;
            bool neg = false;
            if (s[0] == '+' || s[0] == '-') { neg = s[0] == '-'; i = 1; }
            if (i >= s.Length) return false;
            long acc = 0;
            for (; i < s.Length; i++)
            {
                int d = s[i] - '0';
                if ((uint)d > 9u) return false;
                acc = acc * 10 + d;
            }
            value = neg ? -acc : acc;
            return true;
        }

        private static bool TryParseDoubleFast(string s, out double value)
        {
            value = 0d;
            if (string.IsNullOrEmpty(s)) return false;
            int i = 0;
            bool neg = false;
            if (s[0] == '+' || s[0] == '-') { neg = s[0] == '-'; i = 1; }
            if (i >= s.Length) return false;
            long intPart = 0;
            long fracPart = 0;
            int fracLen = 0;
            bool seenDot = false;
            for (; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '.')
                {
                    if (seenDot) return false;
                    seenDot = true;
                    continue;
                }
                int d = c - '0';
                if ((uint)d > 9u) return false;
                if (!seenDot)
                {
                    intPart = intPart * 10 + d;
                }
                else
                {
                    fracPart = fracPart * 10 + d;
                    fracLen++;
                }
            }
            double frac = fracLen > 0 ? fracPart / Math.Pow(10, fracLen) : 0d;
            double result = intPart + frac;
            value = neg ? -result : result;
            return true;
        }

        private static bool TryParseDecimalFast(string s, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrEmpty(s)) return false;
            int i = 0;
            bool neg = false;
            if (s[0] == '+' || s[0] == '-') { neg = s[0] == '-'; i = 1; }
            if (i >= s.Length) return false;
            long intPart = 0;
            long fracPart = 0;
            int fracLen = 0;
            bool seenDot = false;
            for (; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '.')
                {
                    if (seenDot) return false;
                    seenDot = true;
                    continue;
                }
                int d = c - '0';
                if ((uint)d > 9u) return false;
                if (!seenDot)
                {
                    intPart = intPart * 10 + d;
                }
                else
                {
                    fracPart = fracPart * 10 + d;
                    fracLen++;
                }
            }
            decimal frac = fracLen > 0 ? fracPart / (decimal)Math.Pow(10, fracLen) : 0m;
            decimal result = intPart + frac;
            value = neg ? -result : result;
            return true;
        }

        private static bool TryParseFloatFast(string s, out float value)
        {
            value = 0f;
            if (string.IsNullOrEmpty(s)) return false;
            int i = 0;
            bool neg = false;
            if (s[0] == '+' || s[0] == '-') { neg = s[0] == '-'; i = 1; }
            if (i >= s.Length) return false;
            long intPart = 0;
            long fracPart = 0;
            int fracLen = 0;
            bool seenDot = false;
            for (; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '.')
                {
                    if (seenDot) return false;
                    seenDot = true;
                    continue;
                }
                int d = c - '0';
                if ((uint)d > 9u) return false;
                if (!seenDot)
                {
                    intPart = intPart * 10 + d;
                }
                else
                {
                    fracPart = fracPart * 10 + d;
                    fracLen++;
                }
            }
            float frac = fracLen > 0 ? (float)(fracPart / Math.Pow(10, fracLen)) : 0f;
            float result = intPart + frac;
            value = neg ? -result : result;
            return true;
        }
    }
}
