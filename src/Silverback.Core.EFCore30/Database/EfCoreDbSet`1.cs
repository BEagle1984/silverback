﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Silverback.Database;

/// <summary>
///     An implementation of <see cref="IDbSet{TEntity}" /> that works with Entity Framework Core.
/// </summary>
/// <typeparam name="TEntity">
///     The type of the entity being stored in this set.
/// </typeparam>
public class EfCoreDbSet<TEntity> : IDbSet<TEntity>
    where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EfCoreDbSet{TEntity}" /> class.
    /// </summary>
    /// <param name="dbSet">
    ///     The underlying <see cref="DbSet{TEntity}" />.
    /// </param>
    public EfCoreDbSet(DbSet<TEntity> dbSet)
    {
        _dbSet = dbSet;
    }

    /// <inheritdoc cref="IDbSet{TEntity}.Add" />
    public TEntity Add(TEntity entity) => _dbSet.Add(entity).Entity;

    /// <inheritdoc cref="IDbSet{TEntity}.Remove" />
    public TEntity Remove(TEntity entity) => _dbSet.Remove(entity).Entity;

    /// <inheritdoc cref="IDbSet{TEntity}.RemoveRange" />
    public void RemoveRange(IEnumerable<TEntity> entities) => _dbSet.RemoveRange(entities);

    /// <inheritdoc cref="IDbSet{TEntity}.Find" />
    public TEntity? Find(params object[] keyValues) => _dbSet.Find(keyValues);

    /// <inheritdoc cref="IDbSet{TEntity}.FindAsync" />
    public async Task<TEntity?> FindAsync(params object[] keyValues) =>
        await _dbSet.FindAsync(keyValues).ConfigureAwait(false);

    /// <inheritdoc cref="IDbSet{TEntity}.AsQueryable" />
    public IQueryable<TEntity> AsQueryable() => _dbSet;

    /// <inheritdoc cref="IDbSet{TEntity}.GetLocalCache" />
    [SuppressMessage("", "CA1024", Justification = "It must stay a method for backward compatibility")]
    public IEnumerable<TEntity> GetLocalCache() => _dbSet.Local;
}
