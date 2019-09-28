// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Silverback.Database
{
    public class EfCoreQueryable<TEntity> : IDbQueryable<TEntity>
        where TEntity : class
    {
        private readonly IQueryable<TEntity> _dbSet;

        public EfCoreQueryable(IQueryable<TEntity> dbSet)
        {
            _dbSet = dbSet ?? throw new ArgumentNullException(nameof(dbSet));
        }

        public IEnumerator<TEntity> GetEnumerator() => _dbSet.AsEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Type ElementType => _dbSet.AsQueryable().ElementType;
        public Expression Expression => _dbSet.AsQueryable().Expression;
        public IQueryProvider Provider => _dbSet.AsQueryable().Provider;

        public IDbQueryable<TEntity> AsNoTracking() => new EfCoreQueryable<TEntity>(_dbSet.AsNoTracking());

        public IDbQueryable<TEntity> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath) =>
            new EfCoreQueryable<TEntity>(_dbSet.Include(navigationPropertyPath));

        public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
            _dbSet.AnyAsync(cancellationToken);

        public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
            _dbSet.AnyAsync(predicate, cancellationToken);

        public Task<TEntity> FirstOrDefaultAsync(CancellationToken cancellationToken = default) =>
            _dbSet.FirstOrDefaultAsync(cancellationToken);

        public Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
            _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);

        public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
            _dbSet.CountAsync(cancellationToken);

        public Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
            _dbSet.CountAsync(predicate, cancellationToken);

        public Task<List<TResult>> ToListAsync<TResult>(Func<IQueryable<TEntity>, IQueryable<TResult>> query, CancellationToken cancellationToken = default) =>
            query(_dbSet).ToListAsync(cancellationToken);

        public Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TKey, TElement>(Func<IQueryable<TEntity>, IQueryable<TEntity>> query, Func<TEntity, TKey> keySelector, Func<TEntity, TElement> elementSelector, CancellationToken cancellationToken = default) =>
            query(_dbSet).ToDictionaryAsync(keySelector, elementSelector, cancellationToken);
    }
}