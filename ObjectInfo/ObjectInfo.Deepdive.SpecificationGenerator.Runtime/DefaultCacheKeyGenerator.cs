using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery.Models;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching.Implementation
{
    /// <summary>
    /// Default implementation of cache key generation
    /// </summary>
    public class DefaultCacheKeyGenerator : ICacheKeyGenerator
    {
        public string GenerateKey<T>(ISpecification<T> specification) where T : class
        {
            var keyComponents = new List<string>
            {
                typeof(T).FullName ?? typeof(T).Name,
                ComputeSpecificationHash(specification)
            };

            // Handle advanced specifications
            if (specification is IAdvancedSpecification<T> advSpec)
            {
                keyComponents.Add(ComputeAdvancedSpecificationHash(advSpec));
            }

            return string.Join(":", keyComponents);
        }

        private string ComputeSpecificationHash<T>(ISpecification<T> specification) where T : class
        {
            var components = new Dictionary<string, object?>
            {
                { "criteria", specification.Criteria?.ToString() },
                { "includes", specification.Includes.Select(i => i.ToString()).ToList() },
                { "orderBy", specification.OrderBy?.ToString() },
                { "orderByDescending", specification.OrderByDescending?.ToString() },
                { "skip", specification.Skip },
                { "take", specification.Take }
            };

            return ComputeHash(components);
        }

        private string ComputeAdvancedSpecificationHash<T>(IAdvancedSpecification<T> specification) 
            where T : class
        {
            var components = new Dictionary<string, object?>
            {
                { "sortFields", specification.SortFields.Select(s => new 
                    {
                        s.PropertyName,
                        s.Direction,
                        s.Order
                    }).ToList() },
                { "filters", SerializeFilterGroup(specification.RootFilter) }
            };

            return ComputeHash(components);
        }

        private object SerializeFilterGroup(FilterGroup group)
        {
            return new
            {
                group.Operator,
                conditions = group.Conditions.Select(c => new
                {
                    c.PropertyName,
                    c.Operator,
                    value = c.Value?.ToString()
                }).ToList(),
                groups = group.Groups.Select(SerializeFilterGroup).ToList()
            };
        }

        private string ComputeHash(object value)
        {
            var json = JsonSerializer.Serialize(value, new JsonSerializerOptions 
            { 
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(bytes);
        }
    }
}
