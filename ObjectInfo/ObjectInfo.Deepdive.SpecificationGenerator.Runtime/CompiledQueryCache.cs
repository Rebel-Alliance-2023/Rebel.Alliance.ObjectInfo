using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.AdvancedQuery.Models;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching.Configuration;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Runtime.Caching.Implementation
{
    /// <summary>
    /// Caches compiled queries for improved performance
    /// </summary>
    public class CompiledQueryCache : ICompiledQueryCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly CompiledQueryCacheOptions _options;
        private readonly ICacheKeyGenerator _keyGenerator;
        private readonly ICacheStatistics _statistics;
        private readonly Timer _cleanupTimer;

        public CompiledQueryCache(
            IOptions<CompiledQueryCacheOptions> options,
            ICacheKeyGenerator keyGenerator,
            ICacheStatistics statistics)
        {
            _cache = new ConcurrentDictionary<string, CacheEntry>();
            _options = options.Value;
            _keyGenerator = keyGenerator;
            _statistics = statistics;

            _cleanupTimer = new Timer(
                CleanupCache,
                null,
                _options.CleanupInterval,
                _options.CleanupInterval);
        }

        public Func<IQueryable<T>, IQueryable<T>> GetOrAddQueryTransformer<T>(
            ISpecification<T> specification,
            CancellationToken cancellationToken = default) where T : class
        {
            var key = _keyGenerator.GenerateKey(specification) + "_transformer";
            var entry = _cache.GetOrAdd(key, _ => new CacheEntry(BuildQueryTransformer(specification)));

            UpdateStatistics(entry);

            if (entry.Value is Func<IQueryable<T>, IQueryable<T>> transformer)
            {
                return transformer;
            }

            throw new InvalidOperationException("Cached value is not a query transformer");
        }

        public string GetOrAddSqlQuery<T>(
            ISpecification<T> specification,
            CancellationToken cancellationToken = default) where T : class
        {
            var key = _keyGenerator.GenerateKey(specification) + "_sql";
            var entry = _cache.GetOrAdd(key, _ => new CacheEntry(BuildSqlQuery(specification)));

            UpdateStatistics(entry);

            if (entry.Value is string sql)
            {
                return sql;
            }

            throw new InvalidOperationException("Cached value is not a SQL query");
        }

        public void InvalidateQueriesForType<T>() where T : class
        {
            var typePrefix = typeof(T).FullName;
            var keysToRemove = _cache.Keys.Where(k => k.StartsWith(typePrefix!));

            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
                _statistics.RecordEviction();
            }
        }

        public void InvalidateAllQueries()
        {
            _cache.Clear();
            _statistics.RecordEviction();
        }


        private Func<IQueryable<T>, IQueryable<T>> BuildQueryTransformer<T>(ISpecification<T> specification)
            where T : class
        {
            return query =>
            {
                var result = query;

                // Apply criteria
                if (specification.Criteria != null)
                {
                    result = result.Where(specification.Criteria);
                }

                // Apply includes using EntityFramework's Include
                foreach (var include in specification.Includes)
                {
                    result = result.Include(include);
                }

                foreach (var includeString in specification.IncludeStrings)
                {
                    result = result.Include(includeString);
                }

                // Apply ordering
                if (specification.OrderBy != null)
                {
                    result = result.OrderBy(specification.OrderBy);
                }
                else if (specification.OrderByDescending != null)
                {
                    result = result.OrderByDescending(specification.OrderByDescending);
                }

                if (specification is IAdvancedSpecification<T> advSpec)
                {
                    result = ApplyAdvancedSpecification(result, advSpec);
                }

                // Apply paging
                if (specification.IsPagingEnabled)
                {
                    result = result.Skip(specification.Skip!.Value)
                                 .Take(specification.Take!.Value);
                }

                return result;
            };
        }

        private IQueryable<T> ApplyAdvancedSpecification<T>(
            IQueryable<T> query,
            IAdvancedSpecification<T> specification) where T : class
        {
            var result = query;

            // Apply sorting
            var sortFields = specification.SortFields.OrderBy(f => f.Order).ToList();

            if (!sortFields.Any())
                return result;

            // Apply primary sort
            var firstField = sortFields[0];
            result = firstField.Direction == SortDirection.Ascending
                ? result.OrderBy(BuildSortExpression<T>(firstField.PropertyName))
                : result.OrderByDescending(BuildSortExpression<T>(firstField.PropertyName));

            // Apply secondary sorts
            var orderedQuery = (IOrderedQueryable<T>)result;
            foreach (var field in sortFields.Skip(1))
            {
                orderedQuery = field.Direction == SortDirection.Ascending
                    ? orderedQuery.ThenBy(BuildSortExpression<T>(field.PropertyName))
                    : orderedQuery.ThenByDescending(BuildSortExpression<T>(field.PropertyName));
            }

            return orderedQuery;
        }

        private Expression<Func<T, object>> BuildSortExpression<T>(string propertyName)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyName);
            var conversion = Expression.Convert(property, typeof(object));
            return Expression.Lambda<Func<T, object>>(conversion, parameter);
        }

        private string BuildSqlQuery<T>(ISpecification<T> specification) where T : class
        {
            if (specification is ISqlSpecification<T> sqlSpec)
            {
                return sqlSpec.ToSql();
            }

            throw new InvalidOperationException(
                "Cannot build SQL query for specification that doesn't implement ISqlSpecification");
        }

        private void UpdateStatistics(CacheEntry entry)
        {
            entry.Hits++;
            entry.LastAccessed = DateTimeOffset.UtcNow;

            if (entry.Hits >= _options.MinimumHitsForCaching)
            {
                _statistics.RecordHit();
            }
            else
            {
                _statistics.RecordMiss();
            }
        }

        private void CleanupCache(object? state)
        {
            var now = DateTimeOffset.UtcNow;
            var keysToRemove = _cache
                .Where(kvp => 
                    (now - kvp.Value.LastAccessed) > _options.QueryTimeout ||
                    (kvp.Value.Hits < _options.MinimumHitsForCaching && 
                     (now - kvp.Value.Created) > TimeSpan.FromMinutes(5)))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
                _statistics.RecordEviction();
            }
        }

        private class CacheEntry
        {
            public object Value { get; }
            public DateTimeOffset Created { get; }
            public DateTimeOffset LastAccessed { get; set; }
            public int Hits { get; set; }

            public CacheEntry(object value)
            {
                Value = value;
                Created = DateTimeOffset.UtcNow;
                LastAccessed = Created;
                Hits = 0;
            }
        }
    }
}
