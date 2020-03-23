// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Silverback.Database
{
    public class EfCoreDbSet<TEntity> : IDbSet<TEntity>
        where TEntity : class
    {
        private readonly DbSet<TEntity> _dbSet;

        public EfCoreDbSet(DbSet<TEntity> dbSet)
        {
            _dbSet = dbSet;
        }

        public TEntity Add(TEntity entity) => _dbSet.Add(entity).Entity;

        public TEntity Remove(TEntity entity) => _dbSet.Remove(entity).Entity;

        public void RemoveRange(IEnumerable<TEntity> entities) => _dbSet.RemoveRange(entities);

        public TEntity Find(params object[] keyValues) => _dbSet.Find(keyValues);

        public async Task<TEntity> FindAsync(params object[] keyValues) => await _dbSet.FindAsync(keyValues);

        public IQueryable<TEntity> AsQueryable() => _dbSet;

        public IEnumerable<TEntity> GetLocalCache() => _dbSet.Local;
    }
}