using System.Collections.Concurrent;

namespace Rebel.Alliance.Specification.Dapper.Core
{
    /// <summary>
    /// Manages SQL parameters for Dapper queries.
    /// </summary>
    public interface IParameterManager
    {
        /// <summary>
        /// Creates a named parameter and stores the value.
        /// </summary>
        /// <param name="value">The parameter value.</param>
        /// <returns>The generated parameter name.</returns>
        string CreateParameter(object value);

        /// <summary>
        /// Gets all stored parameters as a <see cref="DynamicParameters"/> object.
        /// </summary>
        /// <returns>The dynamic parameters for use with Dapper.</returns>
        DynamicParameters GetParameters();

        /// <summary>
        /// Clears all stored parameters.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Thread-safe implementation of <see cref="IParameterManager"/>.
    /// </summary>
    public class ParameterManager : IParameterManager
    {
        private readonly ConcurrentDictionary<string, object> _parameters = new();
        private int _parameterCount;

        /// <inheritdoc/>
        public string CreateParameter(object value)
        {
            var paramName = $"@p{Interlocked.Increment(ref _parameterCount)}";
            _parameters.TryAdd(paramName, Normalize(value));
            return paramName;
        }

        /// <inheritdoc/>
        public DynamicParameters GetParameters()
        {
            var parameters = new DynamicParameters();
            foreach (var param in _parameters)
            {
                parameters.Add(param.Key, param.Value);
            }
            return parameters;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            _parameters.Clear();
            _parameterCount = 0;
        }

        private static object Normalize(object value)
        {
            if (value == null) return DBNull.Value;
            var t = value.GetType();
            if (t.IsEnum)
            {
                var underlying = Enum.GetUnderlyingType(t);
                return Convert.ChangeType(value, underlying, System.Globalization.CultureInfo.InvariantCulture);
            }
            return value;
        }
    }
}
