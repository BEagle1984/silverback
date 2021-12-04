// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Database;
using Silverback.Util;

namespace Silverback.Infrastructure;

/// <summary>
///     The base class for the repositories used to read and write data from a persistent storage.
/// </summary>
/// <typeparam name="TEntity">
///     The type of the entities being managed by the repository.
/// </typeparam>
public abstract class RepositoryBase<TEntity>
    where TEntity : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RepositoryBase{TEntity}" /> class.
    /// </summary>
    /// <param name="dbContext">
    ///     The <see cref="IDbContext" /> to be used.
    /// </param>
    protected RepositoryBase(IDbContext dbContext)
    {
        DbContext = Check.NotNull(dbContext, nameof(dbContext));
        DbSet = dbContext.GetDbSet<TEntity>();
    }

    /// <summary>
    ///     Gets the underlying <see cref="IDbContext" />.
    /// </summary>
    protected IDbContext DbContext { get; }

    /// <summary>
    ///     Gets the underlying <see cref="IDbSet{TEntity}" />.
    /// </summary>
    protected IDbSet<TEntity> DbSet { get; }
}
