using System.Collections.Concurrent;

namespace Rebel.Alliance.Specification.Dapper.Core
{
    public interface IParameterManager
    {
        string CreateParameter(object value);
        DynamicParameters GetParameters();
        void Clear();
    }

    public class ParameterManager : IParameterManager
    {
        private readonly ConcurrentDictionary<string, object> _parameters = new();
        private int _parameterCount;

        public string CreateParameter(object value)
        {
            var paramName = $"@p{Interlocked.Increment(ref _parameterCount)}";
            _parameters.TryAdd(paramName, value);
            return paramName;
        }

        public DynamicParameters GetParameters()
        {
            var parameters = new DynamicParameters();
            foreach (var param in _parameters)
            {
                parameters.Add(param.Key, param.Value);
            }
            return parameters;
        }

        public void Clear()
        {
            _parameters.Clear();
            _parameterCount = 0;
        }
    }
}
