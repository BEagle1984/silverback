// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Silverback.Database;

internal sealed class EfCoreQueryableExtensions : IQueryableExtensions
{
    public Task<bool> AnyAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AnyAsync(source, cancellationToken);

    public Task<bool> AnyAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AnyAsync(source, predicate, cancellationToken);

    public Task<bool> AllAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AllAsync(source, predicate, cancellationToken);

    public Task<int> CountAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.CountAsync(source, cancellationToken);

    public Task<int> CountAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.CountAsync(source, predicate, cancellationToken);

    public Task<long> LongCountAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.LongCountAsync(source, cancellationToken);

    public Task<long> LongCountAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.LongCountAsync(source, predicate, cancellationToken);

    public Task<TSource> FirstAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.FirstAsync(source, cancellationToken);

    public Task<TSource> FirstAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.FirstAsync(source, predicate, cancellationToken);

    public Task<TSource> FirstOrDefaultAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(source, cancellationToken);

    public Task<TSource?> FirstOrDefaultAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(source, predicate, cancellationToken)!;

    public Task<TSource> LastAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.LastAsync(source, cancellationToken);

    public Task<TSource> LastAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.LastAsync(source, predicate, cancellationToken);

    public Task<TSource> LastOrDefaultAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.LastOrDefaultAsync(source, cancellationToken);

    public Task<TSource> LastOrDefaultAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.LastOrDefaultAsync(source, predicate, cancellationToken);

    public Task<TSource> SingleAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SingleAsync(source, cancellationToken);

    public Task<TSource> SingleAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SingleAsync(source, predicate, cancellationToken);

    public Task<TSource> SingleOrDefaultAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(source, cancellationToken);

    public Task<TSource> SingleOrDefaultAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SingleOrDefaultAsync(source, predicate, cancellationToken);

    public Task<TSource> MinAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.MinAsync(source, cancellationToken);

