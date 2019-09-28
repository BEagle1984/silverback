using System;
using System.Collections.Generic;
using System.Text;
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
