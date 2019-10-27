// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Database
{
    internal static class SilverbackQueryableExtensions
    {
        public static IQueryableExtensions Implementation { get; set; }

        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
            Implementation.AnyAsync(source, cancellationToken);

        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
            Implementation.AnyAsync(source, predicate, cancellationToken);

        public static Task<bool> AllAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
            Implementation.AllAsync(source, predicate, cancellationToken);

        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
            Implementation.CountAsync(source, cancellationToken);

        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
            Implementation.CountAsync(source, predicate, cancellationToken);

        public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
            Implementation.LongCountAsync(source, cancellationToken);

        public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
            Implementation.LongCountAsync(source, predicate, cancellationToken);

        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
            Implementation.FirstAsync(source, cancellationToken);

        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
            Implementation.FirstAsync(source, predicate, cancellationToken);

        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
            Implementation.FirstOrDefaultAsync(source, cancellationToken);

        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
            Implementation.FirstOrDefaultAsync(source, predicate, cancellationToken);

        public static Task<TSource> LastAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
            Implementation.LastAsync(source, cancellationToken);

        public static Task<TSource> LastAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
            Implementation.LastAsync(source, predicate, cancellationToken);

        public static Task<TSource> LastOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
            Implementation.LastOrDefaultAsync(source, cancellationToken);

        public static Task<TSource> LastOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
            Implementation.LastOrDefaultAsync(source, predicate, cancellationToken);

        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
            Implementation.SingleAsync(source, cancellationToken);

        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
            Implementation.SingleAsync(source, predicate, cancellationToken);

        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
            Implementation.SingleOrDefaultAsync(source, cancellationToken);

        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default) =>
            Implementation.SingleOrDefaultAsync(source, predicate, cancellationToken);

        public static Task<TSource> MinAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
            Implementation.MinAsync(source, cancellationToken);

        public static Task<TResult> MinAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default) =>
            Implementation.MinAsync(source, selector, cancellationToken);

        public static Task<TSource> MaxAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
            Implementation.MaxAsync(source, cancellationToken);

        public static Task<TResult> MaxAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken cancellationToken = default) =>
            Implementation.MaxAsync(source, selector, cancellationToken);

        public static Task<decimal> SumAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, cancellationToken);
        
        public static Task<decimal?> SumAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, cancellationToken);

        public static Task<decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, selector, cancellationToken);

        public static Task<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, selector, cancellationToken);

        public static Task<int> SumAsync(this IQueryable<int> source, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, cancellationToken);

        public static Task<int?> SumAsync(this IQueryable<int?> source, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, cancellationToken);

        public static Task<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, selector, cancellationToken);

        public static Task<int?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, selector, cancellationToken);

        public static Task<long> SumAsync(this IQueryable<long> source, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, cancellationToken);

        public static Task<long?> SumAsync(this IQueryable<long?> source, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, cancellationToken);

        public static Task<long> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, selector, cancellationToken);

        public static Task<long?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, selector, cancellationToken);

        public static Task<double> SumAsync(this IQueryable<double> source, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, cancellationToken);

        public static Task<double?> SumAsync(this IQueryable<double?> source, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, cancellationToken);

        public static Task<double> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, selector, cancellationToken);

        public static Task<double?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, selector, cancellationToken);

        public static Task<float> SumAsync(this IQueryable<float> source, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, cancellationToken);

        public static Task<float?> SumAsync(this IQueryable<float?> source, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, cancellationToken);

        public static Task<float> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, selector, cancellationToken);

        public static Task<float?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken = default) =>
            Implementation.SumAsync(source, selector, cancellationToken);

        public static Task<decimal> AverageAsync(this IQueryable<decimal> source, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, cancellationToken);

        public static Task<decimal?> AverageAsync(this IQueryable<decimal?> source, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, cancellationToken);

        public static Task<decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, selector, cancellationToken);

        public static Task<decimal?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, selector, cancellationToken);

        public static Task<double> AverageAsync(this IQueryable<int> source, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, cancellationToken);

        public static Task<double?> AverageAsync(this IQueryable<int?> source, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, cancellationToken);

        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, selector, cancellationToken);

        public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, selector, cancellationToken);

        public static Task<double> AverageAsync(this IQueryable<long> source, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, cancellationToken);

        public static Task<double?> AverageAsync(this IQueryable<long?> source, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, cancellationToken);

        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, selector, cancellationToken);

        public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, selector, cancellationToken);

        public static Task<double> AverageAsync(this IQueryable<double> source, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, cancellationToken);

        public static Task<double?> AverageAsync(this IQueryable<double?> source, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, cancellationToken);

        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, selector, cancellationToken);

        public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, selector, cancellationToken);

        public static Task<float> AverageAsync(this IQueryable<float> source, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, cancellationToken);

        public static Task<float?> AverageAsync(this IQueryable<float?> source, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, cancellationToken);

        public static Task<float> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, selector, cancellationToken);

        public static Task<float?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken cancellationToken = default) =>
            Implementation.AverageAsync(source, selector, cancellationToken);
        
        public static Task<bool> ContainsAsync<TSource>(this IQueryable<TSource> source, TSource item, CancellationToken cancellationToken = default) =>
            Implementation.ContainsAsync(source, item, cancellationToken);

        public static Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
            Implementation.ToListAsync(source, cancellationToken);

        public static Task<TSource[]> ToArrayAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
            Implementation.ToArrayAsync(source, cancellationToken);

        public static IQueryable<TEntity> Include<TEntity, TProperty>(this IQueryable<TEntity> source, Expression<Func<TEntity, TProperty>> navigationPropertyPath)
            where TEntity : class =>
            Implementation.Include(source, navigationPropertyPath);
        
        public static IQueryable<TEntity> Include<TEntity>(this IQueryable<TEntity> source, string navigationPropertyPath)
            where TEntity : class =>
            Implementation.Include(source, navigationPropertyPath);

        public static IQueryable<TEntity> IgnoreQueryFilters<TEntity>(this IQueryable<TEntity> source)
            where TEntity : class =>
            Implementation.IgnoreQueryFilters(source);

        public static IQueryable<TEntity> AsNoTracking<TEntity>(this IQueryable<TEntity> source)
            where TEntity : class =>
            Implementation.AsNoTracking(source);
        
        public static IQueryable<TEntity> AsTracking<TEntity>(this IQueryable<TEntity> source)
            where TEntity : class =>
            Implementation.AsTracking(source);

        public static void Load<TSource>(this IQueryable<TSource> source) =>
            Implementation.Load(source);

        public static Task LoadAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
            Implementation.LoadAsync(source, cancellationToken);

        public static Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, CancellationToken cancellationToken = default) =>
            Implementation.ToDictionaryAsync(source, keySelector, cancellationToken);

        public static Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer, CancellationToken cancellationToken = default) =>
            Implementation.ToDictionaryAsync(source, keySelector, comparer, cancellationToken);

        public static Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, CancellationToken cancellationToken = default) =>
            Implementation.ToDictionaryAsync(source, keySelector, elementSelector, cancellationToken);

        public static Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(this IQueryable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer, CancellationToken cancellationToken = default) =>
            Implementation.ToDictionaryAsync(source, keySelector, elementSelector, comparer, cancellationToken);

        public static Task ForEachAsync<T>(this IQueryable<T> source, Action<T> action, CancellationToken cancellationToken = default) =>
            Implementation.ForEachAsync(source, action, cancellationToken);
    }
}