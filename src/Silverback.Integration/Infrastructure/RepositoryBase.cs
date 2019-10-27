// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Database;

namespace Silverback.Infrastructure
{
    public abstract class RepositoryBase<TEntity>
        where TEntity : class
    {
        protected readonly IDbContext DbContext;
        protected readonly IDbSet<TEntity> DbSet;

        protected RepositoryBase(IDbContext dbContext)
        {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            DbSet = dbContext.GetDbSet<TEntity>();
        }
    }
}