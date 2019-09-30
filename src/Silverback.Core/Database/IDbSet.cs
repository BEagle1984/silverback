// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Silverback.Database
{
    public interface IDbSet<TEntity>
        where TEntity : class
    {
        TEntity Add(TEntity entity);

        TEntity Remove(TEntity entity);

        void RemoveRange(IEnumerable<TEntity> entities);

        TEntity Find(params object[] keyValues);

        Task<TEntity> FindAsync(params object[] keyValues);

        IQueryable<TEntity> AsQueryable();

        IEnumerable<TEntity> GetLocalCache();
    }
}