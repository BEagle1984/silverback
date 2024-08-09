// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.EntityFrameworkCore;
using Silverback.Storage;

namespace Silverback.Lock;

/// <summary>
///     The <see cref="EntityFrameworkLock" /> settings.
/// </summary>
public record EntityFrameworkLockSettings : TableBasedDistributedLockSettings, IEntityFrameworkSettings
{
    private readonly Func<IServiceProvider, ISilverbackContext?, DbContext> _dbContextFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EntityFrameworkLockSettings" /> class.
    /// </summary>
    /// <param name="lockName">
    ///     The name of the lock.
    /// </param>
    /// <param name="dbContextType">
    ///     The type of the <see cref="DbContext" /> to be used to access the database.
    /// </param>
    /// <param name="dbContextFactory">
    ///     The factory method that creates the <see cref="DbContext" /> instance.
    /// </param>
    public EntityFrameworkLockSettings(string lockName, Type dbContextType, Func<IServiceProvider, ISilverbackContext?, DbContext> dbContextFactory)
        : base(lockName)
    {
        DbContextType = dbContextType;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc cref="IEntityFrameworkSettings.DbContextType" />
    public Type DbContextType { get; }

    /// <inheritdoc cref="IEntityFrameworkSettings.GetDbContext" />
    public DbContext GetDbContext(IServiceProvider serviceProvider, ISilverbackContext? context = null) =>
        _dbContextFactory.Invoke(serviceProvider, context);

    /// <inheritdoc cref="DistributedLockSettings.Validate" />
    public override void Validate()
    {
        base.Validate();

        if (DbContextType == null)
            throw new SilverbackConfigurationException("The DbContext type is required.");

        if (_dbContextFactory == null)
            throw new SilverbackConfigurationException("The DbContext factory is required.");
    }
}
