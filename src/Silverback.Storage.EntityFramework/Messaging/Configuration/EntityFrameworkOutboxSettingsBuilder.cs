// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="EntityFrameworkOutboxSettings" />.
/// </summary>
public class EntityFrameworkOutboxSettingsBuilder : IOutboxSettingsImplementationBuilder
{
    private readonly Type _dbContextType;

    private readonly Func<IServiceProvider, SilverbackContext?, DbContext> _dbContextFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EntityFrameworkOutboxSettingsBuilder" /> class.
    /// </summary>
    /// <param name="dbContextType">
    ///     The type of the <see cref="DbContext" /> to be used to access the database.
    /// </param>
    /// <param name="dbContextFactory">
    ///     The factory method that creates the <see cref="DbContext" /> instance.
    /// </param>
    public EntityFrameworkOutboxSettingsBuilder(Type dbContextType, Func<IServiceProvider, SilverbackContext?, DbContext> dbContextFactory)
    {
        _dbContextType = dbContextType;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc cref="IOutboxSettingsImplementationBuilder.Build" />
    public OutboxSettings Build()
    {
        EntityFrameworkOutboxSettings settings = new(_dbContextType, _dbContextFactory);

        settings.Validate();

        return settings;
    }
}
