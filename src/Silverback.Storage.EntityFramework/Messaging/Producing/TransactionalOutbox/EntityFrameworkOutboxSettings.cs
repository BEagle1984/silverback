// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.EntityFrameworkCore;
using Silverback.Lock;
using Silverback.Storage;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     The <see cref="EntityFrameworkOutboxWriter" /> and <see cref="EntityFrameworkOutboxReader" /> settings.
/// </summary>
public record EntityFrameworkOutboxSettings : OutboxSettings, IEntityFrameworkSettings
{
    private readonly Func<IServiceProvider, ISilverbackContext?, DbContext> _dbContextFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EntityFrameworkOutboxSettings" /> class.
    /// </summary>
    /// <param name="dbContextType">
    ///     The type of the <see cref="DbContext" /> to be used to access the database.
    /// </param>
    /// <param name="dbContextFactory">
    ///     The factory method that creates the <see cref="DbContext" /> instance.
    /// </param>
    public EntityFrameworkOutboxSettings(Type dbContextType, Func<IServiceProvider, ISilverbackContext?, DbContext> dbContextFactory)
    {
        DbContextType = dbContextType;
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    ///     Gets the type of the <see cref="DbContext" /> to be used to access the database.
    /// </summary>
    public Type DbContextType { get; }

    /// <summary>
    ///     Gets the factory method that creates the <see cref="DbContext" /> instance.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the <see cref="DbContext" />.
    /// </param>
    /// <param name="context">
    ///     The <see cref="ISilverbackContext" />.
    /// </param>
    /// <returns>
    ///     The <see cref="DbContext" /> instance.
    /// </returns>
    public DbContext GetDbContext(IServiceProvider serviceProvider, ISilverbackContext? context = null) =>
        _dbContextFactory.Invoke(serviceProvider, context);

    /// <summary>
    ///     Returns an instance of <see cref="EntityFrameworkLockSettings" />.
    /// </summary>
    /// <returns>
    ///     The <see cref="EntityFrameworkLockSettings" />.
    /// </returns>
    public override DistributedLockSettings GetCompatibleLockSettings() =>
        new EntityFrameworkLockSettings($"outbox.{DbContextType.Name}", DbContextType, _dbContextFactory);

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
