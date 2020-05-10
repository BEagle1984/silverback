// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Silverback.Database
{
    /// <summary>
    ///     An implementation of <see cref="IDbContext" /> that works with Entity Framework Core.
    /// </summary>
    /// <typeparam name="TDbContext">
    ///     The type of the underlying <see cref="DbContext" />.
    /// </typeparam>
    public class EfCoreDbContext<TDbContext> : IDbContext
        where TDbContext : DbContext
    {
        private readonly TDbContext _dbContext;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EfCoreDbContext{TDbContext}" /> class.
        /// </summary>
        /// <param name="dbContext">
        ///     The type of the underlying <see cref="DbContext" />.
        /// </param>
        public EfCoreDbContext(TDbContext dbContext)
        {
            _dbContext = Check.NotNull(dbContext, nameof(dbContext));
        }

        /// <inheritdoc />
        public IDbSet<TEntity> GetDbSet<TEntity>()
            where TEntity : class =>
            new EfCoreDbSet<TEntity>(
                _dbContext.Set<TEntity>() ??
                throw new DatabaseTableNotFoundException(
                    $"The DbContext doesn't contain a DbSet<{typeof(TEntity).FullName}>."));

        /// <inheritdoc />
        public void SaveChanges() => _dbContext.SaveChanges();

        /// <inheritdoc />
        public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
    }
}