    public Task<TResult> MinAsync<TSource, TResult>(
        IQueryable<TSource> source,
        Expression<Func<TSource, TResult>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.MinAsync(source, selector, cancellationToken);

    public Task<TSource> MaxAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.MaxAsync(source, cancellationToken);

    public Task<TResult> MaxAsync<TSource, TResult>(
        IQueryable<TSource> source,
        Expression<Func<TSource, TResult>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.MaxAsync(source, selector, cancellationToken);

    public Task<decimal> SumAsync(IQueryable<decimal> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

    public Task<decimal?> SumAsync(IQueryable<decimal?> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

    public Task<decimal> SumAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, decimal>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

    public Task<decimal?> SumAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, decimal?>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

    public Task<int> SumAsync(IQueryable<int> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

    public Task<int?> SumAsync(IQueryable<int?> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

    public Task<int> SumAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, int>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

    public Task<int?> SumAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, int?>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

    public Task<long> SumAsync(IQueryable<long> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

    public Task<long?> SumAsync(IQueryable<long?> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

    public Task<long> SumAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, long>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

    public Task<long?> SumAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, long?>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

    public Task<double> SumAsync(IQueryable<double> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

    public Task<double?> SumAsync(IQueryable<double?> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

    public Task<double> SumAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, double>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

    public Task<double?> SumAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, double?>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

    public Task<float> SumAsync(IQueryable<float> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

    public Task<float?> SumAsync(IQueryable<float?> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, cancellationToken);

    public Task<float> SumAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, float>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

    public Task<float?> SumAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, float?>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.SumAsync(source, selector, cancellationToken);

    public Task<decimal> AverageAsync(IQueryable<decimal> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

    public Task<decimal?> AverageAsync(
        IQueryable<decimal?> source,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

    public Task<decimal> AverageAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, decimal>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

    public Task<decimal?> AverageAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, decimal?>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

    public Task<double> AverageAsync(IQueryable<int> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

    public Task<double?> AverageAsync(IQueryable<int?> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

    public Task<double> AverageAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, int>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

    public Task<double?> AverageAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, int?>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

    public Task<double> AverageAsync(IQueryable<long> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

    public Task<double?> AverageAsync(IQueryable<long?> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

    public Task<double> AverageAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, long>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

    public Task<double?> AverageAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, long?>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

    public Task<double> AverageAsync(IQueryable<double> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

    public Task<double?> AverageAsync(IQueryable<double?> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

    public Task<double> AverageAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, double>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

    public Task<double?> AverageAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, double?>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

    public Task<float> AverageAsync(IQueryable<float> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

    public Task<float?> AverageAsync(IQueryable<float?> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, cancellationToken);

    public Task<float> AverageAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, float>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

    public Task<float?> AverageAsync<TSource>(
        IQueryable<TSource> source,
        Expression<Func<TSource, float?>> selector,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.AverageAsync(source, selector, cancellationToken);

    public Task<bool> ContainsAsync<TSource>(
        IQueryable<TSource> source,
        TSource item,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.ContainsAsync(source, item, cancellationToken);

    public Task<List<TSource>> ToListAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.ToListAsync(source, cancellationToken);

    public Task<TSource[]> ToArrayAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.ToArrayAsync(source, cancellationToken);

    public IQueryable<TEntity> Include<TEntity, TProperty>(
        IQueryable<TEntity> source,
        Expression<Func<TEntity, TProperty>> navigationPropertyPath)
        where TEntity : class =>
        EntityFrameworkQueryableExtensions.Include(source, navigationPropertyPath);

    public IQueryable<TEntity> Include<TEntity>(IQueryable<TEntity> source, string navigationPropertyPath)
        where TEntity : class =>
        EntityFrameworkQueryableExtensions.Include(source, navigationPropertyPath);

    public IQueryable<TEntity> IgnoreQueryFilters<TEntity>(IQueryable<TEntity> source)
        where TEntity : class =>
        EntityFrameworkQueryableExtensions.IgnoreQueryFilters(source);

    public IQueryable<TEntity> AsNoTracking<TEntity>(IQueryable<TEntity> source)
        where TEntity : class =>
        EntityFrameworkQueryableExtensions.AsNoTracking(source);

    public IQueryable<TEntity> AsTracking<TEntity>(IQueryable<TEntity> source)
        where TEntity : class =>
        EntityFrameworkQueryableExtensions.AsTracking(source);

    public void Load<TSource>(IQueryable<TSource> source) =>
        EntityFrameworkQueryableExtensions.Load(source);

    public Task LoadAsync<TSource>(IQueryable<TSource> source, CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.LoadAsync(source, cancellationToken);

    public Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(
        IQueryable<TSource> source,
        Func<TSource, TKey> keySelector,
        CancellationToken cancellationToken = default)
        where TKey : notnull =>
        EntityFrameworkQueryableExtensions.ToDictionaryAsync(source, keySelector, cancellationToken);

    public Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(
        IQueryable<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey> comparer,
        CancellationToken cancellationToken = default)
        where TKey : notnull =>
        EntityFrameworkQueryableExtensions.ToDictionaryAsync(source, keySelector, comparer, cancellationToken);

    public Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(
        IQueryable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector,
        CancellationToken cancellationToken = default)
        where TKey : notnull =>
        EntityFrameworkQueryableExtensions.ToDictionaryAsync(
            source,
            keySelector,
            elementSelector,
            cancellationToken);

    public Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(
        IQueryable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector,
        IEqualityComparer<TKey> comparer,
        CancellationToken cancellationToken = default)
        where TKey : notnull =>
        EntityFrameworkQueryableExtensions.ToDictionaryAsync(
            source,
            keySelector,
            elementSelector,
            comparer,
            cancellationToken);

    public Task ForEachAsync<T>(
        IQueryable<T> source,
        Action<T> action,
        CancellationToken cancellationToken = default) =>
        EntityFrameworkQueryableExtensions.ForEachAsync(source, action, cancellationToken);
}
