﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Silverback.Database
{
    public class EfCoreDbContext<TDbContext> : IDbContext
        where TDbContext : DbContext
    {
        private readonly TDbContext _dbContext;

        public EfCoreDbContext(TDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public IDbSet<TEntity> GetDbSet<TEntity>()
            where TEntity : class =>
            new EfCoreDbSet<TEntity>(
                _dbContext.Set<TEntity>() ??
                throw new SilverbackException($"The DbContext doesn't contain a DbSet<{typeof(TEntity).FullName}>."));

        public void SaveChanges() => _dbContext.SaveChanges();

        public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
    }
}