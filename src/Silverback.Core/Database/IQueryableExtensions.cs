// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1506 // Excessive coupling -> this approach is anyway to be deprecated

namespace Silverback.Database
{
    [SuppressMessage("", "SA1600", Justification = "Internal and about to be deprecated")]
    internal interface IQueryableExtensions
    {
        Task<bool> AnyAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken);

        Task<bool> AnyAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken);

        Task<bool> AllAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken);

        Task<int> CountAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken);

        Task<int> CountAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken);

        Task<long> LongCountAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken);

        Task<long> LongCountAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken);

        Task<TSource> FirstAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken);

        Task<TSource> FirstAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken);

        Task<TSource> FirstOrDefaultAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken);

        Task<TSource> FirstOrDefaultAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken);

        Task<TSource> LastAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken);

        Task<TSource> LastAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken);

        Task<TSource> LastOrDefaultAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken);

        Task<TSource> LastOrDefaultAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken);

        Task<TSource> SingleAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken);

        Task<TSource> SingleAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken);

        Task<TSource> SingleOrDefaultAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken);

        Task<TSource> SingleOrDefaultAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken);

        Task<TSource> MinAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken);

        Task<TResult> MinAsync<TSource, TResult>(
            IQueryable<TSource> source,
            Expression<Func<TSource, TResult>> selector,
            CancellationToken cancellationToken);

        Task<TSource> MaxAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken);

        Task<TResult> MaxAsync<TSource, TResult>(
            IQueryable<TSource> source,
            Expression<Func<TSource, TResult>> selector,
            CancellationToken cancellationToken);

        Task<decimal> SumAsync(IQueryable<decimal> source, CancellationToken cancellationToken);

        Task<decimal?> SumAsync(IQueryable<decimal?> source, CancellationToken cancellationToken);

        Task<decimal> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, decimal>> selector,
            CancellationToken cancellationToken);

        Task<decimal?> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, decimal?>> selector,
            CancellationToken cancellationToken);

        Task<int> SumAsync(IQueryable<int> source, CancellationToken cancellationToken);

        Task<int?> SumAsync(IQueryable<int?> source, CancellationToken cancellationToken);

        Task<int> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, int>> selector,
            CancellationToken cancellationToken);

        Task<int?> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, int?>> selector,
            CancellationToken cancellationToken);

        Task<long> SumAsync(IQueryable<long> source, CancellationToken cancellationToken);

        Task<long?> SumAsync(IQueryable<long?> source, CancellationToken cancellationToken);

        Task<long> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, long>> selector,
            CancellationToken cancellationToken);

        Task<long?> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, long?>> selector,
            CancellationToken cancellationToken);

        Task<double> SumAsync(IQueryable<double> source, CancellationToken cancellationToken);

        Task<double?> SumAsync(IQueryable<double?> source, CancellationToken cancellationToken);

        Task<double> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, double>> selector,
            CancellationToken cancellationToken);

        Task<double?> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, double?>> selector,
            CancellationToken cancellationToken);

        Task<float> SumAsync(IQueryable<float> source, CancellationToken cancellationToken);

        Task<float?> SumAsync(IQueryable<float?> source, CancellationToken cancellationToken);

        Task<float> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, float>> selector,
            CancellationToken cancellationToken);

        Task<float?> SumAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, float?>> selector,
            CancellationToken cancellationToken);

        Task<decimal> AverageAsync(IQueryable<decimal> source, CancellationToken cancellationToken);

        Task<decimal?> AverageAsync(IQueryable<decimal?> source, CancellationToken cancellationToken);

        Task<decimal> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, decimal>> selector,
            CancellationToken cancellationToken);

        Task<decimal?> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, decimal?>> selector,
            CancellationToken cancellationToken);

        Task<double> AverageAsync(IQueryable<int> source, CancellationToken cancellationToken);

        Task<double?> AverageAsync(IQueryable<int?> source, CancellationToken cancellationToken);

        Task<double> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, int>> selector,
            CancellationToken cancellationToken);

        Task<double?> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, int?>> selector,
            CancellationToken cancellationToken);

        Task<double> AverageAsync(IQueryable<long> source, CancellationToken cancellationToken);

        Task<double?> AverageAsync(IQueryable<long?> source, CancellationToken cancellationToken);

        Task<double> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, long>> selector,
            CancellationToken cancellationToken);

        Task<double?> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, long?>> selector,
            CancellationToken cancellationToken);

        Task<double> AverageAsync(IQueryable<double> source, CancellationToken cancellationToken);

        Task<double?> AverageAsync(IQueryable<double?> source, CancellationToken cancellationToken);

        Task<double> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, double>> selector,
            CancellationToken cancellationToken);

        Task<double?> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, double?>> selector,
            CancellationToken cancellationToken);

        Task<float> AverageAsync(IQueryable<float> source, CancellationToken cancellationToken);

        Task<float?> AverageAsync(IQueryable<float?> source, CancellationToken cancellationToken);

        Task<float> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, float>> selector,
            CancellationToken cancellationToken);

        Task<float?> AverageAsync<TSource>(
            IQueryable<TSource> source,
            Expression<Func<TSource, float?>> selector,
            CancellationToken cancellationToken);

        Task<bool> ContainsAsync<TSource>(
            IQueryable<TSource> source,
            TSource item,
            CancellationToken cancellationToken);

        Task<List<TSource>> ToListAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken);

        Task<TSource[]> ToArrayAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken);

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
            CancellationToken cancellationToken);

        Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(
            IQueryable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer,
            CancellationToken cancellationToken);

        Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(
            IQueryable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            CancellationToken cancellationToken);

        Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(
            IQueryable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer,
            CancellationToken cancellationToken);

        Task ForEachAsync<T>(IQueryable<T> source, Action<T> action, CancellationToken cancellationToken);
    }
}
