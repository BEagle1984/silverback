// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Database
{
    public interface IDbContext
    {
        IDbSet<TEntity> GetDbSet<TEntity>() where TEntity : class;

        void SaveChanges();

        Task SaveChangesAsync();
    }
}
