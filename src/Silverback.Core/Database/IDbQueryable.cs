// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Database
{
    public interface IDbQueryable<TEntity> : IQueryable<TEntity>
        where TEntity : class
    {
        IDbQueryable<TEntity> AsNoTracking();

        IDbQueryable<TEntity> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath);

        Task<bool> AnyAsync(CancellationToken cancellationToken = default);

        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        Task<TEntity> FirstOrDefaultAsync(CancellationToken cancellationToken = default);

        Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        Task<int> CountAsync(CancellationToken cancellationToken = default);

        Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        Task<List<TResult>> ToListAsync<TResult>(Func<IQueryable<TEntity>, IQueryable<TResult>> query,
            CancellationToken cancellationToken = default);

        Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TKey, TElement>(
            Func<IQueryable<TEntity>, IQueryable<TEntity>> query, Func<TEntity, TKey> keySelector,
            Func<TEntity, TElement> elementSelector, CancellationToken cancellationToken = default);
    }
}