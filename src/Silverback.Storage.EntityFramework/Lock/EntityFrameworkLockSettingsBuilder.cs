// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.EntityFrameworkCore;
using Silverback.Configuration;

namespace Silverback.Lock;

/// <summary>
///     Builds the <see cref="EntityFrameworkLockSettings" />.
/// </summary>
public class EntityFrameworkLockSettingsBuilder : IDistributedLockSettingsImplementationBuilder
{
    private readonly string _lockName;

    private readonly Type _dbContextType;

    private readonly Func<IServiceProvider, ISilverbackContext?, DbContext> _dbContextFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EntityFrameworkLockSettingsBuilder" /> class.
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
    public EntityFrameworkLockSettingsBuilder(string lockName, Type dbContextType, Func<IServiceProvider, ISilverbackContext?, DbContext> dbContextFactory)
    {
        _lockName = lockName;
        _dbContextType = dbContextType;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc cref="IDistributedLockSettingsImplementationBuilder.Build" />
    public DistributedLockSettings Build()
    {
        EntityFrameworkLockSettings settings = new(_lockName, _dbContextType, _dbContextFactory);

        settings.Validate();

        return settings;
    }
}
