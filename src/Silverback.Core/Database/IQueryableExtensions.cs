// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Database
{
    [SuppressMessage("", "SA1600", Justification = "Internal and about to be deprecated")]
    internal interface IQueryableExtensions
    {
        Task<bool> AnyAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken = default);

        Task<bool> AnyAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default);

        Task<bool> AllAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default);

        Task<int> CountAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken = default);

        Task<int> CountAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default);

        Task<long> LongCountAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken = default);

        Task<long> LongCountAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default);

        Task<TSource> FirstAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken = default);

        Task<TSource> FirstAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default);

        Task<TSource> FirstOrDefaultAsync<TSource>(
            IQueryable<TSource> source,
            CancellationToken cancellationToken = default);

        Task<TSource> FirstOrDefaultAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default);

        Task<TSource> LastAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken = default);

        Task<TSource> LastAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default);

        Task<TSource> LastOrDefaultAsync<TSource>(
            IQueryable<TSource> source,
            CancellationToken cancellationToken = default);

        Task<TSource> LastOrDefaultAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default);

        Task<TSource> SingleAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken = default);

        Task<TSource> SingleAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default);

        Task<TSource> SingleOrDefaultAsync<TSource>(
            IQueryable<TSource> source,
            CancellationToken cancellationToken = default);

        Task<TSource> SingleOrDefaultAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default);

        Task<TSource> MinAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken = default);

        Task<TResult> MinAsync<TSource, TResult>(
            IQueryable<TSource> source,
            Expression<Func<TSource, TResult>> selector,
            CancellationToken cancellationToken = default);

        Task<TSource> MaxAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken = default);

        Task<TResult> MaxAsync<TSource, TResult>(
            IQueryable<TSource> source,
            Expression<Func<TSource, TResult>> selector,
            CancellationToken cancellationToken = default);

        Task<decimal> SumAsync(IQueryable<decimal> source, CancellationToken cancellationToken = default);

        Task<decimal?> SumAsync(IQueryable<decimal?> source, CancellationToken cancellationToken = default);

        Task<decimal> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, decimal>> selector,
            CancellationToken cancellationToken = default);

        Task<decimal?> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, decimal?>> selector,
            CancellationToken cancellationToken = default);

        Task<int> SumAsync(IQueryable<int> source, CancellationToken cancellationToken = default);

        Task<int?> SumAsync(IQueryable<int?> source, CancellationToken cancellationToken = default);

        Task<int> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, int>> selector,
            CancellationToken cancellationToken = default);

        Task<int?> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, int?>> selector,
            CancellationToken cancellationToken = default);

        Task<long> SumAsync(IQueryable<long> source, CancellationToken cancellationToken = default);

        Task<long?> SumAsync(IQueryable<long?> source, CancellationToken cancellationToken = default);

        Task<long> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, long>> selector,
            CancellationToken cancellationToken = default);

        Task<long?> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, long?>> selector,
            CancellationToken cancellationToken = default);

        Task<double> SumAsync(IQueryable<double> source, CancellationToken cancellationToken = default);

        Task<double?> SumAsync(IQueryable<double?> source, CancellationToken cancellationToken = default);

        Task<double> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, double>> selector,
            CancellationToken cancellationToken = default);

        Task<double?> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, double?>> selector,
            CancellationToken cancellationToken = default);

        Task<float> SumAsync(IQueryable<float> source, CancellationToken cancellationToken = default);

        Task<float?> SumAsync(IQueryable<float?> source, CancellationToken cancellationToken = default);

        Task<float> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, float>> selector,
            CancellationToken cancellationToken = default);

        Task<float?> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, float?>> selector,
            CancellationToken cancellationToken = default);

        Task<decimal> AverageAsync(IQueryable<decimal> source, CancellationToken cancellationToken = default);

        Task<decimal?> AverageAsync(IQueryable<decimal?> source, CancellationToken cancellationToken = default);

        Task<decimal> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, decimal>> selector,
            CancellationToken cancellationToken = default);

        Task<decimal?> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, decimal?>> selector,
            CancellationToken cancellationToken = default);

        Task<double> AverageAsync(IQueryable<int> source, CancellationToken cancellationToken = default);

        Task<double?> AverageAsync(IQueryable<int?> source, CancellationToken cancellationToken = default);

        Task<double> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, int>> selector,
            CancellationToken cancellationToken = default);

        Task<double?> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, int?>> selector,
            CancellationToken cancellationToken = default);

        Task<double> AverageAsync(IQueryable<long> source, CancellationToken cancellationToken = default);

        Task<double?> AverageAsync(IQueryable<long?> source, CancellationToken cancellationToken = default);

        Task<double> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, long>> selector,
            CancellationToken cancellationToken = default);

        Task<double?> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, long?>> selector,
            CancellationToken cancellationToken = default);

        Task<double> AverageAsync(IQueryable<double> source, CancellationToken cancellationToken = default);

        Task<double?> AverageAsync(IQueryable<double?> source, CancellationToken cancellationToken = default);

        Task<double> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, double>> selector,
            CancellationToken cancellationToken = default);

        Task<double?> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, double?>> selector,
            CancellationToken cancellationToken = default);

        Task<float> AverageAsync(IQueryable<float> source, CancellationToken cancellationToken = default);

        Task<float?> AverageAsync(IQueryable<float?> source, CancellationToken cancellationToken = default);

        Task<float> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, float>> selector,
            CancellationToken cancellationToken = default);

        Task<float?> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, float?>> selector,
            CancellationToken cancellationToken = default);

        Task<bool> ContainsAsync<TSource>(
            IQueryable<TSource> source,
            TSource item,
            CancellationToken cancellationToken = default);

        Task<List<TSource>> ToListAsync<TSource>(
            IQueryable<TSource> source,
            CancellationToken cancellationToken = default);

        Task<TSource[]> ToArrayAsync<TSource>(
            IQueryable<TSource> source,
            CancellationToken cancellationToken = default);

        IQueryable<TEntity> Include<TEntity, TProperty>(
            IQueryable<TEntity> source,
            Expression<Func<TEntity, TProperty>> navigationPropertyPath)
            where TEntity : class;

        IQueryable<TEntity> Include<TEntity>(IQueryable<TEntity> source, string navigationPropertyPath)
            where TEntity : class;

        IQueryable<TEntity> IgnoreQueryFilters<TEntity>(IQueryable<TEntity> source)
            where TEntity : class;

        IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> source)
            where TEntity : class;

        IQueryable<TEntity> AsTracking<TEntity>(IQueryable<TEntity> source)
            where TEntity : class;

        void Load<TSource>(IQueryable<TSource> source);

        Task LoadAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken = default);

        Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(
            IQueryable<TSource> source,
            Func<TSource, TKey> keySelector,
            CancellationToken cancellationToken = default)
            where TKey : notnull;

        Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(
            IQueryable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer,
            CancellationToken cancellationToken = default)
            where TKey : notnull;

        Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(
            IQueryable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            CancellationToken cancellationToken = default)
            where TKey : notnull;

        Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(
            IQueryable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer,
            CancellationToken cancellationToken = default)
            where TKey : notnull;

        Task ForEachAsync<T>(IQueryable<T> source, Action<T> action, CancellationToken cancellationToken = default);
    }
}
