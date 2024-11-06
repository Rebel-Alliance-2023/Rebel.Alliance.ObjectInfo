using System;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching
{
    /// <summary>
    /// Defines the contract for generating cache keys
    /// </summary>
    public interface ICacheKeyGenerator
    {
        /// <summary>
        /// Generates a cache key for a specification
        /// </summary>
        string GenerateKey<T>(ISpecification<T> specification) where T : class;
    }
}
